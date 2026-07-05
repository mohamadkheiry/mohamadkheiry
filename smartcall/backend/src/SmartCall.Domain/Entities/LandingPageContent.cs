namespace SmartCall.Domain.Entities;

/// <summary>
/// A single editable block of the landing page (light CMS).
/// Content is stored per section key and per language.
/// </summary>
public class LandingPageContent
{
    public Guid Id { get; set; }
    /// <summary>Section identifier, e.g. "hero.title", "features.items", "cta.button".</summary>
    public string SectionKey { get; set; } = null!;
    /// <summary>"fa" or "en".</summary>
    public string Language { get; set; } = null!;
    /// <summary>Plain text, HTML or JSON depending on the section.</summary>
    public string Content { get; set; } = null!;
    /// <summary>Optional media path (image uploaded through the admin panel).</summary>
    public string? MediaPath { get; set; }
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
