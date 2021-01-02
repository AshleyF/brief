# Brief Sandbox Journal

Starting experimenting with Brief implementation details while documenting thoughts. Brief is a concatenative language with quotations. See [Vocabulary reference](./Vocabulary.md).

## 09 DEC 2020 Structure

The initial thoughts on the structure of Brief is that we have `Values`, `Words` and a machine `State`.

NOTE: This structure was simplified later (see 21 DEC 2020 Simplification).

`Values` may be primitives (e.g. `double`, `string`, `bool`, ...) or may be compound types (e.g. `Value list`, `Map<string, Value`, `Set<Value>`, ...) or may be `Quotations` which are a list of `Words`. With the inclusion of quotations, functions are data.

```fsharp
type Value =
    | Number    of double
    | String    of string
    | Boolean   of bool
    | List      of Value list
    | Map       of Map<string, Value>
    | Set       of Set<Value>
    | Quotation of Word list
```

DEBATE 0: Sets of values require that words (in quotations) be comparable. Literals and secondaries are easy enough, but primitives (`State -> State` functions) may be more difficult. Considering implementing `IComparable` based on a name or ID.

`Words` represent code. `Literal` values are pushed to the stack, `Primitives` update the state, and `Secondary` words are a list of words to be applied. Note that quotations are also lists of words, but are treated as values (not applied; e.g. a literal quotation is pushed to the stack).

DEBATE 1: We explored whether to include literals or to instead create primitives as closures capturing the value and having the effect of pushing to the stack (data is code). Cute, but we're thinking now that literals should be a distinct kind of word. It can still be said that words are code and so literals are data as code; a nice symmetry with quotations being code as data.

```fsharp
and Word =
    | Literal   of Value
    | Primitive of (State -> State)
    | Secondary of Word list
```

The `State` of the "machine" is a `Continuation` list of words, a parameter `Stack` of values and a `Map` used as a global memory space of named values.

```fsharp
and State = {
    Continuation: Word list
    Stack: Value list
    Map: Map<string, Value> }
```

## 10 DEC 2020 Identity

We decided to resolve debate #0 above by introducing a `NamedPrimitive` type that supports `IComparable` by treating the name as the identity (because functions cannot be compared). This allows `Values` to include `Set<Value>`, which can include `Quotations`, which are lists of `Words`, which include `Primitives` and so must be comparable.

```fsharp
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
```

## 11 DEC 2020 Interpretation

NOTE: This interpretation scheme was simplified later (see 21 DEC 2020 Simplification).

The first cut at interpretation was to recursively evaluate the whole machine state, including an initial `Continuation`. `Literals` are pushed to the `Stack`. `Primitives` are applied to the state and, most interestingly, `Secondaries` are appended to the `Continuation` for evaluation on the next iteration.

```fsharp
let rec interpret state =
    match state.Continuation with
    | Literal v :: t -> { state with Stack = v :: state.Stack; Continuation = t } |> interpret
    | Primitive p :: t -> { state with Continuation = t } |> p.Func |> interpret
    | Secondary (_, s) :: t -> { state with Continuation = s @ t } |> interpret
    | [] -> state
```

The second cut was to remove the `Continuation` from the state and instead process `Word` by `Word`; `Literals` go to the stack, `Primitives` are still applied to the state but are no longer able to manipulate the program, `Secondaries` are evaluated by recursively folding over them.

```fsharp
let rec step state = function
    | Literal v -> { state with Stack = v :: state.Stack }
    | Primitive p -> p.Func state
    | Secondary (_, s) -> Seq.fold step state s
```

In fact, full evaluation is then just a fold: `let interpret = Seq.fold step`. Nice, but unfortunate that `Primitives` can no longer manipulate the program (e.g. to impliment `Dip`) and we don't like that the recursion happens in F#/.NET land rather than in more directly exposed machinery.

DEBATE 2: Should the current `Continuation` be part of the machine state? Probably yes.

The below visualization of the mechanics are quite nice. `Continuation` on the left, `Stack` on the right (or visa versa if you prefer postfix). `Secondary` words are "expanded" and appended to the `Continuation`. I've done this before for a debugger by inserting "break" and "step out" words between the expansion and the rest of the program. Very tangible, exposed, simple mechanics. This idea was inspired by [Stevan Apters's XY language](http://www.nsl.com/k/xy/xy.htm).

    area 7.200000 |                    // initial program
             area | 7.200000           // 7.2 pushed
          * pi sq | 7.200000           // area expanded
       * pi * dup | 7.200000           // sq expanded
           * pi * | 7.200000 7.200000  // 7.2 duplicated
             * pi | 51.840000          // mulpiply
       * 3.141593 | 51.840000          // pi expanded
                * | 3.141593 51.840000 // pi pushed
                  | 162.860163         // multiply

DEBATE 3: Related to #2, Recursive evaluation of `Secondaries` should be handled with exposed mechanics, supporting Brief-based debugging and (obviously) tail recursion. Prepending to a current `Continuation` works and ideas such as inserting debugging words are pretty slick.

A vague idea forming is that a protocol between Brief "actors" could be streaming code. Inspiration comes from the GreenArrays GA144 on which nodes start empty; listening on ports for code to execute. The GA144 has a machanism to point the program counter at a port rather than at memory, reading and executing code as it streams in and instruction words with micronext may easily be used to fill memory with code coming in and jump to that. A Brief machine should start empty and be fed with code. This code should then also be able to be persisted and evaluated internally.

DEBATE 4: Should we allow a hybrid of stored-program instruction and "streaming" instructions to the machine?

The third cut is to change the processing of `Secondaries` to merely prepend to a `Continuation` in the state. This `word` function is similar to this first `interpret` above but is not recursive.

```fsharp
let word state = function
    | Literal v -> { state with Stack = v :: state.Stack }
    | Primitive p -> p.Func state
    | Secondary (_, s) -> { state with Continuation = s @ state.Continuation }
```

Then to interpret a stream of `Words` along with a state, we have two distinct "modes." While the `Continuation` is empty, we simply walk the stream of words one-by-one evaluating them. Otherwise, when there _is_ a `Continuation`, we peal off words from it one-by-one and evaluate them. When there the `Continuation` is empty and the stream is complete, then we terminate.

```fsharp
let rec interpret stream state =
    match state.Continuation with
    | [] ->
        match Seq.tryHead stream with
        | Some w -> word state w |> interpret (Seq.tail stream)
        | None -> state
    | w :: c -> word { state with Continuation = c } w |> interpret stream
```

This now exposes the mechanics of recursion in the machine and allows for simpler debugging. It also exposes the `Continuation` to be manipulated by `Primitives`. Finally, it allows for a hybrid of stored/streaming words.

## 12 DEC 2020 Vocabulary

Let's build up the set of available words. Starting with getting the `depth` of the stack and `clearing` it:

```fsharp
let depth = Primitive (NamedPrimitive ("depth", (fun s ->
    { s with Stack = Number (double s.Stack.Length) :: s.Stack })))

let clear = Primitive (NamedPrimitive ("clear", (fun s -> { s with Stack = [] })))
```

Manipulating the stack:

```fsharp
let dup = Primitive (NamedPrimitive ("dup", (fun s ->
    match s.Stack with
    | x :: t -> { s with Stack = x :: x :: t }
    | _ -> failwith "Stack underflow")))

let drop = Primitive (NamedPrimitive ("drop", (fun s ->
    match s.Stack with
    | _ :: t -> { s with Stack = t }
    | _ -> failwith "Stack underflow")))

let swap = primitive (NamedPrimitive ("swap" (fun s ->
    match s.Stack with
    | x :: y :: t -> { s with Stack = y :: x :: t }
    | _ -> failwith "Stack underflow")))
```

Evaluating quotations on the stack by prepending them to the `Continuation`. This is made possible by this representation of the machine decided on yesterday.

Dip is an interesting word, also taken from Joy. It evaluates a quotation while "dipping" below the next value on the stack. This is accomplished again by manipulating the `Continuation` to prepend the unquoted code followed by the value being dipped under as a literal:

```fsharp
let dip = Primitive (NamedPrimitive ("dip", (fun s ->
    match s.Stack with
    | Quotation q :: v :: t -> { s with Stack = t; Continuation = q @ Literal v :: s.Continuation }
    | _ :: _ :: t -> failwith "Expected qv"
    | _ -> failwith "Stack underflow")))
```

Adding some arithemetic:

```fsharp
let unaryOp name op = Primitive (NamedPrimitive (name, (fun s ->
    match s.Stack with
    | Number x :: t -> { s with Stack = Number (op x) :: t }
    | _ :: t -> failwith "Expected n"
    | _ -> failwith "Stack underflow")))

let binaryOp name op = Primitive (NamedPrimitive (name, (fun s ->
    match s.Stack with
    | Number x :: Number y :: t -> { s with Stack = Number (op x y) :: t }
    | _ :: _ :: t -> failwith "Expected nn"
    | _ -> failwith "Stack underflow")))

let add = binaryOp "+" (+)
let sub = binaryOp "-" (-)
let mul = binaryOp "*" (*)
let div = binaryOp "/" (/)
lte mod = binaryOp "mod" (%)

let neg = unaryOp "neg" (fun n -> -n)
let recip = unaryOp "recip" (fun n -> 1. / n)
let abs = unaryOp "abs" (fun n -> abs n)
```

Adding some secondary words:

```fsharp
let pi = Secondary ("pi", [Literal (Number Math.PI)])
let e = Secondary ("e", [Literal (Number Math.E)])

let sq = Secondary ("sq", [dup; mul])
let area = Secondary ("area", [sq; pi; mul])
```

An example usage:

```fsharp
interpret [Literal (Number 7.2); Literal (String "dip under me"); Literal (Quotation [area]); dip] emptyState |> printDebug
```

## 13 DEC 2020 Dictionary

The first thought was to create a `define` word that would add any `Value` to the `Map`, including `Words`, and add a `find` word to retrieve them. The thought now that this is a base case used by more specialized words to define `Literals`, `Primitives`, and `Secondaries`. Renaming `define` to `!` (taken from Forth) and `find` to `@` (also from Forth), read as "store" and "fetch" of course.

``` fsharp
let store = primitive "!" (fun s ->
    match s.Stack with
    | String n :: v :: t -> { s with Stack = t; Map = Map.add n v s.Map }
    | _ :: _ :: t -> failwith "Expected vs"
    | _ -> failwith "Stack underflow")

let fetch = primitive "@" (fun s ->
    match s.Stack with
    | String n :: t ->
        match Map.tryFind n s.Map with
        | Some v -> { s with Stack = v :: t }
        | None -> failwith "Not found" // TODO return flag?
    | _ :: t -> failwith "Expected s"
    | _ -> failwith "Stack underflow")
```

DEBATE 5: Should definitions be `Words`, `Quotations` or _any_ `Value`?

Defining `Words` does seem special. A `def` word can be created for this but it's merely a renaming of `store`; nothing special other than expecting a `Quotation` but there is no enforcement of that.

```fsharp
let define = Secondary ("def", [store])
```

DEBATE 6: Obviously including a type system to distinguish `define` (expecting a `Quotation`) from `store` (expecting *any* `Value`) is a very big open question. In this particular case a type error would not even cause an issue until later `fetching` and trying to apply a non-`Quotation`.

Now we can `define` words in the "dictionary".

```fsharp
Literal (Quotation [dup]);                      Literal (String "dup");  store
Literal (Quotation [mul]);                      Literal (String "*");    store
Literal (Quotation [Literal (Number Math.PI)]); Literal (String "pi");   store
Literal (Quotation [dup; mul]);                 Literal (String "sq");   store
Literal (Quotation [sq; pi; mul]);              Literal (String "area"); store
```

DEBATE 7: Explore how and whether to support namespaces or something else to avoid collisions in the map. Perhaps `define` should hang everything off of a "dictionary" key or maybe "_dictionary" with a convention that a leading underscore denotes Brief internals. User namespacing would just be a matter of convention. Perhaps `@` and `!` could be redefined by the user to redirect to a branch in the `Map`.

DEBATE 8: Maybe _all_ of Brief's internals, the `Stack`, `Continuation`, and the `Map` itself should be avaliable via fetch/store. For example as "_stack", "_continuation", "_map", ... exposing the entire mechanics to programatic manipulation. Or maybe these should be exposed by individual primitives. The idea of the machine state being something threaded through the execution as opposed to a globally accessible entity seems more ideal.

## 14 DEC 2020 Syntax

It's getting annoying to write Brief code as F#. For example the circle area sample:

```fsharp
let pi = Secondary ("pi", [Literal (Number Math.PI)])
let sq = Secondary ("sq", [dup; mul])
let area = Secondary ("area", [sq; pi; mul])

[Literal (Number 7.2); area] |> interpret emptyState true |> printState
```

It's time to introduce some minimal Brief syntax. `Words` will be simple whitespace-separated tokens interpreted by looking up in a dictionary. If not found, then tokens will be parsed as `Number` literals (e.g. `2.71`, `123`) or as `Booleans` (`true`, `false`) or as single-word `Strings` in the form `'foo` (with a leading tick). At the moment we're ignoring literal forms for `List`, `Map`, `Set`, and more complete support for `Strings`. This is a minimum viable syntax to start with. Additionally, `Quotations` will be supported with square bracket syntax: `[foo bar]`.

TODO Support literal syntax for `List`, `Map`, `Set`, and full `String`

First, to lex source, we merely break words on whitespace and split square brackets into separated tokens. This could have been even simpler by requiring whitespace around square brackets (like Factor). This more compact syntax (like Joy) is more "brief."

```fsharp
let lex source =
    let rec lex' token source = seq {
        let emit (token: char list) = seq { if List.length token > 0 then yield token |> List.rev |> String.Concat }
        match source with
        | c :: t when Char.IsWhiteSpace c ->
            yield! emit token
            yield! lex' [] t
        | ('[' as c) :: t | (']' as c) :: t ->
            yield! emit token
            yield c.ToString()
            yield! lex' [] t
        | c :: t -> yield! lex' (c :: token) t
        | [] -> yield! emit token }
    source |> List.ofSeq |> lex' []
