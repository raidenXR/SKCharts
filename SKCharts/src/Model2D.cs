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
			xmax = MathF.Max(vec.X, xmax);

			ymin = MathF.Min(vec.Y, ymin);
			ymax = MathF.Max(vec.Y, ymax);
		}

		return new Bounds2D(xmin, xmax, ymin, ymax);
	}

	public static Bounds2D GetBounds(double[] x, double[] y)
	{
		Assert(x.Length == y.Length);
	
		var xmin = double.MaxValue;
		var ymin = double.MaxValue;

		var xmax = double.MinValue;
		var ymax = double.MinValue;

		for(int i = 0; i < x.Length; i++)
		{
			xmin = Math.Min(x[i], xmin);
			xmax = Math.Max(x[i], xmax);

			ymin = Math.Min(y[i], ymin);
			ymax = Math.Max(y[i], ymax);
		}

		return new Bounds2D(xmin, xmax, ymin, ymax);
	}

	public static Bounds2D GetBounds(Bounds2D a, Bounds2D b)
	{
		var xmin = Math.Min(a.Xmin, b.Xmin);
		var xmax = Math.Max(a.Xmax, b.Xmax);

		var ymin = Math.Min(a.Ymin, b.Ymin);
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
	double[] xvalues;
	double[] yvalues;
	int vertices_count;
	// Vector2[] vertices;
	Vector2[] vertices_norm;
	// ushort[] indices;
	SKPoint[] points;
	// SKColor[] colors;
	Bounds2D bounds;
	SKColor single_color;
	Chart2DType chart_type;
	SKPaint paint;
	bool is_disposed;
	
	public SKChart2D? Parent {get; set;}

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
			xvalues = new double[size],
			yvalues = new double[size],
			// vertices = new Vector2[size],
			vertices_norm = new Vector2[size],
			points = new SKPoint[size],
			// colors = null,
			// indices = null,
			bounds = new Bounds2D(0, 1, 0, 1),
			chart_type = Chart2DType.Line2D,
			single_color = color,
			paint = new SKPaint{
				StrokeWidth = 2f,
				TextSize = 18f,
				IsAntialias = true,
				Color = color,
			},
		};			
	}

	public static Model2D CreateLine(double[] x, double[] y, SKColor color)
	{
		Assert(x.Length == y.Length);
		
		var xvalues = new double[2 * (x.Length - 2)];
		var yvalues = new double[2 * (y.Length - 2)];
		var vertices_count = 0;
		var size = xvalues.Length;
		// var vertices = new Vector2[2 * (x.Length - 2)];
		// xvalues[0] = x[0];
		// yvalues[0] = y[0];
		// vertices[0] = new Vector2((float)x[0], (float)y[0]);
		// for(int i = 1, j = 1; i < vertices.Length - 1; i += 2, j += 1)
		// for(int i = 1, j = 1; i < size - 1; i += 2, j += 1)
		// {
			// var vec = new Vector2((float)x[j], (float)y[j]);
			// vertices[i + 0] = vec;
			// vertices[i + 1] = vec;
		// 	xvalues[i + 0] = x[j];
		// 	yvalues[i + 1] = y[j];
		// }
		// vertices[vertices.Length - 1] = new Vector2((float)x[x.Length - 1], (float)y[y.Length - 1]);
		// xvalues[size - 1] = x[x.Length - 1];
		// yvalues[size - 1] = y[y.Length - 1];

		CopyLineValues(x, y, xvalues, yvalues, ref vertices_count);
	
		return new Model2D
		{
			// vertices = vertices,
			xvalues = xvalues,
			yvalues = yvalues,
			vertices_count = vertices_count,
			vertices_norm = new Vector2[size],
			points = new SKPoint[size],
			// colors = null,
			// indices = null,
			bounds = Bounds2D.GetBounds(xvalues, yvalues),
			chart_type = Chart2DType.Line2D,
			single_color = color,
			paint = new SKPaint{
				StrokeWidth = 2f,
				TextSize = 18f,
				IsAntialias = true,
				Color = color,
			},
		};		
	}

	public static Model2D CreatePoints(int capacity, SKColor color)
	{		
		return new Model2D
		{
			// vertices = new Vector2[capacity],
			xvalues = new double[capacity],
			yvalues = new double[capacity],
			vertices_count = 0,
			vertices_norm = new Vector2[capacity],
			points = new SKPoint[capacity],
			// colors = null,
			// indices = null,
			bounds = new Bounds2D(0, 1, 0, 1),
			chart_type = Chart2DType.Points,
			single_color = color,
			paint = new SKPaint{
				StrokeWidth = 2f,
				TextSize = 18f,
				IsAntialias = true,
				Color = color,
			},
		};
	}

	public static Model2D CreatePoints(double[] x, double[] y, SKColor color)
	{
		Assert(x.Length == y.Length);
		
		// var vertices = new Vector2[x.Length];
		var size = x.Length;
		var xvalues = new double[size];
		var yvalues = new double[size];
		var vertices_count = 0;
		
		// for(int i = 0; i < vertices.Length; i++)
		// for(int i = 0; i < size; i++)
		// {
			// vertices[i] = new Vector2((float)x[i], (float)y[i]);
		// 	xvalues[i] = x[i];
		// 	yvalues[i] = y[i];
		// }

		CopyPointsValues(x, y, xvalues, yvalues, ref vertices_count);
		
		return new Model2D
		{
			// vertices = vertices,
			xvalues = xvalues,
			yvalues = yvalues,
			vertices_count = vertices_count,
			vertices_norm = new Vector2[size],
			points = new SKPoint[size],
			// colors = null,
			// indices = null,
			bounds = Bounds2D.GetBounds(xvalues, yvalues),
			chart_type = Chart2DType.Points,
			single_color = color,
			paint = new SKPaint{
				StrokeWidth = 2f,
				TextSize = 18f,
				IsAntialias = true,
				Color = color,
			},
		};
	}	



	// public Vector2[] Vertices => vertices;

	public double[] Xvalues => xvalues;

	public double[] Yvalues => yvalues;

	public int VerticesCount => vertices_count;

	public Vector2[] VerticesNorm => vertices_norm;

	// public ushort[] Indices => indices;

	public SKPoint[] Points => points;

	// public SKColor[] Colors => colors;

	public Bounds2D Bounds => bounds;

	public SKColor SingleColor => single_color;

	public Chart2DType ChartType => chart_type;

	public SKPaint? Paint => paint;

	public string? Name {get; set;}

	public int W {get; set;}
	public int H {get; set;}

	
	///<summary>normalizes to [0, 1] the veritces for the display system</summary>
	public void Normalize(Bounds2D bounds)
	{
		for(int i = 0; i < vertices_norm.Length; i++)
		{
			// var vec = vertices[i];
			// var x = (vec.X - bounds.Xmin) / (bounds.Xmax - bounds.Xmin);
			// var y = (vec.Y - bounds.Ymin) / (bounds.Ymax - bounds.Ymin);
			var x = (xvalues[i] - bounds.Xmin) / (bounds.Xmax - bounds.Xmin);
			var y = (yvalues[i] - bounds.Ymin) / (bounds.Ymax - bounds.Ymin);
			vertices_norm[i] = new Vector2((float)x, (float)y);
		}
	}

	public void UpdateBounds()
	{
		// bounds = Bounds2D.GetBounds(vertices);
		bounds = Bounds2D.GetBounds(xvalues, yvalues);
		Normalize(bounds);
	}

	static void CopyLineValues(double[] x, double[] y, double[] xvalues, double[] yvalues, ref int vertices_count)
	{
		Assert(x.Length == y.Length);
		var N = x.Length;
		vertices_count = N;
		// Assert(vertices.Length >= (2 * (N - 2)));
		Assert(xvalues.Length >= (2 * (N - 2)));
				
		// vertices[0] = new Vector2((float)x[0], (float)y[0]);
		xvalues[0] = x[0];
		yvalues[0] = y[0];
		// for(int i = 1, j = 1; j < N && i + 1 < vertices.Length; i += 2, j += 1)
		for(int i = 1, j = 1; j < N && i + 1 < xvalues.Length; i += 2, j += 1)
		{
			// var vec = new Vector2((float)x[j], (float)y[j]);
			// vertices[i + 0] = vec;
			// vertices[i + 1] = vec;
			xvalues[i + 0] = x[j];
			xvalues[i + 1] = x[j];

			yvalues[i + 0] = y[j];
			yvalues[i + 1] = y[j];
		}
		// vertices[vertices.Length - 1] = new Vector2((float)x[x.Length - 1], (float)y[y.Length - 1]);
		xvalues[xvalues.Length - 1] = x[x.Length - 1];
		yvalues[yvalues.Length - 1] = y[y.Length - 1];

		// for(int i = N; i < vertices.Length; i++)
		for(int i = N; i < xvalues.Length; i++)
		{
			// vertices[i] = vertices[N - 1];
			xvalues[i] = xvalues[N - 1];
			yvalues[i] = yvalues[N - 1];
		}

		// this.UpdateBounds();
		
		// if(Parent != null)
		// {
		// 	Parent.UpdateBounds();
		// 	Parent.Update();
		// }
	}

	static void CopyPointsValues(double[] x, double[] y, double[] xvalues, double[] yvalues, ref int vertices_count)
	{
		Assert(x.Length == y.Length);
		var N = x.Length;
		vertices_count = N;
				
		for(int i = 0; i < N; i++)
		{
			// vertices[i] = new Vector2((float)x[i], (float)y[i]);
			xvalues[i] = x[i];
			yvalues[i] = y[i];
		}
		// vertices[vertices.Length - 1] = new Vector2((float)x[x.Length - 1], (float)y[y.Length - 1]);
		xvalues[xvalues.Length - 1] = x[x.Length - 1];
		yvalues[yvalues.Length - 1] = y[y.Length - 1];

		// for(int i = N; i < vertices.Length; i++)
		for(int i = N; i < xvalues.Length; i++)
		{
			// vertices[i] = vertices[N - 1];
			xvalues[i] = xvalues[N - 1];
			yvalues[i] = yvalues[N - 1];
		}

		// this.UpdateBounds();
		
		// if(Parent != null)
		// {
		// 	Parent.UpdateBounds();
		// 	Parent.Update();
		// }
	}

	public void CopyValues(double[] x, double[] y)
	{
		switch(chart_type)
		{
			case Chart2DType.Line2D:
				CopyLineValues(x, y, xvalues, yvalues, ref vertices_count);
				this.UpdateBounds();		
				Parent?.UpdateBounds();
				Parent?.Update();
				break;

			case Chart2DType.Points:
				CopyPointsValues(x, y, xvalues, yvalues, ref vertices_count);
				this.UpdateBounds();		
				Parent?.UpdateBounds();
				Parent?.Update();
				break;

			default: throw new NotImplementedException();
		}
	}
}
