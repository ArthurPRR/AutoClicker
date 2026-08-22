using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace AutoClicker;

public partial class Form1 : Form
{
    private const int ToggleHotkeyId = 1;
    private const int StopHotkeyId = 2;

    private static IntPtr _keyboardHookHandle = IntPtr.Zero;
    private static IntPtr _mouseHookHandle = IntPtr.Zero;

    private static readonly LowLevelKeyboardProc KeyboardHookProc = KeyboardHookCallback;
    private static readonly LowLevelMouseProc MouseHookProc = MouseHookCallback;

    private static Form1? _instance;

    private CancellationTokenSource? _clickLoopCts;
    private int _clicksPerformed;
    private int? _repeatCount;
    private bool _isRunning;

    // Enregistrement d'une action
    private bool _isWaitingForInput;


    private SequenceStep? _recordedStep;


    // État des touches modificatrices
    private static bool _ctrlDown;
    private static bool _shiftDown;
    private static bool _altDown;

    private readonly List<SequenceStep> _sequenceItems = new();
    private int _sequenceIndex;
    private bool _useSequence;

    // ============================================================
    // Types
    // ============================================================

    private enum SequenceStepType
    {
        Keyboard,
        Mouse
    }

    private enum MouseButton
    {
        Left,
        Right,
        Middle
    }

    private sealed class SequenceStep
    {
        public SequenceStepType Type { get; init; }

        public Keys Key { get; init; }

        public List<Keys> Modifiers { get; init; } = new();

        public MouseButton MouseButton { get; init; }

        public Point MousePosition { get; init; }

        public int DelayMs { get; init; }

        public string DisplayName
        {
            get
            {
                var delaySuffix = DelayMs > 0
                    ? $" ({DelayMs} ms)"
                    : string.Empty;

                if (Type == SequenceStepType.Mouse)
                {
                    return $"{MouseButtonToString(MouseButton)} Click ({MousePosition.X}, {MousePosition.Y}){delaySuffix}";
                }

                return $"{FormatComboName(Modifiers, Key)}{delaySuffix}";
            }
        }
    }

    // ============================================================
    // Constructor
    // ============================================================

    public Form1()
    {
        InitializeComponent();
        
        AcceptButton = null;
        
        InitializeDefaults();

        _instance = this;

        Load += Form1_Load;
        FormClosed += Form1_FormClosed;

        chkUseTargetPosition.CheckedChanged += chkUseTargetPosition_CheckedChanged;
        chkUseSequence.CheckedChanged += chkUseSequence_CheckedChanged;
    }

    // ============================================================
    // Initialisation
    // ============================================================

    private void InitializeDefaults()
    {
        // Autorise les coordonnées négatives pour plusieurs écrans.
        nudTargetX.Minimum = -100000;
        nudTargetX.Maximum = 100000;

        nudTargetY.Minimum = -100000;
        nudTargetY.Maximum = 100000;

        nudTargetX.Value = 0;
        nudTargetY.Value = 0;

        btnStop.Enabled = false;

        UpdateTargetControlsState();
    }

    // ============================================================
    // Start / Stop
    // ============================================================