```

Next, tokens are given structure with `Quotations` containing child nodes and/or nested quotations:

```fsharp
type Node =
    | Token of string
    | Quote of Node list

let parse tokens =
    let rec parse' nodes tokens =
        match tokens with
        | "[" :: t ->
            let q, t' = parse' [] t
            parse' (Quote q :: nodes) t'
        | "]" :: t -> List.rev nodes, t
        | [] -> List.rev nodes, []
        | token :: t -> parse' (Token token :: nodes) t
    match tokens |> List.ofSeq |> parse' [] with
    | (result, []) -> result
    | _ -> failwith "Unexpected quotation close"
```

Finally, nodes are given meaning; becoming a sequence of proper `Words` that can be fed into `interpret`:

```fsharp
let rec compile (dictionary: Map<string, Word>) nodes = seq {
    match nodes with
    | Token t :: n ->
        match Map.tryFind t dictionary with
        | Some w -> yield w
        | None ->
            match Double.TryParse t with
            | (true, v) -> yield Literal (Number v)
            | _ ->
                match Boolean.TryParse t with
                | (true, v) -> yield Literal (Boolean v)
                | _ ->
                    if t.StartsWith '\'' && t.Length > 1
                    then yield Literal (String (t.Substring(1)))
                    else failwith (sprintf "Unknown word: %s" t)
        yield! compile dictionary n
    | Quote q :: n ->
        yield Literal (Quotation (compile dictionary q |> List.ofSeq))
        yield! compile dictionary n
    | [] -> () }
```

DEBATE 9: Factor has the idea of parse-time words which could be useful. Once parsing is moved into Brief itself we should consider this.

The complete process is bundled into a single composition:

```fsharp
let brief dictionary = lex >> parse >> compile dictionary >> List.ofSeq
```

The `dictionary` given is preloaded with primitives:

```fsharp
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
    "neg",   neg
    "recip", recip
    "abs",   abs ]
```

DEBATE 10: The `define` word manipulates the machine state rather than this dictionary above. This needs to be resolved with this mapping living _inside_ the machine and manipulable by Brief code.

Secondaries are added to the dictionary:

```fsharp
let define name source dictionary = Map.add name (Secondary (name, brief dictionary source)) dictionary

let prelude =
    primitives
    |> define "pi"   "3.14159"
    |> define "e"    "2.71828"
    |> define "sq"   "dup *"
    |> define "area" "sq pi *"
```

This is getting more manageable with Brief and F# interleaved. Eventually of course Brief will be completly self-hosting. For now, defining secondaried looks like the above and usage looks like the below:

```fsharp
"7.2 area" |> brief prelude |> interpret emptyState |> printState
```

DEBATE 11: The above naturally fell together with postfix notation. We should consider prefix notation as well. This may still be _processed_ in reverse.

## 15 DEC 2020 Self-Hosting + REPL

The unsatisfactory aspect of the `brief` compiler above is that everything is external to the machine. Source is compiled to `Word list` and fed to the machine. The `define` function is not a Brief word, but an external function adding to a dictionary outside of the machine. This is a fine architecture and has been used for very simple machines like Brief Embedded where the host computer does the compilation and the microcontroller runs a *very* simple VM. For a more self-hosting system, these mechanics should be moved inside.

DEBATE 12: Should the Brief VM be self-hosting or simpler but require a host?

We could use the existing `Map` in machine state to contain the dictionary, but it simplifies the code to keep this separate and more type-specific as a `Map<string, Word>`, so adding this to the state.

DEBATE 13: Should the dictionary be separate or just a key (e.g. "_dictionary") in the `Map`?

Exposing this in the language is a simple matter of adding a new `define` primitive:

```fsharp
let define = primitive "define" (fun s ->
    match s.Stack with
    | String n :: Quotation q :: t -> { s with Dictionary = Map.add n (Secondary (n, q)) s.Dictionary; Stack = t }
    | String n :: v :: t -> { s with Dictionary = Map.add n (Literal v) s.Dictionary; Stack = t }
    | _ :: _  :: _ -> failwith "Expected qs"
    | _ -> failwith "Stack underflow")
```

As well as an `eval` primitive to invoke the bare `brief` compiler:

```fsharp
let eval = primitive "eval" (fun s ->
    match s.Stack with
    | String b :: t -> (brief s.Dictionary b |> interpret { s with Stack = t } false)
    | _ :: _ -> failwith "Expected s"
    | _ -> failwith "Stack underflow")
