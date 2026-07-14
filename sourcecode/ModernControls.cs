namespace DEP3;

sealed class BackgroundPanel : Panel
{
    public BackgroundPanel()
    {
        DoubleBuffered = true;
        BackColor = UiTheme.Bg;
    }
}