    private async void btnStart_Click(object sender, EventArgs e)
    {
        if (_isRunning)
            return;

        if (_useSequence && _sequenceItems.Count == 0)
        {
            UpdateStatus("La séquence est vide.");
            return;
        }

        _isRunning = true;
        _clicksPerformed = 0;

        _repeatCount = ParseRepeatCount();

        var initialDelayMs = ParseInitialDelayMs();
        var clickDelayMs = ParseClickDelayMs();

        var target = GetTargetPosition();

        _clickLoopCts = new CancellationTokenSource();
        _sequenceIndex = 0;

        btnStart.Enabled = false;
        btnStop.Enabled = true;

        UpdateStatus("Starting...");

        try
        {
            if (initialDelayMs > 0)
            {
                UpdateStatus($"Starting in {initialDelayMs} ms...");

                await Task.Delay(
                    initialDelayMs,
                    _clickLoopCts.Token);
            }

            while (ShouldContinue())
            {
                _clickLoopCts.Token.ThrowIfCancellationRequested();

                var delayMsForAction = ExecuteNextAction(
                    target,
                    clickDelayMs);

                _clicksPerformed++;

                if (!ShouldContinue())
                    break;

                if (delayMsForAction > 0)
                {
                    var countText = _repeatCount is null
                        ? "∞"
                        : _repeatCount.Value.ToString();

                    UpdateStatus(
                        $"Action {_clicksPerformed}/{countText} • attente {delayMsForAction} ms");

                    await Task.Delay(
                        delayMsForAction,
                        _clickLoopCts.Token);
                }
            }

            UpdateStatus(
                _repeatCount is null
                    ? $"Finished after {_clicksPerformed} action(s)"
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

    private bool ShouldContinue()
    {
        if (_repeatCount is null)
            return true;

        return _clicksPerformed < _repeatCount.Value;
    }

    private void btnStop_Click(object sender, EventArgs e)
    {
        _clickLoopCts?.Cancel();
    }

    // ============================================================
    // Enregistrement clavier / souris
    // ============================================================

    private void btnRecordKey_Click(object sender, EventArgs e)
    {
        StartInputRecording();
        AcceptButton = null;
    }

    private void StartInputRecording()
    {
        if (_isWaitingForInput)
            return;

        _recordedStep = null;

        _ctrlDown = false;
        _shiftDown = false;
        _altDown = false;

        _isWaitingForInput = true;

        btnRecordKey.Text = "Waiting...";
        lblRecordedKey.Text = "Current: waiting...";

        UpdateStatus(
            "Appuie sur une touche, une combinaison ou clique...");
    }
    private void StopInputRecording()
    {
        _isWaitingForInput = false;

        _ctrlDown = false;
        _shiftDown = false;
        _altDown = false;

        btnRecordKey.Text = "Record combo";

        if (_recordedStep != null)
        {
            lblRecordedKey.Text =
                $"Current: {_recordedStep.DisplayName}";
        }
        else
        {
            lblRecordedKey.Text = "Current: none";
        }
    }
    // ============================================================
    // Hook clavier
    // ============================================================

    private static Keys GetKeyFromHook(KBDLLHOOKSTRUCT keyInfo)
    {
        return (Keys)(keyInfo.vkCode & 0xFF);
    }

    private static IntPtr KeyboardHookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(
                _keyboardHookHandle,
                nCode,
                wParam,
                lParam);
        }

        var message = (int)wParam;

        if (message != WM_KEYDOWN &&
            message != WM_KEYUP)
        {
            return CallNextHookEx(
                _keyboardHookHandle,
                nCode,
                wParam,
                lParam);
        }

        var keyInfo =
            Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);

        var key = (Keys)(keyInfo.vkCode & 0xFF);

        bool keyDown = message == WM_KEYDOWN;


        switch (key)
        {
            case Keys.LControlKey:
            case Keys.RControlKey:
            case Keys.ControlKey:
                _ctrlDown = keyDown;
                break;

            case Keys.LShiftKey:
            case Keys.RShiftKey:
            case Keys.ShiftKey:
                _shiftDown = keyDown;
                break;

            case Keys.LMenu:
            case Keys.RMenu:
            case Keys.Menu:
                _altDown = keyDown;
                break;
        }


        if (_instance != null &&
            _instance._isWaitingForInput)
        {
            const uint LLKHF_INJECTED = 0x00000010;

            if ((keyInfo.flags & LLKHF_INJECTED) != 0)
            {
                return CallNextHookEx(
                    _keyboardHookHandle,
                    nCode,
                    wParam,
                    lParam);
            }

            if (keyDown && !IsModifierKey(key))
            {
                var recordedKey = key;

                var modifiers = new List<Keys>();

                if (_ctrlDown)
                    modifiers.Add(Keys.Control);

                if (_shiftDown)
                    modifiers.Add(Keys.Shift);

                if (_altDown)
                    modifiers.Add(Keys.Alt);

                _instance.BeginInvoke(new Action(() =>
                {
                    if (_instance == null ||
                        !_instance._isWaitingForInput)
                    {
                        return;
                    }

                    var step = new SequenceStep
                    {
                        Type = SequenceStepType.Keyboard,
                        Key = recordedKey,
                        Modifiers = modifiers
                    };

                    _instance._recordedStep = step;

                    _instance.lblRecordedKey.Text =
                        $"Current: {step.DisplayName}";

                    _instance.StopInputRecording();
                }));

                return (IntPtr)1;
            }

            return (IntPtr)1;
        }

        return CallNextHookEx(
            _keyboardHookHandle,
            nCode,
            wParam,
            lParam);
    }


