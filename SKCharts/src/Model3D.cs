using System;
using System.Linq;
using System.Numerics;
using static System.Diagnostics.Debug;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SkiaSharp;
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
			var x_is_number = !float.IsNaN(vec.X);
			xmin = x_is_number ? MathF.Min(vec.X, xmin) : xmin;
			xmax = x_is_number ? MathF.Max(vec.X, xmax) : xmax;

			var y_is_number = !float.IsNaN(vec.Y);
			ymin = y_is_number ? MathF.Min(vec.Y, ymin) : ymin;
			ymax = y_is_number ? MathF.Max(vec.Y, ymax) : ymax;

			var z_is_number = !float.IsNaN(vec.Z);
			zmin = z_is_number ? MathF.Min(vec.Z, zmin) : zmin;
			zmax = z_is_number ? MathF.Max(vec.Z, zmax) : zmax;
		}

		return new Bounds3D(xmin, xmax, ymin, ymax, zmin, zmax);
	}

	
	public static Bounds3D GetBounds(double[] x, double[] y, double[] z)
	{
		Assert(x.Length == y.Length && z.Length == z.Length);
	
		var xmin = double.MaxValue;
		var ymin = double.MaxValue;
		var zmin = double.MaxValue;

		var xmax = double.MinValue;
		var ymax = double.MinValue;
		var zmax = double.MinValue;

		for(int i = 0; i < x.Length; i++)
		{		
			var x_is_number = !double.IsNaN(x[i]);
			xmin = x_is_number ? Math.Min(x[i], xmin): xmin;
			xmax = x_is_number ? Math.Max(x[i], xmax): xmax;

			var y_is_number = !double.IsNaN(y[i]);
			ymin = y_is_number ? Math.Min(y[i], ymin) : ymin;
			ymax = y_is_number ? Math.Max(y[i], ymax) : ymax;
			
			var z_is_number = !double.IsNaN(z[i]);
			zmin = z_is_number ? Math.Min(z[i], zmin) : zmin;
			zmax = z_is_number ? Math.Max(z[i], zmax) : zmax;
		}

		return new Bounds3D(xmin, xmax, ymin, ymax, zmin, zmax);
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double SelectMin(double a, double b) 
	{
		if (double.IsNaN(a)) return b;
		else if (double.IsNaN(b)) return a;
		else return Math.Min(a, b);
	}
	
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private static double SelectMax(double a, double b) 
	{
		if (double.IsNaN(a)) return b;
		else if (double.IsNaN(b)) return a;
		else return Math.Max(a, b);
	}

	public static Bounds3D GetBounds(Bounds3D a, Bounds3D b)
	{
		var xmin = SelectMin(a.Xmin, b.Xmin);
		var xmax = SelectMax(a.Xmax, b.Xmax);
		
		var ymin = SelectMin(a.Ymin, b.Ymin);
		var ymax = SelectMax(a.Ymax, b.Ymax);

		var zmin = SelectMin(a.Zmin, b.Zmin);
		var zmax = SelectMax(a.Zmax, b.Zmax);
		
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
	// Vector3[] vertices;
	double[] xvalues;
	double[] yvalues;
	double[] zvalues;
	int vertices_count;
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
		var xvalues = new double[vertices.Length];
		var yvalues = new double[vertices.Length];
		var zvalues = new double[vertices.Length];
		int vertices_count = 0;

		CopyValues(vertices, xvalues, yvalues, zvalues, ref vertices_count);

		return new Model3D
		{
	        // vertices = vertices,
			xvalues = xvalues,
			yvalues = yvalues,
			zvalues = zvalues,
			vertices_count = vertices_count,
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

	public static Model3D CreateSurface(int capacity, int width, int height)
	{
		
		return new Model3D
		{
	        // vertices = new Vector3[capacity],
			xvalues = new double[capacity],
			yvalues = new double[capacity],
			zvalues = new double[capacity],
			vertices_count = 0,
			vertices_norm = new Vector3[capacity],
	        points   = new SKPoint[(width - 1) * (height - 1) * 4],
	        colors   = new SKColor[(width - 1) * (height - 1) * 4],
	        indices  = new ushort[(width - 1) * (height - 1) * 6],
			bounds   = new Bounds3D(0, 1, 0, 1, 0, 1),
			chart_type = Chart3DType.Surface,			
			W = width,
			H = height,
		};
	}

	public static Model3D CreateLine(Vector3[] line, SKColor color)
	{
		var xvalues = new double[line.Length];
		var yvalues = new double[line.Length];
		var zvalues = new double[line.Length];
		int vertices_count = 0;

		CopyValues(line, xvalues, yvalues, zvalues, ref vertices_count);

		return new Model3D
		{
			// vertices = line,
			xvalues = xvalues,
			yvalues = yvalues,
			zvalues = zvalues,
			vertices_count = vertices_count,
			vertices_norm = new Vector3[line.Length],
			points = new SKPoint[line.Length],
			colors = null,
			indices = null,
			bounds = Bounds3D.GetBounds(line),
			chart_type = Chart3DType.Line3D,
		};
	}
	

	// public Vector3[] Vertices => vertices!;
	public double[] Xvalues => xvalues;

	public double[] Yvalues => yvalues;

	public double[] Zvalues => zvalues;

	public int VerticesCount => vertices_count;

	public Vector3[] VerticesNorm => vertices_norm!;

	public ushort[] Indices => indices!;

	public SKPoint[] Points => points!;

	public SKColor[] Colors => colors!;

	public Bounds3D Bounds => bounds;

	public SKColor SingleColor => single_color;

	public Chart3DType ChartType => chart_type;

	public int W {get; set;}
	public int H {get; set;}

	public SKChart3D? Parent {get; set;}
	

	public void Normalize(Bounds3D bounds)
	{
		for(int i = 0; i < vertices_norm.Length; i++)
		{
			// var vec = vertices[i];
			// var x = (vec.X - bounds.Xmin) / (bounds.Xmax - bounds.Xmin);
			// var y = (vec.Y - bounds.Ymin) / (bounds.Ymax - bounds.Ymin);
			// var z = (vec.Z - bounds.Zmin) / (bounds.Zmax - bounds.Zmin);
			var x = (xvalues[i] - bounds.Xmin) / (bounds.Xmax - bounds.Xmin);
			var y = (yvalues[i] - bounds.Ymin) / (bounds.Ymax - bounds.Ymin);
			var z = (zvalues[i] - bounds.Zmin) / (bounds.Zmax - bounds.Zmin);
			vertices_norm[i] = new Vector3((float)x, (float)y, (float)z);
		}
	}

	public void UpdateBounds()
	{
		// bounds = Bounds3D.GetBounds(vertices);
		bounds = Bounds3D.GetBounds(xvalues, yvalues, zvalues);
		Normalize(bounds);
	}	

	public void CopyValues(ReadOnlySpan<Vector3> values)
	{
		// if(values.Length > vertices.Length) 
		if(values.Length > vertices_norm.Length)
			throw new IndexOutOfRangeException("values.Length is greater than vertices length");
	
		for(int i = 0; i < values.Length; i++)
		{
			// vertices[i] =  values[i];
			xvalues[i] = values[i].X;
			yvalues[i] = values[i].Y;
			zvalues[i] = values[i].Z;
		}

		// for(int i = values.Length; i < vertices.Length; i++)
		for(int i = values.Length; i < vertices_norm.Length; i++)
		{
			// vertices[i] = values[values.Length - 1];
			xvalues[i] = values[values.Length - 1].X;			
			yvalues[i] = values[values.Length - 1].Y;
			zvalues[i] = values[values.Length - 1].Z;
		}

		this.UpdateBounds();

		if(Parent != null)
		{
			Parent.UpdateBounds();
			Parent.Update();
		}
	}

	static void CopyValues(Vector3[] vertices, double[] x, double[] y, double[] z, ref int vertices_count)
	{
		for(int i = 0; i < vertices.Length; i++)
		{
			x[i] = vertices[i].X;
			y[i] = vertices[i].Y;
			z[i] = vertices[i].Z;
		}

		vertices_count = vertices.Length;
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

