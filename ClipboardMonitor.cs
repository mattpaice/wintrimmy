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
        TrimClipboardIfNeeded();
    }

    public void TrimClipboardIfNeeded(bool force = false)
    {
        try
        {
            if (!force && !_settings.AutoTrimEnabled) return;
            if (!Clipboard.ContainsText()) return;

            var text = Clipboard.GetText();

            // Skip if this was our own write or content hasn't changed
            if (_isOurWrite || text == _lastClipboardContent)
            {
                _isOurWrite = false;
                return;
            }

            _lastClipboardContent = text;

            var currentText = text;
            var wasTransformed = false;

            var cleaned = _detector.CleanBoxDrawingCharacters(currentText);
            if (cleaned != null)
            {
                currentText = cleaned;
                wasTransformed = true;
            }

            var transformed = _detector.TransformIfCommand(currentText);
            if (transformed != null)
            {
                currentText = transformed;
                wasTransformed = true;
            }
            else if (force && ShouldForceFlatten(currentText))
            {
                var flattened = _detector.Flatten(currentText);
                if (flattened != currentText)
                {
                    currentText = flattened;
                    wasTransformed = true;
                }
            }

            if (!wasTransformed) return;

            _isOurWrite = true;
            Clipboard.SetText(currentText);
            _lastClipboardContent = currentText;

            var summary = Ellipsize(currentText, 90);
            OnClipboardTrimmed?.Invoke(summary);
        }
        catch
        {
            // Clipboard operations can fail if another app has it locked
            // Silently ignore and retry on next tick
        }
    }

    public void ForceTrim() => TrimClipboardIfNeeded(force: true);

    private static string Ellipsize(string text, int maxLength)
    {
        if (text.Length <= maxLength) return text;

        var halfLength = (maxLength - 3) / 2;
        return text[..halfLength] + "..." + text[^halfLength..];
    }

    internal static bool ShouldForceFlatten(string text)
    {
        if (!text.Contains('\n')) return false;

        var segments = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Length == 0) return false;

        return segments.Length <= 10;
    }

    public void Dispose()
    {
        _timer.Stop();
        _timer.Dispose();
    }
}
