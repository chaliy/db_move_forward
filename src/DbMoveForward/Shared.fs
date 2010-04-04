module MoveForward.Shared

type Maybe<'a> = option<'a>

let succeed x = Some(x)

let fail = None

let bind rest = function    
        | None -> fail
        | Some r -> rest r

let delay f = f()
 
type MaybeBuilder() =
    member b.Return(x)  = succeed x
    member b.Bind(p, rest) = bind rest p
    member b.Delay(f)   = delay f
    member b.Let(p,rest) : Maybe<'a> = rest p
   
let maybe = MaybeBuilder()
