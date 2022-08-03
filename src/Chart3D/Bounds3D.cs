

namespace SKCharts
{
    struct Bounds3D
    {
        public double Xmin;
        public double Xmax;
        public double Ymin;
        public double Ymax;
        public double Zmin;
        public double Zmax;
    
        public static Bounds3D GetBounds(Bounds3D? A, Bounds3D? B)
        {
            if(A == null && B != null) return B;
            if(A != null && B == null) return A;
    
            Bounds3D a = A.Value;
            Bounds3D b = B.Value;
        
            double xmin = Math.Min(a.Xmin, b.Xmin);
            double xmax = Math.Max(a.Xmax, b.Xmax);
            double ymin = Math.Min(a.Ymin, b.Ymin);
            double ymax = Math.Max(a.Ymax, b.Ymax);
            double zmin = Math.Min(a.Zmin, b.Zmin);
            double zmax = Math.Max(a.Zmax, b.Zmax);
    
            return new Bounds3D
            {
                Xmin = xmin,
                Xmax = xmax,
                Ymin = ymin,
                Ymax = ymax,
                Zmin = zmin,
                Zmax = zmax
            }
        }
    
        public static Bounds3D GetBounds(ReadOnlySpan<Vector3D> data)
        {
            double xmin = double.MinValue
            double xmax = double.MaxValue
            double ymin = double.MinValue
            double ymax = double.MaxValue
            double zmin = double.MinValue
            double zmax = double.MaxValue
    
            foreach(var pt in data)
            {
                xmin = Math.Min(xmin, pt.Xmin);
                xmax = Math.Max(xmax, pt.Xmax);
                ymin = Math.Min(ymin, pt.Ymin);
                ymax = Math.Max(ymax, pt.Ymax);
                zmin = Math.Min(zmin, pt.Zmin);
                zmax = Math.Max(zmax, pt.Zmax);    
            }
    
            return new Bounds3D
            {
                Xmin = xmin,
                Xmax = xmax,
                Ymin = ymin,
                Ymax = ymax,
                Zmin = zmin,
                Zmax = zmax
            }   
        }
    }
    
}
