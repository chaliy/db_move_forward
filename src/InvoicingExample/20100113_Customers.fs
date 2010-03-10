module _20100113_Customers

open MoveForward
open Model

let up = [       
    create_table Tables.Customers [                
        column "Test" String
        column "Test12" Text
        column "Age" Decimal
    ]
]