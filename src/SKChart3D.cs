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

public enum Chart3DType
{
    Arrows,
    Line3D,
    Points,
    Surface,        
}  


public readonly struct Bounds3D
{
	public readonly double Xmin;
	public readonly double Xmax;
	public readonly double Ymax;
	public readonly double Ymin;
	public readonly double Zmin;
	public readonly double Zmax;

	public Bounds3D(double xmin, double xmax, double ymin, double ymax, double zmin, double zmax)
	{
		Xmin = xmin;
		Xmax = xmax;
		Ymin = ymin;
		Ymax = ymax;
		Zmin = zmin;
		Zmax = zmax;
	}

	
	public static Bounds3D NoneDefined => new Bounds3D(
		double.MaxValue, double.MinValue, 
		double.MaxValue, double.MinValue,
		double.MaxValue, double.MinValue); 

	public static Bounds3D GetBounds(Vector3[] vertices)
	{
		var xmin = float.MaxValue;
		var ymin = float.MaxValue;
		var zmin = float.MaxValue;

		var xmax = float.MinValue;
		var ymax = float.MinValue;
		var zmax = float.MinValue;

		foreach(var vec in vertices)
		{
			xmin = MathF.Min(vec.X, xmin);
			xmax = MathF.Max(vec.X, xmax);

			ymin = MathF.Min(vec.Y, ymin);
			ymax = MathF.Max(vec.Y, ymax);
			
			zmin = MathF.Min(vec.Z, zmin);
			zmax = MathF.Max(vec.Z, zmax);
		}

		return new Bounds3D(xmin, xmax, ymin, ymax, zmin, zmax);
	}

	public static Bounds3D GetBounds(Bounds3D a, Bounds3D b)
	{
		var xmin = Math.Min(a.Xmin, b.Xmin);
		var xmax = Math.Max(a.Xmax, b.Xmax);
		
		var ymin = Math.Min(a.Ymin, b.Ymin);
		var ymax = Math.Max(a.Ymax, b.Ymax);

		var zmin = Math.Min(a.Zmin, b.Zmin);
		var zmax = Math.Max(a.Zmax, b.Zmax);
		
		return new Bounds3D(xmin, xmax, ymin, ymax, zmin, zmax);
	}


	public static Bounds3D Normal
	{
		get => new Bounds3D(0f, 1f, 0f, 1f, 0f, 1f);
	}

	public readonly double TX(double norm)
	{
		return norm * (Xmax - Xmin) + Xmin;
	}

	public readonly double TY(double norm)
	{
		return norm * (Ymax - Ymin) + Ymin;
	}

	public readonly double TZ(double norm)
	{
		return norm * (Zmax - Zmin) + Zmin;
	}
	
    public override string ToString()
    {
        return $"({Xmin}, {Xmax}, {Ymin}, {Ymax}, {Zmin} {Zmax})";
    }
}



public class Model3D
{
	Vector3[] vertices;
	Vector3[] vertices_norm;
	ushort[] indices;
	SKPoint[] points;
	SKColor[] colors;
	Bounds3D bounds;
	SKColor single_color;
	Chart3DType chart_type;

	private Model3D()
	{
		
	}

	public static Model3D CreateSurface(Vector3[] vertices, int width, int height)
	{
		return new Model3D
		{
	        vertices = vertices,
			vertices_norm = new Vector3[vertices.Length],
	        points   = new SKPoint[(width - 1) * (height - 1) * 4],
	        colors   = new SKColor[(width - 1) * (height - 1) * 4],
	        indices  = new ushort[(width - 1) * (height - 1) * 6],
			bounds   = Bounds3D.GetBounds(vertices),
			chart_type = Chart3DType.Surface,			
			W = width,
			H = height,
		};
	}

	public static Model3D CreateLine(Vector3[] line, SKColor color)
	{
		return new Model3D
		{
			vertices = line,
			vertices_norm = new Vector3[line.Length],
			points = new SKPoint[line.Length],
			colors = null,
			indices = null,
			bounds = Bounds3D.GetBounds(line),
			chart_type = Chart3DType.Line3D,
		};
	}
	

	public Vector3[] Vertices => vertices!;

