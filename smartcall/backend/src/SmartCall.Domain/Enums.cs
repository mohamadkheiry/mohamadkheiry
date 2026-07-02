namespace SmartCall.Domain;

public enum CallStatus
{
    Waiting = 0,
    InProgress = 1,
    Ended = 2
}

public enum ParticipantRole
{
    Host = 0,
    Guest = 1,
    SuperAdmin = 2
}

public enum TranslationMethod
{
    Cascade = 0,
    Realtime = 1
}

public enum TokenUsageKind
{
    SpeechToText = 0,
    TextTranslation = 1,
    TextToSpeech = 2,
    RealtimeSpeech = 3
}

public enum FontScope
{
    Header = 0,
    PageTitle = 1,
    Body = 2,
    Button = 3,
    Caption = 4
}

public enum SmtpSecurityMode
{
    None = 0,
    Ssl = 1,
    StartTls = 2
}
