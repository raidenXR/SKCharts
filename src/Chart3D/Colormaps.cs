using System;

namespace SKCharts
{
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
        public const int MAP_SIZE = 64;
    
        SKColorp[] spring = new int[MAP_SIZE];    
        SKColorp[] summer = new int[MAP_SIZE];    
        SKColorp[] autumn = new int[MAP_SIZE];    
        SKColorp[] winter = new int[MAP_SIZE];    
        SKColorp[] gray   = new int[MAP_SIZE];    
        SKColorp[] hot    = new int[MAP_SIZE];    
        SKColorp[] cool   = new int[MAP_SIZE];    
        SKColorp[] jet    = new int[MAP_SIZE];    
    
    
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
    

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static SKColor GetColor(Colormap colormap, double zvalue, double zmin, double zmax)
        {
            int lerp = (int)((double)MAP_SIZE * ((zvalue - zmin) / (zmax - zmin)));
    
            return colormap switch
            {
                Colormap.Spring => spring[lerp],            
                Colormap.Summer => summer[lerp],            
                Colormap.Autumn => autumn[lerp],            
                Colormap.Winter => winter[lerp],            
                Colormap.Gray   => gray[lerp],            
                Colormap.Hot    => hot[lerp],            
                Colormap.Cool   => cool[lerp],            
                Colormap.Jet    => jet[lerp],            
                _ => throw new ArgumentException()
            };
        }
    
        static MakeSpring() void
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
    
        static MakeSummer() void
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
    
        static MakeAutumn() void
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
    
        static MakeWinter() void
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
    
        static MakeGray() void
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
    
        static MakeHot() void
        {
            for(int i = 0; i < MAP_SIZE; i++)
            {                
                double lerp = (double)i / (double)MAP_SIZE;
                int n1 = (int)(3.0 * MAP_SIZE / 8.0);
                int i = (int)((MAP_SIZE - 1.0) * lerp);     // [0, MAP_SIZE] -> [0, 64]
                
                double red = (i < n1) ? (1.0 * (i + 1.0) / n1) : 1.0;
                double green = (i < n1) ? 0.0 : (i >= n1 && i < 2 * n1) ? (1.0 * (i + 1 - n1) / n1) : 1.0;
                double blue = (i < 2 * n1) ? 0.0 : 1.0 * (i + 1 - 2 * n1) / ((double)MAP_SIZE - 2.0 * n1);
                
                red *= 255;
                green *= 255;
                blue *= 255;
                hot[i] = new SKColor((byte)red, (byte)green, (byte)blue);
            }
        }
    
        static MakeCool() void
        {
            for(int i =0 ; i < MAP_SIZE; i++)
            {
                double lerp = (double)i / (double)MAP_SIZE;      // [0, 1]
                int i = (int)((MAP_SIZE - 1) * lerp);     // [0, MAP_SIZE] -> [0, 64]
                double array = 1.0 * i / (MAP_SIZE - 1.0);
    
                byte red = (byte)(255 * array);
                byte green = (byte)(255 * (1 - array));
                byte blue = 255;
    
                cool[i] = new SKColor(red, green, blue);
            }
        }
    
        static MakeJet() void
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
                byte red = (byte)(cMatrix[i, 0] * 255);
                byte green = (byte)(cMatrix[i, 1] * 255);
                byte blue = (byte)(cMatrix[i, 2] * 255);
                
                jet[i] = new SKColor(red, green, blue);
            }           
        }
        
    }
}
