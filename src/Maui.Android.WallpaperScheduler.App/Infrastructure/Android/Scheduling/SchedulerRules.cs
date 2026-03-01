using MAUI.Domain.Models;

namespace MAUI.Infrastructure.Android.Scheduling;

internal static class SchedulerRules
{
	private const long DuplicateTriggerToleranceMillis = 5_000L;

	internal static long ComputeNextDailyTrigger(long referenceMillis, long nowMillis)
	{
		var reference = DateTimeOffset.FromUnixTimeMilliseconds(referenceMillis).LocalDateTime;
		var now = DateTimeOffset.FromUnixTimeMilliseconds(nowMillis).LocalDateTime;

		var next = new DateTime(
			now.Year,
			now.Month,
			now.Day,
			reference.Hour,
			reference.Minute,
			0,
			DateTimeKind.Local);

		if (next <= now)
		{
			next = next.AddDays(1);
		}

		return new DateTimeOffset(next).ToUnixTimeMilliseconds();
	}

	internal static List<SchedulerEntry> DeduplicateSchedules(IReadOnlyList<SchedulerEntry> entries)
	{
		var uniqueEntries = new List<SchedulerEntry>(entries.Count);

		foreach (var entry in entries)
		{
			var exists = uniqueEntries.Any(existing =>
				IsSameSchedule(existing, entry.EndpointUrl, entry.TriggerAtMillis, entry.RepeatDaily));
			if (!exists)
			{
				uniqueEntries.Add(entry);
			}
		}

		return uniqueEntries;
	}

	internal static bool IsSameSchedule(
		SchedulerEntry entry,
		string endpointUrl,
		long triggerAtMillis,
		bool repeatDaily)
	{
		if (entry.RepeatDaily != repeatDaily)
		{
			return false;
		}

		if (!string.Equals(
			NormalizeEndpointForComparison(entry.EndpointUrl),
			NormalizeEndpointForComparison(endpointUrl),
			StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		return Math.Abs(entry.TriggerAtMillis - triggerAtMillis) <= DuplicateTriggerToleranceMillis;
	}

	private static string NormalizeEndpointForComparison(string endpointUrl) =>
		endpointUrl.Trim().TrimEnd('/');
}
