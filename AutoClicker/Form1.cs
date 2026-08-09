using System.Runtime.InteropServices;

namespace AutoClicker;

public partial class Form1 : Form
{
    private CancellationTokenSource? _clickLoopCts;
    private int _clicksPerformed;
    private int? _repeatCount;
    private bool _isRunning;

    public Form1()
    {
        InitializeComponent();
        InitializeDefaults();
    }

    private void InitializeDefaults()
    {
        nudTargetX.Maximum = 100000;
        nudTargetY.Maximum = 100000;
        nudTargetX.Value = 0;
        nudTargetY.Value = 0;
        btnStop.Enabled = false;
    }

    private async void btnStart_Click(object sender, EventArgs e)
    {
        if (_isRunning)
        {
            return;
        }

        _isRunning = true;
        _clicksPerformed = 0;
        _repeatCount = ParseRepeatCount();
        var initialDelayMs = ParseInitialDelayMs();
        var clickDelayMs = ParseClickDelayMs();
        var target = GetTargetPosition();
        _clickLoopCts = new CancellationTokenSource();

        btnStart.Enabled = false;
        btnStop.Enabled = true;
        lblStatus.Text = "Starting...";

        try
        {
            if (initialDelayMs > 0)
            {
                UpdateStatus($"Starting in {initialDelayMs} ms...");
                await Task.Delay(initialDelayMs, _clickLoopCts.Token);
            }

            while (AutoClickerLoopState.ShouldContinue(_clicksPerformed, _repeatCount))
            {
                _clickLoopCts.Token.ThrowIfCancellationRequested();

                PerformClickAt(target);
                _clicksPerformed++;

                if (!AutoClickerLoopState.ShouldContinue(_clicksPerformed, _repeatCount))
                {
                    break;
                }

                if (clickDelayMs > 0)
                {
                    UpdateStatus($"Clic {_clicksPerformed}/{(_repeatCount is null ? "∞" : _repeatCount.Value)} • attente {clickDelayMs} ms");
                    await Task.Delay(clickDelayMs, _clickLoopCts.Token);
                }
            }

            UpdateStatus(_repeatCount is null || _repeatCount <= 0
                ? $"Finished after {_clicksPerformed} clic(s)"
                : $"Finished ({_clicksPerformed}/{_repeatCount})");
        }
        catch (OperationCanceledException)
        {
            UpdateStatus("Stopped");
        }
        finally
        {
            _clickLoopCts?.Dispose();
            _clickLoopCts = null;
            _isRunning = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        _clickLoopCts?.Cancel();
    }

    private void btnUseCurrentPosition_Click(object sender, EventArgs e)
    {
        var position = Cursor.Position;
        nudTargetX.Value = position.X;
        nudTargetY.Value = position.Y;
        chkUseTargetPosition.Checked = true;
        UpdateStatus($"Registered position : {position.X}, {position.Y}");
    }

    private Point GetTargetPosition()
    {
        if (chkUseTargetPosition.Checked)
        {
            return new Point((int)nudTargetX.Value, (int)nudTargetY.Value);
        }

        var position = Cursor.Position;
        return new Point(position.X, position.Y);
    }

    private void PerformClickAt(Point target)
    {
        if (chkUseTargetPosition.Checked)
        {
            SetCursorPos(target.X, target.Y);
        }

        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
    }

    private int ParseInitialDelayMs()
    {
        return (int)nudInitialDelay.Value;
    }

    private int ParseClickDelayMs()
    {
        return (int)nudClickDelay.Value;
    }

    private int? ParseRepeatCount()
    {
        var value = (int)nudRepeat.Value;
        return value <= 0 ? null : value;
    }

    private void UpdateStatus(string text)
    {
        lblStatus.Text = text;
    }

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
}
