module MoveForward.Model

(* Model *)

type TableName = {
    Schema : string
    Name : string
}

type ColumnType =
| String of int
| Text
| Int
| BigInt
| Decimal
| Guid
| DateTime
| PrimmaryKey
| ForeignKey of TableName * string

type Column = {
    Name : string
    Type : ColumnType
}

type Table = {
    Name : TableName
    Columns : Column list    
}

type Moves =
| AddSchema of string
| AddTable of Table
| AddColumn of (TableName * Column)
| Script of string
| Composite of Moves list

type Step = {
    Version : string
    Moves : Moves list
}

type Target = {
    Database : string
    Sequence : string
}