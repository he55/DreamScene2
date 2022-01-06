using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DreamScene2
{
    public partial class MainDialog : Form
    {
        VideoWindow _videoWindow;
        WebWindow _webWindow;
        IntPtr _desktopWindowHandle;
        string _recentPath = Helper.GetPathForAppFolder("recent.txt");
        List<string> _recentFiles = new List<string>();
        bool _isPlaying;
        PerformanceCounter _performanceCounter;
        Settings _settings = Settings.Load();
        Screen _screen;
        int _screenIndex;
        IntPtr _windowHandle;
        bool _isSuspend;
        uint _d3dRenderingSubProcessPid;

        string[] HtmlFileTypes = new string[] { ".htm", ".html", ".mhtml" };
        string[] VideoFileTypes = new string[] { ".mp4", ".mov" };

        public MainDialog()
        {
            InitializeComponent();
            this.Text = Constant.MainWindowTitle;
            this.Icon = DreamScene2.Properties.Resources.icon;
            notifyIcon1.Icon = this.Icon;
            trackBar1.Value = _settings.Volume;
            toolStripMenuItem3.Checked = checkMute.Checked = _settings.IsMuted;
            toolStripMenuItem6.Checked = checkAutoPlay.Checked = _settings.AutoPlay;
            toolStripMenuItem23.Checked = _settings.AutoPause1;
            toolStripMenuItem24.Checked = _settings.AutoPause2;
            toolStripMenuItem25.Checked = _settings.AutoPause3;
            toolStripMenuItem26.Checked = _settings.DisableWebSecurity;
            toolStripMenuItem27.Checked = _settings.DesktopInteraction;
        }


        #region 私有方法

        static bool TryGetWebView2Version(out string version)
        {
            try
            {
                version = CoreWebView2Environment.GetAvailableBrowserVersionString();
                return true;
            }
            catch
            {
                version = null;
                return false;
            }
        }

        void SaveRecentFile(string path)
        {
            if (_recentFiles.Count == 0 || _recentFiles[0] != path)
            {
                if (_recentFiles.Contains(path))
                    _recentFiles.Remove(path);
                _recentFiles.Insert(0, path);

                File.WriteAllLines(_recentPath, _recentFiles);
            }
        }

        void PlayVideo()
        {
            _isPlaying = true;
            _videoWindow.Play();
            toolStripMenuItem2.Text = btnPlay.Text = "暂停";
        }

        void PauseVideo()
        {
            _isPlaying = false;
            _videoWindow.Pause();
            toolStripMenuItem2.Text = btnPlay.Text = "播放";
        }

        void OpenFile(string path)
        {
            Uri uri = new Uri(path);
            SaveRecentFile(path);

            if (uri.Scheme == "http" || uri.Scheme == "https")
            {
                OpenWeb(path);
            }
            else if (uri.Scheme == "file" && File.Exists(path))
            {
                if (HtmlFileTypes.Contains(Path.GetExtension(path).ToLower()))
                    OpenWeb(uri.AbsoluteUri);
                else
                    OpenVideo(path);
            }
        }

        void OpenVideo(string path)
        {
            CloseWindow(WindowType.Video);

            if (_videoWindow == null)
            {
                _videoWindow = new VideoWindow();
                _videoWindow.IsMuted = _settings.IsMuted;
                _videoWindow.Volume = _settings.Volume / 10.0;
                _videoWindow.SetPosition(_screen.Bounds);
                _videoWindow.Show();

                PInvoke.SetParent(_videoWindow.GetHandle(), _desktopWindowHandle);
            }

            _videoWindow.Source = new Uri(path, UriKind.Absolute);
            _videoWindow.Play();

            toolStripMenuItem2.Enabled = btnPlay.Enabled = true;
            toolStripMenuItem3.Enabled = checkMute.Enabled = true;
            toolStripMenuItem5.Enabled = btnClose.Enabled = true;
            trackBar1.Enabled = !_settings.IsMuted;

            _isPlaying = true;
            toolStripMenuItem2.Text = btnPlay.Text = "暂停";
            timer1.Enabled = _settings.AutoPause1 || _settings.AutoPause2 || _settings.AutoPause3;
        }

        void OpenWeb(string url)
        {
            if (!TryGetWebView2Version(out _))
            {
                MessageBox.Show("打开网页功能需要 WebView2 支持。请在托盘图标找到 DreamScene2 然后右键菜单，依次点击 [打开 URL] > [安装 WebView2...] 安装");
                return;
            }

            CloseWindow(WindowType.Web);

            if (_webWindow == null)
            {
                WebWindowOptions webWindowOptions = new WebWindowOptions();
                webWindowOptions.UserDataFolder = Helper.GetPathForAppFolder("");
                webWindowOptions.DisableWebSecurity = _settings.DisableWebSecurity;

                _webWindow = new WebWindow(webWindowOptions);
                _webWindow.SetPosition(_screen.Bounds);
                _webWindow.Show();

                PInvoke.SetParent(_webWindow.GetHandle(), _desktopWindowHandle);
            }

            _webWindow.Source = new Uri(url);

            toolStripMenuItem2.Enabled = btnPlay.Enabled = true;
            toolStripMenuItem3.Enabled = checkMute.Enabled = true;
            toolStripMenuItem5.Enabled = btnClose.Enabled = true;

            toolStripMenuItem2.Text = btnPlay.Text = "暂停";

            if (_settings.DesktopInteraction)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(500);
                    this.Invoke((Action)ForwardMessage);
                });
            }
        }

        void SetWindow(IntPtr hWnd)
        {
            CloseWindow(WindowType.Window);

            if (_windowHandle != hWnd)
            {
                _windowHandle = hWnd;

                PInvoke.DS2_SetWindowPosition(hWnd, _screen.Bounds.ToRECT());
                PInvoke.SetParent(hWnd, _desktopWindowHandle);
            }

            toolStripMenuItem5.Enabled = btnClose.Enabled = true;
        }

        enum WindowType
        {
            None,
            Video,
            Web,
            Window
        }

        WindowType lxc;

        void CloseWindow(WindowType xc)
        {
            toolStripMenuItem5.Enabled = btnClose.Enabled = false;

            if (lxc == WindowType.Web)
            {
                if (_settings.DesktopInteraction)
                {
                    PInvoke.DS2_EndForwardMouseKeyboardMessage();
                }

                if (_isSuspend)
                {
                    PInvoke.DS2_ToggleProcess(_d3dRenderingSubProcessPid, 1);
                    _isSuspend = false;
                }
            }

            if (lxc == WindowType.Video && lxc != xc)
            {
                timer1.Enabled = false;
                _isPlaying = false;
                toolStripMenuItem2.Text = btnPlay.Text = "播放";

                toolStripMenuItem2.Enabled = btnPlay.Enabled = false;
                toolStripMenuItem3.Enabled = checkMute.Enabled = false;
                toolStripMenuItem5.Enabled = btnClose.Enabled = false;
                trackBar1.Enabled = false;

                _videoWindow.Close();
                _videoWindow = null;
            }
            else if (lxc == WindowType.Web && lxc != xc)
            {
                toolStripMenuItem2.Text = btnPlay.Text = "播放";

                toolStripMenuItem2.Enabled = btnPlay.Enabled = false;
                toolStripMenuItem3.Enabled = checkMute.Enabled = false;
                toolStripMenuItem5.Enabled = btnClose.Enabled = false;

                _webWindow.Close();
                _webWindow = null;
            }
            else if (lxc == WindowType.Window)
            {
                _windowHandle = IntPtr.Zero;
                PInvoke.DS2_RestoreLastWindowPosition();
            }

            lxc = xc;

            GC.Collect();
            PInvoke.DS2_RefreshDesktop();
        }

        void ForwardMessage()
        {
            IntPtr hWnd = _webWindow.GetChromeWidgetWin1Handle();
            if (hWnd != IntPtr.Zero)
            {
                PInvoke.DS2_StartForwardMouseKeyboardMessage(hWnd);
            }
        }

        #endregion


        #region 控件事件

        private void Form1_Load(object sender, EventArgs e)
        {
            Task.Run(() =>
            {
                _performanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            });

            _screen = Screen.PrimaryScreen;

            _desktopWindowHandle = PInvoke.DS2_GetDesktopWindowHandle();
            if (_desktopWindowHandle == IntPtr.Zero)
            {
                btnOpenFile.Enabled = false;
                checkAutoPlay.Enabled = false;
                notifyIcon1.Visible = false;
                label1.Visible = true;
                return;
            }

            if (File.Exists(_recentPath))
            {
                string[] paths = File.ReadAllLines(_recentPath);
                _recentFiles.AddRange(paths);
            }

            if (_settings.AutoPlay && _recentFiles.Count != 0)
                OpenFile(_recentFiles[0]);
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_desktopWindowHandle == IntPtr.Zero)
            {
                Environment.Exit(0);
                return;
            }

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();

                if (_settings.FirstRun)
                {
                    notifyIcon1.ShowBalloonTip(1000, "", "程序正在后台运行", ToolTipIcon.None);
                    _settings.FirstRun = false;
                }
            }
            else
            {
                CloseWindow(WindowType.None);
                Settings.Save();
            }
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            var htmlTypes = string.Join(";", HtmlFileTypes.Select(x => $"*{x}"));
            var videoTypes = string.Join(";", VideoFileTypes.Select(x => $"*{x}"));
            var allTypes = $"{videoTypes};{htmlTypes}";
            openFileDialog.Filter = $"All Files|{allTypes}|Video Files|{videoTypes}|HTML Files|{htmlTypes}";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                OpenFile(openFileDialog.FileName);
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (_videoWindow != null)
            {
                if (_isPlaying)
                {
                    timer1.Enabled = false;
                    PauseVideo();
                }
                else
                {
                    PlayVideo();
                    timer1.Enabled = _settings.AutoPause1 || _settings.AutoPause2 || _settings.AutoPause3;
                }
            }

            if (_webWindow != null)
            {
                if (_isSuspend)
                {
                    PInvoke.DS2_ToggleProcess(_d3dRenderingSubProcessPid, 1);
                    _isSuspend = false;
                    toolStripMenuItem2.Text = btnPlay.Text = "暂停";
                }
                else
                {
                    _d3dRenderingSubProcessPid = _webWindow.GetD3DRenderingSubProcessPid();
                    if (_d3dRenderingSubProcessPid != 0)
                    {
                        PInvoke.DS2_ToggleProcess(_d3dRenderingSubProcessPid, 0);
                        _isSuspend = true;
                        toolStripMenuItem2.Text = btnPlay.Text = "播放";
                    }
                }
            }
        }

        private void checkMute_Click(object sender, EventArgs e)
        {
            _settings.IsMuted = toolStripMenuItem3.Checked = checkMute.Checked;
            trackBar1.Enabled = !_settings.IsMuted;
            _videoWindow.IsMuted = _settings.IsMuted;
        }

        private void checkAutoPlay_Click(object sender, EventArgs e)
        {
            _settings.AutoPlay = toolStripMenuItem6.Checked = checkAutoPlay.Checked;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            _settings.Volume = trackBar1.Value;
            _videoWindow.Volume = _settings.Volume / 10.0;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            CloseWindow(WindowType.None);
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.Show();
            this.Activate();
        }

        void array_push(int[] arr, int val)
        {
            for (int i = 0; i < arr.Length - 1; i++)
            {
                arr[i] = arr[i + 1];
            }
            arr[arr.Length - 1] = val;
        }

        int array_sum(int[] arr)
        {
            int sum = 0;
            for (int i = 0; i < arr.Length; i++)
            {
                sum += arr[i];
            }
            return sum;
        }

        bool array_is_max(int[] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                if (arr[i] == 0)
                    return false;
            }
            return true;
        }

        int[] cpuarr = new int[5];
        int[] parr = new int[5];

        private void timer1_Tick(object sender, EventArgs e)
        {
            bool fullScreen;
            if (_settings.AutoPause3)
            {
                fullScreen = PInvoke.DS2_TestScreen(_screen.WorkingArea.ToRECT()) == 0;
                if (fullScreen)
                {
                    if (_isPlaying)
                        PauseVideo();
                    return;
                }
            }
            else
            {
                fullScreen = false;
            }

            if (_settings.AutoPause2)
            {
                float val = _performanceCounter?.NextValue() ?? 0;
                array_push(cpuarr, val > 15.0 ? 1 : 0);

                if (array_is_max(cpuarr))
                {
                    if (_isPlaying)
                        PauseVideo();
                    return;
                }
            }
            else
            {
                array_push(cpuarr, 0);
            }

            if (_settings.AutoPause1)
            {
                bool val = PInvoke.DS2_GetLastInputTickCount() < 500;
                array_push(parr, val ? 1 : 0);

                if (array_is_max(parr))
                {
                    if (_isPlaying)
                        PauseVideo();
                    return;
                }
            }
            else
            {
                array_push(parr, 0);
            }

            if (!fullScreen && !_isPlaying && array_sum(cpuarr) == 0 && array_sum(parr) == 0)
            {
                PlayVideo();
            }
        }

        #endregion


        #region 菜单事件

        private void contextMenuStrip1_Opening(object sender, System.ComponentModel.CancelEventArgs e)
        {
            toolStripMenuItem12.Checked = Helper.CheckStartOnBoot();
            int val = PInvoke.DS2_IsVisibleDesktopIcons();
            toolStripMenuItem13.Text = val != 0 ? "隐藏桌面图标" : "显示桌面图标";
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            notifyIcon1_MouseDoubleClick(null, null);
        }

        private void toolStripMenuItem2_Click(object sender, EventArgs e)
        {
            btnPlay_Click(null, null);
        }

        private void toolStripMenuItem3_Click(object sender, EventArgs e)
        {
            checkMute.Checked = !checkMute.Checked;
            checkMute_Click(null, null);
        }

        private void toolStripMenuItem4_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void toolStripMenuItem5_Click(object sender, EventArgs e)
        {
            btnClose_Click(null, null);
        }

        private void toolStripMenuItem6_Click(object sender, EventArgs e)
        {
            checkAutoPlay.Checked = !checkAutoPlay.Checked;
            checkAutoPlay_Click(null, null);
        }

        private void toolStripMenuItem7_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItem7.DropDownItems.Clear();
            for (int i = 0; i < _recentFiles.Count; i++)
            {
                string filePath = _recentFiles[i];
                ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem($"{i + 1}. {filePath}");
                toolStripMenuItem.Tag = filePath;
                toolStripMenuItem.Click += ToolStripMenuItem_Click1;
                toolStripMenuItem7.DropDownItems.Add(toolStripMenuItem);
            }

            toolStripMenuItem8.Enabled = _recentFiles.Count != 0;
            toolStripMenuItem7.DropDownItems.Add(toolStripMenuItem8);
        }

        private void ToolStripMenuItem_Click1(object sender, EventArgs e)
        {
            OpenFile((string)((ToolStripMenuItem)sender).Tag);
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            _recentFiles.Clear();
            File.WriteAllText(_recentPath, "");
        }

        private void toolStripMenuItem9_Click(object sender, EventArgs e)
        {
            btnOpenFile_Click(null, null);
        }

        private void toolStripMenuItem12_Click(object sender, EventArgs e)
        {
            toolStripMenuItem12.Checked = !toolStripMenuItem12.Checked;
            if (toolStripMenuItem12.Checked)
                Helper.SetStartOnBoot();
            else
                Helper.RemoveStartOnBoot();
        }

        private void toolStripMenuItem14_Click(object sender, EventArgs e)
        {
            AboutDialog aboutDialog = new AboutDialog();
            aboutDialog.Show();
        }

        private void toolStripMenuItem10_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItem10.DropDownItems.Clear();

            Screen[] allScreens = Screen.AllScreens;
            if (allScreens.Length == 0)
            {
                toolStripMenuItem10.DropDownItems.Add(toolStripMenuItem11);
                return;
            }

            for (int i = 0; i < allScreens.Length; i++)
            {
                ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(allScreens[i].Primary ? allScreens[i].DeviceName + " - Primary" : allScreens[i].DeviceName);
                toolStripMenuItem.Checked = _screenIndex == i;
                toolStripMenuItem.Tag = i;
                toolStripMenuItem.Click += ToolStripMenuItem_Click2;
                toolStripMenuItem10.DropDownItems.Add(toolStripMenuItem);
            }
        }

        private void ToolStripMenuItem_Click2(object sender, EventArgs e)
        {
            _screenIndex = (int)((ToolStripMenuItem)sender).Tag;
            _screen = Screen.AllScreens[_screenIndex];

            PInvoke.DS2_RefreshDesktop();
            _videoWindow?.SetPosition(_screen.Bounds);
        }

        private void toolStripMenuItem16_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItem16.DropDownItems.Clear();

            string[] files = Directory.GetFiles(Helper.ExtPath());
            if (files.Length == 0)
            {
                toolStripMenuItem16.DropDownItems.Add(toolStripMenuItem17);
                return;
            }

            foreach (string filePath in files)
            {
                string fileName = Path.GetFileName(filePath);
                string[] arr = fileName.Split(new string[] { "-" }, StringSplitOptions.RemoveEmptyEntries);
                if (int.TryParse(arr[0], System.Globalization.NumberStyles.HexNumber, null, out int val))
                {
                    IntPtr ptr = (IntPtr)val;
                    bool b = PInvoke.IsWindowVisible(ptr);
                    ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(arr[1] + (b ? "" : " (Invalidate)"));
                    toolStripMenuItem.Enabled = b;
                    toolStripMenuItem.Checked = _windowHandle == ptr;
                    toolStripMenuItem.Tag = val;
                    toolStripMenuItem.Click += ToolStripMenuItem_Click3;
                    toolStripMenuItem16.DropDownItems.Add(toolStripMenuItem);
                }
            }
        }

        private void ToolStripMenuItem_Click3(object sender, EventArgs e)
        {
            int hWnd = (int)((ToolStripMenuItem)sender).Tag;
            SetWindow((IntPtr)hWnd);
        }

        private void toolStripMenuItem18_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItem18.DropDownItems.Clear();

            if (TryGetWebView2Version(out string version))
            {
                toolStripMenuItem21.Text = $"WebView2 {version}";
                toolStripMenuItem18.DropDownItems.Add(toolStripMenuItem21);
                toolStripMenuItem18.DropDownItems.Add(toolStripMenuItem26);
                toolStripMenuItem18.DropDownItems.Add(toolStripMenuItem20);
            }
            else
            {
                toolStripMenuItem18.DropDownItems.Add(toolStripMenuItem19);
            }
        }

        private void toolStripMenuItem19_Click(object sender, EventArgs e)
        {
            Helper.OpenLink("https://developer.microsoft.com/en-us/microsoft-edge/webview2/consumer/");
        }

        private void toolStripMenuItem20_Click(object sender, EventArgs e)
        {
            InputDialog inputDialog = new InputDialog();
            if (inputDialog.ShowDialog() == DialogResult.OK)
                OpenFile(inputDialog.URL);
        }

        private void toolStripMenuItem23_Click(object sender, EventArgs e)
        {
            if (sender == toolStripMenuItem23)
            {
                _settings.AutoPause1 = toolStripMenuItem23.Checked = !toolStripMenuItem23.Checked;
            }
            else if (sender == toolStripMenuItem24)
            {
                _settings.AutoPause2 = toolStripMenuItem24.Checked = !toolStripMenuItem24.Checked;
            }
            else if (sender == toolStripMenuItem25)
            {
                _settings.AutoPause3 = toolStripMenuItem25.Checked = !toolStripMenuItem25.Checked;
            }

            if (_videoWindow != null)
            {
                if (_settings.AutoPause1 || _settings.AutoPause2 || _settings.AutoPause3)
                {
                    timer1.Enabled = true;
                }
                else
                {
                    timer1.Enabled = false;
                    if (!_isPlaying) PlayVideo();
                }
            }
        }

        private void toolStripMenuItem26_Click(object sender, EventArgs e)
        {
            _settings.DisableWebSecurity = toolStripMenuItem26.Checked = !toolStripMenuItem26.Checked;
        }

        private void toolStripMenuItem13_Click(object sender, EventArgs e)
        {
            PInvoke.DS2_ToggleShowDesktopIcons();
        }

        private void toolStripMenuItem27_Click(object sender, EventArgs e)
        {
            _settings.DesktopInteraction = toolStripMenuItem27.Checked = !toolStripMenuItem27.Checked;
            if (_webWindow != null)
            {
                if (_settings.DesktopInteraction)
                    ForwardMessage();
                else
                    PInvoke.DS2_EndForwardMouseKeyboardMessage();
            }
        }

        private void toolStripMenuItem28_Click(object sender, EventArgs e)
        {
            Type shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType != null)
            {
                object shellObject = Activator.CreateInstance(shellType);
                shellType.InvokeMember("ToggleDesktop", System.Reflection.BindingFlags.InvokeMethod, null, shellObject, null);
            }
        }

        #endregion


        protected override void WndProc(ref Message m)
        {
            if (m.Msg == (0x0400 + 1001))
            {
                SetWindow(m.WParam);
                return;
            }
            base.WndProc(ref m);
        }
    }
}
