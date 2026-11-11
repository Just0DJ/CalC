using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;
using TitanCalc.Engine;

namespace TitanCalc.UI
{
    /// <summary>
    /// Dual-engine display control: Modern sleek card vs Authentic Casio 2-line Natural Textbook LCD.
    /// </summary>
    public class CasioLcdDisplay : Control
    {
        private string _expression = "";
        private string _result = "0";
        private string _fraction = "";
        private bool _isShiftActive = false;
        private bool _isAlphaActive = false;
        private bool _hasMemory = false;
        private AngleUnit _angleUnit = AngleUnit.Degrees;
        private ThemeColors _theme;
        private Timer _caretTimer;
        private bool _showCaret = true;

        public string Expression
        {
            get { return _expression; }
            set { _expression = value ?? ""; Invalidate(); }
        }

        public string Result
        {
            get { return _result; }
            set { _result = value ?? "0"; Invalidate(); }
        }

        public string Fraction
        {
            get { return _fraction; }
            set { _fraction = value ?? ""; Invalidate(); }
        }

        public bool IsShiftActive
        {
            get { return _isShiftActive; }
            set { _isShiftActive = value; Invalidate(); }
        }

        public bool IsAlphaActive
        {
            get { return _isAlphaActive; }
            set { _isAlphaActive = value; Invalidate(); }
        }

        public bool HasMemory
        {
            get { return _hasMemory; }
            set { _hasMemory = value; Invalidate(); }
        }

        public AngleUnit AngleUnit
        {
            get { return _angleUnit; }
            set { _angleUnit = value; Invalidate(); }
        }

        public ThemeColors Theme
        {
            get { return _theme; }
            set { _theme = value; Invalidate(); }
        }

        public CasioLcdDisplay()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            _theme = RenderTheme.GetModernTheme();

            _caretTimer = new Timer();
            _caretTimer.Interval = 530;
            _caretTimer.Tick += (s, e) =>
            {
                _showCaret = !_showCaret;
                Invalidate();
            };
            _caretTimer.Start();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (_caretTimer != null)
                {
                    _caretTimer.Stop();
                    _caretTimer.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = ClientRectangle;

            if (_theme.IsRetroCasio)
            {
                DrawCasioLcd(g, rect);
            }
            else
            {
                DrawModernDisplay(g, rect);
            }
        }

        private void DrawModernDisplay(Graphics g, Rectangle rect)
        {
            // Background surface
            using (GraphicsPath path = GetRoundedRect(rect, 8))
            using (SolidBrush brush = new SolidBrush(_theme.DisplayBackground))
            using (Pen borderPen = new Pen(_theme.DisplayBorder, 1.5f))
            {
                g.FillPath(brush, path);
                g.DrawPath(borderPen, path);
            }

            // Status bar (Top)
            int statusY = 8;
            DrawModernBadges(g, 12, statusY);

            // Expression line (Middle)
            string displayExpr = FormatCasioMathText(_expression);
            using (Font exprFont = new Font("Segoe UI", 12f, FontStyle.Regular))
            using (SolidBrush exprBrush = new SolidBrush(_theme.TextDisplayExpression))
            {
                string exprWithCaret = displayExpr + (_showCaret ? "|" : " ");
                Rectangle exprRect = new Rectangle(12, 28, rect.Width - 24, 26);
                StringFormat sfExpr = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisPath
                };
                g.DrawString(exprWithCaret, exprFont, exprBrush, exprRect, sfExpr);
            }

            // Result line (Bottom)
            float fontSize = rect.Height < 110 ? 20f : 26f;
            using (Font resFont = new Font("Segoe UI Semibold", fontSize, FontStyle.Bold))
            using (SolidBrush resBrush = new SolidBrush(_theme.TextDisplayResult))
            {
                Rectangle resRect = new Rectangle(12, rect.Height - 48, rect.Width - 24, 40);
                StringFormat sfRes = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(_result, resFont, resBrush, resRect, sfRes);
            }

            // Fraction indicator in bottom-left if available
            if (!string.IsNullOrEmpty(_fraction))
            {
                using (Font fracFont = new Font("Segoe UI", 9.5f, FontStyle.Italic))
                using (SolidBrush fracBrush = new SolidBrush(_theme.TextSecondary))
                {
                    g.DrawString("S\u21d4D: " + _fraction, fracFont, fracBrush, 14, rect.Height - 24);
                }
            }
        }

