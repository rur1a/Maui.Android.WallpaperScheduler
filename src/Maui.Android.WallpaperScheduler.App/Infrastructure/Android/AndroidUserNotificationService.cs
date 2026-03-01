using Android.Widget;
using MAUI.Application.Abstractions;
using Microsoft.Maui.ApplicationModel;

namespace MAUI.Infrastructure.Android;

public sealed class AndroidUserNotificationService : IUserNotificationService
{
	public async Task ShowShortMessageAsync(string message, CancellationToken cancellationToken = default)
	{
		if (string.IsNullOrWhiteSpace(message))
		{
			return;
		}

		await MainThread.InvokeOnMainThreadAsync(() =>
		{
			var context = global::Android.App.Application.Context;
			Toast.MakeText(context, message, ToastLength.Short)?.Show();
		}).ConfigureAwait(false);
	}
}
