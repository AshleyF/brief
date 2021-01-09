module Structure

open System

let _stack        = "_stack"
let _continuation = "_continuation"
let _dictionary   = "_dictionary"

type Value =                        // v
    | Symbol  of string             // y
    | Number  of double             // n
    | String  of string             // s
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
    _dictionary, List [Map Map.empty]]

let primitive name fn = Primitive(name, fn)

let getList name state = match Map.tryFind name state with Some (List s) -> s | _ -> failwith (sprintf "Malformed %s" name)
let setList name state (list: Value list) = Map.add name (List list) state

let getStack = getList _stack
let setStack = setList _stack

let getContinuation = getList _continuation
let setContinuation = setList _continuation

let getDictionary = getList _dictionary
let setDictionary = setList _dictionary

let addPrimitives (state: State) (primitives: Primitive list) =
    let dict = getDictionary state
    match dict with
    | Map h :: t ->
        (Seq.fold (fun m (p: Primitive) -> Map.add (p.Name) (Word p) m) h primitives |> Map) :: t |> setDictionary state
    | _ :: t -> failwith "Malformed dictionary"
    | [] -> failwith "Malformed dictionary"

let addFrame dict = Map Map.empty :: dict

let dropFrame = function
    | _ :: [] -> failwith "Cannot drop final dictionary frame"
    | _ :: t -> t
    | [] -> failwith "Malformed dictionary"

let addWord n v = function
    | Map h :: t -> Map (Map.add n v h) :: t
    | _ :: t -> failwith "Malformed dictionary"
    | [] -> failwith "Malformed dictionary"

let tryFindWord n dict =
    let rec tryFind = function
        | Map h :: t ->
            match Map.tryFind n h with
            | Some v -> Some v
            | None -> tryFind t
        | _ :: t -> failwith "Malformed dictionary"
        | [] -> None
    tryFind dict
