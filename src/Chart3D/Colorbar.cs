using System;
using System.Numerics;
using System.Runtime.InteropServices;
using static System.Runtime.InteropServices.Marshal;

namespace SKCharts
{
    public class Colorbar
    {
        Matrix3x2 scale;
        Matrix3x2 translate;
        Matrix3x2 transform;               
        Colormap colormap;
        Chart3D? parent;
        
        const int MAP_SIZE = Colormaps.MAP_SIZE;
        IntPtr vertices_buffer;
        IntPtr colors_buffer;
        IntPtr indices_buffer;

        const float X_MIN = 0.0f;
        const float X_MAX = 1.0f;
        const float Y_MIN = 0.0f;
        const float Y_MAX = 1.0f;

        public Colorbar(Colormap colormap, Chart3D? parent)
        {
            vertices_buffer = AllocHGlobal(sizeof(SKPoint) * MAP_SIZE);
            colors_buffer = AllocHGlobal(sizeof(SKColor) * MAP_SIZE);
            indices_buffer = AllocHGlobal(sizeof(ushort) * MAP_SIZE);

            this.parent = parent;
        }

        ~Colorbar()
        {
            FreeHGlobal(vertices_buffer);
            FreeHGlobal(colors_buffer);
            FreeHGlobal(indices_buffer);
        }

        public Colormap Colormap
        {
            get => colormap;
            set
            {
                if(colormap != value)
                {
                    colormap = value;
                    parent?.update = true;
                }
            }
        }   

        public Matrix3x2 Scale
        {
            get => scale;
            set => scale = value;
        }

        public Matrix3x2 Translate
        {
            get => translate;
            set => translate = value;
        }

        public void Draw(SKCanvas canvas, double zmin, double zmax, SKPaint colorbar_paint)
        {
            // colorbar
            {
                var y = Y_MIN;
                var dy = (Y_MAX - Y_MIN) / MAP_SIZE;
                var z = zmin;
                var dz = (zmax - zmin) / MAP_SIZE;
                var vertices = new Span<SKPoint>(vertices_buffer.ToPointer(), MAP_SIZE);
                var colors   = new Span<SKColor>(colorbar_paint.ToPointer(), MAP_SIZE);
                var indices  = new Spane<ushort>(indices_buffer.ToPointer(), MAP_SIZE);
            
                for (int i = 0, c = 0, v = 0, n = 0; n < MAP_SIZE; m += 4, i += 6, c += 4, n += 1, y += dy, z += dz)
                {
                    var pt0 = new Vector2(X_MIN, y);
                    var pt1 = new Vector2(X_MAX, y);
                    var pt2 = new Vector2(X_MAX, y + dy);
                    var pt3 = new Vector2(X_MIN, y + dy);
                    
                    vertices[v + 0] = (pt0 * scale * translate).ToSKPoint();
                    vertices[v + 1] = (pt1 * scale * translate).ToSKPoint();
                    vertices[v + 2] = (pt2 * scale * translate).ToSKPoint();
                    vertices[v + 3] = (pt3 * scale * translate).ToSKPoint();

                    indices[i + 0] = (ushort)(v + 0);
                    indices[i + 1] = (ushort)(v + 1);
                    indices[i + 2] = (ushort)(v + 3);
                    indices[i + 3] = (ushort)(v + 3);
                    indices[i + 4] = (ushort)(v + 1);
                    indices[i + 5] = (ushort)(v + 2);

                    var color = Colormaps.GetColor(colormap, z, zmin, zmax);
                    colors[c + 0] = color;
                    colors[c + 1] = color;
                    colors[c + 2] = color;
                    colors[c + 3] = color;                
                }

                Sk.canvas_drawVertices(canvas, vertices[0..v], colors[0..c], indices[0..i], colorbar_paint);
            }

            // frame and labels
            {
                var y = Y_MIN;
                var dy = (Y_MAX - Y_MIN) / MAP_SIZE;
                var z = zmin;
                var dz = (zmax - zmin) / MAP_SIZE;
                var pts = new Span<SKPoint>(vertices_buffer.ToPointer(), MAP_SIZE);
                var i = 0;
                // draw rect
                {
                    var pt0 = new Vector2(X_MIN, Y_MIN);
                    var pt1 = new Vector2(X_MAX, Y_MIN);
                    var pt2 = new Vector2(X_MAX, Y_MAX);
                    var pt3 = new Vector2(X_MIN, Y_MAX);

                    pts[i + 0] = (pt0 * scale * translate).ToSKPoint();
                    pts[i + 1] = (pt1 * scale * translate).ToSKPoint();
                    pts[i + 2] = (pt1 * scale * translate).ToSKPoint();
                    pts[i + 3] = (pt2 * scale * translate).ToSKPoint();
                    pts[i + 4] = (pt2 * scale * translate).ToSKPoint();
                    pts[i + 5] = (pt3 * scale * translate).ToSKPoint();
                    pts[i + 6] = (pt3 * scale * translate).ToSKPoint();
                    pts[i + 7] = (pt0 * scale * translate).ToSKPoint();

                    i += 7;
                }
                
                var dx = (X_MAX - X_MIN) / 4.0f;
                // draw ticks
                for(int n = 0; n < MAP_SIZE; i += 4, n += 1, y += dy, z += dz)
                {
                    var pt0 = new Vector2(X_MIN, y);
                    var pt1 = new Vector2(X_MIN - dx, y);
                    
                    var pt2 = new Vector2(X_MAX, y);
                    var pt3 = new Vector2(X_MAX + dx, y);

                    var label_position = new Vector2(X_MIN - dx * 10.0f, y - dy * 0.25f);

                    pts[i + 0] = (pt0 * scale * translate).ToSKPoint();
                    pts[i + 1] = (pt1 * scale * translate).ToSKPoint();
                    pts[i + 2] = (pt2 * scale * translate).ToSKPoint();
                    pts[i + 3] = (pt3 * scale * translate).ToSKPoint();

                    Sk.canvas_drawDouble(canvas, z, (label_position * scale * translate).ToSKPoint());
                }

                Sk.canvas_drawLines(canvas, pts[0..i], colorbar_paint);
            }
        }
    }
}
