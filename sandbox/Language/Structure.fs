module Structure

open System

type NamedPrimitive(name: string, func: State -> State)= 
    member _.Name = name
    member _.Func = func
    override this.Equals(o) =
        match o with
            | :? NamedPrimitive as p -> this.Name = p.Name
            | _ -> false
    override this.GetHashCode() = hash (this.Name)
    interface IComparable with
        override this.CompareTo(o) =
            match o with
                | :? NamedPrimitive as p -> compare this.Name p.Name
                | _ -> -1

and Word =
    | Literal   of Value
    | Primitive of NamedPrimitive
    | Secondary of string * Word list

and Value =                           // v
    | Number    of double             // n
    | String    of string             // s
    | Boolean   of bool               // b
    | List      of Value list         // l
    | Map       of Map<string, Value> // m
    | Quotation of Word list          // q

and State = {
    Continuation: Word list
    Stack: Value list
    Map: Map<string, Value>
    Dictionary: Map<string, Word> }
let emptyState = { Continuation = []; Stack = []; Map = Map.empty; Dictionary = Map.empty }
