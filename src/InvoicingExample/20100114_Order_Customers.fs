module _20100114_Order_Customers

open Common

let up = [       
    reference_to Invoicing.Order "Customer" Invoicing.Customer    
]