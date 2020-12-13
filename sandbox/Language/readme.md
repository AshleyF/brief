# Brief Sandbox Journal

Starting experimenting with Brief implementation details while documenting thoughts. Brief is a concatenative language with quotations.

## 09 NOV 2020 Structure

My initial thoughts on the structure of Brief is that we have `Values`, `Words` and a machine `State`.

`Values` may be primitives (e.g. `double`, `string`, `bool`, ...) or may be composite types (e.g. `Value list`, `Map<string, Value`, Set<Value>, ...) or may be `Quotations` which are a list of `Words`. With the inclusion of quotations, functions are data.

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

DEBATE 0: Sets of values require that words (in quotations) be comparible. Literals and secondaries are easy enough, but primitives (State->State functions) may be more difficult. Considering implementing `IComparible` based on a name or ID.

`Words` represent code. `Literal` values are pushed to the stack, `Primitives` update the state, and `Secondary` words are a list of words to be applied. Note that quotations are also lists of words, but are treated as values (not applied; e.g. a literal quotation is pushed to the stack).

DEBATE 1: I explored whether to include literals or to instead create primitives as closures capturing the value and having the effect of pushing to the stack (data is code). Cute, but I'm thinking now that literals should be a distinct kind of word. It can still be said that words are code and so literals are data as code; a nice symmetry with quotations being code as data.

```fsharp
and Word =
	| Literal   of Value
	| Primitive of (State -> State)
	| Secondary of Word list
```

The `State` of the "machine" is a `Continuation` list of words (the current continuation), a parameter `Stack` of values and a `Map` used as a global memory space of named values.

```fsharp
and State = {
	Continuation: Word list
	Stack: Value list
	Map: Map<string, Value> }
```

## 10 NOV 2020 Identity

I decided to resolve debate #0 above by introducing a `NamedPrimitive` type that supports `IComparable` by treating the name as the identity (functions cannot be compared). This allows `Values` to include `Set<Value>`, which can include `Quotations`, which are lists of `Words`, which include `Primitives` and so must be comparable.

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

## 11 NOV 2020 Interpretation

My first cut was to recursively evaluate the whole machine state, including an initial `Continuation`. `Literals` are pushed to the `Stack`. `Primitives` are applied to the state and, most interestingly, `Secondaries` are appended to the `Continuation` for evaluation on the next iteration.

```fsharp
let rec eval state =
	match state.Continuation with
	| Literal v :: t -> { state with Stack = v :: state.Stack; Continuation = t } |> eval
	| Primitive p :: t -> { state with Continuation = t } |> p.Func |> eval
	| Secondary (_, s) :: t -> { state with Continuation = s @ t } |> eval
	| [] -> state
```

My second cut was to remove the `Continuation` from the state and instead process `Word` by `Word`; `Literals` go to the stack, `Primitives` are still applied to the state but are no longer able to manipulate the program, `Secondaries` are evaluated by recursively folding over them.

```fsharp
let rec step state = function
	| Literal v -> { state with Stack = v :: state.Stack }
	| Primitive p -> p.Func state
	| Secondary (_, s) -> Seq.fold step state s
```

In fact, full evaluation is then just a fold: `let eval = Seq.fold step`. I like this, but I dislike that `Primitives` can no longer manipulate the program (e.g. to impliment `Dip`) and I don't like that the recursion happens in F#/.NET land rather than in more directly exposed machinery.

DEBATE 2: Should the current `Continuation` be part of the machine state? I think yes.

I love the below visualization of the mechanics. `Continuation` on the left, `Stack` on the right (or visa versa if you prefer postfix). `Secondary` words are "expanded" and appended to the `Continuation`. I've done this before for a debugger by inserting "break" and "step out" words between the expansion and the rest of the program. Very tangible, exposed, simple mechanics.

	area 7.200000 |                    // initial program
	         area | 7.200000           // 7.2 pushed
	      * pi sq | 7.200000           // area expanded
	   * pi * dup | 7.200000           // sq expanded
	       * pi * | 7.200000 7.200000  // 7.2 duplicated
	         * pi | 51.840000          // mulpiply
	   * 3.141593 | 51.840000          // pi expanded
	            * | 3.141593 51.840000 // pi pushed
	              | 162.860163         // multiply

DEBATE 3: Related to #2, Recursive evaluation of `Secondaries` should be handled with exposed mechanics, supporting Brief-based debugging and (obviously) tail recursion. Prepending to a current `Continuation` works and I like ideas such as inserting debuggind words.

