open MoveForward
open MoveForward.Model

let target = { Database = "MoveTest"
               Sequence = "Test" }

Runner.Run(target) 