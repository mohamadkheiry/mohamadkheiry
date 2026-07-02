export interface AuthResult {
  token: string;
  userId: string;
  email: string;
  displayName: string;
  isSuperAdmin: boolean;
}

export interface CallInfo {
  callId: string;
  linkCode: string;
  status: number; // 0 waiting, 1 in progress, 2 ended
  hostName: string;
  createdAt: string;
}

export interface JoinResult {
  callId: string;
  participantId: string;
  role: number; // 0 host, 1 guest, 2 super admin
}

export interface Language {
  code: string;
  englishName: string;
  nativeName: string;
  isRtl: boolean;
}

export interface CascadeResult {
  sourceText: string;
  translatedText: string;
  audioBase64: string;
  audioContentType: string;
}

export interface RealtimeSession {
  clientSecret: string;
  model: string;
  baseUrl: string;
  expiresAt: string;
}

export interface AiSettings {
  hasApiKey: boolean;
  baseUrl: string | null;
  sttModel: string | null;
  translationModel: string | null;
  ttsModel: string | null;
  ttsVoice: string | null;
  realtimeModel: string | null;
  activeMethod: string;
}

export interface GeneralSettings {
  defaultLanguage: string;
  allowLanguageSwitch: boolean;
  iceServersJson: string | null;
}

export interface LandingContentItem {
  id: string;
  sectionKey: string;
  language: string;
  content: string;
  mediaPath: string | null;
}

export interface AdminCall {
  id: string;
  linkCode: string;
  status: number;
  hostName: string;
  createdAt: string;
  startedAt: string | null;
  endedAt: string | null;
  participants: { displayName: string; role: number; targetLanguageCode: string | null; joinedAt: string; leftAt: string | null }[];
  recordings: { id: string; filePath: string; fileSizeBytes: number; startedAt: string; endedAt: string | null }[];
}

export interface AdminUser {
  id: string;
  email: string;
  displayName: string;
  isSuperAdmin: boolean;
  isActive: boolean;
  createdAt: string;
  totalTokensUsed: number;
}

export interface TokenUsageReport {
  systemTotalTokens: number;
  byUser: { userId: string | null; email: string | null; inputTokens: number; outputTokens: number; totalTokens: number }[];
  byCall: { callId: string | null; linkCode: string | null; totalTokens: number }[];
}

export interface AdminLanguage extends Language {
  id: string;
  isActive: boolean;
  sortOrder: number;
}

export interface FontInfo {
  id: string;
  name: string;
  language: string;
  fontFamily: string;
  filePath: string | null;
  isActive: boolean;
}

export interface FontAssignmentInfo {
  id: string;
  scope: number;
  language: string;
  fontId: string;
  fontName: string;
  fontFamily: string;
  fontSizePx: number;
}

export interface Typography {
  fonts: FontInfo[];
  assignments: FontAssignmentInfo[];
}

export interface SmtpSettings {
  host: string | null;
  port: number;
  username: string | null;
  hasPassword: boolean;
  securityMode: number;
  senderName: string | null;
  senderEmail: string | null;
}
