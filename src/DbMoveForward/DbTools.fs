module MoveForward.DbTools

open Microsoft.SqlServer.Management
type internal Smo.Database with
  member x.Tables2 = x.Tables |> Seq.cast<Smo.Table>

open Model
  
    
type VersionsStuff(db : Smo.Database) =

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
    
    let addColumn (col : Column) (target : Smo.Table) =        
        let addColumnOfType dataType =
            target.Columns.Add(new Smo.Column(target, col.Name, dataType))

        match col.Type with
        | String x -> addColumnOfType(Smo.DataType.NVarChar(x))
        | Text -> addColumnOfType Smo.DataType.NVarCharMax
        | Int -> addColumnOfType Smo.DataType.Int
        | BigInt -> addColumnOfType Smo.DataType.BigInt
        | Decimal -> addColumnOfType(Smo.DataType.Decimal(5, 19))
        | DateTime -> addColumnOfType Smo.DataType.DateTime
        | Guid -> addColumnOfType Smo.DataType.UniqueIdentifier
        | PrimmaryKey ->
            // Create Column
            let targetCol = Smo.Column(target, col.Name, Smo.DataType.UniqueIdentifier)
            targetCol.Nullable <- false
            target.Columns.Add(targetCol)
            // Create index..
            let pkeyI = Smo.Index(target, col.Name + "_PK")
            pkeyI.IndexKeyType <- Smo.IndexKeyType.DriPrimaryKey
            pkeyI.IndexedColumns.Add(Smo.IndexedColumn(pkeyI, col.Name))
            target.Indexes.Add(pkeyI)            

        | ForeignKey referencee ->    
            addColumnOfType Smo.DataType.UniqueIdentifier                                                                     
            // Create foreignkey...
            let fk = Smo.ForeignKey(target, target.Name + "_" + col.Name + "_FK")                                          
            let fkc = Smo.ForeignKeyColumn(fk, col.Name, col.Name)
            fk.Columns.Add(fkc)                                 
            fk.ReferencedTable <- referencee.Name
            fk.ReferencedTableSchema <- referencee.Schema
            target.ForeignKeys.Add(fk)            
        
                           
    let createTable table = 
        let target = new Smo.Table(db, table.Name.Name, table.Name.Schema)
                          
        table.Columns
        |> Seq.iter(fun c -> addColumn c target)                
        
        target.Create()    
        

    let ensureTable (name : TableName) =                        
        db.Tables2
        |> Seq.find(fun t -> t.Name = name.Name)            

    let createColumn tableName column =
        let target = ensureTable tableName
        addColumn column target
        target.Alter()

    let createSchema name =
        let sch = new Smo.Schema(db, name)
        sch.Create()            

    let applyMoves moves =
        moves
        |> List.iter(function
                    | AddTable t -> createTable t
                    | AddColumn (t, c) -> createColumn t c 
                    | AddSchema n -> createSchema n )

    member x.ApplyMoves = applyMoves    

type Initializer(target : Target) =
    let srv = Smo.Server()

    let createTable db name (columns : (Smo.Table -> Smo.Column) list) =
        let target = Smo.Table(db, name)            

        columns
        |> List.map(fun n -> n(target))
        |> List.iter(target.Columns.Add)            

        target.Create()
        target
        

    let init() =
        let db = new Smo.Database(srv, target.Database)
        db.Create()

        let versions = 
            createTable db "__MoveVersions" [ fun t -> Smo.Column(t, "Sequence", Smo.DataType.NVarChar(450))                                                         
                                              fun t -> Smo.Column(t, "Version", Smo.DataType.NVarChar(450))                                                  
                                              fun t -> Smo.Column(t, "LastUpdated", Smo.DataType.DateTime) ]

        let logs = 
            createTable db "__MoveLogs" [ fun t -> Smo.Column(t, "ID", Smo.DataType.UniqueIdentifier)                                                       
                                          fun t -> Smo.Column(t, "Sequence", Smo.DataType.NVarChar(450))                                              
                                          fun t -> Smo.Column(t, "Message", Smo.DataType.Text)
                                          fun t -> Smo.Column(t, "EntryDate", Smo.DataType.DateTime) ]

        let stuff = VersionsStuff(db)
        stuff.InitVersion(target.Sequence)


        ()
                                                            
    member x.Init() = init()
    member x.Database() = srv.Databases.[target.Database] 