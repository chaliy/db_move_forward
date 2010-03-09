// get current version
// apply migration

// Scenario
// ver1 = Order {Number : string }
// ver2 = Number -> OrderNumber

// add column
// rename column
// add table
// drop table

open MoveForward

let moves = seq {
    yield! _20100101_Orders.up
    yield! _20100112_Orders_Totals.up    
}
                                          
(Tools.FromMoves moves)
|> Seq.iter(fun t -> printfn "Table %s with %i columns" t.Name t.Columns.Length) 