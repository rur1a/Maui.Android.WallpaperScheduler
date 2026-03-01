using MAUI.Application.Abstractions;
using MAUI.Application.Errors;
using MAUI.Application.Services;
using MAUI.Domain.Models;

namespace Maui.Android.WallpaperScheduler.App.Tests;

public sealed class WallpaperSchedulerServiceTests
{
	[Fact]
	public async Task AddSchedulerAsync_WithInvalidEndpoint_ThrowsValidationException()
	{
		var gateway = new FakeGateway();
		var service = new WallpaperSchedulerService(gateway);

		await Assert.ThrowsAsync<SchedulerValidationException>(() =>
			service.AddSchedulerAsync("ftp://example.com/wallpaper.jpg", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(), repeatDaily: false));
	}

	[Fact]
	public async Task AddSchedulerAsync_WithPastTrigger_ClampsToFuture()
	{
		var gateway = new FakeGateway();
		var service = new WallpaperSchedulerService(gateway);
		var before = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

		await service.AddSchedulerAsync("https://example.com/wallpaper.jpg", 0, repeatDaily: false);

		Assert.True(gateway.LastTriggerAtMillis > before);
	}

	[Fact]
	public async Task AddSchedulerAsync_WithInternalEndpoint_NormalizesTrailingSlash()
	{
		var gateway = new FakeGateway();
		var service = new WallpaperSchedulerService(gateway);

		await service.AddSchedulerAsync("app://wallpaper/year-calendar/", DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + 5_000L, repeatDaily: true);

		Assert.Equal("app://wallpaper/year-calendar", gateway.LastEndpointUrl);
	}

	[Fact]
	public async Task DeleteSchedulerAsync_WithMissingId_ThrowsValidationException()
	{
		var gateway = new FakeGateway();
		var service = new WallpaperSchedulerService(gateway);

		await Assert.ThrowsAsync<SchedulerValidationException>(() =>
			service.DeleteSchedulerAsync(" "));
	}

	private sealed class FakeGateway : IWallpaperSchedulerGateway
	{
		public string LastEndpointUrl { get; private set; } = string.Empty;
		public long LastTriggerAtMillis { get; private set; }

		public Task<IReadOnlyList<SchedulerEntry>> ListSchedulersAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<IReadOnlyList<SchedulerEntry>>(Array.Empty<SchedulerEntry>());

		public Task<string> AddSchedulerAsync(
			string endpointUrl,
			long triggerAtMillis,
			bool repeatDaily,
			CancellationToken cancellationToken = default)
		{
			LastEndpointUrl = endpointUrl;
			LastTriggerAtMillis = triggerAtMillis;
			return Task.FromResult("scheduler-id");
		}

		public Task<bool> DeleteSchedulerAsync(string schedulerId, CancellationToken cancellationToken = default) =>
			Task.FromResult(true);

		public Task<bool> RunNowAsync(string endpointUrl, CancellationToken cancellationToken = default) =>
			Task.FromResult(true);

		public Task<bool?> CanScheduleExactAlarmsAsync(CancellationToken cancellationToken = default) =>
			Task.FromResult<bool?>(true);

		public Task OpenExactAlarmSettingsAsync(CancellationToken cancellationToken = default) =>
			Task.CompletedTask;
	}
}
