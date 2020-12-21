module Primitives

open Structure
open Syntax
open Print
open Interpretation
open Actor

let primitiveState =
    let mutable (primitives : Map<string, (State -> State)>) = Map.empty
    let primitive name fn = primitives <- Map.add name fn primitives

    primitive "!" (fun s ->
        match s.Stack with
        | String n :: v :: t -> { s with Stack = t; Map = Map.add n v s.Map }
        | _ :: _ :: _ -> failwith "Expected vs"
        | _ -> failwith "Stack underflow")

    primitive "@" (fun s ->
        match s.Stack with
        | String n :: t ->
            match Map.tryFind n s.Map with
            | Some v -> { s with Stack = v :: t }
            | None -> failwith "Not found" // TODO return flag?
        | _ :: _ -> failwith "Expected s"
        | _ -> failwith "Stack underflow")

    primitive "i" (fun s ->
        match s.Stack with
        | List q :: t -> { s with Stack = t; Continuation = q @ s.Continuation }
        | _ :: _ -> failwith "Expected q"
        | _ -> failwith "Stack underflow")

    primitive "depth" (fun s ->
        { s with Stack = Number (double s.Stack.Length) :: s.Stack })

    primitive "clear" (fun s -> { s with Stack = [] })

    primitive "dup" (fun s ->
        match s.Stack with
        | x :: t -> { s with Stack = x :: x :: t }
        | _ -> failwith "Stack underflow")

    primitive "drop" (fun s ->
        match s.Stack with
        | _ :: t -> { s with Stack = t }
        | _ -> failwith "Stack underflow")

    primitive "dip" (fun s ->
        match s.Stack with
        | List q :: v :: t -> { s with Stack = t; Continuation = q @ v :: s.Continuation }
        | _ :: _ :: _ -> failwith "Expected vq"
        | _ -> failwith "Stack underflow")

    let binaryOp name op = primitive name (fun s ->
        match s.Stack with
        | Number x :: Number y :: t -> { s with Stack = Number (op x y) :: t }
        | _ :: _ :: _ -> failwith "Expected nn"
        | _ -> failwith "Stack underflow")

    binaryOp "+" (+)
    binaryOp "-" (+)
    binaryOp "*" (*)
    binaryOp "/" (/)

    let unaryOp name op = primitive name (fun s ->
        match s.Stack with
        | Number x :: t -> { s with Stack = Number (op x) :: t }
        | _ :: _ -> failwith "Expected n"
        | _ -> failwith "Stack underflow")

    unaryOp "chs" (fun n -> -n)
    unaryOp "recip" (fun n -> 1. / n)
    unaryOp "abs" (fun n -> abs n)

    primitive "define" (fun s ->
        match s.Stack with
        | String n :: (List _ as q) :: t -> { s with Dictionary = Map.add n q s.Dictionary; Stack = t }
        | String n :: v :: t -> { s with Dictionary = Map.add n v s.Dictionary; Stack = t }
        | _ :: _  :: _ -> failwith "Expected qs"
        | _ -> failwith "Stack underflow")

    primitive "eval" (fun s ->
        match s.Stack with
        | String b :: t -> brief b |> interpret { s with Stack = t } false
        | _ :: _ -> failwith "Expected s"
        | _ -> failwith "Stack underflow")

    primitive "state" (fun s -> printState s; s)

    primitive "post" (fun s ->
        match s.Stack with
        | String n :: List q :: t ->
            match Map.tryFind n registry with
            | Some actor -> actor.Post q; { s with Stack = t }
            | None -> failwith "Actor not found"
        | _ :: _  :: _ -> failwith "Expected ss"
        | _ -> failwith "Stack underflow")

    { emptyState with Primitives = primitives }
