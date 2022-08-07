using System;
using System.Numerics;


namespace SKCharts
{
    static unsafe class Sk
    {
        const string libname = "SkiaSharp.so";

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

        public static void canvas_drawPoints(SKCanvas canvas, )
        {
            
        }

        public static void canvas_drawLines(SKCanvas canvas, )
        {
            
        }

        public static void canvas_drawVertices(SKCanvas canvas, SKVertexMode mode, Span<SKPoint> positions, Span<SKPoint>? textures, Span<SKColor> colors, Span<ushort> indices)
        {
            var _textures = (textures.HasValue) ? textures.Value : null; 
            var vertices = sk_vertices_make_copy((int)mode, positions.Lenght, positions, _textures, colors, indices);
        }

        public static void canvas_drawText(SKCanvas canvas, )
        {
            
        }

        public static void canvas_flush(SKCanvas canvas)
        {
            sk_canvas_flush(canvas);
        }
    }



    public class Chart3D
    {
        #region fields
        Matrix4x4 transform;                             // rotation, translation, scale
        Matrix4x4 projection;
        Matrix4x4 scale;                                 // scale to viewport (window - screen) size
        Vector3 camera = new(1, 1, -1);
        
        const float X_MIN = 0.0f;                        // transform everything in [0, 1] and scale with transform matrix before .ToSKPoint(); 
        const float X_MAX = 1.0f;
        const float Y_MIN = 0.0f;
        const float Y_MAX = 1.0f;
        const float Z_MIN = 0.0f;
        const float Z_MAX = 1.0f;
        
        const int MAX_SIZE = 100_000;
        SKPoint[] points_buffer = new SKPoint[MAX_SIZE];
        ushort[] indices_buffer = new ushort[MAX_SIZE];
        SKColor[] colors_buffer = new SKColor[MAX_SIZE];
        
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
        }
    
        ~Chart3D()
        {
            gridlines_paint.Dispose();
            axes_paint.Dispose();
            labels_paint.Dispose();
            title_paint.Dispose();
            colorbar_paint.Dispose();            
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
            var pts = points_buffer;
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

            Sk.canvas_drawLines(pts.AsSpan(0, i), null, gridlines_paint);            // draw vertices as lines
        }

    
        void DrawAxes(SKCanvas canvas)
        {            
            var pt0 = new Vector3(X_MIN, Y_MIN, Z_MIN);                             // get the 4 vectors of the space O, X, Y, Z
            var ptX = new Vector3(X_MAX, Y_MIN, Z_MIN);
            var ptY = new Vector3(X_MIN, Y_MAX, Z_MIN);
            var ptZ = new Vector3(X_MIN, Y_MIN, Z_MAX);
            
            var pts = points_buffer;
            pts[0] = (pt0 * transform * projection).ToSKPoint();
            pts[1] = (ptX * transform * projection).ToSKPoint();
            pts[2] = (pt0 * transform * projection).ToSKPoint();
            pts[3] = (ptY * transform * projection).ToSKPoint();
            pts[4] = (pt0 * transform * projection).ToSKPoint();
            pts[5] = (ptZ * transform * projection).ToSKPoint();
            
            Sk.canvas_drawLines(pts.AsSpan(0, 6), null, axes_paint);    // draw vertices as lines
        }
    
        void DrawTicks(SKCanvas canvas)
        {    
            var pts = points_buffer;
            var label_buffer = stackalloc char[64];              // buffer to write on labels...
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

                    labels_buffer.AsString();                                   // ReadOnlySpan<char>["as string"]
                    Sk.canvas_drawtext(label_buffer, (pt2 * transform * projection).ToSKPoint(), labels_paint);            // [DllImport(libname)] calls the original function, not the wrapped one...
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

                    labels_buffer.AsString();                                   // ReadOnlySpan<char>["as string"]
                    Sk.canvas_drawtext(label_buffer, (pt2 * transform * projection).ToSKPoint(), labels_paint);                    
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

                    labels_buffer.AsString();                                   // ReadOnlySpan<char>["as string"]
                    Sk.canvas_drawtext(label_buffer, (pt2 * transform * projection).ToSKPoint(), labels_paint);
                }                
            }

            labels_buffer.AsString();                                   // ReadOnlySpan<char>["as string"]
            Sk.canvas_drawLines(pts.AsSpan(0, i), null, axes_paint);    // draw vertices as lines
        }


        #region draw_models
        void DrawSurface(SKCanvas canvas, Model3D model)
        {   
            var pts = model.data;
            var colormap = model.Colormap;
            var vertices = points_buffer;
            var indices = indices_buffer;
            var colors = colors_buffer;
            var zmin = model.Bounds.Zmin;
            var zmax = model.Bounds.Zmax;

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

                for (int v = 0, l = 0, c = 0; i < data.Length; v += 4, l += 6, c += 4)
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
                    
                    indices[l + 0] = (ushort)(v + 0);
                    indices[l + 1] = (ushort)(v + 1);
                    indices[l + 2] = (ushort)(v + 3);
                    indices[l + 3] = (ushort)(v + 3);
                    indices[l + 4] = (ushort)(v + 1);
                    indices[l + 5] = (ushort)(v + 2);
                }
            }
        }

        void DrawLine3D(SKCanvas canvas, ReadOnlySpan<Vector3> data)
        {
            var pts = points_buffer;
            for(int i = 0, j = 1; j < data.Length; i += 2, j += 1)
            {
                pts[i + 0] = (data[j - 1] * transform * projection).ToSKPoint();
                pts[i + 1] = (data[j - 0] * transform * projection).ToSKPoint();
            }
            
            Sk.canvas_drawLines(pts.AsSpan(0, i), null, axes_paint);    // draw vertices as lines
        }
        
        void DrawPoints(SKCanvas canvas, ReadOnlySpan<Vector3> data)
        {
            var pts = points_buffer;
            for(int i = 0; i < data.Length; i += 1)
            {
                pts[i] = (data[i] * transform * projection).ToSKPoint();
            }
            
            Sk.canvas_drawPoints(pts.AsSpan(0, i), null, axes_paint);    // draw vertices as points       
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

            if(Title != null) Sk.canvas_drawText(Title, (title_position * transform * projection).ToSKPoint(), title_paint);
            if(XLabel != null) Sk.canvas_drawText(XLabel, (xlabel_position * transform * projection).ToSKPoint(), title_paint);
            if(YLabel != null) Sk.canvas_drawText(YLabel, (ylabel_position * transform * projection).ToSKPoint(), title_paint);
            if(ZLabel != null) Sk.canvas_drawText(Zlabel, (zlabel_position * transform * projection).ToSKPoint(), title_paint);
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
