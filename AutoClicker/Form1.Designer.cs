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
    private TextBox txtRecordedKey;
    private Button btnRecordKey;
    private Label lblRecordedKey;
    private NumericUpDown nudInitialDelay;
    private NumericUpDown nudClickDelay;
    private NumericUpDown nudRepeat;
    private NumericUpDown nudTargetX;
    private NumericUpDown nudTargetY;
    private CheckBox chkUseTargetPosition;

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
        ClientSize = new Size(420, 320);
        Text = "AutoClicker";

        lblInitialDelay = new Label
        {
            Text = "Initial delay (ms)",
            Location = new Point(20, 25),
            AutoSize = true
        };

        nudInitialDelay = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 10000,
            Value = 0,
            Location = new Point(170, 22),
            Width = 120
        };

        lblClickDelay = new Label
        {
            Text = "Delay between clicks (ms)",
            Location = new Point(20, 60),
            AutoSize = true
        };

        nudClickDelay = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 10000,
            Value = 100,
            Location = new Point(170, 57),
            Width = 120
        };

        lblRepeat = new Label
        {
            Text = "Repeats (0 = infinity)",
            Location = new Point(20, 95),
            AutoSize = true
        };

        nudRepeat = new NumericUpDown
        {
            Minimum = 0,
            Maximum = 100000,
            Value = 0,
            Location = new Point(170, 92),
            Width = 120
        };

        lblTarget = new Label
        {
            Text = "Target position (X, Y)", 
            Location = new Point(20, 130),
            AutoSize = true
        };

        lblTriggerKey = new Label
        {
            Text = "Key to spam",
            Location = new Point(20, 160),
            AutoSize = true
        };

        txtRecordedKey = new TextBox
        {
            Location = new Point(170, 157),
            Width = 120,
            Text = ""
        };

        btnRecordKey = new Button
        {
            Text = "Set key",
            Location = new Point(300, 156),
            Width = 90
        };
        btnRecordKey.Click += btnRecordKey_Click;

        lblRecordedKey = new Label
        {
            Text = "Current: Left Click",
            Location = new Point(20, 190),
            AutoSize = true
        };

        chkUseTargetPosition = new CheckBox
        {
            Text = "Use these coordinates",
            Location = new Point(170, 128),
            AutoSize = true
        };

        nudTargetX = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 100000,
            Value = 0,
            Location = new Point(170, 185),
            Width = 90
        };

        nudTargetY = new NumericUpDown
        {
            Minimum = -100000,
            Maximum = 100000,
            Value = 0,
            Location = new Point(270, 185),
            Width = 90
        };

        btnUseCurrentPosition = new Button
        {
            Text = "Current position",
            Location = new Point(20, 215),
            Width = 140
        };
        btnUseCurrentPosition.Click += btnUseCurrentPosition_Click;

        btnStart = new Button
        {
            Text = "Start",
            Location = new Point(170, 215),
            Width = 120
        };
        btnStart.Click += btnStart_Click;

        btnStop = new Button
        {
            Text = "Stop",
            Location = new Point(300, 215),
            Width = 90,
            Enabled = false
        };
        btnStop.Click += btnStop_Click;

        lblStatus = new Label
        {
            Text = "Ready",
            Location = new Point(20, 270),
            AutoSize = true
        };

        Controls.Add(lblInitialDelay);
        Controls.Add(nudInitialDelay);
        Controls.Add(lblClickDelay);
        Controls.Add(nudClickDelay);
        Controls.Add(lblRepeat);
        Controls.Add(nudRepeat);
        Controls.Add(lblTarget);
        Controls.Add(lblTriggerKey);
        Controls.Add(txtRecordedKey);
        Controls.Add(btnRecordKey);
        Controls.Add(lblRecordedKey);
        Controls.Add(chkUseTargetPosition);
        Controls.Add(nudTargetX);
        Controls.Add(nudTargetY);
        Controls.Add(btnUseCurrentPosition);
        Controls.Add(btnStart);
        Controls.Add(btnStop);
        Controls.Add(lblStatus);
    }

    #endregion
}
