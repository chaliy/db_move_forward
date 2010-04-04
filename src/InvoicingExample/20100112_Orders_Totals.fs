module _20100112_Orders_Totals

open Common

let up = [   
    field_to Invoicing.Order "Total" Amount
]