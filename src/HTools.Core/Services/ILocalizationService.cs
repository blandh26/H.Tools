namespace HTools.Core.Services;

public sealed record LanguageOption(string Code, string NativeName);

public interface ILocalizationService
{
    IReadOnlyList<LanguageOption> SupportedLanguages { get; }

    string CurrentLanguage { get; }

    event EventHandler? LanguageChanged;

    void SetLanguage(string languageCode);

    string Translate(string key);

    string this[string key] { get; }
}
