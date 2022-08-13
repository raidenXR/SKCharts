## WIP

TODO:
___________________________________
- Proper Occulation algorithm
- Matrix4x4, Matrix3x2, Vector3 correct API functions
- SKFunctions make appropriate bindings


UPDATE:
_____________________________________
`sk_canvas_draw_vertices` from sk_canvas.h uses the function \
`sk_vertices_make` 
which creates an **immutable** array of vertices, (it copies the values\
from the respective arrays of *positions*, *colors*, *indices*), because of that is pointless\
to use Skia with the current design as it makes impossible to avoid allocations, i.e reuse buffers.\


The project is terminated, it will not receive updates.\
**P.S** it does not compile, it was meant to be a draft before bindings for libSkiaSharp.so were written\
and then it would be tested and finalized. Since, it is discontinued I make the repo public as a *gist*.
 

