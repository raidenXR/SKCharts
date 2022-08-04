using System;

namespace SKCharts
{
    public enum Model3DMode
    {
        Line,
        Points,
        Surface
    }
    
    
    public class Model3D : IComparable<Model3D>
    {
        Vector3[] data;
        Bounds3D bounds;
        Model3DMode mode;    
        SKColor? color;
        Colormap? colormap;

        public Model3D(Vector3[] data, Model3DMode mode, SKColor? color, Colormap? colormap)
        {
            this.data = data;
            this.bounds = Bounds3D.GetBounds(data);
            this.mode = mode;
            this.color = color;
            this.colormap = colormap;
        }
    
        public ReadOnlySpan<Vector3> Data => data.AsSpan();
        public Span<Vector3> DataMutable  => data.AsSpan();
        public Model3DMode Mode   => mode;
        public Bounds3D Bounds    => bounds;
        public SKColor Color      => (color != null) ? color.Value : throw new NullException();
        public Colormap Colormap  => (colormap != null) ? colormap.Value : throw new NullException();
    
        public int CompareTo(Model3D other)
        {
            return mode.CompareTo(other.mode);
        }     

        public void UpdateBounds(Chart3D? parent)
        {
            bounds = Bounds3D.GetBounds(data);
            parent?.bounds = Bounds3D.GetBounds(this.bounds, parent.bounds);
            parent?.update = true;
        }
    
        public void CopyDataRawF(Chart3D? parent, ReadOnlySpan<float> buffer, int count, int offset_x, int offset_y, int offset_z, int stride)
        {
            for(int i = 0; i < count; i += stride)
            {
                data[i].X = buffer[i + offset_x];
                data[i].Y = buffer[i + offset_y];
                data[i].Z = buffer[i + offset_z];
            }
            
            bounds = Bounds3D.GetBounds(data);
            parent?.bounds = Bounds3D.GetBounds(this.bounds, parent.bounds);
            parent?.update = true;
        }

        #region exports
        [DllExport("CopyData", CallingConvention = CallingConvention.Cdecl)]
        public static void copy_data_rawF(Model3D model, Chart3D parent, float[] buffer, int start, int count, int offset_x, int offset_y, int offset_z, int stride)
        {
            model.CopyDataRawF(parent, buffer.AsSpan()[start..], count, offset_x, offset_y, offset_z, stride);
        }
        #endregion
    }
}
