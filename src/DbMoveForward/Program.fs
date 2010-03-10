open MoveForward
open MoveForward.DbTools

//let moves = seq {
//    yield! _20100101_Orders.up
//    yield! _20100112_Orders_Totals.up    
//}

let target = { Database = "MoveTest"
               Sequence = "Test" }

//let initializer = new Initializer(target)
//initializer.Init()

let ver = "0" // -- Get current version

type Step = {
    Version : string
    Moves : Moves list
}

let step_20100101 = { Version = "20100101"
                      Moves = _20100101_Orders.up }

let step_20100112 = { Version = "20100112"
                      Moves = _20100112_Orders_Totals.up }

let stepsToApply = [ step_20100101
                     step_20100112 ]

open Microsoft.SqlServer.Management.Smo
let srv = new Server()
let db = srv.Databases.[target.Database]

let proc = MovesProcessor(db)
let stuff = SystemStuff(db)

stepsToApply
|> Seq.iter(fun s ->
                proc.ApplyMoves s.Moves
                stuff.UpdateVersion target.Sequence s.Version )