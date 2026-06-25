using System.Windows;
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

            // Start in light theme with a Mica backdrop.
            ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.Mica);
            UpdateThemeButtonIcon();
        }

        private void OnToggleTheme(object sender, RoutedEventArgs e)
        {
            var next = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark
                ? ApplicationTheme.Light
                : ApplicationTheme.Dark;

            ApplicationThemeManager.Apply(next, WindowBackdropType.Mica);
            UpdateThemeButtonIcon();
        }

        private void UpdateThemeButtonIcon()
        {
            bool isDark = ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;

            // Show the icon for the theme you'd switch TO.
            ThemeToggleButton.Icon = new SymbolIcon
            {
                Symbol = isDark ? SymbolRegular.WeatherSunny24 : SymbolRegular.WeatherMoon24
            };
        }
    }
}
