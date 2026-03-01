using MAUI.Domain;
using MAUI.Presentation.ViewModels;
using Microsoft.Maui.Controls.Shapes;
#if ANDROID
using MAUI.Infrastructure.Android.Scheduling;
#endif

namespace MAUI.Presentation.Pages;

public partial class AddSchedulerPage : ContentPage
{
	private readonly AddSchedulerViewModel _viewModel;
	private bool _isInitialized;
	private bool _isAddClickInFlight;
	private bool _isPreviewClickInFlight;

	public AddSchedulerPage(AddSchedulerViewModel viewModel)
	{
		InitializeComponent();
		_viewModel = viewModel;
		BindingContext = _viewModel;
		SchedulerTypePicker.ItemsSource = new[] { "Custom", "Year Calendar" };
		YearCalendarThemePicker.ItemsSource = new[] { "Use app theme", "System", "Light", "Dark", "AMOLED" };
		ModePicker.ItemsSource = new[] { "One-time", "Daily" };
	}

	protected override async void OnAppearing()
	{
		base.OnAppearing();
		OneTimeDatePicker.MinimumDate = DateTime.Today;

		if (_isInitialized)
		{
			return;
		}

		_isInitialized = true;
		await _viewModel.InitializeAsync();
	}

	private async void OnAddSchedulerClicked(object sender, EventArgs e)
	{
		if (_isAddClickInFlight || _viewModel.IsSubmitting)
		{
			return;
		}

		_isAddClickInFlight = true;
		try
		{
			var added = await _viewModel.AddSchedulerAsync();
			if (!added)
			{
				return;
			}

			if (Navigation.ModalStack.Count > 0)
			{
				await Navigation.PopModalAsync();
				return;
			}

			await Navigation.PopAsync();
		}
		finally
		{
			_isAddClickInFlight = false;
		}
	}

	private async void OnOpenExactAlarmSettingsClicked(object sender, EventArgs e)
	{
		await _viewModel.OpenExactAlarmSettingsAsync();
	}

	private async void OnPreviewYearCalendarClicked(object sender, EventArgs e)
	{
		if (_isPreviewClickInFlight || _viewModel.IsSubmitting)
		{
			return;
		}

		_isPreviewClickInFlight = true;
		try
		{
#if ANDROID
			var endpoint = _viewModel.BuildYearCalendarEndpoint();
			if (!WallpaperSourceEndpoints.TryParseYearCalendar(endpoint, out var request))
			{
				await DisplayAlert("Preview", "Unable to build Year Calendar preview configuration.", "OK");
				return;
			}

			var context = Microsoft.Maui.ApplicationModel.Platform.CurrentActivity ?? global::Android.App.Application.Context;
			using var bitmap = YearCalendarWallpaperGenerator.CreateBitmap(context, request.ThemeMode);
			if (bitmap is null)
			{
				await DisplayAlert("Preview", "Failed to generate calendar preview.", "OK");
				return;
			}

			using var stream = new MemoryStream();
			var compressed = bitmap.Compress(global::Android.Graphics.Bitmap.CompressFormat.Png!, 100, stream);
			if (!compressed)
			{
				await DisplayAlert("Preview", "Failed to encode preview image.", "OK");
				return;
			}

			var bytes = stream.ToArray();
			var imageSource = ImageSource.FromStream(() => new MemoryStream(bytes));
			var previewPage = CreatePreviewPage(imageSource);
			await Navigation.PushAsync(previewPage);
#else
			await DisplayAlert("Preview", "Calendar preview is currently available on Android only.", "OK");
#endif
		}
		finally
		{
			_isPreviewClickInFlight = false;
		}
	}

	private ContentPage CreatePreviewPage(ImageSource imageSource)
	{
		var previewBorder = new Border
		{
			StrokeShape = new RoundRectangle { CornerRadius = 16 },
			Stroke = Color.FromArgb("#2A2C31"),
			BackgroundColor = Color.FromArgb("#15171C"),
			Padding = 8,
			Content = new Image
			{
				Source = imageSource,
				Aspect = Aspect.AspectFit,
				HorizontalOptions = LayoutOptions.Fill,
				VerticalOptions = LayoutOptions.Fill
			}
		};

		var closeButton = new Button
		{
			Text = "Close",
			Margin = new Thickness(0, 12, 0, 0),
			HorizontalOptions = LayoutOptions.Center,
			WidthRequest = 160
		};

		closeButton.Clicked += async (_, _) => await Navigation.PopAsync();
		Grid.SetRow(previewBorder, 0);
		Grid.SetRow(closeButton, 1);

		var page = new ContentPage
		{
			Title = "Year Calendar Preview",
			BackgroundColor = Color.FromArgb("#101114"),
			Content = new Grid
			{
				RowDefinitions =
				[
					new RowDefinition(GridLength.Star),
					new RowDefinition(GridLength.Auto)
				],
				Padding = new Thickness(16),
				Children = { previewBorder, closeButton }
			}
		};

		return page;
	}
}
