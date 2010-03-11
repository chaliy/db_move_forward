module _20100112_Orders_Totals

open MoveForward
open MoveForward.Lang
open Model

let up = [   
    add_column Tables.Orders "Total" Decimal
]