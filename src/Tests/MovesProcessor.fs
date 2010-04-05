module ``Moves Processing Specification``

open FsSpec        

open MoveForward.Model
open MoveForward.DbTools
open Microsoft.SqlServer.Management

// Init
let name = sprintf "MoveAutomatedTest-%s" 
                   (System.DateTime.Now.ToString("yyyyMMdd-HHmmss"))                            
let target = { Database = name
               Sequence = "Test" }
let init = Initializer(target, true)
let db = init.Database        
let schema = new Smo.Schema(db, "Fake")
schema.Create()
let movesProcessor = new MovesProcessor(db)

module ``Describe apply moves`` =
    
    let ``do nothing if no moves defined`` = spec {        
        movesProcessor.ApplyMoves([])        
    }

    let ``create simple table`` = spec {
        movesProcessor.ApplyMoves([Moves.AddTable({ Name = {Schema = schema.Name
                                                            Name = "SimpleTable" }
                                                    Columns = [{ Name = "Version"
                                                                 Type = ColumnType.Guid }
                                                               ] })])
                                                               
        let result = db.Tables.["SimpleTable", schema.Name]
        result.should_not_be_null
        result.Columns.Count.should_be_equal_to 1 // Just single column...
    }

    let ``create table with primmary key`` = spec {
        movesProcessor.ApplyMoves([Moves.AddTable({ Name = {Schema = schema.Name
                                                            Name = "TableWithPrimmaryKey" }
                                                    Columns = [{ Name = "PrimmaryKeyColumn"
                                                                 Type = ColumnType.PrimmaryKey }
                                                               ] })])
                                                               
        let result = db.Tables.["TableWithPrimmaryKey", schema.Name]        
        result.Columns.Count.should_be_equal_to 1 // Just single column...
        result.Indexes.Count.should_be_equal_to 1 // Just single index...
        let primmaryKeyIndex = result.Indexes.[0]
        primmaryKeyIndex.IndexKeyType.should_be_equal_to Smo.IndexKeyType.DriPrimaryKey 
        primmaryKeyIndex.IndexedColumns.Count.should_be_equal_to 1
    }

    let ``create table with foreign key`` = spec {
        let refrenceeName = { Schema = schema.Name
                              Name = "RefrenceeTable" }
        movesProcessor.ApplyMoves(
            [Moves.AddTable({ Name = refrenceeName
                              Columns = [{ Name = "RefrenceeTableID"
                                           Type = ColumnType.PrimmaryKey } ] })                                        
             Moves.AddTable({ Name = {Schema = schema.Name
                                      Name = "RefrencerTable" }
                              Columns = [{ Name = "RefrenceeTableID"
                                           Type = ColumnType.ForeignKey(refrenceeName) } ] })])
                                                               
        let result = db.Tables.["RefrencerTable", schema.Name]        
        result.ForeignKeys.Count.should_be_equal_to 1 // Just single column...
    }

    let ``execute arbitrary script`` = spec {
        movesProcessor.ApplyMoves([Moves.Script("CREATE TABLE [Fake].[ArbitraryScriptTable] 
                                                 (
                                                  	 Name   varchar(20)     NOT NULL
                                                 )")])
                                                 
        db.Refresh()
        let result = db.Tables.["ArbitraryScriptTable", "Fake"]                                                         
        result.should_not_be_null
    }

    let ``execute batch script`` = spec {
        movesProcessor.ApplyMoves([Moves.Script("CREATE TABLE [Fake].[BatchScriptTable] (Name varchar(20) NOT NULL )
                                                 GO
                                                 ALTER TABLE [Fake].[BatchScriptTable] ADD AnotherName VARCHAR(20) NULL
                                                 GO")])
                                                 
        db.Refresh()
        let result = db.Tables.["BatchScriptTable", "Fake"]                                                         
        result.should_not_be_null
    }