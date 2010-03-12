﻿module ``Denormalization Specification``

open FsSpec
        
open MoveForward
open MoveForward.Model

module ``Describe resolving tables from moves`` =
    
    
    let ``return at least something`` = spec {
        
        let moves = [AddTable({ Name = { Schema = "Test"; Name = "Test" }
                                Columns = [] })]

        let tables = Denormalization.FromMoves moves

        tables.should_not_be_null
    }
    
    let ``resolve all tables`` = spec {
        
        let moves = [AddTable({ Name = { Schema = "Test"; Name = "Test" }
                                Columns = [] })
                     AddTable({ Name = { Schema = "Test"; Name = "Test2" }
                                Columns = [] })]

        let tables = Denormalization.FromMoves moves

        (tables |> Seq.length).should_be_equal_to(2)
    }
    
    let ``resolve all columns for table`` = spec {
        
        let moves = [AddTable({ Name = { Schema = "Test"; Name = "Test" }
                                Columns = [{Name = "StringColumn"; Type = (String(150)) }
                                           {Name = "DecimalColumn"; Type = Decimal } ] }) ]

        let table = (Denormalization.FromMoves moves) |> Seq.head

        (table.Columns |> Seq.length).should_be_equal_to(2)
    }
    
    let ``resolve all columns including added for table`` = spec {
        
        let moves = [AddTable({ Name = { Schema = "Test"; Name = "Test" }
                                Columns = [{Name = "StringColumn"; Type = (String(150)) }
                                           {Name = "DecimalColumn"; Type = Decimal } ] })
                     AddColumn({ Schema = "Test"; Name = "Test" }, { Name = "AddedColumn"
                                                                     Type = Int } ) ]

        let table = (Denormalization.FromMoves moves) |> Seq.head

        (table.Columns |> Seq.length).should_be_equal_to(3)
    }    

module ``Describe resolving tables from NHib config`` =
    open NHibernate.Cfg

    let ``return at least something`` = spec {
        
        let cfg = new Configuration()

        let tables = Denormalization.FromConfig cfg

        tables.should_not_be_null
    }