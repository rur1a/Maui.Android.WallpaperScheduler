using MAUI.Application.Abstractions;
using MAUI.Application.Errors;
using MAUI.Domain;
using MAUI.Domain.Models;

namespace MAUI.Application.Services;

public sealed class WallpaperSchedulerService : IWallpaperSchedulerService
{
	private readonly IWallpaperSchedulerGateway _gateway;

	public WallpaperSchedulerService(IWallpaperSchedulerGateway gateway)
	{
		_gateway = gateway;
	}

	public Task<IReadOnlyList<SchedulerEntry>> ListSchedulersAsync(CancellationToken cancellationToken = default) =>
		_gateway.ListSchedulersAsync(cancellationToken);

	public Task<string> AddSchedulerAsync(
		string endpointUrl,
		long triggerAtMillis,
		bool repeatDaily,
		CancellationToken cancellationToken = default)
	{
		var normalizedUrl = ValidateEndpointUrl(endpointUrl);
		var safeTriggerAtMillis = Math.Max(triggerAtMillis, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 1_000L);

		return _gateway.AddSchedulerAsync(
			normalizedUrl,
			safeTriggerAtMillis,
			repeatDaily,
			cancellationToken);
	}

	public Task<bool> DeleteSchedulerAsync(string schedulerId, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(schedulerId))
		{
			throw new SchedulerValidationException("Missing scheduler id.");
		}

		return _gateway.DeleteSchedulerAsync(schedulerId, cancellationToken);
	}

	public Task<bool> RunNowAsync(string endpointUrl, CancellationToken cancellationToken = default)
	{
		var normalizedUrl = ValidateEndpointUrl(endpointUrl);
		return _gateway.RunNowAsync(normalizedUrl, cancellationToken);
	}

	public Task<bool?> CanScheduleExactAlarmsAsync(CancellationToken cancellationToken = default) =>
		_gateway.CanScheduleExactAlarmsAsync(cancellationToken);

	public Task OpenExactAlarmSettingsAsync(CancellationToken cancellationToken = default) =>
		_gateway.OpenExactAlarmSettingsAsync(cancellationToken);

	private static string ValidateEndpointUrl(string endpointUrl)
	{
		if (WallpaperSourceEndpoints.IsInternal(endpointUrl))
		{
			return WallpaperSourceEndpoints.Normalize(endpointUrl);
		}

		if (!Uri.TryCreate(endpointUrl?.Trim(), UriKind.Absolute, out var uri) || uri is null)
		{
			throw new SchedulerValidationException("Enter a valid image endpoint URL using http or https.");
		}

		var isHttp = uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase);
		var isHttps = uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);

		if (!isHttp && !isHttps)
		{
			throw new SchedulerValidationException("Enter a valid image endpoint URL using http or https.");
		}

		return uri.ToString();
	}
}
