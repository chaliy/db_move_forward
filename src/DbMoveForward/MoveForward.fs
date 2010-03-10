module MoveForward

(* Model *)

type TableName = {
    Schema : string
    Name : string
}

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
    Name : TableName
    Columns : Column list    
}

type Moves =
| AddSchema of string
| AddTable of Table
| AddColumn of (TableName * Column)

(* DSL *)

let create_schema name =        
    Moves.AddSchema(name)

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
        |> Seq.filter(function
                      | AddTable x -> true
                      | AddColumn  x -> true
                      | _ -> false )
        |> Seq.groupBy(function
                       | AddTable t -> t.Name
                       | AddColumn (t, c) -> t
                       | _ -> failwith "Move is not supported" )
        |> Seq.map(fun (n, mm) -> { Name = n
                                    Columns = mm
                                              |> Seq.collect(function
                                                             | AddTable t -> t.Columns
                                                             | AddColumn (t, c) -> [c]
                                                             | _ -> failwith "Move is not supported" )
                                              |> Seq.toList })        

    let FromConfig (conf : Configuration) =
        conf.ClassMappings        
        |> Seq.map(fun m -> m.Table)
        |> Seq.map(fun t -> { Name = { Schema = t.Schema
                                       Name = t.Name }
                              Columns = [] } )        

module DbTools =

    open Microsoft.SqlServer.Management.Smo    

    type Database with
      member x.Tables2 = x.Tables |> Seq.cast<Table>     

    type Target = {
        Database : string
        Sequence : string
    }           
        
    type SystemStuff(db : Database) =

        let initVersion sequenceName =
            let script = sprintf "insert into __MoveVersions
                                       ([Sequence], [Version], [LastUpdated])
                                 values
                                       ('%s','0',getutcdate())" sequenceName
            db.ExecuteNonQuery(script)

        let updateVersion sequenceName version =
            let script = sprintf "update __MoveVersions
                                       set [Version] = '%s', [LastUpdated] = getutcdate()
                                 where Sequence = '%s'" version sequenceName
            db.ExecuteNonQuery(script)
            
        let currentVersion sequenceName =
            let query = sprintf "select * from __MoveVersions where Sequence = '%s'" sequenceName
            let result = db.ExecuteWithResults(query)
            let row = result.Tables.[0].Rows.[0]
            (row.["Vesion"] :?> string)

        member x.InitVersion = initVersion
        member x.UpdateVersion = updateVersion
            

    type MovesProcessor(db) =    
        
        let resolveDataType = function
                              | String -> DataType.NVarChar(450)
                              | Text -> DataType.Text
                              | Decimal -> DataType.Decimal(5, 19)
                              | _ -> failwith "Column type is not supported yet"        

        let buildColumn tbl c =
            let dataType = resolveDataType c.Type
            let clmn = new Column(tbl, c.Name, dataType) 
            clmn.Nullable <- true
            clmn
                               
        let createTable table =            
            let tbl = new Table(db, table.Name.Name, table.Name.Schema)
            
            table.Columns
            |> Seq.map(buildColumn tbl)        
            |> Seq.iter(tbl.Columns.Add)

            tbl.Create()

        let enusreTable (name : TableName) =                        
            db.Tables2
            |> Seq.find(fun t -> t.Name = name.Name)            

        let createColumn tableName column =
            let tbl = enusreTable tableName
            let clmn = buildColumn tbl column               
            tbl.Columns.Add(clmn)
            tbl.Alter()

        let createSchema name =
            let sch = new Schema(db, name)
            sch.Create()            

        let applyMoves moves =
            moves
            |> List.iter(function
                        | AddTable t -> createTable t
                        | AddColumn (t, c) -> createColumn t c 
                        | AddSchema n -> createSchema n )

        member x.ApplyMoves = applyMoves    

    type Initializer(target : Target) =
        let srv = new Server()        

        let createTable db name (columns : (Table -> Column) list) =
            let tbl = new Table(db, name)            

            columns
            |> List.map(fun n -> n(tbl))
            |> List.iter(tbl.Columns.Add)            

            tbl.Create()
            tbl
            

        let init() =
            let db = new Database(srv, target.Database)
            db.Create()

            let versions = 
                createTable db "__MoveVersions" [ fun t -> Column(t, "Sequence", DataType.NVarChar(450))                                                         
                                                  fun t -> Column(t, "Version", DataType.NVarChar(450))                                                  
                                                  fun t -> Column(t, "LastUpdated", DataType.DateTime) ]

            let logs = 
                createTable db "__MoveLogs" [ fun t -> Column(t, "ID", DataType.UniqueIdentifier)                                                       
                                              fun t -> Column(t, "Sequence", DataType.NVarChar(450))                                              
                                              fun t -> Column(t, "Message", DataType.Text)
                                              fun t -> Column(t, "EntryDate", DataType.DateTime) ]

            let stuff = SystemStuff(db)
            stuff.InitVersion(target.Sequence)


            ()
                                                    
        //let versions = db.Tables2 |> Seq.find(fun x -> x.Name = "__MoveVersions")
        //let logs = db.Tables2 |> Seq.find(fun x -> x.Name = "__MoveLog")     
        member x.Init() = init()   