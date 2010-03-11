module MoveForward.Model

(* Model *)

type TableName = {
    Schema : string
    Name : string
}

type ColumnType =
| String
| Text
| Number
| BigNumber
| Decimal
| Enum
| Guid
| DateTime
| ForeignKey of TableName

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

type Step = {
    Version : string
    Moves : Moves list
}

type Target = {
    Database : string
    Sequence : string
}