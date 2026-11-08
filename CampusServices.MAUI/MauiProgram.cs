using CampusServices.MAUI.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Logging;

namespace CampusServices.MAUI;

public static class MauiProgram
{
	public static MauiApp CreateMauiApp()
	{
		var builder = MauiApp.CreateBuilder();
		builder
			.UseMauiApp<App>()
			.ConfigureFonts(fonts =>
			{
				fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
			});

		builder.Services.AddMauiBlazorWebView();

		// ----- Login state for Blazor (<AuthorizeView>, [Authorize]) -----
		builder.Services.AddAuthorizationCore();
		builder.Services.AddCascadingAuthenticationState();
		builder.Services.AddSingleton<SessionStore>();
		builder.Services.AddSingleton<AppAuthStateProvider>();
		builder.Services.AddSingleton<AuthenticationStateProvider>(sp => sp.GetRequiredService<AppAuthStateProvider>());

		// ----- Talking to the API -----
		// Typed HttpClient: ApiClient receives an HttpClient that already knows the API address.
		builder.Services.AddHttpClient<ApiClient>(client =>
		{
			client.BaseAddress = new Uri(ApiSettings.BaseUrl);
			client.Timeout = TimeSpan.FromSeconds(30);
		});
		builder.Services.AddSingleton<AuthService>();

#if DEBUG
		builder.Services.AddBlazorWebViewDeveloperTools();
		builder.Logging.AddDebug();
#endif

		return builder.Build();
	}
}
