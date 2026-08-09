namespace AutoClicker;

partial class Form1
{
    private System.ComponentModel.IContainer components = null;
    private Button btnStart;
    private Button btnStop;
    private Button btnUseCurrentPosition;
    private Label lblStatus;
    private Label lblInitialDelay;
    private Label lblClickDelay;
    private Label lblRepeat;
    private Label lblTarget;
    private Label lblTriggerKey;
    private Button btnRecordKey;
    private Label lblRecordedKey;
    private NumericUpDown nudInitialDelay;
    private NumericUpDown nudClickDelay;
    private NumericUpDown nudRepeat;
    private NumericUpDown nudTargetX;
    private NumericUpDown nudTargetY;
    private CheckBox chkUseTargetPosition;
    private CheckBox chkUseSequence;
    private ListBox lstSequence;
    private TextBox txtComboInput;
    private Label lblComboInput;
    private Button btnAddSequenceStep;
    private Button btnAddTypedCombo;
    private Button btnRemoveSequenceStep;
    private Button btnClearSequence;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
        {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(860, 430);
        Text = "AutoClicker";

        var panelTiming = new Panel
        {
            Location = new Point(15, 15),
            Size = new Size(430, 85),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblTimingTitle = new Label
        {
            Text = "Timing",
            Location = new Point(10, 8),
            AutoSize = true,
            Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
        };

        lblInitialDelay = new Label
        {
            Text = "Initial delay (ms)",
            Location = new Point(15, 32),
            AutoSize = true
        };

        nudInitialDelay = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 10000,
            Value = 0,
            Location = new Point(160, 30),
            Width = 120
        };

        lblClickDelay = new Label
        {
            Text = "Between clicks (ms)",
            Location = new Point(15, 58),
            AutoSize = true
        };

        nudClickDelay = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 10000,
            Value = 100,
            Location = new Point(160, 56),
            Width = 120
        };

        lblRepeat = new Label
        {
            Text = "Repeats (0 = infinity)",
            Location = new Point(300, 32),
            AutoSize = true
        };

