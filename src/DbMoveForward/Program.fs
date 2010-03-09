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

let tables = 
    moves
    |> Seq.groupBy(function
                   | AddTable t -> t.Name
                   | AddColumn (t, c) -> t )
    |> Seq.map(fun (n, mm) -> { Name = n
                                Columns = mm
                                          |> Seq.collect(function
                                                         | AddTable t -> t.Columns
                                                         | AddColumn (t, c) -> [c] )
                                          |> Seq.toList })
                                          
tables
|> Seq.iter(fun t -> printfn "Table %s with %i columns" t.Name t.Columns.Length)       