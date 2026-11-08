namespace CampusServices.MAUI;

// "Microsoft.Maui.Controls.Application" is written in full because our solution also has a
// namespace called CampusServices.Application (the Application layer) and the short name would clash.
public partial class App : Microsoft.Maui.Controls.Application
{
	public App()
	{
		InitializeComponent();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(new MainPage()) { Title = "Campus Services" };
	}
}
