using System.Speech.Synthesis;
using SpeechApp.Models;

namespace SpeechApp.Services;

/// <summary>Okunan kelimenin metin içindeki konumunu taşır (vurgulama için).</summary>
public sealed class WordProgressEventArgs(int position, int length) : EventArgs
{
    public int Position { get; } = position;
    public int Length { get; } = length;
}

/// <summary>
/// System.Speech SpeechSynthesizer sınıfını saran Text-to-Speech servisi.
/// Okut / Duraklat / Devam / Durdur işlemlerini ve ses ayarlarını yönetir.
/// </summary>
public sealed class TextToSpeechService : IDisposable
{
    public const int MinRate = -10;
    public const int MaxRate = 10;

    private readonly SpeechSynthesizer _synth;
    private bool _disposed;

    public TtsState State { get; private set; } = TtsState.Idle;

    /// <summary>Okuma durumu değiştiğinde tetiklenir.</summary>
    public event EventHandler? StateChanged;

    /// <summary>Her kelime okunmaya başladığında tetiklenir.</summary>
    public event EventHandler<WordProgressEventArgs>? WordSpoken;

    /// <summary>Okuma sırasında hata oluşursa tetiklenir.</summary>
    public event EventHandler<SpeechServiceException>? ErrorOccurred;

    public TextToSpeechService()
    {
        try
        {
            _synth = new SpeechSynthesizer();
            _synth.SetOutputToDefaultAudioDevice();
        }
        catch (Exception ex) when (ex is InvalidOperationException or PlatformNotSupportedException
                                       or System.Runtime.InteropServices.COMException)
        {
            throw new SpeechServiceException(
                "Konuşma sentezleyici başlatılamadı. Windows ses servisi veya hoparlör bulunamadı.", ex);
        }

        _synth.SpeakProgress += (_, e) =>
            WordSpoken?.Invoke(this, new WordProgressEventArgs(e.CharacterPosition, e.CharacterCount));

        _synth.SpeakCompleted += (_, e) =>
        {
            SetState(TtsState.Idle);
            if (e.Error != null && !e.Cancelled)
                ErrorOccurred?.Invoke(this,
                    new SpeechServiceException($"Metin okunurken hata oluştu: {e.Error.Message}", e.Error));
        };
    }

    /// <summary>Bilgisayarda kurulu ve etkin sesleri listeler.</summary>
    public IReadOnlyList<VoiceOption> GetVoices() =>
        _synth.GetInstalledVoices()
              .Where(v => v.Enabled)
              .Select(v => new VoiceOption(v.VoiceInfo.Name,
                  $"{v.VoiceInfo.Name} ({v.VoiceInfo.Culture.DisplayName}, {TranslateGender(v.VoiceInfo.Gender)})",
                  v.VoiceInfo.Culture))
              .ToList();

    private static string TranslateGender(VoiceGender gender) => gender switch
    {
        VoiceGender.Male => "Erkek",
        VoiceGender.Female => "Kadın",
        _ => "Belirsiz"
    };

    public string CurrentVoiceName => _synth.Voice.Name;

    public void SelectVoice(string voiceName)
    {
        EnsureNotDisposed();
        try
        {
            _synth.SelectVoice(voiceName);
        }
        catch (ArgumentException ex)
        {
            throw new SpeechServiceException($"'{voiceName}' sesi seçilemedi.", ex);
        }
    }

    /// <summary>Okuma hızı (-10 en yavaş, 10 en hızlı).</summary>
    public int Rate
    {
        get => _synth.Rate;
        set => _synth.Rate = Math.Clamp(value, MinRate, MaxRate);
    }

    /// <summary>Ses seviyesi (0 - 100).</summary>
    public int Volume
    {
        get => _synth.Volume;
        set => _synth.Volume = Math.Clamp(value, 0, 100);
    }

    /// <summary>Metni asenkron olarak okumaya başlar.</summary>
    public void Speak(string text)
    {
        EnsureNotDisposed();
        if (string.IsNullOrWhiteSpace(text))
            throw new SpeechServiceException("Okunacak metin boş. Lütfen önce bir metin yazın.");

        if (State != TtsState.Idle)
            Stop();

        _synth.SpeakAsync(text);
        SetState(TtsState.Speaking);
    }

    public void Pause()
    {
        if (State != TtsState.Speaking) return;
        _synth.Pause();
        SetState(TtsState.Paused);
    }

    public void Resume()
    {
        if (State != TtsState.Paused) return;
        _synth.Resume();
        SetState(TtsState.Speaking);
    }

    /// <summary>Okumayı tamamen durdurur.</summary>
    public void Stop()
    {
        if (State == TtsState.Idle) return;
        if (State == TtsState.Paused)
            _synth.Resume(); // Duraklatılmış sentezleyici iptal edilmeden önce devam ettirilmeli
        _synth.SpeakAsyncCancelAll();
        SetState(TtsState.Idle);
    }

    /// <summary>Metni hoparlöre değil, bir .wav ses dosyasına yazar.</summary>
    public void SaveToWave(string path, string text)
    {
        EnsureNotDisposed();
        if (string.IsNullOrWhiteSpace(text))
            throw new SpeechServiceException("Kaydedilecek metin boş.");
        if (State != TtsState.Idle)
            throw new SpeechServiceException("Okuma devam ederken ses dosyası oluşturulamaz. Önce okumayı durdurun.");

        try
        {
            _synth.SetOutputToWaveFile(path);
            _synth.Speak(text);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
        {
            throw new SpeechServiceException($"Ses dosyası oluşturulamadı: {ex.Message}", ex);
        }
        finally
        {
            _synth.SetOutputToDefaultAudioDevice();
        }
    }

    private void SetState(TtsState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    private void EnsureNotDisposed() => ObjectDisposedException.ThrowIf(_disposed, this);

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _synth.SpeakAsyncCancelAll(); } catch (InvalidOperationException) { }
        _synth.Dispose();
    }
}