    // ============================================================
    // Hook souris
    // ============================================================

    private static IntPtr MouseHookCallback(
        int nCode,
        IntPtr wParam,
        IntPtr lParam)
    {
        if (nCode < 0)
        {
            return CallNextHookEx(
                _mouseHookHandle,
                nCode,
                wParam,
                lParam);
        }

        if (_instance == null ||
            !_instance._isWaitingForInput)
        {
            return CallNextHookEx(
                _mouseHookHandle,
                nCode,
                wParam,
                lParam);
        }

        var message = (int)wParam;

        MouseButton? button = message switch
        {
            WM_LBUTTONDOWN => MouseButton.Left,
            WM_RBUTTONDOWN => MouseButton.Right,
            WM_MBUTTONDOWN => MouseButton.Middle,
            _ => null
        };

        if (!button.HasValue)
        {
            return CallNextHookEx(
                _mouseHookHandle,
                nCode,
                wParam,
                lParam);
        }

        var mouseInfo =
            Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);

        var position = new Point(
            mouseInfo.pt.x,
            mouseInfo.pt.y);

        var capturedButton = button.Value;

        _instance.BeginInvoke(new Action(() =>
        {
            if (_instance == null ||
                !_instance._isWaitingForInput)
            {
                return;
            }

            var step = new SequenceStep
            {
                Type = SequenceStepType.Mouse,
                MouseButton = capturedButton,
                MousePosition = position
            };

            // On capture seulement.
            // On n'ajoute PAS automatiquement à la séquence.
            _instance._recordedStep = step;

            _instance.lblRecordedKey.Text =
                $"Current: {step.DisplayName}";

            _instance.StopInputRecording();
        }));

