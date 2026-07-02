using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;

namespace SmartCall.Application.Features.Calls;

/// <summary>
/// Each participant independently picks the language they want to HEAR the
/// other side in. Directions are per-participant and may differ.
/// </summary>
public record SetParticipantLanguageCommand(Guid ParticipantId, string LanguageCode) : IRequest;

public class SetParticipantLanguageCommandValidator : AbstractValidator<SetParticipantLanguageCommand>
{
    public SetParticipantLanguageCommandValidator()
    {
        RuleFor(x => x.LanguageCode).NotEmpty().MaximumLength(10);
    }
}

public class SetParticipantLanguageCommandHandler(IAppDbContext db)
    : IRequestHandler<SetParticipantLanguageCommand>
{
    public async Task Handle(SetParticipantLanguageCommand request, CancellationToken ct)
    {
        var participant = await db.CallParticipants
            .FirstOrDefaultAsync(p => p.Id == request.ParticipantId, ct)
            ?? throw new NotFoundException("Participant not found.");

        var languageExists = await db.TranslationLanguages
            .AnyAsync(l => l.Code == request.LanguageCode && l.IsActive, ct);
        if (!languageExists)
            throw new AppValidationException("The selected language is not available.");

        participant.TargetLanguageCode = request.LanguageCode;
        await db.SaveChangesAsync(ct);
    }
}
