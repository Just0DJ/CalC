using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.Windows.Forms;

namespace TitanCalc.UI
{
    public enum ButtonRole
    {
        Number,
        Function,
        Accent,
        Danger
    }

    /// <summary>
    /// Dual-rendered calculator button supporting Modern Material 3 flat surfaces
    /// and Retro Casio fx-991 sculpted beveled keycaps with Shift & Alpha legends.
    /// </summary>
    public class CalculatorButton : Control
    {
        private string _primaryText = "";
        private string _shiftText = "";
        private string _alphaText = "";
        private ButtonRole _role = ButtonRole.Number;
        private ThemeColors _theme;
        private bool _isHovered = false;
        private bool _isPressed = false;

        public string PrimaryText
        {
            get { return _primaryText; }
            set { _primaryText = value; Invalidate(); }
        }

        public string ShiftText
        {
            get { return _shiftText; }
            set { _shiftText = value; Invalidate(); }
        }

        public string AlphaText
        {
            get { return _alphaText; }
            set { _alphaText = value; Invalidate(); }
        }

        public ButtonRole Role
        {
            get { return _role; }
            set { _role = value; Invalidate(); }
        }

        public ThemeColors Theme
        {
            get { return _theme; }
            set { _theme = value; Invalidate(); }
        }

        public CalculatorButton()
        {
            SetStyle(ControlStyles.AllPaintingInWmPaint |
                     ControlStyles.UserPaint |
                     ControlStyles.OptimizedDoubleBuffer |
                     ControlStyles.ResizeRedraw, true);

            _theme = RenderTheme.GetModernTheme();
            Cursor = Cursors.Hand;
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);
            _isHovered = true;
            Invalidate();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);
            _isHovered = false;
            _isPressed = false;
            Invalidate();
        }

        protected override void OnMouseDown(MouseEventArgs e)
        {
            base.OnMouseDown(e);
            if (e.Button == MouseButtons.Left)
            {
                _isPressed = true;
                Invalidate();
            }
        }

        protected override void OnMouseUp(MouseEventArgs e)
        {
            base.OnMouseUp(e);
            _isPressed = false;
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            Graphics g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;

            Rectangle rect = ClientRectangle;

            if (_theme.IsRetroCasio)
            {
                DrawRetroCasioButton(g, rect);
            }
            else
            {
                DrawModernButton(g, rect);
            }
        }

        private void DrawModernButton(Graphics g, Rectangle rect)
        {
            Rectangle btnRect = new Rectangle(rect.X + 2, rect.Y + 2, rect.Width - 4, rect.Height - 4);
            Color bg, border, text;

            switch (_role)
            {
                case ButtonRole.Accent:
                    bg = _isPressed ? _theme.KeyAccentPressed : (_isHovered ? _theme.KeyAccentHover : _theme.KeyAccentBackground);
                    border = _theme.KeyAccentHover;
                    text = _theme.KeyAccentText;
                    break;
                case ButtonRole.Danger:
                    bg = _isPressed ? _theme.KeyDangerBackground : (_isHovered ? _theme.KeyDangerHover : _theme.KeyDangerBackground);
                    border = _theme.KeyDangerHover;
                    text = _theme.KeyDangerText;
                    break;
                case ButtonRole.Function:
                    bg = _isPressed ? _theme.KeyFunctionPressed : (_isHovered ? _theme.KeyFunctionHover : _theme.KeyFunctionBackground);
                    border = _theme.KeyFunctionBorder;
                    text = _theme.KeyFunctionText;
                    break;
                case ButtonRole.Number:
                default:
                    bg = _isPressed ? _theme.KeyNumberPressed : (_isHovered ? _theme.KeyNumberHover : _theme.KeyNumberBackground);
                    border = _theme.KeyNumberBorder;
                    text = _theme.KeyNumberText;
                    break;
            }

            using (GraphicsPath path = GetRoundedRect(btnRect, 6))
            using (SolidBrush brush = new SolidBrush(bg))
            using (Pen pen = new Pen(border, 1f))
            using (SolidBrush textBrush = new SolidBrush(text))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);

                float fontSize = (rect.Height < 40 || _primaryText.Length > 3) ? 10.5f : 13.5f;
                using (Font font = new Font("Segoe UI", fontSize, FontStyle.Regular))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };
                    g.DrawString(_primaryText, font, textBrush, btnRect, sf);
                }
            }
        }

        private void DrawRetroCasioButton(Graphics g, Rectangle rect)
        {
            bool hasLegends = !string.IsNullOrEmpty(_shiftText) || !string.IsNullOrEmpty(_alphaText);
            int legendHeight = hasLegends ? 14 : 0;

            // Draw top Shift (Gold) and Alpha (Red) printed legends on the calculator casing
            if (hasLegends)
            {
                using (Font legendFont = new Font("Lucida Console", 7.5f, FontStyle.Bold))
                {
                    if (!string.IsNullOrEmpty(_shiftText))
                    {
                        using (SolidBrush shiftBrush = new SolidBrush(_theme.ShiftColor))
                        {
                            g.DrawString(_shiftText, legendFont, shiftBrush, rect.X + 2, rect.Y);
                        }
                    }
                    if (!string.IsNullOrEmpty(_alphaText))
                    {
                        using (SolidBrush alphaBrush = new SolidBrush(_theme.AlphaColor))
                        {
                            SizeF sz = g.MeasureString(_alphaText, legendFont);
                            g.DrawString(_alphaText, legendFont, alphaBrush, rect.Right - sz.Width - 2, rect.Y);
                        }
                    }
                }
            }

            // Keycap rectangle
            Rectangle keyRect = new Rectangle(rect.X + 2, rect.Y + legendHeight + 1, rect.Width - 4, rect.Height - legendHeight - 3);
            if (keyRect.Height < 8) return;

            Color baseColor, textColor, borderColor;
            switch (_role)
            {
                case ButtonRole.Accent:
                    baseColor = _isPressed ? _theme.KeyAccentPressed : (_isHovered ? _theme.KeyAccentHover : _theme.KeyAccentBackground);
                    textColor = _theme.KeyAccentText;
                    borderColor = Color.FromArgb(70, 150, 230);
                    break;
                case ButtonRole.Danger:
                    baseColor = _isPressed ? _theme.KeyDangerBackground : (_isHovered ? _theme.KeyDangerHover : _theme.KeyDangerBackground);
                    textColor = _theme.KeyDangerText;
                    borderColor = Color.FromArgb(240, 80, 60);
                    break;
                case ButtonRole.Function:
                    baseColor = _isPressed ? _theme.KeyFunctionPressed : (_isHovered ? _theme.KeyFunctionHover : _theme.KeyFunctionBackground);
                    textColor = _theme.KeyFunctionText;
                    borderColor = _theme.KeyFunctionBorder;
                    break;
                case ButtonRole.Number:
                default:
                    baseColor = _isPressed ? _theme.KeyNumberPressed : (_isHovered ? _theme.KeyNumberHover : _theme.KeyNumberBackground);
                    textColor = _theme.KeyNumberText;
                    borderColor = _theme.KeyNumberBorder;
                    break;
            }

            // 3D Sculpted Bevel / Keycap Gradient
            Color topGrad = ControlPaint.Light(baseColor, 0.25f);
            Color botGrad = ControlPaint.Dark(baseColor, 0.25f);
            if (_isPressed)
            {
                // Invert gradient when pressed for tactile depth
                Color t = topGrad; topGrad = botGrad; botGrad = t;
            }

            using (GraphicsPath keyPath = GetRoundedRect(keyRect, 4))
            using (LinearGradientBrush gradBrush = new LinearGradientBrush(keyRect, topGrad, botGrad, LinearGradientMode.Vertical))
            using (Pen borderPen = new Pen(borderColor, 1f))
            {
                g.FillPath(gradBrush, keyPath);
                g.DrawPath(borderPen, keyPath);

                // Subtle top edge specular highlight
                if (!_isPressed && keyRect.Height > 16)
                {
                    using (Pen highlightPen = new Pen(Color.FromArgb(80, 255, 255, 255), 1f))
                    {
                        g.DrawLine(highlightPen, keyRect.Left + 4, keyRect.Top + 2, keyRect.Right - 4, keyRect.Top + 2);
                    }
                }

                // Primary label in center of sculpted keycap
                float fontSize = (keyRect.Height < 30 || _primaryText.Length > 3) ? 9.5f : 12f;
                using (Font font = new Font("Segoe UI", fontSize, FontStyle.Bold))
                using (SolidBrush textBrush = new SolidBrush(textColor))
                {
                    StringFormat sf = new StringFormat
                    {
                        Alignment = StringAlignment.Center,
                        LineAlignment = StringAlignment.Center
                    };

                    // Draw text (nudged down slightly when pressed)
                    Rectangle textRect = keyRect;
                    if (_isPressed) textRect.Offset(0, 1);
                    g.DrawString(_primaryText, font, textBrush, textRect, sf);
                }
            }
        }

        private static GraphicsPath GetRoundedRect(Rectangle r, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            int d = radius * 2;
            if (d > r.Width) d = r.Width;
            if (d > r.Height) d = r.Height;
            if (d <= 0) { path.AddRectangle(r); return path; }

            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
