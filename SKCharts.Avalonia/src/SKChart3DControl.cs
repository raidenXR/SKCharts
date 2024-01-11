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
using SKCharts;

namespace SKCharts.Avalonia;


public class SKChart3DControl : Control, IDisposable
{
	readonly SKChart3D chart;
    readonly GlyphRun _noSkia;

	bool is_disposed = false;

	public SKChart3D Chart => chart;
	

	public SKChart3DControl()
	{
		chart = new SKChart3D();
        var text = "Current rendering API is not Skia";
        var glyphs = text.Select(ch => Typeface.Default.GlyphTypeface.GetGlyph(ch)).ToArray();
        _noSkia = new GlyphRun(Typeface.Default.GlyphTypeface, 12, text.AsMemory(), glyphs);

	}

	~SKChart3DControl()
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

	public IList<Model3D> Models => chart.Models;
	
	public void AttachModel(Model3D model) 
	{
		Dispatcher.UIThread.Invoke(() => chart.AttachModel(model));
	}

	public void DetachModel(Model3D model)
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

    public class CustomDrawOp : ICustomDrawOperation
    {
        private readonly IImmutableGlyphRunReference? _noSkia;
        private readonly SKChart3DControl _chart_control;
        
        public CustomDrawOp(Rect bounds, GlyphRun noSkia, SKChart3DControl chart_control)
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
				var chart = _chart_control.Chart;
                using var lease = lease_feature.Lease();
                var canvas = lease.SkCanvas;
                canvas.Save();
				chart.Draw(canvas);
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
}

