import * as signalR from '@microsoft/signalr';
import { getToken } from '../api/client';

export interface SignalingEvents {
  onPeerJoined: (connectionId: string, displayName: string, participantId: string) => void;
  onPeerLeft: (connectionId: string) => void;
  onOffer: (fromId: string, sdp: string) => void;
  onAnswer: (fromId: string, sdp: string) => void;
  onIceCandidate: (fromId: string, candidateJson: string) => void;
  onPeerTranslationState: (fromId: string, isActive: boolean, targetLanguage: string) => void;
  onPeerScreenShareState: (fromId: string, isSharing: boolean) => void;
  onPeerRecordingState: (fromId: string, isRecording: boolean) => void;
}

/** SignalR wrapper used for WebRTC signaling and live call notifications. */
export class Signaling {
  private connection: signalR.HubConnection;

  constructor(private events: SignalingEvents) {
    this.connection = new signalR.HubConnectionBuilder()
      .withUrl('/hubs/call', { accessTokenFactory: () => getToken() ?? '' })
      .withAutomaticReconnect()
      .build();

    this.connection.on('PeerJoined', events.onPeerJoined);
    this.connection.on('PeerLeft', events.onPeerLeft);
    this.connection.on('ReceiveOffer', events.onOffer);
    this.connection.on('ReceiveAnswer', events.onAnswer);
    this.connection.on('ReceiveIceCandidate', events.onIceCandidate);
    this.connection.on('PeerTranslationState', events.onPeerTranslationState);
    this.connection.on('PeerScreenShareState', events.onPeerScreenShareState);
    this.connection.on('PeerRecordingState', events.onPeerRecordingState);
  }

  async start() {
    await this.connection.start();
  }

  async stop() {
    await this.connection.stop();
  }

  joinCall(linkCode: string, displayName: string, participantId: string) {
    return this.connection.invoke('JoinCall', linkCode, displayName, participantId);
  }

  sendOffer(targetId: string, sdp: string) {
    return this.connection.invoke('SendOffer', targetId, sdp);
  }

  sendAnswer(targetId: string, sdp: string) {
    return this.connection.invoke('SendAnswer', targetId, sdp);
  }

  sendIceCandidate(targetId: string, candidateJson: string) {
    return this.connection.invoke('SendIceCandidate', targetId, candidateJson);
  }

  notifyTranslationState(linkCode: string, isActive: boolean, targetLanguage: string) {
    return this.connection.invoke('NotifyTranslationState', linkCode, isActive, targetLanguage);
  }

  notifyScreenShareState(linkCode: string, isSharing: boolean) {
    return this.connection.invoke('NotifyScreenShareState', linkCode, isSharing);
  }

  notifyRecordingState(linkCode: string, isRecording: boolean) {
    return this.connection.invoke('NotifyRecordingState', linkCode, isRecording);
  }
}
