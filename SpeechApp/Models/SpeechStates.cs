namespace SpeechApp.Models;

/// <summary>Metin okuma (Text-to-Speech) bölümünün durumları.</summary>
public enum TtsState
{
    /// <summary>Okuma yapılmıyor.</summary>
    Idle,
    /// <summary>Metin sesli okunuyor.</summary>
    Speaking,
    /// <summary>Okuma duraklatıldı.</summary>
    Paused
}

/// <summary>Konuşma tanıma (Speech-to-Text) bölümünün durumları.</summary>
public enum SttState
{
    /// <summary>Mikrofon dinlenmiyor.</summary>
    Idle,
    /// <summary>Mikrofon dinleniyor.</summary>
    Listening,
    /// <summary>Dinleme durduruluyor (son cümle işleniyor).</summary>
    Stopping
}
