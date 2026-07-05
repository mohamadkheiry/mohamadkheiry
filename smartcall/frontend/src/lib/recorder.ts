import { api } from '../api/client';

/**
 * Call recorder: captures local + remote media into a single WebM using a
 * canvas composition, uploading chunks to the server as they are produced so
 * the recording survives crashes and is available to the super admin.
 */
export class CallRecorder {
  private recorder: MediaRecorder | null = null;
  private recordingId: string | null = null;
  private uploadChain: Promise<void> = Promise.resolve();

  async start(callId: string, streams: MediaStream[]) {
    const res = await api.post<{ recordingId: string }>('/api/recordings/start', { callId });
    this.recordingId = res.recordingId;

    // Mix all audio tracks + first available video track.
    const mixed = new MediaStream();
    const audioCtx = new AudioContext();
    const dest = audioCtx.createMediaStreamDestination();
    let hasAudio = false;

    for (const stream of streams) {
      if (stream.getAudioTracks().length > 0) {
        audioCtx.createMediaStreamSource(stream).connect(dest);
        hasAudio = true;
      }
    }
    if (hasAudio) dest.stream.getAudioTracks().forEach((t) => mixed.addTrack(t));

    const videoTrack = streams.flatMap((s) => s.getVideoTracks())[0];
    if (videoTrack) mixed.addTrack(videoTrack);

    const mime = MediaRecorder.isTypeSupported('video/webm;codecs=vp8,opus') ? 'video/webm;codecs=vp8,opus' : 'video/webm';
    this.recorder = new MediaRecorder(mixed, { mimeType: mime, videoBitsPerSecond: 1_500_000 });

    this.recorder.ondataavailable = (e) => {
      if (e.data.size > 0 && this.recordingId) {
        const id = this.recordingId;
        // Chain uploads so chunks are appended in order.
        this.uploadChain = this.uploadChain.then(() => this.uploadChunk(id, e.data));
      }
    };

    this.recorder.start(5000); // flush a chunk every 5s
  }

  private async uploadChunk(recordingId: string, chunk: Blob) {
    try {
      await fetch(`/api/recordings/${recordingId}/chunk`, { method: 'POST', body: chunk });
    } catch {
      /* keep recording; a missed chunk degrades but doesn't stop capture */
    }
  }

  async stop() {
    if (!this.recorder) return;
    const stopped = new Promise<void>((resolve) => {
      this.recorder!.onstop = () => resolve();
    });
    this.recorder.stop();
    await stopped;
    await this.uploadChain;
    if (this.recordingId) await api.post(`/api/recordings/${this.recordingId}/finalize`);
    this.recorder = null;
    this.recordingId = null;
  }

  get isRecording() {
    return this.recorder?.state === 'recording';
  }
}
