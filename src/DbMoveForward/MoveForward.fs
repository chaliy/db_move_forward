﻿module MoveForward

(* Model *)

type TableName = string

type ColumnType =
| String
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

type Step =
    abstract member Up : unit -> unit

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