module _20100101_Orders

open MoveForward
open Model

let up = [   
    create_schema "Invoicing"
    create_table Tables.Orders [                
        column "Test" String        
    ]
]