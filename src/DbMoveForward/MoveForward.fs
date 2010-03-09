module MoveForward

(* Model *)

type TableRef = {
    Name : string
}

type ColumnType =
| String
| Number
| Decimal
| PrimmaryKey
| ForeignKey of TableRef

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
| AddColumn of (TableRef * Column)

type Step =
    abstract member Up : unit -> unit

(* DSL *)

let create_table name (cols : Column list) =        
    Moves.AddTable({ Name = name
                     Columns = cols })

let add_column table name (t : ColumnType) =        
    Moves.AddColumn(table, { Name = name
                             Type = t })

let column name (t : ColumnType) : Column =    
    { Name = name
      Type = t }

let fkey name (t : TableRef) : Column =    
    { Name = name
      Type = ForeignKey(t) }

let pkey: Column =    
    { Name = "ID"
      Type = PrimmaryKey }