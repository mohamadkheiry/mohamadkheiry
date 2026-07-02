using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Admin;

public record FontDto(Guid Id, string Name, string Language, string FontFamily, string? FilePath, bool IsActive);
public record FontAssignmentDto(Guid Id, FontScope Scope, string Language, Guid FontId, string FontName, string FontFamily, int FontSizePx);
public record TypographyDto(List<FontDto> Fonts, List<FontAssignmentDto> Assignments);

public record GetTypographyQuery : IRequest<TypographyDto>;

public class GetTypographyQueryHandler(IAppDbContext db) : IRequestHandler<GetTypographyQuery, TypographyDto>
{
    public async Task<TypographyDto> Handle(GetTypographyQuery request, CancellationToken ct)
    {
        var fonts = await db.Fonts.AsNoTracking()
            .OrderBy(f => f.Language).ThenBy(f => f.Name)
            .Select(f => new FontDto(f.Id, f.Name, f.Language, f.FontFamily, f.FilePath, f.IsActive))
            .ToListAsync(ct);

        var assignments = await db.FontAssignments.AsNoTracking()
            .Select(a => new FontAssignmentDto(a.Id, a.Scope, a.Language, a.FontId, a.Font.Name, a.Font.FontFamily, a.FontSizePx))
            .ToListAsync(ct);

        return new TypographyDto(fonts, assignments);
    }
}

public record UpsertFontCommand(Guid? Id, string Name, string Language, string FontFamily, string? FilePath, bool IsActive) : IRequest<Guid>;

public class UpsertFontCommandValidator : AbstractValidator<UpsertFontCommand>
{
    public UpsertFontCommandValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Language).Must(l => l is "fa" or "en").WithMessage("Language must be 'fa' or 'en'.");
        RuleFor(x => x.FontFamily).NotEmpty().MaximumLength(200);
    }
}

public class UpsertFontCommandHandler(IAppDbContext db) : IRequestHandler<UpsertFontCommand, Guid>
{
    public async Task<Guid> Handle(UpsertFontCommand request, CancellationToken ct)
    {
        Font font;
        if (request.Id.HasValue)
        {
            font = await db.Fonts.FirstOrDefaultAsync(f => f.Id == request.Id.Value, ct)
                ?? throw new NotFoundException("Font not found.");
        }
        else
        {
            font = new Font { Id = Guid.NewGuid() };
            db.Fonts.Add(font);
        }

        font.Name = request.Name.Trim();
        font.Language = request.Language;
        font.FontFamily = request.FontFamily.Trim();
        font.FilePath = request.FilePath;
        font.IsActive = request.IsActive;

        await db.SaveChangesAsync(ct);
        return font.Id;
    }
}

/// <summary>Assigns a font + size to a UI scope (header, titles, body, buttons, captions) per language.</summary>
public record AssignFontCommand(FontScope Scope, string Language, Guid FontId, int FontSizePx) : IRequest;

public class AssignFontCommandHandler(IAppDbContext db) : IRequestHandler<AssignFontCommand>
{
    public async Task Handle(AssignFontCommand request, CancellationToken ct)
    {
        var fontExists = await db.Fonts.AnyAsync(f => f.Id == request.FontId, ct);
        if (!fontExists) throw new NotFoundException("Font not found.");

        var assignment = await db.FontAssignments
            .FirstOrDefaultAsync(a => a.Scope == request.Scope && a.Language == request.Language, ct);

        if (assignment is null)
        {
            assignment = new FontAssignment { Id = Guid.NewGuid(), Scope = request.Scope, Language = request.Language };
            db.FontAssignments.Add(assignment);
        }

        assignment.FontId = request.FontId;
        assignment.FontSizePx = Math.Clamp(request.FontSizePx, 8, 96);
        await db.SaveChangesAsync(ct);
    }
}