        private void DrawCasioLcd(Graphics g, Rectangle rect)
        {
            // Casio outer bezel border
            using (GraphicsPath outerPath = GetRoundedRect(rect, 5))
            using (SolidBrush outerBrush = new SolidBrush(_theme.DisplayBorder))
            {
                g.FillPath(outerBrush, outerPath);
            }

            // Recessed LCD panel
            Rectangle lcdRect = new Rectangle(rect.X + 4, rect.Y + 4, rect.Width - 8, rect.Height - 8);
            using (GraphicsPath lcdPath = GetRoundedRect(lcdRect, 4))
            using (SolidBrush lcdBrush = new SolidBrush(_theme.DisplayBackground))
            using (Pen innerShadowPen = new Pen(Color.FromArgb(60, 0, 0, 0), 1.5f))
            {
                g.FillPath(lcdBrush, lcdPath);
                g.DrawPath(innerShadowPen, lcdPath);
            }

            // Subtle dot-matrix grid overlay simulation
            using (Pen dotPen = new Pen(Color.FromArgb(12, 0, 0, 0), 1f))
            {
                for (int y = lcdRect.Top + 16; y < lcdRect.Bottom; y += 4)
                {
                    g.DrawLine(dotPen, lcdRect.Left + 2, y, lcdRect.Right - 2, y);
                }
            }

            // Top Status Badges strip
            DrawCasioBadges(g, lcdRect.Left + 6, lcdRect.Top + 4);

            // Expression line (Top text line)
            string displayExpr = FormatCasioMathText(_expression);
            using (Font exprFont = new Font("Lucida Console", 11f, FontStyle.Regular))
            using (SolidBrush exprBrush = new SolidBrush(_theme.TextDisplayExpression))
            {
                string text = displayExpr + (_showCaret ? "\u258c" : " ");
                Rectangle exprRect = new Rectangle(lcdRect.Left + 6, lcdRect.Top + 20, lcdRect.Width - 12, 24);
                StringFormat sfExpr = new StringFormat
                {
                    Alignment = StringAlignment.Near,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisPath
                };
                g.DrawString(text, exprFont, exprBrush, exprRect, sfExpr);
            }

            // Result line (Bottom text line)
            float fontSize = rect.Height < 110 ? 18f : 22f;
            using (Font resFont = new Font("Lucida Console", fontSize, FontStyle.Bold))
            using (SolidBrush resBrush = new SolidBrush(_theme.TextDisplayResult))
            {
                Rectangle resRect = new Rectangle(lcdRect.Left + 6, lcdRect.Bottom - 36, lcdRect.Width - 12, 32);
                StringFormat sfRes = new StringFormat
                {
                    Alignment = StringAlignment.Far,
                    LineAlignment = StringAlignment.Center
                };
                g.DrawString(_result, resFont, resBrush, resRect, sfRes);
            }

            // Fraction indicator
            if (!string.IsNullOrEmpty(_fraction))
            {
                using (Font fracFont = new Font("Lucida Console", 9f, FontStyle.Regular))
                using (SolidBrush fracBrush = new SolidBrush(_theme.TextDisplayExpression))
                {
                    g.DrawString("[a b/c]: " + _fraction, fracFont, fracBrush, lcdRect.Left + 6, lcdRect.Bottom - 16);
                }
            }
        }

        private void DrawModernBadges(Graphics g, int x, int y)
        {
            using (Font font = new Font("Segoe UI", 8f, FontStyle.Bold))
            {
                DrawBadge(g, "SHIFT", _isShiftActive, _theme.ShiftColor, font, ref x, y);
                DrawBadge(g, "ALPHA", _isAlphaActive, _theme.AlphaColor, font, ref x, y);
                DrawBadge(g, _angleUnit == AngleUnit.Degrees ? "DEG" : (_angleUnit == AngleUnit.Radians ? "RAD" : "GRA"), true, _theme.BadgeActive, font, ref x, y);
                if (_hasMemory) DrawBadge(g, "M", true, _theme.TextSecondary, font, ref x, y);
                DrawBadge(g, "MATH", true, _theme.BadgeInactive, font, ref x, y);
            }
        }

        private void DrawCasioBadges(Graphics g, int x, int y)
        {
            using (Font font = new Font("Lucida Console", 7.5f, FontStyle.Bold))
            {
                DrawBadge(g, "S", _isShiftActive, _theme.TextDisplayResult, font, ref x, y);
                DrawBadge(g, "A", _isAlphaActive, _theme.TextDisplayResult, font, ref x, y);
                DrawBadge(g, _angleUnit == AngleUnit.Degrees ? "D" : (_angleUnit == AngleUnit.Radians ? "R" : "G"), true, _theme.TextDisplayResult, font, ref x, y);
                if (_hasMemory) DrawBadge(g, "M", true, _theme.TextDisplayResult, font, ref x, y);
                DrawBadge(g, "Math\u25b2\u25bc", true, _theme.TextDisplayResult, font, ref x, y);
            }
        }

        private void DrawBadge(Graphics g, string label, bool active, Color activeColor, Font font, ref int x, int y)
        {
            Color c = active ? activeColor : _theme.BadgeInactive;
            using (SolidBrush brush = new SolidBrush(c))
            {
                SizeF size = g.MeasureString(label, font);
                g.DrawString(label, font, brush, x, y);
                x += (int)size.Width + 8;
            }
        }

        private string FormatCasioMathText(string expr)
        {
            if (string.IsNullOrEmpty(expr)) return "";
            return expr.Replace("*", "\u00d7")
                       .Replace("/", "\u00f7")
                       .Replace("diff(", "\u2202/\u2202x(")
                       .Replace("integ(", "\u222b(")
                       .Replace("sqrt(", "\u221a(")
                       .Replace("pi", "\u03c0");
        }

        private static GraphicsPath GetRoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
