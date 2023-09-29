using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics;
using SkiaSharp;
using SKCharts;

namespace MKXK_GUI;


public enum Colormap
{
    Spring,
    Summer,
    Autumn,
    Winter,
    Gray,     
    Hot,
    Cool,
    Jet
}

public static class Colormaps
{
    const int MAP_SIZE = 64;    
    const byte ALPHA = 255;
    
    static SKColor[] spring = new SKColor[MAP_SIZE];    
    static SKColor[] summer = new SKColor[MAP_SIZE];    
    static SKColor[] autumn = new SKColor[MAP_SIZE];    
    static SKColor[] winter = new SKColor[MAP_SIZE];    
    static SKColor[] gray   = new SKColor[MAP_SIZE];    
    static SKColor[] hot    = new SKColor[MAP_SIZE];    
    static SKColor[] cool   = new SKColor[MAP_SIZE];    
    static SKColor[] jet    = new SKColor[MAP_SIZE];  


    static Colormaps()
    {
        MakeSummer();
        MakeSpring();
        MakeAutumn();
        MakeWinter();
        MakeGray();
        MakeHot();
        MakeCool();
        MakeJet();
    }



    public static void Jet(Span<uint> colors, ReadOnlySpan<Vector3> vertices)
    {       
        Debug.Assert(colors.Length == vertices.Length);
         
        for(int i = 0; i < colors.Length; i++)
        {
            // var lerp = (int)(MAP_SIZE * (vertices[i].Z - 0.0f) / (1.0f - 0.0f));
            var n = (int)(MAP_SIZE * vertices[i].Z);
            // colors[i] = jet[n];
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SKColor Jet(float zvalue, float zmin, float zmax)
    {
        var n = (int)((MAP_SIZE - 1) * (zvalue - zmin) / (zmax - zmin));
        return jet[n];        
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static SKColor Jet(float zvalue)
    {
        Debug.Assert(zvalue >= 0 && zvalue <= 1);

        var n = (int)((MAP_SIZE - 1) * zvalue);
        return jet[n];        
    }

    static void MakeSpring()
    {
        for(int i = 0; i < MAP_SIZE; i++)
        {
            double lerp = (double)i / (double)MAP_SIZE;
            byte r = 255;
            byte g = (byte)(255 * lerp);
            byte b = (byte)(255 - g);

            spring[i] = new SKColor(r, g, b);
        }            
    }
    
    static void MakeSummer()
    {
        for(int i = 0; i < MAP_SIZE; i++)
        {                
            double lerp = (double)i / (double)MAP_SIZE;
            byte r = (byte)(255 * lerp);
            byte g = (byte)(255 * 0.5 * (1 + lerp));
            byte b = (byte)(255 - 0.4);

            summer[i] =  new SKColor(r, g, b);
        }
    }

    static void MakeAutumn()
    {
        for(int i = 0; i < MAP_SIZE; i++)
        {                
            double lerp = (double)i / (double)MAP_SIZE;
            byte r = (byte)(255);
            byte g = (byte)(255 * lerp);
            byte b = (byte)(0);

            autumn[i] =  new SKColor(r, g, b);
        }
    }   

    static void MakeWinter()
    {
        for(int i = 0; i < MAP_SIZE; i++)
        {                
            double lerp = (double)i / (double)MAP_SIZE;
            byte r = (byte)(0);
            byte g = (byte)(255 * lerp);
            byte b = (byte)(255 * (1.0 - 0.5 * lerp));
        
            winter[i] =  new SKColor(r, g, b);
        }
    } 

    static void MakeGray()
    {
        for(int i = 0; i < MAP_SIZE; i++)
        {                
            double lerp = (double)i / (double)MAP_SIZE;
            byte r = (byte)(255 * lerp);
            byte g = (byte)(255 * lerp);
            byte b = (byte)(255 * lerp);
        
            gray[i] =  new SKColor(r, g, b);
        }
    }

    static void MakeHot()
    {
        for(int n = 0; n < MAP_SIZE; n++)
        {                
            double lerp = (double)n / (double)MAP_SIZE;
            int n1 = (int)(3.0 * MAP_SIZE / 8.0);
            int i = (int)((MAP_SIZE - 1.0) * lerp);     // [0, MAP_SIZE] -> [0, 64]
            
            double red = (i < n1) ? (1.0 * (i + 1.0) / n1) : 1.0;
            double green = (i < n1) ? 0.0 : (i >= n1 && i < 2 * n1) ? (1.0 * (i + 1 - n1) / n1) : 1.0;
            double blue = (i < 2 * n1) ? 0.0 : 1.0 * (i + 1 - 2 * n1) / ((double)MAP_SIZE - 2.0 * n1);
            
            red *= 255;
            green *= 255;
            blue *= 255;
            hot[n] = new SKColor((byte)red, (byte)green, (byte)blue);
        }
    }

    static void MakeCool()
    {
        for(int n = 0; n < MAP_SIZE; n++)
        {
            double lerp = (double)n / (double)MAP_SIZE;      // [0, 1]
            int i = (int)((MAP_SIZE - 1) * lerp);     // [0, MAP_SIZE] -> [0, 64]
            double array = 1.0 * i / (MAP_SIZE - 1.0);

            byte red = (byte)(255 * array);
            byte green = (byte)(255 * (1 - array));
            byte blue = 255;

            cool[n] = new SKColor(red, green, blue);
        }
    }

    static void MakeJet()
    {            
        int n = (int)Math.Ceiling(MAP_SIZE / 4.0);
        double[,] cMatrix = new double[MAP_SIZE, 3];
        int nMod = 0;
        double[] array1 = new double[3 * n - 1];
        int[] red = new int[array1.Length];
        int[] green = new int[array1.Length];
        int[] blue = new int[array1.Length];

        //if (MAP_SIZE % 4 == 1) nMod = 1;

        for (int i = 0; i < array1.Length; i++)
        {
            if (i < n) array1[i] = (i + 1.0) / n;
            else if (i >= n && i < 2 * n - 1) array1[i] = 1.0;
            else if (i >= 2 * n - 1) array1[i] = (3.0 * n - 1.0 - i) / n;
            green[i] = (int)Math.Ceiling(n / 2.0) - nMod + i;
            red[i] = green[i] + n;
            blue[i] = green[i] - n;
        }

        int nb = 0;
        for (int i = 0; i < blue.Length; i++)
        {
            if (blue[i] > 0) nb++;
        }

        for (int i = 0; i < MAP_SIZE; i++)
        {
            for (int j = 0; j < red.Length; j++)
            {
                if (i == red[j] && red[j] < MAP_SIZE) cMatrix[i, 0] = array1[i - red[0]];
            }
            for (int j = 0; j < green.Length; j++)
            {
                if (i == green[j] && green[j] < MAP_SIZE) cMatrix[i, 1] = array1[i - green[0]];
            }
            for (int j = 0; j < blue.Length; j++)
            {
                if (i == blue[j] && blue[j] >= 0) cMatrix[i, 2] = array1[array1.Length - 1 - nb + i];
            }
        }

        for (int i = 0; i < MAP_SIZE; i++)
        {
            byte _red = (byte)(cMatrix[i, 0] * 255);
            byte _green = (byte)(cMatrix[i, 1] * 255);
            byte _blue = (byte)(cMatrix[i, 2] * 255);            
            jet[i] = new SKColor(_red, _green, _blue);

            // jet[i] = (uint)new SKColor(_red, _green, _blue);
        }           
    }       
    
}

public class Colorbar : IDisposable
{
    SKPoint[] vertices = new SKPoint[64 * 4];
    SKColor[] colors   = new SKColor[64 * 4];
    ushort[] indices   = new ushort[64 * 6];

    SKPoint[] label_points = new SKPoint[64];
    Memory<SKPoint> label_points_slice;
    double[] label_values  = new double[64];
    
    SKRect border;
    SKChart3D parent;
    bool is_disposed = false;
    SKPaint black_paint = new SKPaint
    {
        Color = SKColors.Black,
        StrokeWidth = 2,
        IsAntialias = true,
        TextSize = 16f,
    };


    public Colorbar(SKChart3D parent)
    {
        this.parent = parent;
    }

    public void Dispose()
    {
        if(!is_disposed)
        {
            black_paint.Dispose();
            is_disposed = true;
        }
    }


    public void Update()
    {
        var transform = Matrix3x2.CreateTranslation(0.7f, 0.0f) * Matrix3x2.CreateScale(0.5f, 0.5f);
        var pos  = Vector2.Transform(new Vector2(1.0f, 0.5f), transform);
        var x = pos.X;
        var y = pos.Y;
        var dx = 0.01f;
        var dy = (1f / 64f);
        var value = 0f;
        var w = (float)parent.Width;
        var h = (float)parent.Height;
        
        var bounds = parent.BoundsBox;
        int n = 0;
        for (int i = 0, c = 0, m = 0; m < vertices.Length; m += 4, i += 6, c += 4, n += 1)
        {
            vertices[m + 0] = new SKPoint(w * x, h * (1 - y));
            vertices[m + 1] = new SKPoint(w * (x + dx), h * (1 - y));
            vertices[m + 2] = new SKPoint(w * (x + dx), h * (1 - y - dy));
            vertices[m + 3] = new SKPoint(w * x, h * (1 - y - dy));

            indices[i + 0] = (ushort)(m + 0);
            indices[i + 1] = (ushort)(m + 1);
            indices[i + 2] = (ushort)(m + 3);
            indices[i + 3] = (ushort)(m + 3);
            indices[i + 4] = (ushort)(m + 1);
            indices[i + 5] = (ushort)(m + 2);

            colors[c + 0] =  Colormaps.Jet(value);
            colors[c + 1] =  Colormaps.Jet(value);
            colors[c + 2] =  Colormaps.Jet(value);
            colors[c + 3] =  Colormaps.Jet(value);

            label_values[n] = bounds.TZ(value);
            label_points[n] = new SKPoint(w * (x  + dx + 0.01f), h * (1 - y));

            value += dy;
            y += dy / 2f;
        }

        label_points_slice = new Memory<SKPoint>(label_points, 0, n);
    }

    public void Draw(SKCanvas canvas)
    {
        canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, colors, indices, black_paint);                        
        
        var slice = label_points_slice.Span;
        for(int i = 0; i < slice.Length; i += 4)
        {
            canvas.DrawText(label_values[i].ToString("N3"), slice[i].X, slice[i].Y, black_paint);
        }        
    }
}
