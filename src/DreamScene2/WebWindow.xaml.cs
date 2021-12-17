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
            WebView2EnvironmentInit();
        }

        async void WebView2EnvironmentInit()
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

        protected override void OnClosed(EventArgs e)
        {
            webView2.Dispose();
            webView2 = null;
            base.OnClosed(e);
        }
    }
}
