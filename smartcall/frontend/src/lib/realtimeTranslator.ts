import { api } from '../api/client';
import type { RealtimeSession } from '../api/types';

export interface RealtimeCallbacks {
  onTranscript: (text: string) => void;
  onError: (message: string) => void;
}

/**
 * Realtime (speech-to-speech) translation client. Gets an ephemeral session
 * from the backend, then opens a WebRTC connection straight to the OpenAI
 * Realtime API: the REMOTE participant's audio goes up, translated speech
 * comes back and plays through a volume-controllable audio element.
 */
export class RealtimeTranslator {
  private pc: RTCPeerConnection | null = null;
  readonly audioElement: HTMLAudioElement;

  constructor(
    private callId: string,
    private targetLanguage: string,
    private callbacks: RealtimeCallbacks,
  ) {
    this.audioElement = new Audio();
    this.audioElement.autoplay = true;
  }

  setVolume(volume: number) {
    this.audioElement.volume = Math.min(1, Math.max(0, volume));
  }

  async start(remoteStream: MediaStream) {
    const session = await api.post<RealtimeSession>('/api/translation/realtime/session', {
      callId: this.callId,
      targetLanguage: this.targetLanguage,
    });

    const pc = new RTCPeerConnection();
    this.pc = pc;

    // Send the remote participant's audio to the model.
    const audioTrack = remoteStream.getAudioTracks()[0];
    if (!audioTrack) throw new Error('Remote stream has no audio track.');
    pc.addTrack(audioTrack, new MediaStream([audioTrack]));

    // Receive translated speech.
    pc.ontrack = (e) => {
      if (e.streams[0]) this.audioElement.srcObject = e.streams[0];
    };

    // Transcript / error events over the data channel.
    const dc = pc.createDataChannel('oai-events');
    dc.onmessage = (e) => {
      try {
        const msg = JSON.parse(e.data);
        if (msg.type === 'response.audio_transcript.done' && msg.transcript)
          this.callbacks.onTranscript(msg.transcript);
        if (msg.type === 'error')
          this.callbacks.onError(msg.error?.message ?? 'Realtime error');
      } catch {
        /* non-JSON frame */
      }
    };

    const offer = await pc.createOffer();
    await pc.setLocalDescription(offer);

    const sdpResponse = await fetch(`${session.baseUrl}/realtime?model=${encodeURIComponent(session.model)}`, {
      method: 'POST',
      body: offer.sdp,
      headers: {
        Authorization: `Bearer ${session.clientSecret}`,
        'Content-Type': 'application/sdp',
      },
    });
    if (!sdpResponse.ok) throw new Error(`Realtime connection failed: HTTP ${sdpResponse.status}`);

    await pc.setRemoteDescription({ type: 'answer', sdp: await sdpResponse.text() });
  }

  stop() {
    this.pc?.close();
    this.pc = null;
    this.audioElement.pause();
    this.audioElement.srcObject = null;
  }
}
