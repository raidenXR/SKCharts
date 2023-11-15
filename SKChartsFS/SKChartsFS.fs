module SKChartsFS
    open System
    open System.Numerics
    open System.Text
    open System.Linq
    open System.IO
    open System.Threading
    open System.Runtime.InteropServices
    open SkiaSharp
    open SKCharts
    open SKCharts.OpenTK
    open Notation


    // Make sure to have SKChart2D ctors with appropriate args and IDisposable()
    let createLineEmpty (capacity: int) (color: SKColor) =
        Model2D.CreateLine(capacity, color)

    let createLineFromValues (values: (float * float) array) (color: SKColor) =
        let x, y = Array.unzip values
        Model2D.CreateLine(x, y, color)

    let createLineFromXY (x: float array) (y: float array) (color: SKColor) = 
        Model2D.CreateLine(x, y, color)


    // Make sure to have SKChart2D ctors with appropriate args and IDisposable()
    let createPointsEmpty (capacity: int) (color: SKColor) =
        Model2D.CreatePoints(capacity, color)

    let createPointsFromValues (values: (float * float) array) (color: SKColor) =
        let x, y = Array.unzip values
        Model2D.CreatePoints(x, y, color)

    let createPointsFromXY (x: float array) (y: float array) (color: SKColor) = 
        Model2D.CreatePoints(x, y, color)


    type Vertices = 
        | Vertices2 of Vector2 array
        | Vertices3 of Vector3 array

    type Model = 
        | Model2 of Model2D
        | Model3 of Model3D
        | ModelNone
        
        member this.UpdateFromXY(x: float array, y: float array) =
            match this with
            | Model2 m -> m.CopyValues(x, y)
            | Model3 m -> Console.WriteLine "not implemented for Model3D"
            | ModelNone -> Console.WriteLine "model is emtpy"
        
    

    type SKChart =
        | Chart2 of SKChart2D
        | Chart3 of SKChart3D
        | ChartNone

        member this.AddModel(model: Model) =
            match model, this with
            | Model2 m, Chart2 c -> c.AttachModel m
            | Model3 m, Chart3 c -> c.AttachModel m
            | _, _ -> Console.WriteLine "invalid model or chart value"

        member this.RemoveModel(model: Model) =
            match model, this with
            | Model2 m, Chart2 c -> c.DetachModel m
            | Model3 m, Chart3 c -> c.DetachModel m
            | _, _ -> Console.WriteLine "invalid model or chart value"

        member this.RemoveModelAt(index: int) =
            match this with
            | Chart2 c -> c.DetachModelAt index
            | Chart3 c -> c.DetachModelAt index
            | _ -> Console.WriteLine "invalid model or chart value"

        member this.Update() = 
            match this with
            | Chart2 c -> c.UpdateBounds(); c.Update()
            | Chart3 c -> c.UpdateBounds(); c.Update()
            | ChartNone -> Console.WriteLine "ChartNone value"
 

        
    let updateModelFromXY (x: float array) (y: float array) (model: Model2D) = 
        model.CopyValues(x, y)

        
    type Renderer(model: Model) =
        let tex_renderer = new TeXRenderer(20f)
        let mutable _model = model
        let mutable _chart = match model with
                             | Model2 m -> Chart2 (new SKChart2D(m))
                             | Model3 m -> Chart3 (new SKChart3D(m))
                             | ModelNone -> ChartNone
        let mutable window: Window option = None  
        

        let createWindow (title: string) (w: int) (h: int) (chart: SKChart) =
            match chart with
            | Chart2 c -> new Window(title, w, h, Chart2D = c)
            | Chart3 c -> new Window(title, w, h, Chart3D = c) 
            | ChartNone -> new Window(title, w, h)

        let renderImg (latex: string) = 
            let parser = new Parser(latex)
            let hlist = parser.Parse().ToList()
            tex_renderer.TypesetRootHList(hlist, new Vector2(30f, 30f))
            tex_renderer.SnapshotNotationImg()


        new(model: Model2D) = Renderer(Model2 model)
        new(model: Model3D) = Renderer(Model3 model)
        new() = Renderer(ModelNone)
            

        member this.Model with get() = _model

        member this.Chart 
            with get() = _chart
            and set(value) = 
                _chart <- value
                match window, _chart with
                | Some w, Chart2 c -> w.Chart2D <- c
                | Some w, Chart3 c -> w.Chart3D <- c
                | Some w, ChartNone -> w.Chart2D <- null; w.Chart3D <- null
                | None, _ -> ignore()
                

        
        member this.Run([<Optional; DefaultParameterValue("RendererTK")>]title: string, [<Optional; DefaultParameterValue(800)>]w: int, [<Optional; DefaultParameterValue(600)>]h: int) =
            let wnd = match _chart with
                      | Chart2 c -> new Window(title, w, h, Chart2D = c)
                      | Chart3 c -> new Window(title, w, h, Chart3D = c) 
                      | ChartNone -> new Window(title, w, h)
            window <- Some wnd
            let thread = new Thread(wnd.Run)
            thread.Start()
            wnd, thread

        member this.SetNotation(latex: string) =
            match window with
            | Some w -> w.Latex <- latex; w.NotationImg <- renderImg latex
            | None -> Console.WriteLine "window is not running"
                         
        member this.Screenshot(filename: string) = 
            match window with
            | Some w -> w.Screenshot filename
            | None -> Console.WriteLine "window is not active"
            
        member this.AddChartWithModel(model: Model) =
            match _chart, model with
            | ChartNone, Model2 m -> _chart <- Chart2(new SKChart2D(m))
            | ChartNone, Model3 m -> _chart <- Chart3(new SKChart3D(m))
            | _, _ -> Console.WriteLine "renderer already contains a chart"
  

