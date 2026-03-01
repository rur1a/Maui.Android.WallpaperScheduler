using MAUI.Application.Abstractions;
using MAUI.Application.Errors;
using MAUI.Presentation.Common;
using Microsoft.Maui.Graphics;
using System.Collections.ObjectModel;

namespace MAUI.Presentation.ViewModels;

public sealed class SchedulerManagerViewModel : ObservableObject
{
	private readonly IWallpaperSchedulerService _schedulerService;
	private bool _isLoading = true;
	private string _statusMessage = string.Empty;
	private bool _statusIsError;

	public SchedulerManagerViewModel(IWallpaperSchedulerService schedulerService)
	{
		_schedulerService = schedulerService;
		Schedulers = new ObservableCollection<SchedulerItemViewModel>();
	}

	public ObservableCollection<SchedulerItemViewModel> Schedulers { get; }

	public bool IsLoading
	{
		get => _isLoading;
		private set
		{
			if (!SetProperty(ref _isLoading, value))
			{
				return;
			}

			OnPropertyChanged(nameof(IsReady));
		}
	}

	public bool IsReady => !IsLoading;

	public string StatusMessage
	{
		get => _statusMessage;
		private set
		{
			if (!SetProperty(ref _statusMessage, value))
			{
				return;
			}

			OnPropertyChanged(nameof(HasStatus));
		}
	}

	public bool StatusIsError
	{
		get => _statusIsError;
		private set
		{
			if (!SetProperty(ref _statusIsError, value))
			{
				return;
			}

			OnPropertyChanged(nameof(StatusColor));
		}
	}

	public bool HasStatus => !string.IsNullOrWhiteSpace(StatusMessage);
	public Color StatusColor => StatusIsError ? Color.FromArgb("#B00020") : Color.FromArgb("#00695C");

	public void SetStatus(string message, bool isError = false)
	{
		StatusMessage = message;
		StatusIsError = isError;
	}

	public async Task LoadSchedulersAsync(bool showLoading = true, CancellationToken cancellationToken = default)
	{
		await RefreshSchedulersAsync(showLoading, cancellationToken).ConfigureAwait(true);
	}

	public async Task<bool> RefreshSchedulersAsync(bool showLoading = true, CancellationToken cancellationToken = default)
	{
		if (showLoading)
		{
			IsLoading = true;
		}

		try
		{
			var entries = await _schedulerService.ListSchedulersAsync(cancellationToken).ConfigureAwait(true);

			Schedulers.Clear();
			foreach (var entry in entries)
			{
				Schedulers.Add(new SchedulerItemViewModel(entry));
			}

			return true;
		}
		catch (Exception exception)
		{
			SetStatus($"Failed to load schedulers: {ResolveErrorMessage(exception)}", isError: true);
			return false;
		}
		finally
		{
			IsLoading = false;
		}
	}

	public async Task<bool> RunNowAsync(SchedulerItemViewModel scheduler, CancellationToken cancellationToken = default)
	{
		if (scheduler.IsBusy)
		{
			return false;
		}

		scheduler.IsBusy = true;
		scheduler.IsRunNowInProgress = true;
		try
		{
			var succeeded = await _schedulerService
				.RunNowAsync(scheduler.EndpointUrl, cancellationToken)
				.ConfigureAwait(true);

			if (succeeded)
			{
				return true;
			}

			SetStatus($"Immediate wallpaper update failed for {scheduler.ShortId}.", isError: true);
			return false;
		}
		catch (Exception exception)
		{
			SetStatus($"Immediate update failed: {ResolveErrorMessage(exception)}", isError: true);
			return false;
		}
		finally
		{
			scheduler.IsRunNowInProgress = false;
			scheduler.IsBusy = false;
		}
	}

	public async Task DeleteSchedulerAsync(SchedulerItemViewModel scheduler, CancellationToken cancellationToken = default)
	{
		scheduler.IsBusy = true;
		try
		{
			var removed = await _schedulerService.DeleteSchedulerAsync(scheduler.Id, cancellationToken).ConfigureAwait(true);
			if (removed)
			{
				Schedulers.Remove(scheduler);
				SetStatus($"Deleted scheduler {scheduler.ShortId}.");
			}
			else
			{
				SetStatus($"Scheduler {scheduler.ShortId} was not found.", isError: true);
			}
		}
		catch (Exception exception)
		{
			SetStatus($"Delete failed: {ResolveErrorMessage(exception)}", isError: true);
		}
		finally
		{
			scheduler.IsBusy = false;
		}
	}

	private static string ResolveErrorMessage(Exception exception)
	{
		if (exception is SchedulerValidationException validationException)
		{
			return validationException.Message;
		}

		return exception.Message;
	}
}
