using SpeechApp.Models;
using SpeechApp.Services;

namespace SpeechApp;

/// <summary>
/// Metin → Ses (Text-to-Speech) bölümü.
/// </summary>
public partial class MainForm
{
    private TextToSpeechService? _tts;

    private RichTextBox _txtTts = null!;
    private ComboBox _cmbVoice = null!;
    private TrackBar _trkRate = null!;
    private TrackBar _trkVolume = null!;
    private Label _lblRate = null!;
    private Label _lblVolume = null!;
    private Label _lblTtsCount = null!;
    private Button _btnSpeak = null!;
    private Button _btnPause = null!;
    private Button _btnStopSpeak = null!;
    private Button _btnSaveWav = null!;

    private Control BuildTtsPanel()
    {
        var group = UiFactory.Group("Metin → Ses (Text-to-Speech)");
        var table = UiFactory.Table(5);
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // ses ayarları
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // hız / seviye
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // butonlar
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); // metin
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));   // sayaç

        // Ses seçimi
        var voiceRow = UiFactory.Row();
        _cmbVoice = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 380 };
        _cmbVoice.SelectedIndexChanged += CmbVoice_SelectedIndexChanged;
        voiceRow.Controls.AddRange([UiFactory.Label("Ses:", true), _cmbVoice]);

        // Hız ve ses seviyesi
        var sliderRow = UiFactory.Row();
        _trkRate = new TrackBar
        {
            Minimum = TextToSpeechService.MinRate, Maximum = TextToSpeechService.MaxRate,
            Value = 0, TickFrequency = 2, Width = 160, AutoSize = false, Height = 32
        };
        _lblRate = UiFactory.Label("0");
        _trkRate.ValueChanged += (_, _) =>
        {
            if (_tts != null) _tts.Rate = _trkRate.Value;
            _lblRate.Text = _trkRate.Value.ToString("+0;-0;0");
        };

        _trkVolume = new TrackBar
        {
            Minimum = 0, Maximum = 100, Value = 100, TickFrequency = 10, Width = 160, AutoSize = false, Height = 32
        };
        _lblVolume = UiFactory.Label("%100");
        _trkVolume.ValueChanged += (_, _) =>
        {
            if (_tts != null) _tts.Volume = _trkVolume.Value;
            _lblVolume.Text = $"%{_trkVolume.Value}";
        };
        sliderRow.Controls.AddRange([
            UiFactory.Label("Hız:", true), _trkRate, _lblRate,
            UiFactory.Label("   Ses seviyesi:", true), _trkVolume, _lblVolume
        ]);

        // Butonlar
        var buttonRow = UiFactory.Row();
        _btnSpeak = UiFactory.Button("▶ Okut", (_, _) => SpeakText(), 100, UiFactory.Accent);
        _btnPause = UiFactory.Button("⏸ Duraklat", (_, _) => TogglePause(), 110);
        _btnStopSpeak = UiFactory.Button("■ Durdur", (_, _) => _tts?.Stop(), 100);
        var btnClear = UiFactory.Button("Temizle", (_, _) => ClearTtsText(), 90);
        var btnSaveTxt = UiFactory.Button(".txt Kaydet", (_, _) => SaveTtsText(), 100);
        _btnSaveWav = UiFactory.Button(".wav Kaydet", (_, _) => SaveTtsWave(), 100);
        buttonRow.Controls.AddRange([_btnSpeak, _btnPause, _btnStopSpeak, btnClear, btnSaveTxt, _btnSaveWav]);

        _txtTts = UiFactory.TextArea();
        _txtTts.Text = "Merhaba! Bu uygulama Nesne Tabanlı Programlama dersi için hazırlandı. " +
                       "Buraya yazdığınız metni Okut butonu ile sesli dinleyebilirsiniz.";
        _txtTts.TextChanged += (_, _) => UpdateTtsCounter();

        _lblTtsCount = UiFactory.Label("");

        table.Controls.Add(voiceRow, 0, 0);
        table.Controls.Add(sliderRow, 0, 1);
        table.Controls.Add(buttonRow, 0, 2);
        table.Controls.Add(_txtTts, 0, 3);
        table.Controls.Add(_lblTtsCount, 0, 4);
        group.Controls.Add(table);

        UpdateTtsCounter();
        return group;
    }

    /// <summary>Sentezleyiciyi oluşturur ve kurulu sesleri listeler.</summary>
    private void InitializeTts()
    {
        try
        {
            _tts = new TextToSpeechService();
        }
        catch (SpeechServiceException ex)
        {
            ShowError(ex.Message);
            SetTtsControlsEnabled(false);
            return;
        }

        _tts.StateChanged += (_, _) => RunOnUi(UpdateTtsButtons);
        _tts.WordSpoken += (_, e) => RunOnUi(() => HighlightWord(e.Position, e.Length));
        _tts.ErrorOccurred += (_, ex) => RunOnUi(() => ShowError(ex.Message));

        var voices = _tts.GetVoices();
        _cmbVoice.Items.Clear();
        foreach (var voice in voices)
            _cmbVoice.Items.Add(voice);

        if (voices.Count == 0)
        {
            ShowError("Bilgisayarda kurulu bir metin okuma sesi bulunamadı.");
            SetTtsControlsEnabled(false);
            return;
        }

        _cmbVoice.SelectedIndex = Math.Max(0, voices.ToList().FindIndex(v => v.Id == _tts.CurrentVoiceName));
        UpdateTtsButtons();
        SetStatus($"{voices.Count} ses bulundu. Metin okumaya hazır.");
    }

    private void CmbVoice_SelectedIndexChanged(object? sender, EventArgs e)
    {
        if (_tts == null || _cmbVoice.SelectedItem is not VoiceOption voice) return;
        try
        {
            _tts.SelectVoice(voice.Id);
            SetStatus($"Ses seçildi: {voice.Id}");
        }
        catch (SpeechServiceException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void SpeakText()
    {
        if (_tts == null) return;
        try
        {
            ClearHighlight();
            _tts.Rate = _trkRate.Value;
            _tts.Volume = _trkVolume.Value;
            _tts.Speak(_txtTts.Text);
            SetStatus("Okuma başladı.");
        }
        catch (SpeechServiceException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void TogglePause()
    {
        if (_tts == null) return;
        if (_tts.State == TtsState.Speaking) { _tts.Pause(); SetStatus("Okuma duraklatıldı."); }
        else if (_tts.State == TtsState.Paused) { _tts.Resume(); SetStatus("Okumaya devam ediliyor."); }
    }

    private void ClearTtsText()
    {
        _tts?.Stop();
        _txtTts.Clear();
        _txtTts.Focus();
        SetStatus("Metin temizlendi.");
    }

    private void SaveTtsText()
    {
        if (string.IsNullOrWhiteSpace(_txtTts.Text))
        {
            ShowError("Kaydedilecek metin yok.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Metin dosyası (*.txt)|*.txt",
            FileName = $"metin_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            TextFileService.SaveText(dialog.FileName, _txtTts.Text);
            SetStatus($"Metin kaydedildi: {dialog.FileName}");
        }
        catch (SpeechServiceException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void SaveTtsWave()
    {
        if (_tts == null) return;
        using var dialog = new SaveFileDialog
        {
            Filter = "WAV ses dosyası (*.wav)|*.wav",
            FileName = $"ses_{DateTime.Now:yyyyMMdd_HHmmss}.wav"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            Cursor = Cursors.WaitCursor;
            _tts.Rate = _trkRate.Value;
            _tts.Volume = _trkVolume.Value;
            _tts.SaveToWave(dialog.FileName, _txtTts.Text);
            SetStatus($"Ses dosyası oluşturuldu: {dialog.FileName}");
        }
        catch (SpeechServiceException ex)
        {
            ShowError(ex.Message);
        }
        finally
        {
            Cursor = Cursors.Default;
        }
    }

    /// <summary>Okunan kelimeyi metin kutusunda sarı renkle vurgular.</summary>
    private void HighlightWord(int position, int length)
    {
        if (position < 0 || position + length > _txtTts.TextLength) return;
        ClearHighlight();
        _txtTts.Select(position, length);
        _txtTts.SelectionBackColor = Color.Gold;
        _txtTts.Select(position, 0);
        _txtTts.ScrollToCaret();
    }

    private void ClearHighlight()
    {
        int start = _txtTts.SelectionStart;
        _txtTts.SelectAll();
        _txtTts.SelectionBackColor = _txtTts.BackColor;
        _txtTts.Select(start, 0);
    }

    private void UpdateTtsButtons()
    {
        var state = _tts?.State ?? TtsState.Idle;
        bool busy = state != TtsState.Idle;

        _btnPause.Enabled = busy;
        _btnPause.Text = state == TtsState.Paused ? "▶ Devam" : "⏸ Duraklat";
        _btnStopSpeak.Enabled = busy;
        _btnSaveWav.Enabled = !busy;
        _cmbVoice.Enabled = !busy;
        _txtTts.ReadOnly = busy; // Okuma sırasında metin değişirse vurgulama kayar

        if (!busy)
        {
            ClearHighlight();
            SetStatus("Okuma tamamlandı / durduruldu.");
        }
        UpdateActivityIndicator();
    }

    private void SetTtsControlsEnabled(bool enabled)
    {
        foreach (var c in new Control[] { _btnSpeak, _btnPause, _btnStopSpeak, _btnSaveWav, _cmbVoice, _trkRate, _trkVolume })
            c.Enabled = enabled;
    }

    private void UpdateTtsCounter()
    {
        string text = _txtTts.Text;
        int words = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;
        _lblTtsCount.Text = $"{text.Length} karakter, {words} kelime   •   İpucu: okunan kelime sarı ile vurgulanır.";
    }
}
