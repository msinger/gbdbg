#!/bin/bash

. $(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null && pwd)/gbm_functions

set -e

exec >&2

DEV=${DEV:-/dev/ttyUSB1}

echo h | gbdbg $DEV
echo wr 0xff50 1 | gbdbg $DEV

tmpfile=
function cleanup () {
	if [ -n "$tmpfile" ]; then
		rm -f "$tmpfile"
	fi
}
trap cleanup EXIT
tmpfile=$(mktemp)

if ! gbm_detect; then
	echo NP GB Memory cartridge not detected. Try power cycling cartridge. >&2
	exit 1
fi

echo Resetting GBM... >&2
gbm_reset

echo Reading flash ID... >&2
flashid=$(gbm_read_flash_id)
echo Flash ID: $flashid >&2
if ((flashid != 0xc289)); then
	echo Unknown flash ID! >&2
	exit 1
fi

echo Disable mapping\; makes entire flash\&RAM accessible... >&2
gbm_unmap

echo wr 0 0x0a | gbdbg $DEV
for (( i = 0; i < 16; i++ )); do
	echo Writing block $i...
	echo wr 0x4000 $i | gbdbg $DEV
	dd of="$tmpfile" bs=8192 count=1
	gbdbg $DEV <<EOF
buf a file $tmpfile
buf a store 0xa000+0x2000
EOF
done
echo wr 0 0 | gbdbg $DEV

echo Resetting GBM... >&2
gbm_reset
