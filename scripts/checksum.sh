#!/bin/bash

set -e

romfile=$1

tmpfile=
function cleanup () {
	if [ -n "$tmpfile" ]; then
		rm -f "$tmpfile"
	fi
}
trap cleanup EXIT
tmpfile=$(mktemp)

cat "$romfile" >"$tmpfile"

rgbfix -fhg "$tmpfile"

if cmp "$romfile" "$tmpfile"; then
	echo OK
else
	echo Wrong checksum!
fi
