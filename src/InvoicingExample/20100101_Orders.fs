module _20100101_Orders

open Shared

let up = [   
    schema Invoicing.Name
    entity Invoicing.Order [                
        field "Test" String        
    ]
]