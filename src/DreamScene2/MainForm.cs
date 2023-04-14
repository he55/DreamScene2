using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace DreamScene2
{
    public partial class MainForm : Form
    {
        public string PlayPath { get; set; }

        IPlayer _player;
        IntPtr _desktopWindowHandle;
        List<string> _recentFiles;
        PerformanceCounter _performanceCounter;
        Settings _settings = Settings.Load();
        Screen _screen;
        int _screenIndex;
        HashSet<IntPtr> _hWndSet = new HashSet<IntPtr>();
        Timer _timer1 = new Timer();
        Timer _timer2 = new Timer();

        public MainForm()
        {
            InitializeComponent();
            _timer1.Interval = 2000;
            _timer1.Tick += timer1_Tick;
            _timer2.Interval = 200;
            _timer2.Tick += timer2_Tick;
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

        void Play_()
        {
            ((IPlayerControl)_player).Play();
            toolStripMenuItem2.Text = btnPlay.Text = "暂停";
        }

        void Pause_()
        {
            ((IPlayerControl)_player).Pause();
            toolStripMenuItem2.Text = btnPlay.Text = "播放";
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

                if (path.EndsWith(".html", StringComparison.OrdinalIgnoreCase))
                    OpenWeb(uri.AbsoluteUri);
                else
                    OpenVideo(path);
            }
        }

        void EnableControl()
        {
            toolStripMenuItem5.Enabled = btnClose.Enabled = true;

            if (_player is IPlayerControl)
            {
                toolStripMenuItem2.Enabled = btnPlay.Enabled = true;
                toolStripMenuItem3.Enabled = checkMute.Enabled = true;

                toolStripMenuItem2.Text = btnPlay.Text = "暂停";
                _timer1.Enabled = _settings.CanPause();
            }
        }

        void OpenVideo(string path)
        {
            CloseWindow(WindowType.Video);

            if (_player == null)
            {
                VideoWindow videoWindow = new VideoWindow();
                _player = videoWindow;
                videoWindow.IsMuted = _settings.IsMuted;
                videoWindow.SetPosition(_screen.Bounds);
                videoWindow.Show();

                PInvoke.SetParent(videoWindow.GetHandle(), _desktopWindowHandle);
            }

            ((IPlayerControl)_player).Source = new Uri(path, UriKind.Absolute);
            ((IPlayerControl)_player).Play();

            EnableControl();
        }

        void OpenWeb(string url)
        {
            if (!WebWindow.TryGetWebView2Version(out _))
            {
                MessageBox.Show("打开网页功能需要 WebView2 支持。请在托盘图标找到 DreamScene2 然后右键菜单，依次点击 [打开 URL] > [安装 WebView2...] 安装。", Constant.ProjectName);
                return;
            }

            CloseWindow(WindowType.Web);

            if (_player == null)
            {
                WebWindowOptions webWindowOptions = new WebWindowOptions();
                webWindowOptions.UserDataFolder = Helper.GetPathForUserAppDataFolder("");
                webWindowOptions.DisableWebSecurity = _settings.DisableWebSecurity;
                webWindowOptions.IsMuted = _settings.IsMuted;

                WebWindow webWindow = new WebWindow(webWindowOptions);
                _player = webWindow;
                webWindow.SetPosition(_screen.Bounds);
                webWindow.Show();

                PInvoke.SetParent(webWindow.GetHandle(), _desktopWindowHandle);
            }

            ((IPlayerControl)_player).Source = new Uri(url);

            EnableControl();

            if (((IPlayerControl)_player).Source.Host.EndsWith("bilibili.com"))
            {
                toolStripMenuItem27.Enabled = false;
            }
            else if (_player is IPlayerInteractive && _settings.UseDesktopInteraction)
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

            WindowPlayer windowPlayer = new WindowPlayer(hWnd);
            _player = windowPlayer;
            windowPlayer.SetPosition(_screen.Bounds);
            PInvoke.SetParent(windowPlayer.GetHandle(), _desktopWindowHandle);

            EnableControl();
        }

        WindowType _lwt;

        void CloseWindow(WindowType wt)
        {
            if (_player == null)
            {
                _lwt = wt;
                return;
            }

            _timer1.Enabled = false;
            toolStripMenuItem2.Text = btnPlay.Text = "播放";

            toolStripMenuItem2.Enabled = btnPlay.Enabled = false;
            toolStripMenuItem3.Enabled = checkMute.Enabled = false;
            toolStripMenuItem5.Enabled = btnClose.Enabled = false;

            toolStripMenuItem27.Enabled = true;

            if (_player is IPlayerInteractive && _settings.UseDesktopInteraction)
                PInvoke.DS2_EndForwardMouseKeyboardMessage();

            if (_lwt == WindowType.Web && !((IPlayerControl)_player).IsPlaying)
                Play_();

            if (_lwt != wt || wt == WindowType.Window)
            {
                _player.Shutdown();
                _player = null;
            }

            _lwt = wt;

            GC.Collect();
            PInvoke.DS2_RefreshDesktop();
        }

        void ForwardMessage()
        {
            IntPtr hWnd = ((IPlayerInteractive)_player).GetMessageHandle();
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

            object sender_ = null;
            switch (_settings.PlayMode)
            {
                case 0:
                    sender_ = toolStripMenuItem29;
                    break;
                case 1:
                    sender_ = toolStripMenuItem30;
                    break;
                case 2:
                    sender_ = toolStripMenuItem31;
                    break;
                case 3:
                    sender_ = toolStripMenuItem32;
                    break;
            }
            toolStripMenuItem29_Click(sender_, null);

            if (!string.IsNullOrEmpty(PlayPath))
            {
                OpenFile(PlayPath);
            }
            else if (_settings.AutoPlay && _recentFiles.Count != 0)
            {
                OpenFile(_recentFiles[0]);
                _timer2.Enabled = _settings.PlayMode != 0 && _recentFiles.Count > 1;
            }

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

            Settings.Save();
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            StringCollection files = ((DataObject)e.Data).GetFileDropList();
            if (files.Count > 0)
                e.Effect = DragDropEffects.All;
        }

        bool _drop;
        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            _drop = true;
            StringCollection files = ((DataObject)e.Data).GetFileDropList();
            OpenFile(files[0]);
        }

        private void btnOpenFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "All Files|*.mp4;*.mov;*.html|Video Files|*.mp4;*.mov|HTML Files|*.html";
            if (openFileDialog.ShowDialog() == DialogResult.OK)
                OpenFile(openFileDialog.FileName);
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (((IPlayerControl)_player).IsPlaying)
            {
                _timer1.Enabled = false;
                _timer2.Enabled = false;
                Pause_();
            }
            else
            {
                Play_();
                _timer1.Enabled = _settings.CanPause();
                _timer2.Enabled = _settings.PlayMode != 0 && _recentFiles.Count > 1;
            }
        }

        private void checkMute_Click(object sender, EventArgs e)
        {
            _settings.IsMuted = toolStripMenuItem3.Checked = checkMute.Checked;
            ((IPlayerControl)_player).IsMuted = _settings.IsMuted;
        }

        private void checkAutoPlay_Click(object sender, EventArgs e)
        {
            _settings.AutoPlay = toolStripMenuItem6.Checked = checkAutoPlay.Checked;
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            _timer2.Enabled = false;
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
                    if (((IPlayerControl)_player).IsPlaying)
                        Pause_();
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
                    if (((IPlayerControl)_player).IsPlaying)
                        Pause_();
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
                    if (((IPlayerControl)_player).IsPlaying)
                        Pause_();
                    return;
                }
            }
            else
            {
                array_push(_parr, 0);
            }

            if (!fullScreen &&
                !((IPlayerControl)_player).IsPlaying &&
                array_sum(_cpuarr) == 0 &&
                array_sum(_parr) == 0)
            {
                Play_();
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
            _timer2.Enabled = false;
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

                    if (_player != null)
                        _player.SetPosition(bounds);
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
                toolStripMenuItem.Checked = _player?.GetHandle() == ptr;
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

            if (WebWindow.TryGetWebView2Version(out string version))
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

            if (_player is IPlayerControl)
            {
                if (_settings.CanPause())
                {
                    _timer1.Enabled = true;
                }
                else
                {
                    _timer1.Enabled = false;
                    if (!((IPlayerControl)_player).IsPlaying)
                        Play_();
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
            if (_player is IPlayerInteractive)
            {
                if (_settings.UseDesktopInteraction)
                    ForwardMessage();
                else
                    PInvoke.DS2_EndForwardMouseKeyboardMessage();
            }
        }

        #endregion

        bool _flag;
        protected override void WndProc(ref Message m)
        {
            const int WM_DISPLAYCHANGE = 0x007E;
            const int WM_HOTKEY = 0x0312;
            const int WM_USER = 0x0400;

            if (m.Msg == WM_DISPLAYCHANGE)
            {
                if (_drop)
                {
                    _drop = false;
                    return;
                }

                if (_flag && _player != null)
                {
                    CloseWindow(WindowType.None);
                    OpenFile(_recentFiles[0]);
                }
                _flag = true;
            }
            else if (m.Msg == WM_HOTKEY && (int)m.WParam == PLAY_HOTKEY_ID)
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

        private void toolStripMenuItem29_Click(object sender, EventArgs e)
        {
            toolStripMenuItem29.Checked = toolStripMenuItem30.Checked = toolStripMenuItem31.Checked = toolStripMenuItem32.Checked = false;
            ((ToolStripMenuItem)sender).Checked = true;

            if (sender == toolStripMenuItem29)
            {
                _timer2.Enabled = false;
                _settings.PlayMode = 0;
                return;
            }

            if (sender == toolStripMenuItem30)
            {
                _timer2.Interval = 1 * 60000;
                _settings.PlayMode = 1;
            }
            else if (sender == toolStripMenuItem31)
            {
                _timer2.Interval = 3 * 60000;
                _settings.PlayMode = 2;
            }
            else if (sender == toolStripMenuItem32)
            {
                _timer2.Interval = 5 * 60000;
                _settings.PlayMode = 3;
            }

            if (_player is IPlayerControl)
                _timer2.Enabled = ((IPlayerControl)_player).IsPlaying && _recentFiles.Count > 1;
            else
                _timer2.Enabled = false;
        }

        int _idx = 1;
        private void timer2_Tick(object sender, EventArgs e)
        {
            if (_idx == _recentFiles.Count)
                _idx = 1;

            OpenFile(_recentFiles[_idx++]);
        }
    }
}