	public Vector3[] VerticesNorm => vertices_norm!;

	public ushort[] Indices => indices!;

	public SKPoint[] Points => points!;

	public SKColor[] Colors => colors!;

	public Bounds3D Bounds => bounds;

	public SKColor SingleColor => single_color;

	public Chart3DType ChartType => chart_type;

	public int W {get; set;}
	public int H {get; set;}
	

	public void Normalize(Bounds3D bounds)
	{
		for(int i = 0; i < vertices_norm.Length; i++)
		{
			var vec = vertices[i];
			var x = (vec.X - bounds.Xmin) / (bounds.Xmax - bounds.Xmin);
			var y = (vec.Y - bounds.Ymin) / (bounds.Ymax - bounds.Ymin);
			var z = (vec.Z - bounds.Zmin) / (bounds.Zmax - bounds.Zmin);
			vertices_norm[i] = new Vector3((float)x, (float)y, (float)z);
		}
	}

	public void UpdateBounds()
	{
		bounds = Bounds3D.GetBounds(vertices);
		Normalize(bounds);
	}	
}


public class Camera
{
	float elevation = 30f;
	float azimuth = -37.5f;
	internal bool needs_resync = true;

	public float Azimuth
	{
		get => azimuth;
		set
		{
			azimuth = Clamp(value, -180f, 180f);
			Resync = true;
		}
	}

	public float Elevation
	{
		get => elevation;
		set
		{
			elevation = Clamp(value, -90f, 90f);
			Resync = true;
		}
	}

	public bool Resync {get; internal set;} = true;

	public Matrix4x4 View
	{
		get 
		{	
			return Matrix4x4.CreateTranslation(-0.5f, -0.5f, -0.5f) *
				Matrix4x4.CreateScale(0.5f, 0.5f, 0.5f) *
				AzimuthElevation(elevation, azimuth) * 
				Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);			
		} 
	}
	
	public static float Clamp(float value, float min, float max)
	{
		return value < min ? min : value > max ? max : value;
	}
	
	public static Matrix4x4 AzimuthElevation(float elevation, float azimuth)
    {
        elevation = Clamp(elevation, -90, 90) * MathF.PI / 180;
        azimuth   = Clamp(azimuth, -180, 180) * MathF.PI / 180;

        var sne = MathF.Sin(elevation);
        var cne = MathF.Cos(elevation);
        var sna = MathF.Sin(azimuth);
        var cna = MathF.Cos(azimuth);

        return new Matrix4x4(cna,     -sne * sna,    cne * sna,     0,
                             sna,      sne * cna,   -cne * cna,     0,
                             0,        cne,          sne,           0,
                             0,          0,           0,            1);
    }
}


public class SKChart3D : Control, IDisposable
{
	Bounds3D bounds;
    readonly GlyphRun _noSkia;

	SKPoint[] gridlines_points = new SKPoint[20 * 3];
	
	SKPoint[] axis_points = new SKPoint[6];
	Vector3[] axis = new Vector3[]
	{
		new Vector3(0, 0, 0),			
		new Vector3(1, 0, 0),			
		new Vector3(0, 0, 0),			
		new Vector3(0, 1, 0),			
		new Vector3(0, 1, 0),			
		new Vector3(0, 1, 1),			
	};

	Vector3[] ticks = new Vector3[100];
	SKPoint[] tick_points = new SKPoint[100];

	double[] label_vecs   = new double[100];
	SKPoint[] label_points = new SKPoint[100];
	Memory<SKPoint> label_points_slice;
	

	bool is_disposed = false;
	List<Model3D> models = new();	

	public IReadOnlyCollection<Model3D> Models => models;

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
	
	public Camera Camera {get;} = new Camera();

	public Colorbar Colorbar {get; private set;}

	public Bounds3D BoundsBox => bounds;


