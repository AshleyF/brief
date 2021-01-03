module Structure

type Value =                          // v
    | Symbol    of string             // y
    | Number    of double             // n
    | String    of string             // s
    | Boolean   of bool               // b
    | List      of Value list         // l
    | Map       of Map<string, Value> // m

and State = {
    Continuation: Value list
    Stack: Value list
    Map: Map<string, Value>
    Dictionary: Map<string, Value> List
    Primitives: Map<string, (State -> State)> }
let emptyState = { Continuation = []; Stack = []; Map = Map.empty; Dictionary = [Map.empty]; Primitives = Map.empty }

let addFrame dict = Map.empty :: dict

let dropFrame = function
    | _ :: [] -> failwith "Cannot drop final dictionary frame"
    | _ :: t -> t
    | [] -> failwith "Malformed dictionary"

let addWord n v = function
    | h :: t -> (Map.add n v h) :: t
    | [] -> failwith "Malformed dictionary"

let tryFindWord n dict =
    let rec tryFind = function
        | h :: t ->
            match Map.tryFind n h with
            | Some v -> Some v
            | None -> tryFind t
        | [] -> None
    tryFind dict
