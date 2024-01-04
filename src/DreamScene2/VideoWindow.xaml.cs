using System;
using System.Drawing;
using System.Windows;
using System.Windows.Interop;

namespace DreamScene2
{
    public partial class VideoWindow : Window, IPlayer, IPlayerControl
    {
        public VideoWindow()
        {
            InitializeComponent();
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
            get => mediaElement.Source;
            set => mediaElement.Source = value;
        }

        public double Volume
        {
            get => mediaElement.Volume;
            set => mediaElement.Volume = value;
        }

        public bool IsMuted
        {
            get => mediaElement.IsMuted;
            set => mediaElement.IsMuted = value;
        }

        public void Play()
        {
            mediaElement.Play();
            IsPlaying = true;
        }

        public void Pause()
        {
            mediaElement.Pause();
            IsPlaying = false;
        }

        private void mediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            mediaElement.Position = TimeSpan.Zero;
            mediaElement.Play();
        }

        protected override void OnClosed(EventArgs e)
        {
            mediaElement.Close();
            mediaElement = null;
        }
    }
}
