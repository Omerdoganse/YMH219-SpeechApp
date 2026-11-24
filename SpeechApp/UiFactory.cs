namespace SpeechApp;

/// <summary>
/// Formdaki kontrolleri aynı görünümle oluşturmak için yardımcı metotlar.
/// </summary>
internal static class UiFactory
{
    public static readonly Color Accent = Color.FromArgb(45, 95, 170);
    public static readonly Color Danger = Color.FromArgb(190, 50, 50);

    public static Button Button(string text, EventHandler onClick, int width = 110, Color? backColor = null)
    {
        var button = new Button
        {
            Text = text,
            Width = width,
            Height = 34,
            Margin = new Padding(0, 0, 6, 6),
            FlatStyle = FlatStyle.Flat,
            BackColor = backColor ?? Color.White,
            ForeColor = backColor.HasValue ? Color.White : Color.FromArgb(40, 40, 40),
            Font = new Font("Segoe UI", 9.5f, backColor.HasValue ? FontStyle.Bold : FontStyle.Regular),
            Cursor = Cursors.Hand
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(180, 185, 195);
        button.Click += onClick;

        // Renkli butonlar devre dışıyken soluk görünsün (kullanıcı geri bildirimi)
        if (backColor.HasValue)
        {
            Color active = backColor.Value;
            button.EnabledChanged += (_, _) =>
                button.BackColor = button.Enabled ? active : Color.FromArgb(200, 203, 210);
        }
        return button;
    }

    public static Label Label(string text, bool bold = false) => new()
    {
        Text = text,
        AutoSize = true,
        Anchor = AnchorStyles.Left,
        Margin = new Padding(0, 6, 6, 6),
        Font = new Font("Segoe UI", 9.5f, bold ? FontStyle.Bold : FontStyle.Regular)
    };

    public static FlowLayoutPanel Row() => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        WrapContents = true,
        Margin = new Padding(0, 0, 0, 4)
    };

    public static RichTextBox TextArea(bool readOnly = false) => new()
    {
        Dock = DockStyle.Fill,
        Font = new Font("Segoe UI", 12f),
        BorderStyle = BorderStyle.FixedSingle,
        ReadOnly = readOnly,
        BackColor = Color.White,
        DetectUrls = false
    };

    public static GroupBox Group(string title) => new()
    {
        Text = title,
        Dock = DockStyle.Fill,
        Padding = new Padding(10),
        Font = new Font("Segoe UI", 11f, FontStyle.Bold),
        ForeColor = Accent
    };

    /// <summary>GroupBox içine yerleştirilecek, normal yazı tipli tablo düzeni.</summary>
    public static TableLayoutPanel Table(int rows)
    {
        var table = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = rows,
            Font = new Font("Segoe UI", 9.5f),
            ForeColor = Color.FromArgb(30, 30, 30)
        };
        table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }
}