I have a still vague idea that a protocol between Brief "actors" could be streaming code. I also am inspired by the GreenArrays GA144 on which nodes start empty; listening on ports for code to execute. The GA144 has a machanism to point the program counter at a port rather than at memory, reading and executing code as it streams in and instruction words with micronext may easily be used to fill memory with code coming in and jump to that. I want something similar. A Brief machine should start empty and be fed with code. This code should then also be able to be persisted and evaluated internally.

DEBATE 4: Should we allow a hybrid of stored-program instruction and "streaming" instructions to the machine?

My third cut is to change the processing of `Secondaries` to merely prepend to a `Continuation` in the state. This `word` function is similar to this first `eval` above but is not recursive.

```fsharp
let word state = function
	| Literal v -> { state with Stack = v :: state.Stack }
	| Primitive p -> p.Func state
	| Secondary (_, s) -> { state with Continuation = s @ state.Continuation }
```

Then to evaluate (`eval`) a stream of `Words` along with a state, we have two distinct "modes." While there is no `Continuation`, we simply walk the stream of words one-by-one evaluating them. When there is no `Continuation` and the stream is complete, then we terminate. Otherwise, when there _is_ a `Continuation`, we peal off words from it one-by-one and evaluate them.

```fsharp
let rec eval stream state =
	match state.Continuation with
	| [] ->
		match Seq.tryHead stream with
		| Some w -> word state w |> eval (Seq.tail stream)
		| None -> state
	| w :: c -> word { state with Continuation = c } w |> eval stream
```

This now exposes the mechanics of recursion in the machine and allows for simpler debugging. It also exposes the `Continuation` to be manipulated by `Primitives`. Finally, it allows for a hybrid of stored/streaming words.

## 12 NOV 2020 Vocabulary

Building up the set of available words, getting the `depth` of the stack and `clearing` it:

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

Evaluating quotations by prepending them to the `Continuation`. This is made possible by this representation of the machine decided on yesterday.

```fsharp
let i = Primitive (NamedPrimitive ("i", (fun s ->
	match s.Stack with
	| Quotation q :: t -> { s with Stack = t; Continuation = q @ s.Continuation }
	| _ :: t -> failwith "Expected q"
	| _ -> failwith "Stack underflow")))
```

Dip is an interesting word taken from Joy. It evaluates a quotation while "dipping" below the next value on the stack. This is accomplished again by manipulating the `Continuation` to prepend the unquoted code followed by the value being dipped under as a literal:

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
eval [Literal (Number 7.2); Literal (String "ashleyf"); Literal (Quotation [area]); dip] emptyState |> printDebug
```

## 13 NOV 2020 Dictionary

My first thought was to create a `define` word that would add any `Value` to the `Map`, including `Words`, and add a `find` word to retrieve them. An `i` word (taken from Joy) can be used to apply `Quotations`. I'm thinking now that this is a base case used by more specialized words to define `Literals`, `Primitives`, and `Secondaries`. I think I'll rename `define` to `!` (taken from Forth) and `find` to `@` (also from Forth), read as "store" and "fetch" of course.

``` fsharp
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
```

DEBATE 5: Should definitions be `Words`, `Quotations` or any `Value`?

Defining `Words` does seem special. I can create a `def` word for this but it's merely a renaming of `store`; nothing special other than expecting a `Quotation` but there is no enforcement of that.

```fsharp
let define = Secondary ("def", [store])
```

DEBATE 6: Obviously including a type system to distinguish `define` (expecting a `Quotation`) from `store` (expecting *any* `Value`) is a very big open question. In this particular case a type error would not even cause an issue until later `fetching` and trying to apply (`i`) a non-`Quotation`.

Now I can `define` words in the "dictionary".

```fsharp
Literal (Quotation [dup]);                      Literal (String "dup");  store
Literal (Quotation [mul]);                      Literal (String "*");    store
Literal (Quotation [Literal (Number Math.PI)]); Literal (String "pi");   store
Literal (Quotation [dup; mul]);                 Literal (String "sq");   store
Literal (Quotation [sq; pi; mul]);              Literal (String "area"); store
```

DEBATE 7: I'll explore how and whether to support namespaces or something else to avoid collisions in the map. Perhaps `define` should hang everything off of a "dictionary" key or maybe "_dictionary" with a convention that underscore denotes Brief internals. User namespacing would just be a matter of convention. Perhaps `@` and `!` could be redefined by the user to redirect to a branch in the `Map`.

DEBATE 8: Maybe _all_ of Brief's internals, the `Stack`, `Continuation`, and the `Map` itself should be avaliable via fetch/store. For example as "_stack", "_continuation", "_map", ... exposing the entire mechanics to programatic manipulation. Or maybe these should be exposed by individual primitives. I rather the idea of the machine state being something threaded through the execution as opposed to a globally accessible entity.

## 14 NOV 2020 Syntax


