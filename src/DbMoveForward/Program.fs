open MoveForward

let moves = seq {
    yield! _20100101_Orders.up
    yield! _20100112_Orders_Totals.up    
}
                                          
(Denormalization.FromMoves moves)
|> Seq.iter(fun t -> printfn "Table %s with %i columns" t.Name t.Columns.Length) 