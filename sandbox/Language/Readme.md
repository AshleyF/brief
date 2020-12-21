# Brief Sandbox Journal

Starting experimenting with Brief implementation details while documenting thoughts. Brief is a concatenative language with quotations.

## 09 DEC 2020 Structure

The initial thoughts on the structure of Brief is that we have `Values`, `Words` and a machine `State`.

`Values` may be primitives (e.g. `double`, `string`, `bool`, ...) or may be composite types (e.g. `Value list`, `Map<string, Value`, `Set<Value>`, ...) or may be `Quotations` which are a list of `Words`. With the inclusion of quotations, functions are data.

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

DEBATE 0: Sets of values require that words (in quotations) be comparible. Literals and secondaries are easy enough, but primitives (`State -> State` functions) may be more difficult. Considering implementing `IComparible` based on a name or ID.

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

DEBATE 3: Related to #2, Recursive evaluation of `Secondaries` should be handled with exposed mechanics, supporting Brief-based debugging and (obviously) tail recursion. Prepending to a current `Continuation` works and ideas such as inserting debuggind words are pretty slick.

A vague idea forming is that a protocol between Brief "actors" could be streaming code. Inspiration comes from the GreenArrays GA144 on which nodes start empty; listening on ports for code to execute. The GA144 has a machanism to point the program counter at a port rather than at memory, reading and executing code as it streams in and instruction words with micronext may easily be used to fill memory with code coming in and jump to that. A Brief machine should start empty and be fed with code. This code should then also be able to be persisted and evaluated internally.

DEBATE 4: Should we allow a hybrid of stored-program instruction and "streaming" instructions to the machine?

The third cut is to change the processing of `Secondaries` to merely prepend to a `Continuation` in the state. This `word` function is similar to this first `interpret` above but is not recursive.

```fsharp
let word state = function
	| Literal v -> { state with Stack = v :: state.Stack }
	| Primitive p -> p.Func state
	| Secondary (_, s) -> { state with Continuation = s @ state.Continuation }
```

Then to interpret a stream of `Words` along with a state, we have two distinct "modes." While there is no `Continuation`, we simply walk the stream of words one-by-one evaluating them. Otherwise, when there _is_ a `Continuation`, we peal off words from it one-by-one and evaluate them. When there is no `Continuation` and the stream is complete, then we terminate.

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
```

Evaluating quotations on the stack by prepending them to the `Continuation`. This is made possible by this representation of the machine decided on yesterday. The name `i` is taken from Joy.

```fsharp
let i = Primitive (NamedPrimitive ("i", (fun s ->
	match s.Stack with
	| Quotation q :: t -> { s with Stack = t; Continuation = q @ s.Continuation }
	| _ :: t -> failwith "Expected q"
	| _ -> failwith "Stack underflow")))
```

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
let sub = binaryOp "-" (+)
let mul = binaryOp "*" (*)
let div = binaryOp "/" (/)

let chs = unaryOp "chs" (fun n -> -n)
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

The first thought was to create a `define` word that would add any `Value` to the `Map`, including `Words`, and add a `find` word to retrieve them. The `i` word (taken from Joy) can be used to apply `Quotations`. The thought now that this is a base case used by more specialized words to define `Literals`, `Primitives`, and `Secondaries`. Renaming `define` to `!` (taken from Forth) and `find` to `@` (also from Forth), read as "store" and "fetch" of course.

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

DEBATE 6: Obviously including a type system to distinguish `define` (expecting a `Quotation`) from `store` (expecting *any* `Value`) is a very big open question. In this particular case a type error would not even cause an issue until later `fetching` and trying to apply (`i`) a non-`Quotation`.

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
    "chs",   chs
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

Using F#'s `MailboxProcessors`, we create actors that accept fragments of Brief source and evaluate them in their own instance of machine state. This state can contain a custom vocabulary of words that then befome the "protocol" for interacting with the actor.

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

20 DEC 2020 Binary Format

Thinking about making actors that span processes and/or machines: should the protocol simply be Brief *source* code (DEBATE 15 above)? Or should there be a simpler and more compact binary representation. In fact, should the Brief machinery be defined in terms of a "byte code"?

DEBATE: 17: Should Brief machinery be defined in terms of a byte code?

Conveying `Word lists` is what we're talking about. We'll need to encode `Values`, including compound `Lists`, `Maps`, and `Sets` (and recursive `Quotations` themselves), and we'll need to encode `Primitive` and `Secondary` words.

`Primitive` words are built into the machine. These should be as few as possible/practical and should be identified by name or some unique number (GUID?) known across machines. Some actors add new `Primitives` (such as Tesla's `honk`) and so need to be able to extend the known set; either by name or otherwise. Either we use something truely globally unique like a GUID or we add namespaces and such (e.g. `brief.dup`, `tesla.honk`).

`Secondaries` can be defined on the host actor and then conveyed by name. Or again, perhaps must be defined with a GUID or namespace.

Both `Primitive` and `Secondary` words should be able to be conveyed without understanding them. Imagine a "relay" actor, forwarding code to other actors for interpretation or some algebraic manipulation of program structure at a higher level. So, the format should support words as "symbols" being conveyed without a necessary mapping to implementation.

In the future there may be name scopes as well for secondaries; that is sequences of words given a name for clarity or factoring out redundancy but then referred to by name only within a "parent" word. For example `area` could define `sq` and `pi` as children and not polute te dictionary with these names - only exposing `area`.

Atomic `Values` are easy enough to encode: `Numbers` as IEEE754, `Strings` as lenth-prefixed UTF-8, `Booleans` as a simple byte (-1, 0), ... Composit `Values` are not much more difficult. `Lists` could be a length followed by n-values, a `Map` could be a length followed by n-pairs of `String`/`Value` and `Sets` could be a length followed by n-distinct `Values`.

In the language itself, where will be a means of composing compound `Values` from atomic ones. An `empty-list` word will push an empty `List` while a `cons` word will add a `Value` to the head; building a list in reverse. Encoding a list this way would use words already available in the machine. Perhaps an `n-cons` word could cons n-values onto a list as a more compact representation. `empty-list 'a 'b 'c 3 n-cons` vs `empty-list 'a cons 'b cons 'c cons`.

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