```

One issue with the above is that source is `interpreted` in terms of the current `Dictionary`. If the source includes newly `defined` words and then dependent words, this will fail. For the moment we'll live with interpreting one definition at a time by folding over a list of strings of source:

```fsharp
let prelude = [
    "3.14159   'pi   define"
    "2.71828   'e    define"
    "[dup *]   'sq   define"
    "[sq pi *] 'area define" ]

let rep state source = [Literal (String source); eval] |> interpret state false

let preludeState = prelude |> Seq.fold rep primitiveState
```

TODO: Definitions currently must be interpreted entirely before referencing definitions. Words should be processed lazily.

Finally, let's throw in a proper REPL!

```fsharp
let rec repl state = Console.ReadLine() |> rep state |> repl

repl preludeState
```

## 16 DEC 2020 Tesla

At this point we want to start to use Brief to do real things. [Controlling a Tesla vehicle](https://github.com/AshleyF/tesla) sounds like a great "hello world."

```fsharp
let mutable car = None

let auth = primitive "auth" (fun s ->
    match s.Stack with
    | String vin :: String pass :: String name :: t ->
        car <- Some (new Tesla(name, pass, vin))
        { s with Stack = t }
    | _ :: _ :: _ :: _ -> failwith "Expected sss"
    | _ -> failwith "Stack underflow")

let teslaCommand name fn = primitive name (fun s ->
    match car with
    | Some c -> { s with Stack = String (fn c) :: s.Stack }
    | None -> failwith "No Tesla car connected")

let wake = teslaCommand "wake" (fun c -> c.WakeUp())
let honk = teslaCommand "honk" (fun c -> c.HonkHorn())
let flash = teslaCommand "flash" (fun c -> c.FlashLights())
let lock = teslaCommand "lock" (fun c -> c.DoorLock())
let unlock = teslaCommand "unlock" (fun c -> c.DoorUnlock())
let startac = teslaCommand "startac" (fun c -> c.AutoConditioningStart())
let stopac = teslaCommand "stopac" (fun c -> c.AutoConditioningStop())
let getCharge = teslaCommand "charge?" (fun c -> c.ChargeState())
let getClimate = teslaCommand "climate?" (fun c -> c.ClimateState())
let getDrive = teslaCommand "drive?" (fun c -> c.DriveState())
let getGui = teslaCommand "gui?" (fun c -> c.GuiSettings())
let getVehicle = teslaCommand "vehicle?" (fun c -> c.VehicleState())

let setChargeLimit = primitive "charge" (fun s ->
    match car with
    | Some c ->
        match s.Stack with
        | Number limit :: t -> { s with Stack = String (c.SetChargeLimit(int limit)) :: t }
        | _ :: _ -> failwith "Expected n"
        | _ -> failwith "Stack underflow"
    | None -> failwith "No Tesla car connected")

let setTemperature = primitive "temperature" (fun s ->
    match car with
    | Some c ->
        match s.Stack with
        | Number driver :: Number passenger :: t ->
            { s with Stack = String (c.SetTemperatures(float driver, float passenger)) :: t }
        | _ :: _ :: _ -> failwith "Expected nn"
        | _ -> failwith "Stack underflow"
    | None -> failwith "No Tesla car connected")

let dict =
    primitiveState.Dictionary
    |> Map.add "auth" auth
    |> Map.add "wake" wake
    |> Map.add "honk" honk
    |> Map.add "flash" flash
    |> Map.add "lock" lock
    |> Map.add "unlock" unlock
    |> Map.add "startac" unlock
    |> Map.add "stopac" unlock
    |> Map.add "charge?" getCharge
    |> Map.add "climate?" getClimate
    |> Map.add "drive?" getDrive
    |> Map.add "gui?" getGui
    |> Map.add "vehicle?" getVehicle
    |> Map.add "charge" setChargeLimit
    |> Map.add "temperature" setTemperature

let teslaState = { primitiveState with Dictionary = dict }
```

Very cool! However, the `mutable car` is an example of state being maintained in a non-functional way and outside of the machine. Also, some of the words retrieve information asynchronously but block the REPL when they should really report back asynchronously.

Next we should consider an actor model to encapsulate state and allow async communication. Messages as Brief code is an interesting idea!

## 17 DEC 2020 Actor Model

A first cut at an actor model: 

```fsharp
type BriefActor = MailboxProcessor<string>

let actor state : BriefActor =
    BriefActor.Start((fun channel ->
        let rec loop state = async {
            let! input = channel.Receive()
            try return! input |> brief state.Dictionary |> interpret state false |> loop
            with ex -> printfn "Actor Error: %s" ex.Message }
        loop state))
```

Using F#'s `MailboxProcessors`, we create actors that accept fragments of Brief source and evaluate them in their own instance of machine state. This state can contain a custom vocabulary of words that then become the "protocol" for interacting with the actor.

```fsharp
let teslaActor =
    let mutable car = None

    let auth = primitive "auth" (fun s ->
        match s.Stack with
        | String vin :: String pass :: String name :: t ->
            car <- Some (new Tesla(name, pass, vin))
            { s with Stack = t }
        | _ :: _ :: _ :: _ -> failwith "Expected sss"
        | _ -> failwith "Stack underflow")

    let teslaCommand name fn = primitive name (fun s ->
        match car with
        | Some c -> { s with Stack = String (fn c) :: s.Stack }
        | None -> failwith "No Tesla car connected")

    let wake = teslaCommand "wake" (fun c -> c.WakeUp())
    let honk = teslaCommand "honk" (fun c -> c.HonkHorn())
    let flash = teslaCommand "flash" (fun c -> c.FlashLights())
    let lock = teslaCommand "lock" (fun c -> c.DoorLock())
    let unlock = teslaCommand "unlock" (fun c -> c.DoorUnlock())
    let startac = teslaCommand "startac" (fun c -> c.AutoConditioningStart())
    let stopac = teslaCommand "stopac" (fun c -> c.AutoConditioningStop())
    let getCharge = teslaCommand "charge?" (fun c -> c.ChargeState())
    let getClimate = teslaCommand "climate?" (fun c -> c.ClimateState())
    let getDrive = teslaCommand "drive?" (fun c -> c.DriveState())
    let getGui = teslaCommand "gui?" (fun c -> c.GuiSettings())
    let getVehicle = teslaCommand "vehicle?" (fun c -> c.VehicleState())

    let setChargeLimit = primitive "charge" (fun s ->
        match car with
        | Some c ->
            match s.Stack with
            | Number limit :: t -> { s with Stack = String (c.SetChargeLimit(int limit)) :: t }
            | _ :: _ -> failwith "Expected n"
            | _ -> failwith "Stack underflow"
        | None -> failwith "No Tesla car connected")

    let setTemperature = primitive "temperature" (fun s ->
        match car with
        | Some c ->
            match s.Stack with
            | Number driver :: Number passenger :: t ->
                { s with Stack = String (c.SetTemperatures(float driver, float passenger)) :: t }
            | _ :: _ :: _ -> failwith "Expected nn"
            | _ -> failwith "Stack underflow"
        | None -> failwith "No Tesla car connected")

    let dict =
        primitiveState.Dictionary
        |> Map.add "auth" auth
        |> Map.add "wake" wake
        |> Map.add "honk" honk
        |> Map.add "flash" flash
        |> Map.add "lock" lock
        |> Map.add "unlock" unlock
        |> Map.add "startac" unlock
        |> Map.add "stopac" unlock
        |> Map.add "charge?" getCharge
        |> Map.add "climate?" getClimate
        |> Map.add "drive?" getDrive
        |> Map.add "gui?" getGui
        |> Map.add "vehicle?" getVehicle
        |> Map.add "charge" setChargeLimit
        |> Map.add "temperature" setTemperature

    let teslaState = { primitiveState with Dictionary = dict }

    actor teslaState
```

Example: `"'foo@bar.com 'MyPassword 'MyVin auth" 'tesla post`, `"honk" 'tesla post`

This helps to encapsulate Tesla-specific state (e.g. the `mutable car`) and to scope the Tesla-specific vocabulary of words.

How are these actors created and wired together? How do they know about each other? The ROS model of a "master" service acting as a liason where "topics" can be published and subscribed to is interesting. A more direct dependency injection style system where actors are passed "channels" on which to communicate is interesting too.

The `teslaActor` (created by `actor teslaState`) could be used directly in F# code with `teslaActor.Post("honk")` for example. But it would be nice to be able to interact with it from the REPL. A Tesla-specific `postTesla` primitive word could be created. Instead, let's try a central `registry` to which we can `register` new `BriefActors` (e.g. `register "tesla" teslaActor`):

```fsharp
let mutable (registry: Map<string, BriefActor>) = Map.empty

let register name actor = registry <- Map.add name actor registry
```

Then a general word to post to a known actor by name:

```fsharp
let post = primitive "post" (fun s ->
    match s.Stack with
    | String n :: String b :: t ->
        match Map.tryFind n registry with
        | Some actor -> actor.Post b; { s with Stack = t }
        | None -> failwith "Actor not found"
    | _ :: _  :: _ -> failwith "Expected ss"
    | _ -> failwith "Stack underflow")
