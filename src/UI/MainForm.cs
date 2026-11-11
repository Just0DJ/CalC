using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Windows.Forms;
using TitanCalc.Engine;
using TitanCalc.Modules;
using TitanCalc.Storage;

namespace TitanCalc.UI
{
    public class MainForm : Form
    {
        private readonly ExpressionEvaluator _evaluator;
        private readonly HistoryLedger _history;
        private readonly SettingsManager _settings;
        private readonly ProgrammerEngine _progEngine;

        private ThemeColors _theme;
        private CasioLcdDisplay _display;
        private Panel _titleBar;
        private Label _titleLabel;
        private Panel _navBar;
        private Panel _contentPanel;
        private Panel _calcPanel;
        private Panel _scientificKeypad;
        private Panel _standardKeypad;
        private Panel _solverPanel;
        private Panel _programmerPanel;
        private Panel _unitPanel;
        private Panel _currencyPanel;
        private Panel _datePanel;
        private Panel _historyPanel;
        private Panel _settingsPanel;

        private bool _isShiftActive = false;
        private bool _isAlphaActive = false;
        private Timer _memoryTrimTimer;
        private const int HOTKEY_ID = 9001;

        public MainForm()
        {
            _evaluator = new ExpressionEvaluator();
            _history = new HistoryLedger();
            _settings = new SettingsManager();
            _progEngine = new ProgrammerEngine();

            _evaluator.AngleMode = _settings.Angle;

            InitializeForm();
            InitializeComponents();
            ApplyTheme(_settings.Theme == AppTheme.RetroCasioFx991);
            SetLayoutMode(_settings.Layout);

            // Register Global Hotkey: Ctrl + Shift + Space
            Win32Dwm.RegisterHotKey(Handle, HOTKEY_ID, Win32Dwm.MOD_CONTROL | Win32Dwm.MOD_SHIFT | Win32Dwm.MOD_NOREPEAT, (int)Keys.Space);

            // Setup proactive memory trimmer to enforce < 15MB strict boundary
            Timer initialTrim = new Timer { Interval = 200 };
            initialTrim.Tick += (s, e) =>
            {
                initialTrim.Stop();
                initialTrim.Dispose();
                Win32Dwm.TrimProcessMemory();
            };
            initialTrim.Start();

            _memoryTrimTimer = new Timer();
            _memoryTrimTimer.Interval = 10000;
            _memoryTrimTimer.Tick += (s, e) =>
            {
                _history.PurgeExpired();
                Win32Dwm.TrimProcessMemory();
            };
            _memoryTrimTimer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            Win32Dwm.ApplyModernWindowAttributes(Handle);
            Win32Dwm.TrimProcessMemory();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            Win32Dwm.TrimProcessMemory();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                Win32Dwm.UnregisterHotKey(Handle, HOTKEY_ID);
                if (_memoryTrimTimer != null)
                {
                    _memoryTrimTimer.Stop();
                    _memoryTrimTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == Win32Dwm.WM_HOTKEY && m.WParam.ToInt32() == HOTKEY_ID)
            {
                ToggleHudMode();
                return;
            }
            base.WndProc(ref m);
        }

        private void InitializeForm()
        {
            Text = "TitanCalc - Engineering Scientific Desktop Calculator";
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.CenterScreen;
            DoubleBuffered = true;
            MinimumSize = new Size(340, 520);
            Size = new Size(620, 680);
            KeyPreview = true;
            KeyDown += MainForm_KeyDown;
        }

        private void InitializeComponents()
        {
            // Title Bar
            _titleBar = new Panel { Dock = DockStyle.Top, Height = 36 };
            _titleBar.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) Win32Dwm.DragWindow(Handle); };

