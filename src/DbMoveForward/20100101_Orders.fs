module _20100101_Orders

open MoveForward

let CUSTOMERS : TableRef = { Name = "Invoicing.Customers" }

let up = [   
    create_table "Invoicing.Orders" [                
        column "Test" String
        fkey "Customers" CUSTOMERS
    ]
]