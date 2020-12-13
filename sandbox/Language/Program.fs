open Structure
open Print
open Prelude
open Interpretation

let preludeState = eval false prelude emptyState

eval true [Literal (Number 7.2); Literal (String "ashleyf"); Literal (Quotation [Literal (String "area"); fetch; i]); dip] preludeState |> printDebug
