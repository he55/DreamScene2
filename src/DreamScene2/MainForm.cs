using Microsoft.Web.WebView2.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DreamScene2
{
    public partial class MainForm : Form
    {
        VideoWindow _videoWindow;
        WebWindow _webWindow;
        IntPtr _desktopWindowHandle;
        List<string> _recentFiles;
        bool _isPlaying;
        PerformanceCounter _performanceCounter;
        Settings _settings = Settings.Load();
        Screen _screen;
        int _screenIndex;
        IntPtr _windowHandle;
        HashSet<IntPtr> _hWndSet = new HashSet<IntPtr>();
        bool _isWebPlaying;
        uint _d3dRenderingSubProcessPid;

        string[] HtmlFileTypes = new string[] { ".htm", ".html", ".mhtml" };
        string[] VideoFileTypes = new string[] { ".mp4", ".mov" };

        public MainForm()
        {
            InitializeComponent();
            this.Text = Constant.MainWindowTitle;
            this.Icon = DreamScene2.Properties.Resources.AppIcon;
            notifyIcon1.Icon = this.Icon;
            toolStripMenuItem3.Checked = checkMute.Checked = _settings.IsMuted;
            toolStripMenuItem6.Checked = checkAutoPlay.Checked = _settings.AutoPlay;
            toolStripMenuItem23.Checked = _settings.AutoPause1;
            toolStripMenuItem24.Checked = _settings.AutoPause2;
            toolStripMenuItem25.Checked = _settings.AutoPause3;
            toolStripMenuItem26.Checked = _settings.DisableWebSecurity;
            toolStripMenuItem27.Checked = _settings.UseDesktopInteraction;
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

        void PlayWeb()
        {
            PInvoke.DS2_ToggleProcess(_d3dRenderingSubProcessPid, 1);
            _isWebPlaying = true;
            toolStripMenuItem2.Text = btnPlay.Text = "暂停";
        }

        void PauseWeb()
        {
            _d3dRenderingSubProcessPid = _webWindow.GetD3DRenderingSubProcessPid();
            if (_d3dRenderingSubProcessPid != 0)
            {
                PInvoke.DS2_ToggleProcess(_d3dRenderingSubProcessPid, 0);
                _isWebPlaying = false;
                toolStripMenuItem2.Text = btnPlay.Text = "播放";
            }
        }

        void OpenFile(string path)
        {
            RecentFile.Update(path);

            Uri uri = new Uri(path);
            if (uri.Scheme == "http" || uri.Scheme == "https")
            {
                OpenWeb(path);
            }
            else if (uri.Scheme == "file")
            {
                if (!File.Exists(path))
                {
                    MessageBox.Show($"找不到文件 \"{path}\"", Constant.ProjectName);
                    return;
                }

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
                _videoWindow.SetPosition(_screen.Bounds);
                _videoWindow.Show();

                PInvoke.SetParent(_videoWindow.GetHandle(), _desktopWindowHandle);
            }

            _videoWindow.Source = new Uri(path, UriKind.Absolute);
            _videoWindow.Play();

            _isPlaying = true;
            EnableControl();
        }

        void OpenWeb(string url)
        {
            if (!TryGetWebView2Version(out _))
            {
                MessageBox.Show("打开网页功能需要 WebView2 支持。请在托盘图标找到 DreamScene2 然后右键菜单，依次点击 [打开 URL] > [安装 WebView2...] 安装。", Constant.ProjectName);
                return;
            }

            CloseWindow(WindowType.Web);

            if (_webWindow == null)
            {
                WebWindowOptions webWindowOptions = new WebWindowOptions();
                webWindowOptions.UserDataFolder = Helper.GetPathForUserAppDataFolder("");
                webWindowOptions.DisableWebSecurity = _settings.DisableWebSecurity;
                webWindowOptions.IsMuted = _settings.IsMuted;

                _webWindow = new WebWindow(webWindowOptions);
                _webWindow.SetPosition(_screen.Bounds);
                _webWindow.Show();

                PInvoke.SetParent(_webWindow.GetHandle(), _desktopWindowHandle);
            }

            _webWindow.Source = new Uri(url);

            _isWebPlaying = true;
            EnableControl();

            if (_webWindow.Source.Host.EndsWith("bilibili.com"))
            {
                toolStripMenuItem27.Enabled = false;
            }
            else if (_settings.UseDesktopInteraction)
            {
                Task.Run(async () =>
                {
                    await Task.Delay(500);
                    this.Invoke((Action)ForwardMessage);
                });
            }
        }

        void EnableControl()
        {
            toolStripMenuItem2.Enabled = btnPlay.Enabled = true;
            toolStripMenuItem3.Enabled = checkMute.Enabled = true;
            toolStripMenuItem5.Enabled = btnClose.Enabled = true;

            toolStripMenuItem2.Text = btnPlay.Text = "暂停";
            timer1.Enabled = _settings.CanPause();
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

        WindowType _lwt;

        void CloseWindow(WindowType wt)
        {
            toolStripMenuItem5.Enabled = btnClose.Enabled = false;
            toolStripMenuItem27.Enabled = true;

            if (_lwt == WindowType.Web)
            {
                if (_settings.UseDesktopInteraction)
                    PInvoke.DS2_EndForwardMouseKeyboardMessage();

                if (!_isWebPlaying)
                {
                    PInvoke.DS2_ToggleProcess(_d3dRenderingSubProcessPid, 1);
                    _isWebPlaying = true;
                }
            }

            if (_lwt == WindowType.Video && _lwt != wt)
            {
                timer1.Enabled = false;
                _isPlaying = false;
                toolStripMenuItem2.Text = btnPlay.Text = "播放";

                toolStripMenuItem2.Enabled = btnPlay.Enabled = false;
                toolStripMenuItem3.Enabled = checkMute.Enabled = false;
                toolStripMenuItem5.Enabled = btnClose.Enabled = false;

                _videoWindow.Close();
                _videoWindow = null;
            }
            else if (_lwt == WindowType.Web && _lwt != wt)
            {
                timer1.Enabled = false;
                _isWebPlaying = false;
                toolStripMenuItem2.Text = btnPlay.Text = "播放";

                toolStripMenuItem2.Enabled = btnPlay.Enabled = false;
                toolStripMenuItem3.Enabled = checkMute.Enabled = false;
                toolStripMenuItem5.Enabled = btnClose.Enabled = false;

                _webWindow.Close();
                _webWindow = null;
            }
            else if (_lwt == WindowType.Window)
            {
                _windowHandle = IntPtr.Zero;
                PInvoke.DS2_RestoreLastWindowPosition();
            }

            _lwt = wt;

            GC.Collect();
            PInvoke.DS2_RefreshDesktop();
        }

        void ForwardMessage()
        {
            IntPtr hWnd = _webWindow.GetChromeWidgetWin1Handle();
            if (hWnd != IntPtr.Zero)
                PInvoke.DS2_StartForwardMouseKeyboardMessage(hWnd);
        }

        #endregion

        #region 控件事件

        const int PLAY_HOTKEY_ID = 10;

        private void MainForm_Load(object sender, EventArgs e)
        {
            _desktopWindowHandle = PInvoke.DS2_GetDesktopWindowHandle();
            if (_desktopWindowHandle == IntPtr.Zero)
            {
                btnOpenFile.Enabled = false;
                checkAutoPlay.Enabled = false;
                notifyIcon1.Visible = false;
                label1.Visible = true;
                return;
            }

            Task.Run(() =>
            {
                _performanceCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
            });

            _screen = Screen.PrimaryScreen;
            _recentFiles = RecentFile.Load();

            if (_settings.AutoPlay && _recentFiles.Count != 0)
                OpenFile(_recentFiles[0]);

            const int MOD_NOREPEAT = 0x4000;
            const int MOD_CONTROL = 0x0002;
            const int MOD_ALT = 0x0001;
            PInvoke.RegisterHotKey(this.Handle, PLAY_HOTKEY_ID, MOD_NOREPEAT | MOD_CONTROL | MOD_ALT, (int)'P');
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (_desktopWindowHandle == IntPtr.Zero)
                return;

            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                Settings.Save();

                if (_settings.FirstRun)
                {
                    notifyIcon1.ShowBalloonTip(1000, "", "DreamScene2 已被最小化到系统托盘。", ToolTipIcon.None);
                    _settings.FirstRun = false;
                }
            }
            else
            {
                notifyIcon1.Visible = false;
                PInvoke.UnregisterHotKey(this.Handle, PLAY_HOTKEY_ID);
                CloseWindow(WindowType.None);
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
                    timer1.Enabled = _settings.CanPause();
                }
            }
            else if (_webWindow != null)
            {
                if (_isWebPlaying)
                {
                    timer1.Enabled = false;
                    PauseWeb();
                }
                else
                {
                    PlayWeb();
                    timer1.Enabled = _settings.CanPause();
                }
            }
        }

        private void checkMute_Click(object sender, EventArgs e)
        {
            _settings.IsMuted = toolStripMenuItem3.Checked = checkMute.Checked;

            if (_videoWindow != null)
                _videoWindow.IsMuted = _settings.IsMuted;
            else if (_webWindow != null)
                _webWindow.IsMuted = _settings.IsMuted;
        }

        private void checkAutoPlay_Click(object sender, EventArgs e)
        {
            _settings.AutoPlay = toolStripMenuItem6.Checked = checkAutoPlay.Checked;
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
                arr[i] = arr[i + 1];
            arr[arr.Length - 1] = val;
        }

        int array_sum(int[] arr)
        {
            int sum = 0;
            for (int i = 0; i < arr.Length; i++)
                sum += arr[i];
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

        int[] _cpuarr = new int[5];
        int[] _parr = new int[5];

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
                    else if (_isWebPlaying)
                        PauseWeb();
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
                array_push(_cpuarr, val > 15.0 ? 1 : 0);

                if (array_is_max(_cpuarr))
                {
                    if (_isPlaying)
                        PauseVideo();
                    else if (_isWebPlaying)
                        PauseWeb();
                    return;
                }
            }
            else
            {
                array_push(_cpuarr, 0);
            }

            if (_settings.AutoPause1)
            {
                bool val = PInvoke.DS2_GetLastInputTickCount() < 500;
                array_push(_parr, val ? 1 : 0);

                if (array_is_max(_parr))
                {
                    if (_isPlaying)
                        PauseVideo();
                    else if (_isWebPlaying)
                        PauseWeb();
                    return;
                }
            }
            else
            {
                array_push(_parr, 0);
            }

            if (!fullScreen && !_isPlaying && array_sum(_cpuarr) == 0 && array_sum(_parr) == 0)
            {
                if (_videoWindow != null)
                    PlayVideo();
                else if (_webWindow != null)
                    PlayWeb();
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

        private async void contextMenuStrip1_Closing(object sender, ToolStripDropDownClosingEventArgs e)
        {
            await Task.Delay(200);
            Settings.Save();
        }

        private void toolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Show();
            this.Activate();
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
                if (i >= 10)
                    break;

                string filePath = _recentFiles[i];
                ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem($"{i + 1}. {filePath}");
                toolStripMenuItem.Tag = filePath;
                toolStripMenuItem.Click += (object sender_, EventArgs e_) =>
                {
                    OpenFile((string)((ToolStripMenuItem)sender_).Tag);
                };
                toolStripMenuItem7.DropDownItems.Add(toolStripMenuItem);
            }

            toolStripMenuItem8.Enabled = _recentFiles.Count != 0;
            toolStripMenuItem7.DropDownItems.Add(toolStripMenuItem8);
        }

        private void toolStripMenuItem8_Click(object sender, EventArgs e)
        {
            RecentFile.Clean();
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
                toolStripMenuItem.Click += (object sender_, EventArgs e_) =>
                {
                    _screenIndex = (int)((ToolStripMenuItem)sender_).Tag;
                    _screen = Screen.AllScreens[_screenIndex];
                    System.Drawing.Rectangle bounds = _screen.Bounds;

                    PInvoke.DS2_RefreshDesktop();

                    if (_videoWindow != null)
                        _videoWindow.SetPosition(bounds);
                    else if (_webWindow != null)
                        _webWindow.SetPosition(bounds);
                    else if (_windowHandle != IntPtr.Zero)
                        PInvoke.DS2_SetWindowPosition(_windowHandle, bounds.ToRECT());
                };
                toolStripMenuItem10.DropDownItems.Add(toolStripMenuItem);
            }
        }

        private void toolStripMenuItem16_DropDownOpening(object sender, EventArgs e)
        {
            toolStripMenuItem16.DropDownItems.Clear();

            if (_hWndSet.Count == 0)
            {
                toolStripMenuItem16.DropDownItems.Add(toolStripMenuItem17);
                return;
            }

            foreach (var val in _hWndSet)
            {
                IntPtr ptr = val;
                bool b = PInvoke.IsWindowVisible(ptr);

                StringBuilder sb = new StringBuilder(128);
                PInvoke.GetWindowText(ptr, sb, sb.Capacity);
                string title = sb.ToString();

                ToolStripMenuItem toolStripMenuItem = new ToolStripMenuItem(title + (b ? "" : " (Invalidate)"));
                toolStripMenuItem.Enabled = b;
                toolStripMenuItem.Checked = _windowHandle == ptr;
                toolStripMenuItem.Tag = val;
                toolStripMenuItem.Click += (object sender_, EventArgs e_) =>
                {
                    SetWindow((IntPtr)((ToolStripMenuItem)sender_).Tag);
                };
                toolStripMenuItem16.DropDownItems.Add(toolStripMenuItem);
            }
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
                _settings.AutoPause1 = toolStripMenuItem23.Checked = !toolStripMenuItem23.Checked;
            else if (sender == toolStripMenuItem24)
                _settings.AutoPause2 = toolStripMenuItem24.Checked = !toolStripMenuItem24.Checked;
            else if (sender == toolStripMenuItem25)
                _settings.AutoPause3 = toolStripMenuItem25.Checked = !toolStripMenuItem25.Checked;

            if (_videoWindow != null)
            {
                if (_settings.CanPause())
                {
                    timer1.Enabled = true;
                }
                else
                {
                    timer1.Enabled = false;
                    if (!_isPlaying)
                        PlayVideo();
                }
            }
            else if (_webWindow != null)
            {
                if (_settings.CanPause())
                {
                    timer1.Enabled = true;
                }
                else
                {
                    timer1.Enabled = false;
                    if (!_isWebPlaying)
                        PlayWeb();
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
            _settings.UseDesktopInteraction = toolStripMenuItem27.Checked = !toolStripMenuItem27.Checked;
            if (_webWindow != null)
            {
                if (_settings.UseDesktopInteraction)
                    ForwardMessage();
                else
                    PInvoke.DS2_EndForwardMouseKeyboardMessage();
            }
        }

        #endregion

        protected override void WndProc(ref Message m)
        {
            const int WM_HOTKEY = 0x0312;
            const int WM_USER = 0x0400;

            if (m.Msg == WM_HOTKEY && (int)m.WParam == PLAY_HOTKEY_ID)
            {
                btnPlay_Click(null, null);
                return;
            }
            else if (m.Msg == (WM_USER + 1001))
            {
                _hWndSet.Add(m.WParam);
                SetWindow(m.WParam);
                return;
            }

            base.WndProc(ref m);
        }
    }
}
