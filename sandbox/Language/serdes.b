    'encode7bit.step [ [ 127 and 128 or cons ] keep 7 >>> trunc encode7bit.encode ] let
    'encode7bit.encode [ dup 128 >= [ encode7bit.step ] [ cons ] if ] let
'encode7bit [ [ ] swap encode7bit.encode reverse ] let

    'serialize.encode-str [ >utf8 count encode7bit prepose ] let
    'serialize.keyvalue [ snoc serialize.encode-str [ snoc nip serialize ] dip prepose ] let
    'serialize.encode-count [ count encode7bit swap ] let
'serialize [ [
    [ dup word? ] [ >str serialize.encode-str 0 ]
    [ dup sym? ]  [ >str serialize.encode-str 1 ]
    [ dup str? ]  [ serialize.encode-str 2 ]
    [ dup num? ]  [ >ieee754 3 ]
    [ dup list? ] [ serialize.encode-count [ serialize ] flatmap compose 4 ]
    [ dup map? ]  [ serialize.encode-count >list [ serialize.keyvalue ] flatmap compose 5 ] ] cond cons ] let

    'decode7bit.step [ snoc dup 127 and tuck <> [ pick <<< -rot [ + ] 2dip [ 7 + ] dip ] dip ] let
    'decode7bit.decode [ decode7bit.step [ decode7bit.decode ] [ -rot drop ] if ] let
'decode7bit [ [ 0 0 ] dip decode7bit.decode ] let 

    'deserialize.decode-str [ decode7bit take utf8> ] let
    'deserialize.keyvalue [ deserialize.decode-str swap deserialize.rec -rot [ ! ] dip ] let
    'deserialize.rec [ snoc [
        [ dup 0 = ] [ drop deserialize.decode-str find ]
        [ dup 1 = ] [ drop deserialize.decode-str >sym ]
        [ dup 2 = ] [ drop deserialize.decode-str ]
        [ dup 3 = ] [ drop 8 take ieee754> ]
        [ dup 4 = ] [ drop [ ] swap decode7bit [ deserialize.rec swap [ cons ] dip ] repeat swap reverse ]
        [ dup 5 = ] [ drop { } swap decode7bit [ deserialize.keyvalue ] repeat swap ] ] cond ] let
'deserialize [ deserialize.rec nip ] let
