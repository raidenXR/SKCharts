using System;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using SkiaSharp;
using static System.Diagnostics.Debug;


namespace SKCharts;


public class SKChart2D : IDisposable
{
	Bounds2D bounds = new Bounds2D(0f, 1f, 0f, 1f);

	// readonly object threadlock = new object();
	
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
	// List<Model2D> models = new();
	ThreadedList<Model2D> models = new();

	public IList<Model2D> Models => models;

	public string XTitle {get; set;} = "X";

	public SKImage XImg {get; set;} = null;
	
	public string YTitle {get; set;} = "Y";

	public SKImage YImg {get; set;} = null;

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

	public double Width {get; set;} = 800;

	public double Height {get; set;} = 600;

	public SKChart2D()
	{
		Update();
	}
	
	///<summary> initialize an SKChart2D instance for a demo </summary>
	public static void Demo(SKChart2D demo)
	{		
		var rand = new Random();
		var x = new double[100];
		var y1 = new double[100];
		var y2 = new double[100];
		
		for (int i = 0; i < 100; i++)
		{
			x[i] = i;
			y1[i] = 90 * rand.NextDouble();
			y2[i] = 90 * Math.Sin(i);
		}

		var _model0 = Model2D.CreateLine(x, y1, SKColors.Green);
		var _model1 = Model2D.CreatePoints(x, y2, SKColors.Red);		
		_model1.Paint.StrokeWidth = 4f;
		_model1.Name = "sin line";

		demo.AttachModel(_model0);
		demo.AttachModel(_model1);
		demo.Update();
	}

	public SKChart2D(Model2D model) 
	{
		AttachModel(model);
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
			foreach(var model in models) model.Dispose();
		}
		is_disposed = true;
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
		model.Parent = this;
		foreach(var _model in models) _model.Normalize(bounds);

		UpdateBounds();
		Update();
	}

	// public void AttachModelThreadLock(Model2D model)
	// {
	// 	lock(threadlock)
	// 	{
	// 		AttachModel(model);			
	// 	}
	// }

	public void DetachModel(Model2D model)
	{
		if(models.Contains(model))
		{
			model.Parent = null;
			models.Remove(model);
			UpdateBounds();
			foreach(var _model in models) _model.Normalize(bounds);
			Update();
		}
	}

	// public void DetachModelThreadLock(Model2D model)
	// {
	// 	lock(threadlock)
	// 	{
	// 		DetachModel(model);
	// 	}
	// }

	public void DetachModelAt(int index)
	{
		if(index < models.Count)
		{
			models[index].Parent = null;
			models.RemoveAt(index);
			UpdateBounds();
			foreach(var _model in models) _model.Normalize(bounds);
			Update();
		}
	}

	public void NormalizeModels()
	{
		UpdateBounds();
		foreach (var _model in models) _model.Normalize(bounds);
		Update();
	}

	// public void DetachModelAtThreadLock(int index)
	// {
	// 	lock(threadlock)
	// 	{
	// 		DetachModelAt(index);
	// 	}
	// }

	public void UpdateBounds()
	{
		bounds = models.Count > 0 ? models[0].Bounds : new Bounds2D(0, 1, 0, 1);
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

	public void Draw(SKCanvas canvas)
	{
		DrawGridLines(canvas);
        DrawAxes(canvas);
        DrawLabels(canvas);						
        DrawModels(canvas);
		DrawTicks(canvas);
		DrawLegend(canvas);		
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
		
		if (XImg != null)		
			canvas.DrawImage(XImg, slice[len + 0].X, slice[len + 0].Y, black_paint);		
		else
			canvas.DrawText(XTitle, slice[len + 0].X, slice[len + 0].Y, black_paint);			
		
		if (YImg != null)
			canvas.DrawImage(YImg, slice[len + 1].X, slice[len + 1].Y, black_paint);
		else
			canvas.DrawText(YTitle, slice[len + 1].X, slice[len + 1].Y, black_paint);	
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
}
