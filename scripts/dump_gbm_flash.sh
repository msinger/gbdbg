#!/bin/bash

. $(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null && pwd)/gbm_functions

set -e

exec 3>&1 >&2

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

opt_reset=
opt_map_entry=
opt_map_all=
dump_map=
dump_flash=y

case "$1" in
	--reset)
		opt_reset=y
		dump_flash=
		;;
	--map-entry)
		opt_map_entry=$(($2))
		dump_flash=
		;;
	--map-all)
		opt_map_all=y
		dump_flash=
		;;
	--dump-map)
		dump_map=y
		dump_flash=
esac

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

if [ -n "$opt_reset" ]; then
	echo Resetting GBM... >&2
	gbm_reset
fi

if [ -n "$opt_map_entry" ]; then
	echo Setup mapping for entry $opt_map_entry... >&2
	gbm_select_map_entry $opt_map_entry
fi

if [ -n "$opt_map_all" ]; then
	echo Disable mapping\; makes entire flash\&RAM accessible... >&2
	gbm_unmap
fi

if [ -n "$dump_map" ]; then
	echo Unlock reading map... >&2
	gbm_unlock_map

	echo Reading map...
	gbdbg $DEV <<EOF
buf a mem 0+128
buf a save $tmpfile
EOF
	cat "$tmpfile" >&3

	echo Resetting GBM... >&2
	gbm_reset
fi

if [ -n "$dump_flash" ]; then
	echo Disable mapping\; makes entire flash\&RAM accessible... >&2
	gbm_unmap

	for (( i = 0; i < 64; i++ )); do
		echo Reading block $i...
		echo wr 0x2100 $i | gbdbg $DEV
		srcadr=0x4000
		if (( i == 0 )); then
			srcadr=0
		fi
		gbdbg $DEV <<EOF
buf a mem $srcadr+0x4000
buf a save $tmpfile
EOF
		cat "$tmpfile" >&3
	done

	echo Resetting GBM... >&2
	gbm_reset
fi