	public SKChart3D()
	{
        var text = "Current rendering API is not Skia";
        var glyphs = text.Select(ch => Typeface.Default.GlyphTypeface.GetGlyph(ch)).ToArray();
        _noSkia = new GlyphRun(Typeface.Default.GlyphTypeface, 12, text.AsMemory(), glyphs);

		Colorbar = new Colorbar(this);
		
		var _model0 = Model3D.CreateSurface(Sinc3D(out int w0, out int h0), w0, h0);
		var _model1 = Model3D.CreateSurface(Peak3D(out int w1, out int h1), w1, h1);

		// AttachModel(_model0);
		AttachModel(_model1);
		Update();
	}

	~SKChart3D()
	{
		Dispose();
	}

	public void Dispose()
	{
		if(!is_disposed)
		{
			black_paint.Dispose();
			silver_paint.Dispose();
			Colorbar?.Dispose();
			is_disposed = true;
		}
	}

	public void AttachModel(Model3D model)
	{
		if(models.Count == 0)
		{
			bounds = model.Bounds;
		}
		else
		{
			bounds = Bounds3D.GetBounds(bounds, model.Bounds);
		}

		models.Add(model);
		foreach(var _model in models) _model.Normalize(bounds);
	}
	
	public void UpdateBounds()
	{
		bounds = Bounds3D.NoneDefined;
		foreach(var model in models)
		{
			bounds = Bounds3D.GetBounds(bounds, model.Bounds);
		}
	}

    public void Update()
    {
        UpdateGridLines();
        UpdateAxes();
        UpdateTicks();
		UpdateModels();
        UpdateLabels();
		Colorbar.Update();

        Camera.Resync = false;        
		
		// foreach(var model in models) Console.WriteLine(model.Bounds.ToString());
		// Console.WriteLine(bounds.ToString());
    }


    private void UpdateModels()
    {
		foreach(var model in models)
		{
	        switch (model.ChartType)
	        {
	            case Chart3DType.Line3D:
	                UpdateLine(model);
					break;

	            case Chart3DType.Points:
	            case Chart3DType.Surface:
	                UpdateSurface(model); 
					break;

	            // case Chart3DType.Arrows:
	                // UpdateArrows(model); break;

	            default: 
					throw new ArgumentException();
	        }			
		}		
    }


    private void UpdateSurface(Model3D model)
    {
		int width  = model.W;
		int height = model.H;
		
		var vertices = model.Vertices;
        var vertices_norm = model.VerticesNorm;
        var points   = model.Points;
        var colors   = model.Colors;
        var indices  = model.Indices;
		var color    = model.SingleColor;

		var elevation = Camera.Elevation;
		var azimuth   = Camera.Azimuth;
		var transform = Camera.View;

		var w = (float)Width;
		var h = (float)Height;

        // Draw mesh chart:
        for (int i = 0, v = 0, c = 0, l = 0; i < width - 1; i++)
        {
            int ii = i;
            if (elevation >= 0)
            {
                ii = i;
                if (azimuth >= -180 && azimuth < 0) ii = width - 2 - i;
            }
            else
            {
                ii = width - 2 - i;
                if (azimuth >= -180 && azimuth < 0) ii = i;
            }

            for (int j = 0; j < height - 1; j++, v += 4, l += 6, c += 4)
            {
                int jj = j;
                if (elevation < 0) jj = height - 2 - j;

				var vec0 = Vector3.Transform(vertices_norm[ii * width + jj], transform);
				var vec1 = Vector3.Transform(vertices_norm[ii * width + jj + 1], transform);
				var vec2 = Vector3.Transform(vertices_norm[width * (ii + 1) + jj + 1], transform);
				var vec3 = Vector3.Transform(vertices_norm[width * (ii + 1) + jj], transform);
				
				points[v + 0] = new SKPoint(vec0.X * w, (1 - vec0.Y) * h);
				points[v + 1] = new SKPoint(vec1.X * w, (1 - vec1.Y) * h);
				points[v + 2] = new SKPoint(vec2.X * w, (1 - vec2.Y) * h);
				points[v + 3] = new SKPoint(vec3.X * w, (1 - vec3.Y) * h);				

                indices[l + 0] = (ushort)(v + 0);
                indices[l + 1] = (ushort)(v + 1);
                indices[l + 2] = (ushort)(v + 3);
                indices[l + 3] = (ushort)(v + 3);
                indices[l + 4] = (ushort)(v + 1);
                indices[l + 5] = (ushort)(v + 2);
				
                // indices[l + 0] = (ushort)(v + 0);
                // indices[l + 1] = (ushort)(v + 1);
                // indices[l + 2] = (ushort)(v + 3);
                // indices[l + 3] = (ushort)(v + 3);
                // indices[l + 4] = (ushort)(v + 2);
                // indices[l + 5] = (ushort)(v + 1);

				colors[c + 0] = Colormaps.Jet(vertices_norm[ii * width + jj].Z);
				colors[c + 1] = Colormaps.Jet(vertices_norm[ii * width + jj + 1].Z);
				colors[c + 2] = Colormaps.Jet(vertices_norm[width * (ii + 1) + jj + 1].Z);
				colors[c + 3] = Colormaps.Jet(vertices_norm[width * (ii + 1) + jj].Z);
            }
        }	
                
    }


