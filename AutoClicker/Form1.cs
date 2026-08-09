using System.Runtime.InteropServices;

namespace AutoClicker;

public partial class Form1 : Form
{
    private const int ToggleHotkeyId = 1;
    private const int StopHotkeyId = 2;
    private static IntPtr _keyboardHookHandle = IntPtr.Zero;
    private static readonly LowLevelKeyboardProc KeyboardHookProc = KeyboardHookCallback;
    private static Form1? _instance;
    private CancellationTokenSource? _clickLoopCts;
    private int _clicksPerformed;
    private int? _repeatCount;
    private bool _isRunning;
    private bool _isWaitingForKey;
    private Keys _recordedKey = Keys.None;

    public Form1()
    {
        InitializeComponent();
        InitializeDefaults();
        _instance = this;
        Load += Form1_Load;
        FormClosed += Form1_FormClosed;
        chkUseTargetPosition.CheckedChanged += chkUseTargetPosition_CheckedChanged;
    }

    private void InitializeDefaults()
    {
        nudTargetX.Maximum = 100000;
        nudTargetY.Maximum = 100000;
        nudTargetX.Value = 0;
        nudTargetY.Value = 0;
        btnStop.Enabled = false;
        UpdateTargetControlsState();
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

    private void btnRecordKey_Click(object sender, EventArgs e)
    {
        _isWaitingForKey = true;
        btnRecordKey.Text = "Press a key...";
        lblRecordedKey.Text = "Current: waiting...";
        UpdateStatus("Press the key you want to record...");
    }

    private void chkUseTargetPosition_CheckedChanged(object? sender, EventArgs e)
    {
        UpdateTargetControlsState();
    }

    private void UpdateTargetControlsState()
    {
        var enabled = chkUseTargetPosition.Checked;
        nudTargetX.Enabled = enabled;
        nudTargetY.Enabled = enabled;
        btnUseCurrentPosition.Enabled = enabled;
    }

    private void ToggleAutoClicker()
    {
        if (_isRunning)
        {
            btnStop_Click(this, EventArgs.Empty);
            return;
        }

        btnStart_Click(this, EventArgs.Empty);
    }

    private void Form1_Load(object? sender, EventArgs e)
    {
        RegisterHotKey(Handle, ToggleHotkeyId, (uint)KeyModifiers.None, (uint)Keys.F1);
        RegisterHotKey(Handle, StopHotkeyId, (uint)KeyModifiers.None, (uint)Keys.F2);
        _keyboardHookHandle = SetWindowsHookEx(WH_KEYBOARD_LL, KeyboardHookProc, GetModuleHandle(null), 0);
        UpdateStatus("Ready (F1: start/stop, F2: stop)");
    }

    private void Form1_FormClosed(object? sender, FormClosedEventArgs e)
    {
        UnregisterHotKey(Handle, ToggleHotkeyId);
        UnregisterHotKey(Handle, StopHotkeyId);
        if (_keyboardHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(_keyboardHookHandle);
            _keyboardHookHandle = IntPtr.Zero;
        }

        if (ReferenceEquals(_instance, this))
        {
            _instance = null;
        }
    }

    protected override void WndProc(ref Message m)
    {
        const int wmHotkey = 0x0312;

        if (m.Msg == wmHotkey && (int)m.WParam == ToggleHotkeyId)
        {
            ToggleAutoClicker();
            return;
        }

        if (m.Msg == wmHotkey && (int)m.WParam == StopHotkeyId)
        {
            btnStop_Click(this, EventArgs.Empty);
            return;
        }

        base.WndProc(ref m);
    }

    private static IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0 && (int)wParam == WM_KEYDOWN)
        {
            var keyInfo = Marshal.ReadInt32(lParam);
            var key = (Keys)keyInfo;

            if (_instance != null && _instance._isWaitingForKey)
            {
                _instance.BeginInvoke(new Action(() =>
                {
                    _instance._recordedKey = key;
                    _instance._isWaitingForKey = false;
                    _instance.btnRecordKey.Text = "Record key";
                    _instance.lblRecordedKey.Text = $"Current: {FormatKeyName(_instance._recordedKey)}";
                    _instance.UpdateStatus($"Recorded: {FormatKeyName(_instance._recordedKey)}");
                }));
            }
        }

        return CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
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

        var selectedKey = ParseTriggeredKey();
        if (selectedKey == Keys.None)
        {
            mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, UIntPtr.Zero);
            mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, UIntPtr.Zero);
            return;
        }

        SendKeyPress(selectedKey);
    }

    private Keys ParseTriggeredKey()
    {
        return _recordedKey;
    }

    private static string FormatKeyName(Keys key)
    {
        return key switch
        {
            Keys.None => "Left Click",
            Keys.Space => "Space",
            Keys.Enter => "Enter",
            Keys.Tab => "Tab",
            Keys.Escape => "Escape",
            Keys.ShiftKey => "Shift",
            Keys.ControlKey => "Ctrl",
            _ => key.ToString()
        };
    }

    private static void SendKeyPress(Keys key)
    {
        if (key == Keys.None)
        {
            return;
        }

        keybd_event((byte)key, 0, 0, UIntPtr.Zero);
        keybd_event((byte)key, 0, KEYEVENTF_KEYUP, UIntPtr.Zero);
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
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll")]
    private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, UIntPtr dwExtraInfo);

    [DllImport("user32.dll")]
    private static extern void keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(string? lpModuleName);

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;
    private const uint KEYEVENTF_KEYUP = 0x0002;
    private const int WH_KEYBOARD_LL = 13;
    private const int WM_KEYDOWN = 0x0100;

    private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

    [Flags]
    private enum KeyModifiers : uint
    {
        None = 0,
        Alt = 1,
        Control = 2,
        Shift = 4,
        Windows = 8
    }
}
