using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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
            e.TextColor = e.Item.Enabled? Color.White : Color.Gray;
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
            if (form!= null)
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
                        b.BackColor = (b.DialogResult == DialogResult.OK || b.DialogResult == DialogResult.Yes)? Blue : Grey;
                }
                else if (c is TextBox)
                {
                    if (c.BackColor == SystemColors.Window) c.BackColor = Input;
                    if (c.ForeColor == SystemColors.WindowText) c.ForeColor = Color.White;
                    ((TextBox)c).BorderStyle = BorderStyle.FixedSingle;
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
            using (Form prompt = new Form()
            {
                Width = 350, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false
            })
            {
                Label textLabel = new Label() { Left = 10, Top = 10, Width = 320, Text = text };
                TextBox textBox = new TextBox() { Left = 10, Top = 35, Width = 310, Text = defaultValue };
                Button confirmation = new Button() { Text = "Ok", Left = 160, Width = 75, Top = 70, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Cancel", Left = 245, Width = 75, Top = 70, DialogResult = DialogResult.Cancel };
                prompt.Controls.AddRange(new Control[] { textBox, confirmation, cancel, textLabel });
                prompt.AcceptButton = confirmation; prompt.CancelButton = cancel;
                DarkTheme.Apply(prompt);
                return prompt.ShowDialog() == DialogResult.OK? textBox.Text : "";
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
        private const uint FO_DELETE = 3; private const ushort FOF_SILENT = 0x4; private const ushort FOF_NOCONFIRMATION = 0x10;
        private const ushort FOF_ALLOWUNDO = 0x40; private const ushort FOF_NOERRORUI = 0x400;
        public static bool SendToRecycle(string path)
        {
            var fs = new SHFILEOPSTRUCT(); fs.hwnd = IntPtr.Zero; fs.wFunc = FO_DELETE;
            fs.pFrom = path + "\0\0"; fs.pTo = null;
            fs.fFlags = (ushort)(FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT | FOF_NOERRORUI);
            return SHFileOperation(ref fs) == 0;
        }
        [DllImport("shell32.dll", CharSet = CharSet.Unicode)][return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SHObjectProperties(IntPtr hwnd, uint shopObjectType, string pszObjectName, string pszPropertyPage);
        public static void ShowProperties(string path, IntPtr hwnd) { SHObjectProperties(hwnd, 2, path, null); }
    }

    public class MiniCommander : Form
    {
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
        private const uint FILE_ATTRIBUTE_NORMAL = 0x80; private const uint FILE_ATTRIBUTE_DIRECTORY = 0x10;
        private const uint SHGFI_ICON = 0x100; private const uint SHGFI_SMALLICON = 0x1; private const uint SHGFI_USEFILEATTRIBUTES = 0x10;
        private static readonly ConcurrentDictionary<string, Image> iconCache = new ConcurrentDictionary<string, Image>(StringComparer.OrdinalIgnoreCase);
        private static Image GetIconFor(string nameOrPath, bool isDir)
        {
            string key = isDir? "\\folder" : Path.GetExtension(nameOrPath).ToLowerInvariant();
            if (string.IsNullOrEmpty(key) &&!isDir) key = "\\noext";
            Image img; if (iconCache.TryGetValue(key, out img)) return img;
            img = null;
            try
            {
                var shfi = new SHFILEINFO(); uint attr = isDir? FILE_ATTRIBUTE_DIRECTORY : FILE_ATTRIBUTE_NORMAL;
                string dummy = isDir? "folder" : ("file" + key);
                IntPtr res = SHGetFileInfo(dummy, attr, ref shfi, (uint)Marshal.SizeOf<SHFILEINFO>(), SHGFI_ICON | SHGFI_SMALLICON | SHGFI_USEFILEATTRIBUTES);
                if (res!= IntPtr.Zero && shfi.hIcon!= IntPtr.Zero)
                {
                    try { using (var ico = Icon.FromHandle(shfi.hIcon)) img = ico.ToBitmap(); } catch { img = null; } finally { DestroyIcon(shfi.hIcon); }
                }
            }
            catch { img = null; }
            if (img!= null) iconCache[key] = img; return img;
        }

        private const string ConfigFilePath = @"C:\MiniTC\MiniTC.txt";

        private static readonly Color ColorBg = Color.FromArgb(30, 30, 30);
        private static readonly Color ColorFg = Color.FromArgb(220, 220, 220);
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
        private static readonly Color ColorSelFg = Color.White;
        private static readonly Color ColorMarkedFg = Color.FromArgb(255, 120, 120);
        private static readonly Color ColorShellOnBg = Color.FromArgb(16, 185, 129);
        private static readonly Color ColorHintFg = Color.Gray;
        private static readonly Color ColorGotoBtnBg = Color.FromArgb(16, 185, 129);
        private static readonly Color ColorDeletePermanentBg = Color.FromArgb(170, 50, 50);
        private static readonly Color ColorDeleteRecycleBg = Color.FromArgb(190, 110, 30);

        private ListView leftList, rightList;
        private ComboBox leftDrive, rightDrive;
        private TextBox leftPath, rightPath;
        private Label statusLabel;
        private TextBox cmdBox;
        private Panel cmdPanel;
        private Button cmdSaveBtn;
        private List<string> savedCommands = new List<string>();
        private int savedCmdIdx = -1;
        private string savedCmdDraft = "";
        private ToolStrip toolBar;
        private ToolStripComboBox fontCombo;
        private SplitContainer topSplit;
        private FlowLayoutPanel leftTabPanel, rightTabPanel;
        private string compareToolPath = "";
        private string compareToolParams = "";
        private ListView lastActive = null;
        private ListView ActiveList { get { return lastActive?? leftList; } }
        private string leftCurrent = @"C:\";
        private string rightCurrent = @"C:\";
        private Font listFont = new Font("Consolas", 14f);
        private HashSet<string> markedLeft = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private string leftLoadedFolder = null;
        private string rightLoadedFolder = null;
        private HashSet<string> markedRight = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> ActiveMarked { get { return ActiveList == leftList? markedLeft : markedRight; } }
        private bool useRealShellMenu = false;

        // --- ALT filter ---
        private static readonly Color ColorFilterBg = Color.FromArgb(15, 45, 90);
        private static readonly Color ColorFilterSelBg = Color.FromArgb(30, 70, 130);
        private string filterText = "";
        private ListView filterList = null;
        private List<ListViewItem> leftFullCache = null;
        private List<ListViewItem> rightFullCache = null;

        private class CopyProgressForm : Form
        {
            public ProgressBar bar; public Label lbl; public Button btnCancel; public bool Canceled = false;
            public CopyProgressForm(string op, int totalFiles)
            {
                Width = 500; Height = 140; FormBorderStyle = FormBorderStyle.FixedDialog; StartPosition = FormStartPosition.CenterParent;
                Text = op; MinimizeBox = false; MaximizeBox = false; ShowInTaskbar = false;
                lbl = new Label(){ Left=10, Top=10, Width=460, Height=30, Text="Priprema..."};
                bar = new ProgressBar(){ Left=10, Top=45, Width=460, Height=20, Minimum=0, Maximum=100 };
                btnCancel = new Button(){ Text="Otkazi", Left=380, Top=75, Width=90, DialogResult=DialogResult.Cancel };
                Controls.AddRange(new Control[]{ lbl, bar, btnCancel });
                this.CancelButton = btnCancel;
                this.FormClosing += (s2,e2)=>{ if(this.DialogResult==DialogResult.Cancel) Canceled=true; };
                DarkTheme.Apply(this);
                btnCancel.Click += (s,e)=>{ Canceled=true; };
            }
        }

        private int sortColumn = 0;
        private SortOrder sortOrder = SortOrder.Ascending;
        private ListView currentSortList = null;

        private class ListViewItemComparer : System.Collections.IComparer
        {
            private int col; private SortOrder order;
            public ListViewItemComparer(int column, SortOrder order) { this.col = column; this.order = order; }
            public int Compare(object x, object y)
            {
                var itemX = x as ListViewItem; var itemY = y as ListViewItem; if (itemX == null || itemY == null) return 0;
                if (itemX.Text == "[..]") return -1; if (itemY.Text == "[..]") return 1;
                string pathX = itemX.Tag as string; string pathY = itemY.Tag as string;
                bool isDirX = (pathX!= null && Directory.Exists(pathX)) || (itemX.SubItems.Count > 1 && itemX.SubItems[1].Text == "<DIR>");
                bool isDirY = (pathY!= null && Directory.Exists(pathY)) || (itemY.SubItems.Count > 1 && itemY.SubItems[1].Text == "<DIR>");
                int result = 0;
                switch (col)
                {
                    case 0:
                        string nameX = itemX.Text.Trim('[', ']'); string nameY = itemY.Text.Trim('[', ']');
                        if (isDirX &&!isDirY) result = -1; else if (!isDirX && isDirY) result = 1;
                        else result = string.Compare(nameX, nameY, StringComparison.OrdinalIgnoreCase); break;
                    case 1:
                        long sizeX = GetSize(itemX, pathX, isDirX); long sizeY = GetSize(itemY, pathY, isDirY);
                        if (isDirX &&!isDirY) result = -1; else if (!isDirX && isDirY) result = 1; else result = sizeX.CompareTo(sizeY); break;
                    case 2:
                        DateTime dtX, dtY; string dateStrX = itemX.SubItems.Count > 2? itemX.SubItems[2].Text : "";
                        string dateStrY = itemY.SubItems.Count > 2? itemY.SubItems[2].Text : "";
                        bool okX = DateTime.TryParseExact(dateStrX, "dd.MM.yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out dtX);
                        bool okY = DateTime.TryParseExact(dateStrY, "dd.MM.yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out dtY);
                        if (!okX) dtX = DateTime.MinValue; if (!okY) dtY = DateTime.MinValue; result = DateTime.Compare(dtX, dtY); break;
                    case 3:
                        string tipX = itemX.SubItems.Count > 3? itemX.SubItems[3].Text : "";
                        string tipY = itemY.SubItems.Count > 3? itemY.SubItems[3].Text : "";
                        result = string.Compare(tipX, tipY, StringComparison.OrdinalIgnoreCase);
                        if (result == 0) result = string.Compare(itemX.Text, itemY.Text, StringComparison.OrdinalIgnoreCase); break;
                }
                if (order == SortOrder.Descending) result = -result; return result;
            }
            private static long GetSize(ListViewItem item, string path, bool isDir)
            {
                if (isDir) return -1;
                try { if (path!= null && File.Exists(path)) return new FileInfo(path).Length; } catch { }
                try
                {
                    string s = item.SubItems[1].Text.Replace(" B", "").Replace(" KB", "").Replace(" MB", "").Trim();
                    long v = 0; if (long.TryParse(s, out v)) { if (item.SubItems[1].Text.Contains("KB")) v *= 1024; if (item.SubItems[1].Text.Contains("MB")) v *= 1024 * 1024; return v; }
                }
                catch { } return 0;
            }
        }
        private class TabInfo { public string Path; }
        private List<TabInfo> leftTabs = new List<TabInfo>(); private List<TabInfo> rightTabs = new List<TabInfo>();
        private int leftTabIdx = 0; private int rightTabIdx = 0;

        public MiniCommander()
        {
            Text = "Mini TC v1.4 (.NET 9) - F6 Move"; Width = 1200; Height = 800; WindowState = FormWindowState.Maximized;
            KeyPreview = true; BackColor = ColorBg; Font = new Font("Consolas", 9.5f);
            ToolStripManager.Renderer = new DarkRenderer(); DoubleBuffered = true;

            toolBar = new ToolStrip { Dock = DockStyle.Top, GripStyle = ToolStripGripStyle.Hidden, BackColor = ColorToolbarBg, ForeColor = ColorToolbarFg };
            toolBar.Items.Add(new ToolStripButton("<", null, (s, e) => SyncPanel(true)) { ToolTipText = "Levi = Desni" });
            toolBar.Items.Add(new ToolStripButton(">", null, (s, e) => SyncPanel(false)) { ToolTipText = "Desni = Levi" });
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripButton("F4 Notepad", null, (s, e) => OpenInNotepad()));
            toolBar.Items.Add(new ToolStripButton("F5 Kopiraj", null, (s, e) => CopySelected()));
            toolBar.Items.Add(new ToolStripButton("F6 Premesti", null, (s, e) => MoveSelected()));
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
            shellToggle.CheckedChanged += (s, e) => { useRealShellMenu = shellToggle.Checked; shellToggle.Text = useRealShellMenu? "Shell ON" : "Shell OFF"; shellToggle.BackColor = useRealShellMenu? ColorShellOnBg : ColorToolbarBg; };
            toolBar.Items.Add(shellToggle);

            topSplit = new SplitContainer { Dock = DockStyle.Fill, BackColor = ColorSplitter, BorderStyle = BorderStyle.None, SplitterWidth = 6 };
            leftDrive = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, TabStop = false, BackColor = ColorCmdBg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            rightDrive = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, TabStop = false, BackColor = ColorCmdBg, ForeColor = Color.White, FlatStyle = FlatStyle.Flat };
            leftPath = new TextBox { Dock = DockStyle.Top, BackColor = ColorCmdBg, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            rightPath = new TextBox { Dock = DockStyle.Top, BackColor = ColorCmdBg, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle };
            leftPath.KeyDown += PathBox_KeyDown; rightPath.KeyDown += PathBox_KeyDown;
            leftTabPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, BackColor = ColorTabBg, WrapContents = false, AutoScroll = true };
            rightTabPanel = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 30, BackColor = ColorTabBg, WrapContents = false, AutoScroll = true };
            leftList = CreateFileList(); rightList = CreateFileList(); DarkTheme.DarkScroll(leftList); DarkTheme.DarkScroll(rightList);
            var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = ColorBg };
            leftPanel.Controls.Add(leftList); leftPanel.Controls.Add(leftPath); leftPanel.Controls.Add(leftDrive); leftPanel.Controls.Add(leftTabPanel);
            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = ColorBg };
            rightPanel.Controls.Add(rightList); rightPanel.Controls.Add(rightPath); rightPanel.Controls.Add(rightDrive); rightPanel.Controls.Add(rightTabPanel);
            topSplit.Panel1.Controls.Add(leftPanel); topSplit.Panel2.Controls.Add(rightPanel);
            cmdBox = new TextBox { Dock = DockStyle.Bottom, Height = 26, BackColor = ColorCmdBg, ForeColor = ColorCmdFg, BorderStyle = BorderStyle.FixedSingle, Font = listFont };
            cmdBox.KeyDown += CmdBox_KeyDown;
            cmdSaveBtn = new Button { Text = "\U0001F4BE Save", Dock = DockStyle.Left, Width = 80, TabStop = false, FlatStyle = FlatStyle.Flat, BackColor = ColorAccent, ForeColor = Color.White };
            cmdSaveBtn.Click += (s, e) => SaveCurrentCommand();
            new ToolTip().SetToolTip(cmdSaveBtn, "Zapamti komandu iz polja (MiniTC.txt). Strelica gore/dole u polju lista sacuvane komande.");
            cmdBox.Dock = DockStyle.Fill; cmdPanel = new Panel { Dock = DockStyle.Bottom, Height = Math.Max(26, cmdBox.PreferredHeight) };
            cmdPanel.Controls.Add(cmdBox); cmdPanel.Controls.Add(cmdSaveBtn);
            var cmdLine = new Label { Dock = DockStyle.Bottom, Height = 2, BackColor = ColorSplitter };
            statusLabel = new Label { Dock = DockStyle.Bottom, Height = 26, BackColor = ColorToolbarBg, ForeColor = ColorStatusFg, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };
            EnableDoubleBuffer(statusLabel); EnableDoubleBuffer(cmdBox);
            Controls.Add(topSplit); Controls.Add(cmdLine); Controls.Add(cmdPanel); Controls.Add(statusLabel); Controls.Add(toolBar);
            this.Resize += (s, e) => { if (this.WindowState == FormWindowState.Minimized) return; FixSplitter(); ResizeColumns(leftList); ResizeColumns(rightList); };
            this.Load += (s, e) => FixSplitter();
            LoadDrives(); LoadTabsFromTxt();
            leftDrive.SelectedIndexChanged += (s, e) => { leftCurrent = leftDrive.Text; markedLeft.Clear(); if (leftTabs.Count > 0) leftTabs[leftTabIdx].Path = leftCurrent; LoadFolder(leftList, leftPath, leftCurrent, null); };
            rightDrive.SelectedIndexChanged += (s, e) => { rightCurrent = rightDrive.Text; markedRight.Clear(); if (rightTabs.Count > 0) rightTabs[rightTabIdx].Path = rightCurrent; LoadFolder(rightList, rightPath, rightCurrent, null); };
            this.KeyDown += OnKeyDown; this.FormClosing += (s, e) => SaveTabsToTxt();
            if (leftTabs.Count > 0) leftCurrent = leftTabs[leftTabIdx].Path; if (rightTabs.Count > 0) rightCurrent = rightTabs[rightTabIdx].Path;
            LoadFolder(leftList, leftPath, leftCurrent, null); LoadFolder(rightList, rightPath, rightCurrent, null);
            RenderTabs(); lastActive = leftList; leftList.Focus(); ChangeFont();
        }

        private void FixSplitter()
        {
            try
            {
                int total = topSplit.Width; if (total < 250) return;
                topSplit.Panel1MinSize = 100; topSplit.Panel2MinSize = 100;
                int half = total / 2; int min = topSplit.Panel1MinSize; int max = total - topSplit.Panel2MinSize - topSplit.SplitterWidth;
                if (max < min) return; if (half < min) half = min; if (half > max) half = max; topSplit.SplitterDistance = half;
            }
            catch { }
        }

        private void LoadTabsFromTxt()
        {
            try
            {
                compareToolPath = ""; compareToolParams = ""; savedCommands.Clear();
                if (!File.Exists(ConfigFilePath))
                {
                    leftTabs = new List<TabInfo> { new TabInfo { Path = @"C:\" } };
                    rightTabs = new List<TabInfo> { new TabInfo { Path = @"C:\" } };
                    if (Directory.Exists(@"D:\")) rightTabs[0].Path = @"D:\";
                    return;
                }
                var lines = File.ReadAllLines(ConfigFilePath); leftTabs.Clear(); rightTabs.Clear(); int mode = 0;
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
                        else if (line.StartsWith("<Folder>", StringComparison.OrdinalIgnoreCase)) { }
                        else if (!line.StartsWith("[") &&!line.StartsWith("<"))
                        {
                            if (string.IsNullOrEmpty(compareToolPath) && (File.Exists(line) || line.EndsWith(".exe", StringComparison.OrdinalIgnoreCase))) compareToolPath = line;
                        }
                        continue;
                    }
                    if (line.StartsWith("<Folder>", StringComparison.OrdinalIgnoreCase))
                    {
                        int s = line.IndexOf('>') + 1; int e = line.LastIndexOf('<'); if (e > s) line = line.Substring(s, e - s).Trim();
                    }
                    if (line.StartsWith("<") && line.EndsWith(">")) continue;
                    if (Directory.Exists(line))
                    {
                        if (mode == 1) leftTabs.Add(new TabInfo { Path = line });
                        else if (mode == 2) rightTabs.Add(new TabInfo { Path = line });
                        else { if (leftTabs.Count <= rightTabs.Count) leftTabs.Add(new TabInfo { Path = line }); else rightTabs.Add(new TabInfo { Path = line }); }
                    }
                }
                if (leftTabs.Count == 0) leftTabs.Add(new TabInfo { Path = @"C:\" });
                if (rightTabs.Count == 0) rightTabs.Add(new TabInfo { Path = @"C:\" });
                leftTabIdx = 0; rightTabIdx = 0;
            }
            catch { leftTabs = new List<TabInfo> { new TabInfo { Path = @"C:\" } }; rightTabs = new List<TabInfo> { new TabInfo { Path = @"C:\" } }; leftTabIdx = 0; rightTabIdx = 0; }
        }

        private string lastConfigError = "";
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
                    if (line.Equals("[LeviPanel]", StringComparison.OrdinalIgnoreCase) || line.Equals("[DesniPanel]", StringComparison.OrdinalIgnoreCase) || line.Equals("[CompareTool]", StringComparison.OrdinalIgnoreCase)) { inSection = false; continue; }
                    if (inSection &&!res.Contains(line)) res.Add(line);
                }
            }
            catch { } return res;
        }
        private void MergeSavedCommandsFromFile() { foreach (var c in ReadSavedCommandsFromFile()) if (!savedCommands.Contains(c)) savedCommands.Add(c); }
        private bool WriteConfigFile(List<string> lines)
        {
            string tmp = ConfigFilePath + ".tmp";
            for (int attempt = 0; attempt < 4; attempt++)
            {
                try { File.WriteAllLines(tmp, lines); File.Copy(tmp, ConfigFilePath, true); try { File.Delete(tmp); } catch { } lastConfigError = ""; return true; }
                catch (Exception ex) { lastConfigError = ex.Message; System.Threading.Thread.Sleep(80); }
            }
            return false;
        }
        private bool SaveTabsToTxt()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigFilePath); if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
                if (leftTabs.Count > leftTabIdx && leftTabIdx >= 0) leftTabs[leftTabIdx].Path = leftCurrent;
                if (rightTabs.Count > rightTabIdx && rightTabIdx >= 0) rightTabs[rightTabIdx].Path = rightCurrent;
                var outLines = new List<string>(); outLines.Add("[LeviPanel]"); foreach (var t in leftTabs) outLines.Add(t.Path); outLines.Add(""); outLines.Add("[DesniPanel]");
                foreach (var t in rightTabs) outLines.Add(t.Path); outLines.Add(""); outLines.Add("[CompareTool]"); outLines.Add("Path=" + compareToolPath); outLines.Add("Params=" + compareToolParams);
                outLines.Add(""); outLines.Add("[SavedCommands]"); MergeSavedCommandsFromFile(); foreach (var c in savedCommands) outLines.Add(c);
                return WriteConfigFile(outLines);
            }
            catch (Exception ex) { lastConfigError = ex.Message; return false; }
        }
        private void RenderTabs()
        {
            leftTabPanel.Controls.Clear();
            for (int i = 0; i < leftTabs.Count; i++)
            {
                int idx = i; string name = GetTabName(leftTabs[i].Path);
                var btn = new Button { Text = (i + 1) + ":" + name, Height = 24, AutoSize = true, BackColor = i == leftTabIdx? ColorTabActiveBg : ColorTabInactiveBg, ForeColor = i == leftTabIdx? ColorTabActiveFg : ColorTabInactiveFg, FlatStyle = FlatStyle.Flat, Margin = new Padding(1) };
                btn.Click += (s, e) => { leftTabs[leftTabIdx].Path = leftCurrent; leftTabIdx = idx; leftCurrent = leftTabs[leftTabIdx].Path; LoadFolder(leftList, leftPath, leftCurrent, null); RenderTabs(); };
                leftTabPanel.Controls.Add(btn);
            }
            rightTabPanel.Controls.Clear();
            for (int i = 0; i < rightTabs.Count; i++)
            {
                int idx = i; string name = GetTabName(rightTabs[i].Path);
                var btn = new Button { Text = (i + 1) + ":" + name, Height = 24, AutoSize = true, BackColor = i == rightTabIdx? ColorTabActiveBg : ColorTabInactiveBg, ForeColor = i == rightTabIdx? ColorTabActiveFg : ColorTabInactiveFg, FlatStyle = FlatStyle.Flat, Margin = new Padding(1) };
                btn.Click += (s, e) => { rightTabs[rightTabIdx].Path = rightCurrent; rightTabIdx = idx; rightCurrent = rightTabs[rightTabIdx].Path; LoadFolder(rightList, rightPath, rightCurrent, null); RenderTabs(); };
                rightTabPanel.Controls.Add(btn);
            }
        }
        private string GetTabName(string path)
        {
            try { if (string.IsNullOrEmpty(path)) return ""; if (path.EndsWith(":\\") || path.EndsWith(":")) return path; string n = Path.GetFileName(path.TrimEnd('\\')); return string.IsNullOrEmpty(n)? path : n; } catch { return path; }
        }
        private void NewTab()
        {
            if (ActiveList == leftList) { leftTabs[leftTabIdx].Path = leftCurrent; leftTabs.Insert(leftTabIdx + 1, new TabInfo { Path = leftCurrent }); leftTabIdx++; RenderTabs(); SaveTabsToTxt(); }
            else { rightTabs[rightTabIdx].Path = rightCurrent; rightTabs.Insert(rightTabIdx + 1, new TabInfo { Path = rightCurrent }); rightTabIdx++; RenderTabs(); SaveTabsToTxt(); }
        }
        private void CloseTab()
        {
            if (ActiveList == leftList) { if (leftTabs.Count <= 1) return; leftTabs.RemoveAt(leftTabIdx); leftTabIdx = Math.Max(0, leftTabIdx - 1); leftCurrent = leftTabs[leftTabIdx].Path; LoadFolder(leftList, leftPath, leftCurrent, null); RenderTabs(); SaveTabsToTxt(); }
            else { if (rightTabs.Count <= 1) return; rightTabs.RemoveAt(rightTabIdx); rightTabIdx = Math.Max(0, rightTabIdx - 1); rightCurrent = rightTabs[rightTabIdx].Path; LoadFolder(rightList, rightPath, rightCurrent, null); RenderTabs(); SaveTabsToTxt(); }
        }
        private void NextTab()
        {
            if (ActiveList == leftList) { leftTabs[leftTabIdx].Path = leftCurrent; leftTabIdx = (leftTabIdx + 1) % leftTabs.Count; leftCurrent = leftTabs[leftTabIdx].Path; LoadFolder(leftList, leftPath, leftCurrent, null); RenderTabs(); }
            else { rightTabs[rightTabIdx].Path = rightCurrent; rightTabIdx = (rightTabIdx + 1) % rightTabs.Count; rightCurrent = rightTabs[rightTabIdx].Path; LoadFolder(rightList, rightPath, rightCurrent, null); RenderTabs(); }
        }
        private bool TryHandleCd(string cmd, string activeCur, ListView list, TextBox pathBox)
        {
            if (!cmd.StartsWith("cd", StringComparison.OrdinalIgnoreCase)) return false; if (cmd.Length > 2 && char.IsLetterOrDigit(cmd[2])) return false;
            string arg = cmd.Length > 2? cmd.Substring(2).Trim() : "";
            if (arg.Length >= 2 && arg[0] == '"' && arg[arg.Length - 1] == '"') arg = arg.Substring(1, arg.Length - 2);
            string target;
            if (arg.Length == 0) target = activeCur;
            else if (arg == "\\" || arg == @"/") target = Path.GetPathRoot(activeCur);
            else if (arg == "..") { var parent = Directory.GetParent(activeCur); target = parent!= null? parent.FullName : activeCur; }
            else if (arg.Length >= 2 && arg[1] == ':') target = arg.Length == 2? arg + "\\" : arg;
            else { try { target = Path.GetFullPath(Path.Combine(activeCur, arg)); } catch { target = arg; } }
            if (!Directory.Exists(target)) { statusLabel.Text = " Ne postoji folder: " + target; lastStatus = statusLabel.Text; cmdBox.Clear(); return true; }
            LoadFolder(list, pathBox, target, activeCur); cmdBox.Clear(); return true;
        }
        private void SaveCurrentCommand()
        {
            string cmd = cmdBox.Text.Trim(); MergeSavedCommandsFromFile();
            if (cmd.Length == 0) statusLabel.Text = " Save: polje za komandu je prazno";
            else if (savedCommands.Contains(cmd)) statusLabel.Text = " Save: komanda vec postoji u MiniTC.txt";
            else { savedCommands.Add(cmd); if (SaveTabsToTxt()) statusLabel.Text = " Sacuvano u MiniTC.txt (" + savedCommands.Count + " komandi): " + cmd; else statusLabel.Text = " GRESKA: ne mogu da upisem u " + ConfigFilePath + " - " + lastConfigError; }
            lastStatus = statusLabel.Text; cmdBox.Focus(); cmdBox.SelectionStart = cmdBox.TextLength;
        }
        private void NavigateSavedCommands(bool up)
        {
            if (savedCommands.Count == 0) return;
            if (savedCmdIdx == -1) { if (!up) return; savedCmdDraft = cmdBox.Text; savedCmdIdx = savedCommands.Count - 1; }
            else if (up) { if (savedCmdIdx > 0) savedCmdIdx--; } else { savedCmdIdx++; if (savedCmdIdx >= savedCommands.Count) savedCmdIdx = -1; }
            cmdBox.Text = savedCmdIdx == -1? savedCmdDraft : savedCommands[savedCmdIdx]; cmdBox.SelectionStart = cmdBox.TextLength;
        }
        private void CmdBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Up || e.KeyCode == Keys.Down) { NavigateSavedCommands(e.KeyCode == Keys.Up); e.Handled = true; e.SuppressKeyPress = true; return; }
            if (e.KeyCode!= Keys.Enter) return; savedCmdIdx = -1;
            string cmd = cmdBox.Text.Trim(); if (string.IsNullOrWhiteSpace(cmd)) return;
            string activeCur = ActiveList == leftList? leftCurrent : rightCurrent; var activePath = ActiveList == leftList? leftPath : rightPath;
            try
            {
                if (cmd.Length == 2 && cmd[1] == ':') cmd += "\\";
                if (TryHandleCd(cmd, activeCur, ActiveList, activePath)) { e.SuppressKeyPress = true; return; }
                if (Directory.Exists(cmd)) { LoadFolder(ActiveList, activePath, cmd, null); cmdBox.Clear(); e.SuppressKeyPress = true; return; }
                if (cmd.IndexOfAny(Path.GetInvalidPathChars()) < 0)
                {
                    string maybeDir = null; try { maybeDir = Path.GetFullPath(Path.Combine(activeCur, cmd)); } catch { }
                    if (maybeDir!= null && Directory.Exists(maybeDir)) { LoadFolder(ActiveList, activePath, maybeDir, activeCur); cmdBox.Clear(); e.SuppressKeyPress = true; return; }
                }
                if (cmd.Equals("..") || cmd.Equals("cd..")) { var parent = Directory.GetParent(activeCur); if (parent!= null) LoadFolder(ActiveList, activePath, parent.FullName, activeCur); cmdBox.Clear(); return; }
                if (cmd.Equals("cmd", StringComparison.OrdinalIgnoreCase)) { Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = activeCur, UseShellExecute = true }); cmdBox.Clear(); return; }
                bool hasIllegal = cmd.IndexOfAny(new char[] { '>', '<', '|', '"' }) >= 0;
                if (!hasIllegal) { try { string maybeFile = Path.Combine(activeCur, cmd); if (File.Exists(maybeFile)) { Process.Start(new ProcessStartInfo(maybeFile) { UseShellExecute = true, WorkingDirectory = activeCur }); cmdBox.Clear(); return; } } catch { } }
                try { if (File.Exists(cmd)) { Process.Start(new ProcessStartInfo(cmd) { UseShellExecute = true }); cmdBox.Clear(); return; } } catch { }
                try
                {
                    string firstToken = cmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstToken))
                    {
                        if (firstToken.Equals("notepad", StringComparison.OrdinalIgnoreCase) || firstToken.Equals("calc", StringComparison.OrdinalIgnoreCase) || firstToken.Equals("mspaint", StringComparison.OrdinalIgnoreCase))
                        {
                            Process.Start(new ProcessStartInfo("cmd.exe", "/c cd /d \"" + activeCur + "\" && " + cmd) { UseShellExecute = true, WorkingDirectory = activeCur }); cmdBox.Clear(); e.SuppressKeyPress = true; return;
                        }
                    }
                }
                catch { }
                Process.Start(new ProcessStartInfo("cmd.exe", "/k cd /d \"" + activeCur + "\" && " + cmd) { UseShellExecute = true, WorkingDirectory = activeCur });
            }
            catch (Exception ex) { statusLabel.Text = " CMD greska: " + ex.Message; lastStatus = statusLabel.Text; }
            cmdBox.Clear(); e.SuppressKeyPress = true;
        }
        private void EnableDoubleBuffer(Control c) { try { typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(c, true, null); } catch { } }
        private TextBoxBase GetFocusedTextBox()
        {
            if (cmdBox!= null && cmdBox.Focused) return cmdBox; if (leftPath!= null && leftPath.Focused) return leftPath; if (rightPath!= null && rightPath.Focused) return rightPath;
            Control c = this.ActiveControl; while (c is ContainerControl) { var next = ((ContainerControl)c).ActiveControl; if (next == null || next == c) break; c = next; } return c as TextBoxBase;
        }
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            var focusedTb = GetFocusedTextBox(); var focused = this.ActiveControl; bool isTyping = focusedTb!= null || focused is ComboBox;
            if (isTyping)
            {
                if (keyData == Keys.Space || keyData == Keys.Delete || keyData == (Keys.Shift | Keys.Delete) || keyData == Keys.Escape) return base.ProcessCmdKey(ref msg, keyData);
                if (isTyping && (keyData == Keys.Tab || keyData == (Keys.Control | Keys.Left) || keyData == (Keys.Control | Keys.Right))) return base.ProcessCmdKey(ref msg, keyData);
                if (focusedTb!= null && (keyData == (Keys.Control | Keys.C) || keyData == (Keys.Control | Keys.X) || keyData == (Keys.Control | Keys.V) || keyData == (Keys.Control | Keys.Insert) || keyData == (Keys.Shift | Keys.Insert))) return base.ProcessCmdKey(ref msg, keyData);
                if (focusedTb!= null && keyData == (Keys.Control | Keys.A)) { focusedTb.SelectAll(); return true; }
            }
            if (keyData == Keys.F3) { RunCompareTool(); return true; }
            if (keyData == Keys.Tab) { if (isTyping) return base.ProcessCmdKey(ref msg, keyData); SwitchPanel(); return true; }
            if (keyData == (Keys.Control | Keys.Tab)) { NextTab(); return true; }
            if (keyData == (Keys.Control | Keys.T)) { NewTab(); return true; }
            if (keyData == (Keys.Control | Keys.W)) { CloseTab(); return true; }
            if (keyData == Keys.Escape)
            {
                if (!string.IsNullOrEmpty(filterText)) { ClearFilter(); return true; }
                ActiveMarked.Clear(); ActiveList.Invalidate(); UpdateStatus(); return true;
            }
            if (keyData == Keys.Back)
            {
                if (!string.IsNullOrEmpty(filterText) && filterList == ActiveList)
                {
                    if (filterText.Length > 1) ApplyFilter(ActiveList, filterText.Substring(0, filterText.Length - 1));
                    else ClearFilter();
                    return true;
                }
            }
            if (keyData == Keys.Space) { ToggleMark(); return true; }
            if (keyData == Keys.F6) { MoveSelected(); return true; }
            if (keyData == (Keys.Shift | Keys.F6)) { RenameSelected(); return true; }
            if (keyData == (Keys.Shift | Keys.Delete)) { DeleteSelected(true); return true; }
            if (keyData == Keys.Delete) { DeleteSelected(false); return true; }
            if (keyData == (Keys.Control | Keys.Left)) { SyncPanel(true); return true; }
            if (keyData == (Keys.Control | Keys.Right)) { SyncPanel(false); return true; }
            if (keyData == (Keys.Alt | Keys.F7)) { OpenSearchDialog(); return true; }
            if (keyData == (Keys.Control | Keys.C)) { ClipboardCopyFiles(); return true; }
            if (keyData == (Keys.Control | Keys.V)) { ClipboardPasteFiles(); return true; }
            if (keyData == (Keys.Control | Keys.X)) { ClipboardCutFiles(); return true; }

            bool alt = (keyData & Keys.Alt) == Keys.Alt;
            bool ctrl = (keyData & Keys.Control) == Keys.Control;
            Keys kCode = keyData & Keys.KeyCode;
            if (alt && !ctrl && !isTyping)
            {
                if (kCode >= Keys.A && kCode <= Keys.Z){ char ch=(char)('a'+(kCode-Keys.A)); JumpToFirst(ch); return true; }
                if (kCode >= Keys.D0 && kCode <= Keys.D9){ char ch=(char)('0'+(kCode-Keys.D0)); JumpToFirst(ch); return true; }
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
        private void SyncPanel(bool leftGetsRight)
        {
            if (leftGetsRight) { LoadFolder(leftList, leftPath, rightCurrent, null); leftList.Focus(); lastActive = leftList; }
            else { LoadFolder(rightList, rightPath, leftCurrent, null); rightList.Focus(); lastActive = rightList; }
        }
        private void SwitchPanel()
        {
            var old = lastActive; if (lastActive == leftList) lastActive = rightList; else lastActive = leftList;
            lastActive.Focus(); if (old!= null) old.Invalidate(); lastActive.Invalidate(); UpdateStatus();
        }
        private void ChangeFont()
        {
            float size = 14f; if (!float.TryParse(fontCombo.Text, out size)) size = 14f; if (size < 8) size = 14;
            var oldFont = listFont; listFont = new Font("Consolas", size, FontStyle.Regular);
            Font uiFont = new Font("Consolas", 9.5f, FontStyle.Regular);
            leftList.Font = listFont; rightList.Font = listFont; leftPath.Font = uiFont; rightPath.Font = uiFont;
            leftDrive.Font = uiFont; rightDrive.Font = uiFont; statusLabel.Font = uiFont; toolBar.Font = uiFont;
            cmdBox.Font = listFont; cmdSaveBtn.Font = uiFont; cmdPanel.Height = Math.Max(26, cmdBox.PreferredHeight);
            ResizeColumns(leftList); ResizeColumns(rightList); leftList.Invalidate(); rightList.Invalidate();
        }
        private void ResizeColumns(ListView lv)
        {
            if (lv == null || lv.Columns.Count < 4) return; if (lv.ClientSize.Width < 50) return;
            int w = lv.ClientSize.Width - 4;
            lv.Columns[0].Width = (int)(w * 0.50); lv.Columns[1].Width = (int)(w * 0.15);
            lv.Columns[2].Width = (int)(w * 0.20); lv.Columns[3].Width = w - lv.Columns[0].Width - lv.Columns[1].Width - lv.Columns[2].Width;
        }
        private ListView CreateFileList()
        {
            var lv = new ListView { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false, OwnerDraw = true, BackColor = ColorBg, ForeColor = ColorFg, Font = listFont, BorderStyle = BorderStyle.None };
            EnableDoubleBuffer(lv);
            lv.Columns.Add("Ime", 300); lv.Columns.Add("Velicina", 80); lv.Columns.Add("Datum", 120); lv.Columns.Add("Tip", 60);
            lv.Resize += (s, e) => ResizeColumns(lv);
            lv.ColumnClick += (s, e) =>
            {
                var list = s as ListView;
                if (currentSortList == list && sortColumn == e.Column) sortOrder = (sortOrder == SortOrder.Ascending)? SortOrder.Descending : SortOrder.Ascending;
                else { sortColumn = e.Column; sortOrder = SortOrder.Ascending; currentSortList = list; }
                list.ListViewItemSorter = new ListViewItemComparer(sortColumn, sortOrder); list.Sort(); list.Focus();
            };
            lv.DrawColumnHeader += (s, e) => { using (var hb = new SolidBrush(ColorHeaderBg)) e.Graphics.FillRectangle(hb, e.Bounds); TextRenderer.DrawText(e.Graphics, e.Header.Text, listFont, e.Bounds, ColorHeaderFg, TextFormatFlags.VerticalCenter | TextFormatFlags.Left); };
            lv.DrawItem += (s, e) => { e.DrawDefault = false; };
            lv.DrawSubItem += (s, e) =>
            {
                var list = s as ListView; string fullPath = e.Item.Tag as string;
                bool isMarked = fullPath!= null && (list == leftList? markedLeft.Contains(fullPath) : markedRight.Contains(fullPath));
                bool isFocusedItem = e.Item.Focused && list.Focused;
                bool isFiltered = list.BackColor.ToArgb() == ColorFilterBg.ToArgb();
                Color back = isFocusedItem? (isFiltered? ColorFilterSelBg : ColorSelBg) : (isFiltered? ColorFilterBg : ColorBg);
                Color fore = isMarked? ColorMarkedFg : (isFocusedItem? ColorSelFg : ColorFg);
                using (var b = new SolidBrush(back)) e.Graphics.FillRectangle(b, e.Bounds);
                Font f = isMarked? new Font(listFont, FontStyle.Bold) : listFont;
                try
                {
                    var textPos = e.Bounds.Location;
                    if (e.ColumnIndex == 0)
                    {
                        bool isDirRow = e.Item.SubItems.Count > 1 && (e.Item.SubItems[1].Text == "<DIR>" || e.Item.SubItems[1].Text == "<DIR UP>");
                        Image icon = GetIconFor(isDirRow? "" : (fullPath?? e.Item.Text), isDirRow);
                        if (icon!= null) { int iy = e.Bounds.Top + (e.Bounds.Height - 16) / 2; e.Graphics.DrawImage(icon, e.Bounds.Left + 2, iy, 16, 16); }
                        textPos = new Point(e.Bounds.Left + 2 + 16 + 4, e.Bounds.Top);
                    }
                    using (var fb = new SolidBrush(fore)) e.Graphics.DrawString(e.SubItem.Text, f, fb, textPos);
                }
                finally { if (isMarked) f.Dispose(); }
            };
            lv.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var item = lv.GetItemAt(e.X, e.Y);
                    if (item!= null) { item.Selected = true; item.Focused = true; lastActive = lv; string p = item.Tag as string; if (!string.IsNullOrEmpty(p)) ShowExplorerContextMenu(p); }
                }
            };
            lv.DoubleClick += (s, e) => EnterFolder();
            lv.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) { EnterFolder(); e.SuppressKeyPress = true; } };
            lv.Enter += (s, e) => { lastActive = lv; };
            return lv;
        }
        private void ShowExplorerContextMenu(string path)
        {
            if (useRealShellMenu)
            {
                try
                {
                    dynamic shell = Activator.CreateInstance(Type.GetTypeFromProgID("Shell.Application"));
                    dynamic folder = shell.NameSpace(Path.GetDirectoryName(path)); dynamic item = folder.ParseName(Path.GetFileName(path));
                    var menu = new ContextMenuStrip();
                    foreach (var v in item.Verbs()) { string vName = v.Name.Replace("&", "").Trim(); if (string.IsNullOrWhiteSpace(vName)) continue; dynamic verb = v; menu.Items.Add(vName, null, (s, e) => { try { verb.DoIt(); } catch { } }); }
                    if (menu.Items.Count == 0) menu.Items.Add("(nema shell verbova)"); menu.Items.Add(new ToolStripSeparator());
                    menu.Items.Add("Svojstva", null, (s, e) => RecycleBin.ShowProperties(path, this.Handle)); menu.Show(Cursor.Position); return;
                }
                catch (Exception ex) { MessageBox.Show("Shell meni nije uspeo: " + ex.Message); }
            }
            var menu2 = new ContextMenuStrip();
            menu2.Items.Add("Otvori", null, (s, e) => { try { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); } catch (Exception ex) { MessageBox.Show("Ne mogu da otvorim: " + ex.Message); } });
            if (File.Exists(path)) menu2.Items.Add("Otvori u Notepadu (F4)", null, (s, e) => OpenInNotepad());
            menu2.Items.Add("Kopiraj putanju (F8)", null, (s, e) => CopyPathToClipboard()); menu2.Items.Add(new ToolStripSeparator());
            menu2.Items.Add("Iseci", null, (s, e) => { try { SetClipboardFiles(new List<string> { path }, true); _isCutOperation = true; _clipboardCutList = new List<string> { path }; } catch (Exception ex) { MessageBox.Show("Clipboard greska: " + ex.Message); } });
            menu2.Items.Add("Kopiraj", null, (s, e) => { try { SetClipboardFiles(new List<string> { path }, false); _isCutOperation = false; _clipboardCutList = null; } catch (Exception ex) { MessageBox.Show("Clipboard greska: " + ex.Message); } });
            menu2.Items.Add(new ToolStripSeparator()); menu2.Items.Add("Posalji u Recycle Bin", null, (s, e) => DeleteSelected(false));
            menu2.Items.Add("Trajno obrisi (Shift+Del)", null, (s, e) => DeleteSelected(true)); menu2.Items.Add(new ToolStripSeparator());
            menu2.Items.Add("Svojstva", null, (s, e) => RecycleBin.ShowProperties(path, this.Handle));
            menu2.Items.Add("Otvori u Exploreru", null, (s, e) => { string arg = File.Exists(path)? "/select, \"" + path + "\"" : "\"" + path + "\""; try { Process.Start("explorer.exe", arg); } catch { } });
            menu2.Show(Cursor.Position);
        }
        private void ToggleMark()
        {
            if (ActiveList.SelectedItems.Count == 0) return;
            var item = ActiveList.SelectedItems[0]; string path = item.Tag as string; if (string.IsNullOrEmpty(path) || path.EndsWith("..")) return; if (item.Text == "[..]") return;
            if (ActiveMarked.Contains(path)) ActiveMarked.Remove(path); else ActiveMarked.Add(path);
            int idx = item.Index; ActiveList.RedrawItems(idx, idx, false);
            if (idx + 1 < ActiveList.Items.Count) { ActiveList.SelectedItems.Clear(); ActiveList.Items[idx + 1].Selected = true; ActiveList.Items[idx + 1].Focused = true; ActiveList.EnsureVisible(idx + 1); }
            UpdateStatus();
        }
        private void LoadDrives()
        {
            string[] drives; try { drives = DriveInfo.GetDrives().Where(d => { try { return d.IsReady; } catch { return false; } }).Select(d => d.RootDirectory.FullName).ToArray(); } catch { drives = new string[] { @"C:\" }; }
            leftDrive.Items.AddRange(drives); rightDrive.Items.AddRange(drives); if (drives.Length > 0) leftDrive.SelectedIndex = 0; if (drives.Length > 1) rightDrive.SelectedIndex = 1;
        }
        private static bool SameFolder(string a, string b) { if (a == null || b == null) return false; return string.Equals(a.TrimEnd('\\'), b.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase); }
        private void LoadFolder(ListView list, TextBox pathBox, string path, string pathToSelect)
        {
            try
            {
                if (filterList == list || (list == leftList && leftFullCache != null) || (list == rightList && rightFullCache != null))
                {
                    if (list == leftList) leftFullCache = null; else rightFullCache = null;
                    if (filterList == list) { filterText = ""; filterList = null; }
                    list.BackColor = ColorBg;
                }
                if (string.IsNullOrEmpty(path) ||!Directory.Exists(path)) path = @"C:\"; pathBox.Text = path;
                { bool isLeft = list == leftList; string prevFolder = isLeft? leftLoadedFolder : rightLoadedFolder; if (!SameFolder(prevFolder, path)) (isLeft? markedLeft : markedRight).Clear(); if (isLeft) leftLoadedFolder = path; else rightLoadedFolder = path; }
                if (list == leftList) { leftCurrent = path; if (leftTabs.Count > leftTabIdx) leftTabs[leftTabIdx].Path = path; } else { rightCurrent = path; if (rightTabs.Count > rightTabIdx) rightTabs[rightTabIdx].Path = path; }
                list.BeginUpdate(); list.Items.Clear(); DirectoryInfo dirInfo = null; try { dirInfo = new DirectoryInfo(path); } catch { dirInfo = null; }
                if (dirInfo == null ||!dirInfo.Exists) { list.EndUpdate(); statusLabel.Text = " Ne mogu da pristupim: " + path; lastStatus = statusLabel.Text; list.Focus(); return; }
                try { if (dirInfo.Parent!= null) { var up = new ListViewItem("[..]") { Tag = dirInfo.Parent.FullName }; up.SubItems.Add(""); up.SubItems.Add(""); up.SubItems.Add("<DIR UP>"); list.Items.Add(up); } } catch { }
                var dirs = new List<DirectoryInfo>(); try { foreach (var d in dirInfo.EnumerateDirectories()) dirs.Add(d); } catch (UnauthorizedAccessException) { statusLabel.Text = " Access denied: " + path + " - preskacem"; lastStatus = statusLabel.Text; } catch (Exception ex) { statusLabel.Text = " Greska: " + ex.Message; lastStatus = statusLabel.Text; }
                foreach (var d in dirs) { try { if ((d.Attributes & FileAttributes.Hidden)!= 0) continue; var item = new ListViewItem("[" + d.Name + "]") { Tag = d.FullName }; item.SubItems.Add("<DIR>"); item.SubItems.Add(d.LastWriteTime.ToString("dd.MM.yyyy HH:mm")); item.SubItems.Add("Folder"); list.Items.Add(item); } catch { } }
                var files = new List<FileInfo>(); try { foreach (var f in dirInfo.EnumerateFiles()) files.Add(f); } catch { }
                foreach (var f in files) { try { if ((f.Attributes & FileAttributes.Hidden)!= 0) continue; var item = new ListViewItem(f.Name) { Tag = f.FullName }; item.SubItems.Add(FormatSize(f.Length)); item.SubItems.Add(f.LastWriteTime.ToString("dd.MM.yyyy HH:mm")); item.SubItems.Add(f.Extension); list.Items.Add(item); } catch { } }
                list.EndUpdate(); bool found = false;
                if (!string.IsNullOrEmpty(pathToSelect)) { foreach (ListViewItem it in list.Items) { if (string.Equals(it.Tag as string, pathToSelect, StringComparison.OrdinalIgnoreCase)) { it.Selected = true; it.Focused = true; list.EnsureVisible(it.Index); found = true; break; } } }
                if (!found && list.Items.Count > 0) { list.Items[0].Selected = true; list.Items[0].Focused = true; }
                ResizeColumns(list); RenderTabs(); list.Focus(); lastActive = list; UpdateStatus();
            }
            catch (Exception ex) { statusLabel.Text = " Greska: " + ex.Message; lastStatus = statusLabel.Text; try { list.EndUpdate(); } catch { } try { list.Focus(); } catch { } }
        }
        private string lastStatus = "";
        private bool _firstActivationDone = false; private bool _refreshingOnActivate = false;
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e); if (!_firstActivationDone) { _firstActivationDone = true; return; } RefreshPanelsKeepState();
        }
        private string ResolveExistingFolder(string p)
        {
            try { while (!string.IsNullOrEmpty(p) &&!Directory.Exists(p)) { string parent = Path.GetDirectoryName(p); if (string.IsNullOrEmpty(parent) || parent == p) return null; p = parent; } } catch { return null; } return p;
        }
        private void RefreshPanelsKeepState()
        {
            if (_refreshingOnActivate) return; if (leftList == null || rightList == null || WindowState == FormWindowState.Minimized) return; _refreshingOnActivate = true;
            try
            {
                Control prevFocus = this.ActiveControl; while (prevFocus is ContainerControl) { var next = ((ContainerControl)prevFocus).ActiveControl; if (next == null || next == prevFocus) break; prevFocus = next; }
                var prevTb = prevFocus as TextBoxBase; int selStart = prevTb!= null? prevTb.SelectionStart : 0; int selLen = prevTb!= null? prevTb.SelectionLength : 0; var prevActive = lastActive;
                MergeSavedCommandsFromFile();
                foreach (var set in new[] { markedLeft, markedRight }) set.RemoveWhere(m =>!File.Exists(m) &&!Directory.Exists(m));
                bool editLeft = prevFocus == leftPath && leftPath.Text!= leftCurrent; bool editRight = prevFocus == rightPath && rightPath.Text!= rightCurrent;
                if (!editLeft) RefreshOnePanel(leftList, leftPath, leftCurrent); if (!editRight) RefreshOnePanel(rightList, rightPath, rightCurrent);
                lastActive = prevActive;
                if (prevFocus!= null &&!prevFocus.IsDisposed) { prevFocus.Focus(); if (prevTb!= null) { try { prevTb.SelectionStart = selStart; prevTb.SelectionLength = selLen; } catch { } } }
                leftList.Invalidate(); rightList.Invalidate(); UpdateStatus();
            }
            catch { } finally { _refreshingOnActivate = false; }
        }
        private void RefreshOnePanel(ListView list, TextBox pathBox, string currentPath)
        {
            try
            {
                string selPath = list.SelectedItems.Count > 0? list.SelectedItems[0].Tag as string : null; int selIdx = list.SelectedItems.Count > 0? list.SelectedItems[0].Index : 0; string topPath = null; try { if (list.TopItem!= null) topPath = list.TopItem.Tag as string; } catch { }
                string target = ResolveExistingFolder(currentPath)?? currentPath; LoadFolder(list, pathBox, target, selPath);
                bool selFound = list.SelectedItems.Count > 0 && string.Equals(list.SelectedItems[0].Tag as string, selPath, StringComparison.OrdinalIgnoreCase);
                if (!selFound && list.Items.Count > 0 && target == currentPath) { int idx = Math.Min(selIdx, list.Items.Count - 1); list.SelectedItems.Clear(); list.Items[idx].Selected = true; list.Items[idx].Focused = true; }
                if (topPath!= null && target == currentPath) { foreach (ListViewItem it in list.Items) { if (string.Equals(it.Tag as string, topPath, StringComparison.OrdinalIgnoreCase)) { try { list.TopItem = it; } catch { } break; } } }
            }
            catch { }
        }
        private void UpdateStatus()
        {
            int marked = ActiveMarked.Count; string sel = ActiveList.SelectedItems.Count > 0? (ActiveList.SelectedItems[0].Tag as string) : "";
            string txt = string.Format(" SEL: {0} | MARK: {1} | Tab:Ctrl+T/W/Tab | CMD dole", sel, marked);
            if (txt!= lastStatus) { statusLabel.Text = txt; lastStatus = txt; }
        }
        private void PathBox_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Enter) { var box = sender as TextBox; var list = box == leftPath? leftList : rightList; if (Directory.Exists(box.Text)) LoadFolder(list, box, box.Text, null); e.SuppressKeyPress = true; } }
        private void EnterFolder()
        {
            if (ActiveList.SelectedItems.Count == 0) return; var tag = ActiveList.SelectedItems[0].Tag as string; if (tag == null) return;
            if (Directory.Exists(tag))
            {
                try { var test = Directory.EnumerateFileSystemEntries(tag).FirstOrDefault(); } catch (UnauthorizedAccessException) { statusLabel.Text = " Access denied: " + tag; lastStatus = statusLabel.Text; return; } catch { }
                string currentBefore = ActiveList == leftList? leftCurrent : rightCurrent; bool isUp = ActiveList.SelectedItems[0].Text == "[..]"; string toSelect = isUp? currentBefore : null;
                LoadFolder(ActiveList, ActiveList == leftList? leftPath : rightPath, tag, toSelect);
            }
            else if (File.Exists(tag)) { try { Process.Start(new ProcessStartInfo(tag) { UseShellExecute = true }); } catch (Exception ex) { statusLabel.Text = " Ne mogu da otvorim: " + ex.Message; lastStatus = statusLabel.Text; } }
        }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            var focused = this.ActiveControl; bool isTyping = GetFocusedTextBox()!= null || focused is ComboBox; if (isTyping) return;
            if (e.Alt && e.KeyCode == Keys.F7) { OpenSearchDialog(); e.Handled = true; return; }
            if (e.Alt && !e.Control && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z) { JumpToFirst((char)e.KeyCode); e.Handled = true; return; }
            if (e.Alt && !e.Control && e.KeyCode >= Keys.D0 && e.KeyCode <= Keys.D9) { JumpToFirst((char)('0' + (e.KeyCode - Keys.D0))); e.Handled = true; return; }
            if (e.KeyCode == Keys.Escape && !string.IsNullOrEmpty(filterText)) { ClearFilter(); e.Handled = true; return; }
            if (e.KeyCode == Keys.Back && !string.IsNullOrEmpty(filterText)) { if (filterText.Length > 1) ApplyFilter(ActiveList, filterText.Substring(0, filterText.Length - 1)); else ClearFilter(); e.Handled = true; return; }
            if (e.Shift && e.KeyCode == Keys.F6) { RenameSelected(); e.Handled = true; return; }
            switch (e.KeyCode)
            {
                case Keys.Back: var box = ActiveList == leftList? leftPath : rightPath; var parent = Directory.GetParent(box.Text); if (parent!= null) LoadFolder(ActiveList, box, parent.FullName, box.Text); break;
                case Keys.F4: OpenInNotepad(); break; case Keys.F5: CopySelected(); break; case Keys.F6: MoveSelected(); break;
                case Keys.F7: CreateFolder(); break; case Keys.F3: RunCompareTool(); break; case Keys.F8: CopyPathToClipboard(); break;
                case Keys.F1: cmdBox.Focus(); cmdBox.SelectAll(); e.Handled = true; break;
            }
            if (e.Control && e.KeyCode == Keys.C) { ClipboardCopyFiles(); e.Handled = true; return; }
            if (e.Control && e.KeyCode == Keys.V) { ClipboardPasteFiles(); e.Handled = true; return; }
        }
        private void ClearFilter()
        {
            ListView lv = filterList ?? ActiveList;
            if (lv == null) return;
            var cache = lv == leftList? leftFullCache : rightFullCache;
            if (cache != null){ lv.BeginUpdate(); lv.Items.Clear(); foreach(var it in cache) lv.Items.Add(it); lv.EndUpdate(); }
            lv.BackColor = ColorBg;
            if (lv == leftList) leftFullCache = null; else rightFullCache = null;
            filterText = ""; filterList = null;
            statusLabel.Text = ""; lastStatus = ""; UpdateStatus();
            lv.Invalidate();
        }
        private void ApplyFilter(ListView list, string prefix)
        {
            if (string.IsNullOrEmpty(prefix)) { ClearFilter(); return; }
            if (list==null) list=ActiveList;
            var cache = list==leftList? leftFullCache : rightFullCache;
            if (cache==null){ var all=new List<ListViewItem>(); foreach(ListViewItem it in list.Items) all.Add(it); if(list==leftList) leftFullCache=all; else rightFullCache=all; cache=all; }
            list.BeginUpdate(); list.Items.Clear();
            ListViewItem up=null; foreach(var it in cache){ if(it.Text=="[..]"){ up=it; continue; } string name=it.Text.Trim('[',']',' '); if(name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) list.Items.Add(it); }
            if(up!=null) list.Items.Insert(0,up);
            list.EndUpdate();
            list.BackColor=ColorFilterBg; filterList=list; filterText=prefix;
            statusLabel.Text=$" FILTER: {prefix} ({list.Items.Count}) - ESC gasi"; lastStatus=statusLabel.Text;
            if(list.Items.Count>0){ int idx=(list.Items[0].Text=="[..]" && list.Items.Count>1)?1:0; list.SelectedItems.Clear(); list.Items[idx].Selected=true; list.Items[idx].Focused=true; list.EnsureVisible(idx); }
            list.Focus(); lastActive=list;
        }
        private void JumpToFirst(char c)
        {
            string ch=c.ToString().ToLowerInvariant();
            if(filterList!=null && filterList!=ActiveList) ClearFilter();
            string nf=(filterList==ActiveList && !string.IsNullOrEmpty(filterText))? filterText+ch : ch;
            ApplyFilter(ActiveList,nf);
        }

        private bool IsSubPath(string basePath, string subPath)
        {
            try
            {
                string baseFull = Path.GetFullPath(basePath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                string subFull = Path.GetFullPath(subPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
                return subFull.StartsWith(baseFull, StringComparison.OrdinalIgnoreCase) && !string.Equals(baseFull, subFull, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }
        private void SafeDeleteFile(string p){ try{ if(File.Exists(p)){ File.SetAttributes(p, FileAttributes.Normal); File.Delete(p);} }catch{} }
        private void SafeDeleteDir(string p){ try{ if(!Directory.Exists(p)) return; foreach(var f in Directory.GetFiles(p,"*",SearchOption.AllDirectories)){ try{ File.SetAttributes(f, FileAttributes.Normal);}catch{} } Directory.Delete(p,true);}catch{} }
        private long GetTotalSize(System.Collections.Generic.IEnumerable<string> paths)
        {
            long total=0;
            foreach(var p in paths)
            {
                try{
                    if(File.Exists(p)) total+= new FileInfo(p).Length;
                    else if(Directory.Exists(p)) total+= Directory.GetFiles(p,"*",SearchOption.AllDirectories).Sum(f=> { try{ return new FileInfo(f).Length;}catch{ return 0; } });
                }catch{}
            }
            return total;
        }
        private long GetFreeSpace(string destDir){ try{ var di=new DriveInfo(Path.GetPathRoot(destDir)); return di.AvailableFreeSpace; }catch{ return long.MaxValue; } }
        private void CopyFileWithProgress(string src, string dst, Action<long> onBytes)
        {
            const int buf=1024*1024;
            using(var sIn=new FileStream(src,FileMode.Open,FileAccess.Read,FileShare.Read))
            using(var sOut=new FileStream(dst,FileMode.Create,FileAccess.Write,FileShare.None))
            { byte[] b=new byte[buf]; int r; while((r=sIn.Read(b,0,b.Length))>0){ sOut.Write(b,0,r); onBytes?.Invoke(r); } }
        }
        private void CopyDirectoryWithProgress(string src, string dest, Action<long> onBytes, Action<string> onFile, Func<bool> isCanceled)
        {
            if(string.Equals(src,dest,StringComparison.OrdinalIgnoreCase)) return;
            if(IsSubPath(src,dest)) throw new IOException($"Ne mogu kopirati folder u sopstveni podfolder: {src} -> {dest}");
            Directory.CreateDirectory(dest);
            foreach(var f in Directory.GetFiles(src))
            {
                if(isCanceled!=null && isCanceled()) throw new OperationCanceledException();
                string df=Path.Combine(dest, Path.GetFileName(f));
                onFile?.Invoke(Path.GetFileName(f));
                if(File.Exists(df)) SafeDeleteFile(df);
                CopyFileWithProgress(f,df,onBytes);
            }
            foreach(var d in Directory.GetDirectories(src))
                CopyDirectoryWithProgress(d, Path.Combine(dest, Path.GetFileName(d)), onBytes, onFile, isCanceled);
        }
        private void CopyDirectory(string src, string dest){ CopyDirectoryWithProgress(src,dest,null,null,null); }
        private void MoveDirectory(string src, string dest)
        {
            if(string.Equals(src,dest,StringComparison.OrdinalIgnoreCase)) return;
            if(IsSubPath(src,dest)) throw new IOException($"Ne mogu premestiti folder u sopstveni podfolder: {src} -> {dest}");
            if(string.Equals(Path.GetPathRoot(src), Path.GetPathRoot(dest), StringComparison.OrdinalIgnoreCase))
            { try{ Directory.Move(src,dest); return; }catch{} }
            CopyDirectoryWithProgress(src,dest,null,null,null);
            SafeDeleteDir(src);
        }

        private void SafeClipboard(string text)
        {
            try { Clipboard.SetText(text); } catch (ThreadStateException) { var t = new Thread(() => { try { Clipboard.SetText(text); } catch { } }); t.SetApartmentState(ApartmentState.STA); t.Start(); t.Join(); } catch { }
        }
        private void CopyPathToClipboard()
        {
            if (ActiveList.SelectedItems.Count == 0) return; var item = ActiveList.SelectedItems[0]; string path = item.Tag as string;
            if (item.Text == "[..]") path = ActiveList == leftList? leftCurrent : rightCurrent; if (string.IsNullOrEmpty(path)) return;
            SafeClipboard(path); statusLabel.Text = " Kopirano: " + path; lastStatus = statusLabel.Text;
        }
        private void OpenInNotepad() { if (ActiveList.SelectedItems.Count == 0) return; var path = ActiveList.SelectedItems[0].Tag as string; if (itemIsFile(path)) { try { Process.Start("notepad.exe", "\"" + path + "\""); } catch { } } }
        private bool itemIsFile(string p) { return!string.IsNullOrEmpty(p) && File.Exists(p); }

        // ========== FIX: JEDAN DIJALOG SA SPISKOM + OVERWRITE OPCIJA ==========
        // Vraca: 0=Cancel, 1=Da/OverwriteAll, 2=Ne/Preskoci postojece
                private int ConfirmCopyMoveEx(string operation, string destDir, List<string> files, out bool hasCollision)
        {
            var existing = files.Where(f => File.Exists(Path.Combine(destDir, Path.GetFileName(f))) || Directory.Exists(Path.Combine(destDir, Path.GetFileName(f)))).ToList();
            hasCollision = existing.Count > 0;

            using (Form f = new Form()
            {
                Width = 600, Height = 420,
                Text = operation,
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false, MaximizeBox = false
            })
            {
                string lblText = hasCollision
                   ? $"{operation} {files.Count} u:\n{destDir}\nPAZNJA: {existing.Count} vec postoji!"
                    : $"{operation} {files.Count} u:\n{destDir}";

                Label lbl = new Label() { Left = 10, Top = 10, Width = 560, Height = 50, Text = lblText };
                ListBox lb = new ListBox() { Left = 10, Top = 65, Width = 560, Height = 275, BackColor = Color.FromArgb(25, 25, 25), ForeColor = Color.White };
                lb.Items.AddRange(files.Select(p => (existing.Contains(p)? "[POSTOJI] " : "") + Path.GetFileName(p) + " (" + p + ")").ToArray());

                Button btnYes = new Button() { Text = "Da - pregazi SVE", Left = 10, Top = 350, Width = 150, DialogResult = DialogResult.Yes, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
                Button btnNo = new Button() { Text = "Ne - preskoci postojece", Left = 170, Top = 350, Width = 170, DialogResult = DialogResult.No };
                Button btnCancel = new Button() { Text = "Otkazi", Left = 480, Top = 350, Width = 90, DialogResult = DialogResult.Cancel };
                Button btnYesSingle = new Button() { Text = "Da", Left = 380, Top = 350, Width = 90, DialogResult = DialogResult.Yes, BackColor = Color.FromArgb(0, 122, 204), ForeColor = Color.White };
                Button btnNoSingle = new Button() { Text = "Ne", Left = 480, Top = 350, Width = 90, DialogResult = DialogResult.No };

                if (hasCollision)
                {
                    f.Controls.AddRange(new Control[] { lbl, lb, btnYes, btnNo, btnCancel });
                    f.AcceptButton = btnYes;
                    f.CancelButton = btnCancel;
                }
                else
                {
                    f.Controls.AddRange(new Control[] { lbl, lb, btnYesSingle, btnNoSingle });
                    f.AcceptButton = btnYesSingle;
                    f.CancelButton = btnNoSingle;
                }

                DarkTheme.Apply(f); DarkTheme.DarkScroll(lb);

                var res = f.ShowDialog(this);
                if (res == DialogResult.Yes) return 1;
                if (res == DialogResult.No) return hasCollision? 2 : 0;
                return 0;
            }
        }

        private async void CopySelected()
        {
            var list = ActiveList; string destDir = list == leftList? rightCurrent : leftCurrent;
            if (!Directory.Exists(destDir)) { MessageBox.Show("Destinacija ne postoji: " + destDir); return; }
            List<string> toCopy;
            if (ActiveMarked.Count > 0) toCopy = ActiveMarked.ToList();
            else
            {
                if (list.SelectedItems.Count == 0) return;
                var sel = list.SelectedItems[0].Tag as string; if (string.IsNullOrEmpty(sel) || sel.EndsWith("..")) return;
                toCopy = new List<string> { sel };
            }
            toCopy = toCopy.Where(p => !IsSubPath(p, Path.Combine(destDir, Path.GetFileName(p)))).ToList();
            if (toCopy.Count==0){ MessageBox.Show("Ne mogu kopirati folder u sopstveni podfolder."); return; }

            long totalSize = GetTotalSize(toCopy);
            long free = GetFreeSpace(destDir);
            if (totalSize > 0 && free != long.MaxValue && totalSize > free)
            {
                if (MessageBox.Show("Nema dovoljno mesta!\nPotrebno: " + FormatSize(totalSize) + 
                    "Slobodno: " + FormatSize(free) + "\nNastavi?", "Provera mesta", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            }

            bool hasCollision; int choice = ConfirmCopyMoveEx("F5 Kopirati", destDir, toCopy, out hasCollision);
            if (choice == 0) return;
            bool overwriteAll = (choice == 1 && hasCollision);
            bool skipAll = (choice == 2);

            var prog = new CopyProgressForm("Kopiranje...", toCopy.Count);
            long doneBytes = 0;
            int copied = 0, skipped = 0;
            var cts = new System.Threading.CancellationTokenSource();
            var progTask = System.Threading.Tasks.Task.Run(()=>{ prog.ShowDialog(this); });

            try
            {
                foreach (var src in toCopy)
                {
                    if (cts.IsCancellationRequested || prog.Canceled) break;
                    try
                    {
                        string dest = Path.Combine(destDir, Path.GetFileName(src));
                        bool exists = File.Exists(dest) || Directory.Exists(dest);
                        if (exists && skipAll) { skipped++; continue; }
                        if (exists && !overwriteAll) { skipped++; continue; }

                        if (File.Exists(src))
                        {
                            if (File.Exists(dest)) SafeDeleteFile(dest);
                            await System.Threading.Tasks.Task.Run(() => CopyFileWithProgress(src, dest, b => { doneBytes+=b; if(totalSize>0) { try{ if(prog.InvokeRequired) prog.BeginInvoke(new Action(()=>{ prog.bar.Value=(int)Math.Min(100, (double)doneBytes*100/totalSize); prog.lbl.Text=Path.GetFileName(src); })); else { prog.bar.Value=(int)Math.Min(100, (double)doneBytes*100/totalSize); prog.lbl.Text=Path.GetFileName(src); } }catch{} } }), cts.Token);
                        }
                        else if (Directory.Exists(src))
                        {
                            if (Directory.Exists(dest) && overwriteAll) SafeDeleteDir(dest);
                            await System.Threading.Tasks.Task.Run(() => CopyDirectoryWithProgress(src, dest, b => { doneBytes+=b; if(totalSize>0) { try{ if(prog.InvokeRequired) prog.BeginInvoke(new Action(()=>{ prog.bar.Value=(int)Math.Min(100, (double)doneBytes*100/totalSize); prog.lbl.Text=Path.GetFileName(src); })); else { prog.bar.Value=(int)Math.Min(100, (double)doneBytes*100/totalSize); prog.lbl.Text=Path.GetFileName(src); } }catch{} } }, f=>{}, ()=> prog.Canceled || cts.IsCancellationRequested), cts.Token);
                        }
                        copied++;
                    }
                    catch (OperationCanceledException){ break; }
                    catch (Exception ex) { MessageBox.Show("Greska " + src + ": " + ex.Message); }
                }
            }
            finally
            {
                try{ prog.Invoke(new Action(()=>prog.Close())); }catch{} 
                try{ await progTask; }catch{}
                prog.Dispose();
            }

            markedLeft.Clear(); markedRight.Clear();
            LoadFolder(leftList, leftPath, leftCurrent, null);
            LoadFolder(rightList, rightPath, rightCurrent, null);
            UpdateStatus();
            statusLabel.Text = $" Kopirano {copied}/{toCopy.Count} (preskoceno {skipped}) u {destDir}"; lastStatus = statusLabel.Text;
        }

        private async void MoveSelected()
        {
            var list = ActiveList; string destDir = list == leftList? rightCurrent : leftCurrent;
            if (!Directory.Exists(destDir)) { MessageBox.Show("Destinacija ne postoji: " + destDir); return; }
            List<string> toMove;
            if (ActiveMarked.Count > 0) toMove = ActiveMarked.ToList();
            else
            {
                if (list.SelectedItems.Count == 0) return;
                var sel = list.SelectedItems[0].Tag as string; if (string.IsNullOrEmpty(sel) || sel.EndsWith("..")) return;
                toMove = new List<string> { sel };
            }
            toMove = toMove.Where(p => !IsSubPath(p, Path.Combine(destDir, Path.GetFileName(p)))).ToList();
            if (toMove.Count==0){ MessageBox.Show("Ne mogu premestiti folder u sopstveni podfolder."); return; }

            long totalSize = GetTotalSize(toMove);
            long free = GetFreeSpace(destDir);
            if (totalSize > 0 && free != long.MaxValue && totalSize > free)
            {
                if (MessageBox.Show("Nema dovoljno mesta!\nPotrebno: " + FormatSize(totalSize) + 
                    "\nSlobodno: " + FormatSize(free) + "\nNastavi?", "Provera mesta", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
            }

            bool hasCollision; int choice = ConfirmCopyMoveEx("F6 Premesti", destDir, toMove, out hasCollision);
            if (choice == 0) return;
            bool overwriteAll = (choice == 1 && hasCollision);
            bool skipAll = (choice == 2);

            var prog = new CopyProgressForm("Premestanje...", toMove.Count);
            int moved = 0, skipped = 0;
            var cts = new System.Threading.CancellationTokenSource();
            var progTask = System.Threading.Tasks.Task.Run(()=>{ prog.ShowDialog(this); });

            try
            {
                foreach (var src in toMove)
                {
                    if (cts.IsCancellationRequested || prog.Canceled) break;
                    try
                    {
                        string dest = Path.Combine(destDir, Path.GetFileName(src));
                        bool exists = File.Exists(dest) || Directory.Exists(dest);
                        if (exists && skipAll) { skipped++; continue; }
                        if (exists && !overwriteAll) { skipped++; continue; }

                        if (File.Exists(src))
                        {
                            if (File.Exists(dest)) SafeDeleteFile(dest);
                            await System.Threading.Tasks.Task.Run(() =>
                            {
                                if (string.Equals(Path.GetPathRoot(src), Path.GetPathRoot(dest), StringComparison.OrdinalIgnoreCase))
                                    File.Move(src, dest);
                                else { File.Copy(src, dest, true); SafeDeleteFile(src); }
                            }, cts.Token);
                        }
                        else if (Directory.Exists(src))
                        {
                            if (Directory.Exists(dest) && overwriteAll) SafeDeleteDir(dest);
                            await System.Threading.Tasks.Task.Run(() => MoveDirectory(src, dest), cts.Token);
                        }
                        moved++;
                    }
                    catch (OperationCanceledException){ break; }
                    catch (Exception ex) { MessageBox.Show("Greska pri premestanju " + src + ": " + ex.Message); }
                }
            }
            finally
            {
                try{ prog.Invoke(new Action(()=>prog.Close())); }catch{} 
                try{ await progTask; }catch{}
                prog.Dispose();
            }

            markedLeft.Clear(); markedRight.Clear();
            LoadFolder(leftList, leftPath, leftCurrent, null);
            LoadFolder(rightList, rightPath, rightCurrent, null);
            UpdateStatus();
            statusLabel.Text = $" Premesteno {moved}/{toMove.Count} (preskoceno {skipped}) u {destDir}"; lastStatus = statusLabel.Text;
        }


        private void CreateFolder()
        {
            var box = ActiveList == leftList? leftPath : rightPath; string input = Prompt.Show("Ime foldera:", "F7", "Novi folder"); if (string.IsNullOrWhiteSpace(input)) return;
            try { string full = Path.Combine(box.Text, input); Directory.CreateDirectory(full); LoadFolder(ActiveList, box, box.Text, full); } catch (Exception ex) { MessageBox.Show("Greska pri kreiranju foldera: " + ex.Message); }
        }
        private void RenameSelected()
        {
            if (ActiveList.SelectedItems.Count == 0) return; var item = ActiveList.SelectedItems[0]; string oldPath = item.Tag as string; if (string.IsNullOrEmpty(oldPath)) return; if (item.Text == "[..]") return;
            string oldName = Path.GetFileName(oldPath); string dir = Path.GetDirectoryName(oldPath); string newName = ShowRenameDialog(oldName); if (string.IsNullOrWhiteSpace(newName)) return; if (newName == oldName) return;
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) { MessageBox.Show("Ime sadrzi nedozvoljene karaktere: " + new string(Path.GetInvalidFileNameChars())); return; }
            string newPath = Path.Combine(dir, newName); if (File.Exists(newPath) || Directory.Exists(newPath)) { MessageBox.Show("Vec postoji fajl/folder sa tim imenom: " + newPath); return; }
            try { if (File.Exists(oldPath)) File.Move(oldPath, newPath); else if (Directory.Exists(oldPath)) Directory.Move(oldPath, newPath); else return; var box = ActiveList == leftList? leftPath : rightPath; LoadFolder(ActiveList, box, box.Text, newPath); } catch (Exception ex) { MessageBox.Show("Greska pri preimenovanju: " + ex.Message); }
        }
        private string ShowRenameDialog(string oldName)
        {
            using (Form prompt = new Form() { Width = 420, Height = 150, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Shift+F6 Rename", StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false })
            {
                Label textLabel = new Label() { Left = 10, Top = 10, Width = 390, Text = "Novo ime:" };
                TextBox textBox = new TextBox() { Left = 10, Top = 35, Width = 380, Text = oldName };
                Button confirmation = new Button() { Text = "Ok", Left = 210, Width = 85, Top = 70, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Cancel", Left = 305, Width = 85, Top = 70, DialogResult = DialogResult.Cancel };
                prompt.Controls.AddRange(new Control[] { textBox, confirmation, cancel, textLabel }); prompt.AcceptButton = confirmation; prompt.CancelButton = cancel;
                DarkTheme.Apply(prompt); prompt.Shown += (s, e) => { textBox.Focus(); textBox.SelectAll(); };
                return prompt.ShowDialog(this) == DialogResult.OK? textBox.Text.Trim() : "";
            }
        }
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
                if (f.ShowDialog(this) == DialogResult.OK) { compareToolPath = txtPath.Text.Trim(); compareToolParams = txtParams.Text.Trim(); SaveTabsToTxt(); statusLabel.Text = " Compare tool sacuvan: " + compareToolPath; lastStatus = statusLabel.Text; }
            }
        }
        private void RunCompareTool()
        {
            if (string.IsNullOrWhiteSpace(compareToolPath) ||!File.Exists(compareToolPath)) { var res = MessageBox.Show("Compare tool nije podesen ili ne postoji:\n" + compareToolPath + "\n\nDa otvoris podesavanja?", "F3 Compare", MessageBoxButtons.YesNo); if (res == DialogResult.Yes) SetupCompareTool(); return; }
            string fileA = null; string fileB = null;
            if (markedLeft.Count == 1 && markedRight.Count == 1) { fileA = markedLeft.First(); fileB = markedRight.First(); }
            else if (markedLeft.Count == 2 && markedRight.Count == 0) { var arr = markedLeft.ToArray(); fileA = arr[0]; fileB = arr[1]; }
            else if (markedRight.Count == 2 && markedLeft.Count == 0) { var arr = markedRight.ToArray(); fileA = arr[0]; fileB = arr[1]; }
            else { string leftSel = leftList.SelectedItems.Count > 0? leftList.SelectedItems[0].Tag as string : null; string rightSel = rightList.SelectedItems.Count > 0? rightList.SelectedItems[0].Tag as string : null; if (leftSel!= null && rightSel!= null && leftSel!= rightSel &&!leftSel.EndsWith("..") &&!rightSel.EndsWith("..")) { if (File.Exists(leftSel) && File.Exists(rightSel)) { fileA = leftSel; fileB = rightSel; } } }
            if (fileA == null || fileB == null) { MessageBox.Show("Obelezi 2 fajla crveno (SPACE) - jedan u levom i jedan u desnom panelu, ili obelezi 2 fajla u istom panelu, pa pritisni F3.\n\nTrenutno: levo marked=" + markedLeft.Count + " desno marked=" + markedRight.Count, "F3 Compare"); return; }
            if (!File.Exists(fileA) ||!File.Exists(fileB)) { MessageBox.Show("Compare radi samo sa fajlovima, ne sa folderima.\n" + fileA + "\n" + fileB); return; }
            try
            {
                string args = ""; if (string.IsNullOrWhiteSpace(compareToolParams)) args = "\"" + fileA + "\" \"" + fileB + "\"";
                else { if (compareToolParams.Contains("%1") || compareToolParams.Contains("%2")) args = compareToolParams.Replace("%1", "\"" + fileA + "\"").Replace("%2", "\"" + fileB + "\""); else args = compareToolParams + " \"" + fileA + "\" \"" + fileB + "\""; }
                Process.Start(new ProcessStartInfo(compareToolPath, args) { UseShellExecute = true }); statusLabel.Text = " Compare: " + Path.GetFileName(fileA) + " vs " + Path.GetFileName(fileB); lastStatus = statusLabel.Text;
            }
            catch (Exception ex) { MessageBox.Show("Greska pri pokretanju compare tool-a: " + ex.Message + "\n\nPutanja: " + compareToolPath + "\nArgs: " + compareToolParams); }
        }
        private void OpenSearchDialog()
        {
            string root = ActiveList == leftList? leftCurrent : rightCurrent;
            using (Form dlg = new Form() { Width = 450, Height = 200, FormBorderStyle = FormBorderStyle.FixedDialog, Text = "Alt+F7 Pretraga - u: " + root, StartPosition = FormStartPosition.CenterParent, MinimizeBox = false, MaximizeBox = false, ShowInTaskbar = false })
            {
                Label lbl = new Label() { Left = 10, Top = 15, Width = 400, Text = "Trazi (npr *.cs, *form*, MiniTC):" };
                TextBox txt = new TextBox() { Left = 10, Top = 35, Width = 410, Text = "" };
                CheckBox chkSub = new CheckBox() { Left = 10, Top = 65, Width = 180, Text = "Pretrazi podfoldere", Checked = true };
                CheckBox chkCase = new CheckBox() { Left = 200, Top = 65, Width = 180, Text = "Case sensitive", Checked = false };
                Button ok = new Button() { Text = "Trazi", Left = 240, Top = 100, Width = 80, DialogResult = DialogResult.OK };
                Button cancel = new Button() { Text = "Otkazi", Left = 330, Top = 100, Width = 90, DialogResult = DialogResult.Cancel };
                dlg.Controls.AddRange(new Control[] { lbl, txt, chkSub, chkCase, ok, cancel }); dlg.AcceptButton = ok; dlg.CancelButton = cancel; DarkTheme.Apply(dlg); dlg.Shown += (s, e) => txt.Focus();
                if (dlg.ShowDialog(this) == DialogResult.OK) { string pattern = txt.Text.Trim(); if (string.IsNullOrWhiteSpace(pattern)) return; ShowSearchResults(pattern, root, chkSub.Checked, chkCase.Checked); }
            }
        }
        private bool IsSearchMatch(string fileName, string pattern, bool caseSensitive)
        {
            try
            {
                StringComparison comp = caseSensitive? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (pattern.Contains("*") || pattern.Contains("?")) { string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$"; var opts = caseSensitive? RegexOptions.None : RegexOptions.IgnoreCase; return Regex.IsMatch(fileName, regexPattern, opts); }
                else { return fileName.IndexOf(pattern, comp) >= 0; }
            }
            catch { return false; }
        }
        private void ShowSearchResults(string pattern, string root, bool includeSub, bool caseSensitive)
        {
            Form resForm = new Form() { Width = 900, Height = 600, Text = "Rezultati pretrage za '" + pattern + "' u " + root + " - DblClick/Enter locira fajl", StartPosition = FormStartPosition.CenterParent, WindowState = FormWindowState.Maximized, MinimizeBox = false, MaximizeBox = true };
            ListView lv = new ListView() { Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, HideSelection = false, MultiSelect = false, BackColor = ColorBg, ForeColor = ColorFg, Font = listFont };
            lv.Columns.Add("Ime", 250); lv.Columns.Add("Putanja", 400); lv.Columns.Add("Velicina", 80); lv.Columns.Add("Datum", 130);
            Action fitColumns = () => { int w = lv.ClientSize.Width - SystemInformation.VerticalScrollBarWidth; if (w <= 0) return; int c0 = (int)(w * 0.25); int c1 = (int)(w * 0.50); int c2 = (int)(w * 0.10); lv.Columns[0].Width = c0; lv.Columns[1].Width = c1; lv.Columns[2].Width = c2; lv.Columns[3].Width = w - c0 - c1 - c2; };
            lv.ClientSizeChanged += (s, e) => fitColumns(); lv.SizeChanged += (s, e) => fitColumns();
            Label lblStatus = new Label() { Dock = DockStyle.Top, Height = 24, BackColor = ColorToolbarBg, ForeColor = ColorStatusFg, Text = " Pretraga u toku: " + root + " za '" + pattern + "'..." };
            Label lblCount = new Label() { Dock = DockStyle.Bottom, Height = 24, BackColor = ColorToolbarBg, ForeColor = ColorStatusFg, Text = " Pronadjeno: 0" };
            Panel bottomPanel = new Panel() { Dock = DockStyle.Bottom, Height = 40, BackColor = ColorToolbarBg };
            Button btnGoto = new Button() { Text = "Idi na fajl (Enter)", Left = 10, Top = 8, Width = 170, BackColor = ColorGotoBtnBg, ForeColor = Color.White };
            Button btnOpen = new Button() { Text = "Otvori", Left = 190, Top = 8, Width = 90 };
            Button btnSelectAll = new Button() { Text = "Selektuj sve u panelu", Left = 290, Top = 8, Width = 200 };
            Button btnClose = new Button() { Text = "Zatvori (Esc)", Left = 780, Top = 8, Width = 130, DialogResult = DialogResult.Cancel };
            bottomPanel.Controls.AddRange(new Control[] { btnGoto, btnOpen, btnSelectAll, btnClose }); resForm.CancelButton = btnClose;
            bottomPanel.Resize += (s, e) => { btnClose.Left = bottomPanel.ClientSize.Width - btnClose.Width - 10; };
            resForm.Controls.Add(lv); resForm.Controls.Add(bottomPanel); resForm.Controls.Add(lblCount); resForm.Controls.Add(lblStatus); DarkTheme.Apply(resForm); DarkTheme.DarkScroll(lv);
            var foundFiles = new List<string>(); var cts = new CancellationTokenSource(); resForm.FormClosing += (s, e) => { cts.Cancel(); };
            Action<Action> ui = (a) => { try { if (!resForm.IsDisposed && resForm.IsHandleCreated) resForm.BeginInvoke(a); } catch { } };
            Action<string> goToFile = (fullPath) => { try { if (File.Exists(fullPath)) { string dir = Path.GetDirectoryName(fullPath); LoadFolder(ActiveList, ActiveList == leftList? leftPath : rightPath, dir, fullPath); resForm.Close(); ActiveList.Focus(); } else if (Directory.Exists(fullPath)) { LoadFolder(ActiveList, ActiveList == leftList? leftPath : rightPath, fullPath, null); resForm.Close(); ActiveList.Focus(); } } catch (Exception ex) { MessageBox.Show("Greska pri lociranju: " + ex.Message); } };
            lv.DoubleClick += (s, e) => { if (lv.SelectedItems.Count > 0) { string p = lv.SelectedItems[0].Tag as string; goToFile(p); } };
            lv.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter && lv.SelectedItems.Count > 0) { string p = lv.SelectedItems[0].Tag as string; goToFile(p); e.SuppressKeyPress = true; } };
            btnGoto.Click += (s, e) => { if (lv.SelectedItems.Count > 0) { string p = lv.SelectedItems[0].Tag as string; goToFile(p); } };
            btnSelectAll.Click += (s, e) =>
            {
                try
                {
                    var files = new List<string>(); lock (foundFiles) { foreach (var f in foundFiles) if (File.Exists(f)) files.Add(f); }
                    if (files.Count == 0) { MessageBox.Show("Nema pronadjenih fajlova za selektovanje."); return; }
                    var list = ActiveList; var marked = ActiveMarked; string first = files[0];
                    LoadFolder(list, list == leftList? leftPath : rightPath, Path.GetDirectoryName(first), first);
                    foreach (var f in files) marked.Add(f); int n = files.Count; resForm.Close(); list.Focus(); list.Invalidate(); UpdateStatus(); statusLabel.Text = " Selektovano fajlova iz pretrage: " + n; lastStatus = statusLabel.Text;
                }
                catch (Exception ex) { MessageBox.Show("Greska pri selektovanju: " + ex.Message); }
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
                        while (stack.Count > 0 &&!token.IsCancellationRequested && count < 1000)
                        {
                            string currentDir = stack.Pop();
                            try
                            {
                                foreach (var file in Directory.EnumerateFiles(currentDir))
                                {
                                    if (token.IsCancellationRequested || count >= 1000) break;
                                    try
                                    {
                                        string name = Path.GetFileName(file);
                                        if (IsSearchMatch(name, pattern, caseSensitive))
                                        {
                                            lock (foundFiles) { foundFiles.Add(file); } count++; int cnt = count; string f = file;
                                            ui(() => { try { var fi = new FileInfo(f); var item = new ListViewItem(fi.Name) { Tag = f }; item.SubItems.Add(Path.GetDirectoryName(f)); item.SubItems.Add(FormatSize(fi.Length)); item.SubItems.Add(fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm")); lv.Items.Add(item); lblCount.Text = " Pronadjeno: " + lv.Items.Count + (cnt >= 1000? " (limit 1000)" : ""); } catch { } });
                                        }
                                    }
                                    catch { }
                                }
                                if (includeSub)
                                {
                                    foreach (var dir in Directory.EnumerateDirectories(currentDir))
                                    {
                                        if (token.IsCancellationRequested) break;
                                        try
                                        {
                                            var di = new DirectoryInfo(dir); if ((di.Attributes & FileAttributes.Hidden)!= 0) continue; if ((di.Attributes & FileAttributes.ReparsePoint)!= 0) continue;
                                            string dirName = Path.GetFileName(dir);
                                            if (IsSearchMatch(dirName, pattern, caseSensitive)) { lock (foundFiles) { foundFiles.Add(dir); } count++; string d = dir; string dn = dirName; DateTime lw = di.LastWriteTime; ui(() => { try { var item = new ListViewItem("[" + dn + "]") { Tag = d }; item.SubItems.Add(Path.GetDirectoryName(d)); item.SubItems.Add("<DIR>"); item.SubItems.Add(lw.ToString("dd.MM.yyyy HH:mm")); lv.Items.Add(item); lblCount.Text = " Pronadjeno: " + lv.Items.Count; } catch { } }); }
                                            stack.Push(dir);
                                        }
                                        catch { }
                                    }
                                }
                                string cd = currentDir; int c2 = count; ui(() => { lblStatus.Text = " Pretraga: " + cd + " | nadjeno: " + c2; });
                            }
                            catch { }
                        }
                        ui(() => { int total; lock (foundFiles) { total = foundFiles.Count; } lblStatus.Text = " Pretraga zavrsena u " + root + " | ukupno: " + total + " rezultata za '" + pattern + "' (Esc za zatvaranje, Enter/DblClick za lociranje)"; lblCount.Text = " Pronadjeno: " + total + (total >= 1000? " (limit 1000)" : "") + " | DblClick ili Enter locira fajl u panelu"; if (lv.Items.Count > 0) { lv.Items[0].Selected = true; lv.Items[0].Focused = true; lv.Focus(); } });
                    }
                    catch (Exception ex) { string msg = ex.Message; ui(() => { lblStatus.Text = " Greska: " + msg; }); }
                });
            };
            resForm.ShowDialog(this); resForm.Dispose();
        }
        private string FormatSize(long bytes) { if (bytes < 1024) return bytes + " B"; if (bytes < 1024 * 1024) return (bytes / 1024) + " KB"; return (bytes / (1024 * 1024)) + " MB"; }
        private List<string> _clipboardCutList = null; private bool _isCutOperation = false;
        private List<string> GetFilesToClipboard()
        {
            List<string> toCopy; if (ActiveMarked.Count > 0) toCopy = ActiveMarked.ToList();
            else { if (ActiveList.SelectedItems.Count == 0) return new List<string>(); var sel = ActiveList.SelectedItems[0].Tag as string; if (string.IsNullOrEmpty(sel) || sel.EndsWith("..")) return new List<string>(); toCopy = new List<string> { sel }; } return toCopy;
        }
        private void SetClipboardFiles(List<string> files, bool cut)
        {
            var sc = new System.Collections.Specialized.StringCollection(); foreach (var f in files) sc.Add(f);
            var data = new DataObject(); data.SetFileDropList(sc); data.SetData("Preferred DropEffect", new MemoryStream(new byte[] { (byte)(cut? 2 : 5), 0, 0, 0 })); Clipboard.SetDataObject(data, true, 10, 100);
        }
        private void ClipboardCopyFiles()
        {
            var files = GetFilesToClipboard(); if (files.Count == 0) return;
            try { SetClipboardFiles(files, false); _isCutOperation = false; _clipboardCutList = null; ActiveMarked.Clear(); ActiveList.Invalidate(); UpdateStatus(); statusLabel.Text = " Clipboard COPY: " + files.Count + " stavki | Ctrl+V za paste u " + (ActiveList == leftList? leftCurrent : rightCurrent); lastStatus = statusLabel.Text; } catch (Exception ex) { MessageBox.Show("Clipboard Copy greska: " + ex.Message); }
        }
        private void ClipboardCutFiles()
        {
            var files = GetFilesToClipboard(); if (files.Count == 0) return;
            try { SetClipboardFiles(files, true); _isCutOperation = true; _clipboardCutList = files.ToList(); ActiveMarked.Clear(); ActiveList.Invalidate(); UpdateStatus(); statusLabel.Text = " Clipboard CUT: " + files.Count + " stavki | Ctrl+V za premestanje u " + (ActiveList == leftList? leftCurrent : rightCurrent); lastStatus = statusLabel.Text; } catch (Exception ex) { MessageBox.Show("Clipboard Cut greska: " + ex.Message); }
        }
        private async void ClipboardPasteFiles()
        {
            try
            {
                System.Collections.Specialized.StringCollection fileList = null; bool isMove = false;
                try
                {
                    if (Clipboard.ContainsFileDropList())
                    {
                        fileList = Clipboard.GetFileDropList();
                        try
                        {
                            var data = Clipboard.GetDataObject();
                            if (data!= null && data.GetDataPresent("Preferred DropEffect"))
                            {
                                object raw = data.GetData("Preferred DropEffect"); var stream = raw as MemoryStream;
                                if (stream!= null) { byte[] bytes = stream.ToArray(); if (bytes.Length > 0 && (bytes[0] & 2)!= 0 && (bytes[0] & 1) == 0) isMove = true; }
                                else { var bytes2 = raw as byte[]; if (bytes2!= null && bytes2.Length > 0 && (bytes2[0] & 2)!= 0 && (bytes2[0] & 1) == 0) isMove = true; }
                            }
                        }
                        catch { }
                    }
                }
                catch { }
                if (fileList == null || fileList.Count == 0) { statusLabel.Text = " Clipboard prazan - nema fajlova za paste"; lastStatus = statusLabel.Text; return; }
                string destDir = ActiveList == leftList? leftCurrent : rightCurrent; var srcList = fileList.Cast<string>().ToList();
                if (isMove) { bool sameFolder = srcList.All(s => string.Equals(Path.GetDirectoryName(s), destDir.TrimEnd('\\') + (destDir.EndsWith(":\\")? "\\" : ""), StringComparison.OrdinalIgnoreCase) || string.Equals(Path.GetDirectoryName(s), destDir, StringComparison.OrdinalIgnoreCase)); if (sameFolder) { statusLabel.Text = " Vec si u istom folderu, nema potrebe za Move"; lastStatus = statusLabel.Text; return; } }
                string opName = isMove? "Premestiti" : "Kopirati";
                if (MessageBox.Show(opName + " " + srcList.Count + " stavki iz clipboard-a u\n" + destDir + "?", "Ctrl+V " + opName, MessageBoxButtons.YesNo)!= DialogResult.Yes) return;
                int okCount = 0;
                foreach (var src in srcList)
                {
                    if (!File.Exists(src) &&!Directory.Exists(src)) continue;
                    try
                    {
                        string dest = Path.Combine(destDir, Path.GetFileName(src)); if (string.Equals(src, dest, StringComparison.OrdinalIgnoreCase)) continue;
                        if (File.Exists(src)) { if (File.Exists(dest)) { var r = MessageBox.Show("Fajl vec postoji:\n" + dest + "\nPregaziti?", "Ctrl+V", MessageBoxButtons.YesNoCancel); if (r == DialogResult.Cancel) break; if (r!= DialogResult.Yes) continue; File.Delete(dest); } if (isMove) await Task.Run(() => File.Move(src, dest)); else await Task.Run(() => File.Copy(src, dest, true)); okCount++; }
                        else if (Directory.Exists(src)) { if (Directory.Exists(dest)) { var r = MessageBox.Show("Folder vec postoji:\n" + dest + "\nSpojiti/pregaziti sadrzaj?", "Ctrl+V", MessageBoxButtons.YesNoCancel); if (r == DialogResult.Cancel) break; if (r!= DialogResult.Yes) continue; } if (isMove) await Task.Run(() => MoveDirectory(src, dest)); else await Task.Run(() => CopyDirectory(src, dest)); okCount++; }
                    }
                    catch (Exception ex) { MessageBox.Show("Greska pri " + opName.ToLower() + " " + src + ": " + ex.Message); }
                }
                LoadFolder(leftList, leftPath, leftCurrent, null); LoadFolder(rightList, rightPath, rightCurrent, null); ActiveMarked.Clear(); UpdateStatus();
                if (okCount > 0) { try { Clipboard.Clear(); } catch { } _isCutOperation = false; _clipboardCutList = null; ActiveMarked.Clear(); ActiveList.Invalidate(); }
                statusLabel.Text = " " + opName + " zavrseno: " + okCount + "/" + srcList.Count + " u " + destDir; lastStatus = statusLabel.Text;
            }
            catch (Exception ex) { MessageBox.Show("Clipboard Paste greska: " + ex.Message); }
        }
        private void SequentialRenameFiles()
        {
            try
            {
                var list = ActiveList; if (list == null || list.Items.Count == 0) { MessageBox.Show("Panel je prazan.", "001 Rename"); return; }
                string curDir = list == leftList? leftCurrent : rightCurrent; if (string.IsNullOrEmpty(curDir) ||!Directory.Exists(curDir)) { MessageBox.Show("Trenutni folder ne postoji: " + curDir, "001 Rename"); return; }
                var filesInOrder = new List<string>(); foreach (ListViewItem it in list.Items) { if (it.Text == "[..]") continue; string p = it.Tag as string; if (string.IsNullOrEmpty(p)) continue; if (File.Exists(p)) filesInOrder.Add(p); }
                if (filesInOrder.Count == 0) { MessageBox.Show("Nema fajlova za preimenovanje u ovom folderu/panelu.", "001 Rename"); return; }
                string input = Prompt.Show("Unesi format pocetnog broja:\n" + "npr 000 = 3 cifre (000,001,002...)\n" + " 0001 = 4 cifre (0001,0002...)\n" + " 001 = krece od 1 sa 3 cifre\n\n" + "Broj fajlova: " + filesInOrder.Count + " u " + curDir, "Sekvencijalno preimenovanje", "000");
                if (string.IsNullOrWhiteSpace(input)) return; input = input.Trim(); int padLen; int startNum = 0; bool isAllZeros = input.Length > 0 && input.All(c => c == '0');
                if (isAllZeros) { padLen = input.Length; startNum = 0; }
                else { if (!input.All(char.IsDigit)) { MessageBox.Show("Unos mora biti broj (npr 000, 0001, 001, 3).", "001 Rename"); return; } if (input.Length == 1 && int.TryParse(input, out int single) && single >= 1 && single <= 9) { padLen = single; startNum = 0; } else { padLen = input.Length; int parsed; if (int.TryParse(input, out parsed)) startNum = parsed; } }
                if (padLen < 1) padLen = 3; if (padLen > 10) padLen = 10;
                var renamePairs = new List<Tuple<string, string>>(); var oldSet = new HashSet<string>(filesInOrder, StringComparer.OrdinalIgnoreCase);
                for (int i = 0; i < filesInOrder.Count; i++) { string oldPath = filesInOrder[i]; string ext = Path.GetExtension(oldPath); int num = startNum + i; string newName = num.ToString("D" + padLen) + ext; string newPath = Path.Combine(curDir, newName); renamePairs.Add(Tuple.Create(oldPath, newPath)); }
                var collisions = renamePairs.Where(p => File.Exists(p.Item2) &&!oldSet.Contains(p.Item2)).ToList();
                if (collisions.Count > 0) { string msg = "Sledeci ciljni fajlovi vec postoje i nisu deo preimenovanja:\n" + string.Join("\n", collisions.Take(10).Select(c => Path.GetFileName(c.Item2))) + (collisions.Count > 10? "\n... i jos " + (collisions.Count - 10) : "") + "\n\nDa nastavim i preskocim postojece (Yes) ili otkazem (No)?"; var dr = MessageBox.Show(msg, "001 Rename - kolizija", MessageBoxButtons.YesNo, MessageBoxIcon.Warning); if (dr!= DialogResult.Yes) return; }
                string preview = string.Join("\n", renamePairs.Take(10).Select(p => Path.GetFileName(p.Item1) + " -> " + Path.GetFileName(p.Item2))) + (renamePairs.Count > 10? "\n... i jos " + (renamePairs.Count - 10) : "");
                var confirm = MessageBox.Show("Preimenovati " + renamePairs.Count + " fajlova u " + curDir + "?\n\n" + preview, "001 Rename - potvrda", MessageBoxButtons.YesNo, MessageBoxIcon.Question); if (confirm!= DialogResult.Yes) return;
                var tempPaths = new List<Tuple<string, string, string>>();
                try
                {
                    foreach (var pair in renamePairs) { string oldPath = pair.Item1; string newPath = pair.Item2; if (string.Equals(oldPath, newPath, StringComparison.OrdinalIgnoreCase)) continue; string tempPath = oldPath + ".tmp_rename_" + Guid.NewGuid().ToString("N"); try { File.Move(oldPath, tempPath); tempPaths.Add(Tuple.Create(oldPath, tempPath, newPath)); } catch (Exception ex) { MessageBox.Show("Greska pri privremenom preimenovanju " + Path.GetFileName(oldPath) + ": " + ex.Message); } }
                    int ok2 = 0;
                    foreach (var t in tempPaths) { string tempPath = t.Item2; string newPath = t.Item3; try { if (File.Exists(newPath)) { if (!oldSet.Contains(newPath)) continue; try { File.Delete(newPath); } catch { } } File.Move(tempPath, newPath); ok2++; } catch (Exception ex) { MessageBox.Show("Greska pri finalnom preimenovanju u " + Path.GetFileName(newPath) + ": " + ex.Message); try { if (File.Exists(tempPath) &&!File.Exists(t.Item1)) File.Move(tempPath, t.Item1); } catch { } } }
                    statusLabel.Text = " 001 Rename: " + ok2 + "/" + filesInOrder.Count + " preimenovano u " + curDir + " (format " + new string('0', padLen) + ")"; lastStatus = statusLabel.Text;
                }
                finally { foreach (var t in tempPaths) { try { if (File.Exists(t.Item2)) File.Move(t.Item2, t.Item1); } catch { } } }
                LoadFolder(leftList, leftPath, leftCurrent, null); LoadFolder(rightList, rightPath, rightCurrent, null); ActiveMarked.Clear(); UpdateStatus();
            }
            catch (Exception ex) { MessageBox.Show("001 Rename greska: " + ex.Message, "001 Rename"); }
        }
        private void DeleteSelected(bool permanent)
        {
            List<string> toDel; if (ActiveMarked.Count > 0) toDel = ActiveMarked.ToList(); else { if (ActiveList.SelectedItems.Count == 0) return; var sel = ActiveList.SelectedItems[0].Tag as string; if (string.IsNullOrEmpty(sel) || sel.EndsWith("..")) return; toDel = new List<string> { sel }; }
            using (Form delForm = new Form() { Width = 600, Height = 400, Text = permanent? "Trajno brisanje (Shift+Del)" : "Brisanje u Recycle Bin (Del)", StartPosition = FormStartPosition.CenterParent, FormBorderStyle = FormBorderStyle.FixedDialog, MinimizeBox = false, MaximizeBox = false })
            {
                Label lbl = new Label() { Left = 10, Top = 10, Width = 560, Height = 40, Text = permanent? "TRAJNO obrisati?" : "Poslati u Recycle Bin?" };
                ListBox lb = new ListBox() { Left = 10, Top = 50, Width = 560, Height = 270 }; lb.Items.AddRange(toDel.ToArray());
                Button ok = new Button() { Text = permanent? "Trajno" : "Recycle", Left = 350, Top = 330, Width = 110, DialogResult = DialogResult.OK, BackColor = permanent? ColorDeletePermanentBg : ColorDeleteRecycleBg };
                Button cancel = new Button() { Text = "Otkazi", Left = 470, Top = 330, Width = 100, DialogResult = DialogResult.Cancel };
                delForm.Controls.AddRange(new Control[] { lbl, lb, ok, cancel }); delForm.AcceptButton = ok; delForm.CancelButton = cancel; DarkTheme.Apply(delForm);
                if (delForm.ShowDialog(this)!= DialogResult.OK) return;
            }
            foreach (var p in toDel) { try { if (File.Exists(p)) { if (permanent) SafeDeleteFile(p); else RecycleBin.SendToRecycle(p); } else if (Directory.Exists(p)) { if (permanent) SafeDeleteDir(p); else RecycleBin.SendToRecycle(p); } } catch (Exception ex) { MessageBox.Show("Greska " + p + ": " + ex.Message); } }
            ActiveMarked.Clear(); LoadFolder(ActiveList, ActiveList == leftList? leftPath : rightPath, ActiveList == leftList? leftCurrent : rightCurrent, null);
        }
    }
}