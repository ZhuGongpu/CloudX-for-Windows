using System;

namespace CloudX.SubWindows
{
    /// <summary>
    ///     AboutUsWindow.xaml 的交互逻辑
    /// </summary>
    public partial class AboutUsWindow
    {
        public AboutUsWindow()
        {
            InitializeComponent();
        }

        private void AboutUsWindow_OnClosed(object sender, EventArgs e)
        {
            VideoBrowser.Dispose();
        }
    }
}