using Microsoft.AspNetCore.SignalR;

namespace SmartCall.Api.Hubs;

/// <summary>
/// WebRTC signaling + realtime call notifications. Clients join a SignalR
/// group per call link code and relay SDP offers/answers and ICE candidates.
/// Translation start/stop events are broadcast so the other side can show
/// live status.
/// </summary>
public class CallHub : Hub
{
    private const string CallGroupPrefix = "call-";

    public async Task JoinCall(string linkCode, string displayName, string participantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, CallGroupPrefix + linkCode);
        Context.Items["linkCode"] = linkCode;
        Context.Items["participantId"] = participantId;
        await Clients.OthersInGroup(CallGroupPrefix + linkCode)
            .SendAsync("PeerJoined", Context.ConnectionId, displayName, participantId);
    }

    // ---- WebRTC signaling relay ----

    public Task SendOffer(string targetConnectionId, string sdp)
        => Clients.Client(targetConnectionId).SendAsync("ReceiveOffer", Context.ConnectionId, sdp);

    public Task SendAnswer(string targetConnectionId, string sdp)
        => Clients.Client(targetConnectionId).SendAsync("ReceiveAnswer", Context.ConnectionId, sdp);

    public Task SendIceCandidate(string targetConnectionId, string candidateJson)
        => Clients.Client(targetConnectionId).SendAsync("ReceiveIceCandidate", Context.ConnectionId, candidateJson);

    // ---- Call state notifications ----

    public Task NotifyTranslationState(string linkCode, bool isActive, string targetLanguage)
        => Clients.OthersInGroup(CallGroupPrefix + linkCode)
            .SendAsync("PeerTranslationState", Context.ConnectionId, isActive, targetLanguage);

    public Task NotifyScreenShareState(string linkCode, bool isSharing)
        => Clients.OthersInGroup(CallGroupPrefix + linkCode)
            .SendAsync("PeerScreenShareState", Context.ConnectionId, isSharing);

    public Task NotifyRecordingState(string linkCode, bool isRecording)
        => Clients.OthersInGroup(CallGroupPrefix + linkCode)
            .SendAsync("PeerRecordingState", Context.ConnectionId, isRecording);

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("linkCode", out var linkCode) && linkCode is string code)
        {
            await Clients.OthersInGroup(CallGroupPrefix + code)
                .SendAsync("PeerLeft", Context.ConnectionId, Context.Items["participantId"] as string);
        }
        await base.OnDisconnectedAsync(exception);
    }
}
