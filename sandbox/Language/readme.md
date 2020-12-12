# Brief Sandbox Journal

Starting experimenting with Brief implementation details while documenting thoughts.

## 09 NOV 2020 Structure

My initial thoughts on the structure of Brief is that we have `Values`, `Words` and a machine `State`.

`Values` may be primitives (e.g. `double`, `string`, `bool`, ...) or may be composite types (e.g. `Value list`, `Map<string, Value`, Set<Value>, ...) or may be `Quotations` which are a list of `Words`. With the inclusion of quotations, functions are data.

	type Value =
		| Number of double
		| String of string
		| Boolean of bool
		| List of Value list
		| Map of Map<string, Value>
		| Set of Set<Value>
		| Quotation of Word list

DEBATE 0: Sets of values require that words (in quotations) be comparible. Literals and secondaries are easy enough, but primitives (State->State functions) may be more difficult. Considering implementing `IComparible` based on a name or ID.

`Words` represent code. `Literal` values are pushed to the stack, `Primitives` update the state, and `Secondary` words are a list of words to be applied. Note that quotations are also lists of words, but are treated as values (not applied; e.g. a literal quotation is pushed to the stack).

DEBATE 1: I explored whether to include literals or to instead create primitives as closures capturing the value and having the effect of pushing to the stack (data is code). Cute, but I'm thinking now that literals should be a distinct kind of word. It can still be said that words are code and so literals are data as code; a nice symmetry with quotations being code as data.

	and Word =
		| Literal of Value
		| Primitive of (State -> State)
		| Secondary of Word list

The `State` of the "machine" is a `Continuation` list of words (the current continuation), a parameter `Stack` of values and a `Dictionary` used as a global memory space of named values.

DEBATE 2: I'll explore how and whether to support namespaces or something else to avoid collisions in the dictionary.

	and State = {
		Continuation: Word list
		Stack: Value list
		Dictionary: Map<string, Value> }

## 10 NOV 2020 Identity

I decided to resolve debate #0 above by introducing a `NamedPrimitive` type that supports `IComparable` by treating the name as the identity (functions cannot be compared). This allows `Values` to include `Set<Value>`, which can include `Quotations`, which are lists of `Words`, which include `Primitives` and so must be comparable.

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

## 11 NOV 2020 Interpretation

My first cut was to recursively evaluate the whole machine state, including an initial `Continuation`. `Literals` are pushed to the `Stack`. `Primitives` are applied to the state and, most interestingly, `Secondaries` are appended to the `Continuation` for evaluation on the next iteration.

	let rec eval state =
		match state.Continuation with
		| Literal v :: t -> { state with Stack = v :: state.Stack; Continuation = t } |> eval
		| Primitive p :: t -> { state with Continuation = t } |> p.Func |> eval
		| Secondary (_, s) :: t -> { state with Continuation = s @ t } |> eval
		| [] -> state

My second cut was to remove the `Continuation` from the state and instead process `Word` by `Word`; `Literals` go to the stack, `Primitives` are still applied to the state but are no longer able to manipulate the program, `Secondaries` are evaluated by recursively folding over them.

	let rec step state = function
		| Literal v -> { state with Stack = v :: state.Stack }
		| Primitive p -> p.Func state
		| Secondary (_, s) -> Seq.fold step state s

In fact, full evaluation is then just a fold: `let eval = Seq.fold step`. I like this, but I dislike that `Primitives` can no longer manipulate the program (e.g. to impliment `Dip`) and I don't like that the recursion happens in F#/.NET land rather than in more directly exposed machinery.

DEBATE 3: Should the current `Continuation` be part of the machine state? I think yes.

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

DEBATE 4: Related to #3, Recursive evaluation of `Secondaries` should be handled with exposed mechanics, supporting Brief-based debugging and (obviously) tail recursion. Prepending to a current `Continuation` works and I like ideas such as inserting debuggind words.

I have a still vague idea that a protocol between Brief "actors" could be streaming code. I also am inspired by the GreenArrays GA144 on which nodes start empty; listening on ports for code to execute. The GA144 has a machanism to point the program counter at a port rather than at memory, reading and executing code as it streams in and instruction words with micronext may easily be used to fill memory with code coming in and jump to that. I want something similar. A Brief machine should start empty and be fed with code. This code should then also be able to be persisted and evaluated internally.

DEBATE 5: Should we allow a hybrid of stored-program instruction and "streaming" instructions to the machine?

My third cut is to change the processing of `Secondaries` to merely prepend to a `Continuation` in the state. This `word` function is similar to this first `eval` above but is not recursive.

	let word state = function
		| Literal v -> { state with Stack = v :: state.Stack }
		| Primitive p -> p.Func state
		| Secondary (_, s) -> { state with Continuation = s @ state.Continuation }

Then to evaluate (`eval`) a stream of `Words` along with a state, we have two distinct "modes." While there is no `Continuation`, we simply walk the stream of words one-by-one evaluating them. When there is no `Continuation` and the stream is complete, then we terminate. Otherwise, when there _is_ a `Continuation`, we peal off words from it one-by-one and evaluate them.

	let rec eval stream state =
		match state.Continuation with
		| [] ->
			match Seq.tryHead stream with
			| Some w -> word state w |> eval (Seq.tail stream)
			| None -> state
		| w :: c -> word { state with Continuation = c } w |> eval stream

This now exposes the mechanics of recursion in the machine and allows for simpler debugging. It also exposes the `Continuation` to be manipulated by `Primitives`. Finally, it allows for a hybrid of stored/streaming words.

## 12 NOV 2020


