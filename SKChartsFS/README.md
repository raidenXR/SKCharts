#### Warning

for notation the Notation.csproj from  https://github.com/raidenXR/Notation.git \
is needed. Clone it and make sure to set the path correctly in `SKChartFS.fsx` and \
samples `.fsx`s.

Unfortunately for the time being, `libSkiaSharp.dll` and `libHarfBuzzSharp.dll` will have to be copied \
manually to the `bin/Debug/dotnet7.0/` directory. \
Those native libs can be downloaded and extracted from the 
- SkiaSharp.NativeAssets.Linux.NoDependencies 
- SkiaSharp.NativeAssets.Win32
- SkiaSharp.NativeAssets.macOS 


For font files any `.ttf` can be loaded, though as for now, `Notation.csproj` expects to read fonts from \ `KaTeX\fonts` directory for testing purposes  https://github.com/KaTeX/KaTeX \
Clone the repo and copy `fonts` directory at `bin/Debug/dotnet7.0/`
