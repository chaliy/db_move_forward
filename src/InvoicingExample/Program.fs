open MoveForward
open MoveForward.Model

let target = { Database = "MoveTest"
               Sequence = "Test" }

Moving.Move(target) 