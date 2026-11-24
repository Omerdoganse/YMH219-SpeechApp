using System.Text;
using SpeechApp.Models;

namespace SpeechApp.Services;

/// <summary>
/// Metinleri ve tanıma sonuçlarını .txt dosyasına kaydeder.
/// </summary>
public static class TextFileService
{
    public static void SaveText(string path, string text)
    {
        try
        {
            File.WriteAllText(path, text, Encoding.UTF8);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            throw new SpeechServiceException($"Dosya kaydedilemedi: {ex.Message}", ex);
        }
    }

    /// <summary>Tanıma geçmişini zaman damgalı satırlar halinde kaydeder.</summary>
    public static void SaveRecognitionLog(string path, IEnumerable<RecognitionEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine("YMH219 SpeechApp - Konuşma Tanıma Kayıtları");
        sb.AppendLine($"Oluşturulma: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
        sb.AppendLine(new string('-', 50));
        foreach (var entry in entries)
            sb.AppendLine(entry.ToString());
        SaveText(path, sb.ToString());
    }
}
