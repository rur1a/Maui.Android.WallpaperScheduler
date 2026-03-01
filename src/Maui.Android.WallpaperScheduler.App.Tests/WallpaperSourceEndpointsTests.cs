using MAUI.Domain;

namespace Maui.Android.WallpaperScheduler.App.Tests;

public sealed class WallpaperSourceEndpointsTests
{
	[Fact]
	public void CreateYearCalendarEndpoint_AppTheme_ReturnsBaseEndpoint()
	{
		var endpoint = WallpaperSourceEndpoints.CreateYearCalendarEndpoint(YearCalendarThemeMode.App);

		Assert.Equal(WallpaperSourceEndpoints.YearCalendar, endpoint);
	}

	[Fact]
	public void TryParseYearCalendar_WithSupportedTheme_ParsesTheme()
	{
		var parsed = WallpaperSourceEndpoints.TryParseYearCalendar(
			"app://wallpaper/year-calendar?theme=amoled",
			out var request);

		Assert.True(parsed);
		Assert.Equal(YearCalendarThemeMode.Amoled, request.ThemeMode);
	}

	[Fact]
	public void TryParseYearCalendar_WithInvalidTheme_ReturnsFalse()
	{
		var parsed = WallpaperSourceEndpoints.TryParseYearCalendar(
			"app://wallpaper/year-calendar?theme=sepia",
			out _);

		Assert.False(parsed);
	}

	[Fact]
	public void IsInternal_ForHttpEndpoint_ReturnsFalse()
	{
		var isInternal = WallpaperSourceEndpoints.IsInternal("https://example.com/wallpaper.jpg");

		Assert.False(isInternal);
	}
}
