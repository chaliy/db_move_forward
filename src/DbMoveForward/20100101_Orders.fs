﻿module _20100101_Orders

open MoveForward
open Model

let up = [   
    create_table Tables.Orders [                
        column "Test" String
        fkey "Customers" Tables.Customers
    ]
]