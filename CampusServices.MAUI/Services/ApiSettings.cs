namespace CampusServices.MAUI.Services;

// Where the API is running. Start the API first, then the app.
public static class ApiSettings
{
#if ANDROID
    // The Android emulator cannot use "localhost" (that is the phone itself).
    // 10.0.2.2 is the special address of YOUR computer. On a real phone use your PC's IP address.
    public const string BaseUrl = "http://10.0.2.2:5080/";
#else
    public const string BaseUrl = "http://localhost:5080/";
#endif
}
