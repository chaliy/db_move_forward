module ``Moves Tools Specification``

open FsSpec        
open Helpers

open MoveForward
open MoveForward.MovesTools

module ``Describe resolving steps`` =
    
    let ``return at least something`` = spec {
        
        let asm = createAssembly [createType("CoolType")]
        let stepsResolver = StepsResolver(asm)

        let steps = stepsResolver.Resolve()
        
        steps.should_not_be_null
    }

    let ``return empty list of steps if none defined`` = spec {
        
        let asm = createAssembly [createType("CoolType")]
        let stepsResolver = StepsResolver(asm)

        let steps = stepsResolver.Resolve()
        
        steps.should_be_empty
    }

    let ``return steps if there is step`` = spec {
        
        let asm = createAssembly [createModule("_1_Something")]
        let stepsResolver = StepsResolver(asm)

        let steps = stepsResolver.Resolve()
        
        steps.should_not_be_empty
    }

    let ``return step with correct verison number`` = spec {
        
        let asm = createAssembly [createModule("_1_Something")]
        let stepsResolver = StepsResolver(asm)

        let step = stepsResolver.Resolve() |> Seq.head
        
        step.Version.should_be_equal_to "1"
    }