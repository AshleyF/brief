let 'encode7bit [ reverse encode7bit.encode swap [ ]
    let 'encode7bit.encode [ if [ encode7bit.step ] [ cons ] >= 128 dup ]
    let 'encode7bit.step [ encode7bit.encode trunc >>> 7 keep [ cons or 128 and 127 ] ] ]

let 'serialize [ cons cond [
	[ 0 serialize.encode-str >str ]                                           [ word? dup ]
    [ 1 serialize.encode-str >str ]                                           [ sym? dup ]
    [ 2 serialize.encode-str ]                                                [ str? dup ]
    [ 3 >ieee754 ]                                                            [ num? dup ]
    [ 4 compose flatmap [ serialize ] serialize.encode-count ]                [ list? dup ]
    [ 5 compose flatmap [ serialize.keyvalue ] >list serialize.encode-count ] [ map? dup ] ]
    let 'serialize.encode-count [ swap encode7bit count ]
    let 'serialize.keyvalue [ prepose dip [ serialize nip snoc ] serialize.encode-str snoc ]
	let 'serialize.encode-str [ prepose encode7bit count >utf8 ] ]

let 'decode7bit [ decode7bit.decode dip [ 0 0 ]
    let 'decode7bit.decode [ if [ decode7bit.decode ] [ drop -rot ] decode7bit.step ]
    let 'decode7bit.step [ dip [ dip [ + 7 ] 2dip [ + ] -rot <<< pick ] <> tuck and 127 dup snoc ] ]