    private void UpdateLine(Model3D model)
    {
        var vertices  = model.Vertices;
        var points    = model.Points;
		// var translate = Matrix4x4.CreateTranslation(0.5f, 0.5f, 0.5f);
		// var scale     = Matrix4x4.CreateScale(0.5f, 0.5f, 0.5f);
  //       var rotation  = AzimuthElevation(Camera.Elevation, Camera.Azimuth);
		// var transform = translate * rotation * scale;
		var transform = Camera.View;		
		
        for (int i = 0, v = 0; i < vertices.Length; i += 2, v += 2)
        {
            points[v + 0] = Cast2D(Vector3.Transform(vertices[i + 0], transform));
            points[v + 1] = Cast2D(Vector3.Transform(vertices[i + 1], transform));
        }
    }


	SKPoint Cast2D(Vector3 vec)
	{
		return new SKPoint
		{
			X = vec.X * (float)Width,
			Y = (1 - vec.Y) * (float)Height,
		};
	}
	
	private void UpdateGridLines()
	{
		var transform = Camera.View;
		int i = 0;

		for(float dv = 0.2f; i < 20; dv += 0.2f, i += 4)
		{
			var vecx0 = Vector3.Transform(new Vector3(dv, 0, 0), transform);
			var vecx1 = Vector3.Transform(new Vector3(dv, 1, 0), transform);
			
			var vecy0 = Vector3.Transform(new Vector3(0, dv, 0), transform);
			var vecy1 = Vector3.Transform(new Vector3(1, dv, 0), transform);
			
			// var vecz0 = Vector3.Transform(new Vector3(0, 1, dv), transform);
			// var vecz1 = Vector3.Transform(new Vector3(-0.05f, 1.05f, dv), transform);
			
			gridlines_points[i + 0] = Cast2D(vecx0);
			gridlines_points[i + 1] = Cast2D(vecx1);
			gridlines_points[i + 2] = Cast2D(vecy0);
			gridlines_points[i + 3] = Cast2D(vecy1);
			// tick_points[i + 4] = Cast2D(vecz0);
			// tick_points[i + 5] = Cast2D(vecz1);			
		}
		
		for(float dv = 0.2f; i < 40; dv += 0.2f, i += 4)
		{
			var vecx0 = Vector3.Transform(new Vector3(0, 1, dv), transform);
			var vecx1 = Vector3.Transform(new Vector3(1, 1, dv), transform);
			
			var vecy0 = Vector3.Transform(new Vector3(1, 0, dv), transform);
			var vecy1 = Vector3.Transform(new Vector3(1, 1, dv), transform);
			
			// var vecz0 = Vector3.Transform(new Vector3(0, 1, dv), transform);
			// var vecz1 = Vector3.Transform(new Vector3(-0.05f, 1.05f, dv), transform);
			
			gridlines_points[i + 0] = Cast2D(vecx0);
			gridlines_points[i + 1] = Cast2D(vecx1);
			gridlines_points[i + 2] = Cast2D(vecy0);
			gridlines_points[i + 3] = Cast2D(vecy1);
			// tick_points[i + 4] = Cast2D(vecz0);
			// tick_points[i + 5] = Cast2D(vecz1);			
		}
		
		for(float dv = 0.2f; i < 60; dv += 0.2f, i += 4)
		{
			var vecx0 = Vector3.Transform(new Vector3(dv, 1, 0), transform);
			var vecx1 = Vector3.Transform(new Vector3(dv, 1, 1), transform);
			
			var vecy0 = Vector3.Transform(new Vector3(1, dv, 0), transform);
			var vecy1 = Vector3.Transform(new Vector3(1, dv, 1), transform);
			
			// var vecz0 = Vector3.Transform(new Vector3(0, 1, dv), transform);
			// var vecz1 = Vector3.Transform(new Vector3(-0.05f, 1.05f, dv), transform);
			
			gridlines_points[i + 0] = Cast2D(vecx0);
			gridlines_points[i + 1] = Cast2D(vecx1);
			gridlines_points[i + 2] = Cast2D(vecy0);
			gridlines_points[i + 3] = Cast2D(vecy1);
			// tick_points[i + 4] = Cast2D(vecz0);
			// tick_points[i + 5] = Cast2D(vecz1);			
		}
	}

