using System;
using System.Linq;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Avalonia.Skia;
using Avalonia.Interactivity;
using Avalonia.Rendering.SceneGraph;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using SkiaSharp;
using System.Windows.Input;
using IO = System.IO;
using static System.Diagnostics.Debug;
using SKCharts;

namespace SKCharts.Avalonia;

public class SKChart2DControl : Control, IDisposable
{
	SKChart2D chart;
	readonly GlyphRun _noSkia;

	bool is_disposed;

	public SKChart2D Chart 
	{
		get => chart;
		set {chart = value;}
	}
	
	// public SKChart2D Chart
	// {
	// 	get {return GetValue(ChartProperty);}
	// 	set {SetValue(ChartProperty, value);}
	// }
	
	public SKChart2DControl()
	{
		chart = new SKChart2D();
		// chart.Models.Clear();

        var text = "Current rendering API is not Skia";
        var glyphs = text.Select(ch => Typeface.Default.GlyphTypeface.GetGlyph(ch)).ToArray();
        _noSkia = new GlyphRun(Typeface.Default.GlyphTypeface, 12, text.AsMemory(), glyphs);
	}

	~SKChart2DControl()
	{
		Dispose();
	}

	public void Dispose()
	{
		if(!is_disposed)
		{
			chart.Dispose();
		}
		is_disposed = true;
	}

	public IList<Model2D> Models => chart.Models;

	public void AttachModel(Model2D model) 
	{
		Dispatcher.UIThread.Invoke(() => chart.AttachModel(model));
	}

	// public SKImage SetXImg(SkiaSharp.SKImage img)
	// {
	// 	Dispatcher.UIThread.Invoke(() => chart.XImg = img)
	// }

	public void DetachModel(Model2D model)
	{
		Dispatcher.UIThread.Invoke(() => chart.DetachModel(model));		
	}

	public void DetachModelAt(int index)
	{
		Dispatcher.UIThread.Invoke(() => chart.DetachModelAt(index));
	}

	
	public void NormalizeModels() 
	{
		chart.NormalizeModels();
	}

	public void Update()
	{
		Dispatcher.UIThread.Invoke(() => chart.Update());
	}

	public static readonly StyledProperty<SKChart2D> ChartProperty =
		AvaloniaProperty.Register<SKChart2DControl, SKChart2D>(nameof(Chart));
	
	public class CustomDrawOp : ICustomDrawOperation
    {
        private readonly IImmutableGlyphRunReference? _noSkia;
        private readonly SKChart2DControl _chart_control;
        
        public CustomDrawOp(Rect bounds, GlyphRun noSkia, SKChart2DControl chart_control)
        {
            _noSkia = noSkia.TryCreateImmutableGlyphRunReference();
            _chart_control = chart_control;
            Bounds = bounds;
        }
        
    
        public void Dispose()
        {
            
        }   

        public Rect Bounds {get;}

        public bool HitTest(Point p) 
        {
            return false;
        }     

        public bool Equals(ICustomDrawOperation? other)
        {
            return false;
        }

        public void Render(ImmediateDrawingContext context)
        {
            var lease_feature = context.TryGetFeature<ISkiaSharpApiLeaseFeature>();
            if(lease_feature == null)
            {
                context.DrawGlyphRun(Brushes.Black, _noSkia!);
            }
            else 
            {
                using var lease = lease_feature.Lease();
                var canvas = lease.SkCanvas;
                canvas.Save();
                {
					var _chart = _chart_control.Chart;
					_chart.DrawGridLines(canvas);
                    _chart.DrawAxes(canvas);
                    _chart.DrawLabels(canvas);						
                    _chart.DrawModels(canvas);
					_chart.DrawTicks(canvas);
					_chart.DrawLegend(canvas);
                }
                canvas.Restore();
            }
        }
    }

    public override void Render(DrawingContext context)
    {
        context.Custom(new CustomDrawOp(new Rect(0, 0, Bounds.Width, Bounds.Height), _noSkia, this));
        Dispatcher.UIThread.InvokeAsync(InvalidateVisual, DispatcherPriority.Background);
    }
    // public Rect Bounds 
    // {
    //     get => base.Bounds;
    // }
    
    public bool HitTest(Point p)
    {
        return false;
    }


	static void NormalizeVertices(Span<Vector2> vertices, ReadOnlySpan<Vector2> original, Bounds2D bounds)
	{
		for(int i = 0; i < original.Length; i++)
		{
			var vec = original[i];
			var x = (vec.X - bounds.Xmin) / (bounds.Xmax - bounds.Xmin);
			var y = (vec.Y - bounds.Ymin) / (bounds.Ymax - bounds.Ymin);
			vertices[i] = new Vector2((float)x, (float)y);
		}
	}

}
