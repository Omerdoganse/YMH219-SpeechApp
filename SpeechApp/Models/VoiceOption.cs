namespace SpeechApp.Models;

/// <summary>
/// ComboBox'ta gösterilen ses veya dil seçeneği.
/// </summary>
/// <param name="Id">Servise iletilecek değer (ses adı veya tanıyıcı kimliği).</param>
/// <param name="DisplayName">Kullanıcıya gösterilecek metin.</param>
public sealed record VoiceOption(string Id, string DisplayName)
{
    public override string ToString() => DisplayName;
}
