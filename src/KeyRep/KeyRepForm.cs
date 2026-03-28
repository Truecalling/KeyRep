using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using VoK.Sdk.Ddo.Enums;

namespace KeyRep
{
    public class KeyRepForm : Form
    {
        private const int WmNchittest = 0x0084;

        private enum DdoSourceKind
        {
            Hotbar,
            InputActionsName,
            RawUint
        }

        private enum TopMode
        {
            Ddo,
            WindowsKey
        }

        private readonly KeyRepService _repeat;

        private readonly Panel _pnlHeader;
        private readonly Label _lblTitle;
        private readonly Button _btnToggleExpand;
        private readonly Panel _pnlCompact;
        private readonly Label _lblStatusMini;
        private readonly Button _btnStartCompact;
        private readonly Button _btnStopCompact;
        private readonly Panel _pnlBody;
        private readonly FlowLayoutPanel _flowBody;

        private readonly RadioButton _rdoDdo = new() { Text = "DDO (SendInput)", AutoSize = true, Checked = true };
        private readonly RadioButton _rdoWin = new() { Text = "Windows key (OS)", AutoSize = true };

        private readonly RadioButton _rdoSrcHotbar = new() { Text = "Hotbar slot", AutoSize = true, Checked = true };
        private readonly RadioButton _rdoSrcEnum = new() { Text = "InputActions name", AutoSize = true };
        private readonly RadioButton _rdoSrcRaw = new() { Text = "Raw command", AutoSize = true };

        private readonly NumericUpDown _numBar = new() { Minimum = 1, Maximum = 20, Value = 1, Width = 56 };
        private readonly NumericUpDown _numSlot = new() { Minimum = 1, Maximum = 10, Value = 1, Width = 56 };
        private readonly ComboBox _cmbDdo = new() { DropDownStyle = ComboBoxStyle.DropDown, Width = 400 };
        private readonly TextBox _txtRawCmd = new() { Width = 120, PlaceholderText = "0x… or decimal" };

