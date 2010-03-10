module ``Moves Tools Specification``

open FsSpec        
open Helpers

open MoveForward
open MoveForward.MovesTools

(* Helpers *)

let createStep name moves = 
    createModule name [createProperty "up" moves]

let createFakeStep name =
            createStep name [Moves.AddSchema("Fake")]

module ``Describe resolving steps`` =
    
    let ``return at least something`` = spec {
        
        let asm = createAssembly [createType("CoolType")]
        let stepsResolver = StepsResolver(asm)

        let steps = stepsResolver.Resolve("0")
        
        steps.should_not_be_null
    }

    let ``return empty list of steps if none defined`` = spec {
        
        let asm = createAssembly [createType("CoolType")]
        let stepsResolver = StepsResolver(asm)

        let steps = stepsResolver.Resolve("0")
        
        steps.should_be_empty
    }

    let ``return steps if there is step`` = spec {
        
        let asm = createAssembly [createStep "_1_Something" [Moves.AddSchema("Fake")]]
        let stepsResolver = StepsResolver(asm)

        let steps = stepsResolver.Resolve("0")
        
        steps.should_not_be_empty
    }

    let ``return step with correct verison number`` = spec {
        
        let asm = createAssembly [createStep "_123_Something" [Moves.AddSchema("Fake")]]
        let stepsResolver = StepsResolver(asm)

        let step = stepsResolver.Resolve("0") |> Seq.head
        
        step.Version.should_be_equal_to "123"
    }

    let ``return step with correct moves`` = spec {
        
        let moves = [ Moves.AddSchema("Schema1")
                      Moves.AddSchema("Schema2") ]                      
        let asm = createAssembly [createStep "_1_Something" moves]
        let stepsResolver = StepsResolver(asm)

        let step = stepsResolver.Resolve("0") |> Seq.head
        
        step.Moves.should_has_items_of 2

        let firstMove = step.Moves |> Seq.head
        firstMove.should_be_equal_to(Moves.AddSchema("Schema1"))

        let secondMove = step.Moves |> Seq.nth(1)
        secondMove.should_be_equal_to(Moves.AddSchema("Schema2"))
    }

    let ``return steps after given version`` = spec {
                        
        let asm = createAssembly [createFakeStep "_20100101_Something"
                                  createFakeStep "_20100102_Something"
                                  createFakeStep "_20100103_Something"
                                  createFakeStep "_20100104_Something"]
        let stepsResolver = StepsResolver(asm)

        let steps = stepsResolver.Resolve("20100102")
        
        steps.should_has_items_of 2

        let firstStep = steps |> Seq.head
        firstStep.Version.should_be_equal_to("20100103")

        let secondStep = steps |> Seq.nth(1)
        secondStep.Version.should_be_equal_to("20100104")
    }