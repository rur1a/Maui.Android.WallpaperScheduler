using Android.App;
using Android.Content;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;
using Microsoft.Maui.Graphics.Platform;
using Microsoft.Maui.Storage;
using MAUI.Domain;
using Shared.Theming;
using Shared.Theming.Maui;

namespace MAUI.Infrastructure.Android.Scheduling;

internal static class YearCalendarWallpaperGenerator
{
	private const int DotColumns = 14;
	private const float BackgroundTopSpaceRatio = 0.18f;
	private const float BottomTextAreaRatio = 0.14f;
	private const float HorizontalPaddingRatio = 0.08f;
	private const float DotFillRatio = 0.62f;

	internal static global::Android.Graphics.Bitmap? CreateBitmap(
		Context context,
		YearCalendarThemeMode themeMode = YearCalendarThemeMode.App,
		DateTimeOffset? nowOverride = null)
	{
		try
		{
			var (width, height) = ResolveWallpaperSize(context);
			var bitmap = global::Android.Graphics.Bitmap.CreateBitmap(
				width,
				height,
				global::Android.Graphics.Bitmap.Config.Argb8888!);

			using var nativeCanvas = new global::Android.Graphics.Canvas(bitmap);
			var canvas = new PlatformCanvas(context)
			{
				Canvas = nativeCanvas
			};
			var now = nowOverride?.LocalDateTime ?? DateTime.Now;
			var palette = ResolvePalette(context, themeMode);
			var drawable = new YearCalendarWallpaperDrawable(now, palette);
			drawable.Draw(canvas, new RectF(0, 0, width, height));
			return bitmap;
		}
		catch
		{
			return null;
		}
	}

	private static (int Width, int Height) ResolveWallpaperSize(Context context)
	{
		const int fallbackWidth = 1440;
		const int fallbackHeight = 3200;

		var wallpaperManager = WallpaperManager.GetInstance(context);
		var width = wallpaperManager?.DesiredMinimumWidth ?? 0;
		var height = wallpaperManager?.DesiredMinimumHeight ?? 0;

		if (width > 0 && height > 0)
		{
			return (Math.Max(width, 720), Math.Max(height, 1280));
		}

		var displayMetrics = context.Resources?.DisplayMetrics;
		if (displayMetrics is not null)
		{
			width = Math.Max(displayMetrics.WidthPixels, displayMetrics.HeightPixels / 2);
			height = Math.Max(displayMetrics.HeightPixels, displayMetrics.WidthPixels);
			return (Math.Max(width, 720), Math.Max(height, 1280));
		}

		return (fallbackWidth, fallbackHeight);
	}

	private static YearCalendarPalette ResolvePalette(Context context, YearCalendarThemeMode themeMode)
	{
		var resources = ResolveThemeResources(context, themeMode);
		var fallbackPalette = ResolveFallbackPalette(themeMode, context);
		var background = TryGetThemeColor(resources, ThemeColorKeys.Background) ?? fallbackPalette.Background;
		var accent = TryGetThemeColor(resources, ThemeColorKeys.Primary) ?? fallbackPalette.Accent;
		var text = TryGetThemeColor(resources, ThemeColorKeys.TextSecondary) ?? fallbackPalette.Text;
		var futureDots = TryGetThemeColor(resources, ThemeColorKeys.Neutral600)
			?? WithAlpha(fallbackPalette.Text, 0.42f);

		return new YearCalendarPalette(
			Background: background,
			PastDot: Colors.White,
			TodayDot: accent,
			FutureDot: WithAlpha(futureDots, 0.9f),
			TextPrimary: accent,
			TextSecondary: WithAlpha(text, 0.95f));
	}

	private static ResourceDictionary? ResolveThemeResources(Context context, YearCalendarThemeMode themeMode)
	{
		if (themeMode == YearCalendarThemeMode.App &&
			Microsoft.Maui.Controls.Application.Current?.Resources is ResourceDictionary appResources)
		{
			return appResources;
		}

		var preference = themeMode switch
		{
			YearCalendarThemeMode.System => AppThemePreference.System,
			YearCalendarThemeMode.Light => AppThemePreference.Light,
			YearCalendarThemeMode.Dark => AppThemePreference.Dark,
			YearCalendarThemeMode.Amoled => AppThemePreference.Amoled,
			_ => ReadStoredThemePreference()
		};

		return BuildThemeResources(context, preference);
	}

