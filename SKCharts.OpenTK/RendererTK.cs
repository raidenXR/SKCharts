using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using OpenTK;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Graphics.OpenGL4;
using SkiaSharp;
using SKCharts;
using ImGuiNET;

namespace SKCharts.OpenTK;


public class SliderValueChangedArgs : EventArgs 
{
	public string Name {get;}
	public double PrevValue {get;}
	public double Value {get;}	

	public SliderValueChangedArgs(string name, double prevValue, double curValue)
	{
		Name = name;
		PrevValue = prevValue;
		Value = curValue;
	}
}

public readonly struct SliderInfo
{
	public readonly string Name;
	public readonly float Min;
	public readonly float Max;
	public readonly float Step;
	public readonly SKImage? Img;

	public SliderInfo(string name, double a, double b, double dx, SKImage? notation_img)
	{
		Name = name;
		Min = (float)a;
		Max = (float)b;
		Step = (float)dx;
		Img = notation_img;
	}

	public void Dispose()
	{
		Img?.Dispose();
	}
}


public class Window : GameWindow
{
	GRGlInterface grgInterface;
	GRContext grContext;
	SKSurface surface;
	SKCanvas canvas;
    GRBackendRenderTarget renderTarget;
    SKPaint TestBrush;

	SKChart2D chart2d;
	SKChart3D chart3d;

	ImGuiController controller;
	
	float width;
	float height;

	SKPaint default_paint = new SKPaint {
		Color = SKColors.Black,
		IsAntialias = true,
		TextSize = 20f,
	};


	public SKChart2D Chart2D 
	{
		get => chart2d;
		set {
			chart2d = value;
			chart2d.Width = width;
			chart2d.Height = height;
		}
	}

	public SKChart3D Chart3D
	{
		get => chart3d;
		set {
			chart3d = value;
			chart3d.Width = width;
			chart3d.Height = height;
		}
	}

	public string? Latex {get; set;}

	SKImage? notation_img;
	public SKImage? NotationImg
	{
		get => notation_img;
		set {
			notation_img?.Dispose(); 
			notation_img = value;
		}
	}

	List<SliderInfo> sliders = new(20);

	public event EventHandler<SliderValueChangedArgs> SliderValueChanged;
	

    public Window(string title, int width, int height) : base(new GameWindowSettings(),
        new NativeWindowSettings {
            Title = title,
            Flags = ContextFlags.ForwardCompatible | ContextFlags.Debug,
            Profile = ContextProfile.Core,
            StartFocused = true,
            WindowBorder = WindowBorder.Fixed,
            Size = new Vector2i(width, height)
		}
    )
    {
        // VSync = VSyncMode.Off;

		this.width = (float)width;
		this.height = (float)height;
    }

	private void OnSliderValueChanged(SliderValueChangedArgs e)
	{
		SliderValueChanged?.Invoke(this, e);
	}

	public void ClearSliders()
	{
		foreach(var slider in sliders) slider.Img?.Dispose();
		sliders.Clear();
	}

	public void AddSlider(string name, double a, double b, double dx, SKImage? img)
	{
		sliders.Add(new SliderInfo(name, a, b, dx, img));
	}

    protected override void OnLoad()
    {
        base.OnLoad();
        //Context.MakeCurrent();
        grgInterface = GRGlInterface.Create();
	    grContext = GRContext.CreateGl(grgInterface);
        renderTarget = new GRBackendRenderTarget(ClientSize.X, ClientSize.Y, 0, 8, new GRGlFramebufferInfo(0, (uint)SizedInternalFormat.Rgba8));
        surface = SKSurface.Create(grContext, renderTarget, GRSurfaceOrigin.BottomLeft, SKColorType.Rgba8888);
        canvas = surface.Canvas;

        TestBrush = new SKPaint
	    {
			Color = SKColors.White,
			IsAntialias = true,
			Style = SKPaintStyle.Fill,
			TextAlign = SKTextAlign.Center,
			TextSize = 24
	    };            

		GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
		controller = new ImGuiController(ClientSize.X, ClientSize.Y);
    }

    protected override void OnUnload()
    {
        TestBrush.Dispose();
        surface.Dispose();
        renderTarget.Dispose();
        grContext.Dispose();
        grgInterface.Dispose();

		chart2d?.Dispose();
		chart3d?.Dispose();

		default_paint.Dispose();
		controller.Dispose();
		
        base.OnUnload();
    }

	protected override void OnResize(ResizeEventArgs e)
	{
		base.OnResize(e);

		GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
		controller.WindowResized(ClientSize.X, ClientSize.Y);
	}

	public SKColor Background {get; set;} = SKColors.White;

    double time = 0;
    protected override void OnRenderFrame(FrameEventArgs e)
    {
        // time += args.Time;
        // canvas.Clear(SKColors.CornflowerBlue);
		canvas.Clear(Background);

		canvas.Save();

		controller.Update(this, (float)e.Time);
		// GL.CreateColor(new Color4(0, 0, 0, 0))
        // TestBrush.Color = SKColors.White;
        // canvas.DrawRoundRect(new SKRoundRect(new SKRect(0, 0, 256, 256), (float)Math.Max(Math.Sin(-time) * 128.0f, 0)), TestBrush);

        // TestBrush.Color = SKColors.Black;
        // canvas.DrawText("Hello, World!", 128, 300, TestBrush);
		chart2d?.Draw(canvas);
		chart3d?.Draw(canvas);

		if(Latex != null) 
		{
			var m = default_paint.MeasureText(Latex);
			var x = -m / 2f + width / 2f;
			var y = 0.92f * this.height;
			canvas.DrawText(Latex, new SKPoint(x, y), default_paint);
		}		
		if(notation_img != null) {
			var w = (float)notation_img.Width;
			var h = (float)notation_img.Height;
			var xi = -w / 2f + width / 2f;
			var yi = 0.08f * this.height;
			canvas.DrawImage(notation_img, new SKPoint(xi, yi), default_paint);
		}

		canvas.Restore();
        canvas.Flush();
		
		// Imgui rendering
		// ImGui.ShowDemoWindow();
		controller.Render();
		RenderSliders(canvas);
		ImGuiController.CheckGLError("End of frame");

        SwapBuffers();
    }

	protected override void OnTextInput(TextInputEventArgs e)
	{
		base.OnTextInput(e);
		controller.PressChar((char)e.Unicode);		
	}

	protected override void OnMouseWheel(MouseWheelEventArgs e)
	{
		base.OnMouseWheel(e);
		controller.MouseScroll(e.Offset);
	}

	private void RenderSliders(SKCanvas canvas)
	{
		// var _x = 40;
		// var _y = 40;
		foreach(var slider in sliders)
		{
			// canvas.DrawImage(slider.Img, new SKPoint(_x, _y), default_paint);
			// _y += slider.Img!.Width;
			// _y += 5;   // offset
			var vec = System.Numerics.Vector2.Zero;
			if(ImGui.SliderFloat2(slider.Name, ref vec, slider.Min, slider.Max)) {
				OnSliderValueChanged(new SliderValueChangedArgs(slider.Name, vec.X, vec.X));
			}
			
		}
	}
}


public static class RendererTK
{
	public static Thread OpenWindow() {		
		var thread = new Thread(() => {
			using var window = new Window("TK renderer", 1240, 720);
			window.Chart3D = new SKChart3D();
			window.Run();
		});
		return thread;
	}
}

