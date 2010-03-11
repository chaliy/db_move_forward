module MoveForward.MovesTools

open Model

open System.Reflection
open Microsoft.FSharp.Reflection

let stepRegex = new System.Text.RegularExpressions.Regex("_(?<version>\d*)_.?")

type StepsResolver(asm : Assembly) =

    let resolveMoves (t : System.Type) =
        let falgs = BindingFlags.Static ||| BindingFlags.Public
        let p = t.GetProperty("up", falgs)
        if p = null then None
        else Some(p.GetValue(null, null) :?> Moves list)

    let resolveSteps lastVersion =
        asm.GetTypes()
        |> Seq.filter(fun x -> FSharpType.IsModule x)
        |> Seq.map(fun t -> (t, stepRegex.Match(t.Name)))
        |> Seq.filter(fun (t, m) -> m.Success)
        |> Seq.map(fun (t, m) -> (t, m.Groups.["version"].Value))
        |> Seq.sortBy(fun (t, v) -> v)
        |> Seq.map(fun (t, v) -> (t, v, resolveMoves t))
        |> Seq.filter(fun (t, v, mm) -> mm.IsSome)
        |> Seq.map(fun (t, v, mm) -> { Version = v
                                       Moves = mm.Value } )
        |> Seq.skipWhile(fun s -> s.Version <= lastVersion)
    
    member x.Resolve = resolveSteps