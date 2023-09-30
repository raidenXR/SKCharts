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


namespace SKCharts;

public enum Chart2DType
{
    Arrows,
    Line2D,
    Points,
    // Surface,        
}  

public readonly struct Bounds2D
{
	public readonly double Xmin;
	public readonly double Xmax;
	public readonly double Ymax;
	public readonly double Ymin;

	public Bounds2D(double xmin, double xmax, double ymin, double ymax)
	{
		Xmin = xmin;
		Xmax = xmax;
		Ymin = ymin;
		Ymax = ymax;
	}

	public static Bounds2D NoneDefined => new Bounds2D(double.MaxValue, double.MinValue, double.MaxValue, double.MinValue); 

	public static Bounds2D GetBounds(Vector2[] vertices)
	{
		var xmin = float.MaxValue;
		var ymin = float.MaxValue;

		var xmax = float.MinValue;
		var ymax = float.MinValue;

		foreach(var vec in vertices)
		{
			xmin = MathF.Min(vec.X, xmin);
			ymin = MathF.Min(vec.Y, ymin);

			xmax = MathF.Max(vec.X, xmax);
			ymax = MathF.Max(vec.Y, ymax);
		}

		return new Bounds2D(xmin, xmax, ymin, ymax);
	}

	public static Bounds2D GetBounds(Bounds2D a, Bounds2D b)
	{
		var xmin = Math.Min(a.Xmin, b.Xmin);
		var ymin = Math.Min(a.Ymin, b.Ymin);

		var xmax = Math.Max(a.Xmax, b.Xmax);
		var ymax = Math.Max(a.Ymax, b.Ymax);
		
		return new Bounds2D(xmin, xmax, ymin, ymax);
	}


	public readonly double TX(double norm)
	{
		return norm * (Xmax - Xmin) + Xmin;
	}

	public readonly double TY(double norm)
	{
		return norm * (Ymax - Ymin) + Ymin;
	}

    public override string ToString()
    {
        return $"({Xmin}, {Xmax}, {Ymin}, {Ymax})";
    }
}

public class Model2D
{
	Vector2[] vertices;
	Vector2[] vertices_norm;
	ushort[] indices;
	SKPoint[] points;
	SKColor[] colors;
	Bounds2D bounds;
	SKColor single_color;
	Chart2DType chart_type;
	
	SKChart2D Parent {get; set;}


	public static Model2D CreateLine(Vector2[] line, SKPaint paint)
	{
		var vertices = new Vector2[2 * (line.Length - 2)];
		vertices[0] = line[0];
		for(int i = 1, j = 1; i < vertices.Length - 1; i += 2, j += 1)
		{
			vertices[i + 0] = line[j];
			vertices[i + 1] = line[j];
		}
		vertices[vertices.Length - 1] = line[line.Length - 1];
	
		return new Model2D
		{
			vertices = vertices,
			vertices_norm = new Vector2[vertices.Length],
			points = new SKPoint[vertices.Length],
			colors = null,
			indices = null,
			bounds = Bounds2D.GetBounds(line),
			chart_type = Chart2DType.Line2D,
			single_color = SKColors.Black,
			Paint = paint,
		};
	}
	
	public static Model2D CreatePoints(Vector2[] line, SKPaint paint)
	{
		return new Model2D
		{
			vertices = line,
			vertices_norm = new Vector2[line.Length],
			points = new SKPoint[line.Length],
			colors = null,
			indices = null,
			bounds = Bounds2D.GetBounds(line),
			chart_type = Chart2DType.Points,
			single_color = SKColors.Black,
			Paint = paint,
		};
	}

	public Vector2[] Vertices => vertices;

	public Vector2[] VerticesNorm => vertices_norm;

	public ushort[] Indices => indices;

	public SKPoint[] Points => points;

	public SKColor[] Colors => colors;

	public Bounds2D Bounds => bounds;

	public SKColor SingleColor => single_color;

	public Chart2DType ChartType => chart_type;

	public SKPaint? Paint {get; set;}

	public string? Name {get; set;}

