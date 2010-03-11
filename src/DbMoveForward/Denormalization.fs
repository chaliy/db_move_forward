module MoveForward.Denormalization

open NHibernate.Cfg
open Model

let FromMoves moves = 
    moves        
    |> Seq.groupBy(function
                   | AddTable t -> Some(t.Name)
                   | AddColumn (t, c) -> Some(t)
                   | _ -> None )
    |> Seq.filter(fun (n, mm) -> n.IsSome)
    |> Seq.map(fun (n, mm) -> (n.Value, mm))
    |> Seq.map(fun (n, mm) -> { Name = n
                                Columns = mm
                                          |> Seq.collect(function
                                                         | AddTable t -> t.Columns
                                                         | AddColumn (t, c) -> [c]
                                                         | _ -> [] )
                                          |> Seq.toList })        

let FromConfig (conf : Configuration) =
    conf.ClassMappings        
    |> Seq.map(fun m -> m.Table)
    |> Seq.map(fun t -> { Name = { Schema = t.Schema
                                   Name = t.Name }
                          Columns = [] } )

