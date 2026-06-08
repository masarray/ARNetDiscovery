using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using ARNetDiscovery.Wpf.ViewModels;

namespace ARNetDiscovery.Wpf;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        StateChanged += (_, _) => ApplyChromeSafeMargin();
        Loaded += (_, _) => ApplyChromeSafeMargin();
    }

    private void TopChrome_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState != MouseButtonState.Pressed)
            return;

        if (IsInteractiveElement(e.OriginalSource as DependencyObject))
            return;

        if (e.ClickCount == 2)
        {
            ToggleMaximizeRestore();
            return;
        }

        try
        {
            DragMove();
        }
        catch
        {
            // Ignore drag exceptions caused by fast click/release sequences.
        }
    }

    private static bool IsInteractiveElement(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is ButtonBase or ComboBox or TextBox or PasswordBox or ScrollBar or Slider)
                return true;

            source = VisualTreeHelper.GetParent(source);
        }

        return false;
    }

    private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        => WindowState = WindowState.Minimized;

    private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        => ToggleMaximizeRestore();

    private void CloseButton_Click(object sender, RoutedEventArgs e)
        => Close();


    private void DiagnosticsPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsInteractiveElement(e.OriginalSource as DependencyObject))
            return;

        if (DataContext is MainViewModel vm && vm.ToggleDiagnosticsCommand.CanExecute(null))
        {
            vm.ToggleDiagnosticsCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void InspectorPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsInteractiveElement(e.OriginalSource as DependencyObject))
            return;

        if (DataContext is MainViewModel { IsInspectorExpanded: false } vm && vm.ToggleInspectorCommand.CanExecute(null))
        {
            vm.ToggleInspectorCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void InspectorHeader_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (IsInteractiveElement(e.OriginalSource as DependencyObject))
            return;

        if (DataContext is MainViewModel vm && vm.ToggleInspectorCommand.CanExecute(null))
        {
            vm.ToggleInspectorCommand.Execute(null);
            e.Handled = true;
        }
    }

    private void ToggleMaximizeRestore()
    {
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        ApplyChromeSafeMargin();
    }

    private void ApplyChromeSafeMargin()
    {
        if (WindowState == WindowState.Maximized)
        {
            MaxWidth = SystemParameters.WorkArea.Width;
            MaxHeight = SystemParameters.WorkArea.Height;
            ShellRoot.Margin = new Thickness(0);
            return;
        }

        MaxWidth = double.PositiveInfinity;
        MaxHeight = double.PositiveInfinity;
        ShellRoot.Margin = new Thickness(0);
    }
}
