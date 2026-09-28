using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Drawing;
using System.Text.RegularExpressions;

namespace MiniTC
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MiniCommander());
        }
    }

    public class Prompt
    {
        public static string Show(string text, string caption, string defaultValue = "")
        {
            Form prompt = new Form()
            {
                Width = 350,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = caption,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false
            };

            Label textLabel = new Label() { Left = 10, Top = 10, Width = 320, Text = text };
            TextBox textBox = new TextBox() { Left = 10, Top = 35, Width = 310, Text = defaultValue };
            Button confirmation = new Button() { Text = "Ok", Left = 160, Width = 75, Top = 70, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 245, Width = 75, Top = 70, DialogResult = DialogResult.Cancel };

            prompt.Controls.AddRange(new Control[] { textBox, confirmation, cancel, textLabel });
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            return prompt.ShowDialog() == DialogResult.OK ? textBox.Text : "";
        }
    }

    public static class RecycleBin
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        struct SHFILEOPSTRUCT
        {
            public IntPtr hwnd;
            public uint wFunc;
            [MarshalAs(UnmanagedType.LPWStr)] public string pFrom;
            [MarshalAs(UnmanagedType.LPWStr)] public string pTo;
            public ushort fFlags;
            public bool fAnyOperationsAborted;
            public IntPtr hNameMappings;
            [MarshalAs(UnmanagedType.LPWStr)] public string lpszProgressTitle;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
        static extern int SHFileOperation(ref SHFILEOPSTRUCT lpFileOp);

        const uint FO_DELETE = 3;
        const ushort FOF_ALLOWUNDO = 0x40;
        const ushort FOF_NOCONFIRMATION = 0x10;
        const ushort FOF_SILENT = 0x4;

        public static bool SendToRecycle(string path)
        {
            var fs = new SHFILEOPSTRUCT();
            fs.wFunc = FO_DELETE;
            fs.pFrom = path + "\0\0";
            fs.fFlags = FOF_ALLOWUNDO | FOF_NOCONFIRMATION | FOF_SILENT;
            return SHFileOperation(ref fs) == 0;
        }

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        static extern bool SHObjectProperties(IntPtr hwnd, int shopObjectType, string pszObjectName, string pszPropertyPage);

        public static void ShowProperties(string path, IntPtr hwnd)
        {
            SHObjectProperties(hwnd, 2, path, null);
        }
    }

    public class MiniCommander : Form
    {
        // ========== PUTANJA ZA CONFIG - PROMENI OVDE AKO TREBA ==========
        // Sada je TXT da ne treba System.Xml.dll
        private const string ConfigFilePath = @"C:\MiniTC\MiniTC.txt";
        // ================================================================

        private ListView leftList, rightList;
        private ComboBox leftDrive, rightDrive;
        private TextBox leftPath, rightPath;
        private Label statusLabel;
        private TextBox cmdBox;
        private ToolStrip toolBar;
        private ToolStripComboBox fontCombo;
        private SplitContainer topSplit;
        private FlowLayoutPanel leftTabPanel, rightTabPanel;

        // 5. Compare Tool
        private string compareToolPath = "";
        private string compareToolParams = "";


        private ListView lastActive = null;
        private ListView ActiveList { get { return lastActive ?? leftList; } }

        private string leftCurrent = @"C:\";
        private string rightCurrent = @"C:\";

        private Font listFont = new Font("Consolas", 14f);

        private HashSet<string> markedLeft = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> markedRight = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> ActiveMarked { get { return ActiveList == leftList ? markedLeft : markedRight; } }

        private bool useRealShellMenu = false;

        // --- SORTIRANJE PO KOLONAMA ---
        private int sortColumn = 0;
        private SortOrder sortOrder = SortOrder.Ascending;
        private ListView currentSortList = null;

        private class ListViewItemComparer : System.Collections.IComparer
        {
            private int col;
            private SortOrder order;

            public ListViewItemComparer(int column, SortOrder order)
            {
                this.col = column;
                this.order = order;
            }

            public int Compare(object x, object y)
            {
                var itemX = x as ListViewItem;
                var itemY = y as ListViewItem;
                if (itemX == null || itemY == null) return 0;

                if (itemX.Text == "[..]") return -1;
                if (itemY.Text == "[..]") return 1;

                string pathX = itemX.Tag as string;
                string pathY = itemY.Tag as string;
                bool isDirX = (pathX != null && Directory.Exists(pathX)) || (itemX.SubItems.Count > 1 && itemX.SubItems[1].Text == "<DIR>");
                bool isDirY = (pathY != null && Directory.Exists(pathY)) || (itemY.SubItems.Count > 1 && itemY.SubItems[1].Text == "<DIR>");

                int result = 0;

                switch (col)
                {
                    case 0: // Ime
                        string nameX = itemX.Text.Trim('[', ']');
                        string nameY = itemY.Text.Trim('[', ']');
                        if (isDirX && !isDirY) result = -1;
                        else if (!isDirX && isDirY) result = 1;
                        else result = string.Compare(nameX, nameY, StringComparison.OrdinalIgnoreCase);
                        break;

                    case 1: // Velicina
                        long sizeX = GetSize(itemX, pathX, isDirX);
                        long sizeY = GetSize(itemY, pathY, isDirY);
                        if (isDirX && !isDirY) result = -1;
                        else if (!isDirX && isDirY) result = 1;
                        else result = sizeX.CompareTo(sizeY);
                        break;

                    case 2: // Datum dd.MM.yyyy HH:mm
                        DateTime dtX, dtY;
                        string dateStrX = itemX.SubItems.Count > 2 ? itemX.SubItems[2].Text : "";
                        string dateStrY = itemY.SubItems.Count > 2 ? itemY.SubItems[2].Text : "";
                        bool okX = DateTime.TryParseExact(dateStrX, "dd.MM.yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out dtX);
                        bool okY = DateTime.TryParseExact(dateStrY, "dd.MM.yyyy HH:mm", null, System.Globalization.DateTimeStyles.None, out dtY);
                        if (!okX) dtX = DateTime.MinValue;
                        if (!okY) dtY = DateTime.MinValue;
                        result = DateTime.Compare(dtX, dtY);
                        break;

                    case 3: // Tip
                        string tipX = itemX.SubItems.Count > 3 ? itemX.SubItems[3].Text : "";
                        string tipY = itemY.SubItems.Count > 3 ? itemY.SubItems[3].Text : "";
                        result = string.Compare(tipX, tipY, StringComparison.OrdinalIgnoreCase);
                        if (result == 0)
                            result = string.Compare(itemX.Text, itemY.Text, StringComparison.OrdinalIgnoreCase);
                        break;
                }

                if (order == SortOrder.Descending) result = -result;
                return result;
            }

            private static long GetSize(ListViewItem item, string path, bool isDir)
            {
                if (isDir) return -1;
                try
                {
                    if (path != null && File.Exists(path))
                        return new FileInfo(path).Length;
                }
                catch { }
                try
                {
                    string s = item.SubItems[1].Text.Replace(" B", "").Replace(" KB", "").Replace(" MB", "").Trim();
                    long v = 0;
                    if (long.TryParse(s, out v))
                    {
                        if (item.SubItems[1].Text.Contains("KB")) v *= 1024;
                        if (item.SubItems[1].Text.Contains("MB")) v *= 1024 * 1024;
                        return v;
                    }
                }
                catch { }
                return 0;
            }
        }


        private class TabInfo
        {
            public string Path;
        }

        private List<TabInfo> leftTabs = new List<TabInfo>();
        private List<TabInfo> rightTabs = new List<TabInfo>();
        private int leftTabIdx = 0;
        private int rightTabIdx = 0;

        public MiniCommander()
        {
            Text = "Mini TC v1.3 - F6 Move";
            Width = 1200;
            Height = 800;
            WindowState = FormWindowState.Maximized;
            KeyPreview = true;
            BackColor = Color.Black;
            DoubleBuffered = true;

            toolBar = new ToolStrip
            {
                Dock = DockStyle.Top,
                GripStyle = ToolStripGripStyle.Hidden,
                BackColor = Color.FromArgb(30, 30, 30),
                ForeColor = Color.White
            };

            toolBar.Items.Add(new ToolStripButton("<", null, (s, e) => SyncPanel(true)) { ToolTipText = "Levi = Desni" });
            toolBar.Items.Add(new ToolStripButton(">", null, (s, e) => SyncPanel(false)) { ToolTipText = "Desni = Levi" });
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripButton("F4 Edit", null, (s, e) => OpenInNotepad()));
            toolBar.Items.Add(new ToolStripButton("F5 Copy", null, (s, e) => CopySelected()));
            toolBar.Items.Add(new ToolStripButton("F6 Move", null, (s, e) => MoveSelected()));
            toolBar.Items.Add(new ToolStripButton("F7 New dir", null, (s, e) => CreateFolder()));
            toolBar.Items.Add(new ToolStripButton("F8 Path", null, (s, e) => CopyPathToClipboard()));
            //toolBar.Items.Add(new ToolStripButton("Del Recycle", null, (s, e) => DeleteSelected(false)));
            //toolBar.Items.Add(new ToolStripButton("Shift+Del", null, (s, e) => DeleteSelected(true)));
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripButton("Alt+F7 Search", null, (s, e) => OpenSearchDialog()) { ToolTipText = "Pretraga fajlova - Alt+F7" });
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripButton("F3 Compare", null, (s, e) => RunCompareTool()) { ToolTipText = "Uporedi 2 fajla (1 levo + 1 desno obelezen) - F3" });
            toolBar.Items.Add(new ToolStripLabel(" Font:"));
            fontCombo = new ToolStripComboBox
            {
                Items = { "10", "12", "14", "16", "18", "20", "22", "26", "32" },
                Text = "14",
                Width = 60
            };
            fontCombo.SelectedIndexChanged += (s, e) => ChangeFont();
            toolBar.Items.Add(fontCombo);
            toolBar.Items.Add(new ToolStripSeparator());
            toolBar.Items.Add(new ToolStripButton("Compare Setup", null, (s, e) => SetupCompareTool()));
            toolBar.Items.Add(new ToolStripSeparator());
            //toolBar.Items.Add(new ToolStripButton("Ctrl+T Tab", null, (s, e) => NewTab()));
            //toolBar.Items.Add(new ToolStripButton("Ctrl+W Close", null, (s, e) => CloseTab()));
            //toolBar.Items.Add(new ToolStripButton("Ctrl+Tab Next", null, (s, e) => NextTab()));

            var shellToggle = new ToolStripButton("Shell OFF") { CheckOnClick = true, Checked = false };
            shellToggle.CheckedChanged += (s, e) =>
            {
                useRealShellMenu = shellToggle.Checked;
                shellToggle.Text = useRealShellMenu ? "Shell ON" : "Shell OFF";
                shellToggle.BackColor = useRealShellMenu ? Color.DarkGreen : Color.FromArgb(30, 30, 30);
            };
            toolBar.Items.Add(shellToggle);

            topSplit = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 600,
                BackColor = Color.DimGray,
                BorderStyle = BorderStyle.None,
                Panel1MinSize = 100,
                Panel2MinSize = 100,
                SplitterWidth = 6
            };

            leftDrive = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, TabStop = false, BackColor = Color.Black, ForeColor = Color.Cyan, FlatStyle = FlatStyle.Flat };
            rightDrive = new ComboBox { Dock = DockStyle.Top, DropDownStyle = ComboBoxStyle.DropDownList, TabStop = false, BackColor = Color.Black, ForeColor = Color.Cyan, FlatStyle = FlatStyle.Flat };

            leftPath = new TextBox { Dock = DockStyle.Top, BackColor = Color.Black, ForeColor = Color.Cyan, BorderStyle = BorderStyle.FixedSingle };
            rightPath = new TextBox { Dock = DockStyle.Top, BackColor = Color.Black, ForeColor = Color.Cyan, BorderStyle = BorderStyle.FixedSingle };

            leftPath.KeyDown += PathBox_KeyDown;
            rightPath.KeyDown += PathBox_KeyDown;

            leftTabPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(20, 20, 20),
                WrapContents = false,
                AutoScroll = true
            };

            rightTabPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Top,
                Height = 30,
                BackColor = Color.FromArgb(20, 20, 20),
                WrapContents = false,
                AutoScroll = true
            };

            leftList = CreateFileList();
            rightList = CreateFileList();

            var leftPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
            leftPanel.Controls.Add(leftList);
            leftPanel.Controls.Add(leftPath);
            leftPanel.Controls.Add(leftDrive);
            leftPanel.Controls.Add(leftTabPanel);

            var rightPanel = new Panel { Dock = DockStyle.Fill, BackColor = Color.Black };
            rightPanel.Controls.Add(rightList);
            rightPanel.Controls.Add(rightPath);
            rightPanel.Controls.Add(rightDrive);
            rightPanel.Controls.Add(rightTabPanel);

            topSplit.Panel1.Controls.Add(leftPanel);
            topSplit.Panel2.Controls.Add(rightPanel);

            cmdBox = new TextBox
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = Color.Black,
                ForeColor = Color.Yellow,
                BorderStyle = BorderStyle.FixedSingle,
                Font = listFont
            };
            cmdBox.KeyDown += CmdBox_KeyDown;

            var cmdLine = new Label { Dock = DockStyle.Bottom, Height = 2, BackColor = Color.DimGray };
            statusLabel = new Label { Dock = DockStyle.Bottom, Height = 28, BackColor = Color.Black, ForeColor = Color.Lime };

            EnableDoubleBuffer(statusLabel);
            EnableDoubleBuffer(cmdBox);

            Controls.Add(topSplit);
            Controls.Add(cmdLine);
            Controls.Add(cmdBox);
            Controls.Add(statusLabel);
            Controls.Add(toolBar);

            this.Resize += (s, e) =>
            {
                if (this.WindowState == FormWindowState.Minimized) return;
                if (this.ClientSize.Width < 200) return;
                try
                {
                    int half = this.ClientSize.Width / 2;
                    int min = topSplit.Panel1MinSize;
                    int max = this.ClientSize.Width - topSplit.Panel2MinSize - topSplit.SplitterWidth;
                    if (max < min) max = min;
                    if (half < min) half = min;
                    if (half > max) half = max;
                    topSplit.SplitterDistance = half;
                    ResizeColumns(leftList);
                    ResizeColumns(rightList);
                }
                catch { }
            };

            LoadDrives();
            LoadTabsFromTxt();

            leftDrive.SelectedIndexChanged += (s, e) =>
            {
                leftCurrent = leftDrive.Text;
                markedLeft.Clear();
                if (leftTabs.Count > 0) leftTabs[leftTabIdx].Path = leftCurrent;
                LoadFolder(leftList, leftPath, leftCurrent, null);
            };

            rightDrive.SelectedIndexChanged += (s, e) =>
            {
                rightCurrent = rightDrive.Text;
                markedRight.Clear();
                if (rightTabs.Count > 0) rightTabs[rightTabIdx].Path = rightCurrent;
                LoadFolder(rightList, rightPath, rightCurrent, null);
            };

            this.KeyDown += OnKeyDown;
            this.FormClosing += (s, e) => SaveTabsToTxt();

            if (leftTabs.Count > 0) leftCurrent = leftTabs[leftTabIdx].Path;
            if (rightTabs.Count > 0) rightCurrent = rightTabs[rightTabIdx].Path;

            LoadFolder(leftList, leftPath, leftCurrent, null);
            LoadFolder(rightList, rightPath, rightCurrent, null);

            RenderTabs();

            lastActive = leftList;
            leftList.Focus();
            ChangeFont();
        }

                private void LoadTabsFromTxt()
        {
            try
            {
                compareToolPath = "";
                compareToolParams = "";

                if (!File.Exists(ConfigFilePath))
                {
                    leftTabs = new List<TabInfo> { new TabInfo { Path = @"C:" } };
                    rightTabs = new List<TabInfo> { new TabInfo { Path = @"C:" } };
                    if (Directory.Exists(@"D:")) rightTabs[0].Path = @"D:";
                    return;
                }

                var lines = File.ReadAllLines(ConfigFilePath);
                leftTabs.Clear();
                rightTabs.Clear();

                int mode = 0; // 0 none, 1 levi, 2 desni, 3 compare

                foreach (var raw in lines)
                {
                    string line = raw.Trim();
                    if (string.IsNullOrWhiteSpace(line)) continue;

                    if (line.Equals("[LeviPanel]", StringComparison.OrdinalIgnoreCase) || line.Equals("LeviPanel", StringComparison.OrdinalIgnoreCase))
                    {
                        mode = 1;
                        continue;
                    }
                    if (line.Equals("[DesniPanel]", StringComparison.OrdinalIgnoreCase) || line.Equals("DesniPanel", StringComparison.OrdinalIgnoreCase))
                    {
                        mode = 2;
                        continue;
                    }
                    if (line.Equals("[CompareTool]", StringComparison.OrdinalIgnoreCase) || line.Equals("CompareTool", StringComparison.OrdinalIgnoreCase))
                    {
                        mode = 3;
                        continue;
                    }

                    if (mode == 3)
                    {
                        // Format: Path=... ili Params=...
                        if (line.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
                            compareToolPath = line.Substring(5).Trim();
                        else if (line.StartsWith("Params=", StringComparison.OrdinalIgnoreCase))
                            compareToolParams = line.Substring(7).Trim();
                        else if (line.StartsWith("<Folder>", StringComparison.OrdinalIgnoreCase)) { }
                        else if (!line.StartsWith("[") && !line.StartsWith("<"))
                        {
                            // ako je samo putanja bez Path=, uzmi kao path ako nema
                            if (string.IsNullOrEmpty(compareToolPath) && (File.Exists(line) || line.EndsWith(".exe", StringComparison.OrdinalIgnoreCase)))
                                compareToolPath = line;
                        }
                        continue;
                    }

                    if (line.StartsWith("<Folder>", StringComparison.OrdinalIgnoreCase))
                    {
                        int s = line.IndexOf('>') + 1;
                        int e = line.LastIndexOf('<');
                        if (e > s) line = line.Substring(s, e - s).Trim();
                    }

                    if (line.StartsWith("<") && line.EndsWith(">")) continue;

                    if (Directory.Exists(line))
                    {
                        if (mode == 1) leftTabs.Add(new TabInfo { Path = line });
                        else if (mode == 2) rightTabs.Add(new TabInfo { Path = line });
                        else
                        {
                            if (leftTabs.Count <= rightTabs.Count) leftTabs.Add(new TabInfo { Path = line });
                            else rightTabs.Add(new TabInfo { Path = line });
                        }
                    }
                }

                if (leftTabs.Count == 0) leftTabs.Add(new TabInfo { Path = @"C:" });
                if (rightTabs.Count == 0) rightTabs.Add(new TabInfo { Path = @"C:" });

                leftTabIdx = 0;
                rightTabIdx = 0;
            }
            catch
            {
                leftTabs = new List<TabInfo> { new TabInfo { Path = @"C:" } };
                rightTabs = new List<TabInfo> { new TabInfo { Path = @"C:" } };
            }
        }

        private void SaveTabsToTxt()
        {
            try
            {
                string dir = Path.GetDirectoryName(ConfigFilePath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                if (leftTabs.Count > leftTabIdx && leftTabIdx >= 0) leftTabs[leftTabIdx].Path = leftCurrent;
                if (rightTabs.Count > rightTabIdx && rightTabIdx >= 0) rightTabs[rightTabIdx].Path = rightCurrent;

                var outLines = new List<string>();
                outLines.Add("[LeviPanel]");
                foreach (var t in leftTabs) outLines.Add(t.Path);
                outLines.Add("");
                outLines.Add("[DesniPanel]");
                foreach (var t in rightTabs) outLines.Add(t.Path);
                outLines.Add("");
                outLines.Add("[CompareTool]");
                outLines.Add("Path=" + compareToolPath);
                outLines.Add("Params=" + compareToolParams);

                File.WriteAllLines(ConfigFilePath, outLines);
            }
            catch { }
        }

        private void RenderTabs()
        {
            leftTabPanel.Controls.Clear();
            for (int i = 0; i < leftTabs.Count; i++)
            {
                int idx = i;
                string name = GetTabName(leftTabs[i].Path);
                var btn = new Button
                {
                    Text = (i + 1) + ":" + name,
                    Height = 24,
                    AutoSize = true,
                    BackColor = i == leftTabIdx ? Color.Yellow : Color.FromArgb(50, 50, 50),
                    ForeColor = i == leftTabIdx ? Color.Black : Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(1)
                };
                btn.Click += (s, e) =>
                {
                    leftTabs[leftTabIdx].Path = leftCurrent;
                    leftTabIdx = idx;
                    leftCurrent = leftTabs[leftTabIdx].Path;
                    LoadFolder(leftList, leftPath, leftCurrent, null);
                    RenderTabs();
                };
                leftTabPanel.Controls.Add(btn);
            }

            rightTabPanel.Controls.Clear();
            for (int i = 0; i < rightTabs.Count; i++)
            {
                int idx = i;
                string name = GetTabName(rightTabs[i].Path);
                var btn = new Button
                {
                    Text = (i + 1) + ":" + name,
                    Height = 24,
                    AutoSize = true,
                    BackColor = i == rightTabIdx ? Color.Yellow : Color.FromArgb(50, 50, 50),
                    ForeColor = i == rightTabIdx ? Color.Black : Color.White,
                    FlatStyle = FlatStyle.Flat,
                    Margin = new Padding(1)
                };
                btn.Click += (s, e) =>
                {
                    rightTabs[rightTabIdx].Path = rightCurrent;
                    rightTabIdx = idx;
                    rightCurrent = rightTabs[rightTabIdx].Path;
                    LoadFolder(rightList, rightPath, rightCurrent, null);
                    RenderTabs();
                };
                rightTabPanel.Controls.Add(btn);
            }
        }

        private string GetTabName(string path)
        {
            try
            {
                if (path.EndsWith(":\\")) return path;
                return Path.GetFileName(path.TrimEnd('\\'));
            }
            catch { return path; }
        }

        private void NewTab()
        {
            if (ActiveList == leftList)
            {
                leftTabs[leftTabIdx].Path = leftCurrent;
                leftTabs.Insert(leftTabIdx + 1, new TabInfo { Path = leftCurrent });
                leftTabIdx++;
                RenderTabs();
                SaveTabsToTxt();
            }
            else
            {
                rightTabs[rightTabIdx].Path = rightCurrent;
                rightTabs.Insert(rightTabIdx + 1, new TabInfo { Path = rightCurrent });
                rightTabIdx++;
                RenderTabs();
                SaveTabsToTxt();
            }
        }

        private void CloseTab()
        {
            if (ActiveList == leftList)
            {
                if (leftTabs.Count <= 1) return;
                leftTabs.RemoveAt(leftTabIdx);
                leftTabIdx = Math.Max(0, leftTabIdx - 1);
                leftCurrent = leftTabs[leftTabIdx].Path;
                LoadFolder(leftList, leftPath, leftCurrent, null);
                RenderTabs();
                SaveTabsToTxt();
            }
            else
            {
                if (rightTabs.Count <= 1) return;
                rightTabs.RemoveAt(rightTabIdx);
                rightTabIdx = Math.Max(0, rightTabIdx - 1);
                rightCurrent = rightTabs[rightTabIdx].Path;
                LoadFolder(rightList, rightPath, rightCurrent, null);
                RenderTabs();
                SaveTabsToTxt();
            }
        }

        private void NextTab()
        {
            if (ActiveList == leftList)
            {
                leftTabs[leftTabIdx].Path = leftCurrent;
                leftTabIdx = (leftTabIdx + 1) % leftTabs.Count;
                leftCurrent = leftTabs[leftTabIdx].Path;
                LoadFolder(leftList, leftPath, leftCurrent, null);
                RenderTabs();
            }
            else
            {
                rightTabs[rightTabIdx].Path = rightCurrent;
                rightTabIdx = (rightTabIdx + 1) % rightTabs.Count;
                rightCurrent = rightTabs[rightTabIdx].Path;
                LoadFolder(rightList, rightPath, rightCurrent, null);
                RenderTabs();
            }
        }

        private void CmdBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode != Keys.Enter) return;

            string cmd = cmdBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(cmd)) return;

            string activeCur = ActiveList == leftList ? leftCurrent : rightCurrent;

            try
            {
                if (cmd.Length == 2 && cmd[1] == ':') cmd += "\\";

                if (Directory.Exists(cmd))
                {
                    LoadFolder(ActiveList, ActiveList == leftList ? leftPath : rightPath, cmd, null);
                    cmdBox.Clear();
                    e.SuppressKeyPress = true;
                    return;
                }

                if (cmd.Equals("..") || cmd.Equals("cd.."))
                {
                    var parent = Directory.GetParent(activeCur);
                    if (parent != null)
                        LoadFolder(ActiveList, ActiveList == leftList ? leftPath : rightPath, parent.FullName, activeCur);
                    cmdBox.Clear();
                    return;
                }

                if (cmd.Equals("cmd", StringComparison.OrdinalIgnoreCase))
                {
                    Process.Start(new ProcessStartInfo("cmd.exe") { WorkingDirectory = activeCur, UseShellExecute = true });
                    cmdBox.Clear();
                    return;
                }

                // FIX: Path.Combine puca na '>' '<' '|' itd. - obmotaj u try/catch i preskoci ako ima illegal chars
                bool hasIllegal = cmd.IndexOfAny(new char[] { '>', '<', '|', '"' }) >= 0;

                if (!hasIllegal)
                {
                    try
                    {
                        string maybeFile = Path.Combine(activeCur, cmd);
                        if (File.Exists(maybeFile))
                        {
                            Process.Start(new ProcessStartInfo(maybeFile) { UseShellExecute = true, WorkingDirectory = activeCur });
                            cmdBox.Clear();
                            return;
                        }
                    }
                    catch { }
                }

                try
                {
                    if (File.Exists(cmd))
                    {
                        Process.Start(new ProcessStartInfo(cmd) { UseShellExecute = true });
                        cmdBox.Clear();
                        return;
                    }
                }
                catch { }

                // Probaj da otvoris kao komandu sa argumentima - npr. "notepad proba.txt" ili "notepad >proba.txt"
                // Ako prvi token postoji kao fajl ili exe u PATH, pokreni ga
                try
                {
                    string firstToken = cmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                    if (!string.IsNullOrEmpty(firstToken))
                    {
                        // ako je notepad, calc, mspaint itd - pusti direktno
                        if (firstToken.Equals("notepad", StringComparison.OrdinalIgnoreCase) ||
                            firstToken.Equals("calc", StringComparison.OrdinalIgnoreCase) ||
                            firstToken.Equals("mspaint", StringComparison.OrdinalIgnoreCase))
                        {
                            // pokreni preko shell-a da bi radio i sa ">" redirekcijom
                            Process.Start(new ProcessStartInfo("cmd.exe", "/c cd /d \"" + activeCur + "\" && " + cmd)
                            {
                                UseShellExecute = true,
                                WorkingDirectory = activeCur
                            });
                            cmdBox.Clear();
                            e.SuppressKeyPress = true;
                            return;
                        }
                    }
                }
                catch { }

                // Default: sve ostalo salji u cmd.exe - on zna da hendluje ">", "|", itd.
                Process.Start(new ProcessStartInfo("cmd.exe", "/k cd /d \"" + activeCur + "\" && " + cmd)
                {
                    UseShellExecute = true,
                    WorkingDirectory = activeCur
                });
            }
            catch (Exception ex)
            {
                statusLabel.Text = " CMD greska: " + ex.Message;
                lastStatus = statusLabel.Text;
            }

            cmdBox.Clear();
            e.SuppressKeyPress = true;
        }

        private void EnableDoubleBuffer(Control c)
        {
            try
            {
                typeof(Control).GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic)
                    .SetValue(c, true, null);
            }
            catch { }
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // BUG FIX: kada kucas u donjem edit boxu (cmdBox) ili u path boxevima, disable sve shortcutove
            // da Space ne selektuje fajl nego da ubaci razmak, Delete ne brise itd.
            var focused = this.ActiveControl;
            bool isTyping = focused is TextBoxBase || focused is ComboBox;
            // ToolStripComboBox fontCombo je malo drugaciji, proveri i da li je cmdBox fokusiran direktno
            if (isTyping || (cmdBox != null && cmdBox.Focused) || (leftPath != null && leftPath.Focused) || (rightPath != null && rightPath.Focused))
            {
                // dozvoli samo Ctrl+T/W/Tab za tabove i F3 za compare da i dalje rade cak i kad kucas? 
                // Po zahtevu: moraju biti disable-ovani SVI ostali shortcutovi kad kucas dole
                // Zato ovde vracamo base da TextBox normalno obradi Space, Delete, itd.
                // Ali F3, Ctrl+T/W/Tab ostavljamo da rade i dok kucas dole ako bas hoces
                if (keyData == Keys.Space || keyData == Keys.Delete || keyData == (Keys.Shift | Keys.Delete) || keyData == Keys.Escape)
                    return base.ProcessCmdKey(ref msg, keyData);

                // Za Tab koji switchuje panele - kad kucas u cmdBox, Tab ne treba da switchuje
                if (isTyping && (keyData == Keys.Tab || keyData == (Keys.Control | Keys.Left) || keyData == (Keys.Control | Keys.Right)))
                    return base.ProcessCmdKey(ref msg, keyData);
            }

            if (keyData == Keys.F3)
            {
                // F3 ipak dozvoli i kad kucas dole - to je compare
                RunCompareTool();
                return true;
            }

            if (keyData == Keys.Tab)
            {
                // ako je fokus u TextBoxu, ne switchuj panele
                if (isTyping) return base.ProcessCmdKey(ref msg, keyData);
                SwitchPanel();
                return true;
            }

            if (keyData == (Keys.Control | Keys.Tab))
            {
                NextTab();
                return true;
            }

            if (keyData == (Keys.Control | Keys.T))
            {
                NewTab();
                return true;
            }

            if (keyData == (Keys.Control | Keys.W))
            {
                CloseTab();
                return true;
            }

            if (keyData == Keys.Escape)
            {
                ActiveMarked.Clear();
                ActiveList.Invalidate();
                UpdateStatus();
                return true;
            }

            if (keyData == Keys.Space)
            {
                ToggleMark();
                return true;
            }

            if (keyData == Keys.F6)
            {
                MoveSelected();
                return true;
            }

            if (keyData == (Keys.Shift | Keys.F6))
            {
                RenameSelected();
                return true;
            }

            if (keyData == (Keys.Shift | Keys.Delete))
            {
                DeleteSelected(true);
                return true;
            }

            if (keyData == Keys.Delete)
            {
                DeleteSelected(false);
                return true;
            }

            if (keyData == (Keys.Control | Keys.Left))
            {
                SyncPanel(true);
                return true;
            }

            if (keyData == (Keys.Control | Keys.Right))
            {
                SyncPanel(false);
                return true;
            }

            if (keyData == (Keys.Alt | Keys.F7))
            {
                OpenSearchDialog();
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        private void SyncPanel(bool leftGetsRight)
        {
            if (leftGetsRight)
            {
                LoadFolder(leftList, leftPath, rightCurrent, null);
                leftList.Focus();
                lastActive = leftList;
            }
            else
            {
                LoadFolder(rightList, rightPath, leftCurrent, null);
                rightList.Focus();
                lastActive = rightList;
            }
        }

        private void SwitchPanel()
        {
            var old = lastActive;
            if (lastActive == leftList) lastActive = rightList;
            else lastActive = leftList;

            lastActive.Focus();
            if (old != null) old.Invalidate();
            lastActive.Invalidate();
            UpdateStatus();
        }

        private void ChangeFont()
        {
            float size = 14f;
            float.TryParse(fontCombo.Text, out size);
            if (size < 8) size = 14;

            listFont = new Font("Consolas", size, FontStyle.Regular);
            Font uiFont = new Font("Consolas", size, FontStyle.Regular);

            leftList.Font = listFont;
            rightList.Font = listFont;
            leftPath.Font = uiFont;
            rightPath.Font = uiFont;
            leftDrive.Font = uiFont;
            rightDrive.Font = uiFont;
            statusLabel.Font = uiFont;
            toolBar.Font = uiFont;
            cmdBox.Font = uiFont;

            ResizeColumns(leftList);
            ResizeColumns(rightList);

            leftList.Invalidate();
            rightList.Invalidate();
        }

        private void ResizeColumns(ListView lv)
        {
            if (lv.ClientSize.Width < 50) return;

            int w = lv.ClientSize.Width - 4;
            lv.Columns[0].Width = (int)(w * 0.50);
            lv.Columns[1].Width = (int)(w * 0.15);
            lv.Columns[2].Width = (int)(w * 0.20);
            lv.Columns[3].Width = w - lv.Columns[0].Width - lv.Columns[1].Width - lv.Columns[2].Width;
        }

        private ListView CreateFileList()
        {
            var lv = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                OwnerDraw = true,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = listFont,
                BorderStyle = BorderStyle.None
            };

            EnableDoubleBuffer(lv);

            lv.Columns.Add("Ime", 300);
            lv.Columns.Add("Veličina", 80);
            lv.Columns.Add("Datum", 120);
            lv.Columns.Add("Tip", 60);

            lv.Resize += (s, e) => ResizeColumns(lv);

            lv.ColumnClick += (s, e) =>
            {
                var list = s as ListView;
                if (currentSortList == list && sortColumn == e.Column)
                {
                    sortOrder = (sortOrder == SortOrder.Ascending) ? SortOrder.Descending : SortOrder.Ascending;
                }
                else
                {
                    sortColumn = e.Column;
                    sortOrder = SortOrder.Ascending;
                    currentSortList = list;
                }

                list.ListViewItemSorter = new ListViewItemComparer(sortColumn, sortOrder);
                list.Sort();
                list.Focus();
            };

            lv.DrawColumnHeader += (s, e) =>
            {
                e.Graphics.FillRectangle(new SolidBrush(Color.FromArgb(55, 55, 55)), e.Bounds);
                TextRenderer.DrawText(e.Graphics, e.Header.Text, listFont, e.Bounds, Color.Cyan, TextFormatFlags.VerticalCenter | TextFormatFlags.Left);
            };

            lv.DrawItem += (s, e) => { e.DrawDefault = false; };

            lv.DrawSubItem += (s, e) =>
            {
                var list = s as ListView;
                string fullPath = e.Item.Tag as string;
                bool isMarked = list == leftList ? markedLeft.Contains(fullPath) : markedRight.Contains(fullPath);
                bool isFocusedItem = e.Item.Focused && list.Focused;

                Color back = isFocusedItem ? Color.White : Color.Black;
                Color fore = isMarked ? Color.Red : (isFocusedItem ? Color.Black : Color.White);

                using (var b = new SolidBrush(back))
                    e.Graphics.FillRectangle(b, e.Bounds);

                var f = isMarked ? new Font(listFont, FontStyle.Bold) : listFont;
                e.Graphics.DrawString(e.SubItem.Text, f, new SolidBrush(fore), e.Bounds.Location);
                if (isMarked) f.Dispose();
            };

            lv.MouseClick += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    var item = lv.GetItemAt(e.X, e.Y);
                    if (item != null)
                    {
                        item.Selected = true;
                        item.Focused = true;
                        lastActive = lv;
                        ShowExplorerContextMenu(item.Tag as string);
                    }
                }
            };

            lv.DoubleClick += (s, e) => EnterFolder();

            lv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    EnterFolder();
                    e.SuppressKeyPress = true;
                }
            };

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
                    dynamic folder = shell.NameSpace(Path.GetDirectoryName(path));
                    dynamic item = folder.ParseName(Path.GetFileName(path));

                    var menu = new ContextMenuStrip();

                    foreach (var v in item.Verbs())
                    {
                        string vName = v.Name.Replace("&", "").Trim();
                        if (string.IsNullOrWhiteSpace(vName)) continue;
                        dynamic verb = v;
                        menu.Items.Add(vName, null, (s, e) => { try { verb.DoIt(); } catch { } });
                    }

                    if (menu.Items.Count == 0) menu.Items.Add("(nema shell verbova)");
                    menu.Items.Add(new ToolStripSeparator());
                    menu.Items.Add("Svojstva", null, (s, e) => RecycleBin.ShowProperties(path, this.Handle));
                    menu.Show(Cursor.Position);
                    return;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Shell meni nije uspeo: " + ex.Message);
                }
            }

            var menu2 = new ContextMenuStrip();
            menu2.Items.Add("Otvori", null, (s, e) => { Process.Start(new ProcessStartInfo(path) { UseShellExecute = true }); });
            if (File.Exists(path)) menu2.Items.Add("Otvori u Notepadu (F4)", null, (s, e) => OpenInNotepad());
            menu2.Items.Add("Kopiraj putanju (F8)", null, (s, e) => CopyPathToClipboard());
            menu2.Items.Add(new ToolStripSeparator());
            menu2.Items.Add("Iseci", null, (s, e) =>
            {
                var sc = new System.Collections.Specialized.StringCollection();
                sc.Add(path);
                Clipboard.SetFileDropList(sc);
            });
            menu2.Items.Add("Kopiraj", null, (s, e) =>
            {
                var sc = new System.Collections.Specialized.StringCollection();
                sc.Add(path);
                Clipboard.SetFileDropList(sc);
                SafeClipboard(path);
            });
            menu2.Items.Add(new ToolStripSeparator());
            menu2.Items.Add("Pošalji u Recycle Bin", null, (s, e) => DeleteSelected(false));
            menu2.Items.Add("Trajno obriši (Shift+Del)", null, (s, e) => DeleteSelected(true));
            menu2.Items.Add(new ToolStripSeparator());
            menu2.Items.Add("Svojstva", null, (s, e) => RecycleBin.ShowProperties(path, this.Handle));
            menu2.Items.Add("Otvori u Exploreru", null, (s, e) =>
            {
                string arg = File.Exists(path) ? $"/select, \"{path}\"" : $"\"{path}\"";
                Process.Start("explorer.exe", arg);
            });
            menu2.Show(Cursor.Position);
        }

        private void ToggleMark()
        {
            if (ActiveList.SelectedItems.Count == 0) return;

            var item = ActiveList.SelectedItems[0];
            string path = item.Tag as string;
            if (path.EndsWith("..") || string.IsNullOrEmpty(path)) return;

            if (ActiveMarked.Contains(path)) ActiveMarked.Remove(path);
            else ActiveMarked.Add(path);

            int idx = item.Index;
            ActiveList.RedrawItems(idx, idx, false);

            if (idx + 1 < ActiveList.Items.Count)
            {
                ActiveList.SelectedItems.Clear();
                ActiveList.Items[idx + 1].Selected = true;
                ActiveList.Items[idx + 1].Focused = true;
                ActiveList.EnsureVisible(idx + 1);
            }

            UpdateStatus();
        }

        private void LoadDrives()
        {
            var drives = DriveInfo.GetDrives().Where(d => d.IsReady).Select(d => d.RootDirectory.FullName).ToArray();
            leftDrive.Items.AddRange(drives);
            rightDrive.Items.AddRange(drives);
            if (drives.Length > 0) leftDrive.SelectedIndex = 0;
            if (drives.Length > 1) rightDrive.SelectedIndex = 1;
        }

                private void LoadFolder(ListView list, TextBox pathBox, string path, string pathToSelect)
        {
            try
            {
                if (!Directory.Exists(path)) path = @"C:";
                pathBox.Text = path;

                if (list == leftList)
                {
                    leftCurrent = path;
                    if (leftTabs.Count > leftTabIdx) leftTabs[leftTabIdx].Path = path;
                }
                else
                {
                    rightCurrent = path;
                    if (rightTabs.Count > rightTabIdx) rightTabs[rightTabIdx].Path = path;
                }

                list.BeginUpdate();
                list.Items.Clear();

                DirectoryInfo dirInfo = null;
                try { dirInfo = new DirectoryInfo(path); }
                catch { dirInfo = null; }

                if (dirInfo == null || !dirInfo.Exists)
                {
                    list.EndUpdate();
                    statusLabel.Text = " Ne mogu da pristupim: " + path;
                    lastStatus = statusLabel.Text;
                    list.Focus();
                    return;
                }

                try
                {
                    if (dirInfo.Parent != null)
                    {
                        var up = new ListViewItem("[..]") { Tag = dirInfo.Parent.FullName };
                        up.SubItems.Add("");
                        up.SubItems.Add("");
                        up.SubItems.Add("<DIR UP>");
                        list.Items.Add(up);
                    }
                }
                catch { }

                IEnumerable<DirectoryInfo> dirs = new List<DirectoryInfo>();
                try
                {
                    dirs = dirInfo.EnumerateDirectories();
                }
                catch (UnauthorizedAccessException)
                {
                    statusLabel.Text = " Access denied: " + path + " - preskacem";
                    lastStatus = statusLabel.Text;
                }
                catch (Exception ex)
                {
                    statusLabel.Text = " Greska: " + ex.Message;
                    lastStatus = statusLabel.Text;
                }

                foreach (var d in dirs)
                {
                    try
                    {
                        if ((d.Attributes & FileAttributes.Hidden) != 0) continue;
                        var item = new ListViewItem("[" + d.Name + "]") { Tag = d.FullName };
                        item.SubItems.Add("<DIR>");
                        item.SubItems.Add(d.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));
                        item.SubItems.Add("Folder");
                        list.Items.Add(item);
                    }
                    catch { }
                }

                IEnumerable<FileInfo> files = new List<FileInfo>();
                try { files = dirInfo.EnumerateFiles(); }
                catch (UnauthorizedAccessException) { }
                catch { }

                foreach (var f in files)
                {
                    try
                    {
                        if ((f.Attributes & FileAttributes.Hidden) != 0) continue;
                        var item = new ListViewItem(f.Name) { Tag = f.FullName };
                        item.SubItems.Add(FormatSize(f.Length));
                        item.SubItems.Add(f.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));
                        item.SubItems.Add(f.Extension);
                        list.Items.Add(item);
                    }
                    catch { }
                }

                list.EndUpdate();

                bool found = false;
                if (!string.IsNullOrEmpty(pathToSelect))
                {
                    foreach (ListViewItem it in list.Items)
                    {
                        if (string.Equals(it.Tag as string, pathToSelect, StringComparison.OrdinalIgnoreCase))
                        {
                            it.Selected = true;
                            it.Focused = true;
                            list.EnsureVisible(it.Index);
                            found = true;
                            break;
                        }
                    }
                }

                if (!found && list.Items.Count > 0)
                {
                    list.Items[0].Selected = true;
                    list.Items[0].Focused = true;
                }

                ResizeColumns(list);
                RenderTabs();
                list.Focus();
                lastActive = list;
                UpdateStatus();
            }
            catch (Exception ex)
            {
                statusLabel.Text = " Greska: " + ex.Message;
                lastStatus = statusLabel.Text;
                try { list.EndUpdate(); } catch { }
                try { list.Focus(); } catch { }
            }
        }

        private string lastStatus = "";

        private void UpdateStatus()
        {
            int marked = ActiveMarked.Count;
            string sel = ActiveList.SelectedItems.Count > 0 ? (ActiveList.SelectedItems[0].Tag as string) : "";
            string txt = string.Format(" SEL: {0} | MARK: {1} | Tab:Ctrl+T/W/Tab | CMD dole", sel, marked);

            if (txt != lastStatus)
            {
                statusLabel.Text = txt;
                lastStatus = txt;
            }
        }

        private void PathBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                var box = sender as TextBox;
                var list = box == leftPath ? leftList : rightList;
                if (Directory.Exists(box.Text))
                    LoadFolder(list, box, box.Text, null);
                e.SuppressKeyPress = true;
            }
        }

                private void EnterFolder()
        {
            if (ActiveList.SelectedItems.Count == 0) return;

            var tag = ActiveList.SelectedItems[0].Tag as string;
            if (tag == null) return;

            if (Directory.Exists(tag))
            {
                try
                {
                    var test = Directory.EnumerateFileSystemEntries(tag).FirstOrDefault();
                }
                catch (UnauthorizedAccessException)
                {
                    statusLabel.Text = " Access denied: " + tag;
                    lastStatus = statusLabel.Text;
                    return;
                }
                catch { }

                string currentBefore = ActiveList == leftList ? leftCurrent : rightCurrent;
                bool isUp = ActiveList.SelectedItems[0].Text == "[..]";
                string toSelect = isUp ? currentBefore : null;
                LoadFolder(ActiveList, ActiveList == leftList ? leftPath : rightPath, tag, toSelect);
            }
            else if (File.Exists(tag))
            {
                try
                {
                    Process.Start(new ProcessStartInfo(tag) { UseShellExecute = true });
                }
                catch (Exception ex)
                {
                    statusLabel.Text = " Ne mogu da otvorim: " + ex.Message;
                    lastStatus = statusLabel.Text;
                }
            }
        }

        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // BUG FIX: ako kucas u cmdBox ili path boxevima, ne hvataj Alt+slovo i ostale precice
            var focused = this.ActiveControl;
            bool isTyping = focused is TextBoxBase || focused is ComboBox || (cmdBox != null && cmdBox.Focused) || (leftPath != null && leftPath.Focused) || (rightPath != null && rightPath.Focused);
            if (isTyping)
            {
                // dozvoli samo da prodje normalno kucanje
                return;
            }

            if (e.Alt && e.KeyCode == Keys.F7)
            {
                OpenSearchDialog();
                e.Handled = true;
                return;
            }

            if (e.Alt && e.KeyCode >= Keys.A && e.KeyCode <= Keys.Z)
            {
                JumpToFirst((char)e.KeyCode);
                e.Handled = true;
                return;
            }

            if (e.Shift && e.KeyCode == Keys.F6)
            {
                RenameSelected();
                e.Handled = true;
                return;
            }

            switch (e.KeyCode)
            {
                case Keys.Back:
                    var box = ActiveList == leftList ? leftPath : rightPath;
                    var parent = Directory.GetParent(box.Text);
                    if (parent != null)
                        LoadFolder(ActiveList, box, parent.FullName, box.Text);
                    break;

                case Keys.F4:
                    OpenInNotepad();
                    break;

                case Keys.F5:
                    CopySelected();
                    break;

                case Keys.F6:
                    MoveSelected();
                    break;

                case Keys.F7:
                    CreateFolder();
                    break;

                case Keys.F3:
                    RunCompareTool();
                    break;

                case Keys.F8:
                    CopyPathToClipboard();
                    break;
            }

            if (e.Control && e.KeyCode == Keys.C)
                CopyPathToClipboard();
        }

        private void JumpToFirst(char c)
        {
            var list = ActiveList;
            string search = c.ToString().ToLower();

            for (int i = 0; i < list.Items.Count; i++)
            {
                var name = list.Items[i].Text.Trim('[', ']', '.').ToLower();
                if (name.StartsWith(search))
                {
                    list.SelectedItems.Clear();
                    list.Items[i].Selected = true;
                    list.Items[i].Focused = true;
                    list.EnsureVisible(i);
                    list.Focus();
                    break;
                }
            }
        }

        private void SafeClipboard(string text)
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (ThreadStateException)
            {
                var t = new Thread(() =>
                {
                    try { Clipboard.SetText(text); } catch { }
                });
                t.SetApartmentState(ApartmentState.STA);
                t.Start();
                t.Join();
            }
            catch { }
        }

        private void CopyPathToClipboard()
        {
            if (ActiveList.SelectedItems.Count == 0) return;

            var item = ActiveList.SelectedItems[0];
            string path = item.Tag as string;
            if (item.Text == "[..]")
                path = ActiveList == leftList ? leftCurrent : rightCurrent;

            SafeClipboard(path);
            statusLabel.Text = " Kopirano: " + path;
            lastStatus = statusLabel.Text;
        }

        private void OpenInNotepad()
        {
            if (ActiveList.SelectedItems.Count == 0) return;
            var path = ActiveList.SelectedItems[0].Tag as string;
            if (itemIsFile(path))
                Process.Start("notepad.exe", "\"" + path + "\"");
        }

        private bool itemIsFile(string p)
        {
            return File.Exists(p);
        }

        private async void CopySelected()
        {
            var list = ActiveList;
            string destDir = list == leftList ? rightCurrent : leftCurrent;

            List<string> toCopy;
            if (ActiveMarked.Count > 0)
                toCopy = ActiveMarked.ToList();
            else
            {
                if (list.SelectedItems.Count == 0) return;
                var sel = list.SelectedItems[0].Tag as string;
                if (sel.EndsWith("..") || string.IsNullOrEmpty(sel)) return;
                toCopy = new List<string> { sel };
            }

            string keepFocus = list.SelectedItems.Count > 0 ? list.SelectedItems[0].Tag as string : null;

            if (MessageBox.Show("Kopirati " + toCopy.Count + " u " + destDir + "?", "F5", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            foreach (var src in toCopy)
            {
                try
                {
                    if (File.Exists(src))
                    {
                        string dest = Path.Combine(destDir, Path.GetFileName(src));
                        await Task.Run(() => File.Copy(src, dest, true));
                    }
                    else if (Directory.Exists(src))
                    {
                        string dest = Path.Combine(destDir, Path.GetFileName(src));
                        await Task.Run(() => CopyDirectory(src, dest));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }
            }

            LoadFolder(leftList, leftPath, leftCurrent, list == leftList ? keepFocus : null);
            LoadFolder(rightList, rightPath, rightCurrent, list == rightList ? keepFocus : null);
            ActiveMarked.Clear();
            UpdateStatus();
        }

        private void CopyDirectory(string src, string dest)
        {
            Directory.CreateDirectory(dest);
            foreach (var f in Directory.GetFiles(src))
                File.Copy(f, Path.Combine(dest, Path.GetFileName(f)), true);
            foreach (var d in Directory.GetDirectories(src))
                CopyDirectory(d, Path.Combine(dest, Path.GetFileName(d)));
        }

        private void MoveDirectory(string src, string dest)
        {
            // Ako je na istom drajvu, Directory.Move je brzi
            try
            {
                Directory.Move(src, dest);
            }
            catch
            {
                // ako je drugi drajv, kopiraj pa obrisi
                CopyDirectory(src, dest);
                Directory.Delete(src, true);
            }
        }

        // F6 Move - kao TC
        private async void MoveSelected()
        {
            var list = ActiveList;
            string destDir = list == leftList ? rightCurrent : leftCurrent;

            List<string> toMove;
            if (ActiveMarked.Count > 0)
                toMove = ActiveMarked.ToList();
            else
            {
                if (list.SelectedItems.Count == 0) return;
                var sel = list.SelectedItems[0].Tag as string;
                if (sel.EndsWith("..") || string.IsNullOrEmpty(sel)) return;
                toMove = new List<string> { sel };
            }

            string keepFocus = list.SelectedItems.Count > 0 ? list.SelectedItems[0].Tag as string : null;

            if (MessageBox.Show("Premestiti " + toMove.Count + " u " + destDir + "?", "F6 Move", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            foreach (var src in toMove)
            {
                try
                {
                    if (File.Exists(src))
                    {
                        string dest = Path.Combine(destDir, Path.GetFileName(src));
                        // ako postoji, pitaj ili pregazi? Za sad pregazi
                        if (File.Exists(dest))
                        {
                            var r = MessageBox.Show("Fajl vec postoji:\n" + dest + "\nPregaziti?", "F6 Move", MessageBoxButtons.YesNo);
                            if (r != DialogResult.Yes) continue;
                            File.Delete(dest);
                        }
                        await Task.Run(() => File.Move(src, dest));
                    }
                    else if (Directory.Exists(src))
                    {
                        string dest = Path.Combine(destDir, Path.GetFileName(src));
                        if (Directory.Exists(dest))
                        {
                            var r = MessageBox.Show("Folder vec postoji:\n" + dest + "\nPremestiti unutra?", "F6 Move", MessageBoxButtons.YesNo);
                            if (r != DialogResult.Yes) continue;
                            // premesti sadrzaj unutra
                            dest = Path.Combine(dest, Path.GetFileName(src));
                        }
                        await Task.Run(() => MoveDirectory(src, dest));
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greska pri premestanju " + src + ": " + ex.Message);
                }
            }

            LoadFolder(leftList, leftPath, leftCurrent, list == leftList ? keepFocus : null);
            LoadFolder(rightList, rightPath, rightCurrent, list == rightList ? keepFocus : null);
            ActiveMarked.Clear();
            UpdateStatus();
        }


        private void CreateFolder()
        {
            var box = ActiveList == leftList ? leftPath : rightPath;
            string input = Prompt.Show("Ime foldera:", "F7", "Novi folder");
            if (string.IsNullOrWhiteSpace(input)) return;

            Directory.CreateDirectory(Path.Combine(box.Text, input));
            LoadFolder(ActiveList, box, box.Text, Path.Combine(box.Text, input));
        }

        // 4. Shift+F6 Rename - custom prompt bez VB dll, sa selektovanim tekstom
        private void RenameSelected()
        {
            if (ActiveList.SelectedItems.Count == 0) return;

            var item = ActiveList.SelectedItems[0];
            string oldPath = item.Tag as string;

            if (string.IsNullOrEmpty(oldPath)) return;
            if (item.Text == "[..]") return;

            string oldName = Path.GetFileName(oldPath);
            string dir = Path.GetDirectoryName(oldPath);

            string newName = ShowRenameDialog(oldName);
            if (string.IsNullOrWhiteSpace(newName)) return;
            if (newName == oldName) return;

            // zabrani nevalidne karaktere
            if (newName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                MessageBox.Show("Ime sadrzi nedozvoljene karaktere: " + new string(Path.GetInvalidFileNameChars()));
                return;
            }

            string newPath = Path.Combine(dir, newName);

            if (File.Exists(newPath) || Directory.Exists(newPath))
            {
                MessageBox.Show("Vec postoji fajl/folder sa tim imenom: " + newPath);
                return;
            }

            try
            {
                if (File.Exists(oldPath))
                    File.Move(oldPath, newPath);
                else if (Directory.Exists(oldPath))
                    Directory.Move(oldPath, newPath);
                else
                    return;

                // osvezi listu i selektuj novi fajl
                var box = ActiveList == leftList ? leftPath : rightPath;
                LoadFolder(ActiveList, box, box.Text, newPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greska pri preimenovanju: " + ex.Message);
            }
        }

        private string ShowRenameDialog(string oldName)
        {
            Form prompt = new Form()
            {
                Width = 420,
                Height = 150,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Shift+F6 Rename",
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false
            };

            Label textLabel = new Label() { Left = 10, Top = 10, Width = 390, Text = "Novo ime:" };
            TextBox textBox = new TextBox() { Left = 10, Top = 35, Width = 380, Text = oldName };

            Button confirmation = new Button() { Text = "Ok", Left = 210, Width = 85, Top = 70, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Cancel", Left = 305, Width = 85, Top = 70, DialogResult = DialogResult.Cancel };

            prompt.Controls.AddRange(new Control[] { textBox, confirmation, cancel, textLabel });
            prompt.AcceptButton = confirmation;
            prompt.CancelButton = cancel;

            prompt.Shown += (s, e) =>
            {
                textBox.Focus();
                textBox.SelectAll();
            };

            return prompt.ShowDialog(this) == DialogResult.OK ? textBox.Text.Trim() : "";
        }

        // ========== 5. COMPARE TOOL ==========
        private void SetupCompareTool()
        {
            Form f = new Form()
            {
                Width = 600,
                Height = 220,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Podesi Compare Tool (pamti se u MiniTC.txt)",
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false
            };

            Label lblPath = new Label() { Left = 10, Top = 15, Width = 560, Text = "Putanja do BeyondCompare.exe / WinMergeU.exe / vsdiffmerge.exe itd:" };
            TextBox txtPath = new TextBox() { Left = 10, Top = 35, Width = 460, Text = compareToolPath };
            Button btnBrowse = new Button() { Left = 480, Top = 33, Width = 80, Text = "Browse..." };

            Label lblParams = new Label() { Left = 10, Top = 65, Width = 560, Text = "Parametri (ostavi prazno za default file1 file2 ili npr. %1 %2):" };
            TextBox txtParams = new TextBox() { Left = 10, Top = 85, Width = 560, Text = compareToolParams };

            Label lblHint = new Label() { Left = 10, Top = 110, Width = 560, Height = 30, ForeColor = Color.Gray, Text = @"Primer Beyond: C:\Program Files\Beyond Compare 4\BCompare.exe | Params: prazno" };

            Button ok = new Button() { Text = "Sacuvaj", Left = 380, Top = 145, Width = 90, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Otkaži", Left = 480, Top = 145, Width = 90, DialogResult = DialogResult.Cancel };

btnBrowse.Click += (s, e) =>
            {
                var thread = new Thread(() =>
                {
                    OpenFileDialog dlg = new OpenFileDialog()
                    {
                        Filter = "EXE files|*.exe|All files|*.*",
                        Title = "Izaberi compare tool"
                    };
                    if (!string.IsNullOrWhiteSpace(txtPath.Text) && File.Exists(txtPath.Text))
                    {
                        try { dlg.InitialDirectory = Path.GetDirectoryName(txtPath.Text); } catch { }
                    }
                    if (dlg.ShowDialog() == DialogResult.OK)
                    {
                        string file = dlg.FileName;
                        f.BeginInvoke(new Action(() => { txtPath.Text = file; }));
                    }
                });
                thread.SetApartmentState(ApartmentState.STA);
                thread.IsBackground = true;
                thread.Start();
            };

            f.Controls.AddRange(new Control[] { lblPath, txtPath, btnBrowse, lblParams, txtParams, lblHint, ok, cancel });
            f.AcceptButton = ok;
            f.CancelButton = cancel;

            if (f.ShowDialog(this) == DialogResult.OK)
            {
                compareToolPath = txtPath.Text.Trim();
                compareToolParams = txtParams.Text.Trim();
                SaveTabsToTxt();
                statusLabel.Text = " Compare tool sacuvan: " + compareToolPath;
                lastStatus = statusLabel.Text;
            }
        }

        private void RunCompareTool()
        {
            if (string.IsNullOrWhiteSpace(compareToolPath) || !File.Exists(compareToolPath))
            {
                var res = MessageBox.Show("Compare tool nije podesen ili ne postoji:\n" + compareToolPath + "\n\nDa otvoris podesavanja?", "F3 Compare", MessageBoxButtons.YesNo);
                if (res == DialogResult.Yes) SetupCompareTool();
                return;
            }

            string fileA = null;
            string fileB = null;

            // 1. probaj marked (crveno) - 1 levo + 1 desno
            if (markedLeft.Count == 1 && markedRight.Count == 1)
            {
                fileA = markedLeft.First();
                fileB = markedRight.First();
            }
            else if (markedLeft.Count == 2 && markedRight.Count == 0)
            {
                // oba na levoj strani obelezena
                var arr = markedLeft.ToArray();
                fileA = arr[0];
                fileB = arr[1];
            }
            else if (markedRight.Count == 2 && markedLeft.Count == 0)
            {
                var arr = markedRight.ToArray();
                fileA = arr[0];
                fileB = arr[1];
            }
            else
            {
                // 2. probaj selektovane (trenutno fokusirane)
                string leftSel = leftList.SelectedItems.Count > 0 ? leftList.SelectedItems[0].Tag as string : null;
                string rightSel = rightList.SelectedItems.Count > 0 ? rightList.SelectedItems[0].Tag as string : null;

                if (leftSel != null && rightSel != null && leftSel != rightSel && !leftSel.EndsWith("..") && !rightSel.EndsWith(".."))
                {
                    if (File.Exists(leftSel) && File.Exists(rightSel))
                    {
                        fileA = leftSel;
                        fileB = rightSel;
                    }
                }
            }

            if (fileA == null || fileB == null)
            {
                MessageBox.Show("Obelezi 2 fajla crveno (SPACE) - jedan u levom i jedan u desnom panelu, ili obelezi 2 fajla u istom panelu, pa pritisni F3.\n\nTrenutno: levo marked=" + markedLeft.Count + " desno marked=" + markedRight.Count, "F3 Compare");
                return;
            }

            if (!File.Exists(fileA) || !File.Exists(fileB))
            {
                MessageBox.Show("Compare radi samo sa fajlovima, ne sa folderima.\n" + fileA + "\n" + fileB);
                return;
            }

            try
            {
                string args = "";
                if (string.IsNullOrWhiteSpace(compareToolParams))
                {
                    args = "\"" + fileA + "\" \"" + fileB + "\"";
                }
                else
                {
                    // podrska za %1 %2 placeholder
                    if (compareToolParams.Contains("%1") || compareToolParams.Contains("%2"))
                    {
                        args = compareToolParams.Replace("%1", "\"" + fileA + "\"").Replace("%2", "\"" + fileB + "\"");
                    }
                    else
                    {
                        args = compareToolParams + " \"" + fileA + "\" \"" + fileB + "\"";
                    }
                }

                Process.Start(new ProcessStartInfo(compareToolPath, args) { UseShellExecute = true });
                statusLabel.Text = " Compare: " + Path.GetFileName(fileA) + " vs " + Path.GetFileName(fileB);
                lastStatus = statusLabel.Text;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Greska pri pokretanju compare tool-a: " + ex.Message + "\n\nPutanja: " + compareToolPath + "\nArgs: " + compareToolParams);
            }
        }

        // ========== 6. ALT+F7 SEARCH - FULL VERZIJA ==========
        private void OpenSearchDialog()
        {
            string root = ActiveList == leftList ? leftCurrent : rightCurrent;
            Form dlg = new Form()
            {
                Width = 450,
                Height = 200,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                Text = "Alt+F7 Pretraga - u: " + root,
                StartPosition = FormStartPosition.CenterParent,
                MinimizeBox = false,
                MaximizeBox = false,
                ShowInTaskbar = false
            };

            Label lbl = new Label() { Left = 10, Top = 15, Width = 400, Text = "Traži (npr *.cs , *form* , MiniTC):" };
            TextBox txt = new TextBox() { Left = 10, Top = 35, Width = 410, Text = "" };
            CheckBox chkSub = new CheckBox() { Left = 10, Top = 65, Width = 180, Text = "Uključi podfoldere", Checked = true };
            CheckBox chkCase = new CheckBox() { Left = 200, Top = 65, Width = 180, Text = "Case sensitive", Checked = false };

            Button ok = new Button() { Text = "Traži", Left = 240, Top = 100, Width = 80, DialogResult = DialogResult.OK };
            Button cancel = new Button() { Text = "Otkaži", Left = 330, Top = 100, Width = 90, DialogResult = DialogResult.Cancel };

            dlg.Controls.AddRange(new Control[] { lbl, txt, chkSub, chkCase, ok, cancel });
            dlg.AcceptButton = ok;
            dlg.CancelButton = cancel;
            txt.Focus();

            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                string pattern = txt.Text.Trim();
                if (string.IsNullOrWhiteSpace(pattern)) return;
                ShowSearchResults(pattern, root, chkSub.Checked, chkCase.Checked);
            }
        }

        private bool IsSearchMatch(string fileName, string pattern, bool caseSensitive)
        {
            try
            {
                StringComparison comp = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
                if (pattern.Contains("*") || pattern.Contains("?"))
                {
                    string regexPattern = "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
                    var opts = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;
                    return Regex.IsMatch(fileName, regexPattern, opts);
                }
                else
                {
                    return fileName.IndexOf(pattern, comp) >= 0;
                }
            }
            catch { return false; }
        }

        private void ShowSearchResults(string pattern, string root, bool includeSub, bool caseSensitive)
        {
            Form resForm = new Form()
            {
                Width = 900,
                Height = 600,
                Text = "Rezultati pretrage za '" + pattern + "' u " + root + " - DblClick/Enter locira fajl",
                StartPosition = FormStartPosition.CenterParent,
                WindowState = FormWindowState.Maximized,
                MinimizeBox = false,
                MaximizeBox = true
            };

            ListView lv = new ListView()
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                HideSelection = false,
                MultiSelect = false,
                BackColor = Color.Black,
                ForeColor = Color.White,
                Font = listFont
            };
            lv.Columns.Add("Ime", 250);
            lv.Columns.Add("Putanja", 400);
            lv.Columns.Add("Veličina", 80);
            lv.Columns.Add("Datum", 130);

            Label lblStatus = new Label() { Dock = DockStyle.Top, Height = 24, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.Lime, Text = " Pretraga u toku: " + root + " za '" + pattern + "'..." };
            Label lblCount = new Label() { Dock = DockStyle.Bottom, Height = 24, BackColor = Color.FromArgb(30, 30, 30), ForeColor = Color.Cyan, Text = " Pronađeno: 0" };

            Panel bottomPanel = new Panel() { Dock = DockStyle.Bottom, Height = 40, BackColor = Color.FromArgb(30, 30, 30) };
            Button btnGoto = new Button() { Text = "Idi na fajl (Enter)", Left = 10, Top = 8, Width = 140, BackColor = Color.Yellow };
            Button btnOpen = new Button() { Text = "Otvori", Left = 160, Top = 8, Width = 80 };
            Button btnClose = new Button() { Text = "Zatvori (Esc)", Left = 780, Top = 8, Width = 100, DialogResult = DialogResult.Cancel };
            bottomPanel.Controls.AddRange(new Control[] { btnGoto, btnOpen, btnClose });

            resForm.Controls.Add(lv);
            resForm.Controls.Add(bottomPanel);
            resForm.Controls.Add(lblCount);
            resForm.Controls.Add(lblStatus);

            List<string> foundFiles = new List<string>();
            bool cancelSearch = false;

            resForm.FormClosing += (s, e) => { cancelSearch = true; };

            Action<string> goToFile = (fullPath) =>
            {
                try
                {
                    if (File.Exists(fullPath))
                    {
                        string dir = Path.GetDirectoryName(fullPath);
                        // lociraj u aktivnom panelu
                        LoadFolder(ActiveList, ActiveList == leftList ? leftPath : rightPath, dir, fullPath);
                        resForm.Close();
                        ActiveList.Focus();
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        LoadFolder(ActiveList, ActiveList == leftList ? leftPath : rightPath, fullPath, null);
                        resForm.Close();
                        ActiveList.Focus();
                    }
                }
                catch (Exception ex) { MessageBox.Show("Greška pri lociranju: " + ex.Message); }
            };

            lv.DoubleClick += (s, e) =>
            {
                if (lv.SelectedItems.Count > 0)
                {
                    string p = lv.SelectedItems[0].Tag as string;
                    goToFile(p);
                }
            };

            lv.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter && lv.SelectedItems.Count > 0)
                {
                    string p = lv.SelectedItems[0].Tag as string;
                    goToFile(p);
                    e.SuppressKeyPress = true;
                }
            };

            btnGoto.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count > 0)
                {
                    string p = lv.SelectedItems[0].Tag as string;
                    goToFile(p);
                }
            };

            btnOpen.Click += (s, e) =>
            {
                if (lv.SelectedItems.Count > 0)
                {
                    string p = lv.SelectedItems[0].Tag as string;
                    try { Process.Start(new ProcessStartInfo(p) { UseShellExecute = true }); } catch { }
                }
            };

            // Pokreni pretragu u background threadu
            Task.Run(() =>
            {
                try
                {
                    var stack = new Stack<string>();
                    stack.Push(root);
                    int count = 0;
                    while (stack.Count > 0 && !cancelSearch && count < 1000)
                    {
                        string currentDir = stack.Pop();
                        try
                        {
                            // fajlovi u ovom folderu
                            foreach (var file in Directory.EnumerateFiles(currentDir))
                            {
                                if (cancelSearch || count >= 1000) break;
                                try
                                {
                                    string name = Path.GetFileName(file);
                                    if (IsSearchMatch(name, pattern, caseSensitive))
                                    {
                                        lock (foundFiles) { foundFiles.Add(file); }
                                        count++;
                                        // update UI
                                        resForm.BeginInvoke(new Action(() =>
                                        {
                                            try
                                            {
                                                var fi = new FileInfo(file);
                                                var item = new ListViewItem(fi.Name) { Tag = file };
                                                item.SubItems.Add(Path.GetDirectoryName(file));
                                                item.SubItems.Add(FormatSize(fi.Length));
                                                item.SubItems.Add(fi.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));
                                                lv.Items.Add(item);
                                                lblCount.Text = " Pronađeno: " + lv.Items.Count + (count >= 1000 ? " (limit 1000)" : "");
                                            }
                                            catch { }
                                        }));
                                    }
                                }
                                catch { }
                            }

                            // folderi - da li da idemo rekurzivno?
                            if (includeSub)
                            {
                                foreach (var dir in Directory.EnumerateDirectories(currentDir))
                                {
                                    if (cancelSearch) break;
                                    try
                                    {
                                        var di = new DirectoryInfo(dir);
                                        if ((di.Attributes & FileAttributes.Hidden) != 0) continue;
                                        if ((di.Attributes & FileAttributes.ReparsePoint) != 0) continue; // preskoci symlink/junction da ne vrti u krug

                                        string dirName = Path.GetFileName(dir);
                                        // ako se i ime foldera poklapa sa patternom, dodaj i njega
                                        if (IsSearchMatch(dirName, pattern, caseSensitive))
                                        {
                                            lock (foundFiles) { foundFiles.Add(dir); }
                                            count++;
                                            resForm.BeginInvoke(new Action(() =>
                                            {
                                                try
                                                {
                                                    var item = new ListViewItem("[" + dirName + "]") { Tag = dir };
                                                    item.SubItems.Add(Path.GetDirectoryName(dir));
                                                    item.SubItems.Add("<DIR>");
                                                    item.SubItems.Add(di.LastWriteTime.ToString("dd.MM.yyyy HH:mm"));
                                                    lv.Items.Add(item);
                                                    lblCount.Text = " Pronađeno: " + lv.Items.Count;
                                                }
                                                catch { }
                                            }));
                                        }

                                        stack.Push(dir);
                                    }
                                    catch { }
                                }
                            }

                            resForm.BeginInvoke(new Action(() => { lblStatus.Text = " Pretraga: " + currentDir + " | nađeno: " + count; }));
                        }
                        catch { }
                    }

                    resForm.BeginInvoke(new Action(() =>
                    {
                        lblStatus.Text = " Pretraga završena u " + root + " | ukupno: " + foundFiles.Count + " rezultata za '" + pattern + "' (Esc za zatvaranje, Enter/DblClick za lociranje)";
                        lblCount.Text = " Pronađeno: " + foundFiles.Count + (foundFiles.Count >= 1000 ? " (limit 1000)" : "") + " | DblClick ili Enter locira fajl u panelu";
                        if (lv.Items.Count > 0) { lv.Items[0].Selected = true; lv.Items[0].Focused = true; lv.Focus(); }
                    }));
                }
                catch (Exception ex)
                {
                    try { resForm.BeginInvoke(new Action(() => { lblStatus.Text = " Greška: " + ex.Message; })); } catch { }
                }
            });

            resForm.ShowDialog(this);
        }

        private string FormatSize(long bytes)
        {
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024) + " KB";
            return (bytes / (1024 * 1024)) + " MB";
        }

        private void DeleteSelected(bool permanent)
        {
            List<string> toDel;

            if (ActiveMarked.Count > 0)
                toDel = ActiveMarked.ToList();
            else
            {
                if (ActiveList.SelectedItems.Count == 0) return;
                var sel = ActiveList.SelectedItems[0].Tag as string;
                if (sel.EndsWith("..")) return;
                toDel = new List<string> { sel };
            }

            Form delForm = new Form()
            {
                Width = 600,
                Height = 400,
                Text = permanent ? "Trajno brisanje (Shift+Del)" : "Brisanje u Recycle Bin (Del)",
                StartPosition = FormStartPosition.CenterParent,
                FormBorderStyle = FormBorderStyle.FixedDialog,
                MinimizeBox = false,
                MaximizeBox = false
            };

            Label lbl = new Label() { Left = 10, Top = 10, Width = 560, Height = 40, Text = permanent ? "TRAJNO obrisati?" : "Poslati u Recycle Bin?" };
            ListBox lb = new ListBox() { Left = 10, Top = 50, Width = 560, Height = 270 };
            lb.Items.AddRange(toDel.ToArray());

            Button ok = new Button() { Text = permanent ? "Trajno" : "Recycle", Left = 350, Top = 330, Width = 110, DialogResult = DialogResult.OK, BackColor = permanent ? Color.Red : Color.Orange };
            Button cancel = new Button() { Text = "Otkaži", Left = 470, Top = 330, Width = 100, DialogResult = DialogResult.Cancel };

            delForm.Controls.AddRange(new Control[] { lbl, lb, ok, cancel });
            delForm.AcceptButton = ok;
            delForm.CancelButton = cancel;

            if (delForm.ShowDialog() != DialogResult.OK) return;

            foreach (var p in toDel)
            {
                try
                {
                    if (File.Exists(p))
                    {
                        if (permanent) File.Delete(p);
                        else RecycleBin.SendToRecycle(p);
                    }
                    else if (Directory.Exists(p))
                    {
                        if (permanent) Directory.Delete(p, true);
                        else RecycleBin.SendToRecycle(p);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Greška " + p + ": " + ex.Message);
                }
            }

            ActiveMarked.Clear();
            LoadFolder(ActiveList, ActiveList == leftList ? leftPath : rightPath, ActiveList == leftList ? leftCurrent : rightCurrent, null);
        }
    }

}
