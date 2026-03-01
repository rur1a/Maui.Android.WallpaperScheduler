using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Provider;
using MAUI.Infrastructure.Android.Scheduling;

namespace MAUI;

[Activity(Theme = "@style/Maui.SplashTheme", MainLauncher = true, LaunchMode = LaunchMode.SingleTop, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize | ConfigChanges.Density)]
public class MainActivity : MauiAppCompatActivity
{
	private static bool _startupChecksPromptedInProcess;

	protected override void OnPostResume()
	{
		base.OnPostResume();

		if (_startupChecksPromptedInProcess)
		{
			return;
		}

		_startupChecksPromptedInProcess = true;
		_ = PromptStartupBackgroundRequirementsAsync();
	}

	private async Task PromptStartupBackgroundRequirementsAsync()
	{
		try
		{
			await PromptExactAlarmAccessAsync().ConfigureAwait(false);
			await PromptBatteryOptimizationExemptionAsync().ConfigureAwait(false);
		}
		catch
		{
			// Startup prompts are best-effort only.
		}
	}

	private async Task PromptExactAlarmAccessAsync()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(31))
		{
			return;
		}

		if (WallpaperSchedulerEngine.CanScheduleExactAlarms(this))
		{
			return;
		}

		var openSettings = await ShowStartupPromptAsync(
			title: "Allow Exact Alarms",
			message: "Wallpaper schedules may run late unless exact alarms are allowed for this app.",
			confirmText: "Open settings").ConfigureAwait(false);

		if (!openSettings)
		{
			return;
		}

		await RunOnUiThreadAsync(() => WallpaperSchedulerEngine.OpenExactAlarmSettings(this))
			.ConfigureAwait(false);
	}

	private async Task PromptBatteryOptimizationExemptionAsync()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			return;
		}

		if (GetSystemService(PowerService) is not PowerManager powerManager)
		{
			return;
		}

		if (powerManager.IsIgnoringBatteryOptimizations(PackageName))
		{
			return;
		}

		var openSettings = await ShowStartupPromptAsync(
			title: "Allow Background Execution",
			message: "Battery optimization can block scheduled wallpaper updates. Disable optimization for this app.",
			confirmText: "Open settings").ConfigureAwait(false);

		if (!openSettings)
		{
			return;
		}

		await RunOnUiThreadAsync(OpenBatteryOptimizationSettings).ConfigureAwait(false);
	}

	private Task<bool> ShowStartupPromptAsync(string title, string message, string confirmText)
	{
		var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

		RunOnUiThread(() =>
		{
			if (IsFinishing || IsDestroyed)
			{
				completion.TrySetResult(false);
				return;
			}

			var builder = new AlertDialog.Builder(this);
			builder.SetTitle(title);
			builder.SetMessage(message);
			builder.SetCancelable(false);
			builder.SetPositiveButton(confirmText, (_, _) => completion.TrySetResult(true));
			builder.SetNegativeButton("Later", (_, _) => completion.TrySetResult(false));

			var dialog = builder.Create();
			dialog?.Show();
		});

		return completion.Task;
	}

	private Task RunOnUiThreadAsync(Action action)
	{
		var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

		RunOnUiThread(() =>
		{
			try
			{
				if (IsFinishing || IsDestroyed)
				{
					completion.TrySetResult();
					return;
				}

				action();
				completion.TrySetResult();
			}
			catch (Exception exception)
			{
				completion.TrySetException(exception);
			}
		});

		return completion.Task;
	}

	private void OpenBatteryOptimizationSettings()
	{
		if (!OperatingSystem.IsAndroidVersionAtLeast(23))
		{
			return;
		}

		var packageUri = global::Android.Net.Uri.Parse($"package:{PackageName}");

		try
		{
			var requestIntent = new Intent(Settings.ActionRequestIgnoreBatteryOptimizations);
			requestIntent.SetData(packageUri);
			StartActivity(requestIntent);
			return;
		}
		catch
		{
			// Fall back to the general battery optimization settings page.
		}

		try
		{
			var fallbackIntent = new Intent(Settings.ActionIgnoreBatteryOptimizationSettings);
			StartActivity(fallbackIntent);
		}
		catch
		{
			// Some devices hide this page.
		}
	}
}
