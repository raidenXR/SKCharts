

namespace SKCharts
{
    public class Colorbar
    {
        const int MAP_SIZE = Colormaps.MAP_SIZE;
    
        SKPoint[] vertices = new SKPoint[MAP_SIZE * 4];   // colorbar
        SKColor[] colors = new SKColor[MAP_SIZE * 4];
        ushort[] indices = new ushort[MAP_SIZE * 6];
        List<string> labels = new List<string>(10);
        List<SKPoint> labelsPositions = new List<SKPoint>(10);
        SKRect border;
        bool drawBoder;
        Colormap colormap;

        int _left, _height;
        double _zmin, _zmax;
        float _fontSize;

        public ColorMap ColorMap
        {
            get => colormap;
            set
            {
                if(colormap != value)
                {
                    colormap = value;
                    SetColorbar(_left, _height, _zmin, _zmax);
                }
            }
        }
   

        public ColorBar(Colormap colormap, int left, int height, double zmin, double zmax, float fontSize, bool drawBorder)
        {
            float x = left;
            float y = height / 2f - 150;
            float dx = 20;
            border = new SKRect(x - 1, y - 1, x + 1 + dx, 400 - y);
            this.drawBoder = drawBorder;
            this.colormap = colormap;

            Restore(left, height, zmin, zmax, fontSize);
        }

        void SetColorbar(int left, int height, double zmin, double zmax)
        {
            float x = left;
            float y = height / 2f - 150;
            float dy = (height - 100f) / MAP_SIZE; // 64f;
            float dx = 20;
            double zvalue = zmax;

            for (int i = 0, c = 0, m = 0; m < vertices.Length; m += 4, i += 6, c += 4)
            {
                vertices[m + 0] = new SKPoint(x, y);
                vertices[m + 1] = new SKPoint(x + dx, y);
                vertices[m + 2] = new SKPoint(x + dx, y + dy);
                vertices[m + 3] = new SKPoint(x, y + dy);

                indices[i + 0] = (ushort)(m + 0);
                indices[i + 1] = (ushort)(m + 1);
                indices[i + 2] = (ushort)(m + 3);
                indices[i + 3] = (ushort)(m + 3);
                indices[i + 4] = (ushort)(m + 1);
                indices[i + 5] = (ushort)(m + 2);

                var color = Colormaps.GetColor(colormap, zvalue, zmin, zmax);
                colors[c + 0] = color;
                colors[c + 1] = color;
                colors[c + 2] = color;
                colors[c + 3] = color;

                zvalue -= (zmax - zmin) / 64d;
                y += dy;
            }
        }

        public void Restore(int left, int height, double zmin, double zmax, float fontSize)
        {
            float x = left;
            float y = height / 2f - 150;
            float dy = (height - 100f) / 64f;
            float dx = 20;
            double value = zmax;

            SetColorbar(left, height, zmin, zmax);

            float z = height / 2f - 150;
            float dz = (height - 100f) / 5f;
            labels.Clear();
            labelsPositions.Clear();

            for (double d = zmax; d > zmin; d -= (zmax - zmin) / 5d)
            {
                labels.Add(d.ToString("0.00"));
                labelsPositions.Add(new SKPoint(x + dx + 10, z + fontSize / 2f));
                z += dz;
            }


            _left = left;
            _height = height;
            _zmin = zmin;
            _zmax = zmax;
            _fontSize = fontSize;
        }

        public void Draw(SKCanvas canvas, SKPaint paint)
        {
            if (drawBoder) canvas.DrawRect(border, paint);
            canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, colors, indices, paint);                        

            for (int i = 0; i < labels.Count; i++)
            {
                canvas.DrawText(labels[i], labelsPositions[i], paint);
                if (drawBoder)
                {
                    SKPoint p0 = labelsPositions[i];
                    p0.X -= 10;
                    SKPoint p1 = new SKPoint(p0.X + 5, p0.Y);
                    canvas.DrawLine(p0, p1, paint);
                }
            }
        }
    }
}
