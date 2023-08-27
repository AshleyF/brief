# Studying Clojure (reading The Joy of Clojure)

- Define function: `(fn sq [x] (* x x))`
- Define anonymously: `(fn [x] (* x x))`
- Apply anonymous: `((fn [x] (* x x)) 7)` => `49`
- Define multible arities:
    `(fn
       ([] 42)
       ([x] (* x x))
       ([x y] (* x y)))`
- Define with `def`: `(def foo 42)` => `foo` => `42`
- Define function with `def`: `(def sq (fun [x] (* x x)))`
- Define function with `defn`: `(defn sq ([x] (* x x)))`
- Reader feature (in-place functions): `#()`
  - `(def foo #(list %))`
  - `(def foo #(list %1))`
  - `(def foo #(list %1 %2))`
  - `(def foo #(list %1 %2 %&))`
- `do` form: `(do (println "test") (println "ing") (* 4 5))`
- Locals (with implicit `do`): `(let [x 7 y 42] (println x y) (* x y))`
- `if` form: `(if (<pred>) (<then) (<else))`
- `when` form: `(when (<pred>) (<do_this>) (<do_that>))`
- `loop` form: `(loop [x 1] (when (< x 10) (println x) (recur (+ x 1))))`
- `throw`: `(throw (Exception. "WTF"))` => `Execution error at user/eval195 (REPL:1). WTF`
- `try`/`catch`: `(try (/ 3 0) (catch Exception e (str "Bad " (.getMessage e))))` => `"Bad Divide by zero"`
- Truthiness: everything except `false` and `nil` and "true"
  - Empty list isn't false. However, `(seq [])` => `nil`
- Search docs: `(find-doc "foo")`
- float/double prone to over/underflow
- All floating point prone to rounding errors (rationals, FTW!)
- Keywords always refer to themselves (single instance): keys, enumerations, multimethod dispatch, directives
- Meta: `(with-meta 'foo {:bar 123})` ... `(meta foo)` => `{:bar 123}`
- Lisp1 (single namespace), Lisp2 (separate function namespace)
- RegEx: `#"myregex"`
- Comma (`,`) is whitespace (optional)
- `(apply hash-map [:foo 123 :bar 456])` applies function to arguments

# Destructuring

- Test vector data: `(def foo [0 1 2 3 4 5])`
- Indexing: `(nth foo 2)`
- Positional destructuring: `(let [[a b c] foo] [c b a])` => `[2 1 0]`
- Positional remaining: `(let [[a b c & more] foo] [c b a more])` => `[2 1 0 (3 4 5)]`
- Using `:as`: `(let [[a b c & more :as all] foo] [c b a more all])` => `[2 1 0 (3 4 5) [0 1 2 3 4 5]]`
- Destructuring vector with map: `(let [{b 1 c 2} foo] [b c])` => `[1 2]`
- Test map data: `(def bar { :a 0 :b 1 :c 2 })`
- Map destructuring: `(let [{x :a y :c} bar] [x y])` => `[0 2]`
- Using `:keys` (also `:strs`, `:syms`): `(let [{:keys [a c]} bar] [a c])` => `[0 2]`
- Using `:as`: `(let [{x :a y :c :as all} bar] [x y all])` => `[0 2 {:a 0, :b 1, :c 2}]`
- Default values with `:or`: `(let [{x :a y :c z :missing :or {z "default"}} bar] [x y z])` => `[0 2 "default"]`

# Quoting

