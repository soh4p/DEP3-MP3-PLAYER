namespace DEP3;

public sealed class MainForm : Form
{
    private readonly AudioPlayer _player = new();
    private readonly AppData _data;
    private readonly List<TrackInfo> _library = [];
    private readonly List<TrackInfo> _queue = [];
    private readonly Random _rng = new();

    private int _index = -1;
    private bool _seeking;
    private bool _suppressList;
    private bool _forceClose;
    private Image? _coverImage;

    private readonly BackgroundPanel _root = new();
    private readonly Panel _header = new();
    private readonly Panel _playerPanel = new();
    private readonly Panel _tools = new();
    private readonly PictureBox _cover = new();
    private readonly Label _title = new();
    private readonly Label _artist = new();
    private readonly Label _timeLeft = new();
    private readonly Label _timeRight = new();
    private readonly Label _volPct = new();
    private readonly TrackBar _volume = new();
    private readonly Button _btnPrev = UiTheme.Button("◀", 40);
    private readonly Button _btnPlayStop = UiTheme.Button("Play", 64);
    private readonly Button _btnNext = UiTheme.Button("▶", 40);
    private readonly Button _btnFolder = UiTheme.Button("Open Folder", 100);
    private readonly Button _btnSettings = UiTheme.Button("Settings", 80);
    private readonly Button _btnNewPlaylist = UiTheme.Button("New Playlist", 100);
    private readonly Button _btnAddToPlaylist = UiTheme.Button("Add", 72);
    private readonly Button _btnRemoveFromPlaylist = UiTheme.Button("Remove", 72);
    private readonly Button _btnDeletePlaylist = UiTheme.Button("Delete", 72);
    private readonly ComboBox _shuffle = new();
    private readonly CheckBox _autoPlay = new();
    private readonly ComboBox _playlistBox = new();
    private readonly ComboBox _artistFilter = new();
    private readonly TextBox _searchBox = UiTheme.SearchBox();
    private readonly SeekLineControl _seekLine = new();
    private readonly ListBox _songList = new();
    private readonly Label _playtimeStat = new();
    private readonly Panel _footer = new();
    private readonly LinkLabel _credit = new();
    private readonly NotifyIcon _tray = new();
    private readonly System.Windows.Forms.Timer _uiTimer = new();

    private DateTime _lastListenSample = DateTime.UtcNow;
    private double _listenSinceSave;

    private const int CoverSize = 136;
    private const int LeftPad = 14;
    private const int InfoLeft = LeftPad + CoverSize + 14;

    public MainForm()
    {
        _data = DataStore.Load();
        BuildUi();
        SetupTray();
        WireEvents();
        ApplySavedSettings(initial: true);

        _volume.Value = (int)Math.Round(Math.Clamp(_data.Volume, 0f, 1f) * 100);
        UpdateVolumeLabel();
        SelectShuffleMode(_data.ShuffleMode);
        _autoPlay.Checked = _data.AutoPlay;

        if (!string.IsNullOrWhiteSpace(_data.MusicFolder) && Directory.Exists(_data.MusicFolder))
            LoadFolder(_data.MusicFolder);
        else
            RefreshPlaylistCombo();

        _uiTimer.Interval = 400;
        _uiTimer.Tick += (_, _) => UpdateSeekUi();
        _uiTimer.Start();
        UpdatePlaytimeDisplay();
    }

