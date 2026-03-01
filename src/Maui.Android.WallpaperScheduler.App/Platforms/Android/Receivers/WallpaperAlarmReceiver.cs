using Android.Content;
using MAUI.Infrastructure.Android.Scheduling;

namespace MAUI.Platforms.Android.Receivers;

[BroadcastReceiver(Enabled = true, Exported = false)]
public sealed class WallpaperAlarmReceiver : BroadcastReceiver
{
	public override void OnReceive(Context? context, Intent? intent)
	{
		if (context is null)
		{
			return;
		}

		var schedulerId = intent?.GetStringExtra(WallpaperSchedulerEngine.ExtraSchedulerId);
		if (string.IsNullOrWhiteSpace(schedulerId))
		{
			return;
		}

		WallpaperSchedulerEngine.OnAlarmTriggered(context, schedulerId);
	}
}
