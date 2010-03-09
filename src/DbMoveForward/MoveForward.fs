module MoveForward

(* Model *)

type TableName = string

type ColumnType =
| String
| Text
| Number
| Decimal
| PrimmaryKey
| ForeignKey of TableName

type Column = {
    Name : string
    Type : ColumnType
}

type Table = {
    Name : string
    Columns : Column list    
}

type Moves =
| AddTable of Table
| AddColumn of (TableName * Column)

(* DSL *)

let create_table (table : TableName) (cols : Column list) =        
    Moves.AddTable({ Name = table
                     Columns = cols })

let add_column table name (t : ColumnType) =        
    Moves.AddColumn(table, { Name = name
                             Type = t })

let column name (t : ColumnType) : Column =    
    { Name = name
      Type = t }

let fkey name (t : TableName) : Column =    
    { Name = name
      Type = ForeignKey(t) }

let pkey: Column =    
    { Name = "ID"
      Type = PrimmaryKey }


module Denormalization =

    open NHibernate.Cfg

    let FromMoves moves = 
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

    let FromConfig (conf : Configuration) =
        conf.ClassMappings        
        |> Seq.map(fun m -> m.Table)
        |> Seq.map(fun t -> { Name = t.Name
                              Columns = [] } )        

open Microsoft.SqlServer.Management.Smo

type DbTools(database : string) =    

    let resolveName (name : TableName) =
        match name.LastIndexOf(".") with
        | x when x < 0 -> ("dbo", name)
        | x -> (name.Substring(0, x), name.Substring(x + 1))

    let resolveDataType = function
                          | String -> DataType.NVarChar(450)
                          | Text -> DataType.Text
                          | _ -> failwith "Column type is not supported yet"
                           

    let createTable table =
        let srv = new Server()        
        let db = srv.Databases.[database]                
        let name = resolveName table.Name
        let tbl = new Table(db, fst(name), snd(name)) 
        
        table.Columns
        |> Seq.map(fun c -> 
                        let dataType = resolveDataType c.Type
                        let clmn = new Column(tbl, c.Name, dataType) 
                        clmn.Nullable <- true
                        clmn )        
        |> Seq.iter(tbl.Columns.Add)
        tbl.Create()


    let applyMoves moves =
        moves
        |> Seq.iter(function
                    | AddTable t -> createTable(t)
                    | _ -> () )

    member x.ApplyMoves = applyMoves