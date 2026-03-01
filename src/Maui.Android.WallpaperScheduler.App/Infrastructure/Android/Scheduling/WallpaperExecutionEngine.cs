using Android.App;
using Android.Content;
using Android.Graphics;
using MAUI.Domain;

namespace MAUI.Infrastructure.Android.Scheduling;

internal enum WallpaperExecutionResult
{
	Success,
	Retry,
	Failure,
}

internal static class WallpaperExecutionEngine
{
	internal static async Task<WallpaperExecutionResult> ExecuteAsync(
		Context context,
		string endpointUrl,
		CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(endpointUrl))
		{
			return WallpaperExecutionResult.Failure;
		}

		try
		{
			if (WallpaperSourceEndpoints.TryParseYearCalendar(endpointUrl, out var yearCalendarRequest))
			{
				using var generatedBitmap = YearCalendarWallpaperGenerator.CreateBitmap(
					context,
					yearCalendarRequest.ThemeMode);
				if (generatedBitmap is null)
				{
					return WallpaperExecutionResult.Failure;
				}

				SetLockScreenWallpaper(context, generatedBitmap);
				return WallpaperExecutionResult.Success;
			}

			using var bitmap = await DownloadBitmapAsync(endpointUrl, cancellationToken).ConfigureAwait(false);
			if (bitmap is null)
			{
				return WallpaperExecutionResult.Failure;
			}

			SetLockScreenWallpaper(context, bitmap);
			return WallpaperExecutionResult.Success;
		}
		catch (IOException)
		{
			return WallpaperExecutionResult.Retry;
		}
		catch (HttpRequestException)
		{
			return WallpaperExecutionResult.Retry;
		}
		catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
		{
			return WallpaperExecutionResult.Retry;
		}
		catch (TaskCanceledException)
		{
			return WallpaperExecutionResult.Failure;
		}
		catch
		{
			return WallpaperExecutionResult.Failure;
		}
	}

	private static async Task<Bitmap?> DownloadBitmapAsync(string endpointUrl, CancellationToken cancellationToken)
	{
		using var httpClient = new HttpClient
		{
			Timeout = TimeSpan.FromSeconds(15),
		};

		using var response = await httpClient
			.GetAsync(endpointUrl, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
			.ConfigureAwait(false);

		var statusCode = (int)response.StatusCode;

		if (statusCode is >= 400 and <= 499)
		{
			return null;
		}

		if (!response.IsSuccessStatusCode)
		{
			throw new IOException($"Server returned HTTP {statusCode}");
		}

		using var stream = await response.Content
			.ReadAsStreamAsync(cancellationToken)
			.ConfigureAwait(false);

		return BitmapFactory.DecodeStream(stream);
	}

	private static void SetLockScreenWallpaper(Context context, Bitmap bitmap)
	{
		var wallpaperManager = WallpaperManager.GetInstance(context);
		if (wallpaperManager is null)
		{
			throw new InvalidOperationException("Wallpaper manager is not available.");
		}

		if (OperatingSystem.IsAndroidVersionAtLeast(24))
		{
			wallpaperManager.SetBitmap(bitmap, null, true, WallpaperManagerFlags.Lock);
			return;
		}

		wallpaperManager.SetBitmap(bitmap);
	}
}