    private void BuildUi()
    {
        Text = "DEP3";
        Icon = AppIcon.Get();
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(640, 480);
        Size = new Size(Math.Max(640, _data.WindowWidth), Math.Max(480, _data.WindowHeight));
        BackColor = UiTheme.Bg;
        ForeColor = UiTheme.Fg;
        Font = UiTheme.Body;

        _root.Dock = DockStyle.Fill;
        Controls.Add(_root);

        // Header
        _header.Dock = DockStyle.Top;
        _header.Height = 42;
        _header.BackColor = UiTheme.Panel;
        _header.Padding = new Padding(LeftPad, 0, LeftPad, 0);

        var brand = new Label
        {
            Text = "DEP3",
            Font = UiTheme.Brand,
            ForeColor = UiTheme.Fg,
            AutoSize = true,
            Location = new Point(LeftPad, 10)
        };

        _btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _btnSettings.Location = new Point(_header.Width - 94, 7);

        _playtimeStat.ForeColor = UiTheme.Muted;
        _playtimeStat.Font = UiTheme.Body;
        _playtimeStat.AutoSize = true;
        _playtimeStat.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        _playtimeStat.TextAlign = ContentAlignment.MiddleRight;

        _header.Controls.Add(brand);
        _header.Controls.Add(_playtimeStat);
        _header.Controls.Add(_btnSettings);
        _header.Resize += (_, _) => LayoutHeader();
        LayoutHeader();

        // Player
        _playerPanel.Dock = DockStyle.Top;
        _playerPanel.Height = 212;
        _playerPanel.BackColor = UiTheme.Bg;
        _playerPanel.Padding = new Padding(LeftPad, 10, LeftPad, 8);

        _cover.Size = new Size(CoverSize, CoverSize);
        _cover.Location = new Point(LeftPad, 10);
        _cover.SizeMode = PictureBoxSizeMode.Zoom;
        _cover.BackColor = UiTheme.Panel;
        _cover.BorderStyle = BorderStyle.None;

        _title.Text = "No track selected";
        _title.Font = UiTheme.Title;
        _title.ForeColor = UiTheme.Fg;
        _title.AutoEllipsis = true;
        _title.Location = new Point(InfoLeft, 10);
        _title.Height = 24;
        _title.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _artist.Text = "—";
        _artist.ForeColor = UiTheme.Muted;
        _artist.AutoEllipsis = true;
        _artist.Location = new Point(InfoLeft, 36);
        _artist.Height = 20;
        _artist.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _seekLine.Location = new Point(InfoLeft, 58);
        _seekLine.Height = 28;
        _seekLine.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        _seekLine.SeekRequested += (_, ratio) =>
        {
            _seeking = true;
            _player.Seek(ratio);
            _seeking = false;
            UpdateSeekUi();
        };

        _timeLeft.Text = "0:00";
        _timeLeft.ForeColor = UiTheme.Muted;
        _timeLeft.AutoSize = true;
        _timeLeft.Location = new Point(InfoLeft, 106);

        _timeRight.Text = "/ 0:00";
        _timeRight.ForeColor = UiTheme.Muted;
        _timeRight.AutoSize = true;
        _timeRight.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _timeRight.Location = new Point(InfoLeft + 36, 106);

        _btnPrev.Location = new Point(InfoLeft, 128);
        _btnPlayStop.Location = new Point(InfoLeft + 48, 128);
        _btnNext.Location = new Point(InfoLeft + 120, 128);

        var volLabel = new Label
        {
            Text = "Vol",
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(InfoLeft, 182)
        };

        UiTheme.TrackBar(_volume);
        _volume.Minimum = 0;
        _volume.Maximum = 100;
        _volume.Location = new Point(InfoLeft + 30, 176);
        _volume.Width = 120;
        _volume.Height = 28;

        _volPct.Text = "70%";
        _volPct.ForeColor = UiTheme.Fg;
        _volPct.AutoSize = true;
        _volPct.Location = new Point(InfoLeft + 156, 182);

        _autoPlay.Text = "AutoPlay";
        _autoPlay.ForeColor = UiTheme.Fg;
        _autoPlay.BackColor = UiTheme.Bg;
        _autoPlay.AutoSize = true;
        _autoPlay.Location = new Point(InfoLeft + 210, 158);

        var shuffleLabel = new Label
        {
            Text = "Shuffle",
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(InfoLeft + 210, 182)
        };

        UiTheme.Combo(_shuffle);
        _shuffle.Items.AddRange(["Off", "Random"]);
        _shuffle.Location = new Point(InfoLeft + 268, 178);
        _shuffle.Height = 24;
        _shuffle.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _playerPanel.Controls.AddRange([
            _cover, _title, _artist, _seekLine, _timeLeft, _timeRight,
            _btnPrev, _btnPlayStop, _btnNext, volLabel, _volume, _volPct,
            _autoPlay, shuffleLabel, _shuffle
        ]);
        _playerPanel.Resize += (_, _) => LayoutPlayer();

        // Tools
        _tools.Dock = DockStyle.Top;
        _tools.Height = 96;
        _tools.BackColor = UiTheme.Bg;
        _tools.Padding = new Padding(LeftPad, 6, LeftPad, 4);

        var plLabel = new Label
        {
            Text = "Playlist",
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(LeftPad + 108, 10)
        };

        UiTheme.Combo(_playlistBox);
        _playlistBox.Location = new Point(LeftPad + 168, 6);
        _playlistBox.Height = 24;
        _playlistBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;

        _btnFolder.Location = new Point(LeftPad, 6);
        _btnNewPlaylist.Location = new Point(LeftPad, 36);
        _btnAddToPlaylist.Location = new Point(LeftPad + 108, 36);
        _btnRemoveFromPlaylist.Location = new Point(LeftPad + 188, 36);
        _btnDeletePlaylist.Location = new Point(LeftPad + 268, 36);

        _searchBox.Location = new Point(LeftPad, 66);
        _searchBox.Anchor = AnchorStyles.Top | AnchorStyles.Left;

        var artistLabel = new Label
        {
            Text = "Artist",
            ForeColor = UiTheme.Muted,
            AutoSize = true,
            Location = new Point(LeftPad + 220, 70)
        };

        UiTheme.Combo(_artistFilter);
        _artistFilter.Location = new Point(LeftPad + 268, 66);
        _artistFilter.Width = 160;
        _artistFilter.Height = 24;
        _artistFilter.Anchor = AnchorStyles.Top | AnchorStyles.Left;
        _artistFilter.Items.Add("All artists");

        _tools.Controls.AddRange([
            _btnFolder, plLabel, _playlistBox,
            _btnNewPlaylist, _btnAddToPlaylist, _btnRemoveFromPlaylist, _btnDeletePlaylist,
            _searchBox, artistLabel, _artistFilter
        ]);
        _tools.Resize += (_, _) => LayoutTools();

        // Song list
        _songList.Dock = DockStyle.Fill;
        _songList.BackColor = UiTheme.Panel;
        _songList.ForeColor = UiTheme.Fg;
        _songList.BorderStyle = BorderStyle.None;
        _songList.Font = new Font("Segoe UI", 9.5f);
        _songList.IntegralHeight = false;

        _footer.Dock = DockStyle.Bottom;
        _footer.Height = 22;
        _footer.BackColor = UiTheme.Bg;

        const string creditText = "Made by Gim4 · My other projects";
        const string githubUrl = "https://github.com/soh4p";
        _credit.Text = creditText;
        _credit.AutoSize = true;
        _credit.Font = new Font("Segoe UI", 7.5f);
        _credit.BackColor = UiTheme.Bg;
        _credit.LinkColor = Color.Black;
        _credit.ActiveLinkColor = UiTheme.Accent;
        _credit.VisitedLinkColor = Color.Black;
        _credit.Links.Add(0, creditText.Length, githubUrl);
        _credit.LinkClicked += (_, _) =>
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = githubUrl,
                    UseShellExecute = true
                });
            }
            catch
            {
                // ignore launch failures
            }
        };
        _footer.Controls.Add(_credit);
        _footer.Resize += (_, _) => LayoutFooter();

        _root.Controls.Add(_songList);
        _root.Controls.Add(_footer);
        _root.Controls.Add(_tools);
        _root.Controls.Add(_playerPanel);
        _root.Controls.Add(_header);

        LayoutPlayer();
        LayoutTools();
        LayoutFooter();
    }

    private void SetupTray()
    {
        var icon = AppIcon.Get();
        _tray.Icon = icon;
        _tray.Text = "DEP3";
        _tray.Visible = false;

        var menu = new ContextMenuStrip();
        var open = new ToolStripMenuItem("Open DEP3");
        open.Click += (_, _) => RestoreFromTray();
        var exit = new ToolStripMenuItem("Exit");
        exit.Click += (_, _) =>
        {
            _forceClose = true;
            Close();
        };
        menu.Items.Add(open);
        menu.Items.Add(exit);
        _tray.ContextMenuStrip = menu;
        _tray.DoubleClick += (_, _) => RestoreFromTray();
    }

    private void RestoreFromTray()
    {
        Show();
        WindowState = FormWindowState.Normal;
        Activate();
        UpdateTrayVisibility();
    }

    private void UpdateTrayVisibility() =>
        _tray.Visible = WindowState == FormWindowState.Minimized;

    private void LayoutHeader()
    {
        _btnSettings.Left = _header.ClientSize.Width - _btnSettings.Width - LeftPad;
        _btnSettings.Top = (_header.ClientSize.Height - _btnSettings.Height) / 2;
        UpdatePlaytimeDisplay();
        _playtimeStat.Top = (_header.ClientSize.Height - _playtimeStat.Height) / 2;
        _playtimeStat.Left = _btnSettings.Left - _playtimeStat.Width - 14;
    }

    private void LayoutPlayer()
    {
        var right = _playerPanel.ClientSize.Width - LeftPad;
        var infoW = Math.Max(200, right - InfoLeft);

        _title.Width = infoW;
        _artist.Width = infoW;
        _seekLine.Width = infoW;
        _timeRight.Left = InfoLeft + _timeLeft.Width + 4;
        _shuffle.Width = Math.Max(100, right - (InfoLeft + 268));
    }

    private void LayoutTools()
    {
        var right = _tools.ClientSize.Width - LeftPad;
        _playlistBox.Width = Math.Max(120, right - (LeftPad + 168));
    }

    private void LayoutFooter()
    {
        _credit.Top = (_footer.ClientSize.Height - _credit.Height) / 2;
        _credit.Left = _footer.ClientSize.Width - _credit.Width - LeftPad;
    }

    private void WireEvents()
    {
        _btnFolder.Click += (_, _) => ChooseFolder();
        _btnSettings.Click += (_, _) => OpenSettings();
        _btnPrev.Click += (_, _) => PlayRelative(-1);
        _btnNext.Click += (_, _) => PlayRelative(1);
        _btnPlayStop.Click += (_, _) => TogglePlayStop();

        _volume.ValueChanged += (_, _) =>
        {
            _data.Volume = _volume.Value / 100f;
            _player.SetVolume(_data.Volume);
            UpdateVolumeLabel();
            Persist();
        };

        _shuffle.SelectedIndexChanged += (_, _) =>
        {
            _data.ShuffleMode = _shuffle.SelectedItem?.ToString() ?? "Off";
            RebuildQueue(keepCurrent: true);
            Persist();
        };

        _autoPlay.CheckedChanged += (_, _) =>
        {
            _data.AutoPlay = _autoPlay.Checked;
            Persist();
        };

        _songList.DoubleClick += (_, _) =>
        {
            if (_songList.SelectedItem is TrackInfo track)
                PlayTrack(track, countPlay: true);
        };

        _playlistBox.SelectedIndexChanged += (_, _) =>
        {
            UpdatePlaylistActions();
            if (_suppressList)
                return;
            ShowCurrentSource();
        };

        _searchBox.TextChanged += (_, _) => RefreshSongList();
        _artistFilter.SelectedIndexChanged += (_, _) => RefreshSongList();

        _btnNewPlaylist.Click += (_, _) => CreatePlaylist();
        _btnAddToPlaylist.Click += (_, _) => AddSelectedToPlaylist();
        _btnRemoveFromPlaylist.Click += (_, _) => RemoveSelectedFromPlaylist();
        _btnDeletePlaylist.Click += (_, _) => DeletePlaylist();

        _player.PlaybackStopped += (_, _) =>
        {
            if (IsDisposed || !_data.AutoPlay)
                return;
            BeginInvoke(() => PlayRelative(1));
        };

        ResizeEnd += (_, _) =>
        {
            _data.WindowWidth = Width;
            _data.WindowHeight = Height;
            Persist();
        };

        Resize += (_, _) => UpdateTrayVisibility();

        FormClosing += OnFormClosing;
    }

    private void OnFormClosing(object? sender, FormClosingEventArgs e)
    {
        if (!_forceClose && _data.MinimizeOnClose && e.CloseReason == CloseReason.UserClosing)
        {
            FlushListenTime();
            Persist();
            e.Cancel = true;
            WindowState = FormWindowState.Minimized;
            UpdateTrayVisibility();
            return;
        }

        FlushListenTime();
        _data.WindowWidth = Width;
        _data.WindowHeight = Height;
        Persist();
        _uiTimer.Stop();
        _tray.Visible = false;
        _tray.Dispose();
        _player.Dispose();
        _coverImage?.Dispose();
    }

    private void ApplySavedSettings(bool initial)
    {
        var deviceNumber = ResolveDeviceNumber(_data.PlaybackDeviceNumber, _data.PlaybackDeviceName);
        _player.Configure(deviceNumber, _data.EqualizerPreset, _data.Volume);

        if (initial)
            StartupHelper.SetLaunchOnStartup(_data.LaunchOnStartup);
    }

    private static int ResolveDeviceNumber(int storedNumber, string? storedName)
    {
        var devices = AudioPlayer.GetDevices();
        if (!string.IsNullOrWhiteSpace(storedName))
        {
            var match = devices.FirstOrDefault(d =>
                string.Equals(d.Name, storedName, StringComparison.OrdinalIgnoreCase));
            if (match.Name is not null)
                return match.Number;
        }

        if (devices.Any(d => d.Number == storedNumber))
            return storedNumber;

        return -1;
    }

    private void OpenSettings()
    {
        using var form = new SettingsForm(_data);
        var result = form.ShowDialog(this);
        if (result is not (DialogResult.OK or DialogResult.Abort))
            return;

        _data.PlaybackDeviceNumber = form.SelectedDeviceNumber;
        _data.PlaybackDeviceName = form.SelectedDeviceName;
        _data.LaunchOnStartup = form.LaunchOnStartup;
        _data.MinimizeOnClose = form.MinimizeOnClose;
        _data.EqualizerPreset = form.EqualizerPreset;

        StartupHelper.SetLaunchOnStartup(_data.LaunchOnStartup);
        _player.Configure(_data.PlaybackDeviceNumber, _data.EqualizerPreset, _data.Volume);
        Persist();

        if (result == DialogResult.Abort)
        {
            _forceClose = true;
            Close();
            return;
        }
    }

    private void UpdateVolumeLabel() => _volPct.Text = $"{_volume.Value}%";

    private void ChooseFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Choose a folder of MP3s",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
            return;

        LoadFolder(dialog.SelectedPath);
    }

    private void LoadFolder(string folder)
    {
        _data.MusicFolder = folder;
        _library.Clear();

        foreach (var file in Directory.EnumerateFiles(folder, "*.mp3", SearchOption.AllDirectories))
        {
            var info = ReadTags(file);
            if (_data.PlayCounts.TryGetValue(file, out var count))
                info.PlayCount = count;
            _library.Add(info);
        }

        _library.Sort((a, b) => string.Compare(a.ToString(), b.ToString(), StringComparison.OrdinalIgnoreCase));
        RebuildQueue(keepCurrent: false);
        RefreshPlaylistCombo();
        RefreshArtistFilter();
        ShowCurrentSource();
        Persist();

        if (_queue.Count > 0)
            PlayAt(0, autoPlay: false, countPlay: false);
    }

    private static TrackInfo ReadTags(string path)
    {
        var info = new TrackInfo
        {
            Path = path,
            Title = Path.GetFileNameWithoutExtension(path),
            Artist = "Unknown"
        };

        try
        {
            using var file = TagLib.File.Create(path);
            if (!string.IsNullOrWhiteSpace(file.Tag.Title))
                info.Title = file.Tag.Title;
            var artists = file.Tag.Performers;
            if (artists is { Length: > 0 } && !string.IsNullOrWhiteSpace(artists[0]))
                info.Artist = string.Join(", ", artists);
            if (file.Properties.Duration > TimeSpan.Zero)
                info.Duration = file.Properties.Duration;
        }
        catch { }

        return info;
    }

    private void RefreshArtistFilter()
    {
        var selected = _artistFilter.SelectedItem as string;
        _suppressList = true;
        _artistFilter.Items.Clear();
        _artistFilter.Items.Add("All artists");
        foreach (var artist in _library.Select(t => t.Artist).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(a => a))
            _artistFilter.Items.Add(artist);

        if (selected is not null && _artistFilter.Items.Contains(selected))
            _artistFilter.SelectedItem = selected;
        else
            _artistFilter.SelectedIndex = 0;
        _suppressList = false;
    }

    private IEnumerable<TrackInfo> GetFilteredTracks()
    {
        IEnumerable<TrackInfo> tracks = _library;

        if (_artistFilter.SelectedItem is string artist && artist != "All artists")
            tracks = tracks.Where(t => string.Equals(t.Artist, artist, StringComparison.OrdinalIgnoreCase));

        var query = _searchBox.Text.Trim();
        if (query.Length > 0)
        {
            tracks = tracks.Where(t =>
                t.Title.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                t.Artist.Contains(query, StringComparison.OrdinalIgnoreCase));
        }

        return tracks;
    }

    private void RefreshSongList()
    {
        _suppressList = true;
        _songList.BeginUpdate();
        _songList.Items.Clear();
        foreach (var track in GetFilteredTracks())
            _songList.Items.Add(track);
        _songList.EndUpdate();
        _suppressList = false;
    }

    private void RebuildQueue(bool keepCurrent)
    {
        string? currentPath = _index >= 0 && _index < _queue.Count ? _queue[_index].Path : null;
        var source = GetVisibleTracks().ToList();

        _queue.Clear();
        switch (_data.ShuffleMode)
        {
            case "Random":
                _queue.AddRange(source.OrderBy(_ => _rng.Next()));
                break;
            default:
                _queue.AddRange(source);
                break;
        }

        if (keepCurrent && currentPath is not null)
        {
            _index = _queue.FindIndex(t => string.Equals(t.Path, currentPath, StringComparison.OrdinalIgnoreCase));
            if (_index < 0 && _queue.Count > 0)
                _index = 0;
        }
        else
        {
            _index = _queue.Count > 0 ? 0 : -1;
        }
    }

    private IEnumerable<TrackInfo> GetVisibleTracks()
    {
        if (_playlistBox.SelectedItem is string name && name != "All songs")
        {
            var playlist = _data.Playlists.FirstOrDefault(p => p.Name == name);
            if (playlist is null)
                return _library;

            var set = new HashSet<string>(playlist.Paths, StringComparer.OrdinalIgnoreCase);
            return _library.Where(t => set.Contains(t.Path));
        }

        return _library;
    }

    private void ShowCurrentSource()
    {
        RebuildQueue(keepCurrent: true);
        RefreshSongList();
    }

    private void RefreshPlaylistCombo()
    {
        var selected = _playlistBox.SelectedItem as string;
        _suppressList = true;
        _playlistBox.Items.Clear();
        _playlistBox.Items.Add("All songs");
        foreach (var playlist in _data.Playlists.OrderBy(p => p.Name))
            _playlistBox.Items.Add(playlist.Name);

        if (selected is not null && _playlistBox.Items.Contains(selected))
            _playlistBox.SelectedItem = selected;
        else
            _playlistBox.SelectedIndex = 0;
        _suppressList = false;
        UpdatePlaylistActions();
    }

    private void UpdatePlaylistActions()
    {
        var playlistSelected = _playlistBox.SelectedItem is string name && name != "All songs";
        _btnAddToPlaylist.Enabled = playlistSelected;
        _btnRemoveFromPlaylist.Enabled = playlistSelected;
        _btnDeletePlaylist.Enabled = playlistSelected;
    }

    private void CreatePlaylist()
    {
        using var prompt = new Form
        {
            Text = "New Playlist",
            FormBorderStyle = FormBorderStyle.FixedDialog,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(320, 110),
            MaximizeBox = false,
            MinimizeBox = false,
            BackColor = UiTheme.Bg,
            ForeColor = UiTheme.Fg
        };

        var box = new TextBox { Location = new Point(16, 16), Width = 288 };
        UiTheme.TextBox(box);

        var ok = UiTheme.Button("Create", 80);
        ok.Location = new Point(128, 60);
        ok.DialogResult = DialogResult.OK;

        var cancel = UiTheme.Button("Cancel", 80);
        cancel.Location = new Point(216, 60);
        cancel.DialogResult = DialogResult.Cancel;

        prompt.Controls.AddRange([box, ok, cancel]);
        prompt.AcceptButton = ok;
        prompt.CancelButton = cancel;

        if (prompt.ShowDialog(this) != DialogResult.OK)
            return;

        var name = box.Text.Trim();
        if (string.IsNullOrWhiteSpace(name))
            return;
        if (_data.Playlists.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase)))
        {
            MessageBox.Show(this, "A playlist with that name already exists.", "DEP3");
            return;
        }

        _data.Playlists.Add(new Playlist { Name = name });
        RefreshPlaylistCombo();
        _playlistBox.SelectedItem = name;
        Persist();
    }

    private void AddSelectedToPlaylist()
    {
        if (_playlistBox.SelectedItem is not string name || name == "All songs")
        {
            MessageBox.Show(this, "Select a playlist first.", "DEP3");
            return;
        }

        if (_songList.SelectedItem is not TrackInfo track)
        {
            MessageBox.Show(this, "Select a song to add.", "DEP3");
            return;
        }

        var playlist = _data.Playlists.First(p => p.Name == name);
        if (!playlist.Paths.Contains(track.Path, StringComparer.OrdinalIgnoreCase))
            playlist.Paths.Add(track.Path);

        Persist();
        RebuildQueue(keepCurrent: true);
    }

    private void RemoveSelectedFromPlaylist()
    {
        if (_playlistBox.SelectedItem is not string name || name == "All songs")
        {
            MessageBox.Show(this, "Select a playlist first.", "DEP3");
            return;
        }

        if (_songList.SelectedItem is not TrackInfo track)
            return;

        var playlist = _data.Playlists.First(p => p.Name == name);
        if (playlist.Paths.RemoveAll(p => string.Equals(p, track.Path, StringComparison.OrdinalIgnoreCase)) == 0)
        {
            MessageBox.Show(this, "That song is not in the selected playlist.", "DEP3");
            return;
        }

        Persist();
        RebuildQueue(keepCurrent: true);
    }

    private void DeletePlaylist()
    {
        if (_playlistBox.SelectedItem is not string name || name == "All songs")
        {
            MessageBox.Show(this, "Select a playlist to delete.", "DEP3");
            return;
        }

        var result = MessageBox.Show(
            this,
            $"Delete playlist \"{name}\"? This cannot be undone.",
            "DEP3",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning,
            MessageBoxDefaultButton.Button2);

        if (result != DialogResult.Yes)
            return;

        _data.Playlists.RemoveAll(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        RefreshPlaylistCombo();
        ShowCurrentSource();
        Persist();
    }

    private void TogglePlayStop()
    {
        if (!_player.HasTrack)
        {
            if (_queue.Count == 0)
                return;
            PlayAt(Math.Max(_index, 0), autoPlay: true, countPlay: true);
            return;
        }

        if (_player.IsPlaying)
        {
            _player.Pause();
            FlushListenTime();
            _btnPlayStop.Text = "Play";
            UpdateSeekUi();
        }
        else
        {
            _player.Play();
            _btnPlayStop.Text = "Stop";
        }
    }

    private void PlayRelative(int delta)
    {
        if (_queue.Count == 0)
            return;

        if (_data.ShuffleMode == "Random" && delta > 0)
        {
            var next = _rng.Next(_queue.Count);
            if (_queue.Count > 1 && next == _index)
                next = (next + 1) % _queue.Count;
            PlayAt(next, autoPlay: true, countPlay: true);
            return;
        }

        var target = _index < 0 ? 0 : (_index + delta + _queue.Count) % _queue.Count;
        PlayAt(target, autoPlay: true, countPlay: true);
    }

    private void PlayTrack(TrackInfo track, bool countPlay)
    {
        var idx = _queue.FindIndex(t => string.Equals(t.Path, track.Path, StringComparison.OrdinalIgnoreCase));
        if (idx < 0)
        {
            RebuildQueue(keepCurrent: false);
            idx = _queue.FindIndex(t => string.Equals(t.Path, track.Path, StringComparison.OrdinalIgnoreCase));
        }

        if (idx >= 0)
            PlayAt(idx, autoPlay: true, countPlay: countPlay);
    }

    private void PlayAt(int index, bool autoPlay, bool countPlay)
    {
        if (index < 0 || index >= _queue.Count)
            return;

        _index = index;
        var track = _queue[_index];

        try
        {
            _player.Load(track.Path, _data.Volume, autoPlay: autoPlay);
            UpdateNowPlaying(track);

            if (countPlay)
            {
                track.PlayCount++;
                _data.PlayCounts[track.Path] = track.PlayCount;
                Persist();
            }

            _btnPlayStop.Text = autoPlay ? "Stop" : "Play";
            HighlightListSelection(track);
        }
        catch
        {
            _btnPlayStop.Text = "Play";
        }
    }

    private void HighlightListSelection(TrackInfo track)
    {
        for (var i = 0; i < _songList.Items.Count; i++)
        {
            if (_songList.Items[i] is TrackInfo t &&
                string.Equals(t.Path, track.Path, StringComparison.OrdinalIgnoreCase))
            {
                _songList.SelectedIndex = i;
                return;
            }
        }
    }

    private void UpdateNowPlaying(TrackInfo track)
    {
        _title.Text = track.Title;
        _artist.Text = track.Artist;
        LoadCover(track.Path);
        UpdateSeekUi();
    }

    private void LoadCover(string path)
    {
        _coverImage?.Dispose();
        _coverImage = null;
        _cover.Image = null;

        try
        {
            using var file = TagLib.File.Create(path);
            var pictures = file.Tag.Pictures;
            if (pictures is not { Length: > 0 })
            {
                _coverImage = CreatePlaceholder();
                _cover.Image = _coverImage;
                return;
            }

            using var ms = new MemoryStream(pictures[0].Data.Data);
            using var temp = Image.FromStream(ms);
            _coverImage = new Bitmap(temp);
            _cover.Image = _coverImage;
        }
        catch
        {
            _coverImage = CreatePlaceholder();
            _cover.Image = _coverImage;
        }
    }

    private static Image CreatePlaceholder()
    {
        var bmp = new Bitmap(CoverSize, CoverSize);
        using var g = Graphics.FromImage(bmp);
        g.Clear(UiTheme.Panel);
        using var brush = new SolidBrush(UiTheme.Muted);
        using var font = new Font("Segoe UI", 10f);
        var text = "DEP3";
        var size = g.MeasureString(text, font);
        g.DrawString(text, font, brush, (CoverSize - size.Width) / 2, (CoverSize - size.Height) / 2);
        return bmp;
    }

    private void UpdateSeekUi()
    {
        TrackListeningTime();

        if (!_player.HasTrack)
        {
            _timeLeft.Text = "0:00";
            _timeRight.Text = "/ 0:00";
            _seekLine.SetPosition(0);
            return;
        }

        var total = _player.TotalTime;
        var current = _player.CurrentTime;
        _timeLeft.Text = FormatTime(current);
        _timeRight.Text = $"/ {FormatTime(total)}";

        if (!_seeking && total.TotalMilliseconds > 0)
            _seekLine.SetPosition(current.TotalMilliseconds / total.TotalMilliseconds);

        _btnPlayStop.Text = _player.IsPlaying ? "Stop" : "Play";
    }

    private void TrackListeningTime()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastListenSample).TotalSeconds;
        _lastListenSample = now;

        if (!_player.IsPlaying || elapsed <= 0 || elapsed > 2)
            return;

        _data.TotalListenSeconds += elapsed;
        _listenSinceSave += elapsed;
        UpdatePlaytimeDisplay();

        if (_listenSinceSave >= 30)
        {
            _listenSinceSave = 0;
            Persist();
        }
    }

    private void FlushListenTime()
    {
        _listenSinceSave = 0;
        _lastListenSample = DateTime.UtcNow;
    }

    private void UpdatePlaytimeDisplay() =>
        _playtimeStat.Text = $"Total playtime: {FormatTotalPlaytime(_data.TotalListenSeconds)}";

    private static string FormatTotalPlaytime(double totalSeconds)
    {
        var ts = TimeSpan.FromSeconds(Math.Max(0, totalSeconds));
        return $"{(int)ts.TotalHours}h {ts.Minutes}m {ts.Seconds}s";
    }

    private static string FormatTime(TimeSpan t) =>
        t.TotalHours >= 1 ? $"{(int)t.TotalHours}:{t.Minutes:D2}:{t.Seconds:D2}" : $"{(int)t.TotalMinutes}:{t.Seconds:D2}";

    private void SelectShuffleMode(string mode)
    {
        if (string.Equals(mode, "Most listened", StringComparison.OrdinalIgnoreCase))
            mode = "Off";

        _shuffle.SelectedIndex = string.Equals(mode, "Random", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        _data.ShuffleMode = _shuffle.SelectedItem?.ToString() ?? "Off";
    }

    private void Persist() => DataStore.Save(_data);
}
