namespace SmartCall.Domain.Entities;

/// <summary>
/// Languages selectable as a translation target inside a call. This list is
/// managed by the super admin because OpenAI's supported-language list
/// changes over time.
/// </summary>
public class TranslationLanguage
{
    public Guid Id { get; set; }
    /// <summary>ISO 639-1 code (e.g. "fa", "en", "de").</summary>
    public string Code { get; set; } = null!;
    public string EnglishName { get; set; } = null!;
    public string NativeName { get; set; } = null!;
    public bool IsRtl { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
}
