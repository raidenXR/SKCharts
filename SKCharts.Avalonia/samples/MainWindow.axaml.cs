using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using SKCharts;
using SKCharts.Avalonia;

namespace Sample;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        
        content_control.Content = new SKCharts.Avalonia.SKChart3DView();
    }

    private void SKChart3DView_Click(object? sender, RoutedEventArgs e)
    {
        content_control.Content = new SKCharts.Avalonia.SKChart3DView();
    }

    private void SKChart2DView_Click(object? sender, RoutedEventArgs e)
    {
        content_control.Content = new SKCharts.Avalonia.SKChart2DView();
    }
    
}
