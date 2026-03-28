using System.Drawing;
using System.Windows.Forms;

namespace KeyRep
{
    internal static class UiTheme
    {
        public static readonly Color Bg = Color.FromArgb(22, 22, 30);
        public static readonly Color BgPanel = Color.FromArgb(30, 30, 42);
        public static readonly Color BgHeader = Color.FromArgb(26, 26, 38);
        public static readonly Color BgInput = Color.FromArgb(42, 42, 58);
        public static readonly Color Text = Color.FromArgb(235, 235, 242);
        public static readonly Color TextMuted = Color.FromArgb(150, 150, 170);
        public static readonly Color Accent = Color.FromArgb(212, 168, 55);
        public static readonly Color AccentDim = Color.FromArgb(90, 72, 28);
        public static readonly Color Border = Color.FromArgb(58, 58, 78);

        public static Font UiFont { get; } = new("Segoe UI", 9f, FontStyle.Regular, GraphicsUnit.Point);
        public static Font UiFontBold { get; } = new("Segoe UI", 9f, FontStyle.Bold, GraphicsUnit.Point);
        public static Font TitleFont { get; } = new("Segoe UI Semibold", 10.5f, FontStyle.Bold, GraphicsUnit.Point);

        public static void ApplyRoot(Form form)
        {
            form.BackColor = Bg;
            form.ForeColor = Text;
            form.Font = UiFont;
            ApplyChildren(form);
        }

        private static void ApplyChildren(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                StyleControl(c);
                ApplyChildren(c);
            }
        }

        private static void StyleControl(Control c)
        {
            switch (c)
            {
                case Panel p when p.Tag as string == "header":
                    p.BackColor = BgHeader;
                    p.ForeColor = Text;
                    return;
                case Panel p when p.Tag as string == "card":
                    p.BackColor = BgPanel;
                    p.ForeColor = Text;
                    return;
                case FlowLayoutPanel:
                case TableLayoutPanel:
                    c.BackColor = Color.Transparent;
                    c.ForeColor = Text;
                    return;
                case Panel:
                    c.BackColor = Color.Transparent;
                    c.ForeColor = Text;
                    return;
                case Label lbl when lbl.Tag as string == "title":
                    lbl.ForeColor = Accent;
                    lbl.Font = TitleFont;
                    return;
                case Label lbl when lbl.Tag as string == "section":
                    lbl.ForeColor = Accent;
                    lbl.Font = UiFontBold;
                    return;
                case Label:
                    c.ForeColor = TextMuted;
                    return;
                case Button b when b.Tag as string == "ghost":
                    StyleGhostButton(b);
                    return;
                case Button b when b.Tag as string == "accent":
                    StyleAccentButton(b);
                    return;
                case Button b:
                    StyleGhostButton(b);
                    return;
                case TextBox tb:
                    tb.BackColor = BgInput;
                    tb.ForeColor = Text;
                    tb.BorderStyle = BorderStyle.FixedSingle;
                    return;
                case ComboBox cb:
                    cb.BackColor = BgInput;
                    cb.ForeColor = Text;
                    cb.FlatStyle = FlatStyle.Flat;
                    return;
                case NumericUpDown n:
                    n.BackColor = BgInput;
                    n.ForeColor = Text;
                    return;
                case CheckBox chk:
                    StyleToggleLike(chk);
                    return;
                case RadioButton r:
                    StyleToggleLike(r);
                    return;
                case TrackBar t:
                    t.BackColor = BgPanel;
                    t.ForeColor = Text;
                    return;
                case PictureBox pb:
                    pb.BackColor = Color.Transparent;
                    return;
            }
        }

        /// <summary>Button-style check/radio so checked state is visible on dark backgrounds.</summary>
        private static void StyleToggleLike(CheckBox c)
        {
            c.FlatStyle = FlatStyle.Flat;
            c.Appearance = Appearance.Button;
            c.BackColor = BgInput;
            c.ForeColor = Text;
            c.FlatAppearance.BorderColor = Border;
            c.FlatAppearance.CheckedBackColor = AccentDim;
            c.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 55, 72);
            c.FlatAppearance.MouseDownBackColor = Color.FromArgb(62, 62, 82);
            c.Cursor = Cursors.Hand;
        }

        private static void StyleToggleLike(RadioButton r)
        {
            r.FlatStyle = FlatStyle.Flat;
            r.Appearance = Appearance.Button;
            r.BackColor = BgInput;
            r.ForeColor = Text;
            r.FlatAppearance.BorderColor = Border;
            r.FlatAppearance.CheckedBackColor = AccentDim;
            r.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 55, 72);
            r.FlatAppearance.MouseDownBackColor = Color.FromArgb(62, 62, 82);
            r.Cursor = Cursors.Hand;
        }

        private static void StyleAccentButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Accent;
            b.BackColor = AccentDim;
            b.ForeColor = Text;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(110, 88, 32);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(130, 100, 36);
            b.Cursor = Cursors.Hand;
        }

        private static void StyleGhostButton(Button b)
        {
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 1;
            b.FlatAppearance.BorderColor = Border;
            b.BackColor = BgInput;
            b.ForeColor = Text;
            b.FlatAppearance.MouseOverBackColor = Color.FromArgb(55, 55, 72);
            b.FlatAppearance.MouseDownBackColor = Color.FromArgb(62, 62, 82);
            b.Cursor = Cursors.Hand;
        }
    }
}
