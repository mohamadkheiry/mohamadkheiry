using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Admin;

public record AdminLanguageDto(Guid Id, string Code, string EnglishName, string NativeName, bool IsRtl, bool IsActive, int SortOrder);

public record GetAdminLanguagesQuery : IRequest<List<AdminLanguageDto>>;

public class GetAdminLanguagesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetAdminLanguagesQuery, List<AdminLanguageDto>>
{
    public async Task<List<AdminLanguageDto>> Handle(GetAdminLanguagesQuery request, CancellationToken ct)
        => await db.TranslationLanguages.AsNoTracking()
            .OrderBy(l => l.SortOrder).ThenBy(l => l.EnglishName)
            .Select(l => new AdminLanguageDto(l.Id, l.Code, l.EnglishName, l.NativeName, l.IsRtl, l.IsActive, l.SortOrder))
            .ToListAsync(ct);
}

/// <summary>
/// Adds or updates a selectable translation language. The list is admin-managed
/// because OpenAI's supported languages change over time.
/// </summary>
public record UpsertLanguageCommand(Guid? Id, string Code, string EnglishName, string NativeName, bool IsRtl, bool IsActive, int SortOrder) : IRequest<Guid>;

public class UpsertLanguageCommandValidator : AbstractValidator<UpsertLanguageCommand>
{
    public UpsertLanguageCommandValidator()
    {
        RuleFor(x => x.Code).NotEmpty().MaximumLength(10);
        RuleFor(x => x.EnglishName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.NativeName).NotEmpty().MaximumLength(100);
    }
}

public class UpsertLanguageCommandHandler(IAppDbContext db) : IRequestHandler<UpsertLanguageCommand, Guid>
{
    public async Task<Guid> Handle(UpsertLanguageCommand request, CancellationToken ct)
    {
        TranslationLanguage entity;
        if (request.Id.HasValue)
        {
            entity = await db.TranslationLanguages.FirstOrDefaultAsync(l => l.Id == request.Id.Value, ct)
                ?? throw new NotFoundException("Language not found.");
        }
        else
        {
            entity = new TranslationLanguage { Id = Guid.NewGuid() };
            db.TranslationLanguages.Add(entity);
        }

        entity.Code = request.Code.Trim().ToLowerInvariant();
        entity.EnglishName = request.EnglishName.Trim();
        entity.NativeName = request.NativeName.Trim();
        entity.IsRtl = request.IsRtl;
        entity.IsActive = request.IsActive;
        entity.SortOrder = request.SortOrder;

        await db.SaveChangesAsync(ct);
        return entity.Id;
    }
}

public record DeleteLanguageCommand(Guid Id) : IRequest;

public class DeleteLanguageCommandHandler(IAppDbContext db) : IRequestHandler<DeleteLanguageCommand>
{
    public async Task Handle(DeleteLanguageCommand request, CancellationToken ct)
    {
        var entity = await db.TranslationLanguages.FirstOrDefaultAsync(l => l.Id == request.Id, ct)
            ?? throw new NotFoundException("Language not found.");
        db.TranslationLanguages.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
