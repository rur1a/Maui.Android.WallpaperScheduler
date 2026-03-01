using MAUI.Domain.Models;

namespace MAUI.Application.Abstractions;

public interface IWallpaperSchedulerService
{
	Task<IReadOnlyList<SchedulerEntry>> ListSchedulersAsync(CancellationToken cancellationToken = default);
	Task<string> AddSchedulerAsync(string endpointUrl, long triggerAtMillis, bool repeatDaily, CancellationToken cancellationToken = default);
	Task<bool> DeleteSchedulerAsync(string schedulerId, CancellationToken cancellationToken = default);
	Task<bool> RunNowAsync(string endpointUrl, CancellationToken cancellationToken = default);
	Task<bool?> CanScheduleExactAlarmsAsync(CancellationToken cancellationToken = default);
	Task OpenExactAlarmSettingsAsync(CancellationToken cancellationToken = default);
}