    private void UpdateAxes()
    {
		var transform = Camera.View;

		for(int i = 0; i < axis.Length; i++)
		{
			var vec = Vector3.Transform(axis[i], transform);
			axis_points[i] = Cast2D(vec);
		}
    }

	private void UpdateTicks()
	{		
		var transform = Camera.View;

		int i = 0;
		for(float dv = 0.2f; dv < 1.0f; dv += 0.2f, i += 6)
		{
			var vecx0 = Vector3.Transform(new Vector3(dv, 0, 0), transform);
			var vecx1 = Vector3.Transform(new Vector3(dv, -0.05f, 0), transform);
			
			var vecy0 = Vector3.Transform(new Vector3(0, dv, 0), transform);
			var vecy1 = Vector3.Transform(new Vector3(-0.05f, dv, 0), transform);
			
			var vecz0 = Vector3.Transform(new Vector3(0, 1, dv), transform);
			var vecz1 = Vector3.Transform(new Vector3(-0.05f, 1.05f, dv), transform);
			
			tick_points[i + 0] = Cast2D(vecx0);
			tick_points[i + 1] = Cast2D(vecx1);
			tick_points[i + 2] = Cast2D(vecy0);
			tick_points[i + 3] = Cast2D(vecy1);
			tick_points[i + 4] = Cast2D(vecz0);
			tick_points[i + 5] = Cast2D(vecz1);
		}
	}

	private void UpdateLabels()
	{
		var transform = Camera.View;
		int n = 0;		
		
		for(float dv = 0.2f; dv < 1.0f; dv += 0.2f, n += 3)
		{
			var vecx = Vector3.Transform(new Vector3(dv, -0.1f, 0), transform);
			var vecy = Vector3.Transform(new Vector3(-0.1f, dv, 0), transform);
			var vecz = Vector3.Transform(new Vector3(-0.1f, 1.1f, dv), transform);

			label_vecs[n + 0] = bounds.TX(dv);
			label_vecs[n + 1] = bounds.TY(dv);
			label_vecs[n + 2] = bounds.TZ(dv);

			label_points[n + 0] = Cast2D(vecx);
			label_points[n + 1] = Cast2D(vecy);
			label_points[n + 2] = Cast2D(vecz);
		}		
		
		var x = Vector3.Transform(new Vector3(1.0f, -0.1f, 0), transform);
		var y = Vector3.Transform(new Vector3(-0.1f, 1.0f, 0), transform);
		var z = Vector3.Transform(new Vector3(-0.1f, 1.1f, 1.0f), transform);

		label_vecs[n + 0] = bounds.TX(1f);
		label_vecs[n + 1] = bounds.TY(1f);
		label_vecs[n + 2] = bounds.TZ(1f);

		label_points[n + 0] = Cast2D(x);
		label_points[n + 1] = Cast2D(y);
		label_points[n + 2] = Cast2D(z);
		n += 3;

		label_points_slice = new Memory<SKPoint>(label_points, 0, n);
	}

