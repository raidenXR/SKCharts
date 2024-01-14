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

public partial class SKChart3DView : UserControl
{
    public SKChart3DView()
    {
        InitializeComponent();
        SKChart3D.Demo(chart_control.Chart);
        this.KeyDown += KeyEnter_Command;
        this.SizeChanged += OnSizeChanged;

        elevation_slider.ValueChanged += ElevationChanged;
        rotation_slider.ValueChanged += RotationChanged;
        slider.ValueChanged += ChangeValues;
    }


    public void ResetSidePanel(string[] variableNames, double[] a, double[] b, double[] dx)
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
            chart_control.Chart.Camera.Elevation = (float)slider.Value;
            chart_control.Chart.Update();
            // if(chart_control.Chart.Camera.Resync)
            // {
            //     Console.WriteLine($"{chart_control.Chart.Camera.Azimuth}, {chart_control.Chart.Camera.Elevation}");
            // }                   
        }
    }

    private void RotationChanged(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if(sender is Slider slider)
        {
            chart_control.Chart.Camera.Azimuth = (float)slider.Value;
            chart_control.Chart.Update();
            // if(chart_control.Chart.Camera.Resync)
            // {
            //     Console.WriteLine($"{chart_control.Chart.Camera.Azimuth}, {chart_control.Chart.Camera.Elevation}");
            // }                   
        }
    }
    
    private void ChangeValues(object? sender, RangeBaseValueChangedEventArgs e)
    {
        if(sender is Slider slider)
        {
            var model = chart_control.Chart.Models.First();
            // var vertices = model.Vertices;
            var values = model.Zvalues;
            var random = new Random();

            // for(int i = 0; i < vertices.Length; i++)
            for(int i = 0; i < values.Length; i++)
            {
                // vertices[i].Z += (0.5f * random.NextSingle() - 1f) + (float)slider.Value;
                values[i] += (0.5 * random.NextSingle() - 1.0) + slider.Value;
            }
                        
            model.UpdateBounds();
            chart_control.Chart.UpdateBounds();
            chart_control.Chart.Update();
        }
    }
    
    private void KeyEnter_Command(object? sender, KeyEventArgs e)
    {
        if(e.Key == Key.Left)  chart_control.Chart.Camera.Elevation -= 5f;        
        if(e.Key == Key.Right) chart_control.Chart.Camera.Elevation += 5f;
        if(e.Key == Key.Up)    chart_control.Chart.Camera.Azimuth -= 5f;
        if(e.Key == Key.Down)  chart_control.Chart.Camera.Azimuth += 5f;

        if(chart_control.Chart.Camera.Resync)
        {
            chart_control.Chart.Update();
            // Console.WriteLine($"{chart_control.Chart.Camera.Azimuth}, {chart_control.Chart.Camera.Elevation}");
        }
    }
}
