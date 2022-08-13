using System;
using System.Numerics;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.Marshal;

namespace SKCharts
{
    static unsafe class Sk
    {

    #if LINUX
        const string libname = "SkiaSharp.so";
    #elif WINDOWS
        const string libname = "SkiaSharp.dll";
    #endif
    
        [DllImport(libname)]
        static extern void sk_canvas_draw_vertices(SKCanvas canvas, const sk_vertices_t* vertices, sk_blendmode_t mode, const sk_paint_t* paint);

        [DllImport(libname)]
        static extern sk_vertices_t* sk_vertices_make_copy(sk_vertices_vertex_mode_t vmode, int vertexCount, const sk_point_t* positions, const sk_point_t* texs, const sk_color_t* colors, int indexCount, const uint16_t* indices);

        [DllImport(libname)]
        static extern void sk_vertices_unref(sk_vertices_t* cvertices);

        [DllImport(libname)]
        static extern void sk_vertices_ref(sk_vertices_t* cvertices);

        [DllImport(libname)]
        static extern void sk_canvas_flush(SKCanvas canvas);

        public static void canvas_drawPoints(SKCanvas canvas, Span<SKPaint> pts, SKPaint paint)
        {
            
        }

        public static void canvas_drawLines(SKCanvas canvas, Span<SKPoint> pts, SKPaint paint)
        {
            
        }

        internal static void write_vertices(ref IntrPtr vertices, Span<SKPoint> positions, Span<SKColor> colors, Span<ushort> indices)
        {
            var mode = ...;           // hard copy value
            sk_make_vertices(mode, positions.Length, positions, colors, indices.Length, indices);
        }
        
        public static void canvas_drawVertices(SKCanvas canvas, IntrPtr vertices)
        {
            unsafe
            {
                uint mode = SKVertexMode.Triangles;        // hard copy value
                SKPoint* textures = null;
                
                var vertices = sk_vertices_make_copy(mode, positions.Length, positions, textures, colors, indices);
                sk_canvas_draw_vertices(...);
            }
        }

        public static void canvas_drawText(SKCanvas canvas, ReadOnlyString<char> text, SKPoint position, SKPaint paint)
        {
            
        }
        
        public static void canvas_drawDouble(SKCanvas canvas, double number, SKPoint position, SKPaint paint)
        {
            var buffer = stackalloc char[64];
            DoubleToString(number, buffer, 3);
            canvas_drawText(canvas, buffer, position, paint);
        }

        public static void canvas_flush(SKCanvas canvas)
        {
            sk_canvas_flush(canvas);
        }
        
        
        static void Reverse(Span<char> str, int len)
        {
            
        }
        
        
        static int IntToString(int x, Span<char> str, int d)
        {
            
        }
 

        static void DoubleToString(double n, Span<char> res, int afterpoint)
        {

        }

        [MethodImpl(MethodImplOptions.AgressiveInlining)]
        public static SKPoint ToSKPoint(this Vector3 vec)
        {
            return new SKPoint(vec.X, vec.Y);
        }

        [MethodImpl(MethodImplOptions.AgressiveInlining)]
        public static SKPoint ToSKPoint(this Vecto2 vec)
        {
            return new SKPoint(vec.X, vec.Y);
        }
    }



    public class Chart3D
    {
        #region fields
        Matrix4x4 scale;                                 // scale to viewport (window - screen) size
        Matrix4x4 translate;
        Matrix4x4 projection;
        Matrix4x4 transform;                             // rotation, translation, scale
        Vector3 camera = new(1, 1, -1);
        
        const float X_MIN = 0.0f;                        // transform everything in [0, 1] and scale with transform matrix before .ToSKPoint(); 
        const float X_MAX = 1.0f;
        const float Y_MIN = 0.0f;
        const float Y_MAX = 1.0f;
        const float Z_MIN = 0.0f;
        const float Z_MAX = 1.0f;
        
        const int MAX_SIZE = 100_000;
        IntPtr points_buffer;
        IntPtr indices_buffer;
        IntPtr colors_buffer;
        IntPtr vertices_buffer;
        
        bool xgrid = true;
        bool ygrid = true;
        bool zgrid = true;
        bool colorbar_visible = false;
        
        SortedSet<Model3D> models = new();
        Colorbar colorbar = new(Colormap.Jet, 400, 150, 100, 50, true);

        SKPaint gridlines_paint;
        SKPaint axes_paint;
        SKPaint labels_paint;
        SKPaint title_paint;
        SKPaint colorbar_paint;
        
        internal Bounds3D? bounds;
        internal bool update = true;
        