	public void Draw(SKCanvas canvas)
	{
		var azimuth = Camera.Azimuth;
		if(azimuth > -90f && azimuth < 90)
		{
			DrawGridLines(canvas);
			DrawAxes(canvas);
			DrawTicks(canvas);
			DrawLabels(canvas);
			DrawModels(canvas);
			Colorbar?.Draw(canvas);
		}
		else
		{
			DrawModels(canvas);
			DrawGridLines(canvas);
			DrawAxes(canvas);
			DrawTicks(canvas);
			DrawLabels(canvas);
			Colorbar?.Draw(canvas);			
		}
	}

	public void DrawModels(SKCanvas canvas)
	{
		foreach(var model in models)
		{
			switch (model.ChartType)
	        {
	            case Chart3DType.Line3D:
					black_paint.Color = model.SingleColor;
	                canvas.DrawPoints(SKPointMode.Lines, model.Points, black_paint);
					black_paint.Color = SKColors.Black;
					break;

	            case Chart3DType.Arrows:
	                canvas.DrawPoints(SKPointMode.Lines, model.Points, black_paint); 
					break;

	            case Chart3DType.Points:                    
	                canvas.DrawPoints(SKPointMode.Points, model.Points, black_paint); 
					break;

	            case Chart3DType.Surface:
	                canvas.DrawVertices(
						SKVertexMode.Triangles, 
						model.Points, 
						null, model.Colors, 
						model.Indices, 
						black_paint
					); 
					break;

	            default: 
					throw new ArgumentException();
	        }
		}
	}

    private void DrawColorBar(SKCanvas canvas)
    {
        // Colorbar?.Draw(canvas);
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
		var len = label_points_slice.Length - 3;
		for(int i = 0; i < len; i += 3)
		{
			canvas.DrawText(label_vecs[i + 0].ToString("N3"), slice[i + 0].X, slice[i + 0].Y, black_paint);	
			canvas.DrawText(label_vecs[i + 1].ToString("N3"), slice[i + 1].X, slice[i + 1].Y, black_paint);	
			canvas.DrawText(label_vecs[i + 2].ToString("N3"), slice[i + 2].X, slice[i + 2].Y, black_paint);	
		}
		
		canvas.DrawText("X", slice[len + 0].X, slice[len + 0].Y, black_paint);	
		canvas.DrawText("Y", slice[len + 1].X, slice[len + 1].Y, black_paint);	
		canvas.DrawText("Z", slice[len + 2].X, slice[len + 2].Y, black_paint);	
	}


    public class CustomDrawOp : ICustomDrawOperation
    {
        private readonly IImmutableGlyphRunReference? _noSkia;
        private readonly SKChart3D _chart;
        
        public CustomDrawOp(Rect bounds, GlyphRun noSkia, SKChart3D chart)
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
				_chart.Draw(canvas);
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


	static void NormalizeVertices(Span<Vector3> vertices, ReadOnlySpan<Vector3> original, Bounds3D bounds)
	{
		for(int i = 0; i < original.Length; i++)
		{
			var vec = original[i];
			var x = (vec.X - bounds.Xmin) / (bounds.Xmax - bounds.Xmin);
			var y = (vec.Y - bounds.Ymin) / (bounds.Ymax - bounds.Ymin);
			var z = (vec.Z - bounds.Zmin) / (bounds.Zmax - bounds.Zmin);
			vertices[i] = new Vector3((float)x, (float)y, (float)z);
		}
	}

	static void CastVertices(SKPoint[] points, Vector3[] vertices, float width, float height)
	{
		var points_f   = Unsafe.As<float[]>(points);
		var vertices_f = Unsafe.As<float[]>(vertices);
		var n_points   = 2 * points.Length;
		var n_vertices = 3 * vertices.Length;

		for(int i = 0, j = 0; i < n_points; i += 2, j += 3)
		{
			points_f[i + 0] = width * vertices_f[j + 0];
			points_f[i + 1] = height * (1.0f - vertices_f[j + 1]);
		}
	}


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private SKPoint Normalize(Vector3 pt, Matrix4x4 transform)
    {
        var x1 = (float)((pt.X - bounds.Xmin) / (bounds.Xmax - bounds.Xmin)); // - 0.5);
        var y1 = (float)((pt.Y - bounds.Ymin) / (bounds.Ymax - bounds.Ymin)); // - 0.5);
        var z1 = (float)((pt.Z - bounds.Zmin) / (bounds.Zmax - bounds.Zmin)); // - 0.5);

        var result = Vector3.Transform(new Vector3(x1, y1, z1), transform);

        // Coordinate transformation from World to Device system:
        // var xShift = 1.05f;
        // var xScale = 1.00f;
        // var yShift = 1.05f;
        // var yScale = 0.90f;

        return new SKPoint
        {
            // X = (xShift + xScale * result.X) * (float)(this.Width / 2),
            // Y = (yShift - yScale * result.Y) * (float)(this.Height / 2),); // 
			X = result.X * (float)Width,
			Y = (1 - result.Y) * (float)Height,
        };
    }


	
	
