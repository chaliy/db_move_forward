open MoveForward
open MoveForward.Model

let target = { Database = "MoveTest"
               Sequence = "Test" }

//let init = DbTools.Initializer(target)
//init.Init()
Moving.Move(target) 