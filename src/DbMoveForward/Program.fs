open MoveForward

let target = { Database = "MoveTest"
               Sequence = "Test" }

//let initializer = new Initializer(target)
//initializer.Init()

//Mover.Move(target)

open System.Reflection
open Microsoft.FSharp.Reflection
open MoveForward.MovesTools

let asm = System.Reflection.Assembly.GetEntryAssembly() 
let stepResolver = StepsResolver(asm)
let stepsToApply = stepResolver.Resolve("0")
let step = stepsToApply |> Seq.head

printfn "Boom!"