	private static ResourceDictionary BuildThemeResources(Context context, AppThemePreference preference)
	{
		var resources = new ResourceDictionary();
		ThemeRuntime.EnsureThemeResources(resources);
		ThemeRuntime.ApplyPlatformThemeColors(
			resources,
			ResolveRequestedTheme(context, preference),
			preference);
		return resources;
	}

	private static AppTheme ResolveRequestedTheme(Context context, AppThemePreference preference)
	{
		return preference switch
		{
			AppThemePreference.Light => AppTheme.Light,
			AppThemePreference.Dark => AppTheme.Dark,
			AppThemePreference.Amoled => AppTheme.Dark,
			_ => ResolveSystemTheme(context)
		};
	}

	private static AppTheme ResolveSystemTheme(Context context)
	{
		var uiMode = context.Resources?.Configuration?.UiMode ?? 0;
		var nightMask = uiMode & global::Android.Content.Res.UiMode.NightMask;
		return nightMask == global::Android.Content.Res.UiMode.NightYes
			? AppTheme.Dark
			: AppTheme.Light;
	}

	private static AppThemePreference ReadStoredThemePreference()
	{
		var value = Preferences.Default.Get(
			SharedThemingOptions.DefaultPreferenceStorageKey,
			(int)AppThemePreference.System);

		return Enum.IsDefined(typeof(AppThemePreference), value)
			? (AppThemePreference)value
			: AppThemePreference.System;
	}

	private static Color? TryGetThemeColor(ResourceDictionary? resources, string key)
	{
		if (resources is null)
		{
			return null;
		}

		if (!resources.TryGetValue(key, out var value))
		{
			return null;
		}

		if (value is Color color)
		{
			return color;
		}

		if (value is SolidColorBrush brush)
		{
			return brush.Color;
		}

		return null;
	}

	private static Color WithAlpha(Color color, float alpha) =>
		new(color.Red, color.Green, color.Blue, Math.Clamp(alpha, 0f, 1f));

	private static FallbackThemePalette ResolveFallbackPalette(
		YearCalendarThemeMode themeMode,
		Context context)
	{
		var isDark = themeMode switch
		{
			YearCalendarThemeMode.Light => false,
			YearCalendarThemeMode.Dark => true,
			YearCalendarThemeMode.Amoled => true,
			YearCalendarThemeMode.System => ResolveSystemTheme(context) == AppTheme.Dark,
			_ => ResolveSystemTheme(context) == AppTheme.Dark
		};

		return isDark
			? new FallbackThemePalette(
				Background: Color.FromArgb("#101114"),
				Accent: Color.FromArgb("#F06A2A"),
				Text: Color.FromArgb("#888888"))
			: new FallbackThemePalette(
				Background: Color.FromArgb("#F7F7F9"),
				Accent: Color.FromArgb("#2B0B98"),
				Text: Color.FromArgb("#3C3F45"));
	}

	private sealed class YearCalendarWallpaperDrawable : IDrawable
	{
		private readonly DateTime _now;
		private readonly YearCalendarPalette _palette;

		public YearCalendarWallpaperDrawable(DateTime now, YearCalendarPalette palette)
		{
			_now = now;
			_palette = palette;
		}

		public void Draw(ICanvas canvas, RectF dirtyRect)
		{
			canvas.SaveState();
			try
			{
				DrawBackground(canvas, dirtyRect);
				DrawDots(canvas, dirtyRect);
				DrawFooter(canvas, dirtyRect);
			}
			finally
			{
				canvas.RestoreState();
			}
		}

		private void DrawBackground(ICanvas canvas, RectF dirtyRect)
		{
			canvas.FillColor = _palette.Background;
			canvas.FillRectangle(dirtyRect);
		}

