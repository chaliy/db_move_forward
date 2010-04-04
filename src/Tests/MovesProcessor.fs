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
let schema = new Smo.Schema(db, "SimpleSchema")
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
        result.Columns.Count.should_be_equal_to 1
    }