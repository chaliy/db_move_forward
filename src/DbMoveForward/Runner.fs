module MoveForward.Runner

open MovesTools
open DbTools

let Move(target) =
        
    let init = Initializer(target)                        
    let db = init.Database()
    let proc = MovesProcessor(db)
    let stuff = VersionsStuff(db)
    let asm = System.Reflection.Assembly.GetEntryAssembly() 
    let stepResolver = StepsResolver(asm)    

    let lastVersion = stuff.CurrentVersion target.Sequence
    printfn "Previous version is %s" lastVersion
    let stepsToApply = stepResolver.Resolve(lastVersion)
    printfn "%i steps are pending" (stepsToApply |> Seq.length)

    stepsToApply
    |> Seq.iter(fun s ->
                    proc.ApplyMoves s.Moves
                    stuff.UpdateVersion target.Sequence s.Version )

    ()