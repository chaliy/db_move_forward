module _20100115_CustomerStars

open Shared

let up = [       
    entity Invoicing.CustomerStar [                        
        field "Star" Number
        reference "Customer" Invoicing.Customer
    ]    
]