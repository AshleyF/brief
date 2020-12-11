# Brief Sandbox Journal

Starting experimenting with Brief implementation details while documenting thoughts.

## 09 NOV 2020

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

DEBATE: Sets of values require that words (in quotations) be comparible. Literals and secondaries are easy enough, but primitives (State->State functions) may be more difficult. Considering implementing `IComparible` based on a name or ID.

`Words` represent code. `Literal` values are pushed to the stack, `Primitives` update the state, and `Secondary` words are a list of words to be applied. Note that quotations are also lists of words, but are treated as values (not applied; e.g. a literal quotation is pushed to the stack).

DEBATE: I explored whether to include literals or to instead create primitives as closures capturing the value and having a state effect of pushing the value to the stack (data is code). Cute, but I'm thinking now that literals should be a distinct kind of word. It can still be said that words are code and so literals are data as code; a nice symmetry with quotations being code as data.

	and Word =
		| Literal of Value
		| Primitive of (State -> State)
		| Secondary of Word list

The `State` of the "machine" is a `Program` list of words (the current continuation), a parameter `Stack` of values and a `Dictionary` used as a global memory space of named values.

DEBATE: I'll explore how and whether to support name spacing or something else to avoid collisions in the dictionary.

	and State = {
		Program: Word list
		Stack: Value list
		Dictionary: Map<string, Value> }
