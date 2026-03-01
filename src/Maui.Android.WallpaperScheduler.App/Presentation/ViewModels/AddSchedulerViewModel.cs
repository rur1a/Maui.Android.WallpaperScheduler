using MAUI.Application.Abstractions;
using MAUI.Application.Errors;
using MAUI.Domain;
using MAUI.Domain.Enums;
using MAUI.Presentation.Common;
using Microsoft.Maui.Graphics;

namespace MAUI.Presentation.ViewModels;

public sealed class AddSchedulerViewModel : ObservableObject
{
	private enum SchedulerType
	{
		Custom = 0,
		YearCalendar = 1,
	}

	private readonly IWallpaperSchedulerService _schedulerService;
	private int _schedulerTypeIndex;
	private int _yearCalendarThemeIndex;
	private string _endpointUrl = string.Empty;
	private int _modeIndex;
	private DateTime _selectedDate = DateTime.Today;
	private TimeSpan _selectedTime = new(DateTime.Now.Hour, DateTime.Now.Minute, 0);
	private bool _isSubmitting;
	private bool? _canScheduleExactAlarms;
	private string _statusMessage = string.Empty;
	private bool _statusIsError;
	private bool _hasLoadedExactAlarmCapability;

	public AddSchedulerViewModel(IWallpaperSchedulerService schedulerService)
	{
		_schedulerService = schedulerService;
	}

	public string EndpointUrl
	{
		get => _endpointUrl;
		set => SetProperty(ref _endpointUrl, value);
	}

	public int SchedulerTypeIndex
	{
		get => _schedulerTypeIndex;
		set
		{
			if (!SetProperty(ref _schedulerTypeIndex, value))
			{
				return;
			}

			OnPropertyChanged(nameof(IsCustomSchedulerType));
			OnPropertyChanged(nameof(IsYearCalendarSchedulerType));
		}
	}

	public bool IsCustomSchedulerType => ResolveSelectedSchedulerType() == SchedulerType.Custom;
	public bool IsYearCalendarSchedulerType => ResolveSelectedSchedulerType() == SchedulerType.YearCalendar;

	public int YearCalendarThemeIndex
	{
		get => _yearCalendarThemeIndex;
		set => SetProperty(ref _yearCalendarThemeIndex, value);
	}

	public YearCalendarThemeMode SelectedYearCalendarThemeMode => ResolveSelectedYearCalendarThemeMode();

	public int ModeIndex
	{
		get => _modeIndex;
		set
		{
			if (!SetProperty(ref _modeIndex, value))
			{
				return;
			}

			OnPropertyChanged(nameof(IsOneTimeMode));
			OnPropertyChanged(nameof(TimeLabel));
		}
	}

	public bool IsOneTimeMode => Mode == ScheduleMode.OneTime;
	public string TimeLabel => IsOneTimeMode ? "Time" : "Daily time";
	public ScheduleMode Mode => ModeIndex == 1 ? ScheduleMode.Daily : ScheduleMode.OneTime;

	public DateTime SelectedDate
	{
		get => _selectedDate;
		set => SetProperty(ref _selectedDate, value.Date);
	}

	public TimeSpan SelectedTime
	{
		get => _selectedTime;
		set => SetProperty(ref _selectedTime, new TimeSpan(value.Hours, value.Minutes, 0));
	}

	public bool IsSubmitting
	{
		get => _isSubmitting;
		private set
		{
			if (!SetProperty(ref _isSubmitting, value))
			{
				return;
			}

			OnPropertyChanged(nameof(CanSubmit));
		}
	}

	public bool CanSubmit => !IsSubmitting;

	public bool? CanScheduleExactAlarms
	{
		get => _canScheduleExactAlarms;
		private set
		{
			if (!SetProperty(ref _canScheduleExactAlarms, value))
			{
				return;
			}

			OnPropertyChanged(nameof(ShowExactAlarmWarning));
		}
	}

	public bool ShowExactAlarmWarning => CanScheduleExactAlarms == false;

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

	public async Task InitializeAsync(CancellationToken cancellationToken = default)
	{
		if (_hasLoadedExactAlarmCapability)
		{
			return;
		}

		try
		{
			CanScheduleExactAlarms = await _schedulerService
				.CanScheduleExactAlarmsAsync(cancellationToken)
				.ConfigureAwait(true);
			_hasLoadedExactAlarmCapability = true;
		}
		catch
		{
			CanScheduleExactAlarms = null;
		}
	}

