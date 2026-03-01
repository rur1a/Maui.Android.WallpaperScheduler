using Android.App;
using Android.Content;
using AndroidX.Work;
using MAUI.Infrastructure.Android.Scheduling;

namespace MAUI.Platforms.Android.Workers;

public sealed class WallpaperWorker : Worker
{
	public WallpaperWorker(Context appContext, WorkerParameters workerParams)
		: base(appContext, workerParams)
	{
	}

	public override Result DoWork()
	{
		var endpointUrl = InputData.GetString(WallpaperSchedulerEngine.WorkInputEndpointUrl);
		var executionResult = WallpaperExecutionEngine
			.ExecuteAsync(ApplicationContext, endpointUrl ?? string.Empty)
			.GetAwaiter()
			.GetResult();

		return executionResult switch
		{
			WallpaperExecutionResult.Success => Result.InvokeSuccess(),
			WallpaperExecutionResult.Retry => Result.InvokeRetry(),
			_ => Result.InvokeFailure(),
		};
	}
}
