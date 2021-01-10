module Interpretation

open Structure

let step state =
    match getContinuation state with
    | Symbol "_return" :: c -> updateDictionary dropFrame state |> setContinuation c
    | Symbol s :: c -> 
        match tryFindWord s state with
        | Some (List l) -> addFrame state |> setContinuation (List.rev l @ Symbol "_return" :: c)
        | Some (Word w) -> setContinuation c state |> w.Func
        | Some v -> pushStack v state |> setContinuation c
        | None -> failwith (sprintf "Unknown word '%s'" s)
    | v :: c -> pushStack v state |> setContinuation c
    | [] -> state

let prependCode state code = updateContinuation (fun c -> List.ofSeq code @ c) state

let stepIn state code = prependCode state code |> step

let stepOut state code =
    let rec out state =
        match getContinuation state with
        | Symbol "_return" :: _ | [] -> step state
        | Symbol "_break" :: c -> setContinuation c state
        | _ -> step state |> out
    prependCode state code |> out

let stepOver state code =
    let state' = prependCode state code
    let state'' = step state'
    let len = getContinuation >> List.length
    if len state'' > len state' then stepOut state'' [] else state''

let interpret state code =
    let rec interpret' state =
        match getContinuation state with
        | [] -> state
        | Symbol "_break" :: c -> setContinuation c state
        | _ -> step state |> interpret'
    prependCode state code |> interpret'
