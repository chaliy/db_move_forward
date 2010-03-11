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

type Step = {
    Version : string
    Moves : Moves list
}

type Target = {
    Database : string
    Sequence : string
}

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

module MovesTools =

    open System.Reflection
    open Microsoft.FSharp.Reflection
    
    let stepRegex = new System.Text.RegularExpressions.Regex("_(?<version>\d*)_.?")    
    
    
    type StepsResolver(asm : Assembly) =

        let resolveMoves (t : System.Type) =
            let falgs = BindingFlags.Static ||| BindingFlags.Public
            let p = t.GetProperty("up", falgs)
            if p = null then None
            else Some(p.GetValue(null, null) :?> Moves list)

        let resolveSteps lastVersion =
            asm.GetTypes()
            |> Seq.filter(fun x -> FSharpType.IsModule x)
            |> Seq.map(fun t -> (t, stepRegex.Match(t.Name)))
            |> Seq.filter(fun (t, m) -> m.Success)
            |> Seq.map(fun (t, m) -> (t, m.Groups.["version"].Value))
            |> Seq.sortBy(fun (t, v) -> v)
            |> Seq.map(fun (t, v) -> (t, v, resolveMoves t))
            |> Seq.filter(fun (t, v, mm) -> mm.IsSome)
            |> Seq.map(fun (t, v, mm) -> { Version = v
                                           Moves = mm.Value } )
            |> Seq.skipWhile(fun s -> s.Version <= lastVersion)
        
        member x.Resolve = resolveSteps

module DbTools =

    open Microsoft.SqlServer.Management.Smo    

    type internal Database with
      member x.Tables2 = x.Tables |> Seq.cast<Table>
      
        
    type VersionsStuff(db : Database) =

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
            (row.["Version"] :?> string)

        member x.InitVersion = initVersion
        member x.UpdateVersion = updateVersion
        member x.CurrentVersion = currentVersion
            

    type MovesProcessor(db) =    
                
        let buildColumn tbl c =
            let dataType = match c.Type with
                           | String -> DataType.NVarChar(450)
                           | Text -> DataType.Text
                           | Decimal -> DataType.Decimal(5, 19)                           
                           | _ -> failwith "Column type is not supported yet"        
            let clmn = new Column(tbl, c.Name, dataType) 
            clmn.Nullable <- true            
            clmn
                               
        let createTable table =            
            let target = new Table(db, table.Name.Name, table.Name.Schema)
            
            // Create primmary key
            let pkeyName = table.Name.Name + "ID"
            target.Columns.Add(new Column(target, pkeyName, DataType.UniqueIdentifier))                    
            let pkeyI = new Index(target, pkeyName + "_PK")
            pkeyI.IndexKeyType <- IndexKeyType.DriPrimaryKey
            pkeyI.IndexedColumns.Add(new IndexedColumn(pkeyI, pkeyName))
            target.Indexes.Add(pkeyI)
                               
            // Add other columns
            table.Columns
            |> Seq.map(buildColumn target)        
            |> Seq.iter(target.Columns.Add)
                                    
            target.Create()

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

            let stuff = VersionsStuff(db)
            stuff.InitVersion(target.Sequence)


            ()
                                                                
        member x.Init() = init()
        member x.Database() = srv.Databases.[target.Database] 

module Mover =

    open MovesTools
    open DbTools
    
    let Move(target) =
        
        let asm = System.Reflection.Assembly.GetEntryAssembly() 
        let stepResolver = StepsResolver(asm)
        let init = Initializer(target)                        
        let db = init.Database()
        let proc = MovesProcessor(db)
        let stuff = VersionsStuff(db)

        let lastVersion = stuff.CurrentVersion target.Sequence
        let stepsToApply = stepResolver.Resolve(lastVersion)

        stepsToApply
        |> Seq.iter(fun s ->
                        proc.ApplyMoves s.Moves
                        stuff.UpdateVersion target.Sequence s.Version )

        ()