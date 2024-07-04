open System

type Value =
  | Number of double
  | String of string
  | List   of Value list
  | Map    of Map<string, Value>

and State = {
  Continuation: char list
  Stack: Value list
  //Map: Map<string, Value>
  //Dictionary: Map<string, Value>
  //Primitives: Map<string, (State -> State)>
  }

let initial = { Continuation = []; Stack = [] }

let rec machine state =
  let resume cont state = { state with Continuation = cont } |> machine
  let digit n cont =
    match state.Stack with
    | Number m :: stack' -> { state with Stack = Number (m * 10. + n) :: stack' } |> resume cont
    | _ -> failwith "Invalid stack"
  let binop op cont =
    match state.Stack with
    | Number x :: Number y :: stack' -> { state with Stack = Number (op y x) :: stack' } |> resume cont
    | _ :: _ :: _ -> failwith "Type error: Expected number number"
    | _ -> failwith "Stack underflow"
  match state.Continuation with
  | ' ' :: cont -> resume cont state
  | '#' :: cont -> { state with Stack = Number 0. :: state.Stack } |> resume cont
  | '0' :: cont -> digit 0. cont
  | '1' :: cont -> digit 1. cont
  | '2' :: cont -> digit 2. cont
  | '3' :: cont -> digit 3. cont
  | '4' :: cont -> digit 4. cont
  | '5' :: cont -> digit 5. cont
  | '6' :: cont -> digit 6. cont
  | '7' :: cont -> digit 7. cont
  | '8' :: cont -> digit 8. cont
  | '9' :: cont -> digit 9. cont
  | '+' :: cont -> binop (+) cont
  | '-' :: cont -> binop (-) cont
  | '*' :: cont -> binop ( * ) cont
  | '/' :: cont -> binop (/) cont
  | '%' :: cont -> binop (%) cont
  | [] -> state
  | _ -> failwith "Invalid literal number mode instruction"

let test code = { initial with Continuation = code |> List.ofSeq } |> machine |> printfn "Result: %A"

test "#0123 #42 #7 * +"

(*
type Value =
  //| Symbol  of string
  | String of string
  | Number of double
  | List   of Value list
  | Map    of Map<string, Value>
    //| Word    of Primitive

and Instruction =
  | In of byte // TODO: stream of bytes
  | Add | Sub | Mul | Div | Mod
  | Drop | Dup | Swap | Over

and State = {
  Continuation: Instruction list
  Stack: Value list
  //Map: Map<string, Value>
  //Dictionary: Map<string, Value>
  //Primitives: Map<string, (State -> State)>
  }

let rec machine (state : State) =
  let resume cont state = { state with Continuation = cont }
  let push stack value state = { state with Stack = value :: stack }
  let binop op cont state =
    match state.Stack with
    | Number x :: Number y :: stack' -> { state with Stack = Number (op y x) :: stack' } |> resume cont |> machine
    | _ :: _ :: _ -> failwith "Type error: Expected number number"
    | _ -> failwith "Stack underflow"
  //let cast fn cont state =
  //  match state.Stack with
  //  | Bytes b :: stack' -> state |> push stack' (fn b) |> resume cont |> machine
  //  | _ -> failwith "Type error: Expected bytes"
  match state.Continuation with
  | In b :: cont -> state |> push state.Stack (b |> double |> Number) |> resume cont |> machine
  //| CastSingle :: cont -> cast (fun b -> Number (BitConverter.ToSingle(b, 0) |> double)) cont state
  //| CastDouble :: cont -> cast (fun b -> Number (BitConverter.ToDouble(b, 0) |> double)) cont state
  //| CastInt8 :: cont -> cast (fun b -> Number (b[0] |> double)) cont state
  //| CastInt16 :: cont -> cast (fun b -> Number (BitConverter.ToInt16(b, 0) |> double)) cont state
  //| CastInt32 :: cont -> cast (fun b -> Number (BitConverter.ToInt32(b, 0) |> double)) cont state
  | Add :: cont -> binop (+) cont state
  | Sub :: cont -> binop (-) cont state
  | Mul :: cont -> binop ( * ) cont state
  | Div :: cont -> binop (/) cont state
  | Mod :: cont -> binop (%) cont state
  | Drop :: cont -> match state.Stack with _ :: stack' -> { state with Stack = stack' } |> resume cont |> machine | _ -> failwith "Stack underflow"
  | Dup :: cont -> match state.Stack with x :: stack' -> { state with Stack = x :: x :: stack' } |> resume cont |> machine | _ -> failwith "Stack underflow"
  | Swap :: cont -> match state.Stack with x :: y :: stack' -> { state with Stack = y :: x :: stack' } |> resume cont |> machine | _ -> failwith "Stack underflow"
  | Over :: cont -> match state.Stack with x :: y :: stack' -> { state with Stack = y :: x :: y :: stack' } |> resume cont |> machine | _ -> failwith "Stack underflow"
  | [] -> state

let initial = { Continuation = []; Stack = [] }

let rec bytecode instructions = seq {
  match instructions with
  | In b :: instructions' -> yield 0uy; yield b ; yield! bytecode instructions'
  | Add  :: instructions' -> yield 1uy          ; yield! bytecode instructions'
  | Sub  :: instructions' -> yield 2uy          ; yield! bytecode instructions'
  | Mul  :: instructions' -> yield 3uy          ; yield! bytecode instructions'
  | Div  :: instructions' -> yield 4uy          ; yield! bytecode instructions'
  | Mod  :: instructions' -> yield 5uy          ; yield! bytecode instructions'
  | Drop :: instructions' -> yield 6uy          ; yield! bytecode instructions'
  | Dup  :: instructions' -> yield 7uy          ; yield! bytecode instructions'
  | Swap :: instructions' -> yield 8uy          ; yield! bytecode instructions'
  | Over :: instructions' -> yield 9uy          ; yield! bytecode instructions'
  | [] -> () }

let rec instructions bytecode = seq {
  match bytecode with
  | 0uy :: b :: code -> yield In b ; yield! instructions code
  | 1uy :: code -> yield Add       ; yield! instructions code
  | 2uy :: code -> yield Sub       ; yield! instructions code
  | 3uy :: code -> yield Mul       ; yield! instructions code
  | 4uy :: code -> yield Div       ; yield! instructions code
  | 5uy :: code -> yield Mod       ; yield! instructions code
  | 6uy :: code -> yield Drop      ; yield! instructions code
  | 7uy :: code -> yield Dup       ; yield! instructions code
  | 8uy :: code -> yield Swap      ; yield! instructions code
  | 9uy :: code -> yield Over      ; yield! instructions code
  | _ :: _ -> failwith "Unknown instruction"
  | [] -> () }

let test code =
  bytecode code |> List.ofSeq |> printfn "Bytecode: %A"
  { initial with Continuation = code } |> machine |> printfn "Result: %A"

test [In 42uy; In 7uy; Sub]
*)