module MoveForward

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

type Step =
    abstract member Up : unit -> unit

let create_table name (cols : Column list) =        
    { new Step with
         member x.Up() = () }
//    { Name = name
//      Columns = cols }

let column name (t : ColumnType) : Column =    
    { Name = name
      Type = t }

let fkey name (t : TableRef) : Column =    
    { Name = name
      Type = ForeignKey(t) }

let primmaryKey: Column =    
    { Name = "ID"
      Type = PrimmaryKey }