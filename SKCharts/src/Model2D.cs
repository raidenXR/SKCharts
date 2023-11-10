using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SkiaSharp;
using static System.Diagnostics.Debug;


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

public class Model2D : IDisposable
{
	Vector2[] vertices;
	Vector2[] vertices_norm;
	ushort[] indices;
	SKPoint[] points;
	SKColor[] colors;
	Bounds2D bounds;
	SKColor single_color;
	Chart2DType chart_type;
	bool is_disposed;
	
	SKChart2D Parent {get; set;}

	public void Dispose()
	{
		if(!is_disposed)
		{
			Paint?.Dispose();
		}
		is_disposed = true;
	}

	~Model2D()
	{
		Dispose();
	}

	public static Model2D CreateLine(int capacity, SKColor color) 
	{
		int size = 2 * (capacity - 2);
		
		return new Model2D{
			vertices = new Vector2[size],
			vertices_norm = new Vector2[size],
			points = new SKPoint[size],
			colors = null,
			indices = null,
			bounds = new Bounds2D(0, 1, 0, 1),
			chart_type = Chart2DType.Line2D,
			single_color = color,
			Paint = null,
		};			
	}

	public static Model2D CreateLine(double[] x, double[] y, SKColor color)
	{
		Assert(x.Length == y.Length);
		
		var vertices = new Vector2[2 * (x.Length - 2)];
		vertices[0] = new Vector2((float)x[0], (float)y[0]);
		for(int i = 1, j = 1; i < vertices.Length - 1; i += 2, j += 1)
		{
			var vec = new Vector2((float)x[j], (float)y[j]);
			vertices[i + 0] = vec;
			vertices[i + 1] = vec;
		}
		vertices[vertices.Length - 1] = new Vector2((float)x[x.Length - 1], (float)y[y.Length - 1]);
	
		return new Model2D
		{
			vertices = vertices,
			vertices_norm = new Vector2[vertices.Length],
			points = new SKPoint[vertices.Length],
			colors = null,
			indices = null,
			bounds = Bounds2D.GetBounds(vertices),
			chart_type = Chart2DType.Line2D,
			single_color = color,
			Paint = new SKPaint{
				IsAntialias = true,
				Color = color,
			},
		};		
	}

	public static Model2D CreatePoints(int capacity, SKColor color)
	{		
		return new Model2D
		{
			vertices = new Vector2[capacity],
			vertices_norm = new Vector2[capacity],
			points = new SKPoint[capacity],
			colors = null,
			indices = null,
			bounds = new Bounds2D(0, 1, 0, 1),
			chart_type = Chart2DType.Points,
			single_color = color,
			Paint = new SKPaint{
				IsAntialias = true,
				Color = color,
			},
		};
	}

	public static Model2D CreatePoints(double[] x, double[] y, SKColor color)
	{
		Assert(x.Length == y.Length);

		var vertices = new Vector2[x.Length];
		for(int i = 0; i < vertices.Length; i++)
		{
			vertices[i] = new Vector2((float)x[i], (float)y[i]);
		}
		
		return new Model2D
		{
			vertices = vertices,
			vertices_norm = new Vector2[vertices.Length],
			points = new SKPoint[vertices.Length],
			colors = null,
			indices = null,
			bounds = Bounds2D.GetBounds(vertices),
			chart_type = Chart2DType.Points,
			single_color = color,
			Paint = new SKPaint{
				IsAntialias = true,
				Color = color,
			},
		};
	}


	
	[Obsolete]
	public static Model2D CreateLine(double[] x, double[] y, uint color)
	{
		return CreateLine(x, y, new SKColor(color));
	}

	[Obsolete]
	public static Model2D CreateLine(int capacity, SKPaint paint) 
	{
		int size = 2 * (capacity - 2);
		
		return new Model2D{
			vertices = new Vector2[size],
			vertices_norm = new Vector2[size],
			points = new SKPoint[size],
			colors = null,
			indices = null,
			bounds = new Bounds2D(0, 1, 0, 1),
			chart_type = Chart2DType.Line2D,
			single_color = SKColors.Black,
			Paint = paint,
		};	
		
	}

	[Obsolete]
	public static Model2D CreateLine(int capacity, uint color)
	{
		return CreateLine(capacity, new SKColor((uint)color));
	}
	
	[Obsolete]
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
	
	[Obsolete]
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

	
	///<summary>normalizes to [0, 1] the veritces for the display system</summary>
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

	public void CopyValues(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
	{
		Assert(x.Length == y.Length);
		var N = x.Length;
		Assert(vertices.Length >= (2 * (N - 2)));
				
		vertices[0] = new Vector2((float)x[0], (float)y[0]);
		for(int i = 1, j = 1; j < N; i += 2, j += 1)
		{
			var vec = new Vector2((float)x[j], (float)y[j]);
			vertices[i + 0] = vec;
			vertices[i + 1] = vec;
		}
		vertices[vertices.Length - 1] = new Vector2((float)x[x.Length - 1], (float)y[y.Length - 1]);

		for(int i = N; i < vertices.Length; i++)
		{
			vertices[i] = vertices[N - 1];
		}
	}
}

