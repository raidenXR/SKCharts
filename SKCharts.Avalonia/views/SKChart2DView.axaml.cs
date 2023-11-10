using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.Markup.Xaml;
using System.Numerics;
using System;
using System.Linq;
using static System.Diagnostics.Debug;
using SKCharts;

namespace SKCharts.Avalonia;

public partial class SKChart2DView : UserControl
{
    public SKChart2DView()
    {
        InitializeComponent();
        this.SizeChanged += OnSizeChanged;
        
        elevation_slider.ValueChanged += ElevationChanged;
        azimuth_slider.ValueChanged += AzimuthChanged;
    }


    public void ResetSidePanel(string [] variableNames, double[] a, double[] b, double[] dx)
    {
        Assert(a.Length == variableNames.Length);
        Assert(a.Length == b.Length);
        Assert(a.Length == dx.Length);
        
        for(int i = 0; i < a.Length; i++)
        {
            
        }
    }

    public void ResetSidePanelDispatch(string[] variableNames, double[] a, double[] b, double[] dx)
    {
        Dispatcher.UIThread.Invoke(() => ResetSidePanel(variableNames, a, b, dx), DispatcherPriority.Background);
    }
    
    private void OnSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        var chart = chart_control.Chart;
        chart.Width = chart_control.Width;
        chart.Height = chart_control.Height;
        chart_control.Chart.Update();
    }
    
    private void ElevationChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if(sender is Slider slider) 
        {
            // chart_control.Chart.Camera.Elevation = (float)slider.Value;
            chart_control.Chart.Update();
            // if(chart_control.Chart.Camera.Resync)
            // {
            //     Console.WriteLine($"{chart_control.Chart.Camera.Azimuth}, {chart_control.Chart.Camera.Elevation}");
            // }                   
        }
    }

    private void AzimuthChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if(sender is Slider slider)
        {
            var random = new Random();
            var model = chart_control.Chart.Models.First();
            var values = model.Vertices;
            values[0].Y = 0.8f * (float)slider.Value;
            for(int i = 1; i < values.Length - 1; i += 2)
            {
                var y = values[i].Y + 30 * random.NextSingle() * 0.1f * (float)slider.Value;
                values[i + 0].Y = y;
                values[i + 1].Y = y;
            }
            model.UpdateBounds();
            chart_control.Chart.UpdateBounds();
            chart_control.Chart.Update();
        }
    }
}