        public Chart3D()
        {
            gridlines_paint = new();
            axes_paint = new();
            labels_paint = new();
            title_paint = new();
            colorbar_paint = new();       
            
            points_buffer = AllocHGlobal(MAX_SIZE * sizeof(SKPoint));
            indices_buffer = AllocHGlobal(MAX_SIZE * sizeof(ushort));
            colors_buffer = AllocHGlobal(MAX_SIZE * sizeof(SKColor));
            // vertices_buffer = AllocHGlobal(MAX_SIZE * sizeof(SKVertices));
        }
    
        ~Chart3D()
        {
            gridlines_paint.Dispose();
            axes_paint.Dispose();
            labels_paint.Dispose();
            title_paint.Dispose();
            colorbar_paint.Dispose();        
            
            FreeHGlobal(points_buffer);
            FreeHGlobal(indices_buffer);
            FreeHGlobal(colors_buffer);
            FreeHGlobal(vertices_buffer);
        }        

        #endregion

        #region methods
        public Chart3D AddModel3D(Model3D model)
        {
            bounds = Bounds3D.GetBounds(this.bounds, model.Bounds);
            models.Add(model);
    
            return this;
        } 
    
        void DrawGridlines(SKCanvas canvas)
        {
            var pts = new Span<SKPoint>(points_buffer.ToPointer(), MAX_SIZE);
            var dx = ...;
            var dy = ...;
            var dz = ...;
            int i = 0;    
            // xy - xz
            {
                for(float x = X_MIN + dx; x < X_MAX; x += dx, i += 4)
                {
                    var pt0 = new Vector3(x, Y_MIN, Z_MIN);
                    var pt1 = new Vector3(x, Y_MAX, Z_MIN);
                    var pt2 = new Vector3(x, Y_MIN, Z_MAX);

                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();
                    pts[i + 1] = (pt1 * transform * projection).ToSKPoint();                    
                    pts[i + 2] = (pt0 * transform * projection).ToSKPoint();
                    pts[i + 3] = (pt2 * transform * projection).ToSKPoint();  
                }
            }   
            // yx - yz
            {
                for(float y = Y_MIN + dy; y < Y_MAX; y += dy, i += 4)
                {
                    var pt0 = new Vector3(X_MIN, y, Z_MIN);
                    var pt1 = new Vector3(X_MAX, y, Z_MIN);
                    var pt2 = new Vector3(X_MIN, y, Z_MAX);

                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();
                    pts[i + 1] = (pt1 * transform * projection).ToSKPoint(); 
                    pts[i + 2] = (pt0 * transform * projection).ToSKPoint(); 
                    pts[i + 3] = (pt2 * transform * projection).ToSKPoint(); 
                }
            }
            // zx - zy
            {
                for(float z = Z_MIN + dz; z < Z_MAX; z += dz, i += 4)
                {
                    var pt0 = new Vector3(X_MIN, Y_MIN, z);
                    var pt1 = new Vector3(X_MAX, Y_MIN, z);
                    var pt2 = new Vector3(X_MIN, Y_MAX, z);
                    
                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();
                    pts[i + 1] = (pt1 * transform * projection).ToSKPoint(); 
                    pts[i + 2] = (pt0 * transform * projection).ToSKPoint(); 
                    pts[i + 3] = (pt2 * transform * projection).ToSKPoint(); 
                }
            }

            Sk.canvas_drawLines(canvas, pts[0..i], gridlines_paint);            // draw vertices as lines
        }

    
        void DrawAxes(SKCanvas canvas)
        {            
            var pt0 = new Vector3(X_MIN, Y_MIN, Z_MIN);                             // get the 4 vectors of the space O, X, Y, Z
            var ptX = new Vector3(X_MAX, Y_MIN, Z_MIN);
            var ptY = new Vector3(X_MIN, Y_MAX, Z_MIN);
            var ptZ = new Vector3(X_MIN, Y_MIN, Z_MAX);
            
            var pts = new Span<SKPoint>(points_buffer.ToPointer(), MAX_SIZE);
            pts[0] = (pt0 * transform * projection).ToSKPoint();
            pts[1] = (ptX * transform * projection).ToSKPoint();
            pts[2] = (pt0 * transform * projection).ToSKPoint();
            pts[3] = (ptY * transform * projection).ToSKPoint();
            pts[4] = (pt0 * transform * projection).ToSKPoint();
            pts[5] = (ptZ * transform * projection).ToSKPoint();
            
            Sk.canvas_drawLines(canvas, pts[0..6], axes_paint);    // draw vertices as lines
        }
    
