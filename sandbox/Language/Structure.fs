module Structure

open System

let _stack        = "_stack"
let _continuation = "_continuation"
let _dictionary   = "_dictionary"

type Value =                        // v
    | Symbol  of string             // y
    | String  of string             // s
    | Number  of double             // n
    | List    of Value list         // l
    | Map     of Map<string, Value> // m
    | Word    of Primitive          // w

and Primitive(name: string, func: State -> State)= 
    member _.Name = name
    member _.Func = func
    override this.Equals(o) =
        match o with
            | :? Primitive as p -> this.Name = p.Name
            | _ -> false
    override this.GetHashCode() = hash (this.Name)
    interface IComparable with
        override this.CompareTo(o) =
            match o with
                | :? Primitive as p -> compare this.Name p.Name
                | _ -> -1

and State = Map<string, Value>

let emptyState = Map.ofList [
    _stack, List []
    _continuation, List []
    _dictionary, Map Map.empty]

let primitive name fn = Primitive(name, fn)

let getList name state = match Map.tryFind name state with Some (List s) -> s | _ -> failwith (sprintf "Malformed %s" name)
let setList name (list: Value list) state = Map.add name (List list) state
let updateList name fn state = setList name (state |> getList name |> fn) state

let getStack = getList _stack
let setStack = setList _stack
let updateStack = updateList _stack
let pushStack v = updateStack (fun s -> v :: s)

let getContinuation = getList _continuation
let setContinuation = setList _continuation
let updateContinuation = updateList _continuation

let getDictionary state = match Map.tryFind _dictionary state with Some (Map m) -> m | _ -> failwith (sprintf "Malformed dictionary")
let setDictionary m state = Map.add _dictionary (Map m) state
let updateDictionary fn state = setDictionary (state |> getDictionary |> fn) state

let addPrimitives (state: State) (primitives: Primitive list) =
    updateDictionary (fun d -> Seq.fold (fun m (p: Primitive) -> Map.add (p.Name) (Word p) m) d primitives) state

let addWord n v state = updateDictionary (Map.add n v) state

let tryFindWord n state = getDictionary state |> Map.tryFind n
