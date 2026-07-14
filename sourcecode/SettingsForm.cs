namespace DEP3;

sealed class SettingsForm : Form
{
    private readonly AppData _data;
    private readonly ComboBox _deviceBox = new();
    private readonly CheckBox _startup = new();
    private readonly CheckBox _minimize = new();
    private readonly ComboBox _eqBox = new();
    private readonly List<(int Number, string Name)> _devices;

    public int SelectedDeviceNumber { get; private set; }
    public string SelectedDeviceName { get; private set; } = "";
    public bool LaunchOnStartup { get; private set; }
    public bool MinimizeOnClose { get; private set; }
    public string EqualizerPreset { get; private set; } = "Flat";

    public SettingsForm(AppData data)
    {
        _data = data;
        _devices = AudioPlayer.GetDevices().ToList();

        Text = "Settings";
        Icon = AppIcon.Get();
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        ClientSize = new Size(420, 300);
        MaximizeBox = false;
        MinimizeBox = false;
        BackColor = UiTheme.Bg;
        ForeColor = UiTheme.Fg;
        Font = UiTheme.Body;

        var y = 16;
        Controls.Add(new Label
        {
            Text = "Settings",
            Font = UiTheme.Title,
            ForeColor = UiTheme.Fg,
            AutoSize = true,
            Location = new Point(16, y)
        });

        y = 52;
        AddFieldLabel("Playback device", 16, y);
        UiTheme.Combo(_deviceBox);
        _deviceBox.SetBounds(16, y + 18, 388, 24);
        foreach (var d in _devices)
            _deviceBox.Items.Add(d.Name);
        _deviceBox.SelectedIndex = ResolveDeviceIndex();

        y = 108;
        _startup.Text = "Launch on startup";
        _startup.ForeColor = UiTheme.Fg;
        _startup.BackColor = UiTheme.Bg;
        _startup.AutoSize = true;
        _startup.Location = new Point(16, y);
        _startup.Checked = _data.LaunchOnStartup || StartupHelper.IsLaunchOnStartup();

        y = 136;
        _minimize.Text = "Minimise on close";
        _minimize.ForeColor = UiTheme.Fg;
        _minimize.BackColor = UiTheme.Bg;
        _minimize.AutoSize = true;
        _minimize.Location = new Point(16, y);
        _minimize.Checked = _data.MinimizeOnClose;

        y = 172;
        AddFieldLabel("Equalizer preset", 16, y);
        UiTheme.Combo(_eqBox);
        _eqBox.SetBounds(16, y + 18, 388, 24);
        _eqBox.Items.AddRange(EqPresets.Names);
        var eqIndex = Array.IndexOf(EqPresets.Names, _data.EqualizerPreset);
        _eqBox.SelectedIndex = eqIndex >= 0 ? eqIndex : 0;

        var save = UiTheme.Button("Save", 80);
        save.Location = new Point(148, 256);
        save.Click += (_, _) => { Commit(); DialogResult = DialogResult.OK; Close(); };

        var exitApp = UiTheme.Button("Exit App", 80);
        exitApp.Location = new Point(236, 256);
        exitApp.Click += (_, _) => { Commit(); DialogResult = DialogResult.Abort; Close(); };

        var cancel = UiTheme.Button("Cancel", 80);
        cancel.Location = new Point(324, 256);
        cancel.Click += (_, _) => { DialogResult = DialogResult.Cancel; Close(); };

        Controls.AddRange([
            _deviceBox, _startup, _minimize, _eqBox,
            save, exitApp, cancel
        ]);
        AcceptButton = save;
        CancelButton = cancel;
    }

    private void AddFieldLabel(string text, int x, int y)
    {
        Controls.Add(new Label
        {
            Text = text,
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(x, y)
        });
    }

    private int ResolveDeviceIndex()
    {
        if (!string.IsNullOrWhiteSpace(_data.PlaybackDeviceName))
        {
            var byName = _devices.FindIndex(d =>
                string.Equals(d.Name, _data.PlaybackDeviceName, StringComparison.OrdinalIgnoreCase));
            if (byName >= 0)
                return byName;
        }

        var byNumber = _devices.FindIndex(d => d.Number == _data.PlaybackDeviceNumber);
        return byNumber >= 0 ? byNumber : 0;
    }

    private void Commit()
    {
        var device = _devices[Math.Max(0, _deviceBox.SelectedIndex)];
        SelectedDeviceNumber = device.Number;
        SelectedDeviceName = device.Name;
        LaunchOnStartup = _startup.Checked;
        MinimizeOnClose = _minimize.Checked;
        EqualizerPreset = _eqBox.SelectedItem?.ToString() ?? "Flat";
    }
}
