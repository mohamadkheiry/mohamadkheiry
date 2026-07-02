using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common.Interfaces;

namespace SmartCall.Application.Features.Calls;

public record LanguageDto(string Code, string EnglishName, string NativeName, bool IsRtl);

public record GetActiveLanguagesQuery : IRequest<List<LanguageDto>>;

public class GetActiveLanguagesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetActiveLanguagesQuery, List<LanguageDto>>
{
    public async Task<List<LanguageDto>> Handle(GetActiveLanguagesQuery request, CancellationToken ct)
        => await db.TranslationLanguages
            .Where(l => l.IsActive)
            .OrderBy(l => l.SortOrder).ThenBy(l => l.EnglishName)
            .Select(l => new LanguageDto(l.Code, l.EnglishName, l.NativeName, l.IsRtl))
            .ToListAsync(ct);
}
