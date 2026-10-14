using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MiniTC
{
    public class Program
    {
        private static void LogCrash(string where, object ex)
        {
            try
            {
                Directory.CreateDirectory(@"C:\MiniTC");
                File.AppendAllText(@"C:\MiniTC\crash.log", DateTime.Now + " [" + where + "]\r\n" + ex + "\r\n\r\n");
            }
            catch { }
        }

        [STAThread]
        public static void Main()
        {
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += (s, e) =>
            {
                LogCrash("ThreadException", e.Exception);
                MessageBox.Show(e.Exception.ToString(), "MiniTC - greska");
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                LogCrash("UnhandledException", e.ExceptionObject);
                try { MessageBox.Show(e.ExceptionObject.ToString(), "MiniTC - fatalna greska"); } catch { }
            };
            TaskScheduler.UnobservedTaskException += (s, e) => { LogCrash("UnobservedTask", e.Exception); e.SetObserved(); };

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MiniCommander());
        }
    }

    public class DarkColorTable : ProfessionalColorTable
    {
        static readonly Color bg = Color.FromArgb(35, 35, 38);
        static readonly Color menuBg = Color.FromArgb(45, 45, 48);
        static readonly Color hover = Color.FromArgb(60, 60, 65);
        static readonly Color border = Color.FromArgb(60, 60, 60);
        static readonly Color blue = Color.FromArgb(0, 122, 204);
        static readonly Color green = Color.FromArgb(16, 185, 129);

        public override Color ToolStripDropDownBackground { get { return menuBg; } }
        public override Color ImageMarginGradientBegin { get { return menuBg; } }
        public override Color ImageMarginGradientMiddle { get { return menuBg; } }
        public override Color ImageMarginGradientEnd { get { return menuBg; } }
        public override Color MenuBorder { get { return border; } }
        public override Color MenuItemBorder { get { return blue; } }
        public override Color MenuItemSelected { get { return hover; } }
        public override Color MenuItemSelectedGradientBegin { get { return hover; } }
        public override Color MenuItemSelectedGradientEnd { get { return hover; } }
        public override Color MenuItemPressedGradientBegin { get { return menuBg; } }
        public override Color MenuItemPressedGradientMiddle { get { return menuBg; } }
        public override Color MenuItemPressedGradientEnd { get { return menuBg; } }
        public override Color MenuStripGradientBegin { get { return bg; } }
        public override Color MenuStripGradientEnd { get { return bg; } }
        public override Color ToolStripGradientBegin { get { return bg; } }
        public override Color ToolStripGradientMiddle { get { return bg; } }
        public override Color ToolStripGradientEnd { get { return bg; } }
        public override Color ToolStripBorder { get { return bg; } }
        public override Color ToolStripContentPanelGradientBegin { get { return bg; } }
        public override Color ToolStripContentPanelGradientEnd { get { return bg; } }
        public override Color ToolStripPanelGradientBegin { get { return bg; } }
        public override Color ToolStripPanelGradientEnd { get { return bg; } }
        public override Color OverflowButtonGradientBegin { get { return bg; } }
        public override Color OverflowButtonGradientMiddle { get { return bg; } }
        public override Color OverflowButtonGradientEnd { get { return bg; } }
        public override Color ButtonSelectedBorder { get { return border; } }
        public override Color ButtonSelectedGradientBegin { get { return hover; } }
        public override Color ButtonSelectedGradientMiddle { get { return hover; } }
        public override Color ButtonSelectedGradientEnd { get { return hover; } }
        public override Color ButtonSelectedHighlight { get { return hover; } }
        public override Color ButtonPressedBorder { get { return blue; } }
        public override Color ButtonPressedGradientBegin { get { return blue; } }
        public override Color ButtonPressedGradientMiddle { get { return blue; } }
        public override Color ButtonPressedGradientEnd { get { return blue; } }
        public override Color ButtonPressedHighlight { get { return blue; } }
        public override Color ButtonCheckedGradientBegin { get { return green; } }
        public override Color ButtonCheckedGradientMiddle { get { return green; } }
        public override Color ButtonCheckedGradientEnd { get { return green; } }
        public override Color ButtonCheckedHighlight { get { return green; } }
        public override Color CheckBackground { get { return blue; } }
        public override Color CheckSelectedBackground { get { return blue; } }
        public override Color CheckPressedBackground { get { return blue; } }
        public override Color SeparatorDark { get { return border; } }
        public override Color SeparatorLight { get { return border; } }
        public override Color GripDark { get { return border; } }
        public override Color GripLight { get { return border; } }
    }

    public class DarkRenderer : ToolStripProfessionalRenderer
    {
        public DarkRenderer() : base(new DarkColorTable()) { RoundedEdges = false; }
        protected override void OnRenderItemText(ToolStripItemTextRenderEventArgs e)
        {
            e.TextColor = e.Item.Enabled ? Color.White : Color.Gray;
            base.OnRenderItemText(e);
        }
        protected override void OnRenderArrow(ToolStripArrowRenderEventArgs e)
        {
            e.ArrowColor = Color.White;
            base.OnRenderArrow(e);
        }
    }

    public static class DarkTheme
    {
        public static readonly Color Bg = Color.FromArgb(30, 30, 30);
        public static readonly Color Input = Color.FromArgb(60, 60, 60);
        public static readonly Color Blue = Color.FromArgb(0, 122, 204);
        public static readonly Color Grey = Color.FromArgb(80, 80, 80);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string pszSubIdList);

        public static void DarkScroll(Control c)
        {
            Action a = () => { try { SetWindowTheme(c.Handle, "DarkMode_Explorer", null); } catch { } };
            if (c.IsHandleCreated) a();
            c.HandleCreated += (s, e) => a();
        }

        public static void Apply(Control root)
        {
            var form = root as Form;
            if (form != null)
            {
                form.BackColor = Bg;
                form.ForeColor = Color.White;
                form.Font = new Font("Consolas", 9f);
            }
            Walk(root);
        }

        private static void Walk(Control parent)
        {
            foreach (Control c in parent.Controls)
            {
                if (c is Button)
                {
                    var b = (Button)c;
                    b.FlatStyle = FlatStyle.Flat;
                    if (b.ForeColor == SystemColors.ControlText) b.ForeColor = Color.White;
                    if (b.BackColor == SystemColors.Control)
                        b.BackColor = (b.DialogResult == DialogResult.OK || b.DialogResult == DialogResult.Yes) ? Blue : Grey;
                }
                else if (c is TextBox)
                {
                    if (c.BackColor == SystemColors.Window || c.BackColor == SystemColors.Control) c.BackColor = Input;
                    if (c.ForeColor == SystemColors.WindowText || c.ForeColor == SystemColors.ControlText) c.ForeColor = Color.White;
                    ((TextBox)c).BorderStyle = BorderStyle.FixedSingle;
                }
                else if (c is ComboBox)
                {
                    c.BackColor = Input; c.ForeColor = Color.White; ((ComboBox)c).FlatStyle = FlatStyle.Flat;
                }
                else if (c is ListBox)
                {
                    c.BackColor = Color.FromArgb(25, 25, 25);
                    c.ForeColor = Color.White;
                    ((ListBox)c).BorderStyle = BorderStyle.FixedSingle;
                    DarkScroll(c);
                }
                else if (c is Label || c is CheckBox || c is RadioButton || c is GroupBox)
                {
                    if (c.ForeColor == SystemColors.ControlText) c.ForeColor = Color.White;
                }
                if (c.HasChildren) Walk(c);
            }
        }
    }

    public class Prompt
    {
        public static string Show(string text, string caption, string defaultValue = "")
        {
            int lines = Math.Max(1, text.Split('\n').Length);
            int lblH = 18 * lines + 4;
            using (Form prompt = new Form()
            {
                Width = 420, Height = 130 + lblH, FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false
            })
            {
                Label textLabel = new Label() { Left = 10, Top = 10, Width = 390, Height = lblH, Text = text };
                TextBox textBox = new TextBox() { Left = 10, Top = 14 + lblH, Width = 385, Text = defaultValue };
                Button confirmation = new Button() { Text = "Ok", Left = 220, Width = 85, Top = 48 + lblH, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Otkazi", Left = 310, Width = 85, Top = 48 + lblH, DialogResult = DialogResult.Cancel };
                prompt.Controls.AddRange(new Control[] { textBox, confirmation, cancel, textLabel });
                prompt.AcceptButton = confirmation; prompt.CancelButton = cancel;
                DarkTheme.Apply(prompt);
                prompt.Shown += (s, e) => { textBox.Focus(); textBox.SelectAll(); };
                return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
            }
        }
    }

    public static class RecycleBin
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd; public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
            public ushort fFlags;
            [MarshalAs(UnmanagedType.Bool)] public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
        }
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)] private static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);
        private const uint FO_DELETE = 3;
        private const ushort FOF_NOCONFIRMATION = 0x10;
        private const ushort FOF_ALLOWUNDO = 0x40;
        private const ushort FOF_WANTNUKEWARNING = 0x4000;

        /// <summary>
        /// Salje sve putanje u Recycle Bin u JEDNOM pozivu. Shell sam prikazuje progres i svoje dijaloge
        /// za greske (Ponovi/Preskoci), a upozorava ako stavka ne moze u korpu (npr. mrezni disk) - nema tihog trajnog brisanja.
        /// </summary>
        public static bool SendToRecycle(IEnumerable<string> paths, IntPtr hwnd, out bool aborted, out int code)
        {
            var sb = new StringBuilder();
            foreach (var p in paths) { sb.Append(p); sb.Append('\0'); }
            sb.Append('\0');
            var fs = new SHFILEOPSTRUCT();
            fs.hwnd = hwnd; fs.wFunc = FO_DELETE; fs.pFrom = sb.ToString(); fs.pTo = null;
            fs.fFlags = (ushort)(FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_WANTNUKEWARNING);
            code = SHFileOperation(ref fs);
            aborted = fs.fAnyOperationsAborted;
            return code == 0 && !aborted;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, string pszObjectName, string pszPropertyPage);
        public static void ShowProperties(string path, IntPtr hwnd) { SHObjectProperties(hwnd, 2, path, null); }
    }

    public class MiniCommander : Form
    {
        // ===================== WinAPI =====================
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon; public int iIcon; public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)] public string szTypeName;
        }
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);
        [DllImport("user32.dll")][return: MarshalAs(UnmanagedType.Bool)] private static extern bool DestroyIcon(IntPtr hIcon);
        [DllImport("shlwapi.dll", CharSet = CharSet.Unicode, ExactSpelling = true)] private static extern int StrCmpLogicalW(string a, string b);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)][return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetDiskFreeSpaceEx(string lpDirectoryName, out ulong lpFreeBytesAvailable, out ulong lpTotalNumberOfBytes, out ulong lpTotalNumberOfFreeBytes);

        private const uint FILE_ATTRIBUTE_NORMAL = 0x80; private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const uint SHGFI_ICON = 0x100; private const uint SHGFI_SMALLICON = 0x1; private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        private const int WM_ACTIVATEAPP = 0x001C;

        private static readonly ConcurrentDictionary<string, Image> iconCache = new ConcurrentDictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static Image GetIconFor(string nameOrPath, bool isDir)
        {
            string key = isDir ? "\\folder" : Path.GetExtension(nameOrPath ?? "").ToLowerInvariant();
            if (string.IsNullOrEmpty(key) && !isDir) key = "\\noext";
            Image img; if (iconCache.TryGetValue(key, out img)) return img;
            img = null;
            try
            {
                var shfi = new SHFILEINFO(); uint attr = isDir ? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                string dummy = isDir ? "folder" : ("file" + (key == "\\noext" ? "" : key));
                IntPtr res = SHGetFileInfo(dummy, attr, ref shfi, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);
                if (res != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
                {
                    try { using (var ico = Icon.FromHandle(shfi.hIcon)) img = ico.ToBitmap(); } catch { img = null; } finally { DestroyIcon(shfi.hIcon); }
                }
            }
            catch { img = null; }
            iconCache[key] = img; // i null se kesira - ne pitamo shell ponovo za svaki red
            return img;
        }

        private const string ConfigFilePath = @"C:\MiniTC\MiniTC.txt";
        private const int LoadTimeoutMs = 8000;

        // ===================== Boje =====================
        private static readonly Color ColorBg = Color.FromArgb(30, 30, 30);
        private static readonly Color ColorFg = Color.FromArgb(220, 220, 220);
        private static readonly Color ColorDirFg = Color.FromArgb(235, 235, 170);
        private static readonly Color ColorAccent = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorToolbarBg = Color.FromArgb(35, 35, 38);
        private static readonly Color ColorToolbarFg = Color.White;
        private static readonly Color ColorSplitter = Color.FromArgb(60, 60, 60);
        private static readonly Color ColorTabBg = Color.FromArgb(35, 35, 38);
        private static readonly Color ColorCmdBg = Color.FromArgb(60, 60, 60);
        private static readonly Color ColorCmdFg = Color.White;
        private static readonly Color ColorStatusFg = Color.Silver;
        private static readonly Color ColorTabActiveBg = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorTabActiveFg = Color.White;
        private static readonly Color ColorTabInactiveBg = Color.FromArgb(60, 60, 65);
        private static readonly Color ColorTabInactiveFg = Color.White;
        private static readonly Color ColorHeaderBg = Color.FromArgb(45, 45, 48);
        private static readonly Color ColorHeaderFg = Color.FromArgb(220, 220, 220);
        private static readonly Color ColorSelBg = Color.FromArgb(0, 122, 204);
        private static readonly Color ColorSelInactiveBg = Color.FromArgb(62, 62, 68);
        private static readonly Color ColorSelFg = Color.White;
        private static readonly Color ColorMarkedFg = Color.FromArgb(255, 120, 120);
        private static readonly Color ColorShellOnBg = Color.FromArgb(16, 185, 129);
        private static readonly Color ColorHintFg = Color.Gray;
        private static readonly Color ColorGotoBtnBg = Color.FromArgb(16, 185, 129);
        private static readonly Color ColorDeletePermanentBg = Color.FromArgb(170, 50, 50);
        private static readonly Color ColorDeleteRecycleBg = Color.FromArgb(190, 110, 30);
        private static readonly Color ColorFilterBg = Color.FromArgb(15, 45, 90);
        private static readonly Color ColorFilterSelBg = Color.FromArgb(30, 70, 130);

        // ===================== Modeli =====================
        private class TabInfo { public string Path; }

        /// <summary>Jedan red u panelu. Metapodaci su u memoriji - sortiranje i crtanje vise ne citaju disk.</summary>
        private sealed class FileItem : ListViewItem
        {
            public string FullPath;
            public string FileName;
            public bool IsDir;
            public bool IsUp;
            public long Size;
            public DateTime Modified;
            public string Ext = "";
            public FileItem(string text) : base(text) { }
        }

        private sealed class EntryData
        {
            public string Name; public string FullPath; public bool IsDir; public long Size; public DateTime Modified; public FileAttributes Attr;
        }

        private sealed class DirSnapshot
        {
            public List<EntryData> Entries = new List<EntryData>();
            public string Error;
            public bool NotFound;
            public long Signature;
        }

        private sealed class PanelState
        {
            public string Name;
            public ListView List;
            public TextBox PathBox;
            public ComboBox Drive;
            public FlowLayoutPanel TabPanel;
            public string Current;
            public readonly HashSet<string> Marked = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public List<FileItem> AllItems = new List<FileItem>();
            public string FilterText = "";
            public int SortCol = 0;
            public SortOrder SortOrder = SortOrder.Ascending;
            public List<TabInfo> Tabs = new List<TabInfo>();
            public int TabIdx = 0;
            public FileSystemWatcher Watcher;
            public bool RefreshPending;
            public long Signature;
        }

        private sealed class FileItemComparer : IComparer<FileItem>
        {
            private readonly int col; private readonly bool desc;
            public FileItemComparer(int col, SortOrder order) { this.col = col; desc = order == SortOrder.Descending; }
            public int Compare(FileItem a, FileItem b)
            {
                if (ReferenceEquals(a, b)) return 0;
                if (a.IsUp) return -1; if (b.IsUp) return 1;
                if (a.IsDir != b.IsDir) return a.IsDir ? -1 : 1; // folderi uvek gore, kao u TC
                int r = 0;
                switch (col)
                {
                    case 1: r = (a.IsDir || b.IsDir) ? 0 : a.Size.CompareTo(b.Size); break;
                    case 2: r = a.Modified.CompareTo(b.Modified); break;
                    case 3: r = string.Compare(a.Ext, b.Ext, StringComparison.OrdinalIgnoreCase); break;
                }
                if (r == 0) r = NameCompare(a.FileName, b.FileName);
                return desc ? -r : r;
            }
            private static int NameCompare(string x, string y)
            {
                try { return StrCmpLogicalW(x, y); } catch { return string.Compare(x, y, StringComparison.OrdinalIgnoreCase); }
            }
        }

        // ===================== File-operation engine modeli =====================
        private enum ConflictMode { Ask = 0, OverwriteAll = 1, SkipAll = 2, OverwriteOlder = 3 }

        private sealed class ConflictResult { public string Path; public bool Overwrite; }

        private sealed class OpContext
        {
            public bool Move;
            public bool ByFiles;               // progres po broju fajlova (brisanje)
            public volatile bool Indeterminate; // premestanje na istom disku - nema skeniranja
            public volatile bool Cancel;
            public volatile bool Canceled;
            public volatile string Phase = "";
            public volatile string CurrentName = "";
            public ConflictMode Conflict = ConflictMode.Ask;
            public bool SkipAllErrors;
            public long TotalBytes, DoneBytes;
            public int TotalFiles, DoneFiles, Skipped, Failed;
            public string DestDir;
            public readonly List<string> Errors = new List<string>();
            public readonly HashSet<string> Completed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            public readonly Stopwatch Watch = Stopwatch.StartNew();
            public Form Form;
        }

        private static readonly EnumerationOptions EnumAll = new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = false, RecurseSubdirectories = false, ReturnSpecialDirectories = false };
        private static readonly EnumerationOptions EnumScan = new EnumerationOptions { AttributesToSkip = 0, IgnoreInaccessible = true, RecurseSubdirectories = false, ReturnSpecialDirectories = false };

        // ===================== Progres prozor =====================
        /// <summary>Modalni progres na UI niti; posao radi pozadinska nit. Prozor sam cita stanje tajmerom.</summary>
        private sealed class CopyProgressForm : Form
        {
            private readonly OpContext ctx;
            private readonly ProgressBar bar;
            private readonly Label lblFile, lblInfo;
            private readonly Button btnCancel;
            private readonly System.Windows.Forms.Timer timer;
            public bool Done;

            public CopyProgressForm(string title, OpContext c)
            {
                ctx = c;
                Text = title; Width = 620; Height = 175; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent;
                MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
                lblFile = new Label { Left = 10, Top = 10, Width = 585, Height = 36, Text = "Priprema...", AutoEllipsis = true };
                bar = new ProgressBar { Left = 10, Top = 50, Width = 585, Height = 20, Minimum = 0, Maximum = 100, Style = ProgressBarStyle.Continuous };
                lblInfo = new Label { Left = 10, Top = 78, Width = 450, Height = 40, Text = "" };
                btnCancel = new Button { Text = "Otkazi (Esc)", Left = 485, Top = 90, Width = 110, Height = 28 };
                btnCancel.Click += (s, e) => RequestCancel();
                Controls.AddRange(new Control[] { lblFile, bar, lblInfo, btnCancel });
                KeyPreview = true;
                KeyDown += (s, e) => { if (e.KeyCode == Keys.Escape) { RequestCancel(); e.Handled = true; } };
                FormClosing += (s, e) => { if (!Done) { e.Cancel = true; RequestCancel(); } };
                DarkTheme.Apply(this);
                timer = new System.Windows.Forms.Timer { Interval = 120 };
                timer.Tick += (s, e) => UpdateUi();
                timer.Start();
            }

            private void RequestCancel()
            {
                ctx.Cancel = true;
                btnCancel.Enabled = false; btnCancel.Text = "Otkazujem...";
            }

            private void UpdateUi()
            {
                try
                {
                    lblFile.Text = ctx.Phase + ctx.CurrentName;
                    long total = Interlocked.Read(ref ctx.TotalBytes), done = Interlocked.Read(ref ctx.DoneBytes);
                    int tf = Volatile.Read(ref ctx.TotalFiles), df = Volatile.Read(ref ctx.DoneFiles);
                    if (ctx.Indeterminate)
                    {
                        if (bar.Style != ProgressBarStyle.Marquee) { bar.Style = ProgressBarStyle.Marquee; bar.MarqueeAnimationSpeed = 30; }
                        lblInfo.Text = df + " fajlova";
                        return;
                    }
                    if (bar.Style != ProgressBarStyle.Continuous) bar.Style = ProgressBarStyle.Continuous;
                    int pct;
                    if (!ctx.ByFiles && total > 0) pct = (int)Math.Max(0, Math.Min(100, done * 100 / total));
                    else pct = tf > 0 ? Math.Min(100, df * 100 / tf) : 0;
                    if (bar.Value != pct) bar.Value = pct;
                    double sec = Math.Max(0.5, ctx.Watch.Elapsed.TotalSeconds);
                    if (ctx.ByFiles) lblInfo.Text = df + " / " + tf + " fajlova";
                    else lblInfo.Text = df + " / " + tf + " fajlova    " + FormatSize(done) + " / " + FormatSize(total) + "\n" + FormatSize((long)(done / sec)) + "/s";
                }
                catch { }
            }

            protected override void Dispose(bool disposing)
            {
                if (disposing) { try { timer.Stop(); timer.Dispose(); } catch { } }
                base.Dispose(disposing);
            }
        }

        // ===================== Polja =====================
        private PanelState L, R;
        private PanelState active;
        private PanelState Active { get { return active ?? L; } }
        private PanelState Other { get { return Active == L ? R : L; } }

        private Label statusLabel;
        private TextBox cmdBox;
        private Panel cmdPanel;
        private Button cmdSaveBtn;
        private List<string> savedCommands = new List<string>();
        private int savedCmdIdx = -1;
        private string savedCmdDraft = "";
        private ToolStrip toolBar;
        private ToolStripComboBox fontCombo;
        private ToolStripButton hiddenToggle;
        private SplitContainer topSplit;
        private double splitRatio = 0.5;
        private bool userDraggingSplitter = false;
        private string compareToolPath = "";
        private string compareToolParams = "";
        private Font listFont = new Font("Consolas", 14f);
        private Font listFontBold = new Font("Consolas", 14f, FontStyle.Bold);
        private Font uiFont = null;
        private bool useRealShellMenu = false;
        private bool showHidden = false;
        private bool opRunning = false;
        private bool lastLoadNotFound = false;
        private string lastStatus = "";
        private string lastConfigError = "";
        private readonly System.Windows.Forms.Timer refreshTimer = new System.Windows.Forms.Timer { Interval = 700 };
        private static readonly string[] ColumnNames = { "Ime", "Velicina", "Datum", "Tip" };

        // ===================== Konstruktor =====================
        public MiniCommander()
        {
            Text = "Mini TC v1.5 (.NET 9)"; Width = 1200; Height = 800; WindowState = FormWindowState.Maximized;
            KeyPreview = true; BackColor = ColorBg; Font = new Font("Consolas", 9.5f);
            ToolStripManager.Renderer = new DarkRenderer(); DoubleBuffered = true;

            L = new PanelState { Name = "L" };
            R = new PanelState { Name = "R" };

            toolBar = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, BackColor = ColorToolbarBg, ForeColor = ColorToolbarFg };
            toolBar.Items.Add(new ToolStripButton("<", null, (s, e) => SyncPanel(true)) { ToolTipText = "Levi = Desni (Ctrl+Levo)" });
            toolBar.Items.Add(new ToolStripButton(">", null, (s, e) => SyncPanel(false)) { ToolTipText = "Desni = Levi (Ctrl+Desno)" });
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripButton("F4 Notepad", null, (s, e) => OpenInNotepad()));
            toolBar.Items.Add(new ToolStripButton("F5 Kopiraj", null, (s, e) => StartTransfer(false)));
            toolBar.Items.Add(new ToolStripButton("F6 Premesti", null, (s, e) => StartTransfer(true)));
            toolBar.Items.Add(new ToolStripButton("F7 Novi Dir", null, (s, e) => CreateFolder()));
            toolBar.Items.Add(new ToolStripButton("F8 Putanja", null, (s, e) => CopyPathToClipboard()));
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripButton("Alt+F7 Search", null, (s, e) => OpenSearchDialog()) { ToolTipText = "Pretraga fajlova - Alt+F7" });
            toolBar.Items.Add(new ToolStripButton("F3 Compare", null, (s, e) => RunCompareTool()) { ToolTipText = "Uporedi 2 fajla (1 levo + 1 desno obelezen) - F3" });
            toolBar.Items.Add(new ToolStripButton("Compare Setup", null, (s, e) => SetupCompareTool()));
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripButton("001 Rename", null, (s, e) => SequentialRenameFiles()) { ToolTipText = "Sekvencijalno preimenuj sve fajlove u aktivnom panelu (001,002...) - zadrzava ekstenzije, redosled kako su sortirani" });
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripButton("Ctrl+T Tab", null, (s, e) => NewTab()));
            toolBar.Items.Add(new ToolStripButton("Ctrl+W Close", null, (s, e) => CloseTab()));
            toolBar.Items.Add(new ToolStripButton("Ctrl+Tab Next", null, (s, e) => NextTab()));
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripLabel(" Font:"));
            fontCombo = new ToolStripComboBox { Width = 60 };
            fontCombo.Items.AddRange(new object[] { "10", "12", "14", "16", "18", "20", "22", "26", "32" });
            fontCombo.Text = "14"; fontCombo.ComboBox.BackColor = ColorCmdBg; fontCombo.ComboBox.ForeColor = Color.White;
            fontCombo.ComboBox.FlatStyle = FlatStyle.Flat; fontCombo.SelectedIndexChanged += (s, e) => ChangeFont();
            toolBar.Items.Add(fontCombo); toolBar.Items.Add(new ToolStripSeparator());
            var shellToggle = new ToolStripButton("Shell OFF") { CheckOnClick = true, Checked = false };
            shellToggle.CheckedChanged += (s, e) => { useRealShellMenu = shellToggle.Checked; shellToggle.Text = useRealShellMenu ? "Shell ON" : "Shell OFF"; shellToggle.BackColor = useRealShellMenu ? ColorShellOnBg : ColorToolbarBg; };
            toolBar.Items.Add(shellToggle);
            hiddenToggle = new ToolStripButton("Skriveni OFF") { CheckOnClick = true, Checked = false, ToolTipText = "Prikazi skrivene fajlove (Ctrl+H)" };
            hiddenToggle.CheckedChanged += (s, e) => SetShowHidden(hiddenToggle.Checked);
            toolBar.Items.Add(hiddenToggle);
            toolBar.Items.Add(new ToolStripButton("Ctrl+R Osvezi", null, (s, e) => { RefreshPanel(L, true); RefreshPanel(R, true); }));

            topSplit = new SplitContainer { Dock = DockStyle.Fill, BackColor = ColorSplitter, BorderStyle = BorderStyle.None, SplitterWidth = 6 };
            topSplit.Panel1.Controls.Add(BuildPanel(L));
            topSplit.Panel2.Controls.Add(BuildPanel(R));
            
            // SplitterMoving se okida SAMO kada korisnik vuce misem
            topSplit.SplitterMoving += (s, e) => { userDraggingSplitter = true; };
            topSplit.SplitterMoved += (s, e) =>
            {
                if (!userDraggingSplitter) return;           // pomeranje od layout-a ignorisemo
                userDraggingSplitter = false;
                if (topSplit.Width > 250) splitRatio = (double)topSplit.SplitterDistance / topSplit.Width;
            };
            topSplit.DoubleClick += (s, e) => { splitRatio = 0.5; FixSplitter(); }; // dupli klik na liniju = sredina


            cmdBox = new TextBox { Dock = DockStyle.Fill, BackColor = ColorCmdBg, ForeColor = ColorCmdFg, BorderStyle = BorderStyle.FixedSingle, Font = listFont };
            cmdBox.KeyDown += CmdBox_KeyDown;
            cmdSaveBtn = new Button { Text = "\U0001F4BE Save", Dock = DockStyle.Left, Width = 80, TabStop = false, FlatStyle = FlatStyle.Flat, BackColor = ColorAccent, ForeColor = Color.White };
            cmdSaveBtn.Click += (s, e) => SaveCurrentCommand();
            new ToolTip().SetToolTip(cmdSaveBtn, "Zapamti komandu iz polja (MiniTC.txt). Strelica gore/dole u polju lista sacuvane komande.");
            cmdPanel = new Panel { Dock = DockStyle.Bottom, Height = Math.Max(26, cmdBox.PreferredHeight) };
            cmdPanel.Controls.Add(cmdBox); cmdPanel.Controls.Add(cmdSaveBtn);
            var cmdLine = new Label { Dock = DockStyle.Bottom, Height = 2, BackColor = ColorSplitter };
            statusLabel = new Label { Dock = DockStyle.Bottom, Height = 26, BackColor = ColorToolbarBg, ForeColor = ColorStatusFg, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0), AutoEllipsis = true };
            EnableDoubleBuffer(statusLabel);
            Controls.Add(topSplit); Controls.Add(cmdLine); Controls.Add(cmdPanel); Controls.Add(statusLabel); Controls.Add(toolBar);

            Resize += (s, e) => { if (WindowState == FormWindowState.Minimized) return; FixSplitter(); ResizeColumns(L.List); ResizeColumns(R.List); };
            Load += (s, e) => FixSplitter();
            Shown += (s, e) => { FixSplitter(); Active.List.Focus(); };
            FormClosing += (s, e) => { SaveTabsToTxt(); DisposeWatcher(L); DisposeWatcher(R); };
            refreshTimer.Tick += RefreshTimer_Tick;

            LoadDrives(L); LoadDrives(R);
            LoadTabsFromTxt();
            foreach (var p in new[] { L, R })
            {
                if (p.TabIdx >= p.Tabs.Count) p.TabIdx = 0;
                string start = p.Tabs[p.TabIdx].Path;
                if (!LoadPanel(p, start, null, false) && !LoadPanel(p, @"C:\", null, false)) p.Current = @"C:\";
            }
            active = L;
            RenderTabs(L); RenderTabs(R);
            ChangeFont();
            UpdateHeaders(L); UpdateHeaders(R);
        }

        private Panel BuildPanel(PanelState p)
        {
            p.Drive = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, TabStop = false, BackColor = ColorCmdBg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            p.Drive.DropDown += (s, e) => LoadDrives(p);
            p.Drive.SelectionChangeCommitted += (s, e) => { var root = p.Drive.SelectedItem as string; if (root != null) { if (!LoadPanel(p, root, null, true)) UpdateDriveCombo(p); } };
            p.PathBox = new TextBox { Dock = DockStyle.Top, BackColor = ColorCmdBg, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            p.PathBox.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; if (LoadPanel(p, p.PathBox.Text.Trim().Trim('"'), null, true)) p.List.Focus(); }
            };
            p.TabPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, BackColor = ColorTabBg, WrapContents = false, AutoScroll = true };
            p.List = CreateFileList(p);
            DarkTheme.DarkScroll(p.List);
            var panel = new Panel { Dock = DockStyle.Fill, BackColor = ColorBg };
            panel.Controls.Add(p.List); panel.Controls.Add(p.PathBox); panel.Controls.Add(p.Drive); panel.Controls.Add(p.TabPanel);
            return panel;
        }

        private void FixSplitter()
        {
            try
            {
                int total = topSplit.Width; if (total < 250) return;
                topSplit.Panel1MinSize = 100; topSplit.Panel2MinSize = 100;
                int want = (int)(total * splitRatio); int min = topSplit.Panel1MinSize; int max = total - topSplit.Panel2MinSize - topSplit.SplitterWidth;
                if (max < min) return; if (want < min) want = min; if (want > max) want = max;
                if (topSplit.SplitterDistance != want) topSplit.SplitterDistance = want;
            }
            catch { }
        }

        // ===================== Konfiguracija =====================
        private void LoadTabsFromTxt()
        {
            try
            {
                compareToolPath = ""; compareToolParams = ""; savedCommands.Clear();
                L.Tabs = new List<TabInfo>(); R.Tabs = new List<TabInfo>();
                if (!File.Exists(ConfigFilePath))
                {
                    L.Tabs.Add(new TabInfo { Path = @"C:\" });
                    R.Tabs.Add(new TabInfo { Path = Directory.Exists(@"D:\") ? @"D:\" : @"C:\" });
                    return;
                }
                var lines = File.ReadAllLines(ConfigFilePath); int mode = 0;
                foreach (var raw in lines)
                {
                    string line = raw.Trim(); if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.Equals("[LeviPanel]", StringComparison.OrdinalIgnoreCase) || line.Equals("LeviPanel", StringComparison.OrdinalIgnoreCase)) { mode = 1; continue; }
                    if (line.Equals("[DesniPanel]", StringComparison.OrdinalIgnoreCase) || line.Equals("DesniPanel", StringComparison.OrdinalIgnoreCase)) { mode = 2; continue; }
                    if (line.Equals("[CompareTool]", StringComparison.OrdinalIgnoreCase) || line.Equals("CompareTool", StringComparison.OrdinalIgnoreCase)) { mode = 3; continue; }
                    if (line.Equals("[SavedCommands]", StringComparison.OrdinalIgnoreCase)) { mode = 4; continue; }
                    if (mode == 4) { if (!savedCommands.Contains(line)) savedCommands.Add(line); continue; }
                    if (mode == 3)
                    {
                        if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase)) compareToolPath = line.Substring(5).Trim();
                        else if (line.StartsWith("Params=", StringComparison.OrdinalIgnoreCase)) compareToolParams = line.Substring(7).Trim();
                        else if (!line.StartsWith("[") && !line.StartsWith("<"))
                        {
                            if (string.IsNullOrEmpty(compareToolPath) && line.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)) compareToolPath = line;
                        }
                        continue;
                    }
                    if (line.StartsWith("<Folder>", StringComparison.OrdinalIgnoreCase))
                    {
                        int s = line.IndexOf('>') + 1; int e = line.LastIndexOf('<'); if (e > s) line = line.Substring(s, e - s).Trim();
                    }
                    if (line.StartsWith("<") && line.EndsWith(">")) continue;
                    // putanja se ne proverava ovde (mrezni disk bi zakocio start) - proverava je LoadPanel sa timeout-om
                    if (mode == 1) L.Tabs.Add(new TabInfo { Path = line });
                    else if (mode == 2) R.Tabs.Add(new TabInfo { Path = line });
                    else { if (L.Tabs.Count <= R.Tabs.Count) L.Tabs.Add(new TabInfo { Path = line }); else R.Tabs.Add(new TabInfo { Path = line }); }
                }
            }
            catch { }
            if (L.Tabs.Count == 0) L.Tabs.Add(new TabInfo { Path = @"C:\" });
            if (R.Tabs.Count == 0) R.Tabs.Add(new TabInfo { Path = @"C:\" });
            L.TabIdx = 0; R.TabIdx = 0;
        }

        private List<string> ReadSavedCommandsFromFile()
        {
            var res = new List<string>();
            try
            {
                if (!File.Exists(ConfigFilePath)) return res; bool inSection = false;
                foreach (var raw in File.ReadAllLines(ConfigFilePath))
                {
                    string line = raw.Trim(); if (line.Length == 0) continue;
                    if (line.Equals("[SavedCommands]", StringComparison.OrdinalIgnoreCase)) { inSection = true; continue; }
                    if (line.StartsWith("[") && line.EndsWith("]")) { inSection = false; continue; }
                    if (inSection && !res.Contains(line)) res.Add(line);
                }
            }
            catch { }
            return res;
        }
        private void MergeSavedCommandsFromFile() { foreach (var c in ReadSavedCommandsFromFile()) if (!savedCommands.Contains(c)) savedCommands.Add(c); }
        private bool WriteConfigFile(List<string> lines)
        {
            string tmp = ConfigFilePath + ".tmp";
            for (int attempt = 0; attempt < 4; attempt++)
            {
                try { File.WriteAllLines(tmp, lines); File.Move(tmp, ConfigFilePath, true); lastConfigError = ""; return true; }
                catch (Exception ex) { lastConfigError = ex.Message; Thread.Sleep(80); }
            }
            try { File.Delete(tmp); } catch { }
            return false;
        }
        private bool SaveTabsToTxt()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigFilePath); if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                foreach (var p in new[] { L, R }) if (p.TabIdx >= 0 && p.TabIdx < p.Tabs.Count && p.Current != null) p.Tabs[p.TabIdx].Path = p.Current;
                var outLines = new List<string>();
                outLines.Add("[LeviPanel]"); foreach (var t in L.Tabs) outLines.Add(t.Path); outLines.Add("");
                outLines.Add("[DesniPanel]"); foreach (var t in R.Tabs) outLines.Add(t.Path); outLines.Add("");
                outLines.Add("[CompareTool]"); outLines.Add("Path=" + compareToolPath); outLines.Add("Params=" + compareToolParams); outLines.Add("");
                outLines.Add("[SavedCommands]"); MergeSavedCommandsFromFile(); foreach (var c in savedCommands) outLines.Add(c);
                return WriteConfigFile(outLines);
            }
            catch (Exception ex) { lastConfigError = ex.Message; return false; }
        }

        // ===================== Tabovi =====================
        private void RenderTabs(PanelState p)
        {
            var panel = p.TabPanel;
            panel.SuspendLayout();
            try
            {
                while (panel.Controls.Count > p.Tabs.Count) { var c = panel.Controls[panel.Controls.Count - 1]; panel.Controls.Remove(c); c.Dispose(); }
                while (panel.Controls.Count < p.Tabs.Count)
                {
                    var btn = new Button { Height = 24, AutoSize = true, FlatStyle = FlatStyle.Flat, Margin = new Padding(1), TabStop = false };
                    btn.Click += (s, e) => SwitchTab(p, (int)((Control)s).Tag);
                    panel.Controls.Add(btn);
                }
                for (int i = 0; i < p.Tabs.Count; i++)
                {
                    var btn = (Button)panel.Controls[i];
                    btn.Tag = i;
                    string text = (i + 1) + ":" + GetTabName(p.Tabs[i].Path);
                    if (btn.Text != text) btn.Text = text;
                    Color bg = i == p.TabIdx ? ColorTabActiveBg : ColorTabInactiveBg;
                    Color fg = i == p.TabIdx ? ColorTabActiveFg : ColorTabInactiveFg;
                    if (btn.BackColor != bg) btn.BackColor = bg;
                    if (btn.ForeColor != fg) btn.ForeColor = fg;
                }
            }
            finally { panel.ResumeLayout(); }
        }
        private static string GetTabName(string path)
        {
            try { if (string.IsNullOrEmpty(path)) return ""; if (path.EndsWith(":\\") || path.EndsWith(":")) return path; string n = Path.GetFileName(path.TrimEnd('\\')); return string.IsNullOrEmpty(n) ? path : n; } catch { return path; }
        }
        private void SwitchTab(PanelState p, int idx)
        {
            if (idx < 0 || idx >= p.Tabs.Count) return;
            p.Tabs[p.TabIdx].Path = p.Current;
            p.TabIdx = idx;
            string target = p.Tabs[idx].Path;
            if (!LoadPanel(p, target, null, true))
            {
                var up = ResolveExistingFolder(target);
                if (up == null || !LoadPanel(p, up, null, true)) { p.Tabs[idx].Path = p.Current; p.List.Focus(); }
            }
            RenderTabs(p);
        }
        private void NewTab()
        {
            var p = Active;
            p.Tabs[p.TabIdx].Path = p.Current;
            p.Tabs.Insert(p.TabIdx + 1, new TabInfo { Path = p.Current });
            p.TabIdx++;
            RenderTabs(p); SaveTabsToTxt();
        }
        private void CloseTab()
        {
            var p = Active; if (p.Tabs.Count <= 1) return;
            p.Tabs.RemoveAt(p.TabIdx);
            int idx = Math.Max(0, p.TabIdx - 1);
            p.TabIdx = idx;
            if (!LoadPanel(p, p.Tabs[idx].Path, null, true)) p.Tabs[idx].Path = p.Current;
            RenderTabs(p); SaveTabsToTxt();
        }
        private void NextTab()
        {
            var p = Active; if (p.Tabs.Count <= 1) return;
            SwitchTab(p, (p.TabIdx + 1) % p.Tabs.Count);
        }

        // ===================== Komandna linija =====================
        private bool TryHandleCd(string cmd, PanelState p)
        {
            if (!cmd.StartsWith("cd", StringComparison.OrdinalIgnoreCase)) return false; if (cmd.Length > 2 && char.IsLetterOrDigit(cmd[2])) return false;
            string arg = cmd.Length > 2 ? cmd.Substring(2).Trim() : "";
            if (arg.StartsWith("/d ", StringComparison.OrdinalIgnoreCase)) arg = arg.Substring(3).Trim();
            if (arg.Length >= 2 && arg[0] == '"' && arg[arg.Length - 1] == '"') arg = arg.Substring(1, arg.Length - 2);
            string target;
            if (arg.Length == 0) target = p.Current;
            else if (arg == "\\" || arg == "/") target = Path.GetPathRoot(p.Current);
            else if (arg == "..") { var parent = Directory.GetParent(p.Current); target = parent != null ? parent.FullName : p.Current; }
            else if (arg.Length >= 2 && arg[1] == ':') target = arg.Length == 2 ? arg + "\\" : arg;
            else { try { target = Path.GetFullPath(Path.Combine(p.Current, arg)); } catch { target = arg; } }
            if (LoadPanel(p, target, SameFolder(target, Path.GetDirectoryName(p.Current ?? "")) ? p.Current : null, false)) cmdBox.Clear();
            return true;
        }
        private void SaveCurrentCommand()
        {
            string cmd = cmdBox.Text.Trim(); MergeSavedCommandsFromFile();
            if (cmd.Length == 0) Status(" Save: polje za komandu je prazno");
            else if (savedCommands.Contains(cmd)) Status(" Save: komanda vec postoji u MiniTC.txt");
            else { savedCommands.Add(cmd); if (SaveTabsToTxt()) Status(" Sacuvano u MiniTC.txt (" + savedCommands.Count + " komandi): " + cmd); else Status(" GRESKA: ne mogu da upisem u " + ConfigFilePath + " - " + lastConfigError); }
            cmdBox.Focus(); cmdBox.SelectionStart = cmdBox.TextLength;
        }
        private void NavigateSavedCommands(bool up)
        {
            if (savedCommands.Count == 0) return;
            if (savedCmdIdx == -1) { if (!up) return; savedCmdDraft = cmdBox.Text; savedCmdIdx = savedCommands.Count - 1; }
            else if (up) { if (savedCmdIdx > 0) savedCmdIdx--; } else { savedCmdIdx++; if (savedCmdIdx >= savedCommands.Count) savedCmdIdx = -1; }
            cmdBox.Text = savedCmdIdx == -1 ? savedCmdDraft : savedCommands[savedCmdIdx]; cmdBox.SelectionStart = cmdBox.TextLength;
        }
        private void CmdBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) { NavigateSavedCommands(e.KeyCode == Keys.Up); e.Handled = true; e.SuppressKeyPress = true; return; }
            if (e.KeyCode != Keys.Enter) return;
            e.SuppressKeyPress = true; savedCmdIdx = -1;
            string cmd = cmdBox.Text.Trim(); if (string.IsNullOrWhiteSpace(cmd)) return;
            var p = Active; string cur = p.Current;
            try
            {
                if (cmd.Length == 2 && cmd[1] == ':') cmd += "\\";
                if (TryHandleCd(cmd, p)) return;
                if (cmd == "..") { GoUp(p); cmdBox.Clear(); return; }
                if (cmd.IndexOfAny(Path.GetInvalidPathChars()) < 0 && cmd.IndexOfAny(new[] { '>', '<', '|', '"', '*', '?' }) < 0)
                {
                    string full = null; try { full = Path.GetFullPath(Path.IsPathRooted(cmd) ? cmd : Path.Combine(cur, cmd)); } catch { }
                    if (full != null && Directory.Exists(full)) { if (LoadPanel(p, full, null, false)) cmdBox.Clear(); return; }
                    if (full != null && File.Exists(full)) { Process.Start(new ProcessStartInfo(full) { UseShellExecute = true, WorkingDirectory = cur }); cmdBox.Clear(); return; }
                }
                if (cmd.Equals("cmd", StringComparison.OrdinalIgnoreCase)) { Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = cur, UseShellExecute = true }); cmdBox.Clear(); return; }
                string firstToken = cmd.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "";
                if (firstToken.Equals("notepad", StringComparison.OrdinalIgnoreCase) || firstToken.Equals("calc", StringComparison.OrdinalIgnoreCase) || firstToken.Equals("mspaint", StringComparison.OrdinalIgnoreCase) || firstToken.Equals("explorer", StringComparison.OrdinalIgnoreCase))
                {
                    var psi = new ProcessStartInfo("cmd.exe", "/c " + cmd) { UseShellExecute = false, CreateNoWindow = true, WorkingDirectory = cur };
                    Process.Start(psi); cmdBox.Clear(); return;
                }
                Process.Start(new ProcessStartInfo("cmd.exe", "/k " + cmd) { UseShellExecute = true, WorkingDirectory = cur });
                cmdBox.Clear();
            }
            catch (Exception ex) { Status(" CMD greska: " + ex.Message); }
        }

        // ===================== Tastatura =====================
        private static void EnableDoubleBuffer(Control c) { try { typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(c, true, null); } catch { } }
        private TextBoxBase GetFocusedTextBox()
        {
            if (cmdBox != null && cmdBox.Focused) return cmdBox;
            if (L.PathBox.Focused) return L.PathBox; if (R.PathBox.Focused) return R.PathBox;
            Control c = ActiveControl; while (c is ContainerControl) { var next = ((ContainerControl)c).ActiveControl; if (next == null || next == c) break; c = next; } return c as TextBoxBase;
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var tb = GetFocusedTextBox();
            bool typing = tb != null || ActiveControl is ComboBox;

            // globalne precice - rade i dok je fokus u komandnoj liniji
            switch (keyData)
            {
                case Keys.F3: RunCompareTool(); return true;
                case Keys.F4: OpenInNotepad(); return true;
                case Keys.Shift | Keys.F4: CreateNewTextFile(); return true;
                case Keys.F5: StartTransfer(false); return true;
                case Keys.F6: StartTransfer(true); return true;
                case Keys.Shift | Keys.F6: RenameSelected(); return true;
                case Keys.F7: CreateFolder(); return true;
                case Keys.Alt | Keys.F7: OpenSearchDialog(); return true;
                case Keys.F8: CopyPathToClipboard(); return true;
                case Keys.F1: cmdBox.Focus(); cmdBox.SelectAll(); return true;
                case Keys.Control | Keys.Tab: NextTab(); return true;
                case Keys.Control | Keys.T: NewTab(); return true;
                case Keys.Control | Keys.W: CloseTab(); return true;
                case Keys.Control | Keys.R: RefreshPanel(Active, true); return true;
                case Keys.Control | Keys.H: hiddenToggle.Checked = !hiddenToggle.Checked; return true;
            }

            if (typing)
            {
                if (keyData == Keys.Escape)
                {
                    if (tb == L.PathBox || tb == R.PathBox) { var p = tb == L.PathBox ? L : R; p.PathBox.Text = p.Current; p.List.Focus(); return true; }
                    if (tb == cmdBox) { Active.List.Focus(); return true; }
                }
                if (tb != null && keyData == (Keys.Control | Keys.A)) { tb.SelectAll(); return true; }
                return base.ProcessCmdKey(ref msg, keyData);
            }

            switch (keyData)
            {
                case Keys.Tab: SwitchPanel(); return true;
                case Keys.Space: ToggleMark(); return true;
                case Keys.Insert: ToggleMark(); return true;
                case Keys.Escape:
                    if (Active.FilterText.Length > 0) ClearFilter(Active);
                    else { Active.Marked.Clear(); Active.List.Invalidate(); UpdateStatus(); }
                    return true;
                case Keys.Back:
                    if (Active.FilterText.Length > 0) { if (Active.FilterText.Length > 1) ApplyFilter(Active, Active.FilterText.Substring(0, Active.FilterText.Length - 1)); else ClearFilter(Active); }
                    else GoUp(Active);
                    return true;
                case Keys.Control | Keys.PageUp: GoUp(Active); return true;
                case Keys.Delete: DeleteSelected(false); return true;
                case Keys.Shift | Keys.Delete: DeleteSelected(true); return true;
                case Keys.Control | Keys.Left: SyncPanel(true); return true;
                case Keys.Control | Keys.Right: SyncPanel(false); return true;
                case Keys.Control | Keys.C: ClipboardCopyFiles(); return true;
                case Keys.Control | Keys.X: ClipboardCutFiles(); return true;
                case Keys.Control | Keys.V: ClipboardPasteFiles(); return true;
                case Keys.Control | Keys.A: MarkAll(); return true;
                case Keys.Multiply: InvertMarks(); return true;
            }

            Keys code = keyData & Keys.KeyCode;
            bool alt = (keyData & Keys.Alt) == Keys.Alt, ctrl = (keyData & Keys.Control) == Keys.Control;
            if (alt && !ctrl)
            {
                if (code >= Keys.A && code <= Keys.Z) { FilterAddChar((char)('a' + (code - Keys.A))); return true; }
                if (code >= Keys.D0 && code <= Keys.D9) { FilterAddChar((char)('0' + (code - Keys.D0))); return true; }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }

        protected override void WndProc(ref Message m)
        {
            base.WndProc(ref m);
            // Osvezi samo kada se korisnik vrati iz DRUGE aplikacije - ne posle zatvaranja sopstvenih dijaloga (to je pravilo blinkanje)
            if (m.Msg == WM_ACTIVATEAPP && m.WParam != IntPtr.Zero && IsHandleCreated)
            {
                try { BeginInvoke(new Action(() => { if (!opRunning) { RefreshPanel(L, false); RefreshPanel(R, false); } })); } catch { }
            }
        }

        private void SyncPanel(bool leftGetsRight)
        {
            if (leftGetsRight) LoadPanel(L, R.Current, null, true);
            else LoadPanel(R, L.Current, null, true);
        }
        private void SwitchPanel() { SetActive(Other); Active.List.Focus(); }
        private void SetActive(PanelState p)
        {
            if (active != p) { active = p; L.List.Invalidate(); R.List.Invalidate(); }
            UpdateStatus();
        }
        private PanelState PanelOf(ListView lv) { return lv == L.List ? L : R; }

        private void ChangeFont()
        {
            float size; if (!float.TryParse(fontCombo.Text, out size) || size < 8) size = 14f;
            // NAPOMENA: stari fontovi se namerno NE Dispose-uju. Control.Font poredi fontove po vrednosti,
            // pa kontrola moze da zadrzi stari objekat - ugasen font = "Parameter is not valid" pri kreiranju prozora.
            if (Math.Abs(listFont.Size - size) > 0.01f)
            {
                listFont = new Font("Consolas", size, FontStyle.Regular);
                listFontBold = new Font("Consolas", size, FontStyle.Bold);
            }
            if (uiFont == null) uiFont = new Font("Consolas", 9.5f, FontStyle.Regular);
            foreach (var p in new[] { L, R }) { p.List.Font = listFont; p.PathBox.Font = uiFont; p.Drive.Font = uiFont; }
            statusLabel.Font = uiFont; toolBar.Font = uiFont;
            cmdBox.Font = listFont; cmdSaveBtn.Font = uiFont; cmdPanel.Height = Math.Max(26, cmdBox.PreferredHeight);
            ResizeColumns(L.List); ResizeColumns(R.List); L.List.Invalidate(); R.List.Invalidate();
        }
        private static void ResizeColumns(ListView lv)
        {
            if (lv == null || lv.Columns.Count < 4) return; if (lv.ClientSize.Width < 50) return;
            int w = lv.ClientSize.Width - 4;
            lv.Columns[0].Width = (int)(w * 0.50); lv.Columns[1].Width = (int)(w * 0.15);
            lv.Columns[2].Width = (int)(w * 0.22); lv.Columns[3].Width = Math.Max(20, w - lv.Columns[0].Width - lv.Columns[1].Width - lv.Columns[2].Width);
        }

        // ===================== Lista fajlova =====================
        private ListView CreateFileList(PanelState p)
        {
            var lv = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false, OwnerDraw = true, BackColor = ColorBg, ForeColor = ColorFg, Font = listFont, BorderStyle = BorderStyle.None };
            EnableDoubleBuffer(lv);
            foreach (var n in ColumnNames) lv.Columns.Add(n, 100);
            lv.Resize += (s, e) => ResizeColumns(lv);
            lv.ColumnClick += (s, e) => SortBy(p, e.Column);
            lv.DrawColumnHeader += (s, e) =>
            {
                using (var hb = new SolidBrush(ColorHeaderBg)) e.Graphics.FillRectangle(hb, e.Bounds);
                var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.SingleLine | (e.ColumnIndex == 1 ? TextFormatFlags.Right : TextFormatFlags.Left);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, listFont, e.Bounds, ColorHeaderFg, flags);
            };
            lv.DrawItem += (s, e) => { e.DrawDefault = false; };
            lv.DrawSubItem += (s, e) =>
            {
                var fi = e.Item as FileItem;
                if (fi == null) { e.DrawDefault = true; return; }
                bool isActivePanel = p == Active;
                bool cursor = fi.Selected;
                bool marked = !fi.IsUp && p.Marked.Contains(fi.FullPath);
                bool filtered = p.FilterText.Length > 0;
                Color back = filtered ? ColorFilterBg : ColorBg;
                if (cursor) back = isActivePanel ? (filtered ? ColorFilterSelBg : ColorSelBg) : ColorSelInactiveBg;
                Color fore = marked ? ColorMarkedFg : (cursor && isActivePanel ? ColorSelFg : (fi.IsDir ? ColorDirFg : ColorFg));
                using (var b = new SolidBrush(back)) e.Graphics.FillRectangle(b, e.Bounds);
                var rect = e.Bounds;
                if (e.ColumnIndex == 0)
                {
                    Image icon = GetIconFor(fi.IsDir ? "" : fi.FileName, fi.IsDir);
                    if (icon != null) { int iy = rect.Top + (rect.Height - 16) / 2; e.Graphics.DrawImage(icon, rect.Left + 2, iy, 16, 16); }
                    rect = new Rectangle(rect.Left + 22, rect.Top, Math.Max(0, rect.Width - 22), rect.Height);
                }
                var flags = TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis | TextFormatFlags.NoPrefix | TextFormatFlags.SingleLine;
                flags |= e.ColumnIndex == 1 ? TextFormatFlags.Right : TextFormatFlags.Left;
                TextRenderer.DrawText(e.Graphics, e.SubItem.Text, marked ? listFontBold : listFont, rect, fore, flags);
            };
            lv.MouseClick += (s, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                var item = lv.GetItemAt(e.X, e.Y) as FileItem;
                if (item == null) return;
                item.Selected = true; item.Focused = true; SetActive(p); lv.Focus();
                if (!item.IsUp && !string.IsNullOrEmpty(item.FullPath)) ShowExplorerContextMenu(item.FullPath);
            };
            lv.DoubleClick += (s, e) => EnterFolder();
            lv.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { EnterFolder(); e.SuppressKeyPress = true; } };
            lv.Enter += (s, e) => SetActive(p);
            lv.SelectedIndexChanged += (s, e) => { if (p == Active) UpdateStatus(); };
            return lv;
        }

        private void UpdateHeaders(PanelState p)
        {
            for (int i = 0; i < ColumnNames.Length && i < p.List.Columns.Count; i++)
            {
                string t = ColumnNames[i] + (i == p.SortCol ? (p.SortOrder == SortOrder.Ascending ? " \u25B2" : " \u25BC") : "");
                if (p.List.Columns[i].Text != t) p.List.Columns[i].Text = t;
            }
        }

        private void SortItems(PanelState p) { p.AllItems.Sort(new FileItemComparer(p.SortCol, p.SortOrder)); }

        private void SortBy(PanelState p, int col)
        {
            if (p.SortCol == col) p.SortOrder = p.SortOrder == SortOrder.Ascending ? SortOrder.Descending : SortOrder.Ascending;
            else { p.SortCol = col; p.SortOrder = SortOrder.Ascending; }
            string sel = SelectedPath(p);
            SortItems(p); UpdateHeaders(p);
            RebuildView(p, sel, 0, null);
            SetActive(p); p.List.Focus();
        }

        private static FileItem SelectedItem(PanelState p)
        {
            try { return p.List.SelectedItems.Count > 0 ? p.List.SelectedItems[0] as FileItem : null; } catch { return null; }
        }
        private static string SelectedPath(PanelState p) { var fi = SelectedItem(p); return fi == null ? null : fi.FullPath; }

        private void ShowExplorerContextMenu(string path)
        {
            if (useRealShellMenu)
            {
                try
                {
                    dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                    dynamic folder = shell.NameSpace(Path.GetDirectoryName(path)); dynamic item = folder.ParseName(Path.GetFileName(path));
                    var menu = new ContextMenuStrip();
                    foreach (var v in item.Verbs()) { string vName = ((string)v.Name).Replace("&", "").Trim(); if (string.IsNullOrWhiteSpace(vName)) continue; dynamic verb = v; menu.Items.Add(vName, null, (s, e) => { try { verb.DoIt(); } catch { } }); }
                    if (menu.Items.Count == 0) menu.Items.Add("(nema shell verbova)"); menu.Items.Add(new ToolStripSeparator());
                    menu.Items.Add("Svojstva", null, (s, e) => RecycleBin.ShowProperties(path, Handle));
                    menu.Closed += (s, e) => BeginInvoke(new Action(() => menu.Dispose()));
                    menu.Show(Cursor.Position); return;
                }
                catch (Exception ex) { MessageBox.Show(this, "Shell meni nije uspeo: " + ex.Message); }
            }
            var menu2 = new ContextMenuStrip();
            menu2.Items.Add("Otvori", null, (s, e) => { try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show(this, "Ne mogu da otvorim: " + ex.Message); } });
            if (File.Exists(path)) menu2.Items.Add("Otvori u Notepadu (F4)", null, (s, e) => OpenInNotepad());
            menu2.Items.Add("Kopiraj putanju (F8)", null, (s, e) => CopyPathToClipboard()); menu2.Items.Add(new ToolStripSeparator());
            menu2.Items.Add("Iseci (Ctrl+X)", null, (s, e) => { try { SetClipboardFiles(new List<string> { path }, true); Status(" Clipboard CUT: " + path); } catch (Exception ex) { MessageBox.Show(this, "Clipboard greska: " + ex.Message); } });
            menu2.Items.Add("Kopiraj (Ctrl+C)", null, (s, e) => { try { SetClipboardFiles(new List<string> { path }, false); Status(" Clipboard COPY: " + path); } catch (Exception ex) { MessageBox.Show(this, "Clipboard greska: " + ex.Message); } });
            menu2.Items.Add("Nalepi (Ctrl+V)", null, (s, e) => ClipboardPasteFiles());
            menu2.Items.Add(new ToolStripSeparator());
            menu2.Items.Add("Preimenuj (Shift+F6)", null, (s, e) => RenameSelected());
            menu2.Items.Add("Posalji u Recycle Bin (Del)", null, (s, e) => DeleteSelected(false));
            menu2.Items.Add("Trajno obrisi (Shift+Del)", null, (s, e) => DeleteSelected(true)); menu2.Items.Add(new ToolStripSeparator());
            menu2.Items.Add("Svojstva", null, (s, e) => RecycleBin.ShowProperties(path, Handle));
            menu2.Items.Add("Otvori u Exploreru", null, (s, e) => { string arg = File.Exists(path) ? "/select,\"" + path + "\"" : "\"" + path + "\""; try { Process.Start("explorer.exe", arg); } catch { } });
            menu2.Closed += (s, e) => BeginInvoke(new Action(() => menu2.Dispose()));
            menu2.Show(Cursor.Position);
        }

        // ===================== Obelezavanje =====================
        private void ToggleMark()
        {
            var p = Active; var item = SelectedItem(p); if (item == null) return;
            if (!item.IsUp)
            {
                if (!p.Marked.Remove(item.FullPath)) p.Marked.Add(item.FullPath);
            }
            int idx = item.Index; p.List.RedrawItems(idx, idx, false);
            if (idx + 1 < p.List.Items.Count) { var next = p.List.Items[idx + 1]; next.Selected = true; next.Focused = true; p.List.EnsureVisible(idx + 1); }
            UpdateStatus();
        }
        private void MarkAll()
        {
            var p = Active; bool allMarked = true;
            foreach (FileItem fi in p.List.Items) if (!fi.IsUp && !p.Marked.Contains(fi.FullPath)) { allMarked = false; break; }
            foreach (FileItem fi in p.List.Items) { if (fi.IsUp) continue; if (allMarked) p.Marked.Remove(fi.FullPath); else p.Marked.Add(fi.FullPath); }
            p.List.Invalidate(); UpdateStatus();
        }
        private void InvertMarks()
        {
            var p = Active;
            foreach (FileItem fi in p.List.Items) { if (fi.IsUp || fi.IsDir) continue; if (!p.Marked.Remove(fi.FullPath)) p.Marked.Add(fi.FullPath); }
            p.List.Invalidate(); UpdateStatus();
        }

        // ===================== Diskovi =====================
        private static string[] GetDriveRoots()
        {
            var res = new List<string>();
            try
            {
                foreach (var d in DriveInfo.GetDrives())
                {
                    try
                    {
                        // IsReady na nedostupnom mreznom disku koci UI po 20+ sekundi - mrezne ne proveravamo
                        if (d.DriveType == DriveType.Network) { res.Add(d.Name); continue; }
                        if (d.DriveType == DriveType.NoRootDirectory) continue;
                        if (d.IsReady) res.Add(d.Name);
                    }
                    catch { }
                }
            }
            catch { }
            if (res.Count == 0) res.Add(@"C:\");
            return res.ToArray();
        }
        private void LoadDrives(PanelState p)
        {
            var roots = GetDriveRoots();
            var cur = p.Drive.Items.Cast<object>().Select(o => o.ToString()).ToArray();
            if (!cur.SequenceEqual(roots, StringComparer.OrdinalIgnoreCase))
            {
                p.Drive.BeginUpdate();
                p.Drive.Items.Clear(); p.Drive.Items.AddRange(roots);
                p.Drive.EndUpdate();
            }
            UpdateDriveCombo(p);
        }
        private void UpdateDriveCombo(PanelState p)
        {
            if (p.Current == null) return;
            string root = null; try { root = Path.GetPathRoot(p.Current); } catch { }
            int idx = -1;
            for (int i = 0; i < p.Drive.Items.Count; i++) if (string.Equals(p.Drive.Items[i].ToString(), root, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
            if (p.Drive.SelectedIndex != idx) p.Drive.SelectedIndex = idx;
        }

        // ===================== Ucitavanje foldera =====================
        private static bool SameFolder(string a, string b)
        {
            if (a == null || b == null) return false;
            return string.Equals(a.TrimEnd('\\', '/'), b.TrimEnd('\\', '/'), StringComparison.OrdinalIgnoreCase);
        }
        private static bool PathEq(string a, string b)
        {
            if (a == null || b == null) return false;
            try { a = Path.GetFullPath(a); b = Path.GetFullPath(b); } catch { }
            return SameFolder(a, b);
        }

        private static DirSnapshot ReadDirectory(string path, bool showHidden)
        {
            var snap = new DirSnapshot();
            try
            {
                var di = new DirectoryInfo(path);
                var hc = new HashCode();
                foreach (var fsi in di.EnumerateFileSystemInfos("*", EnumAll))
                {
                    try
                    {
                        var a = fsi.Attributes;
                        if (!showHidden && (a & FileAttributes.Hidden) != 0) continue;
                        bool isDir = (a & FileAttributes.Directory) != 0;
                        var e = new EntryData { Name = fsi.Name, FullPath = fsi.FullName, IsDir = isDir, Attr = a, Modified = fsi.LastWriteTime };
                        if (!isDir) { try { e.Size = ((FileInfo)fsi).Length; } catch { } }
                        snap.Entries.Add(e);
                        hc.Add(e.Name, StringComparer.OrdinalIgnoreCase); hc.Add(e.Size); hc.Add(e.Modified.Ticks); hc.Add((int)a);
                    }
                    catch { }
                }
                snap.Signature = ((long)snap.Entries.Count << 32) ^ (uint)hc.ToHashCode();
            }
            catch (UnauthorizedAccessException) { snap.Error = "Pristup odbijen (nema prava)"; }
            catch (DirectoryNotFoundException) { snap.Error = "Folder ne postoji"; snap.NotFound = true; }
            catch (IOException ex) { snap.Error = ex.Message; if (ex.HResult == unchecked((int)0x80070003) || ex.HResult == unchecked((int)0x80070002)) snap.NotFound = true; }
            catch (Exception ex) { snap.Error = ex.Message; }
            return snap;
        }

        private static List<FileItem> BuildItems(string path, DirSnapshot snap)
        {
            var list = new List<FileItem>(snap.Entries.Count + 1);
            string parent = null; try { var gp = Directory.GetParent(path); parent = gp != null ? gp.FullName : null; } catch { }
            if (parent != null)
            {
                var up = new FileItem("[..]") { FullPath = parent, FileName = "..", IsDir = true, IsUp = true, Tag = parent };
                up.SubItems.Add(""); up.SubItems.Add(""); up.SubItems.Add("");
                list.Add(up);
            }
            foreach (var e in snap.Entries)
            {
                var it = new FileItem(e.IsDir ? "[" + e.Name + "]" : e.Name)
                {
                    FullPath = e.FullPath, FileName = e.Name, IsDir = e.IsDir, Size = e.Size, Modified = e.Modified,
                    Ext = e.IsDir ? "" : Path.GetExtension(e.Name), Tag = e.FullPath
                };
                it.SubItems.Add(e.IsDir ? "<DIR>" : FormatSize(e.Size));
                it.SubItems.Add(e.Modified.ToString("dd.MM.yyyy HH:mm"));
                it.SubItems.Add(it.Ext);
                list.Add(it);
            }
            return list;
        }

        /// <summary>
        /// Ucitava folder u panel. Citanje diska je na pozadinskoj niti sa timeout-om (mreza/USB ne mogu da zamrznu program).
        /// keepView=true: osvezavanje istog foldera - cuva kursor, skrol, filter; ako se nista nije promenilo - ne dira listu (nema blinkanja).
        /// Vraca false ako folder ne moze da se otvori - panel tada OSTAJE gde je bio.
        /// </summary>
        private bool LoadPanel(PanelState p, string path, string selectPath, bool focus, bool keepView = false, bool force = false)
        {
            lastLoadNotFound = false;
            if (string.IsNullOrWhiteSpace(path)) return false;
            path = path.Trim();
            if (path.Length == 2 && path[1] == ':') path += "\\";
            try { path = Path.GetFullPath(path); } catch { Status(" Neispravna putanja: " + path); return false; }

            var task = Task.Run(() => ReadDirectory(path, showHidden));
            bool finished;
            using (new CursorScope())
                finished = task.Wait(LoadTimeoutMs);
            if (!finished) { Status(" Folder ne odgovara (mreza/disk?): " + path); return false; }
            var snap = task.Result;
            if (snap.Error != null)
            {
                lastLoadNotFound = snap.NotFound;
                Status(" Ne mogu da otvorim: " + path + " - " + snap.Error);
                return false;
            }

            bool sameFolder = SameFolder(p.Current, path);
            if (keepView && sameFolder && !force && snap.Signature == p.Signature)
            {
                p.List.Invalidate(); // samo obelezavanja su mozda promenjena
                return true;
            }

            string prevSel = SelectedPath(p);
            int prevIdx = 0; try { prevIdx = p.List.SelectedIndices.Count > 0 ? p.List.SelectedIndices[0] : 0; } catch { }
            string prevTop = null; try { var ti = p.List.TopItem as FileItem; if (ti != null) prevTop = ti.FullPath; } catch { }

            if (!sameFolder)
            {
                p.Marked.Clear();
                p.FilterText = "";
            }
            else
            {
                var names = new HashSet<string>(snap.Entries.Select(x => x.FullPath), StringComparer.OrdinalIgnoreCase);
                p.Marked.RemoveWhere(m => SameFolder(SafeDirName(m), path) && !names.Contains(m));
            }

            p.Current = path;
            p.Signature = snap.Signature;
            p.AllItems = BuildItems(path, snap);
            SortItems(p);

            string want = selectPath ?? (sameFolder ? prevSel : null);
            RebuildView(p, want, sameFolder ? prevIdx : 0, sameFolder ? prevTop : null);

            if (!(keepView && p.PathBox.Focused) && p.PathBox.Text != path) p.PathBox.Text = path;
            UpdateDriveCombo(p);
            if (p.TabIdx >= 0 && p.TabIdx < p.Tabs.Count) p.Tabs[p.TabIdx].Path = path;
            if (!sameFolder || p.Watcher == null) { SetupWatcher(p); RenderTabs(p); }
            ResizeColumns(p.List);
            if (focus) { SetActive(p); p.List.Focus(); }
            if (p == Active) UpdateStatus();
            return true;
        }

        private static string SafeDirName(string p) { try { return Path.GetDirectoryName(p); } catch { return null; } }

        private sealed class CursorScope : IDisposable
        {
            private readonly Cursor old;
            public CursorScope() { old = Cursor.Current; Cursor.Current = Cursors.WaitCursor; }
            public void Dispose() { Cursor.Current = old; }
        }

        private void RebuildView(PanelState p, string selectPath, int fallbackIdx, string topPath)
        {
            var lv = p.List;
            FileItem[] arr;
            if (p.FilterText.Length > 0) arr = p.AllItems.Where(i => i.IsUp || i.FileName.StartsWith(p.FilterText, StringComparison.OrdinalIgnoreCase)).ToArray();
            else arr = p.AllItems.ToArray();

            int idx = -1;
            if (selectPath != null) for (int i = 0; i < arr.Length; i++) if (!arr[i].IsUp && string.Equals(arr[i].FullPath, selectPath, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
            if (idx < 0 && selectPath != null) for (int i = 0; i < arr.Length; i++) if (string.Equals(arr[i].FullPath, selectPath, StringComparison.OrdinalIgnoreCase)) { idx = i; break; }
            if (idx < 0 && arr.Length > 0) idx = Math.Min(Math.Max(fallbackIdx, 0), arr.Length - 1);

            lv.BeginUpdate();
            try
            {
                lv.ListViewItemSorter = null;
                lv.Items.Clear();
                lv.Items.AddRange(arr);
                Color bg = p.FilterText.Length > 0 ? ColorFilterBg : ColorBg;
                if (lv.BackColor != bg) lv.BackColor = bg;
                if (idx >= 0) { var it = lv.Items[idx]; it.Selected = true; it.Focused = true; }
            }
            finally { lv.EndUpdate(); }

            if (topPath != null)
            {
                foreach (FileItem it in lv.Items) if (string.Equals(it.FullPath, topPath, StringComparison.OrdinalIgnoreCase)) { try { lv.TopItem = it; } catch { } break; }
            }
            if (idx >= 0) { try { lv.EnsureVisible(idx); } catch { } }
        }

        private static string ResolveExistingFolder(string p)
        {
            try
            {
                while (!string.IsNullOrEmpty(p) && !Directory.Exists(p))
                {
                    string parent = Path.GetDirectoryName(p.TrimEnd('\\'));
                    if (string.IsNullOrEmpty(parent) || parent == p) return null; p = parent;
                }
            }
            catch { return null; }
            return p;
        }

        private void RefreshPanel(PanelState p, bool force)
        {
            if (p.Current == null) return;
            if (LoadPanel(p, p.Current, null, false, true, force)) return;
            if (lastLoadNotFound)
            {
                string old = p.Current;
                var up = ResolveExistingFolder(old);
                if (up != null && !SameFolder(up, old)) LoadPanel(p, up, null, false);
            }
        }

        // ===================== Automatsko osvezavanje (FileSystemWatcher) =====================
        private void DisposeWatcher(PanelState p)
        {
            var w = p.Watcher; p.Watcher = null;
            if (w != null) { try { w.EnableRaisingEvents = false; w.Dispose(); } catch { } }
        }
        private void SetupWatcher(PanelState p)
        {
            DisposeWatcher(p);
            try
            {
                var w = new FileSystemWatcher(p.Current)
                {
                    IncludeSubdirectories = false,
                    NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | NotifyFilters.Size | NotifyFilters.LastWrite | NotifyFilters.Attributes,
                    InternalBufferSize = 64 * 1024
                };
                FileSystemEventHandler h = (s, e) => OnWatcherEvent(p, w);
                w.Created += h; w.Deleted += h; w.Changed += h;
                w.Renamed += (s, e) => OnWatcherEvent(p, w);
                w.Error += (s, e) => OnWatcherEvent(p, w);
                w.EnableRaisingEvents = true;
                p.Watcher = w;
            }
            catch { /* npr. neki mrezni share ne podrzava - onda radi samo osvezavanje pri aktivaciji / Ctrl+R */ }
        }
        private void OnWatcherEvent(PanelState p, FileSystemWatcher w)
        {
            // stize sa pozadinske niti
            try
            {
                if (!IsHandleCreated || IsDisposed) return;
                BeginInvoke(new Action(() =>
                {
                    if (!ReferenceEquals(p.Watcher, w)) return; // stari folder
                    p.RefreshPending = true;
                    if (!refreshTimer.Enabled) refreshTimer.Start();
                }));
            }
            catch { }
        }
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            refreshTimer.Stop();
            if (opRunning) { refreshTimer.Start(); return; }
            foreach (var p in new[] { L, R })
                if (p.RefreshPending) { p.RefreshPending = false; RefreshPanel(p, false); }
        }

        private void SetShowHidden(bool on)
        {
            showHidden = on;
            hiddenToggle.Text = on ? "Skriveni ON" : "Skriveni OFF";
            hiddenToggle.BackColor = on ? ColorShellOnBg : ColorToolbarBg;
            RefreshPanel(L, true); RefreshPanel(R, true);
        }

        // ===================== Status =====================
        private void Status(string text) { if (statusLabel == null) return; statusLabel.Text = text; lastStatus = text; }
        private void UpdateStatus()
        {
            if (statusLabel == null || L == null || R == null || Active.List == null) return;
            var p = Active;
            int files = 0, dirs = 0, mFiles = 0, mDirs = 0; long bytes = 0, mBytes = 0;
            foreach (var it in p.AllItems)
            {
                if (it.IsUp) continue;
                bool m = p.Marked.Contains(it.FullPath);
                if (it.IsDir) { dirs++; if (m) mDirs++; }
                else { files++; bytes += it.Size; if (m) { mFiles++; mBytes += it.Size; } }
            }
            int other = p.Marked.Count - mFiles - mDirs;
            string sel = SelectedPath(p) ?? "";
            string txt = " Obelezeno: " + mFiles + " fajl / " + mDirs + " dir (" + FormatSize(mBytes) + ")" + (other > 0 ? " +" + other + " iz drugih foldera" : "") +
                         "  od " + files + " fajl / " + dirs + " dir (" + FormatSize(bytes) + ")" +
                         (p.FilterText.Length > 0 ? "  | FILTER: " + p.FilterText + " (Esc)" : "") + "  | " + sel;
            if (txt != lastStatus) { statusLabel.Text = txt; lastStatus = txt; }
        }

        // ===================== Navigacija =====================
        private void GoUp(PanelState p)
        {
            if (p.Current == null) return;
            DirectoryInfo parent = null; try { parent = Directory.GetParent(p.Current); } catch { }
            if (parent != null) LoadPanel(p, parent.FullName, p.Current, true);
        }
        private void EnterFolder()
        {
            var p = Active; var fi = SelectedItem(p); if (fi == null) return;
            if (fi.IsUp) { GoUp(p); return; }
            if (fi.IsDir) { LoadPanel(p, fi.FullPath, null, true); return; }
            try { Process.Start(new ProcessStartInfo(fi.FullPath) { UseShellExecute = true, WorkingDirectory = p.Current }); }
            catch (Exception ex) { Status(" Ne mogu da otvorim: " + ex.Message); }
        }

        // ===================== Filter (Alt+slovo) =====================
        private void ClearFilter(PanelState p)
        {
            if (p.FilterText.Length == 0) return;
            string sel = SelectedPath(p);
            p.FilterText = "";
            RebuildView(p, sel, 0, null);
            UpdateStatus();
        }
        private bool ApplyFilter(PanelState p, string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) { ClearFilter(p); return true; }
            bool any = p.AllItems.Any(i => !i.IsUp && i.FileName.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            if (!any) return false; // kao TC: ne dodaje slovo koje nista ne pogadja
            p.FilterText = prefix;
            RebuildView(p, null, 0, null);
            var lv = p.List;
            if (lv.Items.Count > 1 && ((FileItem)lv.Items[0]).IsUp) { lv.Items[1].Selected = true; lv.Items[1].Focused = true; lv.EnsureVisible(1); }
            SetActive(p); lv.Focus();
            return true;
        }
        private void FilterAddChar(char c)
        {
            var p = Active;
            if (Other.FilterText.Length > 0) ClearFilter(Other);
            if (!ApplyFilter(p, p.FilterText + char.ToLowerInvariant(c))) System.Media.SystemSounds.Beep.Play();
        }

        private static bool IsSubPath(string basePath, string subPath)
        {
            try
            {
                string baseFull = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string subFull = Path.GetFullPath(subPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return subFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase) && !string.Equals(baseFull, subFull, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static void SafeClipboard(string text)
        {
            try { Clipboard.SetText(text); }
            catch { try { Thread.Sleep(50); Clipboard.SetText(text); } catch { } }
        }
        private void CopyPathToClipboard()
        {
            var p = Active; var fi = SelectedItem(p); if (fi == null) return;
            string path = fi.IsUp ? p.Current : fi.FullPath;
            if (p.Marked.Count > 0) path = string.Join(Environment.NewLine, p.Marked);
            if (string.IsNullOrEmpty(path)) return;
            SafeClipboard(path); Status(" Kopirano: " + path.Replace(Environment.NewLine, " ; "));
        }
        private void OpenInNotepad()
        {
            var p = Active;
            var fi = SelectedItem(p);
            try
            {
                if (fi != null && !fi.IsUp && !fi.IsDir)
                    // kursor je na fajlu - otvori taj fajl
                    Process.Start(new ProcessStartInfo("notepad.exe", "\"" + fi.FullPath + "\"") { UseShellExecute = true, WorkingDirectory = p.Current });
                else
                    // nema fajla (folder, [..], prazan panel) - otvori prazan Notepad
                    Process.Start(new ProcessStartInfo("notepad.exe") { UseShellExecute = true, WorkingDirectory = p.Current });
            }
            catch (Exception ex) { Status(" Notepad greska: " + ex.Message); }
        }

        private void CreateNewTextFile()
        {
            var p = Active;
            if (string.IsNullOrEmpty(p.Current)) return;
            string name = ShowNewFileDialog(p.Current);
            if (string.IsNullOrWhiteSpace(name)) return;

            if (name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            { MessageBox.Show(this, "Ime sadrzi nedozvoljene karaktere: \\ / : * ? \" < > |", "Shift+F4"); return; }
            if (Path.GetFileNameWithoutExtension(name).Length == 0)
            { MessageBox.Show(this, "Upisi ime fajla ispred ekstenzije.", "Shift+F4"); return; }

            string full = Path.Combine(p.Current, name);
            if (Directory.Exists(full))
            { MessageBox.Show(this, "Vec postoji FOLDER sa tim imenom:\n" + full, "Shift+F4"); return; }

            try
            {
                bool existed = File.Exists(full);
                if (!existed)
                    using (new FileStream(full, FileMode.CreateNew, FileAccess.Write)) { } // prazan fajl

                LoadPanel(p, p.Current, full, true, true, true);           // osvezi i stavi kursor na novi fajl
                if (SameFolder(Other.Current, p.Current)) RefreshPanel(Other, true);
                Status(existed ? " Fajl vec postoji - otvaram postojeci: " + full : " Napravljen: " + full);

                Process.Start(new ProcessStartInfo("notepad.exe", "\"" + full + "\"") { UseShellExecute = true, WorkingDirectory = p.Current });
            }
            catch (Exception ex) { MessageBox.Show(this, "Ne mogu da napravim fajl:\n" + FriendlyError(ex), "Shift+F4"); }
        }

        private string ShowNewFileDialog(string folder)
        {
            using (var f = new Form { Width = 520, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Shift+F4 Novi fajl", StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false })
            {
                var lbl = new Label { Left = 10, Top = 10, Width = 480, AutoEllipsis = true, Text = "Ime novog fajla u: " + folder };
                var txt = new TextBox { Left = 10, Top = 35, Width = 480, Text = ".txt" };
                var ok = new Button { Text = "Ok", Left = 310, Width = 85, Top = 70, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Otkazi", Left = 405, Width = 85, Top = 70, DialogResult = DialogResult.Cancel };
                f.Controls.AddRange(new Control[] { lbl, txt, ok, cancel });
                f.AcceptButton = ok; f.CancelButton = cancel;
                DarkTheme.Apply(f);
                // kursor na pocetak, ispred ".txt" - kucas ime, a ekstenziju mozes da izmenis
                f.Shown += (s, e) => { txt.Focus(); txt.SelectionStart = 0; txt.SelectionLength = 0; };
                return f.ShowDialog(this) == DialogResult.OK ? txt.Text.Trim() : "";
            }
        }

        private static List<string> GetOperationItems(PanelState p)
        {
            if (p.Marked.Count > 0) return p.Marked.ToList();
            var fi = SelectedItem(p);
            if (fi == null || fi.IsUp || string.IsNullOrEmpty(fi.FullPath)) return new List<string>();
            return new List<string> { fi.FullPath };
        }

        // =====================================================================================
        // ===================== ENGINE ZA KOPIRANJE / PREMESTANJE / BRISANJE ===================
        // =====================================================================================

        internal static string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024L * 1024) return (bytes / 1024) + " KB";
            if (bytes < 1024L * 1024 * 1024) return (bytes / (1024.0 * 1024)).ToString("0.0") + " MB";
            return (bytes / (1024.0 * 1024 * 1024)).ToString("0.00") + " GB";
        }

        private static bool ExistsAny(string p) { try { return File.Exists(p) || Directory.Exists(p); } catch { return false; } }

        private static bool SameVolume(string a, string b)
        {
            try { return string.Equals(Path.GetPathRoot(Path.GetFullPath(a)), Path.GetPathRoot(Path.GetFullPath(b)), StringComparison.OrdinalIgnoreCase); }
            catch { return false; }
        }

        private static long GetFreeBytes(string dir)
        {
            try
            {
                string d = ResolveExistingFolder(dir) ?? dir;
                if (!d.EndsWith("\\")) d += "\\";
                ulong free, total, totalFree;
                if (GetDiskFreeSpaceEx(d, out free, out total, out totalFree)) return free > long.MaxValue ? long.MaxValue : (long)free;
            }
            catch { }
            return -1;
        }

        private static void RemoveReadOnly(string path)
        {
            try { var a = File.GetAttributes(path); if ((a & FileAttributes.ReadOnly) != 0) File.SetAttributes(path, a & ~FileAttributes.ReadOnly); } catch { }
        }

        private static string FriendlyError(Exception ex)
        {
            string m = ex.Message;
            if (ex is UnauthorizedAccessException) m += "\n(Nema prava pristupa. Za sistemske foldere pokreni MiniTC kao Administrator.)";
            else if (ex is PathTooLongException) m += "\n(Putanja je preduga.)";
            else if (ex is IOException && (ex.HResult & 0xFFFF) == 32) m += "\n(Fajl koristi drugi program.)";
            else if (ex is IOException && ((ex.HResult & 0xFFFF) == 112 || (ex.HResult & 0xFFFF) == 39)) m += "\n(Nema dovoljno mesta na disku.)";
            return m;
        }

        private static void AddError(OpContext ctx, string msg)
        {
            lock (ctx.Errors) ctx.Errors.Add(msg);
            Interlocked.Increment(ref ctx.Failed);
        }

        private static T AskOnUi<T>(OpContext ctx, Func<IWin32Window, T> f)
        {
            var form = ctx.Form;
            return (T)form.Invoke(new Func<T>(() => f(form)));
        }

        /// <summary>
        /// Izvrsava akciju. Kod greske pita korisnika (na UI niti, vlasnik je progres prozor - nema skrivenih MessageBox-ova):
        /// Ponovi / Preskoci / Preskoci sve greske / Otkazi. Vraca true ako je uspelo.
        /// </summary>
        private static bool TryOp(OpContext ctx, string path, Action action)
        {
            while (true)
            {
                if (ctx.Cancel) throw new OperationCanceledException();
                try { action(); return true; }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    string msg = path + "\n" + FriendlyError(ex);
                    if (ctx.SkipAllErrors) { AddError(ctx, msg); return false; }
                    int r = AskOnUi(ctx, owner => ShowChoiceDialog(owner, "Greska", msg, new[] { "Ponovi", "Preskoci", "Preskoci sve greske", "Otkazi" }, 1, 3));
                    if (r == 0) continue;
                    if (r == 3) { ctx.Cancel = true; throw new OperationCanceledException(); }
                    if (r == 2) ctx.SkipAllErrors = true;
                    AddError(ctx, msg);
                    return false;
                }
            }
        }

        private static void Scan(OpContext ctx, string path)
        {
            if (ctx.Cancel) throw new OperationCanceledException();
            try
            {
                var attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == 0)
                {
                    Interlocked.Increment(ref ctx.TotalFiles);
                    try { Interlocked.Add(ref ctx.TotalBytes, new FileInfo(path).Length); } catch { }
                    return;
                }
                if ((attr & FileAttributes.ReparsePoint) != 0) return; // junction/symlink se ne prati
                foreach (var fsi in new DirectoryInfo(path).EnumerateFileSystemInfos("*", EnumScan))
                {
                    if (ctx.Cancel) throw new OperationCanceledException();
                    if ((fsi.Attributes & FileAttributes.Directory) != 0)
                    {
                        if ((fsi.Attributes & FileAttributes.ReparsePoint) == 0) Scan(ctx, fsi.FullName);
                    }
                    else
                    {
                        Interlocked.Increment(ref ctx.TotalFiles);
                        try { Interlocked.Add(ref ctx.TotalBytes, ((FileInfo)fsi).Length); } catch { }
                    }
                }
            }
            catch (OperationCanceledException) { throw; }
            catch { }
        }

        private static ConflictResult ResolveConflict(OpContext ctx, string src, string dst)
        {
            while (true)
            {
                switch (ctx.Conflict)
                {
                    case ConflictMode.OverwriteAll: return new ConflictResult { Path = dst, Overwrite = true };
                    case ConflictMode.SkipAll: return null;
                    case ConflictMode.OverwriteOlder: return IsSrcNewer(src, dst) ? new ConflictResult { Path = dst, Overwrite = true } : null;
                }
                string info = BuildConflictText(src, dst);
                int r = AskOnUi(ctx, owner => ShowChoiceDialog(owner, "Fajl vec postoji", info,
                    new[] { "Pregazi", "Pregazi sve", "Pregazi sve starije", "Preskoci", "Preskoci sve", "Preimenuj", "Otkazi" }, 3, 6));
                switch (r)
                {
                    case 0: return new ConflictResult { Path = dst, Overwrite = true };
                    case 1: ctx.Conflict = ConflictMode.OverwriteAll; continue;
                    case 2: ctx.Conflict = ConflictMode.OverwriteOlder; continue;
                    case 3: return null;
                    case 4: ctx.Conflict = ConflictMode.SkipAll; return null;
                    case 5:
                        string newName = AskOnUi(ctx, owner => Prompt.Show("Novo ime za odrediste:", "Preimenuj", Path.GetFileName(dst)).Trim());
                        if (string.IsNullOrEmpty(newName)) continue;
                        if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) continue;
                        string nd = Path.Combine(Path.GetDirectoryName(dst), newName);
                        if (!ExistsAny(nd)) return new ConflictResult { Path = nd, Overwrite = false };
                        dst = nd; continue;
                    default:
                        ctx.Cancel = true; throw new OperationCanceledException();
                }
            }
        }

        private static bool IsSrcNewer(string src, string dst)
        {
            try { return File.GetLastWriteTimeUtc(src) > File.GetLastWriteTimeUtc(dst).AddSeconds(2); } catch { return false; }
        }

        private static string BuildConflictText(string src, string dst)
        {
            string Desc(string p)
            {
                try { var fi = new FileInfo(p); return FormatSize(fi.Length) + " (" + fi.Length.ToString("N0") + " B)   " + fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm:ss"); }
                catch { return "?"; }
            }
            string newer = IsSrcNewer(src, dst) ? "  <-- NOVIJI" : (IsSrcNewer(dst, src) ? "  (stariji)" : "");
            return "Fajl vec postoji na odredistu.\n\nPOSTOJECI:\n  " + dst + "\n  " + Desc(dst) + "\n\nNOVI (izvor):\n  " + src + "\n  " + Desc(src) + newer;
        }

        /// <summary>Kopira u privremeni fajl pa preimenuje - ako pukne na pola, stari ciljni fajl ostaje netaknut. Cuva datume i atribute.</summary>
        private static void CopyFileCore(OpContext ctx, string src, string dst, bool overwrite)
        {
            string dir = Path.GetDirectoryName(dst);
            string tmp = Path.Combine(dir, "~mtc" + Guid.NewGuid().ToString("N").Substring(0, 10) + ".tmp");
            long copied = 0; bool tmpCreated = false; bool done = false;
            try
            {
                var si = new FileInfo(src);
                using (var sIn = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete, 1 << 16, FileOptions.SequentialScan))
                {
                    using (var sOut = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 1 << 16))
                    {
                        tmpCreated = true;
                        long len = 0; try { len = sIn.Length; } catch { }
                        if (len > 0) { try { sOut.SetLength(len); } catch { } }
                        var buf = new byte[1 << 20]; int r;
                        while ((r = sIn.Read(buf, 0, buf.Length)) > 0)
                        {
                            if (ctx.Cancel) throw new OperationCanceledException();
                            sOut.Write(buf, 0, r);
                            copied += r;
                            Interlocked.Add(ref ctx.DoneBytes, r);
                        }
                        if (sOut.Length != copied) sOut.SetLength(copied);
                    }
                }
                try { File.SetCreationTimeUtc(tmp, si.CreationTimeUtc); File.SetLastWriteTimeUtc(tmp, si.LastWriteTimeUtc); } catch { }
                if (overwrite && File.Exists(dst)) RemoveReadOnly(dst);
                File.Move(tmp, dst, overwrite);
                tmpCreated = false;
                try
                {
                    var keep = si.Attributes & (FileAttributes.ReadOnly | FileAttributes.Hidden | FileAttributes.System | FileAttributes.Archive);
                    if (keep != 0) File.SetAttributes(dst, keep);
                }
                catch { }
                done = true;
            }
            finally
            {
                if (!done)
                {
                    Interlocked.Add(ref ctx.DoneBytes, -copied);
                    if (tmpCreated) { try { File.Delete(tmp); } catch { } }
                }
            }
        }

        private static bool TransferFile(OpContext ctx, string src, string dst)
        {
            if (ctx.Cancel) throw new OperationCanceledException();
            ctx.CurrentName = src;
            long len = 0; try { len = new FileInfo(src).Length; } catch { }
            bool overwrite = false;

            if (Directory.Exists(dst))
            {
                AddError(ctx, dst + "\nNa odredistu postoji FOLDER sa istim imenom - fajl preskocen.");
                Interlocked.Add(ref ctx.DoneBytes, len); return false;
            }
            if (File.Exists(dst))
            {
                if (PathEq(src, dst)) { AddError(ctx, src + "\nIzvor i odrediste su isti fajl."); Interlocked.Add(ref ctx.DoneBytes, len); return false; }
                var res = ResolveConflict(ctx, src, dst);
                if (res == null) { Interlocked.Increment(ref ctx.Skipped); Interlocked.Add(ref ctx.DoneBytes, len); return false; }
                dst = res.Path; overwrite = res.Overwrite;
            }

            string finalDst = dst; bool ow = overwrite;
            bool sameVol = ctx.Move && SameVolume(src, finalDst);
            bool ok = TryOp(ctx, src, () =>
            {
                if (sameVol)
                {
                    if (ow && File.Exists(finalDst)) RemoveReadOnly(finalDst);
                    File.Move(src, finalDst, ow);
                }
                else CopyFileCore(ctx, src, finalDst, ow);
            });
            if (!ok) { Interlocked.Add(ref ctx.DoneBytes, len); return false; }
            if (sameVol) Interlocked.Add(ref ctx.DoneBytes, len);

            if (ctx.Move && !sameVol)
            {
                // izvor se brise TEK posto je kopija uspesno zavrsena
                if (!TryOp(ctx, src, () => { RemoveReadOnly(src); File.Delete(src); })) return false;
            }
            Interlocked.Increment(ref ctx.DoneFiles);
            return true;
        }

        private static bool TransferDir(OpContext ctx, string src, string dst, FileAttributes attr)
        {
            if (ctx.Cancel) throw new OperationCanceledException();
            ctx.CurrentName = src;

            if ((attr & FileAttributes.ReparsePoint) != 0)
            {
                if (ctx.Move && SameVolume(src, dst) && !ExistsAny(dst)) return TryOp(ctx, src, () => Directory.Move(src, dst));
                AddError(ctx, src + "\nLink/junction se ne kopira (preskocen da ne bi kopirao ciljni sadrzaj ili usao u petlju).");
                return false;
            }
            if (File.Exists(dst)) { AddError(ctx, dst + "\nNa odredistu postoji FAJL sa imenom ovog foldera - preskocen."); return false; }

            // brzo premestanje na istom disku (rename) kada cilj ne postoji
            if (ctx.Move && SameVolume(src, dst) && !Directory.Exists(dst))
            {
                try { Directory.Move(src, dst); return true; }
                catch { /* npr. fajl zakljucan - ide se fajl po fajl da bi korisnik dobio konkretnu gresku */ }
            }

            if (!TryOp(ctx, dst, () => Directory.CreateDirectory(dst))) return false;

            FileSystemInfo[] kids = null;
            if (!TryOp(ctx, src, () => kids = new DirectoryInfo(src).GetFileSystemInfos("*", EnumAll))) return false;

            bool all = true;
            foreach (var k in kids)
            {
                if (ctx.Cancel) throw new OperationCanceledException();
                string d = Path.Combine(dst, k.Name);
                bool ok = (k.Attributes & FileAttributes.Directory) != 0
                    ? TransferDir(ctx, k.FullName, d, k.Attributes)
                    : TransferFile(ctx, k.FullName, d);
                if (!ok) all = false;
            }

            if (!ctx.Move)
            {
                try
                {
                    Directory.SetLastWriteTimeUtc(dst, Directory.GetLastWriteTimeUtc(src));
                    var keep = attr & (FileAttributes.Hidden | FileAttributes.System | FileAttributes.ReadOnly);
                    if (keep != 0) File.SetAttributes(dst, FileAttributes.Directory | keep);
                }
                catch { }
            }

            if (ctx.Move && all)
            {
                if (!TryOp(ctx, src, () => { RemoveReadOnly(src); Directory.Delete(src, false); })) return false;
            }
            return all;
        }

        private static bool TransferItem(OpContext ctx, string src, string dst)
        {
            if (PathEq(src, dst)) { AddError(ctx, src + "\nIzvor i odrediste su isti."); return false; }
            FileAttributes attr = 0;
            if (!TryOp(ctx, src, () => attr = File.GetAttributes(src))) return false;
            if ((attr & FileAttributes.Directory) != 0)
            {
                if (IsSubPath(src, dst)) { AddError(ctx, src + "\nNe mogu kopirati/premestiti folder u sopstveni podfolder."); return false; }
                return TransferDir(ctx, src, dst, attr);
            }
            return TransferFile(ctx, src, dst);
        }

        private static void DoTransfer(OpContext ctx, List<KeyValuePair<string, string>> pairs)
        {
            bool allSameVolMove = ctx.Move && pairs.All(pp => SameVolume(pp.Key, pp.Value));
            if (allSameVolMove) ctx.Indeterminate = true;
            else
            {
                ctx.Phase = "Racunam velicinu: ";
                foreach (var pp in pairs) { ctx.CurrentName = pp.Key; Scan(ctx, pp.Key); }
                long need = Interlocked.Read(ref ctx.TotalBytes);
                long free = GetFreeBytes(ctx.DestDir);
                if (free >= 0 && need > free)
                {
                    int r = AskOnUi(ctx, owner => ShowChoiceDialog(owner, "Provera mesta",
                        "Nema dovoljno mesta na odredistu!\n\nPotrebno: " + FormatSize(need) + "\nSlobodno: " + FormatSize(free) + "\n\nNastaviti ipak?",
                        new[] { "Nastavi", "Otkazi" }, 1, 1));
                    if (r != 0) { ctx.Cancel = true; throw new OperationCanceledException(); }
                }
            }
            if (!TryOp(ctx, ctx.DestDir, () => Directory.CreateDirectory(ctx.DestDir))) return;

            ctx.Phase = ctx.Move ? "Premestam: " : "Kopiram: ";
            foreach (var pp in pairs)
            {
                if (ctx.Cancel) throw new OperationCanceledException();
                if (TransferItem(ctx, pp.Key, pp.Value)) lock (ctx.Completed) ctx.Completed.Add(pp.Key);
            }
        }

        private static bool DeleteTree(OpContext ctx, string path)
        {
            if (ctx.Cancel) throw new OperationCanceledException();
            ctx.CurrentName = path;
            FileAttributes attr = 0;
            if (!TryOp(ctx, path, () => attr = File.GetAttributes(path))) return false;
            if ((attr & FileAttributes.Directory) != 0)
            {
                // junction/symlink: brise se samo LINK, nikad sadrzaj na koji pokazuje
                if ((attr & FileAttributes.ReparsePoint) != 0) return TryOp(ctx, path, () => { RemoveReadOnly(path); Directory.Delete(path, false); });
                FileSystemInfo[] kids = null;
                if (!TryOp(ctx, path, () => kids = new DirectoryInfo(path).GetFileSystemInfos("*", EnumAll))) return false;
                bool all = true;
                foreach (var k in kids) if (!DeleteTree(ctx, k.FullName)) all = false;
                if (!all) return false;
                return TryOp(ctx, path, () => { RemoveReadOnly(path); Directory.Delete(path, false); });
            }
            bool ok = TryOp(ctx, path, () => { RemoveReadOnly(path); File.Delete(path); });
            if (ok) Interlocked.Increment(ref ctx.DoneFiles);
            return ok;
        }

        private void RunWithProgress(string title, OpContext ctx, Action<OpContext> work)
        {
            opRunning = true;
            try
            {
                using (var prog = new CopyProgressForm(title, ctx))
                {
                    ctx.Form = prog;
                    prog.Shown += async (s, e) =>
                    {
                        try { await Task.Run(() => work(ctx)); }
                        catch (OperationCanceledException) { ctx.Canceled = true; }
                        catch (Exception ex) { AddError(ctx, FriendlyError(ex)); }
                        if (ctx.Cancel) ctx.Canceled = true;
                        prog.Done = true;
                        prog.Close();
                    };
                    prog.ShowDialog(this);
                }
            }
            finally { opRunning = false; }
        }

        private void SelectInView(PanelState p, string path)
        {
            if (string.IsNullOrEmpty(path)) return;
            foreach (FileItem it in p.List.Items)
                if (string.Equals(it.FullPath, path, StringComparison.OrdinalIgnoreCase)) { it.Selected = true; it.Focused = true; try { p.List.EnsureVisible(it.Index); } catch { } break; }
        }

        private void FinishOp(OpContext ctx, string title, PanelState source, string selectInActive = null)
        {
            if (source != null) foreach (var s in ctx.Completed) source.Marked.Remove(s);
            RefreshPanel(L, false); RefreshPanel(R, false);
            L.List.Invalidate(); R.List.Invalidate();
            SelectInView(Active, selectInActive);
            UpdateStatus();
            string msg = " " + title + ": " + ctx.Completed.Count + " stavki (" + ctx.DoneFiles + " fajlova)";
            if (ctx.Skipped > 0) msg += ", preskoceno " + ctx.Skipped;
            if (ctx.Failed > 0) msg += ", GRESAKA " + ctx.Failed;
            if (ctx.Canceled) msg += "  (OTKAZANO)";
            Status(msg);
            if (ctx.Errors.Count > 0)
            {
                var list = ctx.Errors.Take(300).ToList();
                string text = string.Join("\n\n", list) + (ctx.Errors.Count > 300 ? "\n\n... i jos " + (ctx.Errors.Count - 300) : "");
                ShowChoiceDialog(this, title + " - problemi (" + ctx.Errors.Count + ")", text, new[] { "OK" }, 0, 0);
            }
        }

        /// <summary>Univerzalni tamni dijalog sa dugmicima. Vraca indeks dugmeta (ili cancelIndex na X / Esc).</summary>
        private static int ShowChoiceDialog(IWin32Window owner, string title, string text, string[] buttons, int acceptIndex, int cancelIndex)
        {
            using (var f = new Form { Text = title, FormBorderStyle = FormBorderStyle.FixedDialog, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false })
            {
                f.ClientSize = new Size(720, 300);
                var tb = new TextBox
                {
                    Multiline = true, ReadOnly = true, ScrollBars = ScrollBars.Vertical, Left = 10, Top = 10, Width = 700, Height = 220,
                    Text = (text ?? "").Replace("\r\n", "\n").Replace("\n", "\r\n"), BackColor = Color.FromArgb(25, 25, 25), ForeColor = Color.White, TabStop = false
                };
                var flow = new FlowLayoutPanel { Left = 10, Top = 238, Width = 700, Height = 58, FlowDirection = FlowDirection.LeftToRight, WrapContents = true };
                int result = cancelIndex;
                Button acceptBtn = null, cancelBtn = null;
                for (int i = 0; i < buttons.Length; i++)
                {
                    int idx = i;
                    var b = new Button { Text = buttons[i], AutoSize = true, Height = 28, Margin = new Padding(3), BackColor = i == acceptIndex ? DarkTheme.Blue : DarkTheme.Grey, ForeColor = Color.White };
                    b.Click += (s, e) => { result = idx; f.DialogResult = DialogResult.OK; };
                    flow.Controls.Add(b);
                    if (i == acceptIndex) acceptBtn = b;
                    if (i == cancelIndex) cancelBtn = b;
                }
                f.Controls.Add(tb); f.Controls.Add(flow);
                if (acceptBtn != null) f.AcceptButton = acceptBtn;
                if (cancelBtn != null) f.CancelButton = cancelBtn;
                DarkTheme.Apply(f);
                f.Shown += (s, e) => { if (acceptBtn != null) acceptBtn.Focus(); tb.SelectionLength = 0; };
                var dr = owner != null ? f.ShowDialog(owner) : f.ShowDialog();
                return dr == DialogResult.OK ? result : cancelIndex;
            }
        }

        // ===================== F5 / F6 =====================
        private bool ShowCopyDialog(bool move, List<string> items, string destDir, out string target, out ConflictMode mode)
        {
            target = null; mode = ConflictMode.Ask;
            bool single = items.Count == 1;
            string name = Path.GetFileName(items[0].TrimEnd('\\'));
            string defTarget = single ? Path.Combine(destDir, name) : destDir;
            string op = move ? "Premesti" : "Kopiraj";
            using (var f = new Form { Width = 660, Height = 470, Text = (move ? "F6 " : "F5 ") + op, StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false })
            {
                var lbl = new Label { Left = 10, Top = 10, Width = 620, Height = 20, Text = op + " " + (single ? "\"" + name + "\"" : items.Count + " stavki") + " u (moze se promeniti ime/putanja):" };
                var txt = new TextBox { Left = 10, Top = 34, Width = 620, Text = defTarget };
                var lblMode = new Label { Left = 10, Top = 70, Width = 175, Height = 20, Text = "Ako fajl vec postoji:" };
                var cmb = new ComboBox { Left = 190, Top = 66, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };
                cmb.Items.AddRange(new object[] { "Pitaj za svaki", "Pregazi sve", "Preskoci sve", "Pregazi samo starije" });
                cmb.SelectedIndex = 0;
                var lb = new ListBox { Left = 10, Top = 100, Width = 620, Height = 270, HorizontalScrollbar = true };
                int existCount = 0;
                var lines = new List<string>();
                for (int i = 0; i < items.Count; i++)
                {
                    string it = items[i];
                    bool ex = i < 3000 && ExistsAny(Path.Combine(destDir, Path.GetFileName(it.TrimEnd('\\'))));
                    if (ex) existCount++;
                    lines.Add((ex ? "[POSTOJI] " : "") + it);
                }
                lb.Items.AddRange(lines.ToArray());
                var lblWarn = new Label { Left = 10, Top = 380, Width = 380, Height = 34, ForeColor = Color.FromArgb(255, 170, 80), Text = existCount > 0 ? "PAZNJA: " + existCount + " vec postoji na odredistu." : "" };
                var ok = new Button { Text = "OK (Enter)", Left = 420, Top = 380, Width = 100, Height = 30, DialogResult = DialogResult.OK };
                var cancel = new Button { Text = "Otkazi", Left = 530, Top = 380, Width = 100, Height = 30, DialogResult = DialogResult.Cancel };
                f.Controls.AddRange(new Control[] { lbl, txt, lblMode, cmb, lb, lblWarn, ok, cancel });
                f.AcceptButton = ok; f.CancelButton = cancel;
                DarkTheme.Apply(f); DarkTheme.DarkScroll(lb);
                f.Shown += (s, e) =>
                {
                    txt.Focus();
                    if (single && defTarget.EndsWith(name, StringComparison.Ordinal))
                    {
                        int start = defTarget.Length - name.Length;
                        int len = Directory.Exists(items[0]) ? name.Length : Path.GetFileNameWithoutExtension(name).Length;
                        txt.Select(start, len);
                    }
                    else txt.SelectionStart = txt.TextLength;
                };
                if (f.ShowDialog(this) != DialogResult.OK) return false;
                target = txt.Text.Trim().Trim('"');
                mode = (ConflictMode)cmb.SelectedIndex;
                return target.Length > 0;
            }
        }

        private void StartTransfer(bool move)
        {
            if (opRunning) return;
            var p = Active; var other = Other;
            var items = GetOperationItems(p);
            if (items.Count == 0) return;
            string destDir = other.Current;

            string target; ConflictMode mode;
            if (!ShowCopyDialog(move, items, destDir, out target, out mode)) return;

            var pairs = new List<KeyValuePair<string, string>>();
            string finalDir;
            try
            {
                if (!Path.IsPathRooted(target)) target = Path.Combine(p.Current, target); // relativno = u odnosu na izvorni folder (kao TC)
                bool intoDir = target.EndsWith("\\") || target.EndsWith("/");
                target = Path.GetFullPath(target);
                if (items.Count == 1)
                {
                    string src = items[0];
                    string defTarget = Path.Combine(destDir, Path.GetFileName(src.TrimEnd('\\')));
                    if (!intoDir && !PathEq(target, defTarget) && !PathEq(target, src) && Directory.Exists(target)) intoDir = true;
                    string dst = intoDir ? Path.Combine(target, Path.GetFileName(src.TrimEnd('\\'))) : target.TrimEnd('\\');
                    pairs.Add(new KeyValuePair<string, string>(src, dst));
                    finalDir = Path.GetDirectoryName(dst);
                }
                else
                {
                    foreach (var s in items) pairs.Add(new KeyValuePair<string, string>(s, Path.Combine(target, Path.GetFileName(s.TrimEnd('\\')))));
                    finalDir = target;
                }
            }
            catch (Exception ex) { MessageBox.Show(this, "Neispravna odredisna putanja:\n" + ex.Message, move ? "F6" : "F5"); return; }

            if (pairs.Any(pp => PathEq(pp.Key, pp.Value)))
            {
                MessageBox.Show(this, "Izvor i odrediste su isti.\nZa kopiju u istom folderu upisi drugo ime u F5 dijalogu.", move ? "F6" : "F5", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            var bad = pairs.FirstOrDefault(pp => Directory.Exists(pp.Key) && IsSubPath(pp.Key, pp.Value));
            if (bad.Key != null) { MessageBox.Show(this, "Ne mogu " + (move ? "premestiti" : "kopirati") + " folder u sopstveni podfolder:\n" + bad.Key, move ? "F6" : "F5"); return; }

            var ctx = new OpContext { Move = move, Conflict = mode, DestDir = finalDir };
            RunWithProgress(move ? "Premestanje" : "Kopiranje", ctx, c => DoTransfer(c, pairs));
            FinishOp(ctx, move ? "Premesteno" : "Kopirano", p);
            p.List.Focus();
        }

        // ===================== F7 / Shift+F6 =====================
        private void CreateFolder()
        {
            var p = Active; string input = Prompt.Show("Ime foldera (moze i a\\b\\c):", "F7 Novi folder", "Novi folder");
            input = (input ?? "").Trim(); if (input.Length == 0) return;
            try
            {
                if (input.IndexOfAny(Path.GetInvalidPathChars()) >= 0 || input.IndexOfAny(new[] { '*', '?', '"', '<', '>', '|' }) >= 0) { MessageBox.Show(this, "Ime sadrzi nedozvoljene karaktere."); return; }
                string full = Path.GetFullPath(Path.Combine(p.Current, input));
                Directory.CreateDirectory(full);
                string first = full;
                string rel = Path.GetRelativePath(p.Current, full);
                int sep = rel.IndexOf('\\');
                if (sep > 0) first = Path.Combine(p.Current, rel.Substring(0, sep));
                LoadPanel(p, p.Current, first, true, true, true);
            }
            catch (Exception ex) { MessageBox.Show(this, "Greska pri kreiranju foldera:\n" + FriendlyError(ex)); }
        }
        private void RenameSelected()
        {
            var p = Active; var fi = SelectedItem(p); if (fi == null || fi.IsUp) return;
            string oldPath = fi.FullPath; string oldName = Path.GetFileName(oldPath); string dir = Path.GetDirectoryName(oldPath);
            string newName = ShowRenameDialog(oldName); if (string.IsNullOrWhiteSpace(newName) || newName == oldName) return;
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { MessageBox.Show(this, "Ime sadrzi nedozvoljene karaktere: \\ / : * ? \" < > |"); return; }
            string newPath = Path.Combine(dir, newName);
            bool caseOnly = string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase);
            if (!caseOnly && ExistsAny(newPath)) { MessageBox.Show(this, "Vec postoji fajl/folder sa tim imenom:\n" + newPath); return; }
            try
            {
                bool isDir = Directory.Exists(oldPath);
                if (caseOnly)
                {
                    // promena samo velicine slova (a.txt -> A.txt) mora preko privremenog imena
                    string tmp = Path.Combine(dir, "~mtcren" + Guid.NewGuid().ToString("N").Substring(0, 8));
                    if (isDir) { Directory.Move(oldPath, tmp); Directory.Move(tmp, newPath); }
                    else { File.Move(oldPath, tmp); File.Move(tmp, newPath); }
                }
                else if (isDir) Directory.Move(oldPath, newPath);
                else File.Move(oldPath, newPath);
                if (p.Marked.Remove(oldPath)) p.Marked.Add(newPath);
                LoadPanel(p, p.Current, newPath, true, true, true);
                if (SameFolder(Other.Current, p.Current)) RefreshPanel(Other, true);
            }
            catch (Exception ex) { MessageBox.Show(this, "Greska pri preimenovanju:\n" + FriendlyError(ex)); }
        }
        private string ShowRenameDialog(string oldName)
        {
            using (Form prompt = new Form() { Width = 520, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Shift+F6 Preimenuj", StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false })
            {
                Label textLabel = new Label() { Left = 10, Top = 10, Width = 490, Text = "Novo ime:" };
                TextBox textBox = new TextBox() { Left = 10, Top = 35, Width = 480, Text = oldName };
                Button confirmation = new Button() { Text = "Ok", Left = 310, Width = 85, Top = 70, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Otkazi", Left = 405, Width = 85, Top = 70, DialogResult = DialogResult.Cancel };
                prompt.Controls.AddRange(new Control[] { textBox, confirmation, cancel, textLabel }); prompt.AcceptButton = confirmation; prompt.CancelButton = cancel;
                DarkTheme.Apply(prompt);
                prompt.Shown += (s, e) =>
                {
                    textBox.Focus();
                    int dot = oldName.LastIndexOf('.');
                    if (dot > 0) textBox.Select(0, dot); else textBox.SelectAll();
                };
                return prompt.ShowDialog(this) == DialogResult.OK ? textBox.Text.Trim() : "";
            }
        }

        // ===================== Compare =====================
        private void SetupCompareTool()
        {
            using (Form f = new Form() { Width = 600, Height = 220, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Podesi Compare Tool (pamti se u MiniTC.txt)", StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false })
            {
                Label lblPath = new Label() { Left = 10, Top = 15, Width = 560, Text = "Putanja do BeyondCompare.exe / WinMergeU.exe / vsdiffmerge.exe itd:" };
                TextBox txtPath = new TextBox() { Left = 10, Top = 35, Width = 460, Text = compareToolPath };
                Button btnBrowse = new Button() { Left = 480, Top = 33, Width = 80, Text = "Browse..." };
                Label lblParams = new Label() { Left = 10, Top = 65, Width = 560, Text = "Parametri (ostavi prazno za default file1 file2 ili npr. %1 %2):" };
                TextBox txtParams = new TextBox() { Left = 10, Top = 85, Width = 560, Text = compareToolParams };
                Label lblHint = new Label() { Left = 10, Top = 110, Width = 560, Height = 30, ForeColor = ColorHintFg, Text = @"Primer Beyond: C:\Program Files\Beyond Compare 4\BCompare.exe | Params: prazno" };
                Button ok = new Button() { Text = "Sacuvaj", Left = 380, Top = 145, Width = 90, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Otkazi", Left = 480, Top = 145, Width = 90, DialogResult = DialogResult.Cancel };
                btnBrowse.Click += (s, e) => { using (OpenFileDialog dlg = new OpenFileDialog() { Filter = "EXE files|*.exe|All files|*.*", Title = "Izaberi compare tool" }) { if (!string.IsNullOrWhiteSpace(txtPath.Text) && File.Exists(txtPath.Text)) { try { dlg.InitialDirectory = Path.GetDirectoryName(txtPath.Text); } catch { } } if (dlg.ShowDialog(f) == DialogResult.OK) txtPath.Text = dlg.FileName; } };
                f.Controls.AddRange(new Control[] { lblPath, txtPath, btnBrowse, lblParams, txtParams, lblHint, ok, cancel }); f.AcceptButton = ok; f.CancelButton = cancel; DarkTheme.Apply(f);
                lblHint.ForeColor = ColorHintFg;
                if (f.ShowDialog(this) == DialogResult.OK) { compareToolPath = txtPath.Text.Trim().Trim('"'); compareToolParams = txtParams.Text.Trim(); SaveTabsToTxt(); Status(" Compare tool sacuvan: " + compareToolPath); }
            }
        }
        private void RunCompareTool()
        {
            if (string.IsNullOrWhiteSpace(compareToolPath) || !File.Exists(compareToolPath)) { var res = MessageBox.Show(this, "Compare tool nije podesen ili ne postoji:\n" + compareToolPath + "\n\nDa otvoris podesavanja?", "F3 Compare", MessageBoxButtons.YesNo); if (res == DialogResult.Yes) SetupCompareTool(); return; }
            string fileA = null, fileB = null;
            if (L.Marked.Count == 1 && R.Marked.Count == 1) { fileA = L.Marked.First(); fileB = R.Marked.First(); }
            else if (L.Marked.Count == 2 && R.Marked.Count == 0) { var arr = L.Marked.ToArray(); fileA = arr[0]; fileB = arr[1]; }
            else if (R.Marked.Count == 2 && L.Marked.Count == 0) { var arr = R.Marked.ToArray(); fileA = arr[0]; fileB = arr[1]; }
            else
            {
                var a = SelectedItem(L); var b = SelectedItem(R);
                if (a != null && b != null && !a.IsDir && !b.IsDir && !PathEq(a.FullPath, b.FullPath)) { fileA = a.FullPath; fileB = b.FullPath; }
            }
            if (fileA == null || fileB == null) { MessageBox.Show(this, "Obelezi 2 fajla (SPACE) - jedan u levom i jedan u desnom panelu, ili 2 fajla u istom panelu, pa pritisni F3.\nIli samo postavi kursor na fajl u oba panela.\n\nTrenutno: levo marked=" + L.Marked.Count + " desno marked=" + R.Marked.Count, "F3 Compare"); return; }
            if (!File.Exists(fileA) || !File.Exists(fileB)) { MessageBox.Show(this, "Compare radi samo sa fajlovima, ne sa folderima.\n" + fileA + "\n" + fileB); return; }
            try
            {
                string args;
                if (string.IsNullOrWhiteSpace(compareToolParams)) args = "\"" + fileA + "\" \"" + fileB + "\"";
                else if (compareToolParams.Contains("%1") || compareToolParams.Contains("%2")) args = compareToolParams.Replace("%1", "\"" + fileA + "\"").Replace("%2", "\"" + fileB + "\"");
                else args = compareToolParams + " \"" + fileA + "\" \"" + fileB + "\"";
                Process.Start(new ProcessStartInfo(compareToolPath, args) { UseShellExecute = true }); Status(" Compare: " + Path.GetFileName(fileA) + " vs " + Path.GetFileName(fileB));
            }
            catch (Exception ex) { MessageBox.Show(this, "Greska pri pokretanju compare tool-a: " + ex.Message + "\n\nPutanja: " + compareToolPath + "\nArgs: " + compareToolParams); }
        }

        // ===================== Pretraga (Alt+F7) =====================
        private void OpenSearchDialog()
        {
            var p = Active; string root = p.Current;
            using (Form dlg = new Form() { Width = 450, Height = 200, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Alt+F7 Pretraga - u: " + root, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false })
            {
                Label lbl = new Label() { Left = 10, Top = 15, Width = 400, Text = "Trazi (npr *.cs, *form*, MiniTC):" };
                TextBox txt = new TextBox() { Left = 10, Top = 35, Width = 410, Text = "" };
                CheckBox chkSub = new CheckBox() { Left = 10, Top = 65, Width = 180, Text = "Pretrazi podfoldere", Checked = true };
                CheckBox chkCase = new CheckBox() { Left = 200, Top = 65, Width = 180, Text = "Case sensitive", Checked = false };
                Button ok = new Button() { Text = "Trazi", Left = 240, Top = 100, Width = 80, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Otkazi", Left = 330, Top = 100, Width = 90, DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { lbl, txt, chkSub, chkCase, ok, cancel }); dlg.AcceptButton = ok; dlg.CancelButton = cancel; DarkTheme.Apply(dlg); dlg.Shown += (s, e) => txt.Focus();
                if (dlg.ShowDialog(this) == DialogResult.OK) { string pattern = txt.Text.Trim(); if (string.IsNullOrWhiteSpace(pattern)) return; ShowSearchResults(p, pattern, root, chkSub.Checked, chkCase.Checked); }
            }
        }
        private static bool IsSearchMatch(string fileName, string pattern, bool caseSensitive)
        {
            try
            {
                StringComparison comp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (pattern.Contains("*") || pattern.Contains("?")) { string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$"; var opts = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase; return Regex.IsMatch(fileName, regexPattern, opts); }
                return fileName.IndexOf(pattern, comp) >= 0;
            }
            catch { return false; }
        }
        private void ShowSearchResults(PanelState tp, string pattern, string root, bool includeSub, bool caseSensitive)
        {
            Form resForm = new Form() { Width = 900, Height = 600, Text = "Rezultati pretrage za '" + pattern + "' u " + root + " - DblClick/Enter locira fajl", StartPosition = FormStartPosition.CenterParent, WindowState = FormWindowState.Maximized, MinimizeBox = false, MaximizeBox = true, ShowInTaskbar = false };
            ListView lv = new ListView() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false, BackColor = ColorBg, ForeColor = ColorFg, Font = listFont };
            lv.Columns.Add("Ime", 250); lv.Columns.Add("Putanja", 400); lv.Columns.Add("Velicina", 80); lv.Columns.Add("Datum", 130);
            Action fitColumns = () => { int w = lv.ClientSize.Width - SystemInformation.VerticalScrollBarWidth; if (w <= 0) return; int c0 = (int)(w * 0.25); int c1 = (int)(w * 0.50); int c2 = (int)(w * 0.10); lv.Columns[0].Width = c0; lv.Columns[1].Width = c1; lv.Columns[2].Width = c2; lv.Columns[3].Width = w - c0 - c1 - c2; };
            lv.ClientSizeChanged += (s, e) => fitColumns(); lv.SizeChanged += (s, e) => fitColumns();
            Label lblStatus = new Label() { Dock = DockStyle.Top, Height = 24, BackColor = ColorToolbarBg, ForeColor = ColorStatusFg, Text = " Pretraga u toku: " + root + " za '" + pattern + "'..." };
            Label lblCount = new Label() { Dock = DockStyle.Bottom, Height = 24, BackColor = ColorToolbarBg, ForeColor = ColorStatusFg, Text = " Pronadjeno: 0" };
            Panel bottomPanel = new Panel() { Dock = DockStyle.Bottom, Height = 40, BackColor = ColorToolbarBg };
            Button btnGoto = new Button() { Text = "Idi na fajl (Enter)", Left = 10, Top = 8, Width = 170, BackColor = ColorGotoBtnBg, ForeColor = Color.White };
            Button btnOpen = new Button() { Text = "Otvori", Left = 190, Top = 8, Width = 90 };
            Button btnSelectAll = new Button() { Text = "Obelezi sve pronadjene", Left = 290, Top = 8, Width = 220 };
            Button btnClose = new Button() { Text = "Zatvori (Esc)", Left = 780, Top = 8, Width = 130, DialogResult = DialogResult.Cancel };
            bottomPanel.Controls.AddRange(new Control[] { btnGoto, btnOpen, btnSelectAll, btnClose }); resForm.CancelButton = btnClose;
            bottomPanel.Resize += (s, e) => { btnClose.Left = bottomPanel.ClientSize.Width - btnClose.Width - 10; };
            resForm.Controls.Add(lv); resForm.Controls.Add(bottomPanel); resForm.Controls.Add(lblCount); resForm.Controls.Add(lblStatus); DarkTheme.Apply(resForm); DarkTheme.DarkScroll(lv);
            var foundFiles = new List<string>(); var cts = new CancellationTokenSource(); resForm.FormClosing += (s, e) => { cts.Cancel(); };
            Action<Action> ui = (a) => { try { if (!resForm.IsDisposed && resForm.IsHandleCreated) resForm.BeginInvoke(a); } catch { } };
            Action<string> goToFile = (fullPath) =>
            {
                try
                {
                    if (File.Exists(fullPath)) { if (LoadPanel(tp, Path.GetDirectoryName(fullPath), fullPath, true)) resForm.Close(); }
                    else if (Directory.Exists(fullPath)) { if (LoadPanel(tp, fullPath, null, true)) resForm.Close(); }
                }
                catch (Exception ex) { MessageBox.Show(resForm, "Greska pri lociranju: " + ex.Message); }
            };
            lv.DoubleClick += (s, e) => { if (lv.SelectedItems.Count > 0) goToFile(lv.SelectedItems[0].Tag as string); };
            lv.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && lv.SelectedItems.Count > 0) { goToFile(lv.SelectedItems[0].Tag as string); e.SuppressKeyPress = true; } };
            btnGoto.Click += (s, e) => { if (lv.SelectedItems.Count > 0) goToFile(lv.SelectedItems[0].Tag as string); };
            btnSelectAll.Click += (s, e) =>
            {
                var files = new List<string>(); lock (foundFiles) { foreach (var f in foundFiles) if (File.Exists(f)) files.Add(f); }
                if (files.Count == 0) { MessageBox.Show(resForm, "Nema pronadjenih fajlova za obelezavanje."); return; }
                string first = files[0];
                if (!LoadPanel(tp, Path.GetDirectoryName(first), first, true)) return;
                foreach (var f in files) tp.Marked.Add(f);
                resForm.Close(); tp.List.Invalidate(); UpdateStatus(); Status(" Obelezeno fajlova iz pretrage: " + files.Count + " (F5/F6/Del rade nad svima)");
            };
            btnOpen.Click += (s, e) => { if (lv.SelectedItems.Count > 0) { string p = lv.SelectedItems[0].Tag as string; try { Process.Start(new ProcessStartInfo(p) { UseShellExecute = true }); } catch { } } };
            var token = cts.Token;
            resForm.Shown += (s0, e0) =>
            {
                Task.Run(() =>
                {
                    try
                    {
                        var stack = new Stack<string>(); stack.Push(root); int count = 0;
                        while (stack.Count > 0 && !token.IsCancellationRequested && count < 1000)
                        {
                            string currentDir = stack.Pop();
                            try
                            {
                                foreach (var fsi in new DirectoryInfo(currentDir).EnumerateFileSystemInfos("*", EnumScan))
                                {
                                    if (token.IsCancellationRequested || count >= 1000) break;
                                    try
                                    {
                                        bool isDir = (fsi.Attributes & FileAttributes.Directory) != 0;
                                        if (isDir)
                                        {
                                            if (!includeSub) { }
                                            else if ((fsi.Attributes & (FileAttributes.Hidden | FileAttributes.ReparsePoint)) == 0) stack.Push(fsi.FullName);
                                        }
                                        if (!IsSearchMatch(fsi.Name, pattern, caseSensitive)) continue;
                                        lock (foundFiles) foundFiles.Add(fsi.FullName);
                                        count++;
                                        string full = fsi.FullName, nm = fsi.Name; DateTime lw = fsi.LastWriteTime; long size = isDir ? 0 : ((FileInfo)fsi).Length; int cnt = count;
                                        ui(() => { try { var item = new ListViewItem(isDir ? "[" + nm + "]" : nm) { Tag = full }; item.SubItems.Add(Path.GetDirectoryName(full)); item.SubItems.Add(isDir ? "<DIR>" : FormatSize(size)); item.SubItems.Add(lw.ToString("dd.MM.yyyy HH:mm")); lv.Items.Add(item); lblCount.Text = " Pronadjeno: " + lv.Items.Count + (cnt >= 1000 ? " (limit 1000)" : ""); } catch { } });
                                    }
                                    catch { }
                                }
                                string cd = currentDir; int c2 = count; ui(() => { lblStatus.Text = " Pretraga: " + cd + " | nadjeno: " + c2; });
                            }
                            catch { }
                        }
                        ui(() => { int total; lock (foundFiles) { total = foundFiles.Count; } lblStatus.Text = " Pretraga zavrsena u " + root + " | ukupno: " + total + " rezultata za '" + pattern + "' (Esc za zatvaranje, Enter/DblClick za lociranje)"; lblCount.Text = " Pronadjeno: " + total + (total >= 1000 ? " (limit 1000)" : "") + " | DblClick ili Enter locira fajl u panelu"; if (lv.Items.Count > 0) { lv.Items[0].Selected = true; lv.Items[0].Focused = true; lv.Focus(); } });
                    }
                    catch (Exception ex) { string msg = ex.Message; ui(() => { lblStatus.Text = " Greska: " + msg; }); }
                });
            };
            resForm.ShowDialog(this); resForm.Dispose(); cts.Dispose();
        }

        // ===================== Clipboard (Ctrl+C / Ctrl+X / Ctrl+V) =====================
        private static void SetClipboardFiles(List<string> files, bool cut)
        {
            var sc = new System.Collections.Specialized.StringCollection(); foreach (var f in files) sc.Add(f);
            var data = new DataObject(); data.SetFileDropList(sc); data.SetData("Preferred DropEffect", new MemoryStream(new byte[] { (byte)(cut ? 2 : 5), 0, 0, 0 }));
            Clipboard.SetDataObject(data, true, 10, 100);
        }
        private void ClipboardCopyFiles()
        {
            var p = Active; var files = GetOperationItems(p); if (files.Count == 0) return;
            try { SetClipboardFiles(files, false); p.Marked.Clear(); p.List.Invalidate(); UpdateStatus(); Status(" Clipboard COPY: " + files.Count + " stavki | Ctrl+V za paste"); }
            catch (Exception ex) { MessageBox.Show(this, "Clipboard Copy greska: " + ex.Message); }
        }
        private void ClipboardCutFiles()
        {
            var p = Active; var files = GetOperationItems(p); if (files.Count == 0) return;
            try { SetClipboardFiles(files, true); p.Marked.Clear(); p.List.Invalidate(); UpdateStatus(); Status(" Clipboard CUT: " + files.Count + " stavki | Ctrl+V za premestanje"); }
            catch (Exception ex) { MessageBox.Show(this, "Clipboard Cut greska: " + ex.Message); }
        }
        private static string MakeUniqueCopyName(string dir, string name, bool isDir)
        {
            string baseName = isDir ? name : Path.GetFileNameWithoutExtension(name);
            string ext = isDir ? "" : Path.GetExtension(name);
            for (int i = 1; i < 10000; i++)
            {
                string candidate = baseName + (i == 1 ? " - Kopija" : " - Kopija (" + i + ")") + ext;
                string full = Path.Combine(dir, candidate);
                if (!ExistsAny(full)) return full;
            }
            return Path.Combine(dir, baseName + " - Kopija " + Guid.NewGuid().ToString("N").Substring(0, 6) + ext);
        }
        private void ClipboardPasteFiles()
        {
            if (opRunning) return;
            System.Collections.Specialized.StringCollection fileList = null; bool isMove = false;
            try
            {
                if (Clipboard.ContainsFileDropList())
                {
                    fileList = Clipboard.GetFileDropList();
                    try
                    {
                        var data = Clipboard.GetDataObject();
                        if (data != null && data.GetDataPresent("Preferred DropEffect"))
                        {
                            object raw = data.GetData("Preferred DropEffect");
                            byte[] bytes = raw is MemoryStream ms ? ms.ToArray() : raw as byte[];
                            if (bytes != null && bytes.Length > 0 && (bytes[0] & 2) != 0 && (bytes[0] & 1) == 0) isMove = true;
                        }
                    }
                    catch { }
                }
            }
            catch (Exception ex) { Status(" Clipboard nedostupan: " + ex.Message); return; }
            if (fileList == null || fileList.Count == 0) { Status(" Clipboard prazan - nema fajlova za paste"); return; }

            var p = Active; string destDir = p.Current;
            var pairs = new List<KeyValuePair<string, string>>();
            foreach (var src in fileList.Cast<string>())
            {
                string name = Path.GetFileName(src.TrimEnd('\\')); if (string.IsNullOrEmpty(name)) continue;
                bool isDir = Directory.Exists(src);
                if (!isDir && !File.Exists(src)) continue;
                string dst = Path.Combine(destDir, name);
                if (PathEq(src, dst))
                {
                    if (isMove) continue;                       // premestanje u isti folder - nema sta
                    dst = MakeUniqueCopyName(destDir, name, isDir); // kopija u istom folderu - kao Explorer
                }
                if (isDir && IsSubPath(src, dst)) { Status(" Preskoceno (folder u sopstveni podfolder): " + src); continue; }
                pairs.Add(new KeyValuePair<string, string>(src, dst));
            }
            if (pairs.Count == 0) { Status(" Nema sta da se nalepi (isti folder?)"); return; }

            string opName = isMove ? "Premestiti" : "Kopirati";
            if (MessageBox.Show(this, opName + " " + pairs.Count + " stavki iz clipboard-a u\n" + destDir + " ?", "Ctrl+V " + opName, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

            var ctx = new OpContext { Move = isMove, Conflict = ConflictMode.Ask, DestDir = destDir };
            RunWithProgress(isMove ? "Premestanje (Ctrl+V)" : "Kopiranje (Ctrl+V)", ctx, c => DoTransfer(c, pairs));
            if (isMove && ctx.Completed.Count == pairs.Count) { try { Clipboard.Clear(); } catch { } }
            FinishOp(ctx, isMove ? "Premesteno" : "Kopirano", null, pairs.Count == 1 ? pairs[0].Value : null);
            p.List.Focus();
        }

        // ===================== 001 Rename =====================
        private void SequentialRenameFiles()
        {
            try
            {
                var p = Active; var list = p.List; if (list.Items.Count == 0) { MessageBox.Show(this, "Panel je prazan.", "001 Rename"); return; }
                string curDir = p.Current;
                var filesInOrder = new List<string>();
                foreach (FileItem it in list.Items) { if (it.IsUp || it.IsDir) continue; filesInOrder.Add(it.FullPath); }
                if (filesInOrder.Count == 0) { MessageBox.Show(this, "Nema fajlova za preimenovanje u ovom folderu/panelu.", "001 Rename"); return; }
                string input = Prompt.Show("Unesi format pocetnog broja:\n" + "npr 000 = 3 cifre (000,001,002...)\n" + " 0001 = 4 cifre (0001,0002...)\n" + " 001 = krece od 1 sa 3 cifre\n\n" + "Broj fajlova: " + filesInOrder.Count + " u " + curDir, "Sekvencijalno preimenovanje", "000");
                if (string.IsNullOrWhiteSpace(input)) return; input = input.Trim(); int padLen; int startNum = 0; bool isAllZeros = input.All(c => c == '0');
                if (isAllZeros) { padLen = input.Length; startNum = 0; }
                else
                {
                    if (!input.All(char.IsDigit)) { MessageBox.Show(this, "Unos mora biti broj (npr 000, 0001, 001, 3).", "001 Rename"); return; }
                    int single;
                    if (input.Length == 1 && int.TryParse(input, out single) && single >= 1 && single <= 9) { padLen = single; startNum = 0; }
                    else { padLen = input.Length; int parsed; if (int.TryParse(input, out parsed)) startNum = parsed; }
                }
                if (padLen < 1) padLen = 3; if (padLen > 10) padLen = 10;
                var renamePairs = new List<Tuple<string, string>>(); var oldSet = new HashSet<string>(filesInOrder, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < filesInOrder.Count; i++) { string oldPath = filesInOrder[i]; string ext = Path.GetExtension(oldPath); int num = startNum + i; string newName = num.ToString("D" + padLen) + ext; renamePairs.Add(Tuple.Create(oldPath, Path.Combine(curDir, newName))); }
                var collisions = renamePairs.Where(t => File.Exists(t.Item2) && !oldSet.Contains(t.Item2)).ToList();
                if (collisions.Count > 0) { string msg = "Sledeci ciljni fajlovi vec postoje i nisu deo preimenovanja:\n" + string.Join("\n", collisions.Take(10).Select(c => Path.GetFileName(c.Item2))) + (collisions.Count > 10 ? "\n... i jos " + (collisions.Count - 10) : "") + "\n\nDa nastavim i preskocim postojece (Yes) ili otkazem (No)?"; if (MessageBox.Show(this, msg, "001 Rename - kolizija", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return; }
                string preview = string.Join("\n", renamePairs.Take(10).Select(t => Path.GetFileName(t.Item1) + " -> " + Path.GetFileName(t.Item2))) + (renamePairs.Count > 10 ? "\n... i jos " + (renamePairs.Count - 10) : "");
                if (MessageBox.Show(this, "Preimenovati " + renamePairs.Count + " fajlova u " + curDir + "?\n\n" + preview, "001 Rename - potvrda", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
                var tempPaths = new List<Tuple<string, string, string>>(); int ok2 = 0; var errs = new List<string>();
                opRunning = true;
                try
                {
                    foreach (var pair in renamePairs)
                    {
                        if (string.Equals(pair.Item1, pair.Item2, StringComparison.Ordinal)) { ok2++; continue; }
                        string tempPath = Path.Combine(curDir, "~mtcseq" + Guid.NewGuid().ToString("N").Substring(0, 10) + ".tmp");
                        try { File.Move(pair.Item1, tempPath); tempPaths.Add(Tuple.Create(pair.Item1, tempPath, pair.Item2)); }
                        catch (Exception ex) { errs.Add(Path.GetFileName(pair.Item1) + ": " + ex.Message); }
                    }
                    foreach (var t in tempPaths)
                    {
                        try { if (File.Exists(t.Item3)) continue; File.Move(t.Item2, t.Item3); ok2++; }
                        catch (Exception ex) { errs.Add(Path.GetFileName(t.Item3) + ": " + ex.Message); }
                    }
                }
                finally
                {
                    // sve sto je ostalo na privremenom imenu vraca se na staro ime
                    foreach (var t in tempPaths) { try { if (File.Exists(t.Item2)) File.Move(t.Item2, t.Item1); } catch (Exception ex) { errs.Add("NIJE VRACEN: " + t.Item2 + " -> " + t.Item1 + ": " + ex.Message); } }
                    opRunning = false;
                }
                p.Marked.Clear();
                RefreshPanel(L, true); RefreshPanel(R, true);
                Status(" 001 Rename: " + ok2 + "/" + filesInOrder.Count + " preimenovano u " + curDir + " (format " + new string('0', padLen) + ")");
                if (errs.Count > 0) ShowChoiceDialog(this, "001 Rename - greske", string.Join("\n", errs.Take(100)), new[] { "OK" }, 0, 0);
            }
            catch (Exception ex) { opRunning = false; MessageBox.Show(this, "001 Rename greska: " + ex.Message, "001 Rename"); }
        }

        // ===================== Brisanje =====================
        private void DeleteSelected(bool permanent)
        {
            if (opRunning) return;
            var p = Active; var toDel = GetOperationItems(p); if (toDel.Count == 0) return;
            using (Form delForm = new Form() { Width = 600, Height = 400, Text = permanent ? "Trajno brisanje (Shift+Del)" : "Brisanje u Recycle Bin (Del)", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false })
            {
                Label lbl = new Label() { Left = 10, Top = 10, Width = 560, Height = 40, Text = (permanent ? "TRAJNO obrisati " : "Poslati u Recycle Bin ") + toDel.Count + " stavki?" };
                ListBox lb = new ListBox() { Left = 10, Top = 50, Width = 560, Height = 270, HorizontalScrollbar = true }; lb.Items.AddRange(toDel.ToArray());
                Button ok = new Button() { Text = permanent ? "Trajno" : "Recycle", Left = 350, Top = 330, Width = 110, DialogResult = DialogResult.OK, BackColor = permanent ? ColorDeletePermanentBg : ColorDeleteRecycleBg, ForeColor = Color.White };
                Button cancel = new Button() { Text = "Otkazi", Left = 470, Top = 330, Width = 100, DialogResult = DialogResult.Cancel };
                delForm.Controls.AddRange(new Control[] { lbl, lb, ok, cancel }); delForm.AcceptButton = ok; delForm.CancelButton = cancel; DarkTheme.Apply(delForm);
                if (delForm.ShowDialog(this) != DialogResult.OK) return;
            }

            if (!permanent)
            {
                bool okAll, aborted = false; int code = 0;
                opRunning = true;
                try { okAll = RecycleBin.SendToRecycle(toDel, Handle, out aborted, out code); }
                catch (Exception ex) { okAll = false; MessageBox.Show(this, "Recycle greska: " + ex.Message); }
                finally { opRunning = false; }
                int left = toDel.Count(ExistsAny);
                foreach (var s in toDel) if (!ExistsAny(s)) p.Marked.Remove(s);
                RefreshPanel(L, false); RefreshPanel(R, false);
                if (!okAll && !aborted && left > 0)
                    MessageBox.Show(this, "Recycle Bin nije uspeo za " + left + " stavki (kod 0x" + code.ToString("X") + ").\nMoguci razlozi: nema prava, fajl je u upotrebi, putanja je preduga.\nZa trajno brisanje koristi Shift+Del.", "Del", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                Status(" Recycle: obrisano " + (toDel.Count - left) + "/" + toDel.Count + (left > 0 ? " (" + left + " nije)" : ""));
                return;
            }

            var ctx = new OpContext { ByFiles = true };
            RunWithProgress("Trajno brisanje", ctx, c =>
            {
                c.Phase = "Prebrojavam: ";
                foreach (var s in toDel) { c.CurrentName = s; Scan(c, s); }
                c.Phase = "Brisem: ";
                foreach (var s in toDel)
                {
                    if (c.Cancel) throw new OperationCanceledException();
                    if (DeleteTree(c, s)) lock (c.Completed) c.Completed.Add(s);
                }
            });
            FinishOp(ctx, "Obrisano", p);
            p.List.Focus();
        }
    }
}
