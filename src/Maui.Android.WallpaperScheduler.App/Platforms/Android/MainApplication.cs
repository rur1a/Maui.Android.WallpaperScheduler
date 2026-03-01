using Android.App;
using Android.Runtime;
using Google.Android.Material.Color;
using MAUI.Infrastructure.Android.Scheduling;

namespace MAUI;

[Application]
public class MainApplication : MauiApplication
{
	public MainApplication(IntPtr handle, JniHandleOwnership ownership)
		: base(handle, ownership)
	{
	}

	public override void OnCreate()
	{
		base.OnCreate();
		DynamicColors.ApplyToActivitiesIfAvailable(this);
		WallpaperSchedulerEngine.RescheduleFromStorage(this);
	}

	protected override MauiApp CreateMauiApp() => MauiProgram.CreateMauiApp();
}
