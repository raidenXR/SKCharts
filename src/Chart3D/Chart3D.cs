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
        Matrix3x3 transform;
        Matrix3x3 projection;            
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
            int i = 0;
            // xz
            {
                for(double x = bounds.Xmin + dx; x < bounds.Xmax; x += dx,i += 2)
                {
                    var pt0 = new Vector3(x, bounds.Ymin, bounds.Zmin);
                    var pt1 = new Vector3(x, bounds.Ymin, bounds.Zmax);

                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();
                    pts[i + 1] = (pt1 * transform * projection).ToSKPoint();                     
                }
            }

            // yz
            {
                for(double y = bounds.Xmin + dx; x < bounds.Xmax; x += dx,i += 2)
                {
                    var pt0 = new Vector3(x, bounds.Ymin, bounds.Zmin);
                    var pt1 = new Vector3(x, bounds.Ymin, bounds.Zmax);

                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();
                    pts[i + 1] = (pt1 * transform * projection).ToSKPoint();                     
                }
            }

            // xy
            {
                for(double x = bounds.Xmin + dx; x < bounds.Xmax; x += dx,i += 2)
                {
                    var pt0 = new Vector3(x, bounds.Ymin, bounds.Zmin);
                    var pt1 = new Vector3(x, bounds.Ymin, bounds.Zmax);

                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();
                    pts[i + 1] = (pt1 * transform * projection).ToSKPoint();                     
                }
            }

            Sk.canvas_drawLines(pts.AsSpan(0, i), null, gridlines_paint);    // draw vertices as lines
        }

    
        void DrawAxes(SKCanvas canvas)
        {            
            var pt0 = new Vector3(bounds.Xmin, bounds.Ymin, bounds.Zmin);   // get the 4 vectors of the space O, X, Y, Z
            var ptX = new Vector3(bounds.Xmax, bounds.Ymin, bounds.Zmin);
            var ptY = new Vector3(bounds.Xmin, bounds.Ymax, bounds.Zmin);
            var ptZ = new Vector3(bounds.Xmin, bounds.Ymin, bounds.Zmax);
            
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
            double offset_X = (bouds.Ymax - bound.Ymin) / 10.0;  
            double offset_Y = (bounds.Xmax - bounds.Xmin) / 10.0;
            
            int i = 0;
            // draw X ticks
            {
                for(double x = bounds.Xmin + dx; x < bounds.Xmax; x += dx, i += 2)
                {
                    var pt0 = new Vector3(x, bounds.Ymin, bounds.Zmin);
                    var pt1 = new Vector3(x, bounds.Ymin - offset_X, bounds.Zmin);                    
                    var pt2 = new Vector3(x - dx/2.0, bounds.Ymin - offset_X, bounds.Zmin);
    
                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();                          
                    pts[i + 1] = (pt0 * transform * projection).ToSKPoint()

                    Sk.canvas_drawtext(label_buffer, (pt2 * transform * projection).ToSKPoint(), labels_paint);            // [DllImport(libname)] calls the original function, not the wrapped one...
                }                
            }
            // draw Y ticks
            {                
                for(double y = bounds.Ymin + dy; y < bounds.Ymax; y += dy, i += 2)
                {
                    var pt0 = new Vector3(bounds.Xmin, y, bounds.Zmin);
                    var pt1 = new Vector3(bounds.Xmin - offset_Y, y, bounds.Zmin);                    
                    var pt2 = new Vector3(bounds.Xmin - offset_Y, y - dy/2.0, bounds.Zmin);                    
    
                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();                          
                    pts[i + 1] = (pt0 * transform * projection).ToSKPoint();      

                    Sk.canvas_drawtext(label_buffer, (pt2 * transform * projection).ToSKPoint(), labels_paint);                    
                }                
            }   
            // draw Z ticks
            {                
                for(double z = bounds.Zmin + dz; z < bounds.Zmax; z += dz, i += 2)
                {
                    var pt0 = new Vector3(bounds.Xmin, bounds.Ymin, z);
                    var pt1 = new Vector3(bounds.Xmin - offset_X, bounds.Ymin - offset_Y, z);
                    var pt2 = new Vector3(bounds.Xmin - offset_X, bounds.Ymin - offset_Y, z - dz/2.0);
                        
                    pts[i + 0] = (pt0 * transform * projection).ToSKPoint();                          
                    pts[i + 1] = (pt0 * transform * projection).ToSKPoint();                          

                    Sk.canvas_drawtext(label_buffer, (pt2 * transform * projection).ToSKPoint(), labels_paint);
                }                
            }

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

                for (int i = 0, v = 0, c = 0; i < data.Lenght; i ++ i, v += 4, l += 6, c += 4)
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
            for(int i = 0; 0 < data.Length; i += 1)
            {
                pts[i] = (data[i] * transform * projection).ToSKPoint();
            }
            
            Sk.canvas_drawLines(pts.AsSpan(0, i), null, axes_paint);    // draw vertices as lines            c
        }
        
        void DrawPoints(SKCanvas canvas, ReadOnlySpan<Vector3> data)
        {
            var pts = points_buffer;
            for(int i = 0; 0 < data.Length; i += 1)
            {
                pts[i] = (data[i] * transform * projection).ToSKPoint();
            }
            
            Sk.canvas_drawPoints(pts.AsSpan(0, i), null, axes_paint);    // draw vertices as lines        
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
            if(Title == null) return;
            
            var x = (bounds.Xmax - bounds.Xmin) / 2.0;
            var y = (bounds.Ymax - bounds.Ymin) / 2.0;
            var z = (bounds.Zmax - bounds.Zmin) / 2.0;
            var title_position = new Vector3(x, y, z);

            Sk.canvas_drawText(Title, (SKPoint)title_position, title_paint);
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
            sk_canvas_flush(canvas);

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

        public Matrix3x3 Transform
        {
            get => transform;
            set
            {
                transform = value;
                update = true;
            }        
        }
    
        public Matrix3x3 Projection
        {
            get => projection;
            set
            {
                projection = value;
                update = true;            
            }
        }
        #endregion
    }
}
