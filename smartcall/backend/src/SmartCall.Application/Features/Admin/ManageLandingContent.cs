using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Admin;

public record LandingContentDto(Guid Id, string SectionKey, string Language, string Content, string? MediaPath);

/// <summary>Public query — the landing page reads its content from here (light CMS).</summary>
public record GetLandingContentQuery(string Language) : IRequest<List<LandingContentDto>>;

public class GetLandingContentQueryHandler(IAppDbContext db)
    : IRequestHandler<GetLandingContentQuery, List<LandingContentDto>>
{
    public async Task<List<LandingContentDto>> Handle(GetLandingContentQuery request, CancellationToken ct)
        => await db.LandingPageContents.AsNoTracking()
            .Where(c => c.Language == request.Language)
            .Select(c => new LandingContentDto(c.Id, c.SectionKey, c.Language, c.Content, c.MediaPath))
            .ToListAsync(ct);
}

public record UpsertLandingContentCommand(string SectionKey, string Language, string Content, string? MediaPath) : IRequest;

public class UpsertLandingContentCommandValidator : AbstractValidator<UpsertLandingContentCommand>
{
    public UpsertLandingContentCommandValidator()
    {
        RuleFor(x => x.SectionKey).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Language).Must(l => l is "fa" or "en").WithMessage("Language must be 'fa' or 'en'.");
    }
}

public class UpsertLandingContentCommandHandler(IAppDbContext db) : IRequestHandler<UpsertLandingContentCommand>
{
    public async Task Handle(UpsertLandingContentCommand request, CancellationToken ct)
    {
        var entity = await db.LandingPageContents
            .FirstOrDefaultAsync(c => c.SectionKey == request.SectionKey && c.Language == request.Language, ct);

        if (entity is null)
        {
            entity = new LandingPageContent
            {
                Id = Guid.NewGuid(),
                SectionKey = request.SectionKey,
                Language = request.Language
            };
            db.LandingPageContents.Add(entity);
        }

        entity.Content = request.Content;
        entity.MediaPath = request.MediaPath;
        entity.UpdatedAt = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
    }
}
