using Android.App;
using Android.Content;
using Android.Provider;
using AndroidX.Work;
using MAUI.Domain.Models;
using MAUI.Platforms.Android.Receivers;
using MAUI.Platforms.Android.Workers;

namespace MAUI.Infrastructure.Android.Scheduling;

internal static class WallpaperSchedulerEngine
{
	private static readonly object SyncRoot = new();

	internal const string ExtraSchedulerId = "extra_scheduler_id";
	internal const string WorkInputEndpointUrl = "work_input_endpoint_url";

	internal static string AddAndSchedule(
		Context context,
		string endpointUrl,
		long triggerAtMillis,
		bool repeatDaily)
	{
		var safeTriggerAt = Math.Max(triggerAtMillis, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1_000L);
		SchedulerEntry entry;

		lock (SyncRoot)
		{
			var entries = SchedulerStorage.LoadUnsafe(context);

			var existingEntry = entries.FirstOrDefault(item =>
				SchedulerRules.IsSameSchedule(item, endpointUrl, safeTriggerAt, repeatDaily));

			if (existingEntry is not null)
			{
				entry = existingEntry;
			}
			else
			{
				entry = new SchedulerEntry
				{
					Id = Guid.NewGuid().ToString(),
					EndpointUrl = endpointUrl,
					TriggerAtMillis = safeTriggerAt,
					RepeatDaily = repeatDaily,
				};
				entries.Add(entry);
			}

			SchedulerStorage.PersistUnsafe(context, entries);
		}

		ScheduleAlarm(context, entry);
		return entry.Id;
	}

	internal static IReadOnlyList<SchedulerEntry> ListSchedulers(Context context)
	{
		lock (SyncRoot)
		{
			return SchedulerStorage.LoadUnsafe(context)
				.OrderBy(entry => entry.TriggerAtMillis)
				.ToArray();
		}
	}

	internal static bool DeleteScheduler(Context context, string schedulerId)
	{
		var removed = false;
		lock (SyncRoot)
		{
			var entries = SchedulerStorage.LoadUnsafe(context);
			removed = entries.RemoveAll(entry => entry.Id == schedulerId) > 0;
			if (removed)
			{
				SchedulerStorage.PersistUnsafe(context, entries);
			}
		}

		if (removed)
		{
			CancelAlarm(context, schedulerId);
		}

		return removed;
	}

	internal static async Task<bool> RunNowAsync(
		Context context,
		string endpointUrl,
		CancellationToken cancellationToken = default)
	{
		var result = await WallpaperExecutionEngine
			.ExecuteAsync(context, endpointUrl, cancellationToken)
			.ConfigureAwait(false);
		return result == WallpaperExecutionResult.Success;
	}

	internal static void RescheduleFromStorage(Context context)
	{
		var nowMillis = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
		SchedulerEntry[] entriesToSchedule;

		lock (SyncRoot)
		{
			var currentEntries = SchedulerStorage.LoadUnsafe(context);
			var updatedEntries = new List<SchedulerEntry>(capacity: currentEntries.Count);

			foreach (var entry in currentEntries)
			{
				if (entry.RepeatDaily)
				{
					var nextTriggerAt = SchedulerRules.ComputeNextDailyTrigger(entry.TriggerAtMillis, nowMillis);
					updatedEntries.Add(entry with { TriggerAtMillis = nextTriggerAt });
					continue;
				}

				if (entry.TriggerAtMillis > nowMillis)
				{
					updatedEntries.Add(entry);
				}
			}

			SchedulerStorage.PersistUnsafe(context, updatedEntries);
			entriesToSchedule = updatedEntries.ToArray();
		}

		foreach (var entry in entriesToSchedule)
		{
			if (!entry.RepeatDaily && entry.TriggerAtMillis <= nowMillis)
			{
				continue;
			}

			ScheduleAlarm(context, entry);
		}
	}