	public int W {get; set;}
	public int H {get; set;}

	
	public void Normalize(Bounds2D bounds)
	{
		for(int i = 0; i < vertices_norm.Length; i++)
		{
			var vec = vertices[i];
			var x = (vec.X - bounds.Xmin) / (bounds.Xmax - bounds.Xmin);
			var y = (vec.Y - bounds.Ymin) / (bounds.Ymax - bounds.Ymin);
			vertices_norm[i] = new Vector2((float)x, (float)y);
		}
	}

	public void UpdateBounds()
	{
		bounds = Bounds2D.GetBounds(vertices);
		Normalize(bounds);
	}
}


public class SKChart2D : Control, IDisposable
{
	Bounds2D bounds;
	readonly GlyphRun _noSkia;
	
	SKPoint[] gridlines_points = new SKPoint[20];
	SKPoint[] axis_points = new SKPoint[4];
	Vector2[] axis = new Vector2[]
	{
		new Vector2(0, 0),			
		new Vector2(1, 0),			
		new Vector2(0, 0),			
		new Vector2(0, 1),			
	};

	Vector2[] ticks = new Vector2[100];
	SKPoint[] tick_points = new SKPoint[100];


	double[] label_vecs   = new double[100];
	SKPoint[] label_points = new SKPoint[100];
	Memory<SKPoint> label_points_slice;
	

	bool is_disposed = false;
	List<Model2D> models = new();

	public IReadOnlyCollection<Model2D> Models => models;

	public Vector2 LegendPosition {get; set;}

	// SKPaint fill;
	// ColorBar colorbar;

	SKPaint black_paint = new SKPaint
	{
		Color = SKColors.Black,
		StrokeWidth = 1.5f,
		IsAntialias = true,
		TextSize = 16f,
	};
	
	SKPaint silver_paint = new SKPaint
	{
		Color = SKColors.Silver,
		StrokeWidth = 1.0f,
		IsAntialias = true,
		TextSize = 16f,
	};

	
	public SKChart2D()
	{
        var text = "Current rendering API is not Skia";
        var glyphs = text.Select(ch => Typeface.Default.GlyphTypeface.GetGlyph(ch)).ToArray();
        _noSkia = new GlyphRun(Typeface.Default.GlyphTypeface, 12, text.AsMemory(), glyphs);
		
		var rand = new Random();
		// var vertices = new Vector2[100 - (100 % 3)];
		var vertices = new Vector2[100];
		for(int i = 0; i < vertices.Length; i += 1)
		{
			// vertices[i + 0] = vertices[i - 1];
			var vec = new Vector2(i, 90f * rand.NextSingle());
			vertices[i + 0] = vec;
			// vertices[i + 1] = vec;
		}
		var _paint0 = new SKPaint(){Color = SKColors.Green, StrokeWidth = 2, IsAntialias = true, TextSize = 18f};
		var _model0 = Model2D.CreateLine(vertices, _paint0);

		var vertices2 = new Vector2[100];
		for(int i = 0; i < vertices2.Length - 0; i += 1)
		{
			vertices2[i + 0] = new Vector2(i,  90 * MathF.Sin(i));
			// vertices[i + 1] = vertices[i + 0];
		}
		var _paint1 = new SKPaint(){Color = SKColors.Red, StrokeWidth = 4, IsAntialias = true, TextSize = 18f};
		var _model1 = Model2D.CreatePoints(vertices2, _paint1);
		_model1.Name = "sin line";

		AttachModel(_model0);
		AttachModel(_model1);
		Update();
	}

	~SKChart2D()
	{
		Dispose();
	}

	public void Dispose()
	{
		if(!is_disposed)
		{
			black_paint.Dispose();
			foreach(var model in models) model.Paint?.Dispose();
			is_disposed = true;
		}
	}

	public void AttachModel(Model2D model)
	{
		if(models.Count == 0)
		{
			bounds = model.Bounds;
		}
		else
		{
			bounds = Bounds2D.GetBounds(bounds, model.Bounds);
		}

		models.Add(model);
		foreach(var _model in models) _model.Normalize(bounds);
	}

	public void UpdateBounds()
	{
		bounds = Bounds2D.NoneDefined;
		foreach(var model in models)
		{
			bounds = Bounds2D.GetBounds(bounds, model.Bounds);
		}
	}
	
