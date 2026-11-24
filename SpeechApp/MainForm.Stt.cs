using SpeechApp.Models;
using SpeechApp.Services;

namespace SpeechApp;

/// <summary>
/// Ses → Metin (Speech-to-Text) bölümü.
/// </summary>
public partial class MainForm
{
    private readonly SpeechToTextService _stt = new();
    private readonly List<RecognitionEntry> _recognitions = [];

    private ComboBox _cmbLanguage = null!;
    private Button _btnListen = null!;
    private Button _btnStopListen = null!;
    private ProgressBar _prgMicLevel = null!;
    private Label _lblHypothesis = null!;
    private RichTextBox _txtStt = null!;
    private ListView _lvHistory = null!;

    private Control BuildSttPanel()
    {
        var group = UiFactory.Group("Ses → Metin (Speech-to-Text)");
        var table = UiFactory.Table(6);
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // dil
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // butonlar
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // mikrofon seviyesi
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 45));  // metin
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));     // geçmiş başlığı
        table.RowStyles.Add(new RowStyle(SizeType.Percent, 55));  // geçmiş listesi

        var languageRow = UiFactory.Row();
        _cmbLanguage = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 300 };
        languageRow.Controls.AddRange([UiFactory.Label("Tanıma dili:", true), _cmbLanguage]);

        var buttonRow = UiFactory.Row();
        _btnListen = UiFactory.Button("🎤 Dinlemeyi Başlat", (_, _) => StartListening(), 150, UiFactory.Danger);
        _btnStopListen = UiFactory.Button("■ Durdur", (_, _) => StopListening(), 90);
        var btnClear = UiFactory.Button("Temizle", (_, _) => ClearSttText(), 85);
        var btnSave = UiFactory.Button(".txt Kaydet", (_, _) => SaveSttLog(), 95);
        var btnToTts = UiFactory.Button("← Metni Okut", (_, _) => SendToTts(), 110);
        buttonRow.Controls.AddRange([_btnListen, _btnStopListen, btnClear, btnSave, btnToTts]);

        var levelRow = UiFactory.Row();
        _prgMicLevel = new ProgressBar { Width = 160, Height = 18, Maximum = 100, Margin = new Padding(0, 8, 10, 6) };
        _lblHypothesis = UiFactory.Label("Ara sonuç: -");
        _lblHypothesis.ForeColor = Color.DimGray;
        _lblHypothesis.Font = new Font("Segoe UI", 9.5f, FontStyle.Italic);
        levelRow.Controls.AddRange([UiFactory.Label("Mikrofon:", true), _prgMicLevel, _lblHypothesis]);

        _txtStt = UiFactory.TextArea();

        _lvHistory = new ListView
        {
            Dock = DockStyle.Fill,
            View = View.Details,
            FullRowSelect = true,
            GridLines = true,
            HeaderStyle = ColumnHeaderStyle.Nonclickable
        };
        _lvHistory.Columns.Add("Saat", 80);
        _lvHistory.Columns.Add("Tanınan metin", 330);
        _lvHistory.Columns.Add("Güven", 70, HorizontalAlignment.Right);

        table.Controls.Add(languageRow, 0, 0);
        table.Controls.Add(buttonRow, 0, 1);
        table.Controls.Add(levelRow, 0, 2);
        table.Controls.Add(_txtStt, 0, 3);
        table.Controls.Add(UiFactory.Label("Tanıma geçmişi (zaman damgalı):", true), 0, 4);
        table.Controls.Add(_lvHistory, 0, 5);
        group.Controls.Add(table);

        // Servis olayları
        _stt.StateChanged += (_, _) => RunOnUi(UpdateSttButtons);
        _stt.SpeechRecognized += (_, entry) => RunOnUi(() => AddRecognition(entry));
        _stt.HypothesisChanged += (_, text) => RunOnUi(() => _lblHypothesis.Text = $"Ara sonuç: {text}…");
        _stt.AudioLevelChanged += (_, level) => RunOnUi(() => _prgMicLevel.Value = Math.Clamp(level, 0, 100));
        _stt.ErrorOccurred += (_, ex) => RunOnUi(() => ShowError(ex.Message));

        return group;
    }

    /// <summary>Kurulu konuşma tanıyıcılarını listeler.</summary>
    private void InitializeStt()
    {
        var recognizers = SpeechToTextService.GetRecognizers();
        _cmbLanguage.Items.Clear();
        foreach (var r in recognizers)
            _cmbLanguage.Items.Add(r);

        if (recognizers.Count == 0)
        {
            _btnListen.Enabled = false;
            _cmbLanguage.Enabled = false;
            ShowError("Bu bilgisayarda konuşma tanıma altyapısı bulunamadı. Windows Ayarlar > Saat ve Dil > " +
                      "Konuşma bölümünden bir konuşma dil paketi yükleyin.");
            return;
        }

        // Türkçe tanıyıcı varsa onu, yoksa ilkini seç
        int trIndex = recognizers.ToList().FindIndex(r => r.Culture.Name == "tr-TR");
        _cmbLanguage.SelectedIndex = trIndex >= 0 ? trIndex : 0;
        UpdateSttButtons();
    }

    private void StartListening()
    {
        if (_cmbLanguage.SelectedItem is not VoiceOption recognizer)
        {
            ShowError("Lütfen bir tanıma dili seçin.");
            return;
        }

        try
        {
            _stt.Start(recognizer.Id);
            SetStatus($"Mikrofon dinleniyor ({recognizer.DisplayName}). Konuşmaya başlayabilirsiniz.");
        }
        catch (SpeechServiceException ex)
        {
            ShowError(ex.Message);
        }
    }

    private void StopListening()
    {
        _stt.Stop();
        SetStatus("Dinleme durduruluyor...");
    }

    /// <summary>Tanınan cümleyi metin alanına ve geçmiş listesine ekler.</summary>
    private void AddRecognition(RecognitionEntry entry)
    {
        _recognitions.Add(entry);

        if (_txtStt.TextLength > 0 && !_txtStt.Text.EndsWith(' '))
            _txtStt.AppendText(" ");
        _txtStt.AppendText(entry.Text);
        _txtStt.ScrollToCaret();

        var item = new ListViewItem([entry.Timestamp.ToString("HH:mm:ss"), entry.Text, entry.ConfidencePercent]);
        if (entry.Confidence < 0.5f)
            item.ForeColor = Color.IndianRed; // Düşük güvenli sonuçları belirgin yap
        _lvHistory.Items.Add(item);
        item.EnsureVisible();

        _lblHypothesis.Text = "Ara sonuç: -";
        SetStatus($"Tanındı ({entry.ConfidencePercent}): {entry.Text}");
    }

    private void ClearSttText()
    {
        _txtStt.Clear();
        _lvHistory.Items.Clear();
        _recognitions.Clear();
        _lblHypothesis.Text = "Ara sonuç: -";
        SetStatus("Tanıma sonuçları temizlendi.");
    }

    private void SaveSttLog()
    {
        if (_recognitions.Count == 0)
        {
            ShowError("Kaydedilecek tanıma sonucu yok.");
            return;
        }

        using var dialog = new SaveFileDialog
        {
            Filter = "Metin dosyası (*.txt)|*.txt",
            FileName = $"konusma_{DateTime.Now:yyyyMMdd_HHmmss}.txt"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            TextFileService.SaveRecognitionLog(dialog.FileName, _recognitions);
            SetStatus($"Tanıma kayıtları kaydedildi: {dialog.FileName}");
        }
        catch (SpeechServiceException ex)
        {
            ShowError(ex.Message);
        }
    }

    /// <summary>Tanınan metni Metin → Ses bölümüne aktarıp okutur.</summary>
    private void SendToTts()
    {
        if (string.IsNullOrWhiteSpace(_txtStt.Text))
        {
            ShowError("Aktarılacak tanınmış metin yok.");
            return;
        }
        _tts?.Stop();
        _txtTts.Text = _txtStt.Text;
        SpeakText();
    }

    private void UpdateSttButtons()
    {
        var state = _stt.State;
        _btnListen.Enabled = state == SttState.Idle && _cmbLanguage.Items.Count > 0;
        _btnStopListen.Enabled = state == SttState.Listening;
        _cmbLanguage.Enabled = state == SttState.Idle;
        if (state == SttState.Idle)
        {
            _prgMicLevel.Value = 0;
            _lblHypothesis.Text = "Ara sonuç: -";
        }
        UpdateActivityIndicator();
    }
}