	internal static void OnAlarmTriggered(Context context, string schedulerId)
	{
		SchedulerEntry? entry = null;
		SchedulerEntry? nextDailyEntry = null;

		lock (SyncRoot)
		{
			var entries = SchedulerStorage.LoadUnsafe(context);
			var index = entries.FindIndex(item => item.Id == schedulerId);
			if (index < 0)
			{
				return;
			}

			entry = entries[index];
			if (entry.RepeatDaily)
			{
				var nextTriggerAt = SchedulerRules.ComputeNextDailyTrigger(
					entry.TriggerAtMillis,
					DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
				nextDailyEntry = entry with { TriggerAtMillis = nextTriggerAt };
				entries[index] = nextDailyEntry;
			}
			else
			{
				entries.RemoveAt(index);
			}

			SchedulerStorage.PersistUnsafe(context, entries);
		}

		if (entry is null)
		{
			return;
		}

		EnqueueWallpaperWork(context, entry.EndpointUrl);

		if (nextDailyEntry is not null)
		{
			ScheduleAlarm(context, nextDailyEntry);
		}
	}

	internal static bool CanScheduleExactAlarms(Context context)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(31))
		{
			return true;
		}

		var alarmManager = context.GetSystemService(Context.AlarmService) as AlarmManager;
		return alarmManager?.CanScheduleExactAlarms() ?? false;
	}

	internal static void OpenExactAlarmSettings(Context context)
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(31))
		{
			return;
		}

		try
		{
			var intent = new Intent(Settings.ActionRequestScheduleExactAlarm);
			intent.SetData(global::Android.Net.Uri.Parse($"package:{context.PackageName}"));
			intent.AddFlags(ActivityFlags.NewTask);
			context.StartActivity(intent);
		}
		catch
		{
			// This settings page is optional and unavailable on some devices.
		}
	}

	private static void EnqueueWallpaperWork(Context context, string endpointUrl)
	{
		var inputData = new Data.Builder()
			.PutString(WorkInputEndpointUrl, endpointUrl)
			.Build();

		var request = OneTimeWorkRequest.Builder
			.From<WallpaperWorker>()
			.SetInputData(inputData)
			.Build();

		WorkManager.GetInstance(context).Enqueue(request);
	}

	private static void ScheduleAlarm(Context context, SchedulerEntry entry)
	{
		if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
		{
			return;
		}

		var pendingIntent = BuildAlarmPendingIntent(context, entry.Id);
		alarmManager.Cancel(pendingIntent);

		var safeTriggerAt = Math.Max(entry.TriggerAtMillis, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1_000L);
		var canUseExact = CanScheduleExactAlarms(context);

		try
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(23))
			{
				if (canUseExact)
				{
					alarmManager.SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, safeTriggerAt, pendingIntent);
				}
				else
				{
					alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, safeTriggerAt, pendingIntent);
				}
			}
			else
			{
				alarmManager.SetExact(AlarmType.RtcWakeup, safeTriggerAt, pendingIntent);
			}
		}
		catch (Java.Lang.SecurityException)
		{
			if (OperatingSystem.IsAndroidVersionAtLeast(23))
			{
				alarmManager.SetAndAllowWhileIdle(AlarmType.RtcWakeup, safeTriggerAt, pendingIntent);
			}
			else
			{
				alarmManager.SetExact(AlarmType.RtcWakeup, safeTriggerAt, pendingIntent);
			}
		}
	}

	private static void CancelAlarm(Context context, string schedulerId)
	{
		if (context.GetSystemService(Context.AlarmService) is not AlarmManager alarmManager)
		{
			return;
		}

		var pendingIntent = BuildAlarmPendingIntent(context, schedulerId);
		alarmManager.Cancel(pendingIntent);
	}

	private static PendingIntent BuildAlarmPendingIntent(Context context, string schedulerId)
	{
		var intent = new Intent(context, typeof(WallpaperAlarmReceiver));
		intent.PutExtra(ExtraSchedulerId, schedulerId);
		var flags = PendingIntentFlags.UpdateCurrent;
		if (OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			flags |= PendingIntentFlags.Immutable;
		}

		return PendingIntent.GetBroadcast(
			context,
			RequestCodeForScheduler(schedulerId),
			intent,
			flags)!;
	}

	private static int RequestCodeForScheduler(string schedulerId)
	{
		if (string.IsNullOrEmpty(schedulerId))
		{
			return 0;
		}

		var hash = 0;
		foreach (var character in schedulerId)
		{
			hash = (31 * hash) + character;
		}

		return hash == int.MinValue ? 0 : Math.Abs(hash);
	}
}
