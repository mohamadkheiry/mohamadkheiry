import { FormEvent, useEffect, useRef, useState } from 'react';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Mic, MicOff, Video as VideoIcon, VideoOff, MonitorUp, MonitorX, Disc, Square,
  PhoneOff, Languages, Volume2, VolumeX, Loader2, Radio,
} from 'lucide-react';
import { api } from '../api/client';
import type { CallInfo, JoinResult, Language } from '../api/types';
import { useAuth } from '../store/auth';
import { Signaling } from '../lib/signaling';
import { PeerConnectionManager } from '../lib/webrtc';
import { CascadeTranslator } from '../lib/cascadeTranslator';
import { RealtimeTranslator } from '../lib/realtimeTranslator';
import { CallRecorder } from '../lib/recorder';

type Phase = 'join' | 'connecting' | 'waiting' | 'active' | 'ended';

export default function CallRoom() {
  const { linkCode = '' } = useParams();
  const { t, i18n } = useTranslation();
  const { user } = useAuth();

  const [phase, setPhase] = useState<Phase>('join');
  const [displayName, setDisplayName] = useState(user?.displayName ?? '');
  const [error, setError] = useState('');
  const [callInfo, setCallInfo] = useState<CallInfo | null>(null);
  const [languages, setLanguages] = useState<Language[]>([]);

  // Local device state
  const [micOn, setMicOn] = useState(true);
  const [camOn, setCamOn] = useState(true);
  const [sharing, setSharing] = useState(false);
  const [recording, setRecording] = useState(false);

  // Translation state — each participant independently picks the language
  // they want to HEAR the other side in.
  const [targetLang, setTargetLang] = useState(i18n.language === 'fa' ? 'fa' : 'en');
  const [translating, setTranslating] = useState(false);
  const [translationBusy, setTranslationBusy] = useState(false);

  // Independent audio controls (original vs translated voice of the peer)
  const [originalMuted, setOriginalMuted] = useState(false);
  const [originalVolume, setOriginalVolume] = useState(1);
  const [translatedVolume, setTranslatedVolume] = useState(1);

  // Peer status
  const [peerTranslating, setPeerTranslating] = useState(false);
  const [peerSharing, setPeerSharing] = useState(false);
  const [peerRecordingOn, setPeerRecordingOn] = useState(false);

  const [captionSource, setCaptionSource] = useState('');
  const [captionTranslated, setCaptionTranslated] = useState('');

  const localVideoRef = useRef<HTMLVideoElement>(null);
  const remoteVideoRef = useRef<HTMLVideoElement>(null);
  const signalingRef = useRef<Signaling | null>(null);
  const peerRef = useRef<PeerConnectionManager | null>(null);
  const cascadeRef = useRef<CascadeTranslator | null>(null);
  const realtimeRef = useRef<RealtimeTranslator | null>(null);
  const recorderRef = useRef<CallRecorder | null>(null);
  const remoteStreamRef = useRef<MediaStream | null>(null);
  const joinResultRef = useRef<JoinResult | null>(null);

  useEffect(() => {
    api.get<CallInfo>(`/api/calls/${linkCode}`).then(setCallInfo).catch((e) => setError(e.message));
    api.get<Language[]>('/api/calls/languages').then(setLanguages).catch(() => {});
    return () => cleanup();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [linkCode]);

  useEffect(() => {
    if (remoteVideoRef.current) {
      remoteVideoRef.current.muted = originalMuted;
      remoteVideoRef.current.volume = originalVolume;
    }
  }, [originalMuted, originalVolume, phase]);

  useEffect(() => {
    cascadeRef.current?.setVolume(translatedVolume);
    realtimeRef.current?.setVolume(translatedVolume);
  }, [translatedVolume]);

  const cleanup = () => {
    cascadeRef.current?.stop();
    realtimeRef.current?.stop();
    void recorderRef.current?.stop().catch(() => {});
    peerRef.current?.close();
    void signalingRef.current?.stop().catch(() => {});
    if (joinResultRef.current)
      void api.post(`/api/calls/participants/${joinResultRef.current.participantId}/leave`).catch(() => {});
  };

  const join = async (e: FormEvent) => {
    e.preventDefault();
    setError('');
    setPhase('connecting');
    try {
      const joinResult = await api.post<JoinResult>(`/api/calls/${linkCode}/join`, { displayName });
      joinResultRef.current = joinResult;

      const signaling = new Signaling({
        onPeerJoined: (connectionId) => {
          // Existing side initiates the offer toward the newcomer.
          void peerRef.current?.makeOffer(connectionId);
        },
        onPeerLeft: () => {
          setPhase('waiting');
          if (remoteVideoRef.current) remoteVideoRef.current.srcObject = null;
          remoteStreamRef.current = null;
          stopTranslation();
        },
        onOffer: (fromId, sdp) => void peerRef.current?.handleOffer(fromId, sdp),
        onAnswer: (_fromId, sdp) => void peerRef.current?.handleAnswer(sdp),
        onIceCandidate: (_fromId, candidate) => void peerRef.current?.handleIceCandidate(candidate),
        onPeerTranslationState: (_id, isActive) => setPeerTranslating(isActive),
        onPeerScreenShareState: (_id, isSharing) => setPeerSharing(isSharing),
        onPeerRecordingState: (_id, isRecording) => setPeerRecordingOn(isRecording),
      });
      signalingRef.current = signaling;

      const peer = new PeerConnectionManager(signaling);
      peerRef.current = peer;
      peer.onRemoteStream = (stream) => {
        remoteStreamRef.current = stream;
        if (remoteVideoRef.current) remoteVideoRef.current.srcObject = stream;
        setPhase('active');
      };

      const local = await peer.initLocalMedia();
      if (localVideoRef.current) localVideoRef.current.srcObject = local;

      await signaling.start();
      await signaling.joinCall(linkCode, displayName, joinResult.participantId);
      setPhase('waiting');
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
      setPhase('join');
    }
  };

  const toggleMic = () => {
    peerRef.current?.setMicEnabled(!micOn);
    setMicOn(!micOn);
  };

  const toggleCam = () => {
    peerRef.current?.setCameraEnabled(!camOn);
    setCamOn(!camOn);
  };

  const toggleShare = async () => {
    if (!peerRef.current) return;
    try {
      if (sharing) {
        await peerRef.current.stopScreenShare();
        setSharing(false);
        void signalingRef.current?.notifyScreenShareState(linkCode, false);
      } else {
        await peerRef.current.startScreenShare();
        setSharing(true);
        void signalingRef.current?.notifyScreenShareState(linkCode, true);
      }
    } catch {
      /* user cancelled the picker */
    }
  };

  const toggleRecord = async () => {
    if (!callInfo) return;
    try {
      if (recording) {
        await recorderRef.current?.stop();
        setRecording(false);
        void signalingRef.current?.notifyRecordingState(linkCode, false);
      } else {
        const recorder = new CallRecorder();
        recorderRef.current = recorder;
        const streams = [peerRef.current?.localStream, remoteStreamRef.current].filter(Boolean) as MediaStream[];
        await recorder.start(callInfo.callId, streams);
        setRecording(true);
        void signalingRef.current?.notifyRecordingState(linkCode, true);
      }
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    }
  };

  const startTranslation = async () => {
    if (!callInfo || !remoteStreamRef.current || !joinResultRef.current) return;
    setTranslationBusy(true);
    setError('');
    try {
      await api.put(`/api/calls/participants/${joinResultRef.current.participantId}/language`, {
        languageCode: targetLang,
      });

      const { method } = await api.get<{ method: string }>('/api/translation/method');

      if (method === 'realtime') {
        const rt = new RealtimeTranslator(callInfo.callId, targetLang, {
          onTranscript: (text) => setCaptionTranslated(text),
          onError: (message) => setError(message),
        });
        realtimeRef.current = rt;
        rt.setVolume(translatedVolume);
        await rt.start(remoteStreamRef.current);
      } else {
        const cascade = new CascadeTranslator(callInfo.callId, targetLang, {
          onSegment: (source, translated) => {
            setCaptionSource(source);
            setCaptionTranslated(translated);
          },
          onError: (message) => setError(message),
        });
        cascadeRef.current = cascade;
        cascade.setVolume(translatedVolume);
        cascade.start(remoteStreamRef.current);
      }

      setTranslating(true);
      void signalingRef.current?.notifyTranslationState(linkCode, true, targetLang);
    } catch (err) {
      setError(err instanceof Error ? err.message : t('common.error'));
    } finally {
      setTranslationBusy(false);
    }
  };

  const stopTranslation = () => {
    cascadeRef.current?.stop();
    cascadeRef.current = null;
    realtimeRef.current?.stop();
    realtimeRef.current = null;
    setTranslating(false);
    setCaptionSource('');
    setCaptionTranslated('');
    void signalingRef.current?.notifyTranslationState(linkCode, false, targetLang);
  };

  const hangUp = () => {
    cleanup();
    setPhase('ended');
  };

  // ---- Join screen ----
  if (phase === 'join' || phase === 'connecting') {
    return (
      <div className="card auth-card">
        <h2>{t('call.joinTitle')}</h2>
        {callInfo && <p style={{ color: 'var(--text-dim)', fontSize: 14 }}>{callInfo.hostName}</p>}
        {error && <div className="error-box">{error}</div>}
        <form onSubmit={join}>
          <div className="field">
            <label>{t('call.yourName')}</label>
            <input value={displayName} onChange={(e) => setDisplayName(e.target.value)} required autoFocus />
          </div>
          <button className="btn" type="submit" disabled={phase === 'connecting'} style={{ width: '100%' }}>
            {phase === 'connecting' ? <Loader2 size={16} className="spin" /> : <VideoIcon size={16} />}
            {t('call.join')}
          </button>
        </form>
      </div>
    );
  }

  if (phase === 'ended') {
    return (
      <div className="card auth-card" style={{ textAlign: 'center' }}>
        <h2>{t('call.ended')}</h2>
        <a href="/" className="btn secondary">{t('app.name')}</a>
      </div>
    );
  }

  // ---- Call screen ----
  return (
    <div className="call-room">
      <div className="videos">
        <video ref={remoteVideoRef} className="remote" autoPlay playsInline />
        <video ref={localVideoRef} className="local" autoPlay playsInline muted />

        {phase === 'waiting' && (
          <div className="waiting-overlay">
            <Loader2 size={36} className="spin" />
            <span>{t('call.waiting')}</span>
          </div>
        )}

        {(peerTranslating || peerSharing || peerRecordingOn || recording) && (
          <div className="status-toast">
            {(peerRecordingOn || recording) && <><Disc size={14} color="#ef4757" /> {t('call.peerRecording')}</>}
            {peerTranslating && <><Languages size={14} /> {t('call.peerTranslating')}</>}
            {peerSharing && <><MonitorUp size={14} /> {t('call.peerSharing')}</>}
          </div>
        )}

        {(captionSource || captionTranslated) && (
          <div className="captions">
            {captionSource && <div><span className="line">{captionSource}</span></div>}
            {captionTranslated && <div><span className="line translated">{captionTranslated}</span></div>}
          </div>
        )}
      </div>

      <div className="translation-panel">
        <div className="group">
          <Languages size={16} />
          <span>{t('call.hearIn')}</span>
          <select value={targetLang} onChange={(e) => setTargetLang(e.target.value)} disabled={translating} style={{ width: 'auto' }}>
            {languages.map((l) => (
              <option key={l.code} value={l.code}>
                {l.nativeName} ({l.englishName})
              </option>
            ))}
          </select>
          {!translating ? (
            <button className="btn" onClick={startTranslation} disabled={translationBusy || phase !== 'active'}>
              {translationBusy ? <Loader2 size={15} className="spin" /> : <Radio size={15} />}
              {t('call.startTranslation')}
            </button>
          ) : (
            <button className="btn danger" onClick={stopTranslation}>
              <Square size={15} />
              {t('call.stopTranslation')}
            </button>
          )}
        </div>

        <div className="group">
          <button
            className="btn icon secondary"
            onClick={() => setOriginalMuted(!originalMuted)}
            title={t('call.originalAudio')}
          >
            {originalMuted ? <VolumeX size={16} /> : <Volume2 size={16} />}
          </button>
          <span>{t('call.originalVolume')}</span>
          <input type="range" min={0} max={1} step={0.05} value={originalVolume}
            onChange={(e) => setOriginalVolume(Number(e.target.value))} />
        </div>

        <div className="group">
          <Languages size={16} />
          <span>{t('call.translatedVolume')}</span>
          <input type="range" min={0} max={1} step={0.05} value={translatedVolume}
            onChange={(e) => setTranslatedVolume(Number(e.target.value))} />
        </div>
      </div>

      <div className="call-toolbar">
        <button className={`toolbar-btn ${micOn ? '' : 'danger'}`} onClick={toggleMic}>
          {micOn ? <Mic size={20} /> : <MicOff size={20} />}
          {t('call.mic')}
        </button>
        <button className={`toolbar-btn ${camOn ? '' : 'danger'}`} onClick={toggleCam}>
          {camOn ? <VideoIcon size={20} /> : <VideoOff size={20} />}
          {t('call.camera')}
        </button>
        <button className={`toolbar-btn ${sharing ? 'active' : ''}`} onClick={toggleShare} disabled={phase !== 'active'}>
          {sharing ? <MonitorX size={20} /> : <MonitorUp size={20} />}
          {sharing ? t('call.stopShare') : t('call.screenShare')}
        </button>
        <button className={`toolbar-btn ${recording ? 'active' : ''}`} onClick={toggleRecord}>
          {recording ? <Square size={20} /> : <Disc size={20} />}
          {recording ? t('call.stopRecord') : t('call.record')}
        </button>
        <button className="toolbar-btn danger" onClick={hangUp}>
          <PhoneOff size={20} />
          {t('call.hangup')}
        </button>
      </div>
    </div>
  );
}