        nudRepeat = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 100000,
            Value = 0,
            Location = new Point(300, 56),
            Width = 110
        };

        var panelTarget = new Panel
        {
            Location = new Point(15, 110),
            Size = new Size(430, 120),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblTargetTitle = new Label
        {
            Text = "Target",
            Location = new Point(10, 8),
            AutoSize = true,
            Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
        };

        lblTarget = new Label
        {
            Text = "Position (X, Y)",
            Location = new Point(15, 32),
            AutoSize = true
        };

        chkUseTargetPosition = new CheckBox
        {
            Text = "Use these coordinates",
            Location = new Point(150, 30),
            AutoSize = true
        };

        nudTargetX = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 100000,
            Value = 0,
            Location = new Point(150, 55),
            Width = 90
        };

        nudTargetY = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 100000,
            Value = 0,
            Location = new Point(250, 55),
            Width = 90
        };

        btnUseCurrentPosition = new Button
        {
            Text = "Current position",
            Location = new Point(15, 85),
            Width = 140
        };
        btnUseCurrentPosition.Click += btnUseCurrentPosition_Click;

        var panelTrigger = new Panel
        {
            Location = new Point(15, 240),
            Size = new Size(430, 80),
            BorderStyle = BorderStyle.FixedSingle
        };

        var panelSequence = new Panel
        {
            Location = new Point(470, 15),
            Size = new Size(360, 360),
            BorderStyle = BorderStyle.FixedSingle
        };

        var lblSequenceTitle = new Label
        {
            Text = "Input sequence",
            Location = new Point(10, 8),
            AutoSize = true,
            Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
        };

        chkUseSequence = new CheckBox
        {
            Text = "Repeat sequence in loop",
            Location = new Point(12, 32),
            AutoSize = true
        };
        chkUseSequence.CheckedChanged += chkUseSequence_CheckedChanged;

        lblComboInput = new Label
        {
            Text = "Combo to add",
            Location = new Point(12, 58),
            AutoSize = true
        };

        txtComboInput = new TextBox
        {
            Text = "Ctrl+A",
            Location = new Point(12, 80),
            Width = 200
        };

        btnAddSequenceStep = new Button
        {
            Text = "Add current combo",
            Location = new Point(220, 78),
            Width = 128
        };
        btnAddSequenceStep.Click += btnAddSequenceStep_Click;

        btnAddTypedCombo = new Button
        {
            Text = "Add typed combo",
            Location = new Point(220, 108),
            Width = 128
        };
        btnAddTypedCombo.Click += btnAddTypedCombo_Click;

        btnRemoveSequenceStep = new Button
        {
            Text = "Remove selected",
            Location = new Point(12, 168),
            Width = 120
        };
        btnRemoveSequenceStep.Click += btnRemoveSequenceStep_Click;

        btnClearSequence = new Button
        {
            Text = "Clear",
            Location = new Point(140, 168),
            Width = 60
        };
        btnClearSequence.Click += btnClearSequence_Click;

        lstSequence = new ListBox
        {
            Location = new Point(12, 200),
            Size = new Size(336, 120),
            IntegralHeight = true
        };

        var lblSequenceHelp = new Label
        {
            Text = "Examples: Ctrl+A, Shift+Enter, Left Click",
            Location = new Point(12, 330),
            AutoSize = true
        };

        var lblTriggerTitle = new Label
        {
            Text = "Trigger",
            Location = new Point(10, 8),
            AutoSize = true,
            Font = new Font(FontFamily.GenericSansSerif, 10, FontStyle.Bold)
        };

        lblTriggerKey = new Label
        {
            Text = "Key / combo",
            Location = new Point(15, 32),
            AutoSize = true
        };

        btnRecordKey = new Button
        {
            Text = "Record combo",
            Location = new Point(100, 28),
            Width = 110
        };
        btnRecordKey.Click += btnRecordKey_Click;

        lblRecordedKey = new Label
        {
            Text = "Current: Left Click",
            Location = new Point(230, 32),
            AutoSize = true
        };

        btnStart = new Button
        {
            Text = "Start",
            Location = new Point(15, 330),
            Width = 120
        };
        btnStart.Click += btnStart_Click;

        btnStop = new Button
        {
            Text = "Stop",
            Location = new Point(145, 330),
            Width = 90,
            Enabled = false
        };
        btnStop.Click += btnStop_Click;

        lblStatus = new Label
        {
            Text = "Ready",
            Location = new Point(255, 334),
            AutoSize = true
        };

        panelTiming.Controls.Add(lblTimingTitle);
        panelTiming.Controls.Add(lblInitialDelay);
        panelTiming.Controls.Add(nudInitialDelay);
        panelTiming.Controls.Add(lblClickDelay);
        panelTiming.Controls.Add(nudClickDelay);
        panelTiming.Controls.Add(lblRepeat);
        panelTiming.Controls.Add(nudRepeat);
        Controls.Add(panelTiming);

        panelTarget.Controls.Add(lblTargetTitle);
        panelTarget.Controls.Add(lblTarget);
        panelTarget.Controls.Add(chkUseTargetPosition);
        panelTarget.Controls.Add(nudTargetX);
        panelTarget.Controls.Add(nudTargetY);
        panelTarget.Controls.Add(btnUseCurrentPosition);
        Controls.Add(panelTarget);

        panelTrigger.Controls.Add(lblTriggerTitle);
        panelTrigger.Controls.Add(lblTriggerKey);
        panelTrigger.Controls.Add(btnRecordKey);
        panelTrigger.Controls.Add(lblRecordedKey);
        Controls.Add(panelTrigger);

        panelSequence.Controls.Add(lblSequenceTitle);
        panelSequence.Controls.Add(chkUseSequence);
        panelSequence.Controls.Add(lblComboInput);
        panelSequence.Controls.Add(btnAddSequenceStep);
        panelSequence.Controls.Add(btnRemoveSequenceStep);
        panelSequence.Controls.Add(btnClearSequence);
        panelSequence.Controls.Add(lstSequence);
        panelSequence.Controls.Add(lblSequenceHelp);
        Controls.Add(panelSequence);

        Controls.Add(btnStart);
        Controls.Add(btnStop);
        Controls.Add(lblStatus);
    }

    #endregion
}
