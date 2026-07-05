import { api } from '../api/client';
import type { CascadeResult } from '../api/types';

export interface CascadeCallbacks {
  onSegment: (sourceText: string, translatedText: string) => void;
  onError: (message: string) => void;
}

/**
 * Cascade translation client. Records the REMOTE participant's audio in short
 * segments (voice-activity chunking via MediaRecorder timeslices), sends each
 * segment to the backend pipeline (STT → MT → TTS) and plays back the
 * translated speech through a dedicated, volume-controllable audio element.
 */
export class CascadeTranslator {
  private recorder: MediaRecorder | null = null;
  private chunks: Blob[] = [];
  private running = false;
  private playQueue: string[] = [];
  private playing = false;
  readonly audioElement: HTMLAudioElement;

  constructor(
    private callId: string,
    private targetLanguage: string,
    private callbacks: CascadeCallbacks,
  ) {
    this.audioElement = new Audio();
    this.audioElement.autoplay = true;
  }

  setTargetLanguage(code: string) {
    this.targetLanguage = code;
  }

  setVolume(volume: number) {
    this.audioElement.volume = Math.min(1, Math.max(0, volume));
  }

  start(remoteStream: MediaStream) {
    if (this.running) return;
    this.running = true;

    const audioTracks = remoteStream.getAudioTracks();
    if (audioTracks.length === 0) {
      this.callbacks.onError('Remote stream has no audio track.');
      return;
    }
    const audioOnly = new MediaStream(audioTracks);

    const mime = MediaRecorder.isTypeSupported('audio/webm;codecs=opus') ? 'audio/webm;codecs=opus' : 'audio/webm';
    this.recorder = new MediaRecorder(audioOnly, { mimeType: mime });

    this.recorder.ondataavailable = (e) => {
      if (e.data.size > 0) this.chunks.push(e.data);
    };

    this.recorder.onstop = () => {
      const blob = new Blob(this.chunks, { type: mime });
      this.chunks = [];
      // Skip near-empty segments (silence).
      if (blob.size > 4000) void this.translateSegment(blob);
      if (this.running) this.recorder?.start();
    };

    this.recorder.start();
    // Segment every ~4 seconds: stop → send → restart keeps each file self-contained.
    this.segmentTimer = window.setInterval(() => {
      if (this.recorder?.state === 'recording') this.recorder.stop();
    }, 4000);
  }

  private segmentTimer: number | null = null;

  private async translateSegment(blob: Blob) {
    try {
      const form = new FormData();
      form.append('audio', blob, 'segment.webm');
      form.append('callId', this.callId);
      form.append('targetLanguage', this.targetLanguage);

      const result = await api.postForm<CascadeResult>('/api/translation/cascade', form);
      if (!result.translatedText) return;

      this.callbacks.onSegment(result.sourceText, result.translatedText);
      this.enqueueAudio(`data:${result.audioContentType};base64,${result.audioBase64}`);
    } catch (err) {
      this.callbacks.onError(err instanceof Error ? err.message : 'Translation failed');
    }
  }

  private enqueueAudio(src: string) {
    this.playQueue.push(src);
    if (!this.playing) void this.playNext();
  }

  private async playNext() {
    const next = this.playQueue.shift();
    if (!next) {
      this.playing = false;
      return;
    }
    this.playing = true;
    this.audioElement.src = next;
    try {
      await this.audioElement.play();
      await new Promise<void>((resolve) => {
        this.audioElement.onended = () => resolve();
        this.audioElement.onerror = () => resolve();
      });
    } catch {
      /* autoplay might be blocked until user interaction */
    }
    void this.playNext();
  }

  stop() {
    this.running = false;
    if (this.segmentTimer) window.clearInterval(this.segmentTimer);
    if (this.recorder?.state === 'recording') this.recorder.stop();
    this.recorder = null;
    this.playQueue = [];
    this.audioElement.pause();
  }
}
