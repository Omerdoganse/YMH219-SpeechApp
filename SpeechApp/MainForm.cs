using SpeechApp.Models;
using SpeechApp.Services;

namespace SpeechApp;

/// <summary>
/// Ana pencere. Sol tarafta Metin → Ses, sağ tarafta Ses → Metin bölümü bulunur.
/// Bölümlere ait kodlar MainForm.Tts.cs ve MainForm.Stt.cs dosyalarındadır (partial class).
/// </summary>
public partial class MainForm : Form
{
    private readonly Label _lblActivity;
    private readonly ToolStripStatusLabel _statusMessage;
    private readonly TableLayoutPanel _mainLayout;

    public MainForm()
    {
        Text = "YMH219 - Konuşma ⇄ Metin Dönüştürücü";
        ClientSize = new Size(1180, 720);
        MinimumSize = new Size(1000, 640);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(244, 246, 250);
        Font = new Font("Segoe UI", 9.5f);

        // --- Üstte aktif işlem göstergesi ---
        _lblActivity = new Label
        {
            Dock = DockStyle.Top,
            Height = 46,
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 14f, FontStyle.Bold),
            ForeColor = Color.White
        };

        // --- Alt durum çubuğu ---
        var statusStrip = new StatusStrip();
        _statusMessage = new ToolStripStatusLabel("Hazır") { Spring = true, TextAlign = ContentAlignment.MiddleLeft };
        statusStrip.Items.Add(_statusMessage);

        // --- İki sütunlu ana düzen ---
        _mainLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Padding = new Padding(8)
        };
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        _mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        Controls.Add(_mainLayout);
        Controls.Add(_lblActivity);
        Controls.Add(statusStrip);

        _mainLayout.Controls.Add(BuildTtsPanel(), 0, 0);
        _mainLayout.Controls.Add(BuildSttPanel(), 1, 0);

        UpdateActivityIndicator();
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        InitializeTts();
        InitializeStt();
    }

    /// <summary>
    /// Hangi işlemin aktif olduğunu renkli bir şeritte gösterir.
    /// </summary>
    private void UpdateActivityIndicator()
    {
        var parts = new List<string>();
        Color color = Color.FromArgb(110, 115, 125);

        switch (_tts?.State)
        {
            case TtsState.Speaking:
                parts.Add("🔊 Metin sesli okunuyor");
                color = Color.FromArgb(40, 140, 80);
                break;
            case TtsState.Paused:
                parts.Add("⏸ Okuma duraklatıldı");
                color = Color.FromArgb(210, 130, 30);
                break;
        }

        switch (_stt?.State)
        {
            case SttState.Listening:
                parts.Add("🎤 Mikrofon dinleniyor");
                color = parts.Count > 1 ? Color.FromArgb(120, 60, 160) : Color.FromArgb(200, 50, 50);
                break;
            case SttState.Stopping:
                parts.Add("… Dinleme durduruluyor");
                color = parts.Count > 1 ? Color.FromArgb(120, 60, 160) : Color.FromArgb(160, 90, 90);
                break;
        }

        _lblActivity.Text = parts.Count == 0 ? "● Hazır - aktif işlem yok" : string.Join("   |   ", parts);
        _lblActivity.BackColor = color;
    }

    private void SetStatus(string message) => _statusMessage.Text = $"{DateTime.Now:HH:mm:ss}  {message}";

    private void ShowError(string message)
    {
        SetStatus("Hata: " + message);
        MessageBox.Show(this, message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    /// <summary>Servis olayları farklı bir iş parçacığından gelebilir; arayüzü güvenle günceller.</summary>
    private void RunOnUi(Action action)
    {
        if (IsDisposed || !IsHandleCreated) return;
        if (InvokeRequired) BeginInvoke(action);
        else action();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        _tts?.Dispose();
        _stt.Dispose();
        base.OnFormClosing(e);
    }
}