        void DrawTicks(SKCanvas canvas)
        {    
            var pts = new Span<SKPoint>(points_buffer.ToPointer(), MAX_SIZE);
            var dx = ...;
            var dy = ...;
            var dz = ...;
            
            int i = 0;
            // draw X ticks
            {
                for(float x = X_MIN + dx; x < X_MAX; x += dx, i += 2)
                {
                    var pt0 = new Vector3(x, Y_MIN, Z_MIN);
                    var pt1 = new Vector3(x, Y_MIN - dy, Z_MIN);                    
                    var pt2 = new Vector3(x - dx/2.0f, Y_MIN - dy, Z_MIN);
    
                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();                          
                    pts[i + 1] = (pt1 * transform * projection).ToSKPoint();

                    Sk.canvas_drawDouble(canvas, bounds.InterpX(x), (pt2 * transform * projection).ToSKPoint(), labels_paint);            // [DllImport(libname)] calls the original function, not the wrapped one...
                }                
            }
            // draw Y ticks
            {                
                for(double y = Y_MIN + dy; y < Y_MAX; y += dy, i += 2)
                {
                    var pt0 = new Vector3(X_MIN, y, Z_MIN);
                    var pt1 = new Vector3(X_MIN - dx, y, Z_MIN);                    
                    var pt2 = new Vector3(X_MIN - dx, y - dy/2.0f, Z_MIN);                    
    
                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();                          
                    pts[i + 1] = (pt1 * transform * projection).ToSKPoint();      
                    
                    Sk.canvas_drawDouble(canvas, bounds.InterpY(y), (pt2 * transform * projection).ToSKPoint(), labels_paint);                    
                }                
            }   
            // draw Z ticks
            {                
                for(double z = Z_MIN + dz; z < Z_MAX; z += dz, i += 2)
                {
                    var pt0 = new Vector3(X_MIN, Y_MIN, z);
                    var pt1 = new Vector3(X_MIN - dx, Y_MIN - dy, z);
                    var pt2 = new Vector3(X_MIN - dx, Y_MIN - dy, z - dz/2.0f);
                        
                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();                          
                    pts[i + 1] = (pt1 * transform * projection).ToSKPoint();                          

                    Sk.canvas_drawDouble(canvas, bounds.InterpZ(z), (pt2 * transform * projection).ToSKPoint(), labels_paint);
                }                
            }

            Sk.canvas_drawLines(canvas, pts[0..i], axes_paint);    // draw vertices as lines
        }


        #region draw_models
        void DrawSurface(SKCanvas canvas, Model3D model)
        {   
            var width = model.Width.Value;
            var height = model.Height.Value;
            var zmin = model.Bounds.Zmin;
            var zmax = model.Bounds.Zmax;
            var pts = model.data;
            var colormap = model.Colormap;
            var vertices = new Span<SKPoint>(points_buffer.ToPointer(), MAX_SIZE);
            var indices  = new Span<SKPoint>(indices_buffer.ToPointer(), MAX_SIZE);
            var colors   = new Span<SKPoint>(colors_buffer.ToPointer(), MAX_SIZE);            

            // REPLACE WITH PROPER OCCULATION ALGORITHM !!!
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

                for (int v = 0, i = 0, c = 0; i < data.Length; v += 4, i += 6, c += 4)
                {
                    //int jj = j;
                    //if (elevation < 0) jj = height - 2 - j;
                    int jj = elevation < 0 ? height - 2 - j : j;

                    vertices[v + 0] = (pts[(ii + 0) * (jj + 0)] * transform * projection).ToSKPoint();
                    vertices[v + 1] = (pts[(ii + 0) * (jj + 1)] * transform * projection).ToSKPoint();
                    vertices[v + 2] = (pts[(ii + 1) * (jj + 1)] * transform * projection).ToSKPoint();
                    vertices[v + 3] = (pts[(ii + 1) * (jj + 0)] * transform * projection).ToSKPoint();

                    colors[c + 0] = Colormaps.GetColor(colormap, pts[(ii + 0) * (jj + 0)].Z, zmin, zmax);
                    colors[c + 1] = Colormaps.GetColor(colormap, pts[(ii + 0) * (jj + 1)].Z, zmin, zmax);
                    colors[c + 2] = Colormaps.GetColor(colormap, pts[(ii + 1) * (jj + 1)].Z, zmin, zmax);
                    colors[c + 3] = Colormaps.GetColor(colormap, pts[(ii + 1) * (jj + 0)].Z, zmin, zmax);
                    
                    indices[i + 0] = (ushort)(v + 0);
                    indices[i + 1] = (ushort)(v + 1);
                    indices[i + 2] = (ushort)(v + 3);
                    indices[i + 3] = (ushort)(v + 3);
                    indices[i + 4] = (ushort)(v + 1);
                    indices[i + 5] = (ushort)(v + 2);
                }
            }
            
