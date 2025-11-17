namespace WinTrimmy;

public class ClipboardMonitor : IDisposable
{
    private readonly AppSettings _settings;
    private readonly CommandDetector _detector;
    private readonly System.Windows.Forms.Timer _timer;
    private string _lastClipboardContent = "";
    private bool _isOurWrite = false;

    public event Action<string>? OnClipboardTrimmed;

    public ClipboardMonitor(AppSettings settings)
    {
        _settings = settings;
        _detector = new CommandDetector(settings);
        _timer = new System.Windows.Forms.Timer
        {
            Interval = 150 // Poll every 150ms like the macOS version
        };
        _timer.Tick += Timer_Tick;
    }

    public void Start()
    {
        _timer.Start();
    }

    public void Stop()
    {
        _timer.Stop();
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (!_settings.AutoTrimEnabled) return;

        TrimClipboardIfNeeded();
    }

    public void TrimClipboardIfNeeded()
    {
        try
        {
            if (!Clipboard.ContainsText()) return;

            var text = Clipboard.GetText();

            // Skip if this was our own write or content hasn't changed
            if (_isOurWrite || text == _lastClipboardContent)
            {
                _isOurWrite = false;
                return;
            }

            _lastClipboardContent = text;

            // Clean box drawing characters first
            var cleaned = _detector.CleanBoxDrawingCharacters(text);

            // Try to transform if it looks like a command
            var transformed = _detector.TransformIfCommand(cleaned);

            if (transformed != null && transformed != text)
            {
                _isOurWrite = true;
                Clipboard.SetText(transformed);
                _lastClipboardContent = transformed;

                var summary = Ellipsize(transformed, 90);
                OnClipboardTrimmed?.Invoke(summary);
            }
        }
        catch
        {
            // Clipboard operations can fail if another app has it locked
            // Silently ignore and retry on next tick
        }
    }

    public void ForceTrim()
    {
        try
        {
            if (!Clipboard.ContainsText()) return;

            var text = Clipboard.GetText();
            var cleaned = _detector.CleanBoxDrawingCharacters(text);
            var flattened = _detector.Flatten(cleaned);

            if (flattened != text)
            {
                _isOurWrite = true;
                Clipboard.SetText(flattened);
                _lastClipboardContent = flattened;

                var summary = Ellipsize(flattened, 90);
                OnClipboardTrimmed?.Invoke(summary);
            }
        }
        catch
        {
            // Silently fail
        }
    }

    private static string Ellipsize(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;

        var halfLength = (maxLength - 3) / 2;
        return text[..halfLength] + "..." + text[^halfLength..];
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
