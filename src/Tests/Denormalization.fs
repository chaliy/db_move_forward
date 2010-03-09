module ``Denormalization Specification``

open FsSpec
        
open MoveForward

module ``Describe resolving tables from moves`` =
    
    
    let ``return at least something`` = spec {
        
        let moves = [AddTable({ Name = "Test"
                                Columns = [] })]

        let tables = Denormalization.FromMoves moves

        tables.should_not_be_null
    }
    
    let ``resolve all tables`` = spec {
        
        let moves = [AddTable({ Name = "Test"
                                Columns = [] })
                     AddTable({ Name = "Test2"
                                Columns = [] })]

        let tables = Denormalization.FromMoves moves

        (tables |> Seq.length).should_be_equal_to(2)
    }
    
    let ``resolve all columns for table`` = spec {
        
        let moves = [AddTable({ Name = "Test"
                                Columns = [column "StringColumn" String 
                                           column "DecimalColumn" Decimal ] }) ]

        let table = (Denormalization.FromMoves moves) |> Seq.head

        (table.Columns |> Seq.length).should_be_equal_to(2)
    }
    
    let ``resolve all columns including added for table`` = spec {
        
        let moves = [AddTable({ Name = "Test"
                                Columns = [column "StringColumn" String 
                                           column "DecimalColumn" Decimal ] })
                     AddColumn("Test", { Name = "AddedColumn"
                                         Type = Number } ) ]

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