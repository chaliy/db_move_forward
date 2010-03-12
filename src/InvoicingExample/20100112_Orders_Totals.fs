module _20100112_Orders_Totals

open Shared

let up = [   
    field_to Invoicing.Order "Total" Amount
]