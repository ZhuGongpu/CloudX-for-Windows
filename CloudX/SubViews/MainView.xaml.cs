using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace CloudX.SubViews
{
    /// <summary>
    ///     MainView.xaml 的交互逻辑
    /// </summary>
    public partial class MainView : UserControl
    {
        public MainView()
        {
            InitializeComponent();
        }

        private void FlipView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var flipview = ((FlipView) sender);
            switch (flipview.SelectedIndex)
            {
                case 0:
                    flipview.BannerText = "CloudX 是一款完全跨平台的多设备互联方案，我们为你连通一切";
                    break;
                case 1:
                    flipview.BannerText = "CloudX为您的PC拓展了全新的操作方式，您的多台移动设备可以共享同一个云";
                    break;
                case 2:
                    flipview.BannerText = "专业的开发团队全面考虑您的操作体验";
                    break;
            }
        }
    }
}