    public void Update()
    {
        UpdateGridLines();
        UpdateAxes();
        UpdateTicks();
		UpdateModels();
        UpdateLabels();

		var transform = Matrix3x2.CreateTranslation(0.6f, 0.6f) * Matrix3x2.CreateScale(0.5f, 0.5f);
		var pos = Vector2.Transform(new Vector2(1f, 0.8f), transform);
		
		LegendPosition = new Vector2
		{
			X = (float)(Width * pos.X),
			Y = (float)(Height * (1 - pos.Y)),
		};

        // Camera.Resync = false;        
		// foreach(var model in models) Console.WriteLine(model.Bounds.ToString());
		// Console.WriteLine(bounds.ToString());
    }


    private void UpdateModels()
    {
		foreach(var model in models)
		{
	        switch (model.ChartType)
	        {
	            case Chart2DType.Line2D:
				case Chart2DType.Points:
	                UpdateLine(model);
					break;


	            default: 
					throw new ArgumentException();
	        }			
		}		
    }

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private Matrix3x2 CreateTransform()
	{
		var translate = Matrix3x2.CreateTranslation(0.3f, 0.4f);
		var scale     = Matrix3x2.CreateScale(0.6f, 0.6f);
		var transform = translate * scale;
		return transform;
	}

    private void UpdateLine(Model2D model)
    {
        var vertices  = model.VerticesNorm;
        var points    = model.Points;
		var transform = CreateTransform();
		
		var width  = (float)Width;
		var height = (float)Height;
		
        for (int i = 0; i < vertices.Length; i++)
        {
            var vec = Vector2.Transform(vertices[i], transform);			
            points[i] = new SKPoint(width * vec.X, height * (1 - vec.Y));
        }
    }


	SKPoint Cast2D(Vector2 vec)
	{
		return new SKPoint
		{
			X = vec.X * (float)Width,
			Y = (1 - vec.Y) * (float)Height,
		};
	}
	
	private void UpdateGridLines()
	{
		var transform = CreateTransform();
	    int i = 0;
		
		for(float dv = 0.2f; i < 20; dv += 0.2f, i += 4) 
		{
			var vec0 = Vector2.Transform(new Vector2(dv, 0), transform);
			var vec1 = Vector2.Transform(new Vector2(dv, 1), transform);
			
			var vec2 = Vector2.Transform(new Vector2(0, dv), transform);
			var vec3 = Vector2.Transform(new Vector2(1, dv), transform);

			gridlines_points[i + 0] = Cast2D(vec0);
			gridlines_points[i + 1] = Cast2D(vec1);
			gridlines_points[i + 2] = Cast2D(vec2);
			gridlines_points[i + 3] = Cast2D(vec3);
		}
	}

    private void UpdateAxes()
    {
		var transform = CreateTransform();

		for(int i = 0; i < axis.Length; i++)
		{
			var vec = Vector2.Transform(axis[i], transform);
			axis_points[i] = Cast2D(vec);
		}
		// foreach(var pt in axis_points) Console.Write($"{pt.X}, {pt.Y}   ");
		// Console.WriteLine();		

    }

