namespace MAUI.Domain;

public enum YearCalendarThemeMode
{
	App = 0,
	System = 1,
	Light = 2,
	Dark = 3,
	Amoled = 4,
}

public readonly record struct YearCalendarWallpaperRequest(YearCalendarThemeMode ThemeMode);

public static class WallpaperSourceEndpoints
{
	public const string YearCalendar = "app://wallpaper/year-calendar";
	private const string YearCalendarThemeQueryKey = "theme";

	public static bool IsYearCalendar(string? endpointUrl) =>
		TryParseYearCalendar(endpointUrl, out _);

	public static bool IsInternal(string? endpointUrl) => IsYearCalendar(endpointUrl);

	public static string CreateYearCalendarEndpoint(YearCalendarThemeMode themeMode)
	{
		if (themeMode == YearCalendarThemeMode.App)
		{
			return YearCalendar;
		}

		return $"{YearCalendar}?{YearCalendarThemeQueryKey}={ToQueryValue(themeMode)}";
	}

	public static bool TryParseYearCalendar(string? endpointUrl, out YearCalendarWallpaperRequest request)
	{
		request = new YearCalendarWallpaperRequest(YearCalendarThemeMode.App);

		if (!Uri.TryCreate(endpointUrl?.Trim(), UriKind.Absolute, out var uri) || uri is null)
		{
			return false;
		}

		if (!uri.Scheme.Equals("app", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		if (!uri.Host.Equals("wallpaper", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var path = uri.AbsolutePath.TrimEnd('/');
		if (!path.Equals("/year-calendar", StringComparison.OrdinalIgnoreCase))
		{
			return false;
		}

		var themeValue = TryGetQueryValue(uri.Query, YearCalendarThemeQueryKey);
		if (string.IsNullOrWhiteSpace(themeValue))
		{
			return true;
		}

		if (!TryParseThemeMode(themeValue, out var themeMode))
		{
			return false;
		}

		request = new YearCalendarWallpaperRequest(themeMode);
		return true;
	}

	public static string Normalize(string endpointUrl) => endpointUrl.Trim().TrimEnd('/');

	private static string? TryGetQueryValue(string query, string key)
	{
		if (string.IsNullOrWhiteSpace(query))
		{
			return null;
		}

		var trimmedQuery = query[0] == '?' ? query[1..] : query;
		foreach (var segment in trimmedQuery.Split('&', StringSplitOptions.RemoveEmptyEntries))
		{
			var parts = segment.Split('=', 2);
			if (parts.Length == 0)
			{
				continue;
			}

			var segmentKey = Uri.UnescapeDataString(parts[0]);
			if (!segmentKey.Equals(key, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			if (parts.Length < 2)
			{
				return string.Empty;
			}

			return Uri.UnescapeDataString(parts[1]);
		}

		return null;
	}

	private static bool TryParseThemeMode(string value, out YearCalendarThemeMode themeMode)
	{
		switch (value.Trim().ToLowerInvariant())
		{
			case "app":
				themeMode = YearCalendarThemeMode.App;
				return true;
			case "system":
				themeMode = YearCalendarThemeMode.System;
				return true;
			case "light":
				themeMode = YearCalendarThemeMode.Light;
				return true;
			case "dark":
				themeMode = YearCalendarThemeMode.Dark;
				return true;
			case "amoled":
				themeMode = YearCalendarThemeMode.Amoled;
				return true;
			default:
				themeMode = YearCalendarThemeMode.App;
				return false;
		}
	}

	private static string ToQueryValue(YearCalendarThemeMode themeMode) => themeMode switch
	{
		YearCalendarThemeMode.System => "system",
		YearCalendarThemeMode.Light => "light",
		YearCalendarThemeMode.Dark => "dark",
		YearCalendarThemeMode.Amoled => "amoled",
		_ => "app",
	};
}
