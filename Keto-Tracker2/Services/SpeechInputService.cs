namespace Keto_Tracker2.Services;

/// <summary>
/// Voice input through the phone's own speech recogniser (Android RecognizerIntent).
/// Offline recognition is preferred, so with the offline language pack installed nothing leaves the phone.
/// </summary>
public class SpeechInputService
{
    private const string LanguagePrefKey = "voice_language";

    /// <summary>"ru-RU" (default) or "en-US".</summary>
    public string Language
    {
        get => Preferences.Default.Get(LanguagePrefKey, "ru-RU");
        set => Preferences.Default.Set(LanguagePrefKey, value);
    }

    public bool IsRussian => Language.StartsWith("ru", StringComparison.OrdinalIgnoreCase);

#if ANDROID
    public bool IsSupported => true;
#else
    public bool IsSupported => false;
#endif

    /// <summary>
    /// Opens the system "speak now" dialog and returns what was heard (best guess first),
    /// or null when the user cancelled or nothing was recognised.
    /// </summary>
    public Task<IReadOnlyList<string>?> ListenAsync()
    {
#if ANDROID
        return MainActivity.ListenForSpeechAsync(Language);
#else
        return Task.FromResult<IReadOnlyList<string>?>(null);
#endif
    }
}
