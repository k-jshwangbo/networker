using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace Networker.Views
{
    /// <summary>
    /// MainWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class MainWindow : FluentWindow
    {
        public MainWindow()
        {
            InitializeComponent();

            // Apply the current Windows theme (light/dark) and the window
            // backdrop, and keep following the system if the user switches it.
            // SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica);

        }
    }
}