	public async Task<bool> AddSchedulerAsync(CancellationToken cancellationToken = default)
	{
		if (IsSubmitting)
		{
			return false;
		}

		return ResolveSelectedSchedulerType() switch
		{
			SchedulerType.Custom => await AddCustomSchedulerAsync(cancellationToken).ConfigureAwait(true),
			SchedulerType.YearCalendar => await AddYearCalendarSchedulerAsync(cancellationToken).ConfigureAwait(true),
			_ => SetUnsupportedSchedulerType()
		};
	}

	public async Task OpenExactAlarmSettingsAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			await _schedulerService.OpenExactAlarmSettingsAsync(cancellationToken).ConfigureAwait(true);
		}
		catch
		{
			// Optional action. Ignore failures because some devices do not expose this page.
		}
	}

	private async Task<bool> AddCustomSchedulerAsync(CancellationToken cancellationToken)
	{
		if (!IsValidHttpEndpoint(EndpointUrl))
		{
			SetStatus("Enter a valid image endpoint URL using http or https.", isError: true);
			return false;
		}

		return await AddSchedulerCoreAsync(EndpointUrl, cancellationToken).ConfigureAwait(true);
	}

	private async Task<bool> AddYearCalendarSchedulerAsync(CancellationToken cancellationToken)
	{
		return await AddSchedulerCoreAsync(BuildYearCalendarEndpoint(), cancellationToken)
			.ConfigureAwait(true);
	}

	public string BuildYearCalendarEndpoint() =>
		WallpaperSourceEndpoints.CreateYearCalendarEndpoint(SelectedYearCalendarThemeMode);

	private async Task<bool> AddSchedulerCoreAsync(string endpointUrl, CancellationToken cancellationToken)
	{
		var triggerTime = BuildTriggerTime();
		if (Mode == ScheduleMode.OneTime && triggerTime <= DateTime.Now)
		{
			SetStatus("Pick a future date and time for one-time mode.", isError: true);
			return false;
		}

		IsSubmitting = true;
		try
		{
			await _schedulerService.AddSchedulerAsync(
				endpointUrl,
				new DateTimeOffset(triggerTime).ToUnixTimeMilliseconds(),
				Mode == ScheduleMode.Daily,
				cancellationToken).ConfigureAwait(true);

			SetStatus(string.Empty);
			return true;
		}
		catch (Exception exception)
		{
			SetStatus($"Add scheduler failed: {ResolveErrorMessage(exception)}", isError: true);
			return false;
		}
		finally
		{
			IsSubmitting = false;
		}
	}

	private bool SetUnsupportedSchedulerType()
	{
		SetStatus("Selected scheduler type is not supported yet.", isError: true);
		return false;
	}

	private DateTime BuildTriggerTime()
	{
		if (Mode == ScheduleMode.OneTime)
		{
			return SelectedDate.Date + SelectedTime;
		}

		var now = DateTime.Now;
		var candidate = new DateTime(
			now.Year,
			now.Month,
			now.Day,
			SelectedTime.Hours,
			SelectedTime.Minutes,
			0,
			DateTimeKind.Local);

		if (candidate <= now)
		{
			candidate = candidate.AddDays(1);
		}

		return candidate;
	}

	private void SetStatus(string message, bool isError = false)
	{
		StatusMessage = message;
		StatusIsError = isError;
	}

	private static string ResolveErrorMessage(Exception exception)
	{
		if (exception is SchedulerValidationException validationException)
		{
			return validationException.Message;
		}

		return exception.Message;
	}

	private static bool IsValidHttpEndpoint(string endpointUrl)
	{
		if (!Uri.TryCreate(endpointUrl?.Trim(), UriKind.Absolute, out var uri) || uri is null)
		{
			return false;
		}

		return uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
			uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase);
	}

	private SchedulerType? ResolveSelectedSchedulerType() => SchedulerTypeIndex switch
	{
		0 => SchedulerType.Custom,
		1 => SchedulerType.YearCalendar,
		_ => null
	};

	private YearCalendarThemeMode ResolveSelectedYearCalendarThemeMode() => YearCalendarThemeIndex switch
	{
		1 => YearCalendarThemeMode.System,
		2 => YearCalendarThemeMode.Light,
		3 => YearCalendarThemeMode.Dark,
		4 => YearCalendarThemeMode.Amoled,
		_ => YearCalendarThemeMode.App
	};
}