- `(quote foo)`
- `'foo`
- Vector elements eval'd by default: `[1 (* 2 3)]` => `[1 6]` but `[1 '(* 2 3)]` => `[1 (* 2 3)]`
- Lists expected to be prefixed by functions unless quoted: `(eval (cons '* '(2 3))` => `6`
- Syntax quote (`` ` ``) qualified names: `` `map`` => `clojure.core/map`, `` `(* 2 3)`` => `(closure.core/* 2 3)`
- Unquote with `~`: `` `(* 2 ~(+ 3 4))`` => `(clojure.core/* 2 7)`
- Unquote-splicing: ``(let [x '(2 3)] `(1 ~x))`` => `(1 (2 3))`, ``(let [x '(2 3)] `(1 ~@x))`` => `(1 2 3)`
- Auto-gensym: `` `foo#`` => `foo__192__auto__`

# Compound Data

- `seq` protocol: `first`, `rest`
- Vector is O(log32 n)
- `rseq` on Vector, `keys`/`vals` on HashMap, etc. return Sequence
- `vector-of :int` (`:char`, `:long`, `:float`, ...)
- Pour one sequence `into` another
- `(nth <vector> n)`, `(get <vector> n)`, `(<vector> n)`
  - `(<vector> n)` throws if `nil` on out of range
  - `(nth <vector> n)` throws on out of range
  - `(nth <vector> n :whoops)` returns `:whoops` if not found
  - `(get <vector> n :whoops)` returns `:whoops` if not found
  - `assoc` replaces elements (only on existing indices or 1-past-end)
  - `replace` replaces elements of vectors or sequences (`(replace { 1 123 2 456 } <vector>)`)
  - `update`, `get`, `get-in`, `update-in`, `assoc-in`, ...
  - `conj`, `pop`, `peek` adds/removes/peeks right end (`IPersistentStack`)
    - More efficent and clear than `last`, `dissoc`, ...
- Vectors reduce the need for reversing lists (as in other Lisps)
- `subvec` creates persistent slices
- MapEntry is actually a Vector
- `conj` is always most efficient for the data type
- `cons` or `conj` onto `nil` makes a single-element collection (e.g. `(cons 42 nil)`/`(conj nil 42)` => `(42)`)
- `#{:foo :bar}` (PersistentHashSet), `(sorted-set :foo :bar)` (PersistentTreeSet)
- Trick using `some` with a set as the predicate: `(some #{:foo} [:bar :baz :foo])` => `:foo` (or `nil` if not found)
- `sorted-set-by`/`sorted-map-by` takes custom comparison function
- `clojure.set/intersection`|`union`|`difference`
- `(hash-map :foo 123, :bar 456)` => `{:bar 456, :foo 123}` (or use literal syntax)
- `(seq <map>)` (MapEntry is Vector and supports `key`/`val`), `(keys <map>)`, `(vals <map>)`
- `(zipmap [:a :b] [1 2])` => `{:a 1, :b 2}`
- Sorted map can replace vals if keys have same sort order! `(assoc (sorted-map 1 :int) 1.0 :float)` => `{1 : float}`
- `array-map` maintains insertion order
- Collections are functions! `(def foo {:x 123 :y 456 :z 789})` `(foo :y)` => `456`
- Keywords are functions: `(:y foo)` => `456`
- Collections can be passed as functions to `map`/`filter`/`fold`: `(map foo [:x :z])` => `(123 789)`
- `rest` doesn't reify, while `next` does (to determine whether more elements)
- `lazy-seq` macro: `(defn step [s] (lazy-seq (if (seq s) [(first s) (step (rest s))] [])))` `(step [1 2 3])` => `(1 (2 (3 ())))`
- Another `lazy-seq` example: `(defn rangeseq [i] (lazy-seq (when (> i 0) (cons i (rangeseq (dec i))))))` `(rangeseq 10)` => `(10 9 8 7 6 5 4 3 2 1)`
- `delay` and `force` macros to control laziness
- `if-let`/`when-let` instead of `(if ... (let ...) ...)`
- `comp` composes functions `(comp first rest rest)`
- `apply` applies a function (duh!)
- `partial` manual partial application: `(partial * 2)` (not curried all the way down though)
- `compliment` reverses truthiness of a function
- Named arguments: `(defn foo [& [:keys [a b] :or [a :foo b :bar]]] ...)`
- Pre/post conditions: `(defn foo {:pre [(...) (...)] :post [(...) (...)]} ...)`
- Closures as objects: shared state - ctor returning a map of functions (methods)

# Namespaces

- Declare: `(ns foo)`
- Load other: `(ns foo (:require bar))`
- Alias: `(ns foo (:require [bar.baz :as blah]))`
- Mapping: `(ns foo (:use [bar.bar :only [blah]]))`
- Mapping already loaded: `(ns foo (:refer bar.baz))`

# Tail Recursion

First WTF moment: Clojure's built atop the JVM which apparently doesn't have tail call optimization. So, this blows the stack:

```clojure
(defn fac [n a] (if (< n 1N) a (fac (- n 1N) (* n a))))
(fac 10000N) ; blow the stack
```

Even with the recursive call in proper tail position. But there is a `recur` form for tail calls:

```clojure
(defn fac [n a] (if (< n 1N) a (recur (- n 1N) (* n a)))) ; using recur
(fac 100000N 1N) ;=> GIANT NUMBER!
```

So then how do you do *mutual* recursion? Can't use `recur` to call *other* functions. This blows the stack too:

```clojure
(declare isOdd?)
(defn isEven? [n] (if (zero? n) true (isOdd? (dec n))))
(defn isOdd? [n] (if (zero? n) false (isOven? (dec n))))
(isEven? 100000) ; blow the stack
```

There's a `trampoline` function that takes a function along with arguments and calls it. If it returns a function, it calls that, etc. When it returns a non-function, that's the result:

```clojure
(declare isOdd?)
(defn isEven? [n] (if (zero? n) true #(isOdd? (dec n)))) ; note the #
(defn isOdd? [n] (if (zero? n) false #(isOven? (dec n)))) ; note the #
(trampoline iseven? 100000) ; => true
```

Pretty weird and ugly, I think...

# ClojureScript

- `clj -M -m cljs.main -c hello.core -r`
- `clj -M -m cljs.main -O advanced -c hello.core -r`