		private void DrawDots(ICanvas canvas, RectF dirtyRect)
		{
			var year = _now.Year;
			var daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
			var todayIndex = Math.Clamp(_now.DayOfYear - 1, 0, daysInYear - 1);
			var rows = (int)Math.Ceiling(daysInYear / (float)DotColumns);

			var horizontalPadding = dirtyRect.Width * HorizontalPaddingRatio;
			var topSpace = dirtyRect.Height * BackgroundTopSpaceRatio;
			var bottomReserved = dirtyRect.Height * BottomTextAreaRatio;

			var gridWidth = dirtyRect.Width - (horizontalPadding * 2f);
			var gridHeight = dirtyRect.Height - topSpace - bottomReserved;
			if (gridWidth <= 0f || gridHeight <= 0f)
			{
				return;
			}

			var cellSize = Math.Min(gridWidth / DotColumns, gridHeight / rows);
			var actualGridWidth = DotColumns * cellSize;
			var actualGridHeight = rows * cellSize;
			var startX = dirtyRect.X + ((dirtyRect.Width - actualGridWidth) / 2f);
			var startY = dirtyRect.Y + topSpace + ((gridHeight - actualGridHeight) / 2f);
			var dotRadius = (cellSize * DotFillRatio) / 2f;

			for (var index = 0; index < daysInYear; index++)
			{
				var column = index % DotColumns;
				var row = index / DotColumns;
				var centerX = startX + (column * cellSize) + (cellSize / 2f);
				var centerY = startY + (row * cellSize) + (cellSize / 2f);

				canvas.FillColor = ResolveDotColor(index, todayIndex);
				canvas.FillCircle(centerX, centerY, dotRadius);
			}
		}

		private Color ResolveDotColor(int index, int todayIndex)
		{
			if (index < todayIndex)
			{
				return _palette.PastDot;
			}

			if (index == todayIndex)
			{
				return _palette.TodayDot;
			}

			return _palette.FutureDot;
		}

		private void DrawFooter(ICanvas canvas, RectF dirtyRect)
		{
			var year = _now.Year;
			var daysInYear = DateTime.IsLeapYear(year) ? 366 : 365;
			var daysElapsed = Math.Clamp(_now.DayOfYear, 1, daysInYear);
			var daysLeft = Math.Max(daysInYear - daysElapsed, 0);
			var percent = (int)Math.Round((daysElapsed / (double)daysInYear) * 100d);

			var footerY = dirtyRect.Bottom - (dirtyRect.Height * 0.11f);
			var footerHeight = Math.Max(48f, dirtyRect.Height * 0.05f);
			var footerRect = new RectF(dirtyRect.X, footerY, dirtyRect.Width, footerHeight);
			var centerX = dirtyRect.X + (dirtyRect.Width / 2f);
			var gap = Math.Max(6f, dirtyRect.Width * 0.01f);
			var leftText = $"{daysLeft}d left";
			var rightText = $"{percent}%";

			var footerFontSize = Math.Max(16f, dirtyRect.Width * 0.019f);
			canvas.FontSize = footerFontSize;
			var leftSize = canvas.GetStringSize(leftText, null!, footerFontSize);
			var rightSize = canvas.GetStringSize(rightText, null!, footerFontSize);
			var groupWidth = leftSize.Width + gap + rightSize.Width;
			var groupStartX = centerX - (groupWidth / 2f);

			canvas.FontColor = _palette.TextPrimary;
			canvas.DrawString(
				leftText,
				new RectF(groupStartX, footerRect.Y, Math.Max(leftSize.Width, 1f), footerRect.Height),
				HorizontalAlignment.Left,
				VerticalAlignment.Center);

			canvas.FontColor = _palette.TextSecondary;
			canvas.DrawString(
				rightText,
				new RectF(groupStartX + leftSize.Width + gap, footerRect.Y, Math.Max(rightSize.Width, 1f), footerRect.Height),
				HorizontalAlignment.Left,
				VerticalAlignment.Center);
		}
	}

	private readonly record struct YearCalendarPalette(
		Color Background,
		Color PastDot,
		Color TodayDot,
		Color FutureDot,
		Color TextPrimary,
		Color TextSecondary);

	private readonly record struct FallbackThemePalette(
		Color Background,
		Color Accent,
		Color Text);
}
