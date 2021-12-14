using System;
using System.Windows;
using Microsoft.Web.WebView2.Core;

namespace DreamScene2
{
    /// <summary>
    /// Interaction logic for WebWindow.xaml
    /// </summary>
    public partial class WebWindow : Window
    {
        public WebWindow()
        {
            InitializeComponent();
            WebView2EnvironmentInit();
        }

        async void WebView2EnvironmentInit()
        {
            var webView2Environment = await CoreWebView2Environment.CreateAsync(null, Helper.GetPath(""));
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
