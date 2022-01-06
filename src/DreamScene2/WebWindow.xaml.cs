using Microsoft.Web.WebView2.Core;
using System;
using System.Windows;

namespace DreamScene2
{
    public partial class WebWindow : Window
    {
        WebWindowOptions _webWindowOptions;

        public WebWindow(WebWindowOptions webWindowOptions)
        {
            _webWindowOptions = webWindowOptions;
            InitializeComponent();
            InitializeCoreWebView2Environment();
        }

        async void InitializeCoreWebView2Environment()
        {
            string args = null;
            if (_webWindowOptions.DisableWebSecurity)
            {
                args = "--disable-web-security";
            }

            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, _webWindowOptions.UserDataFolder, new CoreWebView2EnvironmentOptions(args));
            await webView2.EnsureCoreWebView2Async(webView2Environment);
        }

        public Uri Source
        {
            get => webView2.Source;
            set => webView2.Source = value;
        }

        public IntPtr GetChromeWidgetWin1Handle()
        {
            IntPtr chrome_WidgetWin_0 = PInvoke.FindWindowEx(webView2.Handle, IntPtr.Zero, "Chrome_WidgetWin_0", null);
            if (chrome_WidgetWin_0 == IntPtr.Zero)
            {
                return IntPtr.Zero;
            }
            return PInvoke.FindWindowEx(chrome_WidgetWin_0, IntPtr.Zero, "Chrome_WidgetWin_1", null);
        }

        public uint GetD3DRenderingSubProcessPid()
        {
            IntPtr chrome_WidgetWin_1 = GetChromeWidgetWin1Handle();
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
            base.OnClosed(e);
        }
    }
}
