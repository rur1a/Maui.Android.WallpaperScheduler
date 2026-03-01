using MAUI.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using MAUI.Application.Abstractions;
using Shared.Theming;
using Shared.Theming.Abstractions;

namespace MAUI.Presentation.Pages;

public partial class SchedulerManagerPage : ContentPage
{
	private readonly SchedulerManagerViewModel _viewModel;
	private readonly IServiceProvider _serviceProvider;
	private readonly IUserNotificationService _userNotificationService;
	private readonly IThemeService _themeService;
	private bool _isInitialized;
	private bool _isRefreshAnimating;

	public SchedulerManagerPage(
		SchedulerManagerViewModel viewModel,
		IServiceProvider serviceProvider,
		IUserNotificationService userNotificationService,
		IThemeService themeService)
	{
		InitializeComponent();
		_viewModel = viewModel;
		_serviceProvider = serviceProvider;
		_userNotificationService = userNotificationService;
		_themeService = themeService;
		BindingContext = _viewModel;
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();

		if (_isInitialized)
		{
			await _viewModel.LoadSchedulersAsync(showLoading: false);
			return;
		}

		_isInitialized = true;
		await _viewModel.LoadSchedulersAsync(showLoading: true);
	}

	private async void OnRefreshClicked(object sender, EventArgs e)
	{
		if (_isRefreshAnimating)
		{
			return;
		}

		_isRefreshAnimating = true;
		var animationTask = AnimateRefreshAsync();
		try
		{
			var refreshed = await _viewModel.RefreshSchedulersAsync(showLoading: false);
			var message = refreshed ? "Schedulers refreshed." : "Refresh failed.";
			await _userNotificationService.ShowShortMessageAsync(message);
		}
		finally
		{
			_isRefreshAnimating = false;
			await animationTask;
			RefreshIconButton.Rotation = 0;
		}
	}

	private async void OnThemeSettingsClicked(object sender, EventArgs e)
	{
		var selectedOption = await DisplayActionSheet(
			$"Theme ({ResolveThemeLabel(_themeService.CurrentPreference)})",
			"Cancel",
			null,
			"System",
			"Light",
			"Dark",
			"AMOLED");

		if (string.IsNullOrWhiteSpace(selectedOption) || selectedOption.Equals("Cancel", StringComparison.Ordinal))
		{
			return;
		}

		var selectedTheme = selectedOption switch
		{
			"Light" => AppThemePreference.Light,
			"Dark" => AppThemePreference.Dark,
			"AMOLED" => AppThemePreference.Amoled,
			_ => AppThemePreference.System
		};

		_themeService.SetPreference(selectedTheme);
	}

	private async void OnAddSchedulerClicked(object sender, EventArgs e)
	{
		var page = _serviceProvider.GetRequiredService<AddSchedulerPage>();
		await Navigation.PushModalAsync(new NavigationPage(page));
	}

	private async void OnRunNowClicked(object sender, EventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not SchedulerItemViewModel scheduler)
		{
			return;
		}

		if (scheduler.IsBusy)
		{
			return;
		}

		var succeeded = await _viewModel.RunNowAsync(scheduler);
		var message = succeeded
			? "Test run completed successfully."
			: "Test run failed.";
		await _userNotificationService.ShowShortMessageAsync(message);
	}

	private async void OnDeleteClicked(object sender, EventArgs e)
	{
		if ((sender as BindableObject)?.BindingContext is not SchedulerItemViewModel scheduler)
		{
			return;
		}

		var shouldDelete = await DisplayAlert(
			"Delete scheduler",
			"Remove this scheduler from active list?",
			"Delete",
			"Cancel");

		if (!shouldDelete)
		{
			return;
		}

		await _viewModel.DeleteSchedulerAsync(scheduler);
	}

	private static string ResolveThemeLabel(AppThemePreference preference) => preference switch
	{
		AppThemePreference.Light => "Light",
		AppThemePreference.Dark => "Dark",
		AppThemePreference.Amoled => "AMOLED",
		_ => "System"
	};

	private async Task AnimateRefreshAsync()
	{
		while (_isRefreshAnimating)
		{
			await RefreshIconButton.RotateTo(360, 500, Easing.Linear);
			RefreshIconButton.Rotation = 0;
		}
	}

}
