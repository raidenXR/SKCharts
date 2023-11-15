#### Warning

for notation the Notation.csproj from  https://github.com/raidenXR/Notation.git \
is needed. Clone it and make sure to set the path correctly in `SKChartFS.fsx` and \
samples `.fsx`s.

Unfortunately for the time being, `libSkiaSharp.dll` and `libHarfBuzzSharp.dll` will have to be copied \
manually to the `bin/Debug/dotnet7.0/` directory. \
Those native libs can be downloaded from the 
- SkiaSharp.NativeAssets.Linux.NoDependencies 
- SkiaSharp.NativeAssets.Win32
- SkiaSharp.NativeAssets.macOS 


For font files any `.ttf` can be loaded, though as for now, Notation.csproj expects to find fonts from KaTeX \
in the font file at `bin/Debug/dotnet7.0/`
