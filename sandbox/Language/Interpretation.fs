module Interpretation

open Structure

let step into state =
    let pause c = if into then Symbol "_pause" :: c else c
    match getContinuation state with
    | Symbol "_return" :: c -> updateDictionary dropFrame state |> setContinuation c
    | Symbol "_pause" :: c -> setContinuation c state
    | Symbol s :: c -> 
        let c' = pause c
        match tryFindWord s state with
        | Some (List l) ->
            if List.contains (Symbol "let") l
            then addFrame state |> setContinuation (List.rev l @ Symbol "_return" :: c')
            else setContinuation (List.rev l @ c') state
        | Some (Word w) -> setContinuation c' state |> w.Func
        | Some v -> pushStack v state |> setContinuation c'
        | None -> failwith (sprintf "Unknown word '%s'" s)
    | v :: c -> pushStack v state |> setContinuation (pause c)
    | [] -> state

let rec skip state =
    match getContinuation state with
    | Symbol "_return" :: _
    | Symbol "_pause" :: _ -> step false state |> skip
    | _ -> state

let stepIn state = state |> skip |> step true

let stepOut state =
    let rec out state =
        match getContinuation state with
        | Symbol "_pause" :: _ | [] -> step false state
        | Symbol "_break" :: c -> setContinuation c state
        | _ -> step false state |> out
    out state

let stepOver state = state |> stepIn |> stepOut

let interpret code state =
    let rec interpret' state =
        match getContinuation state with
        | [] -> state
        | Symbol "_break" :: c -> setContinuation c state
        | _ -> step false state |> interpret'
    state |> updateContinuation (fun c -> List.ofSeq code @ c) |> interpret'
