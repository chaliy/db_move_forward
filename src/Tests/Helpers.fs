module Helpers

open System
open System.Reflection

(* Reflection utils *)    
let createAssembly (tt : Type seq) =
        { new Assembly() with
              override x.GetTypes() = tt |> Seq.toArray }

let createType name =
     { new TypeDelegator(typeof<string>) with
            override x.Name = name } :> Type

let createModule name =    

    let moduleAttrs = 
        [ CompilationMappingAttribute(SourceConstructFlags.Module) ]   
        |> List.map(fun x -> x :> obj)
        |> List.toArray

    { new TypeDelegator(typeof<string>) with
            override x.Name = name
            override x.GetCustomAttributes(b) = moduleAttrs
            override x.GetCustomAttributes(attributeType, b) = moduleAttrs } :> Type