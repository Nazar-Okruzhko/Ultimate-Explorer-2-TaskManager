// -------------------------------------------------------------------------------
//  WINDOWS EXPLORER  –  Pixel-Perfect GDI+ Recreation
//  C# .NET Framework 4.8  |  Single File  |  Custom GDI+ Rendering
// -------------------------------------------------------------------------------
//  ICONS  ?  Place 24×24 PNG files in:  <exe-dir>\Explorer.exe\icons\Win10\
//
//  Required icon names (filename without .png):
//    backward_arrow        forward_arrow         previous_small_arrow
//    up_arrow              reload                search
//    organize              new_folder            change_view
//    more_options          preview_pane          help
//    quick_access          this_pc               network
//    desktop               downloads             documents
//    pictures              music                 videos
//    3dobjects             drives                folder
//    file
//  All 24×24 pixels.  Missing icons show a generated placeholder.
// -------------------------------------------------------------------------------
 
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
 
namespace WinExplorer
{
    // --------------------------------------------------------------------------
    //  Entry Point
    // --------------------------------------------------------------------------
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new ExplorerForm());
        }
    }
 
    // --------------------------------------------------------------------------
    //  Theme  (all colours / fonts in one place)
    // --------------------------------------------------------------------------
    static class Th
    {
        public static readonly Color Bg         = Color.FromArgb(240, 240, 240);
        public static readonly Color SelFill    = Color.FromArgb(204, 232, 255);   // #CCE8FF
        public static readonly Color SelBorder  = Color.FromArgb(153, 209, 255);   // #99D1FF
        public static readonly Color HoverFill  = Color.FromArgb(229, 241, 251);
        public static readonly Color HoverBord  = Color.FromArgb(0, 120, 215);
        public static readonly Color PressFill  = Color.FromArgb(204, 228, 247);
        public static readonly Color PressBord  = Color.FromArgb(0, 84, 153);
        public static readonly Color SepColor   = Color.FromArgb(208, 208, 208);
        public static readonly Color Border     = Color.FromArgb(180, 180, 180);
        public static readonly Color PaneSep    = Color.FromArgb(213, 213, 213);
        public static readonly Color HdrBg      = Color.FromArgb(240, 240, 240);
        public static readonly Color HdrBorder  = Color.FromArgb(200, 200, 200);
        public static readonly Color TreeBg     = Color.White;
        public static readonly Color ContentBg  = Color.White;
        public static readonly Color TxtColor   = Color.FromArgb(0, 0, 0);
        public static readonly Color TxtDisabled= Color.FromArgb(130, 130, 130);
 
        public static readonly Font UiFont  = new Font("Segoe UI", 9f);
        public static readonly Font UiSmall = new Font("Segoe UI", 8f);
        public static readonly Font UiBold  = new Font("Segoe UI", 9f, FontStyle.Bold);
 
        public static void DrawHover(Graphics g, Rectangle r)
        {
            using (var b = new SolidBrush(HoverFill)) g.FillRectangle(b, r);
            using (var p = new Pen(HoverBord))
                g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
        }
        public static void DrawPress(Graphics g, Rectangle r)
        {
            using (var b = new SolidBrush(PressFill)) g.FillRectangle(b, r);
            using (var p = new Pen(PressBord))
                g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
        }
        public static void DrawSel(Graphics g, Rectangle r)
        {
            using (var b = new SolidBrush(SelFill)) g.FillRectangle(b, r);
            using (var p = new Pen(SelBorder))
                g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
        }
 
        // Draw a small solid downward triangle  ?
        public static void DrawDropArrow(Graphics g, int cx, int cy, Color col)
        {
            Point[] pts = { new Point(cx - 3, cy - 2), new Point(cx + 3, cy - 2), new Point(cx, cy + 2) };
            using (var b = new SolidBrush(col)) g.FillPolygon(b, pts);
        }
    }
 
    // --------------------------------------------------------------------------
    //  Icon Cache / Loader
    // --------------------------------------------------------------------------
    static class Icons
    {
        static readonly string Dir = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "Explorer.exe", "icons", "Win10");
 
        static readonly Dictionary<string, Image> Cache =
            new Dictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
 
        public static Image Get(string name)
        {
            if (Cache.TryGetValue(name, out var cached)) return cached;
            string file = Path.Combine(Dir, name + ".png");
            Image img = null;
            if (File.Exists(file))
            {
                try
                {
                    var raw = Image.FromFile(file);
                    if (raw.Width == 24 && raw.Height == 24) { img = raw; }
                    else
                    {
                        var bmp = new Bitmap(24, 24);
                        using (var g = Graphics.FromImage(bmp))
                        { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(raw, 0, 0, 24, 24); }
                        raw.Dispose(); img = bmp;
                    }
                }
                catch { img = null; }
            }
            if (img == null) img = MakePlaceholder(name);
            Cache[name] = img;
            return img;
        }
 
        static Image MakePlaceholder(string name)
        {
            var bmp = new Bitmap(24, 24);
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.Transparent);
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
 
                bool isFolder = IsFolder(name);
                bool isArrow  = name.Contains("arrow") || name == "backward" || name == "forward";
 
                if (isFolder)
                {
                    // Solid folder silhouette
                    using (var bf = new SolidBrush(Color.FromArgb(255, 213, 84)))
                    {
                        g.FillRectangle(bf, 2, 9, 20, 12);
                        g.FillRectangle(bf, 2, 6, 8, 4);
                    }
                    using (var pf = new Pen(Color.FromArgb(190, 150, 30), 1f))
                    {
                        g.DrawRectangle(pf, 2, 9, 19, 11);
                        g.DrawRectangle(pf, 2, 6, 7, 3);
                    }
                }
                else if (isArrow || name.Contains("backward") || name.Contains("forward") || name.Contains("up_arrow"))
                {
                    using (var p = new Pen(Color.FromArgb(70, 130, 180), 2f) { EndCap = LineCap.Round, StartCap = LineCap.Round })
                    {
                        if (name.Contains("backward") || name.Contains("back"))
                        { g.DrawLine(p, 17, 12, 7, 12); g.DrawLine(p, 7, 12, 12, 7); g.DrawLine(p, 7, 12, 12, 17); }
                        else if (name.Contains("forward"))
                        { g.DrawLine(p, 7, 12, 17, 12); g.DrawLine(p, 17, 12, 12, 7); g.DrawLine(p, 17, 12, 12, 17); }
                        else if (name.Contains("up"))
                        { g.DrawLine(p, 12, 17, 12, 7); g.DrawLine(p, 12, 7, 7, 12); g.DrawLine(p, 12, 7, 17, 12); }
                        else
                        { g.DrawLine(p, 5, 12, 19, 12); g.DrawLine(p, 5, 12, 10, 7); g.DrawLine(p, 5, 12, 10, 17); }
                    }
                }
                else if (name == "search")
                {
                    using (var p = new Pen(Color.FromArgb(80, 80, 80), 2f))
                    {
                        g.DrawEllipse(p, 4, 4, 12, 12);
                        g.DrawLine(p, 14, 14, 19, 19);
                    }
                }
                else if (name == "preview_pane")
                {
                    using (var p = new Pen(Color.FromArgb(80, 80, 80), 1.5f))
                    {
                        g.DrawRectangle(p, 3, 4, 18, 16);
                        g.DrawLine(p, 12, 4, 12, 20);
                    }
                }
                else if (name == "help")
                {
                    using (var p = new Pen(Color.FromArgb(0, 102, 204), 2f))
                        g.DrawEllipse(p, 2, 2, 19, 19);
                    using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString("?", new Font("Segoe UI", 10f, FontStyle.Bold), new SolidBrush(Color.FromArgb(0, 102, 204)),
                            new RectangleF(0, 0, 24, 24), fmt);
                }
                else if (name == "network")
                {
                    using (var p = new Pen(Color.FromArgb(80, 80, 80), 1.5f))
                    {
                        g.DrawEllipse(p, 8, 3, 8, 6);
                        g.DrawEllipse(p, 2, 14, 6, 6);
                        g.DrawEllipse(p, 16, 14, 6, 6);
                        g.DrawLine(p, 12, 9, 5, 14); g.DrawLine(p, 12, 9, 19, 14);
                    }
                }
                else if (name == "this_pc")
                {
                    using (var p = new Pen(Color.FromArgb(80, 80, 80), 1.5f))
                    {
                        g.DrawRectangle(p, 3, 4, 18, 12);
                        g.DrawLine(p, 10, 16, 10, 19); g.DrawLine(p, 14, 16, 14, 19);
                        g.DrawLine(p, 7, 19, 17, 19);
                    }
                }
                else if (name == "change_view" || name == "more_options")
                {
                    using (var p = new Pen(Color.FromArgb(80, 80, 80), 1.5f))
                    {
                        g.DrawRectangle(p, 2, 3, 8, 7);
                        g.DrawRectangle(p, 14, 3, 8, 7);
                        g.DrawRectangle(p, 2, 14, 8, 7);
                        g.DrawRectangle(p, 14, 14, 8, 7);
                    }
                }
                else if (name == "new_folder")
                {
                    // folder with +
                    using (var bf = new SolidBrush(Color.FromArgb(255, 213, 84)))
                    { g.FillRectangle(bf, 2, 9, 16, 11); g.FillRectangle(bf, 2, 6, 8, 4); }
                    using (var p = new Pen(Color.FromArgb(80, 80, 80), 2f))
                    { g.DrawLine(p, 17, 13, 22, 13); g.DrawLine(p, 20, 10, 20, 17); }
                }
                else if (name == "organize")
                {
                    using (var p = new Pen(Color.FromArgb(80, 80, 80), 1.5f))
                    {
                        g.DrawLine(p, 3, 6, 21, 6); g.DrawLine(p, 3, 12, 21, 12); g.DrawLine(p, 3, 18, 21, 18);
                        g.DrawLine(p, 3, 3, 3, 21);
                    }
                }
                else if (name == "file")
                {
                    using (var p = new Pen(Color.FromArgb(80, 80, 80), 1.5f))
                    {
                        var pts = new Point[] { new Point(5, 3), new Point(15, 3), new Point(19, 7), new Point(19, 21), new Point(5, 21) };
                        g.DrawPolygon(p, pts);
                        g.DrawLine(p, 15, 3, 15, 7); g.DrawLine(p, 15, 7, 19, 7);
                    }
                }
                else if (name == "quick_access")
                {
                    // Star icon
                    using (var b = new SolidBrush(Color.FromArgb(255, 185, 0)))
                    {
                        Point[] star = GetStarPoints(12, 12, 10, 4, 5);
                        g.FillPolygon(b, star);
                    }
                }
                else
                {
                    // Generic: small rectangle with label initial
                    using (var p = new Pen(Color.FromArgb(130, 130, 130), 1f))
                        g.DrawRectangle(p, 3, 3, 17, 17);
                    using (var fmt = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                        g.DrawString(name.Length > 0 ? name[0].ToString().ToUpper() : "?",
                            new Font("Segoe UI", 7f), Brushes.Gray,
                            new RectangleF(0, 0, 24, 24), fmt);
                }
            }
            return bmp;
        }
 
        static bool IsFolder(string name) =>
            name == "folder" || name == "quick_access" || name == "desktop" ||
            name == "downloads" || name == "documents" || name == "pictures" ||
            name == "music" || name == "videos" || name == "3dobjects" ||
            name == "drives" || name == "this_pc";
 
        static Point[] GetStarPoints(int cx, int cy, int outerR, int innerR, int numPoints)
        {
            var pts = new Point[numPoints * 2];
            double step = Math.PI / numPoints;
            for (int i = 0; i < numPoints * 2; i++)
            {
                double a = i * step - Math.PI / 2;
                int r = (i % 2 == 0) ? outerR : innerR;
                pts[i] = new Point((int)(cx + r * Math.Cos(a)), (int)(cy + r * Math.Sin(a)));
            }
            return pts;
        }
    }
 
    // --------------------------------------------------------------------------
    //  Data Models
    // --------------------------------------------------------------------------
    enum SortCol  { Name, Date, Type, Size }
    enum SortDir  { Asc, Desc }
    enum ViewMode { Details, LargeIcons, MediumIcons, SmallIcons, List,
                    ExtraLargeIcons, Tiles, Content }
 
    class TreeNode2          // avoid conflict with WinForms TreeNode
    {
        public string  Label;
        public string  Path;       // null for virtual roots
        public string  IconName;
        public bool    Expanded;
        public bool    IsVirtual;
        public bool    IsRoot;     // Quick Access / This PC / Network
        public int     Level;
        public List<TreeNode2> Children = new List<TreeNode2>();
        public TreeNode2 Parent;
        public bool    HasChildren;   // allows expand arrow even before load
        public Rectangle Bounds;      // filled during draw pass
    }
 
    class ContentItem
    {
        public string   Name;
        public string   FullPath;
        public DateTime DateModified;
        public string   ItemType;
        public long     Size;
        public bool     IsDirectory;
        public bool     Selected;
 
        public string SizeStr =>
            IsDirectory ? "" :
            Size < 1024            ? $"{Size} B" :
            Size < 1_048_576       ? $"{Size / 1024.0:F1} KB" :
                                     $"{Size / 1_048_576.0:F1} MB";
 
        public string DateStr => DateModified == default ? "" :
            DateModified.ToString("M/d/yyyy h:mm tt");
    }
 
    // --------------------------------------------------------------------------
    //  Custom Menu Renderer (Windows-10-style menus with icons)
    // --------------------------------------------------------------------------
    class ExplorerMenuRenderer : ToolStripProfessionalRenderer
    {
        public ExplorerMenuRenderer() : base(new ExplorerColorTable()) { }
 
        protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Available) return;
            if (e.Item.Selected && e.Item.Enabled)
            {
                var r = new Rectangle(2, 0, e.Item.Width - 4, e.Item.Height - 1);
                e.Graphics.Clear(Color.White);
                Th.DrawSel(e.Graphics, r);
            }
            else e.Graphics.Clear(Color.White);
        }
 
        protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var p = new Pen(Th.SepColor))
                e.Graphics.DrawLine(p, 30, y, e.Item.Width - 4, y);
        }
 
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Color.Black : Th.TxtDisabled;
            e.Graphics.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            base.OnRenderItemText(e);
        }
 
        protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
            => e.Graphics.Clear(Color.White);
 
        protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
        {
            using (var p = new Pen(Th.Border))
                e.Graphics.DrawRectangle(p, 0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
        }
 
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = e.Item.Enabled ? Color.Black : Th.TxtDisabled;
            base.OnRenderArrow(e);
        }
 
        protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e)
        {
            if (e.Image == null) return;
            var r = e.ImageRectangle;
            e.Graphics.DrawImage(e.Image, r.X, r.Y, 16, 16);
        }
    }
 
    class ExplorerColorTable : ProfessionalColorTable
    {
        public override Color MenuBorder                       => Th.Border;
        public override Color MenuItemBorder                   => Color.Transparent;
        public override Color MenuItemSelected                 => Th.SelFill;
        public override Color MenuItemSelectedGradientBegin    => Th.SelFill;
        public override Color MenuItemSelectedGradientEnd      => Th.SelFill;
        public override Color ToolStripDropDownBackground      => Color.White;
        public override Color ImageMarginGradientBegin         => Color.White;
        public override Color ImageMarginGradientMiddle        => Color.White;
        public override Color ImageMarginGradientEnd           => Color.White;
    }
 
    // --------------------------------------------------------------------------
    //  TOP NAVIGATION BAR
    //  Height: 34 px
    //  Layout (L?R): Back(28) · Fwd(28) · RecentLoc(14) · Up(28) ·
    //                [PathBox fills] · RecentFold(14) · [12] · [SearchBox 202] · [12]
    // --------------------------------------------------------------------------
    class TopNavBar : Panel
    {
        public const int BAR_H = 34;
        const int BTN_Y  = 6;   // top of icon area
        const int BTN_H  = 22;  // height of icon area (=path box height)
        const int ICON   = 16;  // icon draw size inside buttons
 
        // Button rectangles (recalculated in Resize)
        Rectangle _rBack, _rFwd, _rRecLoc, _rUp, _rRecFold;
 
        // Hover / press tracking
        enum HitBtn { None, Back, Fwd, RecLoc, Up, RecFold }
        HitBtn _hov = HitBtn.None, _prs = HitBtn.None;
 
        bool _backEnabled = false, _fwdEnabled = false;
 
        TextBox _pathBox;
        Panel   _searchPanel;
        TextBox _searchBox;
 
        public event EventHandler BackClick;
        public event EventHandler ForwardClick;
        public event EventHandler UpClick;
        public event Action<string> Navigate;
        public event Action<string> SearchChanged;
 
        public string CurrentPath
        {
            get => _pathBox.Text;
            set { _pathBox.Text = value; }
        }
        public bool BackEnabled  { get => _backEnabled; set { _backEnabled = value; Invalidate(); } }
        public bool ForwardEnabled { get => _fwdEnabled; set { _fwdEnabled = value; Invalidate(); } }
 
        public TopNavBar()
        {
            Height = BAR_H;
            Dock   = DockStyle.Top;
            BackColor = Th.Bg;
            DoubleBuffered = true;
 
            // Path TextBox
            _pathBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Th.UiFont,
                BackColor = Color.White,
                ForeColor = Th.TxtColor,
            };
            _pathBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Return) { Navigate?.Invoke(_pathBox.Text); e.Handled = true; }
            };
            Controls.Add(_pathBox);
 
            // Search Panel (custom bordered)
            _searchPanel = new Panel { BackColor = Color.White };
            _searchPanel.Paint += DrawSearchPanel;
 
            _searchBox = new TextBox
            {
                BorderStyle = BorderStyle.None,
                Font = Th.UiFont,
                BackColor = Color.White,
                ForeColor = Th.TxtDisabled,
                Text = "Search",
            };
            _searchBox.GotFocus  += (s, e) => { if (_searchBox.Text == "Search") { _searchBox.Text = ""; _searchBox.ForeColor = Th.TxtColor; } };
            _searchBox.LostFocus += (s, e) => { if (_searchBox.Text == "") { _searchBox.Text = "Search"; _searchBox.ForeColor = Th.TxtDisabled; } };
            _searchBox.TextChanged += (s, e) => SearchChanged?.Invoke(_searchBox.Text);
 
            _searchPanel.Controls.Add(_searchBox);
            Controls.Add(_searchPanel);
 
            // Mouse events
            MouseMove   += OnMM;
            MouseDown   += OnMD;
            MouseUp     += OnMU;
            MouseLeave  += (s, e) => { _hov = HitBtn.None; Invalidate(); };
 
            // Layout on resize
            Resize += (s, e) => DoLayout();
        }
 
        void DrawSearchPanel(object sender, PaintEventArgs e)
        {
            var g = e.Graphics;
            g.Clear(Color.White);
            using (var p = new Pen(Th.PaneSep))
                g.DrawRectangle(p, 0, 0, _searchPanel.Width - 1, _searchPanel.Height - 1);
            // Search icon on right
            var icon = Icons.Get("search");
            g.DrawImage(icon, _searchPanel.Width - 20, 3, 14, 14);
        }
 
        void DoLayout()
        {
            int x = 2;
            _rBack   = new Rectangle(x, BTN_Y, 28, BTN_H); x += 28;
            _rFwd    = new Rectangle(x, BTN_Y, 28, BTN_H); x += 28;
            _rRecLoc = new Rectangle(x, BTN_Y, 14, BTN_H); x += 14 + 2;
            _rUp     = new Rectangle(x, BTN_Y, 28, BTN_H); x += 28 + 2;
 
            // Path box: from x to (Width - 12 - 202 - 12 - 14 - 2)
            const int searchW  = 202;
            const int padRight = 12;
            const int recFW    = 14;
            int pathRight = Width - padRight - searchW - padRight - recFW - 2;
            int pathW     = Math.Max(40, pathRight - x - 1);
 
            // PathBox inside a 22px-tall bordered area
            int pbX = x, pbY = BTN_Y, pbW = pathW, pbH = BTN_H;
            _pathBox.SetBounds(pbX + 3, pbY + 3, pbW - 6, pbH - 6);
 
            x += pathW + 2;
            _rRecFold = new Rectangle(x, BTN_Y, recFW, BTN_H); x += recFW + 12;
 
            // Search panel
            _searchPanel.SetBounds(x, BTN_Y, searchW, BTN_H);
            _searchBox.SetBounds(3, 3, searchW - 22, BTN_H - 6);
        }
 
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Th.Bg);
 
            // Bottom divider
            using (var p = new Pen(Th.PaneSep))
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);
 
            DrawNavBtn(g, _rBack,    "backward_arrow", _backEnabled,   HitBtn.Back);
            DrawNavBtn(g, _rFwd,     "forward_arrow",  _fwdEnabled,    HitBtn.Fwd);
            DrawDropArrowBtn(g, _rRecLoc, HitBtn.RecLoc);
            DrawNavBtn(g, _rUp,      "up_arrow",       true,           HitBtn.Up);
 
            // Path box border
            int pbX = _pathBox.Left - 3, pbY = BTN_Y;
            int pbW = _pathBox.Width + 6, pbH = BTN_H;
            using (var p = new Pen(Th.PaneSep))
                g.DrawRectangle(p, pbX, pbY, pbW - 1, pbH - 1);
 
            DrawDropArrowBtn(g, _rRecFold, HitBtn.RecFold);
        }
 
        void DrawNavBtn(Graphics g, Rectangle r, string icon, bool enabled, HitBtn btn)
        {
            if (_prs == btn && enabled) Th.DrawPress(g, r);
            else if (_hov == btn && enabled) Th.DrawHover(g, r);
 
            var img  = Icons.Get(icon);
            int ix   = r.X + (r.Width  - ICON) / 2;
            int iy   = r.Y + (r.Height - ICON) / 2;
            if (enabled) g.DrawImage(img, ix, iy, ICON, ICON);
            else
            {
                var attrs = new System.Drawing.Imaging.ImageAttributes();
                var cm    = new System.Drawing.Imaging.ColorMatrix();
                cm.Matrix33 = 0.35f;
                attrs.SetColorMatrix(cm);
                g.DrawImage(img, new Rectangle(ix, iy, ICON, ICON),
                    0, 0, ICON, ICON, GraphicsUnit.Pixel, attrs);
            }
        }
 
        void DrawDropArrowBtn(Graphics g, Rectangle r, HitBtn btn)
        {
            if (_prs == btn) Th.DrawPress(g, r);
            else if (_hov == btn) Th.DrawHover(g, r);
            Th.DrawDropArrow(g, r.X + r.Width / 2, r.Y + r.Height / 2, Th.TxtColor);
        }
 
        HitBtn HitTest(Point pt)
        {
            if (_rBack.Contains(pt))    return HitBtn.Back;
            if (_rFwd.Contains(pt))     return HitBtn.Fwd;
            if (_rRecLoc.Contains(pt))  return HitBtn.RecLoc;
            if (_rUp.Contains(pt))      return HitBtn.Up;
            if (_rRecFold.Contains(pt)) return HitBtn.RecFold;
            return HitBtn.None;
        }
 
        void OnMM(object s, MouseEventArgs e) { var h = HitTest(e.Location); if (h != _hov) { _hov = h; Invalidate(); } }
        void OnMD(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _prs = HitTest(e.Location); Invalidate(); } }
        void OnMU(object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var hit = HitTest(e.Location);
            _prs = HitBtn.None;
            if (hit == HitBtn.Back   && _backEnabled) BackClick?.Invoke(this, EventArgs.Empty);
            else if (hit == HitBtn.Fwd && _fwdEnabled) ForwardClick?.Invoke(this, EventArgs.Empty);
            else if (hit == HitBtn.Up) UpClick?.Invoke(this, EventArgs.Empty);
            Invalidate();
        }
 
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            DoLayout();
        }
    }
 
    // --------------------------------------------------------------------------
    //  COMMAND BAR
    //  Height: 31 px
    //  Layout (L?R): [3] · Organize(91×26) · [2] · NewFolder(88×26) · [flex] ·
    //                ChangeView(27×26) · MoreOptions(19×26) · [10] ·
    //                Preview(28×26) · [8] · Help(28×26) · [9]
    // --------------------------------------------------------------------------
    class CommandBar : Panel
    {
        public const int BAR_H   = 31;
        const int BTN_Y  = 3;    // top of buttons (31 - 26 = 5 ? (5-2)/2 ˜ 2, use 3 for slight padding difference)
        const int BTN_H  = 26;
        const int ICON   = 16;
 
        enum HitBtn
        {
            None,
            Organize, OrganizeDrop,
            NewFolder,
            ChangeView, MoreOptions,
            Preview,
            Help
        }
        HitBtn _hov = HitBtn.None, _prs = HitBtn.None;
 
        Rectangle _rOrg, _rOrgDrop;
        Rectangle _rNF;
        Rectangle _rCV, _rMO;
        Rectangle _rPrev, _rHelp;
 
        public event EventHandler NewFolderClick;
        public event EventHandler PreviewPaneClick;
        public event EventHandler HelpClick;
        public event Action<ViewMode> ViewChanged;
 
        // Organize dropdown
        ContextMenuStrip _organizeMenu;
        // View more-options dropdown
        ContextMenuStrip _viewMenu;
 
        public CommandBar()
        {
            Height = BAR_H;
            Dock   = DockStyle.Top;
            BackColor = Th.Bg;
            DoubleBuffered = true;
 
            BuildOrganizeMenu();
            BuildViewMenu();
 
            MouseMove   += OnMM;
            MouseDown   += OnMD;
            MouseUp     += OnMU;
            MouseLeave  += (s, e) => { _hov = HitBtn.None; Invalidate(); };
            Resize      += (s, e) => DoLayout();
        }
 
        void DoLayout()
        {
            int x = 3;
            // Organize: main part (91 - 16 for drop arrow) + drop arrow (16)
            int orgMain = 75, orgDrop = 16;
            _rOrg     = new Rectangle(x, BTN_Y, orgMain, BTN_H);
            _rOrgDrop = new Rectangle(x + orgMain, BTN_Y, orgDrop, BTN_H);
            x += 91 + 2;
 
            _rNF = new Rectangle(x, BTN_Y, 88, BTN_H); x += 88;
 
            // Right-side buttons: work from right
            int rx = Width - 9;
            rx -= 28; _rHelp = new Rectangle(rx, BTN_Y, 28, BTN_H);
            rx -= 8;
            rx -= 28; _rPrev = new Rectangle(rx, BTN_Y, 28, BTN_H);
            rx -= 10;
            rx -= 19; _rMO   = new Rectangle(rx, BTN_Y, 19, BTN_H);
            rx -= 27; _rCV   = new Rectangle(rx, BTN_Y, 27, BTN_H);
        }
 
        void BuildOrganizeMenu()
        {
            _organizeMenu = new ContextMenuStrip { Renderer = new ExplorerMenuRenderer() };
            _organizeMenu.Items.AddRange(new ToolStripItem[]
            {
                MItem("Cut",    "file"),
                MItem("Copy",   "file"),
                MItem("Paste",  "file"),
                MItem("Undo",   "backward_arrow"),
                MItem("Redo",   "forward_arrow"),
                new ToolStripSeparator(),
                MItem("Select All", null),
                new ToolStripSeparator(),
                MItem("Layout",    null),
                MItem("Options",   null),
                new ToolStripSeparator(),
                MItem("Delete",    null),
                MItem("Rename",    null),
                MItem("Remove Properties", null),
                MItem("Properties", null),
                new ToolStripSeparator(),
                MItem("Close",  null),
            });
        }
 
        void BuildViewMenu()
        {
            _viewMenu = new ContextMenuStrip { Renderer = new ExplorerMenuRenderer() };
            var modes = new[] {
                ("Extra Large Icons", ViewMode.ExtraLargeIcons),
                ("Large Icons",       ViewMode.LargeIcons),
                ("Medium Icons",      ViewMode.MediumIcons),
                ("Small Icons",       ViewMode.SmallIcons),
                ("List",              ViewMode.List),
                ("Details",           ViewMode.Details),
                ("Tiles",             ViewMode.Tiles),
                ("Content",           ViewMode.Content),
            };
            foreach (var (label, vm) in modes)
            {
                var vm2 = vm;
                var item = MItem(label, "change_view");
                item.Click += (s, e) => ViewChanged?.Invoke(vm2);
                _viewMenu.Items.Add(item);
            }
        }
 
        static ToolStripMenuItem MItem(string text, string iconName)
        {
            var item = new ToolStripMenuItem(text)
            {
                Font = Th.UiFont,
            };
            if (iconName != null)
            {
                var img16 = Icons.Get(iconName);
                var bmp   = new Bitmap(16, 16);
                using (var g = Graphics.FromImage(bmp))
                { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(img16, 0, 0, 16, 16); }
                item.Image = bmp;
            }
            return item;
        }
 
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Th.Bg);
 
            // Bottom divider
            using (var p = new Pen(Th.PaneSep))
                g.DrawLine(p, 0, Height - 1, Width, Height - 1);
 
            // Organize button (split)
            DrawSplitBtn(g, _rOrg, _rOrgDrop, "organize", "Organize",
                showText: true, hovMain: _hov == HitBtn.Organize, hovDrop: _hov == HitBtn.OrganizeDrop,
                prsMain: _prs == HitBtn.Organize, prsDrop: _prs == HitBtn.OrganizeDrop);
 
            // New Folder button
            DrawCmdBtn(g, _rNF, "new_folder", "New folder", _hov == HitBtn.NewFolder, _prs == HitBtn.NewFolder);
 
            // Change View button
            DrawCmdBtn(g, _rCV, "change_view", null, _hov == HitBtn.ChangeView, _prs == HitBtn.ChangeView);
 
            // More Options (drop arrow only, immediately adjacent)
            DrawDropOnlyBtn(g, _rMO, _hov == HitBtn.MoreOptions, _prs == HitBtn.MoreOptions);
 
            // Thin vertical separator between ChangeView and MoreOptions
            using (var sp = new Pen(Th.SepColor))
                g.DrawLine(sp, _rMO.Left, _rMO.Top + 2, _rMO.Left, _rMO.Bottom - 2);
 
            // Preview pane
            DrawCmdBtn(g, _rPrev, "preview_pane", null, _hov == HitBtn.Preview, _prs == HitBtn.Preview);
 
            // Help
            DrawCmdBtn(g, _rHelp, "help", null, _hov == HitBtn.Help, _prs == HitBtn.Help);
        }
 
        void DrawCmdBtn(Graphics g, Rectangle r, string icon, string text,
                        bool hover, bool press)
        {
            if (press) Th.DrawPress(g, r);
            else if (hover) Th.DrawHover(g, r);
 
            int ix, iy;
            if (text != null)
            {
                ix = r.X + 4; iy = r.Y + (r.Height - ICON) / 2;
                g.DrawImage(Icons.Get(icon), ix, iy, ICON, ICON);
                var tf = new RectangleF(ix + ICON + 3, r.Y, r.Width - ICON - 10, r.Height);
                using (var fmt = new StringFormat { LineAlignment = StringAlignment.Center })
                    g.DrawString(text, Th.UiFont, Brushes.Black, tf, fmt);
            }
            else
            {
                ix = r.X + (r.Width  - ICON) / 2;
                iy = r.Y + (r.Height - ICON) / 2;
                g.DrawImage(Icons.Get(icon), ix, iy, ICON, ICON);
            }
        }
 
        void DrawSplitBtn(Graphics g,
                          Rectangle rMain, Rectangle rDrop,
                          string icon, string text,
                          bool showText,
                          bool hovMain, bool hovDrop,
                          bool prsMain, bool prsDrop)
        {
            if (prsMain) Th.DrawPress(g, rMain);
            else if (hovMain) Th.DrawHover(g, rMain);
 
            if (prsDrop) Th.DrawPress(g, rDrop);
            else if (hovDrop) Th.DrawHover(g, rDrop);
 
            // icon + text in main part
            int ix = rMain.X + 4, iy = rMain.Y + (rMain.Height - ICON) / 2;
            g.DrawImage(Icons.Get(icon), ix, iy, ICON, ICON);
            if (showText)
            {
                var tf = new RectangleF(ix + ICON + 3, rMain.Y, rMain.Width - ICON - 7, rMain.Height);
                using (var fmt = new StringFormat { LineAlignment = StringAlignment.Center })
                    g.DrawString(text, Th.UiFont, Brushes.Black, tf, fmt);
            }
 
            // separator line between main and drop
            using (var sp = new Pen(Th.SepColor))
                g.DrawLine(sp, rDrop.Left, rDrop.Top + 2, rDrop.Left, rDrop.Bottom - 2);
 
            // drop arrow
            Th.DrawDropArrow(g, rDrop.X + rDrop.Width / 2, rDrop.Y + rDrop.Height / 2, Th.TxtColor);
        }
 
        void DrawDropOnlyBtn(Graphics g, Rectangle r, bool hover, bool press)
        {
            if (press) Th.DrawPress(g, r);
            else if (hover) Th.DrawHover(g, r);
            Th.DrawDropArrow(g, r.X + r.Width / 2, r.Y + r.Height / 2 + 1, Th.TxtColor);
        }
 
        HitBtn HitTest(Point pt)
        {
            if (_rOrg.Contains(pt))     return HitBtn.Organize;
            if (_rOrgDrop.Contains(pt)) return HitBtn.OrganizeDrop;
            if (_rNF.Contains(pt))      return HitBtn.NewFolder;
            if (_rCV.Contains(pt))      return HitBtn.ChangeView;
            if (_rMO.Contains(pt))      return HitBtn.MoreOptions;
            if (_rPrev.Contains(pt))    return HitBtn.Preview;
            if (_rHelp.Contains(pt))    return HitBtn.Help;
            return HitBtn.None;
        }
 
        void OnMM(object s, MouseEventArgs e) { var h = HitTest(e.Location); if (h != _hov) { _hov = h; Invalidate(); } }
        void OnMD(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) { _prs = HitTest(e.Location); Invalidate(); } }
        void OnMU(object s, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            var hit = HitTest(e.Location);
            _prs = HitBtn.None;
            Invalidate();
 
            switch (hit)
            {
                case HitBtn.OrganizeDrop:
                    _organizeMenu.Show(this, new Point(_rOrg.Left, BAR_H));
                    break;
                case HitBtn.Organize:
                    _organizeMenu.Show(this, new Point(_rOrg.Left, BAR_H));
                    break;
                case HitBtn.NewFolder:
                    NewFolderClick?.Invoke(this, EventArgs.Empty);
                    break;
                case HitBtn.ChangeView:
                    _viewMenu.Show(this, new Point(_rCV.Left, BAR_H));
                    break;
                case HitBtn.MoreOptions:
                    _viewMenu.Show(this, new Point(_rCV.Left, BAR_H));
                    break;
                case HitBtn.Preview:
                    PreviewPaneClick?.Invoke(this, EventArgs.Empty);
                    break;
                case HitBtn.Help:
                    HelpClick?.Invoke(this, EventArgs.Empty);
                    break;
            }
        }
 
        protected override void OnCreateControl()
        {
            base.OnCreateControl();
            DoLayout();
        }
    }
 
    // --------------------------------------------------------------------------
    //  TREE PANE  (left panel – Quick Access / This PC / Network)
    //  Vertical scrollbar only; 2 px right margin
    //  Root indent: 24px; child indent: +8px per level
    //  Row height: 20 px
    // --------------------------------------------------------------------------
    class TreePane : Panel
    {
        const int ROW_H      = 20;
        const int ROOT_INDENT= 8;   // left margin before level-0 items
        const int LVL_INDENT = 16;  // pixels per level
        const int ICON_SZ    = 16;
        const int ARROW_W    = 12;
 
        List<TreeNode2> _flat   = new List<TreeNode2>();
        TreeNode2       _root;    // invisible root
        TreeNode2       _selected;
        int             _scrollY = 0;
        int             _totalH  = 0;
 
        VScrollBar _vsb;
 
        public event Action<TreeNode2> NodeSelected;
 
        public TreePane()
        {
            BackColor = Th.TreeBg;
            DoubleBuffered = true;
 
            _vsb = new VScrollBar
            {
                Dock    = DockStyle.Right,
                Minimum = 0, Maximum = 0,
                SmallChange = ROW_H, LargeChange = 100,
                Visible = false,
            };
            _vsb.ValueChanged += (s, e) => { _scrollY = _vsb.Value; Invalidate(); };
            Controls.Add(_vsb);
 
            MouseDown  += OnMD;
            MouseWheel += OnMW;
            Resize     += (s, e) => { Rebuild(); Invalidate(); };
 
            BuildTree();
        }
 
        void BuildTree()
        {
            _root = new TreeNode2 { Label = "__root__", IsVirtual = true, Expanded = true };
 
            // -- Quick Access -------------------------------------------
            var qa = AddChild(_root, "Quick access", null, "quick_access", isVirtual: true, isRoot: true);
            qa.Expanded = true;
            AddChild(qa, "Desktop",   SpecialDir(Environment.SpecialFolder.DesktopDirectory), "desktop");
            AddChild(qa, "Downloads", GetDownloads(), "downloads");
            AddChild(qa, "Documents", SpecialDir(Environment.SpecialFolder.MyDocuments),  "documents");
            AddChild(qa, "Pictures",  SpecialDir(Environment.SpecialFolder.MyPictures),   "pictures");
 
            // -- This PC ------------------------------------------------
            var tpc = AddChild(_root, "This PC", null, "this_pc", isVirtual: true, isRoot: true);
            AddChild(tpc, "3D Objects", null,                                    "3dobjects");
            AddChild(tpc, "Desktop",    SpecialDir(Environment.SpecialFolder.DesktopDirectory), "desktop");
            AddChild(tpc, "Documents",  SpecialDir(Environment.SpecialFolder.MyDocuments),  "documents");
            AddChild(tpc, "Downloads",  GetDownloads(),  "downloads");
            AddChild(tpc, "Music",      SpecialDir(Environment.SpecialFolder.MyMusic),       "music");
            AddChild(tpc, "Pictures",   SpecialDir(Environment.SpecialFolder.MyPictures),    "pictures");
            AddChild(tpc, "Videos",     SpecialDir(Environment.SpecialFolder.MyVideos),      "videos");
 
            // Drives
            foreach (var drive in DriveInfo.GetDrives())
            {
                try
                {
                    string label = drive.IsReady && !string.IsNullOrEmpty(drive.VolumeLabel)
                        ? $"{drive.VolumeLabel} ({drive.Name.TrimEnd('\\')})"
                        : $"Local Disk ({drive.Name.TrimEnd('\\')})";
                    var d = AddChild(tpc, label, drive.RootDirectory.FullName, "drives");
                    d.HasChildren = true;
                }
                catch { }
            }
 
            // -- Network -----------------------------------------------
            AddChild(_root, "Network", null, "network", isVirtual: true, isRoot: true);
 
            Rebuild();
        }
 
        static string SpecialDir(Environment.SpecialFolder sf) =>
            Environment.GetFolderPath(sf);
 
        static string GetDownloads() =>
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
 
        TreeNode2 AddChild(TreeNode2 parent, string label, string path, string icon,
                           bool isVirtual = false, bool isRoot = false)
        {
            var n = new TreeNode2
            {
                Label = label, Path = path, IconName = icon,
                IsVirtual = isVirtual, IsRoot = isRoot,
                Parent = parent,
                Level  = parent == _root ? 0 : parent.Level + 1,
                HasChildren = !isVirtual && path != null,
            };
            parent.Children.Add(n);
            return n;
        }
 
        void Rebuild()
        {
            _flat.Clear();
            Flatten(_root);
            _totalH = _flat.Count * ROW_H;
            UpdateScroll();
        }
 
        void Flatten(TreeNode2 n)
        {
            foreach (var c in n.Children)
            {
                _flat.Add(c);
                if (c.Expanded) Flatten(c);
            }
        }
 
        void UpdateScroll()
        {
            int visH = ClientSize.Height;
            if (_totalH > visH)
            {
                _vsb.Visible  = true;
                _vsb.Maximum  = Math.Max(0, _totalH - visH + _vsb.LargeChange);
                _scrollY      = Math.Min(_scrollY, Math.Max(0, _totalH - visH));
                _vsb.Value    = _scrollY;
            }
            else
            {
                _vsb.Visible = false;
                _scrollY     = 0;
            }
        }
 
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Th.TreeBg);
 
            int listW = Width - (_vsb.Visible ? _vsb.Width : 0) - 2; // 2px right padding
            int y0    = -_scrollY;
 
            for (int i = 0; i < _flat.Count; i++)
            {
                var n = _flat[i];
                int y = y0 + i * ROW_H;
                if (y + ROW_H < 0)  continue;
                if (y > Height)     break;
 
                n.Bounds = new Rectangle(0, y, listW, ROW_H);
 
                bool sel = n == _selected;
                if (sel) Th.DrawSel(g, new Rectangle(0, y, listW, ROW_H - 1));
 
                int indent = ROOT_INDENT + n.Level * LVL_INDENT;
 
                // Expand arrow
                bool hasKids = n.Children.Count > 0 || n.HasChildren;
                if (hasKids)
                {
                    int ax = indent - 2, ay = y + ROW_H / 2;
                    DrawExpandArrow(g, ax, ay, n.Expanded);
                }
 
                // Icon
                int iconX = indent + ARROW_W;
                g.DrawImage(Icons.Get(n.IconName ?? "folder"), iconX, y + (ROW_H - ICON_SZ) / 2, ICON_SZ, ICON_SZ);
 
                // Label
                int labelX  = iconX + ICON_SZ + 3;
                int labelW  = Math.Max(1, listW - labelX - 2);
                var labelR  = new RectangleF(labelX, y + 1, labelW, ROW_H - 2);
                var fmt     = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
                var font    = n.IsRoot ? Th.UiBold : Th.UiFont;
                g.DrawString(n.Label, font, Brushes.Black, labelR, fmt);
 
                // Root category: thin line above (except first)
                if (n.IsRoot && i > 0)
                    using (var sp = new Pen(Th.SepColor))
                        g.DrawLine(sp, 0, y, listW, y);
            }
 
            // Right-edge 2px border
            using (var rp = new Pen(Th.PaneSep))
                g.DrawLine(rp, listW, 0, listW, Height);
        }
 
        static void DrawExpandArrow(Graphics g, int cx, int cy, bool expanded)
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            Point[] pts;
            if (expanded)
                pts = new[] { new Point(cx - 4, cy - 2), new Point(cx + 4, cy - 2), new Point(cx, cy + 3) };
            else
                pts = new[] { new Point(cx - 2, cy - 4), new Point(cx - 2, cy + 4), new Point(cx + 3, cy) };
            using (var b = new SolidBrush(Color.FromArgb(100, 100, 100)))
                g.FillPolygon(b, pts);
            g.SmoothingMode = SmoothingMode.Default;
        }
 
        void OnMD(object s, MouseEventArgs e)
        {
            int idx = (_scrollY + e.Y) / ROW_H;
            if (idx < 0 || idx >= _flat.Count) return;
            var n = _flat[idx];
 
            // Click on expand arrow?
            int indent = ROOT_INDENT + n.Level * LVL_INDENT;
            bool hasKids = n.Children.Count > 0 || n.HasChildren;
            if (hasKids && e.X >= indent - 8 && e.X <= indent + ARROW_W)
            {
                ToggleExpand(n);
                return;
            }
 
            _selected = n;
            Invalidate();
 
            if (e.Button == MouseButtons.Left)
                NodeSelected?.Invoke(n);
        }
 
        void ToggleExpand(TreeNode2 n)
        {
            if (!n.Expanded)
            {
                // Load real filesystem children if needed
                if (n.Children.Count == 0 && n.Path != null && !n.IsVirtual)
                    LoadFsChildren(n);
                n.Expanded = true;
            }
            else n.Expanded = false;
            Rebuild();
            Invalidate();
        }
 
        void LoadFsChildren(TreeNode2 n)
        {
            try
            {
                var dirs = Directory.GetDirectories(n.Path);
                foreach (var d in dirs)
                {
                    var child = new TreeNode2
                    {
                        Label  = Path.GetFileName(d),
                        Path   = d,
                        IconName = "folder",
                        Parent = n,
                        Level  = n.Level + 1,
                        HasChildren = true,
                    };
                    n.Children.Add(child);
                }
                n.HasChildren = false; // already loaded
            }
            catch { }
        }
 
        void OnMW(object s, MouseEventArgs e)
        {
            _scrollY = Math.Max(0, Math.Min(_scrollY - e.Delta / 3,
                Math.Max(0, _totalH - ClientSize.Height)));
            if (_vsb.Visible) _vsb.Value = _scrollY;
            Invalidate();
        }
 
        public void SelectPath(string path)
        {
            foreach (var n in _flat)
                if (n.Path != null && n.Path.Equals(path, StringComparison.OrdinalIgnoreCase))
                { _selected = n; Invalidate(); return; }
        }
    }
 
    // --------------------------------------------------------------------------
    //  CONTENT PANE  (right panel)
    //  Column headers: Name | Date modified | Type | Size
    //  Default view: Details
    //  Multi-select: #CCE8FF fill, #99D1FF border
    // --------------------------------------------------------------------------
    class ContentPane : Panel
    {
        const int HDR_H  = 22;
        const int ROW_H  = 20;
        const int ICON_SZ= 16;
 
        // Column widths
        int _wName  = 300, _wDate = 160, _wType = 100, _wSize = 80;
 
        List<ContentItem> _items    = new List<ContentItem>();
        HashSet<int>      _selSet   = new HashSet<int>();
        int               _lastSel  = -1;
 
        SortCol _sortCol = SortCol.Name;
        SortDir _sortDir = SortDir.Asc;
        int     _scrollY = 0;
 
        // Column header resize drag
        enum ColDrag { None, Name, Date, Type }
        ColDrag _drag = ColDrag.None;
        int     _dragStartX, _dragStartW;
 
        // Marquee selection
        bool    _marquee;
        Point   _marqStart, _marqCur;
 
        // Hover
        int     _hovRow = -1;
 
        VScrollBar _vsb;
 
        public string CurrentPath { get; private set; } = "";
        public event Action<ContentItem> ItemActivated;
        public event Action<ContentItem> ShowContextMenu;
 
        // Context menus
        ContextMenuStrip _bgMenu, _folderMenu, _fileMenu;
 
        public ContentPane()
        {
            BackColor = Th.ContentBg;
            DoubleBuffered = true;
 
            _vsb = new VScrollBar { Dock = DockStyle.Right, Minimum = 0, Visible = false };
            _vsb.ValueChanged += (s, e) => { _scrollY = _vsb.Value; Invalidate(); };
            Controls.Add(_vsb);
 
            BuildContextMenus();
 
            MouseDown   += OnMD;
            MouseMove   += OnMM;
            MouseUp     += OnMU;
            MouseWheel  += OnMW;
            MouseLeave  += (s, e) => { _hovRow = -1; Invalidate(); };
            DoubleClick += OnDblClick;
            Resize      += (s, e) => UpdateScroll();
        }
 
        // -- Load content from path ------------------------------------------
        public void LoadPath(string path)
        {
            CurrentPath = path;
            _items.Clear();
            _selSet.Clear();
            _lastSel = -1;
            _scrollY = 0;
 
            if (string.IsNullOrEmpty(path) || !Directory.Exists(path))
            {
                Invalidate();
                return;
            }
 
            try
            {
                foreach (var d in Directory.GetDirectories(path))
                {
                    try
                    {
                        var di = new DirectoryInfo(d);
                        _items.Add(new ContentItem
                        {
                            Name         = di.Name,
                            FullPath     = di.FullName,
                            DateModified = di.LastWriteTime,
                            ItemType     = "File folder",
                            Size         = 0,
                            IsDirectory  = true,
                        });
                    }
                    catch { }
                }
                foreach (var f in Directory.GetFiles(path))
                {
                    try
                    {
                        var fi = new FileInfo(f);
                        _items.Add(new ContentItem
                        {
                            Name         = fi.Name,
                            FullPath     = fi.FullName,
                            DateModified = fi.LastWriteTime,
                            ItemType     = GetTypeString(fi.Extension),
                            Size         = fi.Length,
                            IsDirectory  = false,
                        });
                    }
                    catch { }
                }
            }
            catch { }
 
            SortItems();
            UpdateScroll();
            Invalidate();
        }
 
        static string GetTypeString(string ext)
        {
            if (string.IsNullOrEmpty(ext)) return "File";
            switch (ext.ToLower())
            {
                case ".txt":  return "Text Document";
                case ".png":
                case ".jpg":
                case ".jpeg":
                case ".bmp":  return "Image";
                case ".cs":   return "C# Source File";
                case ".exe":  return "Application";
                case ".dll":  return "Application Extension";
                case ".zip":  return "Compressed (zipped) Folder";
                case ".pdf":  return "PDF Document";
                case ".mp3":  return "MP3 File";
                case ".mp4":  return "MP4 Video";
                default:      return ext.TrimStart('.').ToUpper() + " File";
            }
        }
 
        void SortItems()
        {
            IEnumerable<ContentItem> sorted;
            switch (_sortCol)
            {
                case SortCol.Name: sorted = _items.OrderBy(i => !i.IsDirectory).ThenBy(i => i.Name, StringComparer.OrdinalIgnoreCase); break;
                case SortCol.Date: sorted = _items.OrderBy(i => i.DateModified); break;
                case SortCol.Type: sorted = _items.OrderBy(i => i.ItemType, StringComparer.OrdinalIgnoreCase); break;
                case SortCol.Size: sorted = _items.OrderBy(i => i.Size); break;
                default:           sorted = _items; break;
            }
            if (_sortDir == SortDir.Desc) sorted = sorted.Reverse();
            _items = sorted.ToList();
        }
 
        void UpdateScroll()
        {
            int total = _items.Count * ROW_H;
            int vis   = ClientSize.Height - HDR_H;
            if (total > vis)
            {
                _vsb.Visible     = true;
                _vsb.Maximum     = Math.Max(0, total - vis + _vsb.LargeChange);
                _vsb.SmallChange = ROW_H;
                _vsb.LargeChange = Math.Max(1, vis);
            }
            else { _vsb.Visible = false; _scrollY = 0; }
        }
 
        // -- Paint ----------------------------------------------------------
        protected override void OnPaint(PaintEventArgs e)
        {
            var g = e.Graphics;
            g.TextRenderingHint = TextRenderingHint.ClearTypeGridFit;
            g.Clear(Th.ContentBg);
 
            DrawHeader(g);
            DrawItems(g);
            if (_marquee) DrawMarquee(g);
        }
 
        void DrawHeader(Graphics g)
        {
            int listW = ListWidth();
            using (var hb = new SolidBrush(Th.HdrBg)) g.FillRectangle(hb, 0, 0, listW, HDR_H);
            using (var hbord = new Pen(Th.HdrBorder)) g.DrawLine(hbord, 0, HDR_H - 1, listW, HDR_H - 1);
 
            // Column positions
            (int x, string label, SortCol col, int w)[] cols = ColDefs();
 
            var fmt = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
 
            foreach (var (x, label, col, w) in cols)
            {
                // Hover background
                // Column header text
                var r = new RectangleF(x + 4, 0, w - 8, HDR_H);
                g.DrawString(label, Th.UiFont, Brushes.Black, r, fmt);
 
                // Sort indicator
                if (col == _sortCol)
                {
                    int ax = x + w - 12, ay = HDR_H / 2;
                    if (_sortDir == SortDir.Asc)
                    { Point[] p = { new Point(ax - 3, ay + 2), new Point(ax + 3, ay + 2), new Point(ax, ay - 2) }; g.FillPolygon(Brushes.Gray, p); }
                    else
                    { Point[] p = { new Point(ax - 3, ay - 2), new Point(ax + 3, ay - 2), new Point(ax, ay + 2) }; g.FillPolygon(Brushes.Gray, p); }
                }
 
                // Column divider
                if (x + w < listW)
                    using (var sp = new Pen(Th.HdrBorder))
                        g.DrawLine(sp, x + w - 1, 2, x + w - 1, HDR_H - 2);
            }
        }
 
        (int x, string label, SortCol col, int w)[] ColDefs()
        {
            int listW = ListWidth();
            int nameW = listW - _wDate - _wType - _wSize;
            return new[]
            {
                (0,                     "Name",          SortCol.Name, nameW),
                (nameW,                 "Date modified", SortCol.Date, _wDate),
                (nameW + _wDate,        "Type",          SortCol.Type, _wType),
                (nameW + _wDate + _wType,"Size",         SortCol.Size, _wSize),
            };
        }
 
        void DrawItems(Graphics g)
        {
            int listW  = ListWidth();
            var cols   = ColDefs();
            var fmt    = new StringFormat { LineAlignment = StringAlignment.Center, Trimming = StringTrimming.EllipsisCharacter };
 
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                int y    = HDR_H + i * ROW_H - _scrollY;
                if (y + ROW_H < HDR_H) continue;
                if (y > Height)        break;
 
                bool sel  = _selSet.Contains(i);
                bool hov  = i == _hovRow;
 
                if (sel)  Th.DrawSel(g, new Rectangle(0, y, listW, ROW_H - 1));
                else if (hov) Th.DrawHover(g, new Rectangle(0, y, listW, ROW_H - 1));
 
                // Icon + Name
                string iconName = item.IsDirectory ? "folder" : "file";
                g.DrawImage(Icons.Get(iconName), cols[0].x + 2, y + (ROW_H - ICON_SZ) / 2, ICON_SZ, ICON_SZ);
                var nameR = new RectangleF(cols[0].x + ICON_SZ + 6, y, cols[0].w - ICON_SZ - 8, ROW_H);
                g.DrawString(item.Name, Th.UiFont, Brushes.Black, nameR, fmt);
 
                // Date
                var dateR = new RectangleF(cols[1].x + 4, y, cols[1].w - 8, ROW_H);
                g.DrawString(item.DateStr, Th.UiFont, Brushes.Black, dateR, fmt);
 
                // Type
                var typeR = new RectangleF(cols[2].x + 4, y, cols[2].w - 8, ROW_H);
                g.DrawString(item.ItemType, Th.UiFont, Brushes.Black, typeR, fmt);
 
                // Size
                var sizeR = new RectangleF(cols[3].x + 4, y, cols[3].w - 8, ROW_H);
                var sfmt  = new StringFormat(fmt) { Alignment = StringAlignment.Far };
                g.DrawString(item.SizeStr, Th.UiFont, Brushes.Black, sizeR, sfmt);
 
                // Row separator
                using (var rp = new Pen(Color.FromArgb(240, 240, 240)))
                    g.DrawLine(rp, 0, y + ROW_H - 1, listW, y + ROW_H - 1);
            }
        }
 
        void DrawMarquee(Graphics g)
        {
            int x = Math.Min(_marqStart.X, _marqCur.X);
            int y = Math.Min(_marqStart.Y, _marqCur.Y);
            int w = Math.Abs(_marqCur.X - _marqStart.X);
            int h = Math.Abs(_marqCur.Y - _marqStart.Y);
            var r = new Rectangle(x, y, w, h);
            using (var b = new SolidBrush(Color.FromArgb(80, Th.SelFill))) g.FillRectangle(b, r);
            using (var p = new Pen(Th.SelBorder)) g.DrawRectangle(p, r.X, r.Y, r.Width - 1, r.Height - 1);
        }
 
        // -- Mouse handling -------------------------------------------------
        void OnMD(object s, MouseEventArgs e)
        {
            Focus();
 
            // Column header click?
            if (e.Y < HDR_H)
            {
                HandleHeaderClick(e);
                return;
            }
 
            int idx = RowAt(e.Y);
 
            if (e.Button == MouseButtons.Left)
            {
                bool ctrl  = (Control.ModifierKeys & Keys.Control) != 0;
                bool shift = (Control.ModifierKeys & Keys.Shift)   != 0;
 
                if (idx >= 0 && idx < _items.Count)
                {
                    if (ctrl)
                    {
                        if (_selSet.Contains(idx)) _selSet.Remove(idx);
                        else _selSet.Add(idx);
                        _lastSel = idx;
                    }
                    else if (shift && _lastSel >= 0)
                    {
                        _selSet.Clear();
                        int lo = Math.Min(_lastSel, idx), hi = Math.Max(_lastSel, idx);
                        for (int i = lo; i <= hi; i++) _selSet.Add(i);
                    }
                    else
                    {
                        _selSet.Clear();
                        _selSet.Add(idx);
                        _lastSel = idx;
                    }
                }
                else
                {
                    // Clicked empty area – start marquee
                    if (!ctrl && !shift) _selSet.Clear();
                    _lastSel  = -1;
                    _marquee  = true;
                    _marqStart = e.Location;
                    _marqCur   = e.Location;
                    Capture    = true;
                }
            }
            else if (e.Button == MouseButtons.Right)
            {
                if (idx >= 0 && idx < _items.Count)
                {
                    if (!_selSet.Contains(idx)) { _selSet.Clear(); _selSet.Add(idx); _lastSel = idx; }
                    var item = _items[idx];
                    var menu = item.IsDirectory ? _folderMenu : _fileMenu;
                    Invalidate();
                    menu.Show(this, e.Location);
                }
                else
                {
                    Invalidate();
                    _bgMenu.Show(this, e.Location);
                }
            }
 
            Invalidate();
        }
 
        void OnMM(object s, MouseEventArgs e)
        {
            if (_marquee)
            {
                _marqCur = e.Location;
                // Update selection based on marquee
                UpdateMarqueeSelection();
                Invalidate();
                return;
            }
 
            int idx = e.Y < HDR_H ? -1 : RowAt(e.Y);
            if (idx != _hovRow) { _hovRow = idx; Invalidate(); }
        }
 
        void OnMU(object s, MouseEventArgs e)
        {
            if (_marquee) { _marquee = false; Capture = false; Invalidate(); }
        }
 
        void UpdateMarqueeSelection()
        {
            int listW = ListWidth();
            int x1 = Math.Min(_marqStart.X, _marqCur.X);
            int y1 = Math.Min(_marqStart.Y, _marqCur.Y);
            int x2 = Math.Max(_marqStart.X, _marqCur.X);
            int y2 = Math.Max(_marqStart.Y, _marqCur.Y);
 
            _selSet.Clear();
            for (int i = 0; i < _items.Count; i++)
            {
                int ry1 = HDR_H + i * ROW_H - _scrollY;
                int ry2 = ry1 + ROW_H;
                if (ry2 > y1 && ry1 < y2) _selSet.Add(i);
            }
        }
 
        void OnMW(object s, MouseEventArgs e)
        {
            _scrollY = Math.Max(0, _scrollY - e.Delta / 3);
            if (_vsb.Visible)
            {
                _scrollY = Math.Min(_scrollY, _vsb.Maximum - _vsb.LargeChange + 1);
                _vsb.Value = _scrollY;
            }
            Invalidate();
        }
 
        void OnDblClick(object s, EventArgs e)
        {
            var mp = PointToClient(Cursor.Position);
            int idx = RowAt(mp.Y);
            if (idx >= 0 && idx < _items.Count)
                ItemActivated?.Invoke(_items[idx]);
        }
 
        void HandleHeaderClick(MouseEventArgs e)
        {
            var cols = ColDefs();
            foreach (var (x, _, col, w) in cols)
            {
                if (e.X >= x && e.X < x + w)
                {
                    if (_sortCol == col) _sortDir = _sortDir == SortDir.Asc ? SortDir.Desc : SortDir.Asc;
                    else { _sortCol = col; _sortDir = SortDir.Asc; }
                    SortItems();
                    _selSet.Clear();
                    Invalidate();
                    return;
                }
            }
        }
 
        int RowAt(int y)
        {
            if (y < HDR_H) return -1;
            return (_scrollY + y - HDR_H) / ROW_H;
        }
 
        int ListWidth() => Width - (_vsb.Visible ? _vsb.Width : 0);
 
        // -- Context Menus -------------------------------------------------
        void BuildContextMenus()
        {
            var rend = new ExplorerMenuRenderer();
 
            // Background (empty area) context menu
            _bgMenu = new ContextMenuStrip { Renderer = rend };
            var viewSub  = Sub("View");
            AddViewItems(viewSub);
            var sortSub  = Sub("Sort by");
            AddSortItems(sortSub);
            var groupSub = Sub("Group by");
            AddGroupItems(groupSub);
            var giveSub  = Sub("Give access to");
            AddGiveAccessItems(giveSub);
            var newSub   = Sub("New");
            AddNewItems(newSub);
 
            _bgMenu.Items.AddRange(new ToolStripItem[]
            {
                viewSub,
                sortSub,
                groupSub,
                MItem("Refresh",           "reload"),
                new ToolStripSeparator(),
                MItem("Paste",             "file"),
                MItem("Paste shortcut",    "file"),
                MItem("Undo Delete",       "backward_arrow"),
                new ToolStripSeparator(),
                giveSub,
                new ToolStripSeparator(),
                newSub,
                new ToolStripSeparator(),
                MItem("Properties",        "organize"),
            });
 
            // Folder context menu
            _folderMenu = new ContextMenuStrip { Renderer = rend };
            var folderGiveSub = Sub("Give access to");
            AddGiveAccessItems(folderGiveSub);
            var folderSendSub = Sub("Send to");
            _folderMenu.Items.AddRange(new ToolStripItem[]
            {
                MItem("Open",              "folder"),
                MItem("Open in new window","folder"),
                MItem("Pin to Quick access", "quick_access"),
                MItem("Take Ownership",    "organize"),
                new ToolStripSeparator(),
                folderGiveSub,
                MItem("Restore",           "backward_arrow"),
                new ToolStripSeparator(),
                folderSendSub,
                new ToolStripSeparator(),
                MItem("Cut",               "file"),
                MItem("Copy",              "file"),
                new ToolStripSeparator(),
                MItem("Create shortcut",   "file"),
                MItem("Delete",            "file"),
                MItem("Rename",            "file"),
                new ToolStripSeparator(),
                MItem("Properties",        "organize"),
            });
 
            // File context menu
            _fileMenu = new ContextMenuStrip { Renderer = rend };
            var fileGiveSub = Sub("Give access to");
            AddGiveAccessItems(fileGiveSub);
            var fileOpenWith = Sub("Open with");
            var fileSendTo  = Sub("Send to");
            _fileMenu.Items.AddRange(new ToolStripItem[]
            {
                MItem("Open",                   "file"),
                MItem("Pin",                    "quick_access"),
                MItem("Edit",                   "file"),
                MItem("Take Ownership",         "organize"),
                fileOpenWith,
                new ToolStripSeparator(),
                fileGiveSub,
                MItem("Restore previous version","backward_arrow"),
                new ToolStripSeparator(),
                fileSendTo,
                MItem("Cut",                    "file"),
                MItem("Copy",                   "file"),
                new ToolStripSeparator(),
                MItem("Create shortcut",        "file"),
                MItem("Delete",                 "file"),
                MItem("Rename",                 "file"),
                new ToolStripSeparator(),
                MItem("Properties",             "organize"),
            });
        }
 
        static ToolStripMenuItem Sub(string text)
        {
            var m = new ToolStripMenuItem(text) { Font = Th.UiFont };
            return m;
        }
 
        static ToolStripMenuItem MItem(string text, string icon)
        {
            var m = new ToolStripMenuItem(text) { Font = Th.UiFont };
            if (icon != null)
            {
                var src = Icons.Get(icon);
                var bmp = new Bitmap(16, 16);
                using (var g = Graphics.FromImage(bmp))
                { g.InterpolationMode = InterpolationMode.HighQualityBicubic; g.DrawImage(src, 0, 0, 16, 16); }
                m.Image = bmp;
            }
            return m;
        }
 
        static void AddViewItems(ToolStripMenuItem sub)
        {
            string[] labels = { "Extra Large Icons", "Large Icons", "Medium Icons", "Small Icons",
                                 "List", "Details", "Tiles", "Content" };
            foreach (var l in labels) sub.DropDownItems.Add(MItem(l, "change_view"));
        }
 
        static void AddSortItems(ToolStripMenuItem sub)
        {
            sub.DropDownItems.AddRange(new ToolStripItem[]
            {
                MItem("Name",       null), MItem("Date modified", null),
                MItem("Type",       null), MItem("Size",          null),
                new ToolStripSeparator(),
                MItem("Ascending",  null), MItem("Descending",    null),
                new ToolStripSeparator(),
                MItem("More...",    null),
            });
        }
 
        static void AddGroupItems(ToolStripMenuItem sub)
        {
            sub.DropDownItems.AddRange(new ToolStripItem[]
            {
                MItem("Name",       null), MItem("Date modified", null),
                MItem("Type",       null), MItem("Size",          null),
                new ToolStripSeparator(),
                MItem("Ascending",  null), MItem("Descending",    null),
                new ToolStripSeparator(),
                MItem("More...",    null),
            });
        }
 
        static void AddGiveAccessItems(ToolStripMenuItem sub)
        {
            sub.DropDownItems.AddRange(new ToolStripItem[]
            {
                MItem("Remove access",               null),
                MItem("Homegroup (view)",             "network"),
                MItem("Homegroup (view and edit)",    "network"),
                new ToolStripSeparator(),
                MItem("Specific people...",           null),
            });
        }
 
        static void AddNewItems(ToolStripMenuItem sub)
        {
            sub.DropDownItems.AddRange(new ToolStripItem[]
            {
                MItem("Folder",             "folder"),
                MItem("Shortcut",           "file"),
                new ToolStripSeparator(),
                MItem("Bitmap image",       "file"),
                MItem("Contact",            "file"),
                MItem("Rich Text Format",   "file"),
                MItem("Text Document",      "file"),
            });
        }
    }
 
    // --------------------------------------------------------------------------
    //  CUSTOM SPLITTER
    //  Thin 4px vertical divider between tree and content; draggable
    // --------------------------------------------------------------------------
    class SplitterBar : Control
    {
        bool _drag;
        int  _startX, _startW;
        Control _leftCtrl;
 
        public SplitterBar(Control leftPanel)
        {
            _leftCtrl = leftPanel;
            Width     = 4;
            Cursor    = Cursors.VSplit;
            BackColor = Th.PaneSep;
 
            MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                { _drag = true; _startX = Cursor.Position.X; _startW = _leftCtrl.Width; Capture = true; }
            };
            MouseMove += (s, e) =>
            {
                if (!_drag) return;
                int newW = Math.Max(100, _startW + Cursor.Position.X - _startX);
                _leftCtrl.Width = newW;
                if (Parent != null) Parent.PerformLayout();
            };
            MouseUp += (s, e) => { _drag = false; Capture = false; };
        }
 
        protected override void OnPaint(PaintEventArgs e)
        {
            e.Graphics.Clear(Th.PaneSep);
        }
    }
 
    // --------------------------------------------------------------------------
    //  STATUS BAR
    // --------------------------------------------------------------------------
    class ExplorerStatusBar : Panel
    {
        Label _label;
 
        public ExplorerStatusBar()
        {
            Height = 22;
            Dock   = DockStyle.Bottom;
            BackColor = Th.Bg;
 
            _label = new Label
            {
                AutoSize = false,
                Dock     = DockStyle.Fill,
                TextAlign= ContentAlignment.MiddleLeft,
                Font     = Th.UiFont,
                Padding  = new Padding(6, 0, 0, 0),
            };
            Controls.Add(_label);
        }
 
        public string Text
        {
            get => _label.Text;
            set => _label.Text = value;
        }
 
        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var p = new Pen(Th.PaneSep))
                e.Graphics.DrawLine(p, 0, 0, Width, 0);
        }
    }
 
    // --------------------------------------------------------------------------
    //  MAIN EXPLORER FORM
    // --------------------------------------------------------------------------
    class ExplorerForm : Form
    {
        TopNavBar        _nav;
        CommandBar       _cmd;
        TreePane         _tree;
        ContentPane      _content;
        SplitterBar      _splitter;
        ExplorerStatusBar _status;
        Panel            _mainArea;
 
        // Navigation history
        List<string> _history    = new List<string>();
        int          _historyIdx = -1;
 
        public ExplorerForm()
        {
            Text           = "File Explorer";
            MinimumSize    = new Size(700, 450);
            Size           = new Size(1100, 680);
            StartPosition  = FormStartPosition.CenterScreen;
            BackColor      = Th.Bg;
            Icon           = CreateAppIcon();
            Font           = Th.UiFont;
 
            BuildLayout();
            WireEvents();
 
            // Open Quick Access on start
            Navigate("Quick access");
        }
 
        void BuildLayout()
        {
            SuspendLayout();
 
            _nav    = new TopNavBar();
            _cmd    = new CommandBar();
            _status = new ExplorerStatusBar();
 
            // Main area (tree + splitter + content)
            _mainArea = new Panel { Dock = DockStyle.Fill };
 
            _tree    = new TreePane  { Dock = DockStyle.Left, Width = 220 };
            _content = new ContentPane { Dock = DockStyle.Fill };
            _splitter = new SplitterBar(_tree) { Dock = DockStyle.Left };
 
            _mainArea.Controls.Add(_content);
            _mainArea.Controls.Add(_splitter);
            _mainArea.Controls.Add(_tree);
 
            Controls.Add(_mainArea);
            Controls.Add(_status);
            Controls.Add(_cmd);
            Controls.Add(_nav);
 
            ResumeLayout(false);
        }
 
        void WireEvents()
        {
            // Navigation bar events
            _nav.BackClick    += (s, e) => GoBack();
            _nav.ForwardClick += (s, e) => GoForward();
            _nav.UpClick      += (s, e) => GoUp();
            _nav.Navigate     += path => Navigate(path);
            _nav.SearchChanged += q => _status.Text = string.IsNullOrEmpty(q) ? "" : $"Search: {q}";
 
            // Command bar events
            _cmd.NewFolderClick    += (s, e) => NewFolder();
            _cmd.HelpClick         += (s, e) => OpenHelp();
            _cmd.PreviewPaneClick  += (s, e) => TogglePreview();
            _cmd.ViewChanged       += vm => _status.Text = $"View: {vm}";
 
            // Tree events
            _tree.NodeSelected += node =>
            {
                string path = node.Path;
                if (path != null && Directory.Exists(path))
                    NavigateContent(path);
                else
                    _status.Text = node.Label;
            };
 
            // Content events
            _content.ItemActivated += item =>
            {
                if (item.IsDirectory)
                    Navigate(item.FullPath);
                else
                    OpenFile(item.FullPath);
            };
        }
 
        // -- Navigation helpers ---------------------------------------------
        void Navigate(string path)
        {
            // Trim history forward if we navigated back
            if (_historyIdx < _history.Count - 1)
                _history.RemoveRange(_historyIdx + 1, _history.Count - _historyIdx - 1);
 
            _history.Add(path);
            _historyIdx = _history.Count - 1;
 
            ApplyNavigation(path);
        }
 
        void NavigateContent(string path)
        {
            // Navigate without updating history (called from tree clicks)
            // Still push to history so back/forward work
            Navigate(path);
        }
 
        void ApplyNavigation(string path)
        {
            _nav.CurrentPath    = path;
            _nav.BackEnabled    = _historyIdx > 0;
            _nav.ForwardEnabled = _historyIdx < _history.Count - 1;
 
            if (path == "Quick access")
            {
                // Show pinned folders in content
                ShowQuickAccess();
                _tree.SelectPath(null);
            }
            else if (Directory.Exists(path))
            {
                _content.LoadPath(path);
                _tree.SelectPath(path);
                Text = $"{Path.GetFileName(path) ?? path} – File Explorer";
            }
 
            UpdateStatus();
        }
 
        void ShowQuickAccess()
        {
            // We don't have a dedicated method on ContentPane for virtual folders,
            // so load the user profile folder as a representative default.
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            _content.LoadPath(profile);
            Text = "Quick access – File Explorer";
        }
 
        void GoBack()
        {
            if (_historyIdx > 0)
            {
                _historyIdx--;
                ApplyNavigation(_history[_historyIdx]);
            }
        }
 
        void GoForward()
        {
            if (_historyIdx < _history.Count - 1)
            {
                _historyIdx++;
                ApplyNavigation(_history[_historyIdx]);
            }
        }
 
        void GoUp()
        {
            string cur = _nav.CurrentPath;
            if (string.IsNullOrEmpty(cur) || cur == "Quick access") return;
 
            string parent = null;
            try { parent = Directory.GetParent(cur)?.FullName; } catch { }
 
            if (parent != null) Navigate(parent);
        }
 
        // -- Actions --------------------------------------------------------
        void NewFolder()
        {
            string cur = _content.CurrentPath;
            if (!Directory.Exists(cur)) return;
            string newPath = Path.Combine(cur, "New folder");
            int i = 2;
            while (Directory.Exists(newPath)) newPath = Path.Combine(cur, $"New folder ({i++})");
            try
            {
                Directory.CreateDirectory(newPath);
                _content.LoadPath(cur);
                _status.Text = $"Created: {newPath}";
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
 
        void OpenHelp() =>
            Process.Start(new ProcessStartInfo("https://go.microsoft.com/fwlink/?LinkID=2004439") { UseShellExecute = true });
 
        void TogglePreview() => _status.Text = "Preview pane toggled.";
 
        void OpenFile(string path)
        {
            try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        }
 
        void UpdateStatus()
        {
            string path = _nav.CurrentPath;
            if (path == "Quick access") { _status.Text = "Quick access"; return; }
            if (!Directory.Exists(path)) { _status.Text = path; return; }
            try
            {
                int dirs  = Directory.GetDirectories(path).Length;
                int files = Directory.GetFiles(path).Length;
                _status.Text = $"{dirs + files} items";
            }
            catch { _status.Text = path; }
        }
 
        // Simple icon generated at runtime (48×48)
        static Icon CreateAppIcon()
        {
            var bmp = new Bitmap(32, 32);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.Clear(Color.Transparent);
                // Folder shape
                using (var b = new SolidBrush(Color.FromArgb(255, 186, 0)))
                { g.FillRectangle(b, 2, 10, 28, 18); g.FillRectangle(b, 2, 7, 12, 5); }
                using (var p = new Pen(Color.FromArgb(200, 150, 0), 1.5f))
                { g.DrawRectangle(p, 2, 10, 27, 17); g.DrawRectangle(p, 2, 7, 11, 4); }
            }
            using (var ms = new MemoryStream())
            {
                bmp.Save(ms, System.Drawing.Imaging.ImageFormat.Png);
                bmp.Dispose();
                // Minimal ICO wrapper
                byte[] png  = ms.ToArray();
                using (var ico = new MemoryStream())
                {
                    // ICO header
                    ico.Write(new byte[] { 0, 0, 1, 0, 1, 0 }, 0, 6);
                    ico.Write(new byte[] { 32, 32, 0, 0, 1, 0, 32, 0 }, 0, 8);
                    int dataOffset = 6 + 16;
                    byte[] dataOffsetBytes = BitConverter.GetBytes(dataOffset);
                    ico.Write(dataOffsetBytes, 0, 4);
                    // Actually, embedding PNG in ICO for simplicity is not standard
                    // Return null and fall back to no icon
                    return null;
                }
            }
        }
 
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // Ensure toolbar layout is computed after form is shown
            _nav.Invalidate();
            _cmd.Invalidate();
        }
 
        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.KeyCode == Keys.Back || (e.Alt && e.KeyCode == Keys.Left))  GoBack();
            else if (e.Alt && e.KeyCode == Keys.Right) GoForward();
            else if (e.Alt && e.KeyCode == Keys.Up)    GoUp();
            else if (e.KeyCode == Keys.F5)             _content.LoadPath(_content.CurrentPath);
        }
    }
}
 