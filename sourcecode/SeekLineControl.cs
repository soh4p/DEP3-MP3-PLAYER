namespace DEP3;

sealed class SeekLineControl : Control
{
    private double _position;
    private bool _dragging;

    public event EventHandler<double>? SeekRequested;

    public SeekLineControl()
    {
        DoubleBuffered = true;
        Height = 28;
        Cursor = Cursors.Hand;
        BackColor = UiTheme.Bg;
    }

    public void SetPosition(double ratio)
    {
        _position = Math.Clamp(ratio, 0, 1);
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.Clear(BackColor);

        var left = 6;
        var right = Width - 6;
        var mid = Height / 2f;
        var x = left + (float)(_position * (right - left));

        using (var track = new Pen(Color.FromArgb(55, 55, 55), 3))
            g.DrawLine(track, left, mid, right, mid);

        using (var progress = new Pen(UiTheme.Accent, 3))
            g.DrawLine(progress, left, mid, x, mid);

        using var head = new Pen(Color.White, 2);
        g.DrawLine(head, x, 6, x, Height - 6);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left || Width <= 0)
            return;

        _dragging = true;
        Capture = true;
        RequestSeek(e.X);
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_dragging)
            RequestSeek(e.X);
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            _dragging = false;
            Capture = false;
        }

        base.OnMouseUp(e);
    }

    private void RequestSeek(int x)
    {
        var left = 6;
        var right = Width - 6;
        var ratio = Math.Clamp((x - left) / (double)Math.Max(1, right - left), 0, 1);
        SetPosition(ratio);
        SeekRequested?.Invoke(this, ratio);
    }
}