        private readonly ComboBox _cmbModifier = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly ComboBox _cmbModifier2 = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 200 };
        private readonly ComboBox _cmbKey = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 220 };
        private readonly TextBox _txtCustomKey = new() { Width = 120, PlaceholderText = "Keys enum name" };
        private readonly Button _btnCaptureKey = new() { Text = "Capture key…", Tag = "ghost", AutoSize = true, Padding = new Padding(10, 5, 10, 5) };
        private readonly Label _lblCaptureHint = new()
        {
            Visible = false,
            AutoSize = true,
            MaximumSize = new Size(500, 0),
            Text = "Click Capture, then press the shortcut here. One list row = one physical key (one VK). Esc cancels."
        };
        private bool _captureKeyActive;
        private readonly CheckBox _chkRelease = new() { Text = "ReleaseInput after send (toggles)", AutoSize = true };

        private readonly NumericUpDown _numIntervalMin = new() { Width = 88 };
        private readonly NumericUpDown _numIntervalMax = new() { Width = 88 };
        private readonly TrackBar _trkIntervalMin = new();
        private readonly TrackBar _trkIntervalMax = new();
        private bool _intervalWireSync;

        private readonly CheckBox _chkInfinite = new() { Text = "Repeat forever", AutoSize = true };
        private readonly NumericUpDown _numSeconds = new() { Minimum = 1, Maximum = 86400, Value = 60, Width = 64 };
        private readonly TrackBar _trkDuration = new();

        private readonly Button _btnStart = new() { Text = "Start", AutoSize = true, Padding = new Padding(12, 5, 12, 5) };
        private readonly Button _btnStop = new() { Text = "Stop", Enabled = false, AutoSize = true, Padding = new Padding(12, 5, 12, 5) };
        private readonly Button _btnTestOnce = new() { Text = "Test once", AutoSize = true, Padding = new Padding(10, 5, 10, 5) };
        private readonly Label _lblStatus = new() { AutoSize = true, Text = "Idle.", MaximumSize = new Size(520, 0) };
        private readonly Label _lblDh = new() { AutoSize = true, MaximumSize = new Size(520, 0) };

        private readonly System.Windows.Forms.Timer _statusTimer;
        private readonly System.Windows.Forms.Timer _repeatTimer;

        private readonly FlowLayoutPanel _pnlTopMode;
        private readonly FlowLayoutPanel _pnlDdoBlock;
        private readonly FlowLayoutPanel _pnlDdoSource;
        private readonly FlowLayoutPanel _pnlDdoDetails;
        private readonly FlowLayoutPanel _pnlWinBlock;

        private bool _expanded = true;
        private bool _repeatRunning;
        private TopMode _runTopMode;
        private DdoSourceKind _runDdoSource;
        private uint _runFixedDdoCommand;
        private int _runBar1;
        private int _runSlot1;
        private int _runWinKey;
        private int _runModifierKey;
        private int _runModifierKey2;
        private int _runIntervalMin;
        private int _runIntervalMax;
        private bool _runSendRelease;
        private DateTime? _runEndUtc;

        public KeyRepForm(KeyRepService repeat)
        {
            _repeat = repeat;
            Text = "KeyRep";
            FormBorderStyle = FormBorderStyle.FixedSingle;
            ControlBox = false;
            MaximizeBox = false;
            MinimizeBox = false;
            ShowIcon = false;
            ShowInTaskbar = false;
            StartPosition = FormStartPosition.Manual;
            AutoSize = false;
            MinimumSize = new Size(540, 560);
            ClientSize = new Size(580, 680);
            Padding = new Padding(0);
            DoubleBuffered = true;
            KeyPreview = true;

            _lblTitle = new Label
            {
                Text = "KeyRep",
                AutoSize = true,
                Tag = "title",
                Padding = new Padding(10, 8, 6, 6)
            };

            _btnToggleExpand = new Button
            {
                Text = "Collapse UI",
                Tag = "ghost",
                AutoSize = true,
                Padding = new Padding(10, 5, 10, 5),
                Margin = new Padding(6, 6, 10, 6)
            };
            _btnToggleExpand.Click += (_, _) => SetExpanded(!_expanded);

            _pnlHeader = new Panel
            {
                Height = 44,
                Dock = DockStyle.Top,
                Tag = "header",
                Padding = new Padding(0)
            };
            var headerFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = false,
                Padding = new Padding(0),
                Margin = new Padding(0)
            };
            var headerLogo = BrandAssets.TryLoadLogoPng(36);
            if (headerLogo != null)
            {
                var pic = new PictureBox
                {
                    Image = headerLogo,
                    SizeMode = PictureBoxSizeMode.Zoom,
                    Size = new Size(36, 36),
                    Margin = new Padding(10, 4, 6, 4)
                };
                headerFlow.Controls.Add(pic);
            }

            headerFlow.Controls.Add(_lblTitle);
            headerFlow.Controls.Add(_btnToggleExpand);
            _pnlHeader.Controls.Add(headerFlow);

            _lblStatusMini = new Label { AutoSize = true, Text = "Idle.", Padding = new Padding(10, 6, 6, 6) };
            _btnStartCompact = new Button { Text = "Start", Tag = "accent", AutoSize = true, Padding = new Padding(10, 5, 10, 5) };
            _btnStopCompact = new Button { Text = "Stop", Tag = "ghost", Enabled = false, AutoSize = true, Padding = new Padding(10, 5, 10, 5) };
            _btnStartCompact.Click += (_, _) => StartClicked();
            _btnStopCompact.Click += (_, _) => StopRepeat();

            _pnlCompact = new Panel { Dock = DockStyle.Fill, Visible = false, Padding = new Padding(0) };
            var compactFlow = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill,
                FlowDirection = FlowDirection.LeftToRight,
                WrapContents = true,
                Padding = new Padding(6, 6, 6, 6)
            };
            compactFlow.Controls.Add(_lblStatusMini);
            compactFlow.Controls.Add(_btnStartCompact);
            compactFlow.Controls.Add(_btnStopCompact);
            _pnlCompact.Controls.Add(compactFlow);

            _flowBody = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                AutoSize = true,
                AutoSizeMode = AutoSizeMode.GrowAndShrink,
                FlowDirection = FlowDirection.TopDown,
                WrapContents = false,
                Padding = new Padding(10, 4, 10, 8)
            };

            _pnlBody = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true,
                Padding = new Padding(0),
                Visible = true
            };
            _pnlBody.Controls.Add(_flowBody);
            _pnlBody.Resize += (_, _) => SyncBodyFlowWidth();

            PopulateDdoCombo();
            PopulateModifierCombo(_cmbModifier);
            PopulateModifierCombo(_cmbModifier2);
            PopulateKeyCombo();

            if (!KeyRepService.WindowsKeySendSupported)
                _rdoWin.Enabled = false;

            SetupIntervalSecondsBinding();
            WireTrackBarAndNumeric(_trkDuration, _numSeconds, 3600);

            _pnlTopMode = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.LeftToRight,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            _pnlTopMode.Controls.Add(_rdoDdo);
            _pnlTopMode.Controls.Add(_rdoWin);

            _pnlDdoSource = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            _pnlDdoSource.Controls.Add(_rdoSrcHotbar);
            _pnlDdoSource.Controls.Add(_rdoSrcEnum);
            _pnlDdoSource.Controls.Add(_rdoSrcRaw);

            _pnlDdoDetails = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            _pnlDdoDetails.Controls.Add(LabelRow("Bar:", _numBar));
            _pnlDdoDetails.Controls.Add(LabelRow("Slot:", _numSlot));
            _pnlDdoDetails.Controls.Add(LabelRow("InputActions:", _cmbDdo));
            _pnlDdoDetails.Controls.Add(LabelRow("Command:", _txtRawCmd));
            _pnlDdoDetails.Controls.Add(_chkRelease);

            _pnlDdoBlock = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 0)
            };
            _pnlDdoBlock.Controls.Add(_pnlDdoSource);
            _pnlDdoBlock.Controls.Add(_pnlDdoDetails);

            var pnlWinKeys = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 2, 0, 0)
            };
            pnlWinKeys.Controls.Add(LabelRow("Modifier 1:", _cmbModifier));
            pnlWinKeys.Controls.Add(LabelRow("Modifier 2:", _cmbModifier2));
            pnlWinKeys.Controls.Add(LabelRow("Key:", _cmbKey));
            var customFlow = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 0, 0, 0) };
            customFlow.Controls.Add(new Label { Text = "Custom:", AutoSize = true, Padding = new Padding(0, 5, 6, 0) });
            customFlow.Controls.Add(_txtCustomKey);
            pnlWinKeys.Controls.Add(customFlow);
            _btnCaptureKey.Click += (_, _) => ToggleCaptureKeyMode();
            pnlWinKeys.Controls.Add(_btnCaptureKey);
            pnlWinKeys.Controls.Add(_lblCaptureHint);

            _pnlWinBlock = new FlowLayoutPanel
            {
                FlowDirection = FlowDirection.TopDown,
                AutoSize = true,
                WrapContents = false,
                Margin = new Padding(0, 0, 0, 0),
                Visible = false
            };
            _pnlWinBlock.Controls.Add(pnlWinKeys);

            _rdoDdo.CheckedChanged += (_, _) => SyncModeUi();
            _rdoWin.CheckedChanged += (_, _) => SyncModeUi();
            _rdoSrcHotbar.CheckedChanged += (_, _) => SyncModeUi();
            _rdoSrcEnum.CheckedChanged += (_, _) => SyncModeUi();
            _rdoSrcRaw.CheckedChanged += (_, _) => SyncModeUi();
            SyncModeUi();

            _chkInfinite.CheckedChanged += (_, _) =>
            {
                _numSeconds.Enabled = !_chkInfinite.Checked;
                _trkDuration.Enabled = !_chkInfinite.Checked;
            };
            _numSeconds.Enabled = !_chkInfinite.Checked;
            _trkDuration.Enabled = !_chkInfinite.Checked;

            _btnStart.Tag = "accent";
            _btnStop.Tag = "ghost";
            _btnTestOnce.Tag = "ghost";
            _btnStart.Click += (_, _) => StartClicked();
            _btnStop.Click += (_, _) => StopRepeat();
            _btnTestOnce.Click += (_, _) => TestOnceClicked();

            _repeatTimer = new System.Windows.Forms.Timer();
            _repeatTimer.Tick += RepeatTimerOnTick;

            _statusTimer = new System.Windows.Forms.Timer { Interval = 400 };
            _statusTimer.Tick += (_, _) => RefreshStatusLine();
            _statusTimer.Start();

            _flowBody.Controls.Add(SectionLabel("Mode"));
            _flowBody.Controls.Add(_pnlTopMode);
            _flowBody.Controls.Add(_pnlDdoBlock);
            _flowBody.Controls.Add(_pnlWinBlock);
            _flowBody.Controls.Add(_lblDh);

            _flowBody.Controls.Add(SectionLabel("Delay"));
            _flowBody.Controls.Add(DualIntervalRow(_trkIntervalMin, _numIntervalMin, _trkIntervalMax, _numIntervalMax, 540));
            _flowBody.Controls.Add(_chkInfinite);
            _flowBody.Controls.Add(SliderNumericRow("Duration (s)", _trkDuration, _numSeconds, 540));

            var buttons = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 6, 0, 0) };
            buttons.Controls.Add(_btnStart);
            buttons.Controls.Add(_btnStop);
            buttons.Controls.Add(_btnTestOnce);
            _flowBody.Controls.Add(buttons);
            _flowBody.Controls.Add(_lblStatus);

            var mainFill = new Panel { Dock = DockStyle.Fill, Padding = new Padding(0) };
            mainFill.Controls.Add(_pnlBody);
            mainFill.Controls.Add(_pnlCompact);

            Controls.Add(mainFill);
            Controls.Add(_pnlHeader);

            UiTheme.ApplyRoot(this);

            Load += (_, _) =>
            {
                SyncBodyFlowWidth();
                RefreshStatusLine();
            };

            FormClosing += (_, e) =>
            {
                if (e.CloseReason == CloseReason.UserClosing && _repeatRunning)
                {
                    var r = MessageBox.Show(this, "Key repeat is still running. Stop and close?", "KeyRep", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (r != DialogResult.Yes)
                    {
                        e.Cancel = true;
                        return;
                    }
                }

                EndCaptureKeyMode();
                StopRepeat();
                _repeatTimer.Stop();
                _statusTimer.Stop();
                _statusTimer.Dispose();
                _repeatTimer.Dispose();
            };
        }

        protected override void OnPreviewKeyDown(PreviewKeyDownEventArgs e)
        {
            if (_captureKeyActive)
                e.IsInputKey = true;
            base.OnPreviewKeyDown(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            if (_captureKeyActive)
            {
                if (e.KeyCode == Keys.Escape)
                {
                    EndCaptureKeyMode();
                    SetStatus("Capture cancelled.");
                    e.Handled = true;
                    e.SuppressKeyPress = true;
                    base.OnKeyDown(e);
                    return;
                }

                if (IsModifierOnlyKeyCode(e.KeyCode))
                {
                    base.OnKeyDown(e);
                    return;
                }

                ApplyCapturedChord(e);
                e.Handled = true;
                e.SuppressKeyPress = true;
                base.OnKeyDown(e);
                return;
            }

            base.OnKeyDown(e);
        }

        private static bool IsModifierOnlyKeyCode(Keys key) =>
            key is Keys.ShiftKey or Keys.ControlKey or Keys.Menu
                or Keys.LShiftKey or Keys.RShiftKey
                or Keys.LControlKey or Keys.RControlKey
                or Keys.LMenu or Keys.RMenu;

        private void ToggleCaptureKeyMode()
        {
            if (!KeyRepService.WindowsKeySendSupported || _rdoDdo.Checked)
                return;

            if (_captureKeyActive)
            {
                EndCaptureKeyMode();
                SetStatus("Capture cancelled.");
                return;
            }

            _captureKeyActive = true;
            _btnCaptureKey.Text = "Cancel capture";
            _lblCaptureHint.Visible = true;
            Focus();
            SetStatus("Listening: press key or Ctrl/Alt/Shift+key. Esc=cancel. (Max 2 modifiers.)");
        }

        private void EndCaptureKeyMode()
        {
            _captureKeyActive = false;
            _btnCaptureKey.Text = "Capture key…";
            _lblCaptureHint.Visible = false;
        }

        private void ApplyCapturedChord(KeyEventArgs e)
        {
            var mods = new List<int>();
            if (e.Control)
                mods.Add((int)Keys.LControlKey);
            if (e.Alt)
                mods.Add((int)Keys.LMenu);
            if (e.Shift)
                mods.Add((int)Keys.LShiftKey);

            var m1 = mods.Count > 0 ? mods[0] : 0;
            var m2 = mods.Count > 1 ? mods[1] : 0;

            SelectModifierCombo(_cmbModifier, m1);
            SelectModifierCombo(_cmbModifier2, m2);

            _txtCustomKey.Text = e.KeyCode.ToString();
            _cmbKey.SelectedIndex = -1;

            EndCaptureKeyMode();
            SetStatus(string.Format(CultureInfo.InvariantCulture, "Captured {0} into Custom (dropdown cleared).", e.KeyCode));
        }

        private static void SelectModifierCombo(ComboBox cmb, int virtualKeyCode)
        {
            if (virtualKeyCode == 0)
            {
                cmb.SelectedIndex = 0;
                return;
            }

            for (var i = 0; i < cmb.Items.Count; i++)
            {
                if (cmb.Items[i] is KeyOption ko && ko.VirtualKeyCode == virtualKeyCode)
                {
                    cmb.SelectedIndex = i;
                    return;
                }
            }

            cmb.SelectedIndex = 0;
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WmNchittest)
            {
                base.WndProc(ref m);
                if (m.Result == (IntPtr)2)
                    m.Result = (IntPtr)1;
                return;
            }

            base.WndProc(ref m);
        }

        private void SetExpanded(bool expanded)
        {
            _expanded = expanded;
            _pnlBody.Visible = expanded;
            _pnlCompact.Visible = !expanded;
            _btnToggleExpand.Text = expanded ? "Collapse UI" : "Expand UI";
            MinimumSize = expanded ? new Size(540, 560) : new Size(300, 110);
            ClientSize = expanded ? new Size(Math.Max(ClientSize.Width, 580), Math.Max(ClientSize.Height, 680)) : new Size(Math.Max(ClientSize.Width, 340), 108);
            BeginInvoke(new Action(SyncBodyFlowWidth));
        }

        private void SyncBodyFlowWidth()
        {
            if (_pnlBody.IsDisposed || _flowBody.IsDisposed)
                return;
            var w = _pnlBody.ClientSize.Width;
            if (w < 40)
                return;
            _flowBody.Width = w;
        }

        private static Label SectionLabel(string text) =>
            new()
            {
                Text = text,
                AutoSize = true,
                Tag = "section",
                Margin = new Padding(0, 8, 0, 2)
            };

        private static TableLayoutPanel DualIntervalRow(TrackBar trkMin, NumericUpDown numMin, TrackBar trkMax, NumericUpDown numMax, int contentWidth)
        {
            var outer = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 4),
                Width = contentWidth
            };
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            outer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50f));
            outer.Controls.Add(new Label { Text = "Delay min (seconds)", AutoSize = true, Margin = new Padding(0, 0, 0, 2) }, 0, 0);
            outer.Controls.Add(new Label { Text = "Delay max (seconds)", AutoSize = true, Margin = new Padding(0, 0, 0, 2) }, 1, 0);
            outer.Controls.Add(SliderNumericCell(trkMin, numMin), 0, 1);
            outer.Controls.Add(SliderNumericCell(trkMax, numMax), 1, 1);
            return outer;
        }

        private static TableLayoutPanel SliderNumericCell(TrackBar trk, NumericUpDown num)
        {
            var row = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 2,
                RowCount = 1,
                Margin = new Padding(0, 0, 6, 0),
                Dock = DockStyle.Fill
            };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 92f));
            trk.Dock = DockStyle.Fill;
            trk.Margin = new Padding(0, 2, 6, 2);
            num.Margin = new Padding(0, 4, 0, 0);
            num.TextAlign = HorizontalAlignment.Right;
            row.Controls.Add(trk, 0, 0);
            row.Controls.Add(num, 1, 0);
            return row;
        }

        private static TableLayoutPanel SliderNumericRow(string caption, TrackBar trk, NumericUpDown num, int contentWidth)
        {
            var t = new TableLayoutPanel
            {
                AutoSize = true,
                ColumnCount = 1,
                RowCount = 2,
                Margin = new Padding(0, 0, 0, 4),
                Width = contentWidth
            };
            t.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
            var cap = new Label { Text = caption, AutoSize = true, Margin = new Padding(0, 0, 0, 2) };
            var row = SliderNumericCell(trk, num);
            t.Controls.Add(cap, 0, 0);
            t.Controls.Add(row, 0, 1);
            return t;
        }

        private void SetupIntervalSecondsBinding()
        {
            foreach (var trk in new[] { _trkIntervalMin, _trkIntervalMax })
            {
                trk.Minimum = 5;
                trk.Maximum = 32767;
                trk.TickFrequency = Math.Max(1, (trk.Maximum - trk.Minimum) / 12);
                trk.LargeChange = Math.Max(1, (trk.Maximum - trk.Minimum) / 15);
                trk.SmallChange = Math.Max(1, (trk.Maximum - trk.Minimum) / 60);
            }

            _numIntervalMin.DecimalPlaces = 2;
            _numIntervalMin.Increment = 0.05m;
            _numIntervalMin.Minimum = 0.05m;
            _numIntervalMin.Maximum = 3600m;
            _numIntervalMin.Value = 0.2m;

            _numIntervalMax.DecimalPlaces = 2;
            _numIntervalMax.Increment = 0.05m;
            _numIntervalMax.Minimum = 0.05m;
            _numIntervalMax.Maximum = 3600m;
            _numIntervalMax.Value = 0.4m;

            NormalizeIntervalControls();

            _trkIntervalMin.ValueChanged += (_, _) => IntervalMinFromTrackBar();
            _numIntervalMin.ValueChanged += (_, _) => IntervalMinFromNumeric();
            _trkIntervalMax.ValueChanged += (_, _) => IntervalMaxFromTrackBar();
            _numIntervalMax.ValueChanged += (_, _) => IntervalMaxFromNumeric();
        }

        private void NormalizeIntervalControls()
        {
            _intervalWireSync = true;
            try
            {
                _numIntervalMin.Value = decimal.Round(_numIntervalMin.Value, 2);
                _numIntervalMax.Value = decimal.Round(_numIntervalMax.Value, 2);
                var csMin = Math.Clamp((int)Math.Round((double)_numIntervalMin.Value * 100), 5, 32767);
                _trkIntervalMin.Value = csMin;
                ApplyMinIncreasedRulesWhileSyncing();
            }
            finally
            {
                _intervalWireSync = false;
            }
        }

        private void ApplyMinIncreasedRulesWhileSyncing()
        {
            _numIntervalMax.Minimum = _numIntervalMin.Value;
            if (_numIntervalMax.Value < _numIntervalMin.Value)
                _numIntervalMax.Value = _numIntervalMin.Value;

            var minCs = Math.Clamp((int)Math.Round((double)_numIntervalMin.Value * 100), 5, 32767);
            _trkIntervalMax.Minimum = minCs;
            if (_trkIntervalMax.Value < minCs)
                _trkIntervalMax.Value = minCs;

            var maxCs = Math.Clamp((int)Math.Round((double)_numIntervalMax.Value * 100), minCs, 32767);
            if (_trkIntervalMax.Value != maxCs)
                _trkIntervalMax.Value = maxCs;
            _numIntervalMax.Value = decimal.Round(_trkIntervalMax.Value / 100m, 2);
        }

        private void IntervalMinFromNumeric()
        {
            if (_intervalWireSync)
                return;
            _intervalWireSync = true;
            try
            {
                var cs = Math.Clamp((int)Math.Round((double)_numIntervalMin.Value * 100), 5, 32767);
                _trkIntervalMin.Value = cs;
                ApplyMinIncreasedRulesWhileSyncing();
            }
            finally
            {
                _intervalWireSync = false;
            }
        }

        private void IntervalMinFromTrackBar()
        {
            if (_intervalWireSync)
                return;
            _intervalWireSync = true;
            try
            {
                _numIntervalMin.Value = decimal.Round(_trkIntervalMin.Value / 100m, 2);
                ApplyMinIncreasedRulesWhileSyncing();
            }
            finally
            {
                _intervalWireSync = false;
            }
        }

        private void IntervalMaxFromNumeric()
        {
            if (_intervalWireSync)
                return;
            _intervalWireSync = true;
            try
            {
                if (_numIntervalMax.Value < _numIntervalMin.Value)
                    _numIntervalMax.Value = _numIntervalMin.Value;
                var minCs = Math.Clamp((int)Math.Round((double)_numIntervalMin.Value * 100), 5, 32767);
                var cs = Math.Clamp((int)Math.Round((double)_numIntervalMax.Value * 100), minCs, 32767);
                _trkIntervalMax.Value = cs;
                _numIntervalMax.Value = decimal.Round(_trkIntervalMax.Value / 100m, 2);
            }
            finally
            {
                _intervalWireSync = false;
            }
        }

        private void IntervalMaxFromTrackBar()
        {
            if (_intervalWireSync)
                return;
            _intervalWireSync = true;
            try
            {
                _numIntervalMax.Value = decimal.Round(_trkIntervalMax.Value / 100m, 2);
                if (_numIntervalMax.Value < _numIntervalMin.Value)
                {
                    _numIntervalMax.Value = _numIntervalMin.Value;
                    var minCs = Math.Clamp((int)Math.Round((double)_numIntervalMin.Value * 100), 5, 32767);
                    _trkIntervalMax.Value = minCs;
                }
            }
            finally
            {
                _intervalWireSync = false;
            }
        }

        private static int SecondsToIntervalMs(decimal seconds)
        {
            var ms = (int)Math.Round((double)seconds * 1000.0);
            return Math.Max(1, ms);
        }

        private static void WireTrackBarAndNumeric(TrackBar trk, NumericUpDown num, int sliderMax)
        {
            var smax = Math.Min(sliderMax, (int)num.Maximum);
            trk.Minimum = (int)num.Minimum;
            trk.Maximum = Math.Max(trk.Minimum, smax);
            trk.TickFrequency = Math.Max(1, (trk.Maximum - trk.Minimum) / 12);
            trk.LargeChange = Math.Max(1, (trk.Maximum - trk.Minimum) / 15);
            trk.SmallChange = Math.Max(1, (trk.Maximum - trk.Minimum) / 60);

            var syncing = false;

            void numToTrk()
            {
                syncing = true;
                var v = (int)num.Value;
                trk.Value = Math.Clamp(v, trk.Minimum, trk.Maximum);
                syncing = false;
            }

            void trkToNum()
            {
                syncing = true;
                num.Value = Math.Clamp(trk.Value, num.Minimum, num.Maximum);
                syncing = false;
            }

            trk.ValueChanged += (_, _) =>
            {
                if (!syncing)
                    trkToNum();
            };
            num.ValueChanged += (_, _) =>
            {
                if (!syncing)
                    numToTrk();
            };
            numToTrk();
        }

        private void SetStatus(string text)
        {
            _lblStatus.Text = text;
            _lblStatusMini.Text = text;
        }

        private void RefreshStatusLine()
        {
            if (IsDisposed)
                return;
            _lblDh.Text = "DH HotKeysEnabled: " + (_repeat.HotKeysEnabled ? "true" : "false");
        }

        private void SyncModeUi()
        {
            var ddo = _rdoDdo.Checked;
            _pnlDdoBlock.Visible = ddo;
            _pnlWinBlock.Visible = !ddo;

            if (ddo)
            {
                EndCaptureKeyMode();
                _btnCaptureKey.Enabled = false;
                _numBar.Enabled = _rdoSrcHotbar.Checked;
                _numSlot.Enabled = _rdoSrcHotbar.Checked;
                _cmbDdo.Enabled = _rdoSrcEnum.Checked;
                _txtRawCmd.Enabled = _rdoSrcRaw.Checked;
            }
            else
            {
                _btnCaptureKey.Enabled = KeyRepService.WindowsKeySendSupported;
            }
        }

        private static Control LabelRow(string text, Control right)
        {
            var p = new FlowLayoutPanel { AutoSize = true, FlowDirection = FlowDirection.LeftToRight, Margin = new Padding(0, 1, 0, 0) };
            p.Controls.Add(new Label { Text = text, AutoSize = true, Padding = new Padding(0, 5, 6, 0) });
            p.Controls.Add(right);
            return p;
        }

        private void PopulateDdoCombo()
        {
            var names = Enum.GetNames(typeof(InputActions))
                .Where(n => n.Contains("SHORTCUT_BAR", StringComparison.OrdinalIgnoreCase)
                            || n.Contains("PRIMARY", StringComparison.OrdinalIgnoreCase)
                            || n.Contains("ATTACK", StringComparison.OrdinalIgnoreCase))
                .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
                .Take(160)
                .ToList();

            foreach (var n in names)
                _cmbDdo.Items.Add(n);

            var pick = names.FirstOrDefault(n => string.Equals(n, "SHORTCUT_BAR1_KEY1", StringComparison.OrdinalIgnoreCase));
            if (pick != null)
                _cmbDdo.Text = pick;
            else if (_cmbDdo.Items.Count > 0)
                _cmbDdo.Text = (string)_cmbDdo.Items[0]!;
        }

        private static void PopulateModifierCombo(ComboBox cmb)
        {
            foreach (var o in ModifierOptions)
                cmb.Items.Add(o);
            cmb.SelectedIndex = 0;
        }

        private static readonly KeyOption[] ModifierOptions =
        {
            new("None", 0),
            new("Ctrl — left", (int)Keys.LControlKey),
            new("Ctrl — right", (int)Keys.RControlKey),
            new("Alt — left", (int)Keys.LMenu),
            new("Alt — right", (int)Keys.RMenu),
            new("Shift — left", (int)Keys.LShiftKey),
            new("Shift — right", (int)Keys.RShiftKey),
            new("Win — left", (int)Keys.LWin),
            new("Win — right", (int)Keys.RWin),
        };

        private void PopulateKeyCombo()
        {
            foreach (var o in WinKeyCatalog())
                _cmbKey.Items.Add(o);
            if (_cmbKey.Items.Count > 0)
                _cmbKey.SelectedIndex = 0;
        }

        private sealed class KeyOption
        {
            public KeyOption(string label, int virtualKeyCode)
            {
                Label = label;
                VirtualKeyCode = virtualKeyCode;
            }

            public string Label { get; }
            public int VirtualKeyCode { get; }
            public override string ToString() => Label;
        }

        private static IEnumerable<KeyOption> WinKeyCatalog()
        {
            yield return new KeyOption("Space", (int)Keys.Space);
            yield return new KeyOption("Tab", (int)Keys.Tab);
            yield return new KeyOption("Enter", (int)Keys.Enter);
            yield return new KeyOption("Escape", (int)Keys.Escape);
            yield return new KeyOption("Backspace", (int)Keys.Back);

            for (var i = 1; i <= 12; i++)
            {
                var fk = (Keys)((int)Keys.F1 + (i - 1));
                yield return new KeyOption("F" + i.ToString(CultureInfo.InvariantCulture), (int)fk);
            }

            for (var d = 1; d <= 9; d++)
                yield return new KeyOption(d.ToString(CultureInfo.InvariantCulture), (int)Keys.D0 + d);
            yield return new KeyOption("0 (digit row)", (int)Keys.D0);

            for (var c = 'A'; c <= 'Z'; c++)
                yield return new KeyOption(c.ToString(), (int)c);

            yield return new KeyOption("Oemtilde (` key, 1 VK)", (int)Keys.Oemtilde);
            yield return new KeyOption("OemMinus (- key, 1 VK)", (int)Keys.OemMinus);
            yield return new KeyOption("OemPlus (= key, 1 VK)", 0xBB);
            yield return new KeyOption("OemOpenBrackets ([ key, 1 VK)", (int)Keys.OemOpenBrackets);
            yield return new KeyOption("OemCloseBrackets (] key, 1 VK)", (int)Keys.OemCloseBrackets);
            yield return new KeyOption("OemPipe (\\ key, 1 VK)", (int)Keys.OemPipe);
            yield return new KeyOption("OemSemicolon (; key, 1 VK)", (int)Keys.OemSemicolon);
            yield return new KeyOption("OemQuotes (' key, 1 VK)", (int)Keys.OemQuotes);
            yield return new KeyOption("Oemcomma (, key, 1 VK)", (int)Keys.Oemcomma);
            yield return new KeyOption("OemPeriod (. key, 1 VK)", (int)Keys.OemPeriod);
            yield return new KeyOption("OemQuestion (/ key, 1 VK)", (int)Keys.OemQuestion);

            yield return new KeyOption("Insert", (int)Keys.Insert);
            yield return new KeyOption("Delete", (int)Keys.Delete);
            yield return new KeyOption("Home", (int)Keys.Home);
            yield return new KeyOption("End", (int)Keys.End);
            yield return new KeyOption("Page Up", (int)Keys.Prior);
            yield return new KeyOption("Page Down", (int)Keys.Next);
            yield return new KeyOption("Arrow Up", (int)Keys.Up);
            yield return new KeyOption("Arrow Down", (int)Keys.Down);
            yield return new KeyOption("Arrow Left", (int)Keys.Left);
            yield return new KeyOption("Arrow Right", (int)Keys.Right);

            yield return new KeyOption("Pause (Break, 1 VK)", (int)Keys.Pause);
            yield return new KeyOption("Caps Lock", (int)Keys.CapsLock);
            yield return new KeyOption("Scroll Lock", (int)Keys.Scroll);
            yield return new KeyOption("Print Screen", (int)Keys.PrintScreen);
            yield return new KeyOption("Apps (context menu key)", (int)Keys.Apps);

            yield return new KeyOption("Num Lock", (int)Keys.NumLock);
            yield return new KeyOption("Numpad /", (int)Keys.Divide);
            yield return new KeyOption("Numpad *", (int)Keys.Multiply);
            yield return new KeyOption("Numpad -", (int)Keys.Subtract);
            yield return new KeyOption("Numpad +", (int)Keys.Add);
            yield return new KeyOption("Numpad .", (int)Keys.Decimal);
            for (var n = 0; n <= 9; n++)
            {
                var nk = (Keys)((int)Keys.NumPad0 + n);
                yield return new KeyOption("Numpad " + n.ToString(CultureInfo.InvariantCulture), (int)nk);
            }

            yield return new KeyOption("Left Ctrl (as key)", (int)Keys.LControlKey);
            yield return new KeyOption("Right Ctrl (as key)", (int)Keys.RControlKey);
            yield return new KeyOption("Left Alt (as key)", (int)Keys.LMenu);
            yield return new KeyOption("Right Alt (as key)", (int)Keys.RMenu);
            yield return new KeyOption("Left Shift (as key)", (int)Keys.LShiftKey);
            yield return new KeyOption("Right Shift (as key)", (int)Keys.RShiftKey);
            yield return new KeyOption("Left Win (as key)", (int)Keys.LWin);
            yield return new KeyOption("Right Win (as key)", (int)Keys.RWin);
        }

        private int ResolveModifierCode(ComboBox cmb)
        {
            if (cmb.SelectedItem is KeyOption mo)
                return mo.VirtualKeyCode;
            return 0;
        }

        private int ResolveKeyCode()
        {
            var custom = _txtCustomKey.Text.Trim();
            if (custom.Length > 0 && Enum.TryParse<Keys>(custom, ignoreCase: true, out var parsed))
                return (int)parsed;

            if (_cmbKey.SelectedItem is KeyOption sel)
                return sel.VirtualKeyCode;

            return (int)Keys.Space;
        }

        private DdoSourceKind CurrentDdoSource()
        {
            if (_rdoSrcEnum.Checked)
                return DdoSourceKind.InputActionsName;
            if (_rdoSrcRaw.Checked)
                return DdoSourceKind.RawUint;
            return DdoSourceKind.Hotbar;
        }

        private bool TryResolveDdoCommand(out uint cmd, out string? err)
        {
            err = null;
            cmd = 0;
            switch (CurrentDdoSource())
            {
                case DdoSourceKind.Hotbar:
                    return _repeat.TryGetHotbarCommand((int)_numBar.Value, (int)_numSlot.Value, out cmd, out err);
                case DdoSourceKind.InputActionsName:
                {
                    var name = _cmbDdo.Text.Trim();
                    if (name.Length == 0 || !Enum.TryParse<InputActions>(name, ignoreCase: true, out var ia))
                    {
                        err = "Invalid InputActions name.";
                        return false;
                    }

                    cmd = Convert.ToUInt32(ia);
                    return true;
                }
                case DdoSourceKind.RawUint:
                {
                    var t = _txtRawCmd.Text.Trim();
                    if (t.Length == 0)
                    {
                        err = "Enter a raw command number.";
                        return false;
                    }

                    if (t.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
                    {
                        if (!uint.TryParse(t[2..], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out cmd))
                        {
                            err = "Bad hex value.";
                            return false;
                        }

                        return true;
                    }

                    if (!uint.TryParse(t, NumberStyles.Integer, CultureInfo.InvariantCulture, out cmd))
                    {
                        err = "Bad decimal value.";
                        return false;
                    }

                    return true;
                }
                default:
                    err = "Unknown source.";
                    return false;
            }
        }

        private void TestOnceClicked()
        {
            if (_captureKeyActive)
                EndCaptureKeyMode();

            if (_rdoDdo.Checked)
            {
                if (!TryResolveDdoCommand(out var cmd, out var err))
                {
                    MessageBox.Show(this, err ?? "Error", "KeyRep", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _repeat.SendDdo(cmd, _chkRelease.Checked);
                SetStatus(string.Format(CultureInfo.InvariantCulture, "Test: SendInput({0})", cmd));
            }
            else
            {
                if (!KeyRepService.WindowsKeySendSupported)
                {
                    MessageBox.Show(this, "Windows key mode unavailable.", "KeyRep", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var m1 = ResolveModifierCode(_cmbModifier);
                var m2 = ResolveModifierCode(_cmbModifier2);
                var k = ResolveKeyCode();
                _repeat.SendWindowsKey(m1, m2, k);
                SetStatus(string.Format(CultureInfo.InvariantCulture, "Test: mod1={0} mod2={1} key={2}", m1, m2, k));
            }
        }

        private static int NextTimerIntervalMs(int min, int max)
        {
            if (max < min)
                (min, max) = (max, min);
            if (min < 1)
                min = 1;
            return min == max ? min : Random.Shared.Next(min, max + 1);
        }

        private void StartClicked()
        {
            if (_repeatRunning)
                return;

            if (_captureKeyActive)
                EndCaptureKeyMode();

            NormalizeIntervalControls();
            var minMs = SecondsToIntervalMs(_numIntervalMin.Value);
            var maxMs = SecondsToIntervalMs(_numIntervalMax.Value);
            if (maxMs < minMs)
                maxMs = minMs;
            _runIntervalMin = minMs;
            _runIntervalMax = maxMs;

            if (_rdoDdo.Checked)
            {
                if (!TryResolveDdoCommand(out var cmd, out var err))
                {
                    MessageBox.Show(this, err ?? "Error", "KeyRep", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _runTopMode = TopMode.Ddo;
                _runDdoSource = CurrentDdoSource();
                _runFixedDdoCommand = cmd;
                _runBar1 = (int)_numBar.Value;
                _runSlot1 = (int)_numSlot.Value;
                _runSendRelease = _chkRelease.Checked;
            }
            else
            {
                if (!KeyRepService.WindowsKeySendSupported)
                {
                    MessageBox.Show(this, "Windows key mode unavailable.", "KeyRep", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _runTopMode = TopMode.WindowsKey;
                _runModifierKey = ResolveModifierCode(_cmbModifier);
                _runModifierKey2 = ResolveModifierCode(_cmbModifier2);
                _runWinKey = ResolveKeyCode();
            }

            _runEndUtc = _chkInfinite.Checked ? null : DateTime.UtcNow.AddSeconds((int)_numSeconds.Value);
            _repeatTimer.Interval = NextTimerIntervalMs(_runIntervalMin, _runIntervalMax);
            _repeatTimer.Start();
            _repeatRunning = true;
            _btnStart.Enabled = false;
            _btnStop.Enabled = true;
            _btnStartCompact.Enabled = false;
            _btnStopCompact.Enabled = true;
            SetStatus(_chkInfinite.Checked ? "Running…" : string.Format(CultureInfo.InvariantCulture, "Running {0}s…", (int)_numSeconds.Value));
        }

        private void StopRepeat()
        {
            _repeatTimer.Stop();
            _repeatRunning = false;
            _btnStart.Enabled = true;
            _btnStop.Enabled = false;
            _btnStartCompact.Enabled = true;
            _btnStopCompact.Enabled = false;
            SetStatus("Stopped.");
        }

        private void RepeatTimerOnTick(object? sender, EventArgs e)
        {
            if (!_repeatRunning)
                return;

            if (_runEndUtc.HasValue && DateTime.UtcNow >= _runEndUtc.Value)
            {
                StopRepeat();
                SetStatus("Finished (duration).");
                return;
            }

            if (_runTopMode == TopMode.Ddo)
            {
                uint cmd;
                if (_runDdoSource == DdoSourceKind.Hotbar)
                {
                    if (!_repeat.TryGetHotbarCommand(_runBar1, _runSlot1, out cmd, out _))
                        return;
                }
                else
                {
                    cmd = _runFixedDdoCommand;
                }

                _repeat.SendDdo(cmd, _runSendRelease);
            }
            else
            {
                _repeat.SendWindowsKey(_runModifierKey, _runModifierKey2, _runWinKey);
            }

            _repeatTimer.Interval = NextTimerIntervalMs(_runIntervalMin, _runIntervalMax);
        }
    }
}