            Sk.write_vertices(ref vertices_buffer, vertices[0..v], colors[0..c], indices[0..i]);
            Sk.canvas_drawVertices(canvas, vertices_buffer);
        }

        void DrawLine3D(SKCanvas canvas, ReadOnlySpan<Vector3> data)
        {
            var pts = new Span<SKPoint>(points_buffer.ToPointer(), MAX_SIZE);
            for(int i = 0, j = 1; j < data.Length; i += 2, j += 1)
            {
                pts[i + 0] = (data[j - 1] * transform * projection).ToSKPoint();
                pts[i + 1] = (data[j - 0] * transform * projection).ToSKPoint();
            }
            
            Sk.canvas_drawLines(canvas, pts[0..i], axes_paint);    // draw vertices as lines
        }
        
        void DrawPoints(SKCanvas canvas, ReadOnlySpan<Vector3> data)
        {
            var pts = new Span<SKPoint>(points_buffer.ToPointer(), MAX_SIZE);
            for(int i = 0; i < data.Length; i += 1)
            {
                pts[i] = (data[i] * transform * projection).ToSKPoint();
            }
            
            Sk.canvas_drawPoints(canvas, pts[0..i], axes_paint);    // draw vertices as points       
        }
        #endregion
                 
    
        void DrawModels(SKCanvas canvas)
        {          
            foreach(var model in models)
            {        
                switch(model.Mode)
                {
                    case Model3DMode.Line: DrawLine3D(canvas, model.Data); break;
                    case Model3DMode.Points: DrawPoints(canvas, model.Data); break;
                    case Model3DMode.Surface: DrawSurface(canvas, model.Data); break;
                } 
            }
        }

        void DrawTitle(SKCanvas canvas)
        {            
            var x = (X_MAX - X_MIN) / 2.0f;
            var y = (Y_MAX - Y_MIN) / 2.0f;
            var z = (Z_MAX - Z_MIN) / 2.0f;
            var dx = ...;
            var dy = ...;
            var dz = ...;
            var title_position = new Vector3(x, y, z);
            var xlabel_position = new Vector3(x, Y_MIN - 2.0f * dy, Z_MIN);
            var ylabel_position = new Vector3(X_MIN - 2.0f * dx, y, Z_MIN);
            var zlabel_position = new Vector3(X_MIN - 2.0f * dx, Y_MIN - 2.0f * dy, z);

            if(Title != null) Sk.canvas_drawText(canvas, Title, (title_position * transform * projection).ToSKPoint(), title_paint);
            if(XLabel != null) Sk.canvas_drawText(canvas, XLabel, (xlabel_position * transform * projection).ToSKPoint(), title_paint);
            if(YLabel != null) Sk.canvas_drawText(canvas, YLabel, (ylabel_position * transform * projection).ToSKPoint(), title_paint);
            if(ZLabel != null) Sk.canvas_drawText(canvas, Zlabel, (zlabel_position * transform * projection).ToSKPoint(), title_paint);
        }

        void DrawColorbar(SKCanvas canvas)
        {
            colorbar.Draw(canvas, colorbar_paint);
        }
    
        public void Draw(SKCanvas canvas)
        {
            if(!update) return;

            canvas.Clear(SKColors.White);
        
            DrawGridlines(canvas);
            DrawAxes(canvas);
            DrawTicks(canvas);
            DrawModels(canvas);
            DrawTitle(canvas);
            DrawColorbar(canvas);
            Sk.canvas_flush(canvas);

            update = false;
        }       

        SKPoint ToSK(Vector3 vec)
        {
            var pt = vec * transform * projection;

            return new SKPoint(pt.X, pt.Y);
        }
        #endregion

        #region exports
        
        #endregion

        #region properties
        public string? Title { get; set; }
        public string? XLabel { get; set; }
        public string? YLabel { get; set; }
        public string? ZLabel { get; set; }
    
        public SKPaint PaintGridlines => gridlines_paint;
        public SKPaint PaintAxes      => axes_paint;
        public SKPaint PaintLabels    => labels_paint;
        public SKPaint PaintTitle     => title_paint;
        public SKPaint PaintColorbar  => colorbar_paint;

        public Matrix4x4 Transform
        {
            get => transform;
            set
            {
                transform = value;
                update = true;
            }        
        }
    
        public Matrix4x4 Projection
        {
            get => projection;
            set
            {
                projection = value;
                update = true;            
            }
        }
        
        public Matrix4x4 Scale
        {
            get => scale;
            set
            {
                scale = value;
                update = true;
            }
        }
        #endregion
    }
}