            _titleLabel = new Label
            {
                Text = "\u2699 TitanCalc  fx-991 Pro",
                Font = new Font("Segoe UI Semibold", 10f, FontStyle.Bold),
                Location = new Point(12, 8),
                AutoSize = true
            };
            _titleLabel.MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) Win32Dwm.DragWindow(Handle); };
            _titleBar.Controls.Add(_titleLabel);

            // Title Bar Control Buttons (Right aligned)
            AddTitleBarButton("\u2715", (s, e) => Close(), 0, true);
            AddTitleBarButton("\u25a2", (s, e) => WindowState = WindowState == FormWindowState.Maximized ? FormWindowState.Normal : FormWindowState.Maximized, 38, false);
            AddTitleBarButton("\u2015", (s, e) => WindowState = FormWindowState.Minimized, 76, false);
            AddTitleBarButton("\ud83d\udccc", (s, e) => ToggleHudMode(), 114, false); // Pin/HUD toggle

            Controls.Add(_titleBar);

            // Navigation Bar
            _navBar = new Panel { Dock = DockStyle.Top, Height = 32 };
            BuildNavBar();
            Controls.Add(_navBar);

            // Display
            _display = new CasioLcdDisplay { Dock = DockStyle.Top, Height = 110 };
            Controls.Add(_display);

            // Content Panel (Holds active module view)
            _contentPanel = new Panel { Dock = DockStyle.Fill };
            Controls.Add(_contentPanel);

            // Build Modules
            BuildCalculatorPanel();
            BuildSolverPanel();
            BuildProgrammerPanel();
            BuildUnitConverterPanel();
            BuildCurrencyPanel();
            BuildDatePanel();
            BuildHistoryPanel();
            BuildSettingsPanel();

            ShowModulePanel(_calcPanel);
        }

        private void AddTitleBarButton(string symbol, EventHandler onClick, int rightOffset, bool isClose)
        {
            Button btn = new Button
            {
                Text = symbol,
                Size = new Size(38, 36),
                FlatStyle = FlatStyle.Flat,
                Font = new Font("Segoe UI", 9.5f),
                Anchor = AnchorStyles.Top | AnchorStyles.Right,
                TabStop = false
            };
            btn.FlatAppearance.BorderSize = 0;
            btn.Location = new Point(_titleBar.Width - 38 - rightOffset, 0);
            btn.Click += onClick;
            _titleBar.Controls.Add(btn);

            _titleBar.Resize += (s, e) =>
            {
                btn.Location = new Point(_titleBar.Width - 38 - rightOffset, 0);
            };
        }

        private void BuildNavBar()
        {
            string[] tabs = new string[] { "CALC", "SOLVE", "BASE-N", "UNITS", "CURRENCY", "DATE", "HISTORY", "SETTINGS" };
            int x = 6;
            for (int i = 0; i < tabs.Length; i++)
            {
                string tabName = tabs[i];
                Button btn = new Button
                {
                    Text = tabName,
                    Height = 26,
                    Width = 64,
                    Location = new Point(x, 3),
                    FlatStyle = FlatStyle.Flat,
                    Font = new Font("Segoe UI", 8.2f, FontStyle.Bold),
                    TabStop = false
                };
                btn.FlatAppearance.BorderSize = 0;
                btn.Click += (s, e) => OnNavTabClicked(tabName);
                _navBar.Controls.Add(btn);
                x += 68;
            }
        }

        private void OnNavTabClicked(string tab)
        {
            switch (tab)
            {
                case "CALC": ShowModulePanel(_calcPanel); break;
                case "SOLVE": ShowModulePanel(_solverPanel); break;
                case "BASE-N": ShowModulePanel(_programmerPanel); break;
                case "UNITS": ShowModulePanel(_unitPanel); break;
                case "CURRENCY": ShowModulePanel(_currencyPanel); break;
                case "DATE": ShowModulePanel(_datePanel); break;
                case "HISTORY": RefreshHistoryView(); ShowModulePanel(_historyPanel); break;
                case "SETTINGS": ShowModulePanel(_settingsPanel); break;
            }
        }

        private void ShowModulePanel(Panel panel)
        {
            _contentPanel.Controls.Clear();
            panel.Dock = DockStyle.Fill;
            _contentPanel.Controls.Add(panel);
            _contentPanel.BringToFront();
            _display.Visible = (panel == _calcPanel || panel == _programmerPanel);
        }

        #region Calculator Keypad Construction
        private void BuildCalculatorPanel()
        {
            _calcPanel = new Panel { Dock = DockStyle.Fill };

            // Scientific sliding drawer (Left)
            _scientificKeypad = new Panel { Dock = DockStyle.Left, Width = 260 };
            BuildScientificKeypad();
            _calcPanel.Controls.Add(_scientificKeypad);

            // Standard 4-function keypad (Fill/Right)
            _standardKeypad = new Panel { Dock = DockStyle.Fill };
            BuildStandardKeypad();
            _calcPanel.Controls.Add(_standardKeypad);
        }

        private void BuildScientificKeypad()
        {
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 6,
                Padding = new Padding(4)
            };
            for (int c = 0; c < 4; c++) tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            for (int r = 0; r < 6; r++) tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 100f / 6f));

            AddCalcBtn(tlp, "SHIFT", "", "", ButtonRole.Accent, (s, e) => ToggleShift(), 0, 0);
            AddCalcBtn(tlp, "ALPHA", "", "", ButtonRole.Danger, (s, e) => ToggleAlpha(), 1, 0);
            AddCalcBtn(tlp, "MODE", "", "", ButtonRole.Function, (s, e) => ToggleLayoutMode(), 2, 0);
            AddCalcBtn(tlp, "ENG", "x10\u00b3", "x10\u207b\u00b3", ButtonRole.Function, (s, e) => ApplyEngFormat(), 3, 0);

            AddCalcBtn(tlp, "sin", "sin\u207b\u00b9", "sinh", ButtonRole.Function, (s, e) => InsertFunction("sin"), 0, 1);
            AddCalcBtn(tlp, "cos", "cos\u207b\u00b9", "cosh", ButtonRole.Function, (s, e) => InsertFunction("cos"), 1, 1);
            AddCalcBtn(tlp, "tan", "tan\u207b\u00b9", "tanh", ButtonRole.Function, (s, e) => InsertFunction("tan"), 2, 1);
            AddCalcBtn(tlp, "ln", "e\u02e3", "log2", ButtonRole.Function, (s, e) => InsertFunction("ln"), 3, 1);

            AddCalcBtn(tlp, "x\u00b2", "\u221a", "x\u00b3", ButtonRole.Function, (s, e) => InsertToken("^2"), 0, 2);
            AddCalcBtn(tlp, "x\u02b8", "\u207f\u221a", "^-1", ButtonRole.Function, (s, e) => InsertToken("^"), 1, 2);
            AddCalcBtn(tlp, "log", "10\u02e3", "logb", ButtonRole.Function, (s, e) => InsertFunction("log"), 2, 2);
            AddCalcBtn(tlp, "\u221a", "\u221b", "\u207f\u221a", ButtonRole.Function, (s, e) => InsertFunction("sqrt"), 3, 2);

            AddCalcBtn(tlp, "d/dx", "\u222b", "\u2211", ButtonRole.Function, (s, e) => InsertCalculusToken("diff"), 0, 3);
            AddCalcBtn(tlp, "\u222b", "d/dx", "\u220f", ButtonRole.Function, (s, e) => InsertCalculusToken("integ"), 1, 3);
            AddCalcBtn(tlp, "(", "[", "{", ButtonRole.Function, (s, e) => InsertToken("("), 2, 3);
            AddCalcBtn(tlp, ")", "]", "}", ButtonRole.Function, (s, e) => InsertToken(")"), 3, 3);

            AddCalcBtn(tlp, "x!", "nPr", "nCr", ButtonRole.Function, (s, e) => InsertToken("!"), 0, 4);
            AddCalcBtn(tlp, "nPr", "nCr", "x!", ButtonRole.Function, (s, e) => InsertFunction("nPr"), 1, 4);
            AddCalcBtn(tlp, "nCr", "nPr", "%", ButtonRole.Function, (s, e) => InsertFunction("nCr"), 2, 4);
            AddCalcBtn(tlp, "S\u21d4D", "F\u21d4D", "a b/c", ButtonRole.Function, (s, e) => ToggleFractionDisplay(), 3, 4);

            AddCalcBtn(tlp, "CONST", "c", "G", ButtonRole.Function, (s, e) => ShowConstantsMenu(), 0, 5);
            AddCalcBtn(tlp, "\u03c0", "e", "euler", ButtonRole.Function, (s, e) => InsertToken("pi"), 1, 5);
            AddCalcBtn(tlp, "i", "abs", "arg", ButtonRole.Function, (s, e) => InsertToken("i"), 2, 5);
            AddCalcBtn(tlp, "x", "y", "z", ButtonRole.Function, (s, e) => InsertToken("x"), 3, 5);

            _scientificKeypad.Controls.Add(tlp);
        }

        private void BuildStandardKeypad()
        {
            TableLayoutPanel tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 4,
                RowCount = 5,
                Padding = new Padding(4)
            };
            for (int c = 0; c < 4; c++) tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25f));
            for (int r = 0; r < 5; r++) tlp.RowStyles.Add(new RowStyle(SizeType.Percent, 20f));

            AddCalcBtn(tlp, "7", "", "", ButtonRole.Number, (s, e) => InsertToken("7"), 0, 0);
            AddCalcBtn(tlp, "8", "", "", ButtonRole.Number, (s, e) => InsertToken("8"), 1, 0);
            AddCalcBtn(tlp, "9", "", "", ButtonRole.Number, (s, e) => InsertToken("9"), 2, 0);
            AddCalcBtn(tlp, "DEL", "INS", "UNDO", ButtonRole.Danger, (s, e) => BackspaceToken(), 3, 0);

            AddCalcBtn(tlp, "4", "", "", ButtonRole.Number, (s, e) => InsertToken("4"), 0, 1);
            AddCalcBtn(tlp, "5", "", "", ButtonRole.Number, (s, e) => InsertToken("5"), 1, 1);
            AddCalcBtn(tlp, "6", "", "", ButtonRole.Number, (s, e) => InsertToken("6"), 2, 1);
            AddCalcBtn(tlp, "AC", "CLR", "OFF", ButtonRole.Danger, (s, e) => ClearAll(), 3, 1);

            AddCalcBtn(tlp, "1", "", "", ButtonRole.Number, (s, e) => InsertToken("1"), 0, 2);
            AddCalcBtn(tlp, "2", "", "", ButtonRole.Number, (s, e) => InsertToken("2"), 1, 2);
            AddCalcBtn(tlp, "3", "", "", ButtonRole.Number, (s, e) => InsertToken("3"), 2, 2);
            AddCalcBtn(tlp, "\u00d7", "*", "nPr", ButtonRole.Function, (s, e) => InsertToken("*"), 3, 2);

            AddCalcBtn(tlp, "0", "00", "000", ButtonRole.Number, (s, e) => InsertToken("0"), 0, 3);
            AddCalcBtn(tlp, ".", "Rand", "DRG", ButtonRole.Number, (s, e) => InsertToken("."), 1, 3);
            AddCalcBtn(tlp, "\u00d710\u02e3", "e", "%", ButtonRole.Function, (s, e) => InsertToken("e"), 2, 3);
            AddCalcBtn(tlp, "\u00f7", "/", "mod", ButtonRole.Function, (s, e) => InsertToken("/"), 3, 3);

            AddCalcBtn(tlp, "+", "", "", ButtonRole.Function, (s, e) => InsertToken("+"), 0, 4);
            AddCalcBtn(tlp, "-", "(-)", "", ButtonRole.Function, (s, e) => InsertToken("-"), 1, 4);
            AddCalcBtn(tlp, "ANS", "M+", "M-", ButtonRole.Function, (s, e) => InsertToken("ans"), 2, 4);
            AddCalcBtn(tlp, "=", "SOLVE", "%", ButtonRole.Accent, (s, e) => ExecuteEvaluation(), 3, 4);

            _standardKeypad.Controls.Add(tlp);
        }

        private void AddCalcBtn(TableLayoutPanel tlp, string primary, string shift, string alpha, ButtonRole role, EventHandler onClick, int col, int row)
        {
            CalculatorButton btn = new CalculatorButton
            {
                Dock = DockStyle.Fill,
                PrimaryText = primary,
                ShiftText = shift,
                AlphaText = alpha,
                Role = role,
                Theme = _theme,
                Margin = new Padding(2)
            };
            btn.Click += onClick;
            tlp.Controls.Add(btn, col, row);
        }
        #endregion

        #region Evaluation & Actions
        private void InsertToken(string token)
        {
            if (_isShiftActive)
            {
                token = GetShiftVariant(token);
                _isShiftActive = false;
                _display.IsShiftActive = false;
            }
            else if (_isAlphaActive)
            {
                token = GetAlphaVariant(token);
                _isAlphaActive = false;
                _display.IsAlphaActive = false;
            }

            _display.Expression += token;
            EvaluatePreview();
        }

        private void InsertFunction(string fn)
        {
            if (_isShiftActive)
            {
                if (fn == "sin") fn = "asin";
                else if (fn == "cos") fn = "acos";
                else if (fn == "tan") fn = "atan";
                _isShiftActive = false;
                _display.IsShiftActive = false;
            }
            else if (_isAlphaActive)
            {
                if (fn == "sin") fn = "sinh";
                else if (fn == "cos") fn = "cosh";
                else if (fn == "tan") fn = "tanh";
                _isAlphaActive = false;
                _display.IsAlphaActive = false;
            }

            _display.Expression += fn + "(";
        }

        private void InsertCalculusToken(string type)
        {
            if (type == "diff")
                _display.Expression += "diff(x^2, 1)";
            else if (type == "integ")
                _display.Expression += "integ(x^2, 0, 1)";
            EvaluatePreview();
        }

        private void BackspaceToken()
        {
            if (_display.Expression.Length > 0)
            {
                _display.Expression = _display.Expression.Substring(0, _display.Expression.Length - 1);
                EvaluatePreview();
            }
        }

        private void ClearAll()
        {
            _display.Expression = "";
            _display.Result = "0";
            _display.Fraction = "";
            _isShiftActive = false;
            _isAlphaActive = false;
            _display.IsShiftActive = false;
            _display.IsAlphaActive = false;
        }

        private void ToggleShift()
        {
            _isShiftActive = !_isShiftActive;
            if (_isShiftActive) _isAlphaActive = false;
            _display.IsShiftActive = _isShiftActive;
            _display.IsAlphaActive = _isAlphaActive;
        }

        private void ToggleAlpha()
        {
            _isAlphaActive = !_isAlphaActive;
            if (_isAlphaActive) _isShiftActive = false;
            _display.IsShiftActive = _isShiftActive;
            _display.IsAlphaActive = _isAlphaActive;
        }

        private void ToggleFractionDisplay()
        {
            _settings.PreferFraction = !_settings.PreferFraction;
            _settings.Save();
            ExecuteEvaluation();
        }

        private void ApplyEngFormat()
        {
            try
            {
                double val = _evaluator.LastAnswer;
                string eng = EngineeringConstants.FormatWithSIPrefix(val);
                _display.Result = eng;
            }
            catch { }
        }

        private void ShowConstantsMenu()
        {
            ContextMenu cm = new ContextMenu();
            foreach (var c in EngineeringConstants.PhysicalConstants)
            {
                var entry = c;
                cm.MenuItems.Add(string.Format("{0} ({1}) = {2} {3}", entry.Symbol, entry.Name, entry.Value, entry.Unit), (s, e) =>
                {
                    InsertToken(entry.Value.ToString(CultureInfo.InvariantCulture));
                });
            }
            cm.Show(this, PointToClient(Cursor.Position));
        }

        private void EvaluatePreview()
        {
            if (string.IsNullOrWhiteSpace(_display.Expression))
            {
                _display.Result = "0";
                _display.Fraction = "";
                return;
            }

            try
            {
                EvalResult res = _evaluator.Evaluate(_display.Expression);
                _display.Result = res.Format(_settings.PreferFraction);
                _display.Fraction = (res.HasRational && res.RationalVal.Value.Denominator != 1) ? res.RationalVal.Value.ToFractionString() : "";
            }
            catch
            {
                // Incomplete expression while typing
            }
        }

        private void ExecuteEvaluation()
        {
            if (string.IsNullOrWhiteSpace(_display.Expression)) return;

            try
            {
                EvalResult res = _evaluator.Evaluate(_display.Expression);
                string resStr = res.Format(_settings.PreferFraction);
                string fracStr = (res.HasRational && res.RationalVal.Value.Denominator != 1) ? res.RationalVal.Value.ToFractionString() : "";

                _display.Result = resStr;
                _display.Fraction = fracStr;
                _evaluator.LastAnswer = res.RealVal;

                // Record into memory-managed history ledger
                _history.Add(_display.Expression, resStr, fracStr, res.IsComplex);

                // Trim process memory back down to OS
                Win32Dwm.TrimProcessMemory();
            }
            catch (Exception ex)
            {
                _display.Result = "Syntax ERROR";
                _display.Fraction = ex.Message;
            }
        }

        private string GetShiftVariant(string tok)
        {
            if (tok == "pi") return "e";
            if (tok == "^2") return "sqrt(";
            if (tok == "!") return "nPr";
            if (tok == "*") return "nPr";
            if (tok == "/") return "nCr";
            return tok;
        }

        private string GetAlphaVariant(string tok)
        {
            if (tok == "pi") return "euler";
            if (tok == "ans") return "M";
            return tok;
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9 && !e.Shift)
            {
                InsertToken(((int)e.KeyCode - (int)Keys.D0).ToString());
                e.Handled = true;
            }
            else if (e.KeyCode >= Keys.NumPad0 && e.KeyCode <= Keys.NumPad9)
            {
                InsertToken(((int)e.KeyCode - (int)Keys.NumPad0).ToString());
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Add || (e.KeyCode == Keys.Oemplus && e.Shift))
            {
                InsertToken("+");
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Subtract || e.KeyCode == Keys.OemMinus)
            {
                InsertToken("-");
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Multiply || (e.KeyCode == Keys.D8 && e.Shift))
            {
                InsertToken("*");
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Divide || e.KeyCode == Keys.OemQuestion)
            {
                InsertToken("/");
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Enter || e.KeyCode == Keys.Oemplus)
            {
                ExecuteEvaluation();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Back)
            {
                BackspaceToken();
                e.Handled = true;
            }
            else if (e.KeyCode == Keys.Escape)
            {
                ClearAll();
                e.Handled = true;
            }
        }
        #endregion

        #region Form Factor & Theme Switching
        public void ToggleLayoutMode()
        {
            SetLayoutMode(_settings.Layout == CalculatorLayout.CompactStandard ? CalculatorLayout.ExpandedScientific : CalculatorLayout.CompactStandard);
        }

        public void SetLayoutMode(CalculatorLayout mode)
        {
            _settings.Layout = mode;
            _settings.Save();

            if (mode == CalculatorLayout.CompactStandard)
            {
                _scientificKeypad.Visible = false;
                Width = 340;
            }
            else
            {
                _scientificKeypad.Visible = true;
                Width = 620;
            }
        }

        public void ApplyTheme(bool retroCasio)
        {
            _theme = retroCasio ? RenderTheme.GetRetroCasioTheme() : RenderTheme.GetModernTheme();
            _settings.Theme = retroCasio ? AppTheme.RetroCasioFx991 : AppTheme.ModernMaterial3;
            _settings.Save();

            BackColor = _theme.WindowBackground;
            _titleBar.BackColor = _theme.TitleBarBackground;
            _titleLabel.ForeColor = _theme.TextPrimary;
            _titleLabel.Text = retroCasio ? "CASIO fx-991EX CLASSWIZ" : "\u2699 TitanCalc  fx-991 Pro";

            foreach (Control c in _titleBar.Controls)
            {
                if (c is Button)
                {
                    c.ForeColor = _theme.TextPrimary;
                    c.BackColor = _theme.TitleBarBackground;
                }
            }

            _navBar.BackColor = _theme.SurfaceBackground;
            foreach (Control c in _navBar.Controls)
            {
                if (c is Button)
                {
                    c.ForeColor = _theme.TextSecondary;
                    c.BackColor = _theme.SurfaceBackground;
                }
            }

            _display.Theme = _theme;
            PropagateThemeToButtons(_scientificKeypad);
            PropagateThemeToButtons(_standardKeypad);

            Invalidate(true);
        }

        private void PropagateThemeToButtons(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is CalculatorButton)
                {
                    ((CalculatorButton)c).Theme = _theme;
                }
                else if (c.HasChildren)
                {
                    PropagateThemeToButtons(c);
                }
            }
        }

        public void ToggleHudMode()
        {
            _settings.AlwaysOnTop = !_settings.AlwaysOnTop;
            _settings.Save();

            TopMost = _settings.AlwaysOnTop;
            Opacity = _settings.AlwaysOnTop ? _settings.HudOpacity : 1.0;

            if (_settings.AlwaysOnTop)
            {
                SetLayoutMode(CalculatorLayout.CompactStandard);
            }
        }
        #endregion

        #region Integrated Tool Modules (Solver, Programmer, Units, Currency, Date, History, Settings)
        private void BuildSolverPanel()
        {
            _solverPanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(12) };

            Label lbl = new Label
            {
                Text = "Casio fx-991 SOLVE & Equation Engine",
                Font = new Font("Segoe UI", 12f, FontStyle.Bold),
                ForeColor = Color.White,
                AutoSize = true,
                Location = new Point(12, 10)
            };
            _solverPanel.Controls.Add(lbl);

            // Newton-Raphson f(x) = 0
            Label lblNR = new Label { Text = "Newton-Raphson f(x) = 0 Solver:", ForeColor = Color.LightSkyBlue, Location = new Point(12, 45), AutoSize = true };
            _solverPanel.Controls.Add(lblNR);

            TextBox txtExpr = new TextBox { Text = "x^3 - 4*x - 9", Location = new Point(12, 68), Width = 280, Font = new Font("Segoe UI", 10f) };
            TextBox txtGuess = new TextBox { Text = "2.0", Location = new Point(300, 68), Width = 60, Font = new Font("Segoe UI", 10f) };
            Button btnSolve = new Button { Text = "SOLVE", Location = new Point(370, 66), Width = 70, Height = 28, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Teal };
            Label lblSolResult = new Label { Text = "Root: ", ForeColor = Color.White, Location = new Point(12, 98), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

            btnSolve.Click += (s, e) =>
            {
                try
                {
                    double guess = double.Parse(txtGuess.Text, CultureInfo.InvariantCulture);
                    Func<double, double> func = (v) =>
                    {
                        var ev = new ExpressionEvaluator();
                        ev.SetVariable("x", v);
                        return ev.Evaluate(txtExpr.Text).RealVal;
                    };
                    var sol = EquationSolver.SolveNewtonRaphson(func, guess);
                    lblSolResult.Text = string.Format("Root x = {0} (Iter: {1}, Error: {2:G4})", sol.Root, sol.Iterations, sol.Error);
                }
                catch (Exception ex)
                {
                    lblSolResult.Text = "Error: " + ex.Message;
                }
            };

            _solverPanel.Controls.Add(txtExpr);
            _solverPanel.Controls.Add(txtGuess);
            _solverPanel.Controls.Add(btnSolve);
            _solverPanel.Controls.Add(lblSolResult);

            // Polynomial Root Finder (Quadratic & Cubic)
            Label lblPoly = new Label { Text = "Polynomial Solver (ax\u00b3 + bx\u00b2 + cx + d = 0):", ForeColor = Color.LightSkyBlue, Location = new Point(12, 140), AutoSize = true };
            _solverPanel.Controls.Add(lblPoly);

            TextBox txtA = new TextBox { Text = "1", Location = new Point(12, 165), Width = 45 };
            TextBox txtB = new TextBox { Text = "-6", Location = new Point(65, 165), Width = 45 };
            TextBox txtC = new TextBox { Text = "11", Location = new Point(118, 165), Width = 45 };
            TextBox txtD = new TextBox { Text = "-6", Location = new Point(171, 165), Width = 45 };
            Button btnPoly = new Button { Text = "Find Roots", Location = new Point(226, 163), Width = 90, Height = 26, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Teal };
            Label lblPolyRes = new Label { Text = "Roots: ", ForeColor = Color.White, Location = new Point(12, 195), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

            btnPoly.Click += (s, e) =>
            {
                try
                {
                    double a = double.Parse(txtA.Text, CultureInfo.InvariantCulture);
                    double b = double.Parse(txtB.Text, CultureInfo.InvariantCulture);
                    double c = double.Parse(txtC.Text, CultureInfo.InvariantCulture);
                    double d = double.Parse(txtD.Text, CultureInfo.InvariantCulture);
                    var roots = EquationSolver.SolveCubic(a, b, c, d);
                    lblPolyRes.Text = "Roots: " + string.Join(",  ", roots.ConvertAll(r => r.ToString()).ToArray());
                }
                catch (Exception ex) { lblPolyRes.Text = "Error: " + ex.Message; }
            };

            _solverPanel.Controls.Add(txtA);
            _solverPanel.Controls.Add(txtB);
            _solverPanel.Controls.Add(txtC);
            _solverPanel.Controls.Add(txtD);
            _solverPanel.Controls.Add(btnPoly);
            _solverPanel.Controls.Add(lblPolyRes);

            // 2x2 Simultaneous Equations
            Label lblSimul = new Label { Text = "2x2 Simultaneous System (a1*x + b1*y = c1):", ForeColor = Color.LightSkyBlue, Location = new Point(12, 235), AutoSize = true };
            _solverPanel.Controls.Add(lblSimul);

            TextBox sA1 = new TextBox { Text = "2", Location = new Point(12, 260), Width = 40 };
            TextBox sB1 = new TextBox { Text = "3", Location = new Point(58, 260), Width = 40 };
            TextBox sC1 = new TextBox { Text = "12", Location = new Point(104, 260), Width = 45 };

            TextBox sA2 = new TextBox { Text = "5", Location = new Point(12, 290), Width = 40 };
            TextBox sB2 = new TextBox { Text = "-2", Location = new Point(58, 290), Width = 40 };
            TextBox sC2 = new TextBox { Text = "11", Location = new Point(104, 290), Width = 45 };

            Button btnSimul = new Button { Text = "Solve System", Location = new Point(160, 273), Width = 100, Height = 30, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Teal };
            Label lblSimulRes = new Label { Text = "x = ?, y = ?", ForeColor = Color.White, Location = new Point(12, 325), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

            btnSimul.Click += (s, e) =>
            {
                try
                {
                    double a1 = double.Parse(sA1.Text, CultureInfo.InvariantCulture);
                    double b1 = double.Parse(sB1.Text, CultureInfo.InvariantCulture);
                    double c1 = double.Parse(sC1.Text, CultureInfo.InvariantCulture);
                    double a2 = double.Parse(sA2.Text, CultureInfo.InvariantCulture);
                    double b2 = double.Parse(sB2.Text, CultureInfo.InvariantCulture);
                    double c2 = double.Parse(sC2.Text, CultureInfo.InvariantCulture);
                    var sol = EquationSolver.SolveSimultaneous2x2(a1, b1, c1, a2, b2, c2);
                    lblSimulRes.Text = string.Format("x = {0},  y = {1}", sol.X, sol.Y);
                }
                catch (Exception ex) { lblSimulRes.Text = "Error: " + ex.Message; }
            };

            _solverPanel.Controls.Add(sA1); _solverPanel.Controls.Add(sB1); _solverPanel.Controls.Add(sC1);
            _solverPanel.Controls.Add(sA2); _solverPanel.Controls.Add(sB2); _solverPanel.Controls.Add(sC2);
            _solverPanel.Controls.Add(btnSimul);
            _solverPanel.Controls.Add(lblSimulRes);
        }

        private void BuildProgrammerPanel()
        {
            _programmerPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            Label lbl = new Label { Text = "Programmer / Base-N Live Bitwise Engine", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(12, 10), AutoSize = true };
            _programmerPanel.Controls.Add(lbl);

            // Word Size Selection
            Label lblWord = new Label { Text = "Word Size: ", ForeColor = Color.LightSkyBlue, Location = new Point(12, 45), AutoSize = true };
            ComboBox cmbWord = new ComboBox { Location = new Point(90, 42), Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbWord.Items.AddRange(new object[] { "64-bit (QWord)", "32-bit (DWord)", "16-bit (Word)", "8-bit (Byte)" });
            cmbWord.SelectedIndex = 0;
            _programmerPanel.Controls.Add(lblWord);
            _programmerPanel.Controls.Add(cmbWord);

            // Live Base-N Fields
            Label lblHex = new Label { Text = "HEX:", ForeColor = Color.LightSkyBlue, Location = new Point(12, 85), Width = 45 };
            TextBox txtHex = new TextBox { Location = new Point(60, 82), Width = 380, Font = new Font("Consolas", 10.5f) };
            Label lblDec = new Label { Text = "DEC:", ForeColor = Color.LightSkyBlue, Location = new Point(12, 120), Width = 45 };
            TextBox txtDec = new TextBox { Location = new Point(60, 117), Width = 380, Font = new Font("Consolas", 10.5f) };
            Label lblOct = new Label { Text = "OCT:", ForeColor = Color.LightSkyBlue, Location = new Point(12, 155), Width = 45 };
            TextBox txtOct = new TextBox { Location = new Point(60, 152), Width = 380, Font = new Font("Consolas", 10.5f) };
            Label lblBin = new Label { Text = "BIN:", ForeColor = Color.LightSkyBlue, Location = new Point(12, 190), Width = 45 };
            TextBox txtBin = new TextBox { Location = new Point(60, 187), Width = 380, Font = new Font("Consolas", 10f) };

            Action updateFields = () =>
            {
                txtHex.Text = _progEngine.ToHex();
                txtDec.Text = _progEngine.ToDec(true);
                txtOct.Text = _progEngine.ToOct();
                txtBin.Text = _progEngine.ToBin(true);
            };

            cmbWord.SelectedIndexChanged += (s, e) =>
            {
                if (cmbWord.SelectedIndex == 0) _progEngine.WordSize = WordSize.QWord;
                else if (cmbWord.SelectedIndex == 1) _progEngine.WordSize = WordSize.DWord;
                else if (cmbWord.SelectedIndex == 2) _progEngine.WordSize = WordSize.Word;
                else _progEngine.WordSize = WordSize.Byte;
                updateFields();
            };

            txtDec.TextChanged += (s, e) =>
            {
                if (txtDec.Focused)
                {
                    try { _progEngine.SetFromDec(txtDec.Text); } catch { }
                    txtHex.Text = _progEngine.ToHex();
                    txtOct.Text = _progEngine.ToOct();
                    txtBin.Text = _progEngine.ToBin(true);
                }
            };

            txtHex.TextChanged += (s, e) =>
            {
                if (txtHex.Focused)
                {
                    try { _progEngine.SetFromHex(txtHex.Text); } catch { }
                    txtDec.Text = _progEngine.ToDec(true);
                    txtOct.Text = _progEngine.ToOct();
                    txtBin.Text = _progEngine.ToBin(true);
                }
            };

            // Bitwise Operation Buttons (AND, OR, XOR, NOT, NAND, NOR, LSH, RSH)
            int bx = 12, by = 230;
            string[] ops = new string[] { "AND", "OR", "XOR", "NOT", "NAND", "NOR", "<< 1", ">> 1" };
            for (int i = 0; i < ops.Length; i++)
            {
                string op = ops[i];
                Button b = new Button { Text = op, Location = new Point(bx, by), Width = 55, Height = 32, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(40, 50, 70) };
                b.Click += (s, e) =>
                {
                    if (op == "NOT") _progEngine.Not();
                    else if (op == "<< 1") _progEngine.ShiftLeft(1);
                    else if (op == ">> 1") _progEngine.ShiftRight(1);
                    updateFields();
                };
                _programmerPanel.Controls.Add(b);
                bx += 60;
                if (bx > 420) { bx = 12; by += 38; }
            }

            _programmerPanel.Controls.Add(lblHex); _programmerPanel.Controls.Add(txtHex);
            _programmerPanel.Controls.Add(lblDec); _programmerPanel.Controls.Add(txtDec);
            _programmerPanel.Controls.Add(lblOct); _programmerPanel.Controls.Add(txtOct);
            _programmerPanel.Controls.Add(lblBin); _programmerPanel.Controls.Add(txtBin);

            _progEngine.Value = 255;
            updateFields();
        }

        private void BuildUnitConverterPanel()
        {
            _unitPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

            Label lbl = new Label { Text = "Real-Time Bidirectional Unit Converter", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            _unitPanel.Controls.Add(lbl);

            Label lblCat = new Label { Text = "Category:", ForeColor = Color.LightSkyBlue, Location = new Point(16, 50), AutoSize = true };
            ComboBox cmbCat = new ComboBox { Location = new Point(16, 72), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            foreach (var k in UnitConverter.Categories.Keys) cmbCat.Items.Add(k);
            cmbCat.SelectedIndex = 0;
            _unitPanel.Controls.Add(lblCat);
            _unitPanel.Controls.Add(cmbCat);

            // Left Unit
            TextBox txtVal1 = new TextBox { Text = "1", Location = new Point(16, 115), Width = 140, Font = new Font("Segoe UI", 11f) };
            ComboBox cmbUnit1 = new ComboBox { Location = new Point(165, 115), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };

            // Right Unit
            TextBox txtVal2 = new TextBox { Location = new Point(16, 160), Width = 140, Font = new Font("Segoe UI", 11f) };
            ComboBox cmbUnit2 = new ComboBox { Location = new Point(165, 160), Width = 180, DropDownStyle = ComboBoxStyle.DropDownList };

            Action updateUnits = () =>
            {
                cmbUnit1.Items.Clear();
                cmbUnit2.Items.Clear();
                string catName = cmbCat.SelectedItem.ToString();
                var cat = UnitConverter.Categories[catName];
                foreach (var u in cat.UnitsToSI.Keys)
                {
                    cmbUnit1.Items.Add(u);
                    cmbUnit2.Items.Add(u);
                }
                if (cmbUnit1.Items.Count > 0) cmbUnit1.SelectedIndex = 0;
                if (cmbUnit2.Items.Count > 1) cmbUnit2.SelectedIndex = 1;
            };

            Action convert1To2 = () =>
            {
                if (cmbUnit1.SelectedItem == null || cmbUnit2.SelectedItem == null) return;
                double v1;
                if (double.TryParse(txtVal1.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out v1))
                {
                    double v2 = UnitConverter.Convert(cmbCat.SelectedItem.ToString(), cmbUnit1.SelectedItem.ToString(), cmbUnit2.SelectedItem.ToString(), v1);
                    txtVal2.Text = v2.ToString("G8", CultureInfo.InvariantCulture);
                }
            };

            Action convert2To1 = () =>
            {
                if (cmbUnit1.SelectedItem == null || cmbUnit2.SelectedItem == null) return;
                double v2;
                if (double.TryParse(txtVal2.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out v2))
                {
                    double v1 = UnitConverter.Convert(cmbCat.SelectedItem.ToString(), cmbUnit2.SelectedItem.ToString(), cmbUnit1.SelectedItem.ToString(), v2);
                    txtVal1.Text = v1.ToString("G8", CultureInfo.InvariantCulture);
                }
            };

            cmbCat.SelectedIndexChanged += (s, e) => { updateUnits(); convert1To2(); };
            cmbUnit1.SelectedIndexChanged += (s, e) => convert1To2();
            cmbUnit2.SelectedIndexChanged += (s, e) => convert1To2();
            txtVal1.TextChanged += (s, e) => { if (txtVal1.Focused) convert1To2(); };
            txtVal2.TextChanged += (s, e) => { if (txtVal2.Focused) convert2To1(); };

            updateUnits();
            convert1To2();

            _unitPanel.Controls.Add(txtVal1);
            _unitPanel.Controls.Add(cmbUnit1);
            _unitPanel.Controls.Add(txtVal2);
            _unitPanel.Controls.Add(cmbUnit2);
        }

        private void BuildCurrencyPanel()
        {
            _currencyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

            Label lbl = new Label { Text = "Offline-First Currency Converter", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            _currencyPanel.Controls.Add(lbl);

            TextBox txtAmount = new TextBox { Text = "100", Location = new Point(16, 55), Width = 120, Font = new Font("Segoe UI", 11f) };
            ComboBox cmbFrom = new ComboBox { Location = new Point(145, 55), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
            Label lblArrow = new Label { Text = "\u2794", ForeColor = Color.White, Location = new Point(245, 57), AutoSize = true, Font = new Font("Segoe UI", 11f) };
            ComboBox cmbTo = new ComboBox { Location = new Point(275, 55), Width = 90, DropDownStyle = ComboBoxStyle.DropDownList };
            Label lblResult = new Label { Text = "Result: ", ForeColor = Color.LightSkyBlue, Location = new Point(16, 95), AutoSize = true, Font = new Font("Segoe UI", 11f, FontStyle.Bold) };

            foreach (var c in CurrencyConverter.Currencies)
            {
                cmbFrom.Items.Add(c);
                cmbTo.Items.Add(c);
            }
            cmbFrom.SelectedItem = "USD";
            cmbTo.SelectedItem = "EUR";

            Action doConvert = () =>
            {
                double amt;
                if (double.TryParse(txtAmount.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out amt) && cmbFrom.SelectedItem != null && cmbTo.SelectedItem != null)
                {
                    double res = CurrencyConverter.Convert(cmbFrom.SelectedItem.ToString(), cmbTo.SelectedItem.ToString(), amt);
                    lblResult.Text = string.Format(CultureInfo.InvariantCulture, "{0} {1} = {2:N2} {3}", amt, cmbFrom.SelectedItem, res, cmbTo.SelectedItem);
                }
            };

            txtAmount.TextChanged += (s, e) => doConvert();
            cmbFrom.SelectedIndexChanged += (s, e) => doConvert();
            cmbTo.SelectedIndexChanged += (s, e) => doConvert();
            doConvert();

            Button btnSync = new Button { Text = "Sync Live Rates", Location = new Point(16, 140), Width = 130, Height = 30, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.Teal };
            Label lblSyncStatus = new Label { Text = "Rates: Cached offline", ForeColor = Color.LightGray, Location = new Point(155, 147), AutoSize = true };

            btnSync.Click += (s, e) =>
            {
                lblSyncStatus.Text = "Syncing in background...";
                CurrencyConverter.SyncRatesAsync((success, msg) =>
                {
                    BeginInvoke(new Action(() =>
                    {
                        lblSyncStatus.Text = msg;
                        doConvert();
                    }));
                });
            };

            _currencyPanel.Controls.Add(txtAmount);
            _currencyPanel.Controls.Add(cmbFrom);
            _currencyPanel.Controls.Add(lblArrow);
            _currencyPanel.Controls.Add(cmbTo);
            _currencyPanel.Controls.Add(lblResult);
            _currencyPanel.Controls.Add(btnSync);
            _currencyPanel.Controls.Add(lblSyncStatus);
        }

        private void BuildDatePanel()
        {
            _datePanel = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(16) };

            Label lbl = new Label { Text = "Chronological Age & Date Arithmetic", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            _datePanel.Controls.Add(lbl);

            // Birthday Age Breakdown
            Label lblBday = new Label { Text = "Birthday / Date of Birth:", ForeColor = Color.LightSkyBlue, Location = new Point(16, 50), AutoSize = true };
            DateTimePicker dtBirth = new DateTimePicker { Location = new Point(16, 72), Width = 220, Value = new DateTime(2000, 1, 1) };
            Label lblAgeRes = new Label { Text = "", ForeColor = Color.White, Location = new Point(16, 105), AutoSize = true, Font = new Font("Segoe UI", 10f, FontStyle.Bold) };

            Action updateAge = () =>
            {
                AgeBreakdown ab = DateChronology.CalculateAge(dtBirth.Value, DateTime.Now);
                lblAgeRes.Text = string.Format("Age: {0} Years, {1} Months, {2} Days\nTotal: {3:N0} Days lived",
                    ab.Years, ab.Months, ab.Days, ab.TotalDays);
            };
            dtBirth.ValueChanged += (s, e) => updateAge();
            updateAge();

            _datePanel.Controls.Add(lblBday);
            _datePanel.Controls.Add(dtBirth);
            _datePanel.Controls.Add(lblAgeRes);

            // Date Difference & Business Days
            Label lblDiff = new Label { Text = "Date Difference & Business Days:", ForeColor = Color.LightSkyBlue, Location = new Point(16, 160), AutoSize = true };
            DateTimePicker dtStart = new DateTimePicker { Location = new Point(16, 185), Width = 160, Value = DateTime.Today };
            DateTimePicker dtEnd = new DateTimePicker { Location = new Point(190, 185), Width = 160, Value = DateTime.Today.AddDays(30) };
            Label lblDiffRes = new Label { Text = "", ForeColor = Color.White, Location = new Point(16, 220), AutoSize = true, Font = new Font("Segoe UI", 10f) };

            Action updateDiff = () =>
            {
                int totalDays = (int)(dtEnd.Value.Date - dtStart.Value.Date).TotalDays;
                int bizDays = DateChronology.CountBusinessDays(dtStart.Value, dtEnd.Value);
                lblDiffRes.Text = string.Format("Total Difference: {0} calendar days\nBusiness Days (Mon-Fri): {1} days", Math.Abs(totalDays), bizDays);
            };
            dtStart.ValueChanged += (s, e) => updateDiff();
            dtEnd.ValueChanged += (s, e) => updateDiff();
            updateDiff();

            _datePanel.Controls.Add(lblDiff);
            _datePanel.Controls.Add(dtStart);
            _datePanel.Controls.Add(dtEnd);
            _datePanel.Controls.Add(lblDiffRes);
        }

        private void BuildHistoryPanel()
        {
            _historyPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12) };

            Label lbl = new Label { Text = "Memory-Managed Calculation History Ledger", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(12, 10), AutoSize = true };
            _historyPanel.Controls.Add(lbl);

            ListBox lst = new ListBox
            {
                Location = new Point(12, 45),
                Size = new Size(580, 420),
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
                Font = new Font("Consolas", 10f),
                BackColor = Color.FromArgb(25, 30, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };

            lst.DoubleClick += (s, e) =>
            {
                if (lst.SelectedItem != null)
                {
                    string item = lst.SelectedItem.ToString();
                    int eq = item.LastIndexOf(" = ");
                    if (eq >= 0)
                    {
                        string expr = item.Substring(0, eq).Trim();
                        string val = item.Substring(eq + 3).Trim();

                        // Interactive Recall: Inject ANS or expression
                        _display.Expression = expr;
                        _display.Result = val;
                        ShowModulePanel(_calcPanel);
                    }
                }
            };

            Button btnClear = new Button { Text = "Clear History", Location = new Point(12, 475), Width = 110, Height = 28, FlatStyle = FlatStyle.Flat, ForeColor = Color.White, BackColor = Color.FromArgb(180, 50, 50), Anchor = AnchorStyles.Bottom | AnchorStyles.Left };
            btnClear.Click += (s, e) =>
            {
                _history.Clear();
                RefreshHistoryView();
            };

            _historyPanel.Controls.Add(lst);
            _historyPanel.Controls.Add(btnClear);
        }

        private void RefreshHistoryView()
        {
            ListBox lst = null;
            foreach (Control c in _historyPanel.Controls) if (c is ListBox) { lst = (ListBox)c; break; }
            if (lst == null) return;

            lst.Items.Clear();
            foreach (var h in _history.Entries)
            {
                string line = string.Format(CultureInfo.InvariantCulture, "{0} = {1}", h.Expression, h.Result);
                if (!string.IsNullOrEmpty(h.ExactFraction)) line += " [" + h.ExactFraction + "]";
                lst.Items.Add(line);
            }
        }

        private void BuildSettingsPanel()
        {
            _settingsPanel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(16) };

            Label lbl = new Label { Text = "Application Settings & Architecture", Font = new Font("Segoe UI", 12f, FontStyle.Bold), ForeColor = Color.White, Location = new Point(16, 12), AutoSize = true };
            _settingsPanel.Controls.Add(lbl);

            // Design Language Toggle
            Label lblDesign = new Label { Text = "Design Language Aesthetic:", ForeColor = Color.LightSkyBlue, Location = new Point(16, 50), AutoSize = true };
            RadioButton rbModern = new RadioButton { Text = "Mode A: Minimalist Modern (Material 3 / Fluid)", Location = new Point(16, 75), AutoSize = true, ForeColor = Color.White, Checked = _settings.Theme == AppTheme.ModernMaterial3 };
            RadioButton rbRetro = new RadioButton { Text = "Mode B: Maximalist Retro (Hardware Casio fx-991)", Location = new Point(16, 100), AutoSize = true, ForeColor = Color.White, Checked = _settings.Theme == AppTheme.RetroCasioFx991 };

            rbModern.CheckedChanged += (s, e) => { if (rbModern.Checked) ApplyTheme(false); };
            rbRetro.CheckedChanged += (s, e) => { if (rbRetro.Checked) ApplyTheme(true); };

            _settingsPanel.Controls.Add(lblDesign);
            _settingsPanel.Controls.Add(rbModern);
            _settingsPanel.Controls.Add(rbRetro);

            // Form Factor Mode
            Label lblForm = new Label { Text = "Default Form Factor:", ForeColor = Color.LightSkyBlue, Location = new Point(16, 135), AutoSize = true };
            RadioButton rbCompact = new RadioButton { Text = "Compact Standard (4-Function)", Location = new Point(16, 160), AutoSize = true, ForeColor = Color.White, Checked = _settings.Layout == CalculatorLayout.CompactStandard };
            RadioButton rbExpanded = new RadioButton { Text = "Expanded Scientific (Full Drawer)", Location = new Point(16, 185), AutoSize = true, ForeColor = Color.White, Checked = _settings.Layout == CalculatorLayout.ExpandedScientific };

            rbCompact.CheckedChanged += (s, e) => { if (rbCompact.Checked) SetLayoutMode(CalculatorLayout.CompactStandard); };
            rbExpanded.CheckedChanged += (s, e) => { if (rbExpanded.Checked) SetLayoutMode(CalculatorLayout.ExpandedScientific); };

            _settingsPanel.Controls.Add(lblForm);
            _settingsPanel.Controls.Add(rbCompact);
            _settingsPanel.Controls.Add(rbExpanded);

            // History Retention Window
            Label lblRet = new Label { Text = "History Retention Window:", ForeColor = Color.LightSkyBlue, Location = new Point(16, 220), AutoSize = true };
            ComboBox cmbRet = new ComboBox { Location = new Point(16, 245), Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbRet.Items.AddRange(new object[] { "30 Minutes (Auto-purge on idle)", "8 Hours (Single workday session)", "Persistent / Forever" });
            cmbRet.SelectedIndex = (int)_settings.Retention;
            cmbRet.SelectedIndexChanged += (s, e) =>
            {
                _settings.Retention = (RetentionPolicy)cmbRet.SelectedIndex;
                _history.Retention = _settings.Retention;
                _settings.Save();
            };
            _settingsPanel.Controls.Add(lblRet);
            _settingsPanel.Controls.Add(cmbRet);

            // Angle Unit
            Label lblAng = new Label { Text = "Angle Unit:", ForeColor = Color.LightSkyBlue, Location = new Point(16, 285), AutoSize = true };
            ComboBox cmbAng = new ComboBox { Location = new Point(16, 310), Width = 160, DropDownStyle = ComboBoxStyle.DropDownList };
            cmbAng.Items.AddRange(new object[] { "Degrees (DEG)", "Radians (RAD)", "Gradians (GRAD)" });
            cmbAng.SelectedIndex = (int)_settings.Angle;
            cmbAng.SelectedIndexChanged += (s, e) =>
            {
                _settings.Angle = (AngleUnit)cmbAng.SelectedIndex;
                _evaluator.AngleMode = _settings.Angle;
                _display.AngleUnit = _settings.Angle;
                _settings.Save();
            };
            _settingsPanel.Controls.Add(lblAng);
            _settingsPanel.Controls.Add(cmbAng);
        }
        #endregion
    }
}