```

DEBATE 14: Should actors use a ROS-style "master" or [Clojure core.async-style "channels"](https://clojure.org/news/2013/06/28/clojure-clore-async-channels)?

DEBATE 15: Should actors communicate with source? No. Communication should be more structured. Perhaps parsed Brief, but containing words that are unknown to the host (e.g. `honk` is known only within the `teslaAgent`).

DEBATE 16: This is fine for stand-along actors. How are actors to be wired together into a graph?

## 20 DEC 2020 Binary Format

NOTE: This was removed later in favor of plain source as the storage and conveyance format for now.

Thinking about making actors that span processes and/or machines: should the protocol simply be Brief *source* code (DEBATE 15 above)? Or should there be a simpler and more compact binary representation. In fact, should the Brief machinery be defined in terms of a "byte code"?

DEBATE: 17: Should Brief machinery be defined in terms of a byte code?

Conveying `Word lists` is what we're talking about. We'll need to encode `Values`, including compound `Lists`, `Maps`, and `Sets` (and recursive `Quotations` themselves), and we'll need to encode `Primitive` and `Secondary` words.

`Primitive` words are built into the machine. These should be as few as possible/practical and should be identified by name or some unique number (GUID?) known across machines. Some actors add new `Primitives` (such as Tesla's `honk`) and so need to be able to extend the known set; either by name or otherwise. Either we use something truely globally unique like a GUID or we add namespaces and such (e.g. `brief.dup`, `tesla.honk`).

`Secondaries` can be defined on the host actor and then conveyed by name. Or again, perhaps must be defined with a GUID or namespace.

Both `Primitive` and `Secondary` words should be able to be conveyed without understanding them. Imagine a "relay" actor, forwarding code to other actors for interpretation or some algebraic manipulation of program structure at a higher level. So, the format should support words as "symbols" being conveyed without a necessary mapping to implementation.

In the future there may be name scopes as well for secondaries; that is, sequences of words given a name for clarity or factoring out redundancy but then referred to by name only within a "parent" word. For example `area` could define `sq` and `pi` as children and not polute the dictionary with these names - only exposing `area`.

Atomic `Values` are easy enough to encode: `Numbers` as IEEE754, `Strings` as length-prefixed UTF-8, `Booleans` as a simple byte (-1, 0), ... Compound `Values` are not much more difficult. `Lists` could be a length followed by n-values, a `Map` could be a length followed by n-pairs of `String`/`Value` and `Sets` could be a length followed by n-distinct `Values`.

In the language itself, there will be a means of composing compound `Values` from atomic ones. An `empty-list` word will push an empty `List` while a `cons` word will add a `Value` to the head; building a list in reverse. Encoding a list this way would use words already available in the machine. Perhaps an `n-cons` word could cons n-values onto a list as a more compact representation. `empty-list 'a 'b 'c 3 n-cons` vs `empty-list 'a cons 'b cons 'c cons`.

Yet another encoding could mimic the source format more closely with begin/end delimiters.

DEBATE 18: How exactly should compound `Values` be encoded?

Looking at the original definition of `Words` and `Values`:

```fsharp
and Word =
    | Literal   of Value
    | Primitive of (State -> State)
    | Secondary of Word list

and Value =
    | Number    of double
    | String    of string
    | Boolean   of bool
    | List      of Value list
    | Map       of Map<string, Value>
    | Set       of Set<Value>
    | Quotation of Word list
```

Encoded `Words` are then reduced to just `Literal of Value | Symbol of string` and `Values` include only atomic ones: `Number of double | String of string | Boolean of bool`. Compound `Lists`, `Maps`, `Sets` and `Quotations` are defined using `Symbols` representing words to build these compounds from atomic pieces.

```fsharp
type Token =
    | Sym  of string
    | QSym of string
    | Num  of double
    | Str  of string
    | Bool of bool
```

Symbols (`Sym`) are identical to strings (`Str`), except that they represent words and are meant to be interpreted (eventually) and contain no whitespace in Brief syntax. They need a separate representation in the token stream. Even though they have the same underlying type as `Tokens`, they have very different symantics and slightly different syntax. In fact, how will we encode `Quotations`? Lists of `Words`, which are not themselves `Values`. This is what `QSym` is, a "quoted symbol."

```fsharp
let rec tokensOfWords words = seq {
    let rec tokensOfValue v = seq {
        match v with
        | Number    n -> yield Num n
        | String    s -> yield Str s
        | Boolean   b -> yield Bool b
        | List l ->
            yield Sym "list"
            for v in List.rev l do
                yield! tokensOfValue v
                yield Sym "cons"
        | Map m ->
            yield Sym "map"
            for kv in m do
                yield! tokensOfValue kv.Value
                yield Str kv.Key
                yield Sym "add"
        | Quotation q ->
            yield Sym "quote"
            for w in List.rev q do
                yield! tokensOfWords [w]
                yield Sym "qcons" }
    match words with
    | Literal v :: t -> yield! tokensOfValue v; yield! tokensOfWords t
    | Primitive p :: t-> yield Sym p.Name; yield! tokensOfWords t
    | Secondary (n, _) :: t-> yield Sym n; yield! tokensOfWords t
    | [] -> () }
```

Any sequence of `Words` (even hierarcical `Quotations`) can be reduced to a flat sequence of these tokens and then serialized to bytes quite straight forwardly.

Before jumping in, let's look at [Manfred von Thun's the Flat-Joy (Floy) paper](https://hypercubed.github.io/joy/html/jp-flatjoy.html). He too used quotations of single words, each followed by a `concat` and even proposed a "Q foo" syntax for what is essentially the same thing as our `QSym`.

The problem to sleep on tonight is whether `Words` should be data; something that can be pushed to the stack. Perhaps symbols are a value directly. Maybe symbols remain lazy until evaluation-time. Maybe `Quotations` and `Lists` are one and the same? The fact that Joy and XY make a distinction tells me there must be a reason...

## 21 DEC 2020 Simplification

After thinking about it, it seems that `Words` and `Values` can be furture unified and no distinction is needed between `Quotations` and regular `Lists`. This seems to work out *much* simpler. The structure has now been reduced to:

```fsharp
type Value =                          // v
    | Symbol    of string             // y
    | Number    of double             // n
    | String    of string             // s
    | Boolean   of bool               // b
    | List      of Value list         // l
    | Map       of Map<string, Value> // m
```

`Words` go away and are replaced by `Symbols` in the `Value` union type. They are distinct from `Strings` in that they do represent words in the language. `Quotation` also go away and are now just `Lists`.

```fsharp
and State = {
    Continuation: Value list
    Stack: Value list
    Map: Map<string, Value>
    Dictionary: Map<string, Value>
    Primitives: Map<string, (State -> State)> }
```

 `NamedPrimitives` also go away and `Primitives` are added to the `State` as simple `(State -> State)` functions.

The `Dictionary` is now a mapping of named `Values` rather than words but again is meant to contain word definitions, as opposed to the `Map` which is meant as user-space to store values:

Compiling no longer produces `Words`. Tokens representing literal values are merely emitted as `Values` rather than `Literal Values`. Words are emitted as `Symbols` without conversion to a primitive or secondary.

```fsharp
let rec compile nodes = seq {
    match nodes with
    | Token t :: n ->
        match Double.TryParse t with
        | (true, v) -> yield Number v
        | _ ->
            match Boolean.TryParse t with
            | (true, v) -> yield Boolean v
            | _ ->
                if t.StartsWith '\'' then yield (if t.Length > 1 then t.Substring(1) else "") |> String
                else yield Symbol t
        yield! compile n
    | Quote q :: n ->
        yield List (compile q |> List.ofSeq)
        yield! compile n
    | [] -> () }
```

Instead, now `interpret` is responsible for giving `Symbols` meaning; attempting to find them in the `Dictionary` or in the set of `Primitives` at interpretation-time. Notice that this means that `compiled` code can now be held and passed around even when the primitives are unknown (e.g. the Tesla actor's internal primitives).

```fsharp
let rec interpret state debug stream =
    let word state = function
        | Symbol s ->
            match Map.tryFind s state.Dictionary with
            | Some (List l) -> { state with Continuation = l @ state.Continuation }
            | Some v -> { state with Continuation = v :: state.Continuation }
            | None ->
                match Map.tryFind s state.Primitives with
                | Some p -> p state
                | None -> failwith (sprintf "Unknown word '%s'" s)
        | v -> { state with Stack = v :: state.Stack }
    ...
```

For example, `Actors` now take messages of `Value list` instead of `string` and now `interpret` them directly. For example, to control the Tesla we now post quotations (`Lists`) to the actor (just replacing `"` syntax with square brackets, but _structurally_ much different): `['foo@bar.com 'MyPassword 'MyVin auth] 'tesla post`, `[honk] 'tesla post`

## 22 DEC 2020 IFTTT

