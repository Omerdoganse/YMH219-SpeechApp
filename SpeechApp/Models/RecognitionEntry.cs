namespace SpeechApp.Models;

/// <summary>
/// Konuşma tanıma sonucunda elde edilen tek bir cümle.
/// </summary>
public sealed class RecognitionEntry
{
    public DateTime Timestamp { get; }
    public string Text { get; }

    /// <summary>Tanıma motorunun güven skoru (0 - 1 arası).</summary>
    public float Confidence { get; }

    public RecognitionEntry(string text, float confidence, DateTime? timestamp = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new ArgumentException("Tanınan metin boş olamaz.", nameof(text));

        Text = text.Trim();
        Confidence = Math.Clamp(confidence, 0f, 1f);
        Timestamp = timestamp ?? DateTime.Now;
    }

    /// <summary>Güven skorunu yüzde olarak verir (örn. "%87").</summary>
    public string ConfidencePercent => $"%{Confidence * 100:0}";

    /// <summary>Dosyaya kaydederken kullanılan satır biçimi.</summary>
    public override string ToString() =>
        $"[{Timestamp:dd.MM.yyyy HH:mm:ss}] ({ConfidencePercent}) {Text}";
}
