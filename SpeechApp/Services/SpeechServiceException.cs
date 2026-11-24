namespace SpeechApp.Services;

/// <summary>
/// Ses servislerinde oluşan ve kullanıcıya doğrudan gösterilebilecek,
/// anlaşılır Türkçe mesaj taşıyan hata türü.
/// </summary>
public class SpeechServiceException : Exception
{
    public SpeechServiceException(string message) : base(message) { }

    public SpeechServiceException(string message, Exception inner) : base(message, inner) { }
}
