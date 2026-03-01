using Android.Content;
using MAUI.Infrastructure.Android.Scheduling;

namespace MAUI.Platforms.Android.Receivers;

[BroadcastReceiver(Enabled = true, Exported = true)]
[global::Android.App.IntentFilter(new[] { Intent.ActionBootCompleted, Intent.ActionMyPackageReplaced })]
public sealed class WallpaperBootReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (context is null)
		{
			return;
		}

		var action = intent?.Action;
		if (action == Intent.ActionBootCompleted || action == Intent.ActionMyPackageReplaced)
		{
			WallpaperSchedulerEngine.RescheduleFromStorage(context);
		}
	}
}