    public static float ToRadians(float degrees)
	{
		return (float) (degrees * 0.017453292519943295769236907684886);
	}

    

	static Vector3[] Sinc3D(out int width, out int height)
    {
        float Xmin = -3;
        float Xmax = 3;
        float Ymin = -3;
        float Ymax = 3;
        float Zmin = -8;
        float Zmax = 8;
        float XTick = 4;
        float YTick = 4;
        float ZTick = 0.5f;

        float XLimitMin = Xmin;
        float YLimitMin = Ymin;
        float XSpacing = 0.2f;
        float YSpacing = 0.2f;
        width = (int)((Xmax - Xmin) / XSpacing) + 1;
        height = (int)((Ymax - Ymin) / YSpacing) + 1;
        Vector3[] pts = new Vector3[width * height];

        for (int i = 0; i < width; i++)
        {
            for (int j = 0; j < height; j++)
            {
                float x = XLimitMin + i * XSpacing;
                float y = YLimitMin + j * YSpacing;
                float r = (float)(Math.Sqrt(x * x + y * y) + 0.000001);
                float z = (float)(Math.Sin(r) / r);
                pts[i * width + j] = new Vector3(x, y, z);
            }
        }

        return pts;
    }
	
    internal static Vector3[] Peak3D(out int w, out int h)
    {
        float Xmin = -3;
        float Xmax = 3;
        float Ymin = -3;
        float Ymax = 3;
        float Zmin = -8;
        float Zmax = 8;

        float XLimitMin = Xmin;
        float YLimitMin = Ymin;
        float XSpacing = 0.2f;
        float YSpacing = 0.2f;
        int XNumber = Convert.ToInt16((Xmax - Xmin) / XSpacing) + 1;
        int YNumber = Convert.ToInt16((Ymax - Ymin) / YSpacing) + 1;

        //ref DataSeriesSurface ds = ref cs.DS;

        Vector3[] pts = new Vector3[XNumber * YNumber];

        for (int i = 0; i < XNumber; i++)
        {
            for (int j = 0; j < YNumber; j++)
            {
                double x = XLimitMin + i * XSpacing;
                double y = YLimitMin + j * YSpacing;
                double z = 3 * Math.Pow((1 - x), 2) *
                	Math.Exp(-x * x - (y + 1) * (y + 1)) - 10 *
                	(0.2 * x - Math.Pow(x, 3) - Math.Pow(y, 5)) *
                	Math.Exp(-x * x - y * y) - 1 / 3 *
                	Math.Exp(-(x + 1) * (x + 1) - y * y);

                Vector3 vec = new Vector3((float)x, (float)y, (float)z);
                pts[i * XNumber + j] = vec;

                // Xmin = vec.X < Xmin ? vec.X : Xmin;
                // Ymin = vec.Y < Ymin ? vec.Y : Ymin;
                // Zmin = vec.Z < Zmin ? vec.Z : Zmin;

                // Xmax = vec.X > Xmax ? vec.X : Xmax;
                // Ymax = vec.Y > Ymax ? vec.Y : Ymax;
                // Zmax = vec.Z > Zmax ? vec.Z : Zmax;
            }
        }

        // Console.WriteLine($"{Xmin} {Xmax} {Ymin} {Ymax} {Zmin} {Zmax}");

		w = XNumber;
		h = YNumber;

        return pts;
    }
}
