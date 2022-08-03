

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

        public event OnDataChanged;       // ??? when bounds changee update Chart bounds ???

        public Model3D(Vector3[] data, Model3DMode mode, SKColor? color, Colormap? colormap)
        {
            this.data = data;
            this.bounds = Bounds3D.GetBounds(data);
            this.mode = mode;
            this.color = color;
            this.colormap = colormap;
        }
    
        public ReadOnlySpan<Vector3> Data => data.AsSpan();
        public Model3DMode Mode   => mode;
        public Bounds3D Bounds    => bounds;
        public SKColor Color      => (color != null) ? color.Value : throw new NullException();
        public Colormap Colormap  => (colormap != null) ? colormap.Value : throw new NullException();
    
        public int CompareTo(Model3DMode other)
        {
            return mode.CompareTo(other.mode);
        }
    }
}
