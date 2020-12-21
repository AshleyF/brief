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
    Dictionary: Map<string, Value>
    Primitives: Map<string, (State -> State)> }
let emptyState = { Continuation = []; Stack = []; Map = Map.empty; Dictionary = Map.empty; Primitives = Map.empty }