	private void UpdateTicks()
	{		
		var transform = CreateTransform();

		int i = 0;
		for(float dv = 0.2f; dv < 1.0f; dv += 0.2f, i += 4)
		{
			var vecx0 = Vector2.Transform(new Vector2(dv, 0), transform);
			var vecx1 = Vector2.Transform(new Vector2(dv, -0.05f), transform);
			
			var vecy0 = Vector2.Transform(new Vector2(0, dv), transform);
			var vecy1 = Vector2.Transform(new Vector2(-0.05f, dv), transform);
			
			
			tick_points[i + 0] = Cast2D(vecx0);
			tick_points[i + 1] = Cast2D(vecx1);
			tick_points[i + 2] = Cast2D(vecy0);
			tick_points[i + 3] = Cast2D(vecy1);
		}
	}

	
	private void UpdateLabels()
	{
		var transform = CreateTransform();
		int n = 0;		
		
		for(float dv = 0.2f; dv < 1.0f; dv += 0.2f, n += 2)
		{
			// add extra offset for strings
			var vecx = Vector2.Transform(new Vector2(dv - 0.05f, -0.1f), transform);
			var vecy = Vector2.Transform(new Vector2(-0.1f - 0.05f, dv), transform);

			label_vecs[n + 0] = bounds.TX(dv);
			label_vecs[n + 1] = bounds.TY(dv);

			label_points[n + 0] = Cast2D(vecx);
			label_points[n + 1] = Cast2D(vecy);
		}		

		var x = Vector2.Transform(new Vector2(1.0f, -0.1f), transform);
		var y = Vector2.Transform(new Vector2(-0.1f, 1.0f), transform);

		label_vecs[n + 0] = bounds.TX(1f);
		label_vecs[n + 1] = bounds.TY(1f);

		label_points[n + 0] = Cast2D(x);
		label_points[n + 1] = Cast2D(y);
		n += 2;
		
		label_points_slice = new Memory<SKPoint>(label_points, 0, n);
	}

	public void DrawModels(SKCanvas canvas)
	{
		foreach(var model in models)
		{
			switch (model.ChartType)
	        {
	            case Chart2DType.Line2D:
	                canvas.DrawPoints(SKPointMode.Lines, model.Points, model.Paint ?? black_paint);
					break;

	            case Chart2DType.Points:                    
	                canvas.DrawPoints(SKPointMode.Points, model.Points, model.Paint ?? black_paint); 
					break;

	            default: 
					throw new ArgumentException();
	        }
		}
	}

    public void DrawGridLines(SKCanvas canvas)
    {
        canvas.DrawPoints(SKPointMode.Lines, gridlines_points, silver_paint);
    }
	
    public void DrawAxes(SKCanvas canvas)
    {
        canvas.DrawPoints(SKPointMode.Lines, axis_points, black_paint);
    }

	public void DrawTicks(SKCanvas canvas)
	{
		canvas.DrawPoints(SKPointMode.Lines, tick_points, black_paint);
	}

	public void DrawLabels(SKCanvas canvas)
	{		
		var slice = label_points_slice.Span;
		var len = label_points_slice.Length - 2;
		for(int i = 0; i < len; i += 2)
		{
			canvas.DrawText(label_vecs[i + 0].ToString("N3"), slice[i + 0].X, slice[i + 0].Y, black_paint);	
			canvas.DrawText(label_vecs[i + 1].ToString("N3"), slice[i + 1].X, slice[i + 1].Y, black_paint);	
		}
		
		canvas.DrawText("X", slice[len + 0].X, slice[len + 0].Y, black_paint);	
		canvas.DrawText("Y", slice[len + 1].X, slice[len + 1].Y, black_paint);	
	}
	
    public void DrawLegend(SKCanvas canvas)
    {		
        if(true)
        {
			var vec = LegendPosition;
			int i = 0;

            foreach(var model in models)
            {
                canvas.DrawCircle(new SKPoint(vec.X, vec.Y), 5f, model.Paint ?? black_paint);
                canvas.DrawText(model.Name ?? $"model{i}", new SKPoint(vec.X + 10f, vec.Y), model.Paint ?? black_paint);
                vec.Y += 20f;
				i +=  1;
            }
        }
    }
	
	
	public class CustomDrawOp : ICustomDrawOperation
    {
        private readonly IImmutableGlyphRunReference? _noSkia;
        private readonly SKChart2D _chart;
        
        public CustomDrawOp(Rect bounds, GlyphRun noSkia, SKChart2D chart)
        {
            _noSkia = noSkia.TryCreateImmutableGlyphRunReference();
            _chart = chart;
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
                // if(_chart.Camera.Resync)
                {
                    // _chart.TransformCamera();
					// try
					// {
					_chart.DrawGridLines(canvas);
                    _chart.DrawAxes(canvas);
                    _chart.DrawLabels(canvas);						
                    _chart.DrawModels(canvas);
					_chart.DrawTicks(canvas);
					_chart.DrawLegend(canvas);
					// }
					// catch(Exception e)
					// {
					// 	Console.WriteLine(e);
					// }
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
