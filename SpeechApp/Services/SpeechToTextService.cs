using System.Globalization;
using System.Speech.Recognition;
using SpeechApp.Models;

namespace SpeechApp.Services;

/// <summary>
/// System.Speech SpeechRecognitionEngine sınıfını saran Speech-to-Text servisi.
/// Varsayılan mikrofonu dinler ve serbest dikte (DictationGrammar) ile konuşmayı metne çevirir.
/// </summary>
public sealed class SpeechToTextService : IDisposable
{
    private SpeechRecognitionEngine? _engine;
    private bool _disposed;

    public SttState State { get; private set; } = SttState.Idle;

    /// <summary>Kullanılan tanıyıcının dili (dinleme başlayınca dolar).</summary>
    public CultureInfo? CurrentCulture { get; private set; }

    public event EventHandler? StateChanged;

    /// <summary>Bir cümle kesin olarak tanındığında tetiklenir.</summary>
    public event EventHandler<RecognitionEntry>? SpeechRecognized;

    /// <summary>Konuşma devam ederken motorun anlık tahmini (ara sonuç).</summary>
    public event EventHandler<string>? HypothesisChanged;

    /// <summary>Mikrofon ses seviyesi (0 - 100).</summary>
    public event EventHandler<int>? AudioLevelChanged;

    public event EventHandler<SpeechServiceException>? ErrorOccurred;

    /// <summary>Bilgisayarda kurulu konuşma tanıyıcılarını (dilleri) listeler.</summary>
    public static IReadOnlyList<VoiceOption> GetRecognizers()
    {
        try
        {
            return SpeechRecognitionEngine.InstalledRecognizers()
                .Select(r => new VoiceOption(r.Id, $"{r.Culture.DisplayName} ({r.Culture.Name})", r.Culture))
                .ToList();
        }
        catch (Exception ex) when (ex is InvalidOperationException or PlatformNotSupportedException
                                       or System.Runtime.InteropServices.COMException)
        {
            return [];
        }
    }

    /// <summary>Seçilen tanıyıcı ile mikrofonu dinlemeye başlar.</summary>
    /// <param name="recognizerId">GetRecognizers() ile alınan tanıyıcı kimliği.</param>
    public void Start(string recognizerId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (State != SttState.Idle) return;

        var info = SpeechRecognitionEngine.InstalledRecognizers().FirstOrDefault(r => r.Id == recognizerId)
                   ?? throw new SpeechServiceException(
                       "Seçilen dil için konuşma tanıyıcı bulunamadı. Windows Ayarlar > Saat ve Dil > Konuşma " +
                       "bölümünden konuşma dil paketini yükleyin.");

        var engine = new SpeechRecognitionEngine(info);
        try
        {
            engine.SetInputToDefaultAudioDevice();
        }
        catch (InvalidOperationException ex)
        {
            engine.Dispose();
            throw new SpeechServiceException(
                "Mikrofon bulunamadı veya erişilemiyor. Mikrofonun takılı olduğundan ve Windows Gizlilik " +
                "ayarlarında uygulamaların mikrofona erişimine izin verildiğinden emin olun.", ex);
        }

        engine.LoadGrammar(new DictationGrammar { Name = "Serbest dikte" });
        engine.SpeechRecognized += Engine_SpeechRecognized;
        engine.SpeechHypothesized += (_, e) => HypothesisChanged?.Invoke(this, e.Result.Text);
        engine.AudioLevelUpdated += (_, e) => AudioLevelChanged?.Invoke(this, e.AudioLevel);
        engine.RecognizeCompleted += Engine_RecognizeCompleted;

        try
        {
            engine.RecognizeAsync(RecognizeMode.Multiple);
        }
        catch (InvalidOperationException ex)
        {
            engine.Dispose();
            throw new SpeechServiceException($"Konuşma tanıma başlatılamadı: {ex.Message}", ex);
        }

        _engine = engine;
        CurrentCulture = info.Culture;
        SetState(SttState.Listening);
    }

    /// <summary>Dinlemeyi durdurur; o an söylenen cümle tamamlanıp sonra durur.</summary>
    public void Stop()
    {
        if (_engine == null || State != SttState.Listening) return;
        SetState(SttState.Stopping);
        _engine.RecognizeAsyncStop();
    }

    private void Engine_SpeechRecognized(object? sender, SpeechRecognizedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(e.Result.Text)) return;
        SpeechRecognized?.Invoke(this, new RecognitionEntry(e.Result.Text, e.Result.Confidence));
    }

    private void Engine_RecognizeCompleted(object? sender, RecognizeCompletedEventArgs e)
    {
        if (e.Error != null)
            ErrorOccurred?.Invoke(this,
                new SpeechServiceException($"Konuşma tanıma hatası: {e.Error.Message}", e.Error));

        ReleaseEngine();
        AudioLevelChanged?.Invoke(this, 0);
        SetState(SttState.Idle);
    }

    private void ReleaseEngine()
    {
        if (_engine == null) return;
        _engine.SpeechRecognized -= Engine_SpeechRecognized;
        _engine.RecognizeCompleted -= Engine_RecognizeCompleted;
        _engine.Dispose();
        _engine = null;
    }

    private void SetState(SttState state)
    {
        if (State == state) return;
        State = state;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        try { _engine?.RecognizeAsyncCancel(); } catch (InvalidOperationException) { }
        ReleaseEngine();
    }
}
