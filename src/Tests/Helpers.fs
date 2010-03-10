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

let createProperty n v =
    { new PropertyInfo() with
            override x.Name = n
            member x.GetAccessors(b) = [||]
            member x.GetGetMethod(b) = failwith "Not implemented"
            member x.GetIndexParameters() = [||]
            member x.GetSetMethod(b) = failwith "Not implemented"
            member x.GetValue(o, f, b, i, c) = v
            member x.SetValue(o, v, f, b, i, c) = ()
            member x.Attributes = PropertyAttributes.None
            member x.CanRead = true
            member x.CanWrite = false
            member x.PropertyType = v.GetType()
            member x.DeclaringType = typeof<string>
            member x.ReflectedType = typeof<string>
            member x.GetCustomAttributes(b) = [||]
            member x.GetCustomAttributes(t, b) = [||]
            member x.IsDefined(t, b) = false }

let createModule name (properties : PropertyInfo seq) =
    
    let moduleAttrs = 
        [ CompilationMappingAttribute(SourceConstructFlags.Module) ]   
        |> List.map(fun x -> x :> obj)
        |> List.toArray

    { new TypeDelegator(typeof<string>) with
            override x.Name = name
            override x.GetPropertyImpl(name : string,
                                       flags : BindingFlags,
                                       binder : Binder,
                                       returnType : Type,
                                       types : Type array, 
                                       modifiers : ParameterModifier[] ) = 
                properties 
                |> Seq.find(fun p -> p.Name = name)
            override x.GetCustomAttributes(b) = moduleAttrs
            override x.GetCustomAttributes(attributeType, b) = moduleAttrs } :> Type