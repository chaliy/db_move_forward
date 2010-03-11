module MoveForward.DbTools

open Model

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

    let systemColumns : Model.Column list = [
        { Name = "Version"; Type = Model.ColumnType.Guid}
        { Name = "LastUpdatedBy"; Type = Model.ColumnType.Guid }
        { Name = "LastUpdatedDate"; Type = Model.ColumnType.DateTime }
        { Name = "CreatedBy"; Type = Model.ColumnType.Guid }
        { Name = "CreatedDate"; Type = Model.ColumnType.DateTime }
        { Name = "ContextID"; Type = Model.ColumnType.Guid }
    ]
            
    let buildColumn tbl c =
        let dataType = match c.Type with
                       | String -> DataType.NVarChar(450)
                       | Text -> DataType.NVarCharMax
                       | Number -> DataType.Int
                       | BigNumber -> DataType.BigInt
                       | Decimal -> DataType.Decimal(5, 19)                           
                       | Enum -> DataType.NVarChar(64)
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
                                   
        table.Columns // Add other columns
        |> List.append(systemColumns) // Also add support columns...                                        
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

