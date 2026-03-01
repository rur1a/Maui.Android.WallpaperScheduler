using MAUI.Presentation.Pages;

namespace MAUI;

public partial class App : Microsoft.Maui.Controls.Application
{
	public App(IServiceProvider serviceProvider)
	{
		InitializeComponent();
		MainPage = serviceProvider.GetRequiredService<SchedulerManagerPage>();
	}
}