The If-This-Then-That (IFTTT) service allows connecting events from various devices to actuations of various other devices. One event sensing "device" is a ["webhook"](https://ifttt.com/maker_webhooks). Once an account is setup, a webhook can be created and assigned a key. Triggering the webhook is a simple matter of an HTTP GET request to the a URL in the form: `https://maker.ifttt.com/trigger/<event>/with/key/<key>` where the `<event>` is some name you invent (e.g. `lights-on`, `lights-off`) and the `<key>` is the one assigned by IFTTT. Named events can then be wired to devices in the IFTTT interface (e.g. If light-on then HUE Lights: On). Additionally, some devices accept parameters and these can be posted by appending `?value1=<val1>&value2=<val2>&value3=<val3>` to the URL. These values can be used in IFTTT triggers as "ingredients" in their UI.

Let's build a new actor for this:

```fsharp
let triggerActor =
    let triggerState =
        let mutable (primitives : Map<string, (State -> State)>) = primitiveState.Primitives
        let primitive name fn = primitives <- Map.add name fn primitives

        let triggerEvent event val1 val2 val3 key = async {
            use client = new HttpClient(Timeout = TimeSpan.FromMinutes 1.)
            client.Timeout <- TimeSpan.FromMinutes 1.
            let url = sprintf "https://maker.ifttt.com/trigger/%s/with/key/%s?value1=%s&value2=%s&value3=%s" event key val1 val2 val3
            use! response = Async.AwaitTask (client.GetAsync(url))
            response.EnsureSuccessStatusCode() |> ignore }

        primitive "hook" (fun s ->
            match s.Stack with
            | String key :: String val3 :: String val2 :: String val1 :: String event :: t ->
                triggerEvent event val1 val2 val3 key |> Async.RunSynchronously
                { s with Stack = t }
            | _ :: _ :: _ :: _ :: _ :: _ -> failwith "Expected ssss"
            | _ -> failwith "Stack underflow")

        { primitiveState with Primitives = primitives }

    actor triggerState
```

We don't want to expose the IFTTT key or the Tesla credentials in source. We also don't want to have to type them repeatedly. Let's add the ability to pass Brief source to the executable on startup:

```fsharp
let commandLine =
    let exe = Environment.GetCommandLineArgs().[0]
    Environment.CommandLine.Substring(exe.Length)

commandLine :: prelude |> Seq.fold rep primitiveState |> repl
```

Then we can add to the application arguments something like:

```brief
[['foo@bar.com 'MyPassword 'MyVin auth] 'tesla post] 'tesla-auth define
['MyKey 'ifttt-key define] 'trigger post
[[' ' ' ifttt-key hook] 'ifttt define] 'trigger post
```

Notice that we define a `tesla-auth` word in the REPL's dictionary that `posts` to the `tesla` actor. Even more interestingly, we define the `ifttt-key` _in the `trigger` dictionary_ by posting a `define` to the actor. The same goes for the `ifttt` word which makes triggering without value arguments more convenient.

Finally, we can now say something like `tesla-auth [honk] 'tesla post` or `['lights-on ifttt] 'trigger post`.

## 23 DEC 2020 _Reverse_ Reverse Polish Notation

Let's try reversing the syntax to see how it feels for a while. It's like prefix-notation Lisp now, but without the parenthesis. Fixed aritiy words still lead to a concise (brief) syntax but prefix notation may read better:

```brief
define 'area [* pi sq]
define 'pi 3.14159
define 'sq [* dup]
```

The semantics are the same however with tokens processed in reverse right-to-left. Source can also be thought of as being processed from bottom-to-top with definitions being _followed_ (in English reading order) by their dependencies. This could be useful later once name scopes are used. For example the `area` word could contain the `pi` and `sq` dependencies as children and offsides-rule indenting:

```brief
define 'area [* pi sq]
    define 'pi 3.14159
    define 'sq [* dup]
```

To make this change, the `lex` function merely reverses the sequence of tokens (`... |> Seq.rev`). Also, the `parser` changed to give opposite meaning to `[` and `]` as well as `{` and `}` tokens. The `parser` also now reverses the tokens within `Lists` and `Maps` so that they're treated in the order written. When a definition is expanded onto the `Continuation` it is reversed (again) for interpretation. In a couple of places, parameters were rearranged to be in a more "natural" order (e.g. `Tesla.auth` still expects user name and password in that order in source).

Also today, we added syntax for maps in the form `{ 'key1 <value>  'key2 <value> }`. Brace-delimited sets of pairs of `String` and `Value`, that's it. Conventionally, we'll put whitespace around `{` and `}` tokens (though not required, same as list syntax) and two spaces between name/value pairs when on the same line as other pairs (again, a single space is enough for the lexer).

## 25 DEC 2020 Remoting

Using some old code [from VimSpeak](https://github.com/AshleyF/VimSpeak), let's try controlling Brief with our voice!

The `System.Speech` APIs require .NET Framework, while Brief has been written for .NET Core. For simplicity, let's build a speech app as [a separate project here](https://github.com/AshleyF/brief/tree/gh-pages/speech). Speech reco is aweful for dictation, but works quite well with a grammar. This grammar building code is very convenient:

```fsharp
type GrammarAST<'a> =
    | Phrase   of string * string option
    | Optional of GrammarAST<'a>
    | Sequence of GrammarAST<'a> list
    | Choice   of GrammarAST<'a> list
    | Dictation

let rec speechGrammar = function
    | Phrase (say, Some value) ->
        let g = new GrammarBuilder(say)
        g.Append(new SemanticResultValue(value))
        g
    | Phrase (say, None) -> new GrammarBuilder(say)
    | Optional g -> new GrammarBuilder(speechGrammar g, 0, 1)
    | Sequence gs ->
        let builder = new GrammarBuilder()
        List.iter (fun g -> builder.Append(speechGrammar g)) gs
        builder
    | Choice cs -> new GrammarBuilder(new Choices(List.map speechGrammar cs |> Array.ofList))
    | Dictation ->
        let dict = new GrammarBuilder()
        dict.AppendDictation()
        dict
```

With it, we can build a grammar for controlling the lights in the house using IFTTT:

```fsharp
let briefLightsOn = "post 'trigger [hook ifttt-key 'all-lights-on ' ' ']"
let briefLightsOff = "post 'trigger [hook ifttt-key 'all-lights-off ' ' ']"
let briefLightsDim = "post 'trigger [hook ifttt-key 'all-lights-dim '50 ' ']"
let briefLightsBright = "post 'trigger [hook ifttt-key 'all-lights-dim '100 ' ']"

let lightsOn = Choice [
    Phrase ("Illuminate",         Some briefLightsOn)
    Phrase ("Turn on",            Some briefLightsOn)
    Phrase ("Lights on",          Some briefLightsOn)
    Phrase ("Turn lights on",     Some briefLightsOn)
    Phrase ("Turn the lights on", Some briefLightsOn)]

let lightsOff = Choice [
    Phrase ("Turn off",            Some briefLightsOff)
    Phrase ("Lights off",          Some briefLightsOff)
    Phrase ("Turn lights off",     Some briefLightsOff)
    Phrase ("Turn the lights off", Some briefLightsOff)]

let lightsDim = Choice [
    Phrase ("Dim",            Some briefLightsDim)
    Phrase ("Dim lights",     Some briefLightsDim)
    Phrase ("Lights dim",     Some briefLightsDim)
    Phrase ("Dim the lights", Some briefLightsDim)]

let lightsBright = Choice [
    Phrase ("Bright",              Some briefLightsBright)
    Phrase ("Brighten",            Some briefLightsBright)
    Phrase ("Bright lights",       Some briefLightsBright)
    Phrase ("Lights bright",       Some briefLightsBright)
    Phrase ("Brigten lights",      Some briefLightsBright)
    Phrase ("Brighten the lights", Some briefLightsBright)]

let grammar = new Grammar(Choice [lightsOn; lightsOff; lightsDim; lightsBright] |> speechGrammar)
reco.LoadGrammar(grammar)

while true do
    let res = reco.Recognize()
    if res <> null then
        printfn "Sync Reco: %s %f" res.Text res.Confidence
        let sem = if res.Semantics.Value = null then None else Some (res.Semantics.Value :?> string)
        match sem with
        | Some s -> if res.Confidence > 0.7f then post s
        | None -> synth.Speak("What?")
```

Notice that the semantic values set on the various phrases are just fragments of Brief source (e.g. `post 'trigger [hook ifttt-key 'all-lights-on ' ' ']`). In the future, we will make this a proper Brief actor and add primitives for defining grammars and binding speech phrases to Brief quotations.

The `post` function writes the source to a TCP socket if anyone is listening:

```fsharp
let mutable writer : BinaryWriter option = None
let server () =
    let listener = new TcpListener(IPAddress.Loopback, 11411)
    listener.Start()
    while true do
        try
            writer <- Some (new BinaryWriter(listener.AcceptTcpClient().GetStream()))
        with ex -> printfn "Connection Error: %s" ex.Message
(new Thread(new ThreadStart(server), IsBackground = true)).Start()

let post brief =
    printfn "Brief: %s" brief
    match writer with
    | Some w -> w.Write(brief)
    | None -> printf "No connection"
```

This little Speech app isn't tightly integrated with Brief at all. It just sends strings that happen to be valid Brief source. In the future we want to host the Brief "engine" within separate processes and shuttle code over the "wire" in some specific serialization format (or perhaps in string form).

Next we add a simple `remoteActor` that listens on a TCP socket and executes code off the wire:

```fsharp
let remoteActor host port =
    let channel = actor primitiveState
    let reader = new BinaryReader((new TcpClient(host, port)).GetStream())
    let rec read () =
        let source = reader.ReadString()
        printfn "Remote: %s" source
        source |> brief |> channel.Post
        read ()
    (new Thread(new ThreadStart(read), IsBackground = true)).Start()
```

Adding this to the main program, we have successfully cobbled together a complete system to control the lights using `System.Speech` and the `trigger` actor executing, all glued together with Brief.

```fsharp
let speech = Remote.remoteActor "127.0.0.1" 11411
```

---

Let's control the vaccuume:

```fsharp
let roombaCleanAll = "'Cleaning the floors|post 'trigger [hook ifttt-key 'roomba-clean-all ' ' ']"
let roombaDock = "'Docking the vaccuume|post 'trigger [hook ifttt-key 'roomba-dock ' ' ']"

let roomba = Choice [
    Phrase ("Clean the floors", Some roombaCleanAll)
    Phrase ("Clean floors", Some roombaCleanAll)
    Phrase ("Go back to the dock", Some roombaDock)
    Phrase ("Go to the dock", Some roombaDock)]
```

This is going to be fun! We really need to add the ability to author grammars in Brief. Soon...

## 26 DEC 2020 Combinators, Conditionals, etc.

In preparation for adding to the language, let's move the `Prelude.fs` to a simple text file (`Prelude.b` -- `.b` for Brief) and add the following primitive to load Brief source files:

```fsharp
primitive "load" (fun s ->
    match s.Stack with
    | String p :: t -> File.ReadAllText(sprintf "%s.b" p) |> rep { s with Stack = t }
    | _  :: _ -> failwith "Expected s"
    | _ -> failwith "Stack underflow")
```

We'll be adding new primitives and new secondaries. Sometimes we'll have a choice to make. For example, `over` could be defined as:

 ```fsharp
primitive "over" (fun s ->
    match s.Stack with
    | x :: y :: t -> { s with Stack = y :: x :: y :: t }
    | _ -> failwith "Stack underflow")
 ```

Or we could define it in terms of other primitives: `let 'over [swap dip [dup]]`. This is less efficient, but Brief is not meant to be a high-performance language. For now, we'll favor secondaries and optimize later as needed. This will keep the implementation small. We may later port this to Python, JavaScript, etc. and a small implementation will be appreciated.

### Combinators

Let's add some of the combinators, [inspired by Aaron Bull Schaefer's Factor blog post](https://elasticdog.com/2008/12/beginning-factor-shufflers-and-combinators/). We have `drop`, `dup`, and `swap` aleardy. We can add versions that work with pairs or tripples of values and we can add the `keep` word:

```brief
let '3keep [3dip dip [3dup]]
let '2keep [2dip dip [2dup]]
let 'keep [dip dip [dup]]

let '3drop [2drop drop]
let '2drop [drop drop]

let '3dup [dup 2dup]
let '2dup [over over]

let '3dip [dip [2dip] swap]
let '2dip [dip [dip] swap]
```

Maybe add `pick` and `nip`, `tuck`, `over`, `rot` and `-rot`

```fsharp
primitive "pick" (fun s ->
    match s.Stack with
    | x :: y :: z :: t -> { s with Stack = z :: x :: y :: z :: t }
    | _ -> failwith "Stack underflow")
```

```brief
let 'over [swap dip [dup]]
let '2over [pick pick]
let 'nip [drop swap]
let 'tuck [over swap]
let 'rot [swap dip [swap]]
let '-rot [dip [swap] swap]
```

Then let's add the extremely useful cleave, spread and apply combinators:

```brief
let 'bi [apply dip [keep]]
let '2bi [apply dip [2keep]]
let '3bi [apply dip [3keep]]
let 'bi* [apply dip [dip]]
let '2bi* [apply dip [2dip]]
let 'bi@ [apply 2dip dup]
let '2bi@ [apply 3dip dup]

let 'tri [apply dip [keep] 2dip [keep]]
let '2tri [apply dip [2keep] 2dip [2keep]]
let '3tri [apply dip [3keep] 2dip [3keep]]
let 'tri* [apply dip [dip] 2dip [2dip]]
let '2tri* [apply dip [2dip] 2dip [4dip]]
let 'tri@ [apply 2dip dup 3dip dup]
let '2tri@ [apply 4dip dup]
```

We can then start adding interesting words in terms of these:

```brief
let 'both? [and bi@]
let 'either? [or bi@]
let 'neither? [not or bi@]
```

### Conditionals

The `if` primitive will serve as our conditional. In a completely prefix-notation manner, with no special syntax or semantics, this selects one or the other quotation (`Lists`) based on a `Boolean` value.

```fsharp
primitive "if" (fun s ->
    match s.Stack with
    | List q :: List r :: Boolean b :: t ->
        { s with Stack = t; Continuation = List.rev (if b then q else r) @ s.Continuation }
    | _ :: _ :: _ -> failwith "Expected vq"
    | _ -> failwith "Stack underflow")
```

To execute something only `when` true or `unless` it's true, just pass an empty quotation (`[]`) to do nothing in one or the other branch:

```brief
let 'when [if swap []]
let 'unless [if []]
```

Unconditional application of a quotation is just:

```brief
let 'apply [when swap true]
```

To make logical choices, we need boolean algebra operations and comparison to map regular values to `Booleans`.

```fsharp
let booleanOp name op = primitive name (fun s ->
    match s.Stack with
    | Boolean x :: Boolean y :: t -> { s with Stack = Boolean (op x y) :: t }
    | _ :: _ :: _ -> failwith "Expected bb"
    | _ -> failwith "Stack underflow")

booleanOp "and" (&&)
booleanOp "or" (||)

primitive "not" (fun s ->
    match s.Stack with
    | Boolean x :: t -> { s with Stack = Boolean (not x) :: t }
    | _ :: _ -> failwith "Expected b"
    | _ -> failwith "Stack underflow")
```

TODO Reduce boolean operations to `nand`, from which we derrive`and`, `or`, `xor`, `not`, `nor` and `xnor`.

Basic comparison operations:

```fsharp
let comparisonOp name op = primitive name (fun s ->
    match s.Stack with
    | x :: y :: t -> { s with Stack = Boolean (op x y) :: t }
    | _ -> failwith "Stack underflow")

comparisonOp "=" (=)
comparisonOp ">" (>)
```

Then define `<`, `<=`, `>=`, and `<>` in terms of these:

```brief
let '< [not or 2bi [>] [=]]
let '<= [or 2bi [<] [=]]
let '>= [or 2bi [>] [=]]
let '<> [not =]
```

### Miscellaneous

Removing the F# implementations of `neg` and `abs`:

```fsharp
unaryOp "neg" (fun n -> -n)
unaryOp "abs" (fun n -> abs n)
```

And replacing with Brief implementations:

```brief
let 'neg [- 0]
let 'abs [when [neg] < 0 dup]
```

Plus a few more:

```brief
let 'sign [min 1 max -1]
let 'min [drop when [swap] < 2dup]
let 'max [drop when [swap] > 2dup]
```

It's pretty fun filling out the language and starting to really code in Brief itself instead of F#!

## 27 DEC 2020 Type Casting, (De)Construction

Let's continue to fill out the language with type casts, 

### Type Casts

To determine the type of a value on the stack:

```fsharp
primitive "type" (fun s ->
    let kind, t =
        match s.Stack with
        | Symbol  _ :: t -> "sym", t
        | Number  _ :: t -> "num", t
        | String  _ :: t -> "str", t
        | Boolean _ :: t -> "bool", t
        | List    _ :: t -> "list", t
        | Map     _ :: t -> "map", t
        | [] -> failwith "Stack underflow"
    { s with Stack = String kind :: t })
```

And helpers for each type:

```brief
let 'list? [= 'list type]
let 'sym?  [= 'sym  type]
let 'num?  [= 'num  type]
let 'str?  [= 'str  type]
let 'bool? [= 'bool type]
let 'list? [= 'list type]
let 'map?  [= 'map  type]
```

Casting values between types: `Numbers`, `Booleans`, and `Strings` (that don't contain white space) may be cast to `Symbols`:

```fsharp
primitive ">sym" (fun s ->
    match s.Stack with
    | Symbol _ :: _ -> s
    | String str :: t ->
        if str |> Seq.exists (System.Char.IsWhiteSpace)
        then failwith "Symbols cannot contain whitespace."
        else { s with Stack = Symbol str :: t }
    | List _ :: t -> failwith "Lists cannot be cast to a Symbol value."
    | Map _ :: t -> failwith "Maps cannot be cast to a Symbol value."
    | v :: t -> { s with Stack = Symbol (stringOfValue v) :: t }
    | _ -> failwith "Stack underflow")
```

`Symbols` and `Strings` may be parsed as `Numbers`. `Booleans` can be cast with the rule that `true` is -1 (all bits on) and `false` is 0. `Lists` and `Maps` cast to the length of the collection.

```fsharp
primitive ">num" (fun s ->
    match s.Stack with
    | Number _ :: _ -> s
    | Symbol y :: t | String y :: t ->
        match System.Double.TryParse y with
        | (true, v) -> { s with Stack = Number v :: t }
        | _ -> failwith "Cannot cast to Number"
    | Boolean b :: t -> { s with Stack = Number (if b then -1. else 0.) :: t}
    | List l :: t -> { s with Stack = Number (List.length l |> float) :: t }
    | Map m :: t -> { s with Stack = Number (Map.count m |> float) :: t }
    | _ -> failwith "Stack underflow")
```

Any value can be cast to a `String` as essentially Brief source format. `Strings` themselves, however, retain their value without being surrounded by quotes (`"`) or prefixed with a tick (`'`).

```fsharp
primitive ">str" (fun s ->
    match s.Stack with
    | String _ :: _ -> s
    | v :: t -> { s with Stack = String (stringOfValue v) :: t }
    | _ -> failwith "Stack underflow")
```

`Symbols` and `Strings` may be parsed as `Booleans`. `Numbers` become `Booleans` with the rule that zero is `false` and any other value (expecially -1) is `true`. `Lists` and `Maps` convert indicating whether the collection is empty.

```fsharp
primitive ">bool" (fun s ->
    match s.Stack with
    | Boolean _ :: _ -> s
    | Symbol y :: t | String y :: t ->
        match System.Boolean.TryParse y with
        | (true, v) -> { s with Stack = Boolean v :: t }
        | _ -> failwith "Cannot cast to Number"
    | Number n :: t -> { s with Stack = Boolean (n <> 0.) :: t }
    | List l :: t -> { s with Stack = Boolean (List.isEmpty l |> not) :: t }
    | Map m :: t -> { s with Stack = Boolean (Map.isEmpty m |> not) :: t }
    | _ -> failwith "Stack underflow")
```

Values cannot generally be cast to a `List` or `Map`. One potential could be `Lists` of pairs of name/value converted to/from `Maps`

DEBATE 19: Should be support `>map` of `Lists` of name/value pairs and visa versa?

One exception we'll make is to convert `Strings` and `Symbols` to `Lists` of single-character `Strings`. Rather than name this `>list`, we'll name it `split` because `>string` doesn't do the reverse. Instead `join` will do the reverse (although still missing symmetry with `join split <symbol>` producing a `String` rather than a `Symbol`).

```fsharp
primitive "split" (fun s ->
    match s.Stack with
    | Symbol y :: t | String y :: t -> { s with Stack = (y |> Seq.toList |> List.map (string >> String) |> List) :: t }
    | _ :: _ -> failwith "Expected s"
    | _ -> failwith "Stack underflow")

primitive "join" (fun s ->
    let str = function String y | Symbol y -> y | _ -> failwith "Expected List of Strings/Symbols"
    match s.Stack with
    | List l :: t -> { s with Stack = (l |> Seq.map str |> String.concat "" |> String) :: t }
    | _ :: _ -> failwith "Expected l"
    | _ -> failwith "Stack underflow")
```

### List and Map Words

Counting the number of elements in a `List` or `Map` can be done by:

```fsharp
primitive "count" (fun s ->
    match s.Stack with
    | (List l :: _) as t -> { s with Stack = (List.length l |> double |> Number) :: t }
    | (Map m :: _) as t -> { s with Stack = (Map.count m |> double |> Number) :: t }
    | _ :: _ -> failwith "Cannot cast to List"
    | _ -> failwith "Stack underflow")
```

With this we can create `empty?`. We could've used `>bool` instead, but that would work with types other than collections with strange behavior (e.g. `empty? 0` -> `true`?!).

```brief
let 'empty? [= 0 count]
```

Pushing an empty list of course is just the literal `[]`. Adding elements to this can be done by "consing" onto the head (e.g. `cons 1 [2 3]` -> `[1 2 3]`). The reverse of this we'll call `snoc` (e.g. `snoc [1 2 3]` -> `1 [2 3]`).

```fsharp
primitive "cons" (fun s ->
    match s.Stack with
    | v :: List l :: t -> { s with Stack = List (v :: l) :: t }
    | _ :: _ :: _ -> failwith "Expected vl"
    | _ -> failwith "Stack underflow")

primitive "snoc" (fun s ->
    match s.Stack with
    | List (h :: t') :: t -> { s with Stack = h :: List t' :: t }
    | List _ :: _ -> failwith "Expected non-empty list"
    | _ :: _ :: _ -> failwith "Expected vl"
    | _ -> failwith "Stack underflow")
```

With `snoc` we get both the head and tail of the list. Individual words will be useful:

```brief
let 'head [nip snoc]
let 'tail [drop snoc]
```

Obviously, plenty of other operations will be added for sorting, reversing, filtering, folding, mapping, indexing into, etc. These can be built from primitives I believe.

For `Maps` we need to be able to query for the existence of a key and to retrieve values by key, as well as to add key/value pairs. Perhaps we'll take over the fetch (`@`) and store (`!`) words for this and rename the existing ones to `map@` and `map!` (maybe these should go away and a single word to retrieve the machine state `Map` should be added). For example: `! 'foo 123 {}` -> `{ 'foo 123 }`, `@ 'foo { 'foo 123 }` -> `123 { 'foo 123 }`, `key? 'foo { 'foo 123 }` -> `true { 'foo 123 }`.

```fsharp
primitive "key?" (fun s ->
    match s.Stack with
    | String k :: (Map m :: _ as t) -> { s with Stack = Boolean (Map.containsKey k m) :: t }
    | _ :: _ :: _ -> failwith "Expected sm"
    | _ -> failwith "Stack underflow")

primitive "@" (fun s ->
    match s.Stack with
    | String k :: (Map m :: _ as t) ->
        match Map.tryFind k m with
        | Some v -> { s with Stack = v :: t }
        | None -> failwith "Key not found"
    | _ :: _ :: _ -> failwith "Expected sm"
    | _ -> failwith "Stack underflow")

primitive "!" (fun s ->
    match s.Stack with
    | String k :: v :: Map m :: t -> { s with Stack = Map (Map.add k v m) :: t }
    | _ :: _ :: _ :: _ -> failwith "Expected vsm"
    | _ -> failwith "Stack underflow")
```

## 28 DEC 2020 Flow Control

Let's try implimenting a `reverse` word to reverse the contents of a list. The purely functional strategy will be to peel off values and build a new list in reverse. First thought:

```brief
let 'rev [if [drop] [rev dip [cons] swap snoc] empty? dup]
let 'reverse [rev swap []]
```

The `reverse` word merely slips an initial empty list under the list being reversed and calls `rev` which peels off values (`snoc`), slips them under (`swap`), and `conses` onto the new list. It does this while the list has more to peel off (`empty?`) and otherwise `drops` the now empty list. This brings up a point about words defined in terms of "internal" words like here `reverse` using `rev` but this `rev` word isn't very useful on its own and doesn't need to be exposed.

DEBATE 20: Should we add name scoping and perhaps an "off sides" rule?

Trying to factor out the general pattern, let's define a `while` word to apply a quotation while another quotation is `true`. The `do` word will apply the second quotation once while keeping both quotations. This can be used with `while` to apply once up front (e.g. `while do ...`) and is used inside the `while` word itself to recursively iterate.

```brief
let 'do [2dip dup]
let 'while [if [while do] [2drop] rot dip [dip dup]]
```

Now, using our out `while` word, we can redefine `reverse`:

```brief
let 'reverse [drop while [dip [cons] swap snoc] [not empty?] swap []]
```

Slip the initial empty list under (`swap []`) and then `while` the list is `not empty?` we peel off a value (`swap snoc`) and prepend it to the reverse list (`dip [cons]`). This is a bit more clear and doesn't require an "internal" word and doesn't even expose that it's using recursion (`reverse` doesn't refer to itself; `while` does).

How about we add an `until` word which is essentially the same as `while` but with the condition reversed (with `dip [compose [not]]`:

```brief
let 'until [while dip [compose [not]]]
```

Then `reverse` can be written a bit more naturally as `until ... empty?` rather than `while ... not empty?`:

```brief
let 'reverse [drop until [dip [cons] swap snoc] [empty?] swap []]
```

This is fun!

## 29 DEC 2020 Map, Fold, Filter

Let's add the triumverate of functional programming, `map`, `fold` and `filter`. Starting with `fold`:

```brief
let 'fold [if [2drop] [fold dip dup 2dip [snoc] -rot] empty? rot]
```

This recursive definition is simple enough. It expects arguments in the form `<function> <seed> <list>`. For example, `[+] 0 [1 2 3]`. It will apply the `<function>` to the `<seed>` and the first element of the list (`snoced` off), then again to the result and the next element of the list until it's `empty?`. "Until it's `emtpty?`" Hmmm... perhaps we can redefine in terms of `until` and avoid the direct recursion; about equally simple:

```brief
let 'fold [2drop until [dip dup 2dip [snoc] -rot] [empty? rot]]
```

And now `reverse` can be redefined once again as quite simply: `let 'reverse [fold [cons swap] []]`.

We can also define a couple of simple but possibly useful words to compute the `sum` or `product` of a list of numbers:

```brief
let 'sum [fold [+] 0]
let 'product [fold [*] 1]
```

Funny enough, `map` is very similar. For example, to multiply each element by two: `fold [cons * 2 swap] []` and the `reverse` the result. The goal is to make an expression like `map [* 2] [1 2 3]` produce `[2 4 6]`. We just need to `compose` a `cons` before and a `swap` after (`compose swap [swap] compose [cons]`), then `swap` and empty list behind this (`swap []`), then `fold` over the list and `reverse` the result:

```brief
let 'map [reverse fold swap [] compose swap [swap] compose [cons]]
```

Next we define `filter`, which removes elements from a list depending on whether a quotation returns true. For example, `filter [even?] [1 2 3 4 5]` would produce `[2 4]` where `let 'even? [= 0 mod swap 2]`. The first thought is to first `map` the elements to single-element or empty lists with `map [if [cons swap []] [[] drop] even? dup]` and then combine the lists with `fold [compose] []`. This requires a bit of tricky `compose` calls to build the expresion with `even?` embedded:

```brief
let 'filter [fold [compose] [] map compose [if [cons swap []] [[] drop]] compose swap [dup]]
```

Another thought is to do it directly while folding over the list; either `consing` it onto a new list or `dropping` it, then `reversing` the result.

```brief
let 'filter [reverse fold swap [] compose [if [cons swap] [drop swap]] compose swap [over]]
```

This seems equally tricky and maybe a bit less clear. The need to reverse probably makes it similar performance to the two-pass `map`/`filter` version so perhaps we'll keep the first version for now.

We can define a `swons` word as `cons swap` (taken [from Factor](https://docs.factorcode.org/content/word-swons,lists.html)) and replace the places we've done this (in `filter` and `reverse`). This ability to factor out code it one of the beauties of concatenative languages. In fact `swons []` can be replaced with a `quote` word meant to create single-element `Lists` from `Values`. So, `cons swap []` becomes just `quote`:

```brief
let 'filter [fold [compose] [] map compose [if [quote] [[] drop]] compose swap [dup]]
```

One final trick for the day. Let's add `range <n> <m>` word to generate a `List` of numbers from n to m. For example, this would generate `[0 1 2 3 4 5 6 7 8 9 10]`:

```brief
drop while [dip [cons] + -1 dup] [<= 0 dup] 10 []
```

Starting with `0 10` on the stack, `-rot []` will place the initial empty list underneath. A `compose [<=] swons [dup]` will create the `[<= <n> dup]` expression:

```brief
let 'range [drop while [dip [cons] + -1 dup] compose [<=] swons [dup] -rot []]
```

And now implement `factorial` in terms of this. Factorial of a number is just the `product` of a `range` from `1` to that number:

```brief
let 'factorial [product range 1]
```

The finale for the day is to implement the solution to [Project Euler problem #1](https://projecteuler.net/problem=1); Find the sum of all the multiples of 3 or 5 below 1000. This was [done in a previous encarnation of Brief some years ago](https://www.youtube.com/watch?v=R3MNcA2dpts).

```brief
let 'multiple? [= 0 mod swap]
sum filter [or bi [multiple? 3] [multiple? 5]] range 1 999
```

Pretty "brief" solution. Fun, fun!

## 30 DEC 2020 Infix, Prefix, Postfix

The `= 0 mod swap` above is bothersome. It would be better to avoid the `swap` (also in `let 'even? [= 0 mod swap 2]`). And the definition of `let '-- [+ -1]` should be just `let '-- [- 1]` symetrical with `let '++ [+ 1]`. The problem is the order of arguments to these binary operations. While keeping them in their standard infix order, they're not convenient for currying. While saying that `- x y` means "subtract y from x", what does `- x` mean? "Subtract <something> from x"? Much more convenient to say that it means "subtract x from ..." whatever's on the stack. The same goes for comparison operations. `> 10` should mean "is greater than 10 ...", not "is 10 greater than ..." It may be confusing, but let's reverse the arguments.

It's interesting to note that infix argument order works nicely with postfix notation because `y x -` in postfix does mean "subtract x from y" and `x -` also means "subtract x." You can even have increment/decrement operators like `1+` and `11` that are simply defined as `1 +` and `1 -`. We had to use names like `++` and `--` in Brief because `-1`, for example, would be confused with a negative-one literal.

While using the current system I'm personally finding myself authoring code backward; writing right to left. This may be because of being used to RPN, Forth, Factor, Joy, ... or may be that postfix is truely superior. Let's stick with prefix notation a bit longer and see what other snags we hit.

## 31 DEC 2020 Fried Quotations

Inspired by [Factor's fried quotations idea](https://docs.factorcode.org/content/article-fry.html), let's build a `fry` word for Brief, but without the special `'[` parsing word and without the distinction between `_` and `@` specifiers.

What `fry` will do is replace "holes" in the form `_` with values from the stack:

```brief
fry [this _ a _] 'is 'test
| [this is a test]
```

To accomplish this, the strategy will be to build a `List` of `Lists` and then flatten this. Some languages call this general idea `SelectMany`. We'll call it `flatmap`; essentially mapping over a list with a function that produces zero to many results as `Lists`, then `fold [compose] []` over that to concatenate all the inner lists:

```brief
let 'flatmap [fold [compose] [] map]
```

Using this, `fry` is quite simple. Just `flapmap` over the quotation and either rotate in values from the stack to replace "holes" or `quote` non-holes:

```brief
let 'fry [flatmap [if [rot drop] [quote] = >sym '_ dup]]
```

Right away we can start using `fry` to simplify some existing definitions. For example, `range` does this complicated `compose [>=] swons [dup]`:

```brief
let 'range [drop while [dip [cons] -- dup] compose [>=] swons [dup] -rot []]
```

This is merely to build up an expression in the form `[>= <some_value> dup]`. Replacing with `fry [>= _ dup]` is _much_ more clear:

```brief
let 'range [drop while [dip [cons] -- dup] fry [>= _ dup] -rot []]
```

The current `filter` definition literaly has a `flatmap` built into it (`fold [compose] [] map`):

```brief
let 'filter [fold [compose] [] map compose [if [quote] [[] drop]] compose swap [dup]]
```

This can be replaced of course. The beauty of the "algebra" of Brief is that these refactorings can be done blindly.

```brief
let 'filter [flatmap compose [if [quote] [[] drop]] compose swap [dup]]
```

Also, notice that the `compose [if [quote] [[] drop]] compose swap [dup]` is essentially constructing an expression of the form `[if [quote] [[] drop] <some_predicate> dup]`. This can be replaced with a fried quotation:

```brief
let 'filter [flatmap fry [if [quote] [[] drop] _ dup]]
```

An interesting one is in `map`. The `compose swap [swap] compose [cons]` is just constructing `[cons <some_expression> swap]`:

We _can_ implement `prepose` ([taken from Factor](https://docs.factorcode.org/content/word-prepose,kernel.html)):

```brief
let 'prepose [compose swap]
```

And slightly simplify `map`:

```brief
let 'map [reverse fold swap [] prepose [swap] compose [cons]]
```

However, replacing the whole thing with a `fry [cons _ swap]` causes infinite recursion!

```brief
let 'map [reverse fold swap [] fry [cons _ swap]]
```

This is because `fry` itself uses `flatmap`, which uses `map`, which would then be defined in terms of `fry`. We'll have to see if there's a elegant solution to this some time. For now, we'll leave it as is.

We also want to allow splicing in _whole quotations_. In Factor this is done with a separate `@` token, but we'll just make it work by "magic" based on the type of value filling the hole:

```brief
fry [this _ test] [is another]
| [this is another test]
```

If the value is a list (`list?`) then we won't quote it and instead will allow `flatmap` to flatten it:

```brief
let 'fry [flatmap [if [unless [quote] list? dup rot drop] [quote] = >sym '_ dup]]
```

If we really want a quotation to fill a "hole" then we'll double-quote it:

```brief
fry [this _ test] [[is another]]
| [this [is another] test]
```

This leaves the onus on the caller which is probably why Factor uses another token to enforce it. We don't have type safety anyway, so... something to consider for the future.

DEBATE 21: Should fried quotations support [an `@` fry specifier](https://docs.factorcode.org/content/word-__at__%2Cfry.html)?

What we still don't support is _nested_ fried quotations (aka *deep* fried quotations!). The following should be made to work:

```brief
fry [foo _ [bar _ ] baz] 1 2
| [foo 1 [bar 2] baz]
```

To do this, instead of blindly quoting (`quote`) non-holes, we want to recursively `fry` them if they're a list (`[if [<recursively_fry>] [quote] list? dup]`). Recursively `frying` is a bit tricky because of the state of the stack at that point. It contains the quotation being fried and a partial resulting quotation. We need to rotate (`-rot`) the non-hole below these, the dip down (`2dip`) below them and fry (`[fry]`) this against the remaining stack, and then rotate (`rot`) the result back to be quotes (`quote`). Written forward, that's `quote rot 2dip [fry] -rot`.

Notice how while thinking about this, we thing _operationally_ about the stack effects and walk through the words in revers order just as the machine does. It's still unclear whether prefix or postfix notation will be best in the end. The thinking is that maybe once we've moved on to higher level words, a top-down reading will be clear.

DEBATE 22: Should we use prefix or postfix notation?

Reading what we've just come up with as, "The `quote` of a value rotated from below, after having dipped down to fry it, after having rotated it below to begin with". Each of the "after having..." parts make is clear that thinking top-down about this is difficult.

At any rate, here's what we have:

```brief
let 'fry [flatmap [if [unless [quote] list? dup rot drop] [if [quote rot 2dip [fry] -rot] [quote] list? dup] = >sym '_ dup]]
```

A very long and confusing definition! Factoring out some pieced to "document" them with name/intent:

```brief
let 'fill [unless [quote] list? dup rot drop]
let 'deepfry [if [quote rot 2dip [fry] -rot] [quote] list? dup]
let 'hole? [= >sym '_ dup]
```

Then it can be written much more clearly:

```brief
let 'fry [flatmap [if [fill] [deepfry] hole?]]
```

It's a shame to polute the dictionary with these extra words though. We could prefix them with `fry.` as a "namespace" of sorts and could indent them to show that they "belong" to the parent `fry` word:

```brief
let 'fry [flatmap [if [fry.fill] [fry.deepfry] fry.hole?]]
    let 'fry.fill [unless [quote] list? dup rot drop]
    let 'fry.deepfry [if [quote rot 2dip [fry] -rot] [quote] list? dup]
    let 'fry.hole? [= >sym '_ dup]
```

It would be better to come up with a system for proper name scoping.
