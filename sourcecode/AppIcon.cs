namespace DEP3;

static class AppIcon
{
    private static Icon? _cached;

    public static Icon Get()
    {
        if (_cached is not null)
            return _cached;

        var path = Environment.ProcessPath ?? Application.ExecutablePath;
        _cached = Icon.ExtractAssociatedIcon(path) ?? SystemIcons.Application;
        return _cached;
    }
}