        return CallNextHookEx(
            _mouseHookHandle,
            nCode,
            wParam,
            lParam);
    }

    // ============================================================
    // Séquence
    // ============================================================

    private void chkUseSequence_CheckedChanged(
        object? sender,
        EventArgs e)
    {
        _useSequence = chkUseSequence.Checked;
    }

    private void btnAddSequenceStep_Click(
        object sender,
        EventArgs e)
    {
        if (_recordedStep != null)
        {
            AddSequenceStep(_recordedStep);

            return;
        }
        var text = txtComboInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(text))
        {
            UpdateStatus("Aucun combo enregistré.");
            return;
        }

        if (TryParseSequenceStep(text, out var step))
        {
            AddSequenceStep(step);

            txtComboInput.Clear();
            lblRecordedKey.Text = "Current: none";
        }
        else
        {
            UpdateStatus($"Input invalide : {text}");
        }
    }

    private void btnAddTypedCombo_Click(
        object sender,
        EventArgs e)
    {
        var text = txtComboInput.Text.Trim();

        if (string.IsNullOrWhiteSpace(text))
            return;

        if (TryParseSequenceStep(text, out var step))
        {
            AddSequenceStep(step);
            txtComboInput.Text = string.Empty;
        }
        else
        {
            UpdateStatus($"Input invalide : {text}");
        }
    }

    private void AddSequenceStep(SequenceStep step)
    {
        var delayedStep = ApplyStepDelay(
            step,
            (int)nudSequenceDelay.Value);

        _sequenceItems.Add(delayedStep);

        lstSequence.Items.Add(delayedStep.DisplayName);

        chkUseSequence.Checked = true;

        UpdateStatus($"Added: {delayedStep.DisplayName}");
    }

    private static SequenceStep ApplyStepDelay(
        SequenceStep step,
        int delayMs)
    {
        if (step.Type == SequenceStepType.Mouse)
        {
            return new SequenceStep
            {
                Type = SequenceStepType.Mouse,
                MouseButton = step.MouseButton,
                MousePosition = step.MousePosition,
                DelayMs = delayMs
            };
        }

        return new SequenceStep
        {
            Type = SequenceStepType.Keyboard,
            Key = step.Key,
            Modifiers = step.Modifiers.ToList(),
            DelayMs = delayMs
        };
    }

    private void btnRemoveSequenceStep_Click(
        object sender,
        EventArgs e)
    {
        if (lstSequence.SelectedIndex < 0)
            return;

        var index = lstSequence.SelectedIndex;

        lstSequence.Items.RemoveAt(index);
        _sequenceItems.RemoveAt(index);

        if (_sequenceItems.Count == 0)
        {
            _sequenceIndex = 0;
        }
        else if (index < _sequenceIndex)
        {
            _sequenceIndex--;
        }
        else if (
            index == _sequenceIndex &&
            _sequenceIndex >= _sequenceItems.Count)
        {
            _sequenceIndex = 0;
        }
    }

    private void btnClearSequence_Click(
        object sender,
        EventArgs e)
    {
        _sequenceItems.Clear();
        lstSequence.Items.Clear();

        _sequenceIndex = 0;

        UpdateStatus("Sequence cleared");
    }

    private SequenceStep? GetNextSequenceStep()
    {
        if (_sequenceItems.Count == 0)
            return null;

        var step = _sequenceItems[_sequenceIndex];

        _sequenceIndex =
            (_sequenceIndex + 1) %
            _sequenceItems.Count;

        return step;
    }

    // ============================================================
    // Exécution
    // ============================================================

    private int ExecuteNextAction(
        Point target,
        int defaultDelayMs)
    {
        if (_useSequence && _sequenceItems.Count > 0)
        {
            var step = GetNextSequenceStep();

            if (step != null)
            {
                ExecuteSequenceStep(step);

                return step.DelayMs > 0
                    ? step.DelayMs
                    : defaultDelayMs;
            }

            return defaultDelayMs;
        }

        // Pas de séquence : clic gauche classique.
        if (chkUseTargetPosition.Checked)
        {
            SetCursorPos(target.X, target.Y);
        }

        SendMouseClick(MouseButton.Left);

        return defaultDelayMs;
    }

    private void ExecuteSequenceStep(SequenceStep step)
    {
        if (step.Type == SequenceStepType.Mouse)
        {
            Point position;

            if (chkUseTargetPosition.Checked)
            {
                position = GetTargetPosition();
            }
            else
            {
                position = step.MousePosition;
            }

            SetCursorPos(position.X, position.Y);

            SendMouseClick(step.MouseButton);

            return;
        }

        SendKeyCombination(
            step.Modifiers,
            step.Key);
    }

    // ============================================================
    // Parsing manuel
    // ============================================================

    private static bool TryParseSequenceStep(
        string text,
        out SequenceStep step)
    {
        step = new SequenceStep();

        text = text.Trim();

        // Clics souris
        switch (text.ToLowerInvariant())
        {
            case "left click":
            case "leftmouse":
            case "left mouse":
            case "mouse left":

                step = new SequenceStep
                {
                    Type = SequenceStepType.Mouse,
                    MouseButton = MouseButton.Left,
                    MousePosition = Cursor.Position
                };

                return true;

            case "right click":
            case "rightmouse":
            case "right mouse":
            case "mouse right":

                step = new SequenceStep
                {
                    Type = SequenceStepType.Mouse,
                    MouseButton = MouseButton.Right,
                    MousePosition = Cursor.Position
                };

                return true;

            case "middle click":
            case "middlemouse":
            case "middle mouse":
            case "mouse middle":

                step = new SequenceStep
                {
                    Type = SequenceStepType.Mouse,
                    MouseButton = MouseButton.Middle,
                    MousePosition = Cursor.Position
                };

                return true;
        }

        // Clavier
        var parts = text.Split(
            '+',
            StringSplitOptions.TrimEntries |
            StringSplitOptions.RemoveEmptyEntries);

        var modifiers = new List<Keys>();
        Keys key = Keys.None;

        foreach (var part in parts)
        {
            if (TryParseModifier(part, out var modifier))
            {
                modifiers.Add(modifier);
                continue;
            }

            if (TryParseKey(part, out var parsedKey))
            {
                key = parsedKey;
                continue;
            }

            return false;
        }

        if (key == Keys.None)
            return false;

        step = new SequenceStep
        {
            Type = SequenceStepType.Keyboard,
            Key = key,
            Modifiers = modifiers
        };

        return true;
    }

    private static bool TryParseModifier(
        string value,
        out Keys modifier)
    {
        modifier = value.Trim().ToLowerInvariant() switch
        {
            "ctrl" => Keys.Control,
            "control" => Keys.Control,

            "shift" => Keys.Shift,

            "alt" => Keys.Alt,

            _ => Keys.None
        };

        return modifier != Keys.None;
    }

    private static bool TryParseKey(
        string value,
        out Keys key)
    {
        value = value.Trim();

        if (Enum.TryParse(value, true, out key))
        {
            return key != Keys.None;
        }

        key = value.ToLowerInvariant() switch
        {
            "space" => Keys.Space,
            "enter" => Keys.Enter,
            "return" => Keys.Enter,
            "tab" => Keys.Tab,
            "escape" => Keys.Escape,
            "esc" => Keys.Escape,
            "backspace" => Keys.Back,
            "back" => Keys.Back,

            "left" => Keys.Left,
            "right" => Keys.Right,
            "up" => Keys.Up,
            "down" => Keys.Down,

            "delete" => Keys.Delete,
            "insert" => Keys.Insert,
            "home" => Keys.Home,
            "end" => Keys.End,
            "pageup" => Keys.PageUp,
            "pagedown" => Keys.PageDown,

            _ => Keys.None
        };

        return key != Keys.None;
    }

    // ============================================================
    // Envoi clavier
    // ============================================================

    private static void SendKeyCombination(
        IEnumerable<Keys> modifiers,
        Keys key)
    {
        if (key == Keys.None)
            return;

        var modifierKeys = modifiers.ToList();

        var inputs = new List<INPUT>();

        // KeyDown modifiers
        foreach (var modifier in modifierKeys)
        {
            inputs.Add(CreateKeyInput(
                modifier,
                false));
        }

        // KeyDown main key
        inputs.Add(CreateKeyInput(
            key,
            false));

        // KeyUp main key
        inputs.Add(CreateKeyInput(
            key,
            true));

        // KeyUp modifiers
        for (var i = modifierKeys.Count - 1;
             i >= 0;
             i--)
        {
            inputs.Add(CreateKeyInput(
                modifierKeys[i],
                true));
        }

        SendInput(
            (uint)inputs.Count,
            inputs.ToArray(),
            Marshal.SizeOf<INPUT>());
    }

    private static INPUT CreateKeyInput(
        Keys key,
        bool keyUp)
    {
        return new INPUT
        {
            Type = INPUT_KEYBOARD,

            Data = new InputUnion
            {
                Keyboard = new KEYBDINPUT
                {
                    WVk = (ushort)(key & Keys.KeyCode),
                    WScan = 0,

                    DwFlags = keyUp
                        ? KEYEVENTF_KEYUP
                        : 0,

                    Time = 0,
                    DwExtraInfo = IntPtr.Zero
                }
            }
        };
    }

    // ============================================================
    // Envoi souris
    // ============================================================

    private static void SendMouseClick(
        MouseButton button)
    {
        uint down;
        uint up;

        switch (button)
        {
            case MouseButton.Left:
                down = MOUSEEVENTF_LEFTDOWN;
                up = MOUSEEVENTF_LEFTUP;
                break;

            case MouseButton.Right:
                down = MOUSEEVENTF_RIGHTDOWN;
                up = MOUSEEVENTF_RIGHTUP;
                break;

            case MouseButton.Middle:
                down = MOUSEEVENTF_MIDDLEDOWN;
                up = MOUSEEVENTF_MIDDLEUP;
                break;

            default:
                return;
        }

        var inputs = new[]
        {
            new INPUT
            {
                Type = INPUT_MOUSE,

                Data = new InputUnion
                {
                    Mouse = new MOUSEINPUT
                    {
                        Dx = 0,
                        Dy = 0,
                        MouseData = 0,
                        DwFlags = down,
                        Time = 0,
                        DwExtraInfo = IntPtr.Zero
                    }
                }
            },

            new INPUT
            {
                Type = INPUT_MOUSE,

                Data = new InputUnion
                {
                    Mouse = new MOUSEINPUT
                    {
                        Dx = 0,
                        Dy = 0,
                        MouseData = 0,
                        DwFlags = up,
                        Time = 0,
                        DwExtraInfo = IntPtr.Zero
                    }
                }
            }
        };

        SendInput(
            (uint)inputs.Length,
            inputs,
            Marshal.SizeOf<INPUT>());
    }

    // ============================================================
    // Position
    // ============================================================

    private void btnUseCurrentPosition_Click(
        object sender,
        EventArgs e)
    {
        var position = Cursor.Position;

        nudTargetX.Value = position.X;
        nudTargetY.Value = position.Y;

        chkUseTargetPosition.Checked = true;

        UpdateStatus(
            $"Registered position : {position.X}, {position.Y}");
    }

    private Point GetTargetPosition()
    {
        if (chkUseTargetPosition.Checked)
        {
            return new Point(
                (int)nudTargetX.Value,
                (int)nudTargetY.Value);
        }

        return Cursor.Position;
    }

    private void chkUseTargetPosition_CheckedChanged(
        object? sender,
        EventArgs e)
    {
        UpdateTargetControlsState();
    }

    private void UpdateTargetControlsState()
    {
        var enabled =
            chkUseTargetPosition.Checked;

        nudTargetX.Enabled = enabled;
        nudTargetY.Enabled = enabled;
        btnUseCurrentPosition.Enabled = enabled;
    }

    // ============================================================
    // Hotkeys
    // ============================================================

    private void ToggleAutoClicker()
    {
        if (_isRunning)
        {
            btnStop_Click(
                this,
                EventArgs.Empty);

            return;
        }

        btnStart_Click(
            this,
            EventArgs.Empty);
    }

    private void Form1_Load(
        object? sender,
        EventArgs e)
    {
        RegisterHotKey(
            Handle,
            ToggleHotkeyId,
            0,
            (uint)Keys.F1);

        RegisterHotKey(
            Handle,
            StopHotkeyId,
            0,
            (uint)Keys.F2);

        _keyboardHookHandle =
            SetWindowsHookEx(
                WH_KEYBOARD_LL,
                KeyboardHookProc,
                GetModuleHandle(null),
                0);

        _mouseHookHandle =
            SetWindowsHookEx(
                WH_MOUSE_LL,
                MouseHookProc,
                GetModuleHandle(null),
                0);

        UpdateStatus(
            "Ready (F1: start/stop, F2: stop)");
    }

    private void Form1_FormClosed(
        object? sender,
        FormClosedEventArgs e)
    {
        _clickLoopCts?.Cancel();

        UnregisterHotKey(
            Handle,
            ToggleHotkeyId);

        UnregisterHotKey(
            Handle,
            StopHotkeyId);

        if (_keyboardHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(
                _keyboardHookHandle);

            _keyboardHookHandle = IntPtr.Zero;
        }

        if (_mouseHookHandle != IntPtr.Zero)
        {
            UnhookWindowsHookEx(
                _mouseHookHandle);

            _mouseHookHandle = IntPtr.Zero;
        }

        if (ReferenceEquals(_instance, this))
        {
            _instance = null;
        }
    }

    protected override void WndProc(
        ref Message m)
    {
        const int WM_HOTKEY = 0x0312;

        if (m.Msg == WM_HOTKEY)
        {
            var id = (int)m.WParam;

            if (id == ToggleHotkeyId)
            {
                if (_isWaitingForInput)
                {
                    base.WndProc(ref m);
                    return;
                }

                ToggleAutoClicker();
                return;
            }

            if (id == StopHotkeyId)
            {
                if (_isWaitingForInput)
                {
                    base.WndProc(ref m);
                    return;
                }

                btnStop_Click(
                    this,
                    EventArgs.Empty);

                return;
            }
        }

        base.WndProc(ref m);
    }

    // ============================================================
    // Affichage
    // ============================================================

    private static string MouseButtonToString(
        MouseButton button)
    {
        return button switch
        {
            MouseButton.Left => "Left",
            MouseButton.Right => "Right",
            MouseButton.Middle => "Middle",
            _ => "Unknown"
        };
    }

    private static string FormatComboName(
        IEnumerable<Keys> modifiers,
        Keys key)
    {
        var parts = new List<string>();

        foreach (var modifier in modifiers)
        {
            switch (modifier)
            {
                case Keys.Control:
                    parts.Add("Ctrl");
                    break;

                case Keys.Shift:
                    parts.Add("Shift");
                    break;

                case Keys.Alt:
                    parts.Add("Alt");
                    break;
            }
        }

        if (key == Keys.None)
            return string.Join(" + ", parts);

        parts.Add(FormatKeyName(key));

        return string.Join(" + ", parts);
    }

    private static string FormatKeyName(Keys key)
    {
        return key switch
        {
            Keys.None => "None",
            Keys.Space => "Space",
            Keys.Enter => "Enter",
            Keys.Tab => "Tab",
            Keys.Escape => "Escape",
            Keys.Back => "Backspace",

            Keys.Left => "Left",
            Keys.Right => "Right",
            Keys.Up => "Up",
            Keys.Down => "Down",

            _ => key.ToString()
        };
    }

    private static bool IsModifierKey(Keys key)
    {
        return key switch
        {
            Keys.Control or
            Keys.ControlKey or
            Keys.LControlKey or
            Keys.RControlKey => true,

            Keys.Shift or
            Keys.ShiftKey or
            Keys.LShiftKey or
            Keys.RShiftKey => true,

            Keys.Menu or
            Keys.Alt or
            Keys.LMenu or
            Keys.RMenu => true,

            _ => false
        };
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

        return value <= 0
            ? null
            : value;
    }

    private void UpdateStatus(string text)
    {
        lblStatus.Text = text;
    }

    // ============================================================
    // Structures Win32
    // ============================================================

    [StructLayout(LayoutKind.Sequential)]
    private struct INPUT
    {
        public uint Type;
        public InputUnion Data;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct InputUnion
    {
        [FieldOffset(0)]
        public MOUSEINPUT Mouse;

        [FieldOffset(0)]
        public KEYBDINPUT Keyboard;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MOUSEINPUT
    {
        public int Dx;
        public int Dy;
        public uint MouseData;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct KEYBDINPUT
    {
        public ushort WVk;
        public ushort WScan;
        public uint DwFlags;
        public uint Time;
        public IntPtr DwExtraInfo;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int x;
        public int y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MSLLHOOKSTRUCT
    {
        public POINT pt;
        public uint mouseData;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

#pragma warning disable CS0649

    [StructLayout(LayoutKind.Sequential)]
    private struct KBDLLHOOKSTRUCT
    {
        public uint vkCode;
        public uint scanCode;
        public uint flags;
        public uint time;
        public IntPtr dwExtraInfo;
    }

#pragma warning restore CS0649

    // ============================================================
    // Win32
    // ============================================================

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(
        IntPtr hWnd,
        int id,
        uint fsModifiers,
        uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(
        IntPtr hWnd,
        int id);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(
        int x,
        int y);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern uint SendInput(
        uint nInputs,
        [In] INPUT[] pInputs,
        int cbSize);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelKeyboardProc callback,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SetWindowsHookEx(
        int idHook,
        LowLevelMouseProc callback,
        IntPtr hMod,
        uint dwThreadId);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool UnhookWindowsHookEx(
        IntPtr hhk);

    [DllImport("user32.dll")]
    private static extern IntPtr CallNextHookEx(
        IntPtr hhk,
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern IntPtr GetModuleHandle(
        string? lpModuleName);

    // ============================================================
    // Hooks
    // ============================================================

    private delegate IntPtr LowLevelKeyboardProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    private delegate IntPtr LowLevelMouseProc(
        int nCode,
        IntPtr wParam,
        IntPtr lParam);

    // ============================================================
    // Constantes clavier
    // ============================================================

    private const uint INPUT_MOUSE = 0;
    private const uint INPUT_KEYBOARD = 1;

    private const uint KEYEVENTF_KEYUP = 0x0002;

    // ============================================================
    // Constantes souris
    // ============================================================

    private const uint MOUSEEVENTF_LEFTDOWN = 0x0002;
    private const uint MOUSEEVENTF_LEFTUP = 0x0004;

    private const uint MOUSEEVENTF_RIGHTDOWN = 0x0008;
    private const uint MOUSEEVENTF_RIGHTUP = 0x0010;

    private const uint MOUSEEVENTF_MIDDLEDOWN = 0x0020;
    private const uint MOUSEEVENTF_MIDDLEUP = 0x0040;

    // ============================================================
    // Messages
    // ============================================================

    private const int WH_KEYBOARD_LL = 13;
    private const int WH_MOUSE_LL = 14;

    private const int WM_KEYDOWN = 0x0100;
    private const int WM_KEYUP = 0x0101;

    private const int WM_LBUTTONDOWN = 0x0201;
    private const int WM_RBUTTONDOWN = 0x0204;
    private const int WM_MBUTTONDOWN = 0x0207;
}