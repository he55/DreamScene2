using Microsoft.Web.WebView2.Core;
using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Interop;

namespace DreamScene2
{
    public partial class WebWindow : Window, IPlayer, IPlayerControl, IPlayerInteractive
    {
        WebWindowOptions _webWindowOptions;
        uint _d3dRenderingSubProcessPid;

        public WebWindow(WebWindowOptions webWindowOptions)
        {
            _webWindowOptions = webWindowOptions;
            InitializeComponent();
            InitializeCoreWebView2Environment();
        }

        async void InitializeCoreWebView2Environment()
        {
            string args = "--autoplay-policy=no-user-gesture-required ";
            if (_webWindowOptions.DisableWebSecurity)
                args += "--disable-web-security";

            CoreWebView2Environment coreWebView2Environment = await CoreWebView2Environment.CreateAsync(null, _webWindowOptions.UserDataFolder, new CoreWebView2EnvironmentOptions(args));
            await webView2.EnsureCoreWebView2Async(coreWebView2Environment);
            webView2.CoreWebView2.IsMuted = _webWindowOptions.IsMuted;

            IsPlaying = true;

            string script = File.ReadAllText(Helper.GetPathForStartupFolder("script.js"));
            await webView2.CoreWebView2.AddScriptToExecuteOnDocumentCreatedAsync(script);
        }

        public IntPtr GetHandle()
        {
            return new WindowInteropHelper(this).Handle;
        }

        public void SetPosition(Rectangle rect)
        {
            this.WindowStyle = WindowStyle.None;
            this.ResizeMode = ResizeMode.NoResize;
            this.Left = rect.Left;
            this.Top = rect.Top;
            this.Width = rect.Width;
            this.Height = rect.Height;
        }

        public void Shutdown()
        {
            this.Close();
        }

        public bool IsPlaying { get; private set; }

        public Uri Source
        {
            get => webView2.Source;
            set => webView2.Source = value;
        }

        public bool IsMuted
        {
            get => webView2.CoreWebView2.IsMuted;
            set => webView2.CoreWebView2.IsMuted = value;
        }

        public double Volume { get; set; }

        public void Play()
        {
            PInvoke.DS2_ToggleProcess(_d3dRenderingSubProcessPid, 1);
            IsPlaying = true;
        }

        public void Pause()
        {
            _d3dRenderingSubProcessPid = GetD3DRenderingSubProcessPid();
            PInvoke.DS2_ToggleProcess(_d3dRenderingSubProcessPid, 0);
            IsPlaying = false;
        }

        public IntPtr GetMessageHandle()
        {
            IntPtr chrome_WidgetWin_0 = PInvoke.FindWindowEx(webView2.Handle, IntPtr.Zero, "Chrome_WidgetWin_0", null);
            if (chrome_WidgetWin_0 == IntPtr.Zero)
                return IntPtr.Zero;
            return PInvoke.FindWindowEx(chrome_WidgetWin_0, IntPtr.Zero, "Chrome_WidgetWin_1", null);
        }

        public uint GetD3DRenderingSubProcessPid()
        {
            IntPtr chrome_WidgetWin_1 = GetMessageHandle();
            if (chrome_WidgetWin_1 != IntPtr.Zero)
            {
                IntPtr d3dWindowHandle = PInvoke.FindWindowEx(chrome_WidgetWin_1, IntPtr.Zero, "Intermediate D3D Window", null);
                PInvoke.GetWindowThreadProcessId(d3dWindowHandle, out uint pid);
                return pid;
            }
            return 0;
        }

        protected override void OnClosed(EventArgs e)
        {
            webView2.Dispose();
            webView2 = null;
        }

        public static bool TryGetWebView2Version(out string version)
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
    }
}
