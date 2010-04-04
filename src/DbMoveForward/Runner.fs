module MoveForward.Runner

open MovesTools
open DbTools

let Run(target, force) =
        
    let init = Initializer(target, force)
    let db = init.Database        
    let proc = MovesProcessor(db)
    let stuff = VersionsStuff(db)
    let asm = System.Reflection.Assembly.GetEntryAssembly() 
    let stepResolver = StepsResolver(asm)    

    let lastVersion = stuff.CurrentVersion target.Sequence
    printfn "Previous version is %s" lastVersion
    let stepsToApply = stepResolver.Resolve(lastVersion)
    printfn "%i steps are pending" (stepsToApply |> Seq.length)

    // Backup database before appling steps..
    // Probably also should stop connections first..    
    printfn "Backup database..."
    init.Backup(lastVersion)

    for s in stepsToApply do
        printfn "\r\nStep %s in process" s.Version        
        proc.ApplyMoves s.Moves
        stuff.UpdateVersion target.Sequence s.Version
        printfn "Step %s -- Done" s.Version