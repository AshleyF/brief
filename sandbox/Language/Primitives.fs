module Primitives

open Structure

let primitive name fn = Primitive (NamedPrimitive (name, fn))

let store = primitive "store" (fun s ->
    match s.Stack with
    | String n :: v :: t -> { s with Stack = t; Map = Map.add n v s.Map }
    | _ :: _ :: t -> failwith "Expected vs"
    | _ -> failwith "Stack underflow")

let fetch = primitive "fetch" (fun s ->
    match s.Stack with
    | String n :: t ->
        match Map.tryFind n s.Map with
        | Some v -> { s with Stack = v :: t }
        | None -> failwith "Not found" // TODO return flag?
    | _ :: t -> failwith "Expected s"
    | _ -> failwith "Stack underflow")

let i = primitive "i" (fun s ->
    match s.Stack with
    | Quotation q :: t -> { s with Stack = t; Continuation = q @ s.Continuation }
    | _ :: t -> failwith "Expected q"
    | _ -> failwith "Stack underflow")

let depth = primitive "depth" (fun s ->
    { s with Stack = Number (double s.Stack.Length) :: s.Stack })

let clear = primitive "clear" (fun s -> { s with Stack = [] })

let dup = primitive "dup" (fun s ->
    match s.Stack with
    | x :: t -> { s with Stack = x :: x :: t }
    | _ -> failwith "Stack underflow")

let drop = primitive "drop" (fun s ->
    match s.Stack with
    | _ :: t -> { s with Stack = t }
    | _ -> failwith "Stack underflow")

let dip = primitive "dip" (fun s ->
    match s.Stack with
    | Quotation q :: v :: t -> { s with Stack = t; Continuation = q @ Literal v :: s.Continuation }
    | _ :: _ :: t -> failwith "Expected vq"
    | _ -> failwith "Stack underflow")

let unaryOp name op = primitive name (fun s ->
    match s.Stack with
    | Number x :: t -> { s with Stack = Number (op x) :: t }
    | _ :: t -> failwith "Expected n"
    | _ -> failwith "Stack underflow")

let binaryOp name op = primitive name (fun s ->
    match s.Stack with
    | Number x :: Number y :: t -> { s with Stack = Number (op x y) :: t }
    | _ :: _ :: t -> failwith "Expected nn"
    | _ -> failwith "Stack underflow")

let add = binaryOp "+" (+)
let sub = binaryOp "-" (+)
let mul = binaryOp "*" (*)
let div = binaryOp "/" (/)

let chs = unaryOp "chs" (fun n -> -n)
let recip = unaryOp "recip" (fun n -> 1. / n)
let abs = unaryOp "abs" (fun n -> abs n)

let primitives = Map.ofList [
    "!",     store
    "@",     fetch
    "i",     i
    "depth", depth
    "clear", clear
    "dup",   dup
    "drop",  drop
    "dip",   dip
    "+",     add
    "-",     sub
    "*",     mul
    "/",     div
    "chs",   chs
    "recip", recip
    "abs",   abs
    ]
