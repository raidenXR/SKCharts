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

	public static Model3D CreateSurface(int capacity, int width, int height)
	{
		
		return new Model3D
		{
	        vertices = new Vector3[capacity],
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

	public SKChart3D? Parent {get; set;}
	

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

	public void CopyValues(ReadOnlySpan<Vector3> values)
	{
		if(values.Length > vertices.Length) 
			throw new IndexOutOfRangeException("values.Length is greater than vertices length");
	
		for(int i = 0; i < values.Length; i++)
		{
			vertices[i] =  values[i];
		}

		for(int i = values.Length; i < vertices.Length; i++)
		{
			vertices[i] = values[values.Length - 1];
		}

		this.UpdateBounds();

		if(Parent != null)
		{
			Parent.UpdateBounds();
			Parent.Update();
		}
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

