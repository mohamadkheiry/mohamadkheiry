import { api } from '../api/client';
import { Signaling } from './signaling';

/**
 * 1:1 WebRTC peer connection. ICE servers (STUN/TURN) come from the backend,
 * where the super admin configures them.
 */
export class PeerConnectionManager {
  private pc: RTCPeerConnection | null = null;
  private remoteId: string | null = null;
  private screenTrack: MediaStreamTrack | null = null;

  localStream: MediaStream | null = null;
  onRemoteStream: ((stream: MediaStream) => void) | null = null;

  constructor(private signaling: Signaling) {}

  async initLocalMedia(): Promise<MediaStream> {
    this.localStream = await navigator.mediaDevices.getUserMedia({
      video: { width: { ideal: 1280 }, height: { ideal: 720 } },
      audio: { echoCancellation: true, noiseSuppression: true },
    });
    return this.localStream;
  }

  private async createPeer(): Promise<RTCPeerConnection> {
    const iceServers = await api.get<RTCIceServer[]>('/api/calls/ice-servers');
    const pc = new RTCPeerConnection({ iceServers });

    this.localStream?.getTracks().forEach((track) => pc.addTrack(track, this.localStream!));

    pc.onicecandidate = (e) => {
      if (e.candidate && this.remoteId)
        this.signaling.sendIceCandidate(this.remoteId, JSON.stringify(e.candidate));
    };

    pc.ontrack = (e) => {
      if (e.streams[0]) this.onRemoteStream?.(e.streams[0]);
    };

    this.pc = pc;
    return pc;
  }

  /** Called by the side that was already in the room when a new peer joins. */
  async makeOffer(remoteConnectionId: string) {
    this.remoteId = remoteConnectionId;
    const pc = await this.createPeer();
    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);
    await this.signaling.sendOffer(remoteConnectionId, offer.sdp!);
  }

  async handleOffer(fromId: string, sdp: string) {
    this.remoteId = fromId;
    const pc = await this.createPeer();
    await pc.setRemoteDescription({ type: 'offer', sdp });
    const answer = await pc.createAnswer();
    await pc.setLocalDescription(answer);
    await this.signaling.sendAnswer(fromId, answer.sdp!);
  }

  async handleAnswer(sdp: string) {
    await this.pc?.setRemoteDescription({ type: 'answer', sdp });
  }

  async handleIceCandidate(candidateJson: string) {
    try {
      await this.pc?.addIceCandidate(JSON.parse(candidateJson));
    } catch {
      // Candidates may arrive before the description is set; safe to ignore stragglers.
    }
  }

  setMicEnabled(enabled: boolean) {
    this.localStream?.getAudioTracks().forEach((t) => (t.enabled = enabled));
  }

  setCameraEnabled(enabled: boolean) {
    this.localStream?.getVideoTracks().forEach((t) => (t.enabled = enabled));
  }

  /** Replaces the outgoing video track with the screen capture track. */
  async startScreenShare(): Promise<MediaStream> {
    const screen = await navigator.mediaDevices.getDisplayMedia({ video: true, audio: false });
    this.screenTrack = screen.getVideoTracks()[0];
    const sender = this.pc?.getSenders().find((s) => s.track?.kind === 'video');
    if (sender && this.screenTrack) await sender.replaceTrack(this.screenTrack);
    this.screenTrack.onended = () => void this.stopScreenShare();
    return screen;
  }

  async stopScreenShare() {
    this.screenTrack?.stop();
    this.screenTrack = null;
    const cameraTrack = this.localStream?.getVideoTracks()[0];
    const sender = this.pc?.getSenders().find((s) => s.track?.kind === 'video' || s.track === null);
    if (sender && cameraTrack) await sender.replaceTrack(cameraTrack);
  }

  close() {
    this.screenTrack?.stop();
    this.localStream?.getTracks().forEach((t) => t.stop());
    this.pc?.close();
    this.pc = null;
  }
}
