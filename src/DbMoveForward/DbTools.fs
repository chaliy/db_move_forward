module MoveForward.DbTools

open Microsoft.SqlServer.Management
type internal Smo.Database with
  member x.Tables2 = x.Tables |> Seq.cast<Smo.Table>

open Shared
open Model
    
type VersionsStuff(db : Smo.Database) =
    
    let updateVersion sequenceName version =
        // TODO o_O... nothing else to say...
        let script = sprintf "IF EXISTS (SELECT * FROM __MoveVersions WHERE Sequence = '%s')
                                   UPDATE __MoveVersions
                                 SET [Version] = '%s'
                                    ,[LastUpdated] = getutcdate()
                                 WHERE Sequence = '%s'
                              ELSE
                                 INSERT INTO __MoveVersions
                                     ([Sequence]
                                     ,[Version] 
                                     ,[LastUpdated]) 
                                 VALUES 
                                     ('%s' 
                                     ,'%s' 
                                     ,getutcdate())" sequenceName version sequenceName sequenceName version
                                                  
        db.ExecuteNonQuery(script)
        
    let currentVersion sequenceName =
        let query = sprintf "select * from __MoveVersions where Sequence = '%s'" sequenceName
        let result = db.ExecuteWithResults(query)
        let table = result.Tables.[0]
        if table.Rows.Count > 0 then
            (table.Rows.[0].["Version"] :?> string)        
        else "0"
            
    member x.UpdateVersion = updateVersion
    member x.CurrentVersion = currentVersion


type MovesProcessor(db) =

    let trace = new Event<string>()
    
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
        trace.Trigger(sprintf "Table %s has been created" table.Name.Name)
        

    let ensureTable (name : TableName) =                        
        db.Tables2
        |> Seq.find(fun t -> t.Name = name.Name)            

    let createColumn tableName column =
        let target = ensureTable tableName
        addColumn column target
        target.Alter()
        trace.Trigger(sprintf "Table %s has been altered with column %s" tableName.Name column.Name)

    let createSchema name =
        if db.Schemas.Contains(name) then
            trace.Trigger(sprintf "Schema %s already existis, skipped" name)
        else
            let sch = new Smo.Schema(db, name)
            sch.Create()
            trace.Trigger(sprintf "Schema %s has been created" name)

    let applyMoves moves =
        moves
        |> List.iter(function
                    | AddTable t -> createTable t
                    | AddColumn (t, c) -> createColumn t c 
                    | AddSchema n -> createSchema n )

    member x.ApplyMoves = applyMoves    
    member x.Trace = trace.Publish


type Initializer(target : Target, ?force : bool) =
    let force = defaultArg force false
    let srv = Smo.Server()
    let trace = new Event<string>()

    let createTable db name (columns : (Smo.Table -> Smo.Column) list) =
        let target = Smo.Table(db, name)            

        columns
        |> List.map(fun n -> n(target))
        |> List.iter(target.Columns.Add)            

        target.Create()
        target               

    let createSupportTables db =
        createTable db "__MoveVersions" [ fun t -> Smo.Column(t, "Sequence", Smo.DataType.NVarChar(450))                                                         
                                          fun t -> Smo.Column(t, "Version", Smo.DataType.NVarChar(450))                                                  
                                          fun t -> Smo.Column(t, "LastUpdated", Smo.DataType.DateTime) ] |> ignore
    
        createTable db "__MoveLogs" [ fun t -> Smo.Column(t, "ID", Smo.DataType.UniqueIdentifier)                                                       
                                      fun t -> Smo.Column(t, "Sequence", Smo.DataType.NVarChar(450))                                              
                                      fun t -> Smo.Column(t, "Message", Smo.DataType.Text)
                                      fun t -> Smo.Column(t, "EntryDate", Smo.DataType.DateTime) ] |> ignore

        trace.Trigger(sprintf "Support tables __MoveVersions & __MoveLogs has been created")

        

    let createDatabase() =
        let db = new Smo.Database(srv, target.Database)
        db.Create()

        trace.Trigger(sprintf "Database %s has been created" target.Database)

        createSupportTables(db)
                
        db

    let ensureConfiguredDatabase() =
        let db = srv.Databases.[target.Database]     
        if db.Tables.Contains("__MoveVersions") = false
           || db.Tables.Contains("__MoveLogs") = false then
           if force then
                createSupportTables(db)
           else                       
                failwith "Support tables was not found, run tool with --force argument."        
        db
                
    let ensureDatabase() =        
        if srv.Databases.Contains(target.Database) = false then
            if force then
                createDatabase()
            else
                failwith (sprintf "Database %s was not found, run tool with --force argument." target.Database)                                
        else
            ensureConfiguredDatabase()
          
    let db = ensureDatabase()

    let backup(version) =
        trace.Trigger(sprintf "Backup database...")
        let sqlBackup = new Smo.Backup()
        let backupTime = System.DateTime.UtcNow
        sqlBackup.Action <- Smo.BackupActionType.Database;
        sqlBackup.BackupSetDescription <- sprintf "Before migration. Last migration was %s. Backup date: %s %s"
                                         version 
                                         (backupTime.ToLongDateString())
                                         (backupTime.ToLongTimeString())
        sqlBackup.BackupSetName <- "Migrations"

        sqlBackup.Database <- db.Name
                 
        sqlBackup.Initialize <- true
        sqlBackup.Checksum <- true
        sqlBackup.ContinueAfterError <- true
        sqlBackup.FormatMedia <- false
        
        let name = sprintf "%s-%s(ver-%s).bak" 
                            db.Name 
                            (backupTime.ToString("yyyyMMdd-HHmmss"))
                            version

        sqlBackup.Devices.Add(new Smo.BackupDeviceItem(name, Smo.DeviceType.File));
        sqlBackup.Incremental <- false
        
        sqlBackup.LogTruncation <- Smo.BackupTruncateLogType.NoTruncate;

        sqlBackup.SqlBackup(srv)
        trace.Trigger(sprintf "Backup saved to %s\\%s" srv.BackupDirectory name)
                                       
    member x.Database = db 
    member x.Backup = backup
    member x.Trace = trace.Publish