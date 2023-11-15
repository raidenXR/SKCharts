#r "nuget: SkiaSharp, 2.88.6"
#r "nuget: OpenTK, 4.8.1"
#r "nuget: ImGui.NET, 1.89.9.3"
#I "bin/Debug/net7.0"
#r "SKCharts.dll"
#r "SKCharts.OpenTK.dll"
#r "SKChartsFS.dll"
#r "Notation.dll"

open System.Runtime.InteropServices
open System
open System.Diagnostics
open System.IO
open System.Threading
open SkiaSharp
open SKCharts
open SKCharts.OpenTK
open SKChartsFS

let functions = [
	"g(x) = \\int_a^b \\frac{1}{2} x^2 dx"
	"f(x) = 4.3 \\cdot x^{3A_z} \\cdot \\Gamma - N_A + \\frac{A + D}{B - G} + (A - B_{\\gamma})"
	"f(x) = \\int _a ^b \\frac{(A + B - 94.3)}{e^{-RT} \\cdot 9.43 x} dx"
	"f(x) = \\int _a ^b \\frac{(A + B - 94.3)}{e^{-RT \\cdot \\frac{K_l}{N_t}} \\cdot 9.43 x} dx"
	"\\frac{(A-B)}{(e^{-RT})} + \\frac{1}{2}"
	"f(x) = (A_n + B_{n + 1}) - \\frac{1}{2} x^2 \\cdot \\gamma"
	"g(x) = E^{-RT} + 4.213 T - 6.422 T - \\gamma^{-2}"
	"z(x) = 3.2343 e^{-1.2} + 8.5"
	"a(x) = \\frac{Z - 9.2 + A^2}{e^{0.8}} + \\frac{x^2 + 2 * x + 1}{x^3 - 1}"
	"g(x) = \\int_a^b \\frac{1}{2} x^2 dx"
	"f(x) = (A_n + B_{n + 1}) - \\frac{1}{2} x^2 \\cdot \\gamma"
	"g(x) = E^{-RT} + 4.213 T - 6.422 T - \\gamma^{-2}"
	"z(x) = 3.2343 e^{-1.2} + 8.5"
	"a(x) = \\frac{Z - 9.2 + A^2}{e^{0.8}} + \\frac{x^2 + 2 * x + 1}{c_{\\delta} + a_{\\gamma}}"
]

// lines
let x = [|for i in 0..100 -> float i|]

let y0 = [|for i in 0..100 -> float i|]
let line0 = createLineFromXY x y0 SKColors.Blue

let y1 = y0 |> Array.map(fun a -> 0.12 * a * a - 4.2 * a + 3.1)
let line1 = createLineFromXY x y1 SKColors.Red

let y2 = Array.map2 (fun a b -> a - b) y0 y1
let line2 = createLineFromXY x y2 SKColors.Green


let renderer = new Renderer(line0)
let window, thread = renderer.Run()


renderer.Chart.AddModel(Model2 line1)
renderer.Chart.AddModel(Model2 line2)

renderer.SetNotation functions[1]

renderer.Screenshot "screenshot_img2.png"

renderer.Chart.RemoveModelAt(1)
