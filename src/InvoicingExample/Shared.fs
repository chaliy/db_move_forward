module Shared

open MoveForward.Model

type EntityName = {
    Name : string
    Module : string
}

type FieldType =
| String
| Text
| Amount
| Number

module Utils =

    let prularize name =
        name + "s"

    let makeColumnType = function
                         | String -> ColumnType.String(450)
                         | Text -> ColumnType.Text
                         | Amount -> ColumnType.Decimal
                         | Number -> ColumnType.Int

    let makeTableName name =
        { Name = prularize name.Name
          Schema = name.Module } 

let field name (t : FieldType) : Column =    
    { Name = name
      Type = Utils.makeColumnType t }

let reference name (e : EntityName) : Column =    
    { Name = name
      Type = ColumnType.ForeignKey(Utils.makeTableName e) }

let systemColumns = [
        { Name = "Version"; Type = ColumnType.Guid}
        { Name = "LastUpdatedBy"; Type = ColumnType.Guid }
        { Name = "LastUpdatedDate"; Type = ColumnType.DateTime }
        { Name = "CreatedBy"; Type = ColumnType.Guid }
        { Name = "CreatedDate"; Type = ColumnType.DateTime }
        { Name = "ContextID"; Type = ColumnType.Guid }
    ]

let schema name = 
    Moves.AddSchema(name)

let entity name (cols : Column list) =        
    let columns = 
        [{ Name = name.Name + "ID"
           Type = ColumnType.PrimmaryKey }]
        |> List.append cols
        |> List.append systemColumns 

    Moves.AddTable({ Name = Utils.makeTableName name
                     Columns = columns })

let field_to entity name (t : FieldType) =        
    Moves.AddColumn(Utils.makeTableName entity, { Name = name
                                                  Type = Utils.makeColumnType t })

let reference_to entity name (e : EntityName) =    
    Moves.AddColumn(Utils.makeTableName entity, { Name = name + "ID"
                                                  Type = ForeignKey(Utils.makeTableName e) })    

module Invoicing =
    
    let Name = "Invoicing"

    let Customer = { Module = Name; Name = "Customer" }
    let Order = { Module = Name; Name = "Order" }