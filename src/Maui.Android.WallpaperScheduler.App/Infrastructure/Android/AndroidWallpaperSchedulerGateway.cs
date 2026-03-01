using MAUI.Application.Abstractions;
using MAUI.Domain.Models;
using MAUI.Infrastructure.Android.Scheduling;

namespace MAUI.Infrastructure.Android;

public sealed class AndroidWallpaperSchedulerGateway : IWallpaperSchedulerGateway
{
	public Task<IReadOnlyList<SchedulerEntry>> ListSchedulersAsync(CancellationToken cancellationToken = default)
	{
		var schedulers = WallpaperSchedulerEngine.ListSchedulers(global::Android.App.Application.Context);
		return Task.FromResult<IReadOnlyList<SchedulerEntry>>(schedulers);
	}

	public Task<string> AddSchedulerAsync(
		string endpointUrl,
		long triggerAtMillis,
		bool repeatDaily,
		CancellationToken cancellationToken = default)
	{
		var schedulerId = WallpaperSchedulerEngine.AddAndSchedule(
			global::Android.App.Application.Context,
			endpointUrl,
			triggerAtMillis,
			repeatDaily);

		return Task.FromResult(schedulerId);
	}

	public Task<bool> DeleteSchedulerAsync(string schedulerId, CancellationToken cancellationToken = default)
	{
		var removed = WallpaperSchedulerEngine.DeleteScheduler(global::Android.App.Application.Context, schedulerId);
		return Task.FromResult(removed);
	}

	public Task<bool> RunNowAsync(string endpointUrl, CancellationToken cancellationToken = default)
	{
		return WallpaperSchedulerEngine.RunNowAsync(
			global::Android.App.Application.Context,
			endpointUrl,
			cancellationToken);
	}

	public Task<bool?> CanScheduleExactAlarmsAsync(CancellationToken cancellationToken = default)
	{
		var canScheduleExactAlarms = WallpaperSchedulerEngine.CanScheduleExactAlarms(global::Android.App.Application.Context);
		return Task.FromResult<bool?>(canScheduleExactAlarms);
	}

	public Task OpenExactAlarmSettingsAsync(CancellationToken cancellationToken = default)
	{
		WallpaperSchedulerEngine.OpenExactAlarmSettings(global::Android.App.Application.Context);
		return Task.CompletedTask;
	}
}
