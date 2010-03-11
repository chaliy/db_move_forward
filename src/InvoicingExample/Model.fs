module Model

open MoveForward.Model

module Tables =

    let Customers = { Schema = "Invoicing"; Name = "Customers" }
    let Orders = { Schema = "Invoicing"; Name = "Orders" }
