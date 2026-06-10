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
}
