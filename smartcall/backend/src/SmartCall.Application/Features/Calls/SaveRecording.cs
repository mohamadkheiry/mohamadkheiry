using MediatR;
using Microsoft.EntityFrameworkCore;
using SmartCall.Application.Common;
using SmartCall.Application.Common.Interfaces;
using SmartCall.Domain.Entities;

namespace SmartCall.Application.Features.Calls;

/// <summary>
/// Starts a recording row; the API layer streams uploaded chunks into storage
/// and finalizes with <see cref="FinalizeRecordingCommand"/>.
/// </summary>
public record StartRecordingCommand(Guid CallId) : IRequest<Guid>;

public class StartRecordingCommandHandler(IAppDbContext db) : IRequestHandler<StartRecordingCommand, Guid>
{
    public async Task<Guid> Handle(StartRecordingCommand request, CancellationToken ct)
    {
        var callExists = await db.Calls.AnyAsync(c => c.Id == request.CallId, ct);
        if (!callExists) throw new NotFoundException("Call not found.");

        var recording = new CallRecording
        {
            Id = Guid.NewGuid(),
            CallId = request.CallId,
            FilePath = $"recordings/{request.CallId}/{Guid.NewGuid():N}.webm",
            StartedAt = DateTime.UtcNow
        };
        db.CallRecordings.Add(recording);
        await db.SaveChangesAsync(ct);
        return recording.Id;
    }
}

public record AppendRecordingChunkCommand(Guid RecordingId, Stream Chunk) : IRequest;

public class AppendRecordingChunkCommandHandler(IAppDbContext db, IFileStorageService storage)
    : IRequestHandler<AppendRecordingChunkCommand>
{
    public async Task Handle(AppendRecordingChunkCommand request, CancellationToken ct)
    {
        var recording = await db.CallRecordings.FirstOrDefaultAsync(r => r.Id == request.RecordingId, ct)
            ?? throw new NotFoundException("Recording not found.");
        await storage.AppendAsync(request.Chunk, recording.FilePath, ct);
    }
}

public record FinalizeRecordingCommand(Guid RecordingId) : IRequest;

public class FinalizeRecordingCommandHandler(IAppDbContext db, IFileStorageService storage)
    : IRequestHandler<FinalizeRecordingCommand>
{
    public async Task Handle(FinalizeRecordingCommand request, CancellationToken ct)
    {
        var recording = await db.CallRecordings.FirstOrDefaultAsync(r => r.Id == request.RecordingId, ct)
            ?? throw new NotFoundException("Recording not found.");
        recording.EndedAt = DateTime.UtcNow;
        recording.FileSizeBytes = await storage.GetSizeAsync(recording.FilePath, ct);
        await db.SaveChangesAsync(ct);
    }
}
