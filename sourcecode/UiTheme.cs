using System.Runtime.InteropServices;

namespace DEP3;

static class UiTheme
{
    public static readonly Color Bg = Color.FromArgb(22, 22, 22);
    public static readonly Color Panel = Color.FromArgb(32, 32, 32);
    public static readonly Color Input = Color.FromArgb(38, 38, 38);
    public static readonly Color Fg = Color.FromArgb(230, 230, 230);
    public static readonly Color Muted = Color.FromArgb(140, 140, 140);
    public static readonly Color Accent = Color.FromArgb(46, 160, 120);

    public static readonly Font Body = new("Segoe UI", 9f);
    public static readonly Font Title = new("Segoe UI Semibold", 13f);
    public static readonly Font Brand = new("Segoe UI Semibold", 15f);

    private const int EmSetCueBanner = 0x1501;

    [DllImport("user32.dll", CharSet = CharSet.Unicode)]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, string? lParam);

    public static Button Button(string text, int width)
    {
        var btn = new Button
        {
            Text = text,
            Width = width,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Panel,
            ForeColor = Fg,
            Font = Body,
            Cursor = Cursors.Hand,
            TabStop = false
        };
        btn.FlatAppearance.BorderColor = Color.FromArgb(55, 55, 55);
        btn.FlatAppearance.BorderSize = 1;
        return btn;
    }

    public static void Combo(ComboBox box)
    {
        box.DropDownStyle = ComboBoxStyle.DropDownList;
        box.BackColor = Input;
        box.ForeColor = Fg;
        box.FlatStyle = FlatStyle.Flat;
        box.Font = Body;
    }

    public static void TextBox(TextBox box)
    {
        box.BackColor = Input;
        box.ForeColor = Fg;
        box.BorderStyle = BorderStyle.FixedSingle;
        box.Font = Body;
    }

    public static TextBox SearchBox()
    {
        var box = new TextBox
        {
            Width = 190,
            Height = 26,
            BackColor = Input,
            ForeColor = Fg,
            BorderStyle = BorderStyle.FixedSingle,
            Font = Body
        };
        box.HandleCreated += (_, _) => SendMessage(box.Handle, EmSetCueBanner, 1, "Search songs...");
        return box;
    }

    public static void TrackBar(TrackBar bar)
    {
        bar.TickStyle = TickStyle.None;
        bar.BackColor = Bg;
    }
}
