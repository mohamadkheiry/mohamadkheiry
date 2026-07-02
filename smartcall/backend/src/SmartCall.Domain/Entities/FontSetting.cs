namespace SmartCall.Domain.Entities;

/// <summary>An uploaded or registered font family (Persian or English).</summary>
public class Font
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    /// <summary>"fa" or "en".</summary>
    public string Language { get; set; } = null!;
    /// <summary>CSS font-family value.</summary>
    public string FontFamily { get; set; } = null!;
    /// <summary>Relative path of the uploaded font file (woff2/ttf), if self-hosted.</summary>
    public string? FilePath { get; set; }
    public bool IsActive { get; set; } = true;
}

/// <summary>Assigns a font + size to a UI scope (header, body, buttons, ...).</summary>
public class FontAssignment
{
    public Guid Id { get; set; }
    public FontScope Scope { get; set; }
    /// <summary>"fa" or "en" — each UI language has its own assignment per scope.</summary>
    public string Language { get; set; } = null!;
    public Guid FontId { get; set; }
    public Font Font { get; set; } = null!;
    public int FontSizePx { get; set; } = 16;
}
