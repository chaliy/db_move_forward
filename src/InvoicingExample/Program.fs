open MoveForward
open MoveForward.Model

let target = { Database = "MoveTest"
               Sequence = "Test" }

let force = 
    System.Environment.GetCommandLineArgs() 
    |> Array.exists  (fun x -> x = "/force" || x = "--force")    

Runner.Run(target, force) 