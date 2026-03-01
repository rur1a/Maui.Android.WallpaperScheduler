using MAUI.Domain;
using MAUI.Domain.Models;
using MAUI.Presentation.Common;

namespace MAUI.Presentation.ViewModels;

public sealed class SchedulerItemViewModel : ObservableObject
{
	private bool _isBusy;
	private bool _isRunNowInProgress;

	public SchedulerItemViewModel(SchedulerEntry entry)
	{
		Id = entry.Id;
		EndpointUrl = entry.EndpointUrl;
		TriggerAtMillis = entry.TriggerAtMillis;
		RepeatDaily = entry.RepeatDaily;
	}

	public string Id { get; }
	public string EndpointUrl { get; }
	public long TriggerAtMillis { get; }
	public bool RepeatDaily { get; }
	public bool IsYearCalendarScheduler => WallpaperSourceEndpoints.IsYearCalendar(EndpointUrl);

	public string ShortId => Id.Length > 8 ? Id[..8] : Id;
	public string ScheduleTypeLabel => $"{(RepeatDaily ? "Daily" : "One-time")} - {(IsYearCalendarScheduler ? "Year Calendar" : "Custom")}";
	public string TriggerAtLabel => $"Next run: {DateTimeOffset.FromUnixTimeMilliseconds(TriggerAtMillis).LocalDateTime:yyyy-MM-dd HH:mm}";
	public bool ShowEndpointUrl => !IsYearCalendarScheduler;
	public string SourceDescription => IsYearCalendarScheduler
		? $"Generated on-device ({ResolveYearCalendarThemeLabel()})"
		: EndpointUrl;

	public bool IsBusy
	{
		get => _isBusy;
		set
		{
			if (!SetProperty(ref _isBusy, value))
			{
				return;
			}

			OnPropertyChanged(nameof(IsNotBusy));
		}
	}

	public bool IsNotBusy => !IsBusy;

	public bool IsRunNowInProgress
	{
		get => _isRunNowInProgress;
		set => SetProperty(ref _isRunNowInProgress, value);
	}

	private string ResolveYearCalendarThemeLabel()
	{
		if (!WallpaperSourceEndpoints.TryParseYearCalendar(EndpointUrl, out var request))
		{
			return "App theme";
		}

		return request.ThemeMode switch
		{
			YearCalendarThemeMode.System => "System theme",
			YearCalendarThemeMode.Light => "Light theme",
			YearCalendarThemeMode.Dark => "Dark theme",
			YearCalendarThemeMode.Amoled => "AMOLED theme",
			_ => "App theme"
		};
	}
}
