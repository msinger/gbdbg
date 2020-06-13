#!/bin/bash

set -e

exec 3>&1 >&2

DEV=${DEV:-/dev/ttyUSB1}

echo h | gbdbg $DEV
echo wr 0xff50 1 | gbdbg $DEV

type=$(echo rd 0x147 | gbdbg $DEV)

case "$type" in
0x20)
	echo Has MBC6
	;;
*)
	echo Unsupported MBC >&2
	exit 1
	;;
esac

tmpfile=
function cleanup () {
	if [ -n "$tmpfile" ]; then
		rm -f "$tmpfile"
	fi
}
trap cleanup EXIT
tmpfile=$(mktemp)

function disable_flash () {
	echo wr 0x0000 0 | gbdbg $DEV
	echo wr 0x1000 1 | gbdbg $DEV
	echo wr 0x0c00 0 | gbdbg $DEV
	echo wr 0x1000 0 | gbdbg $DEV
	echo wr 0x2800 0 | gbdbg $DEV
	echo wr 0x3800 0 | gbdbg $DEV
}

echo wr 0x0000 0 | gbdbg $DEV
echo wr 0x1000 1 | gbdbg $DEV
echo wr 0x0c00 1 | gbdbg $DEV
echo wr 0x1000 0 | gbdbg $DEV
echo wr 0x2800 8 | gbdbg $DEV
echo wr 0x3800 8 | gbdbg $DEV

echo wr 0x3000 2    | gbdbg $DEV
echo wr 0x7555 0xaa | gbdbg $DEV
echo wr 0x3000 1    | gbdbg $DEV
echo wr 0x6aaa 0x55 | gbdbg $DEV
echo wr 0x3000 2    | gbdbg $DEV
echo wr 0x7555 0x90 | gbdbg $DEV
echo wr 0x3000 0    | gbdbg $DEV
mfc_id=$(echo rd 0x6000 | gbdbg $DEV)
dev_id=$(echo rd 0x6001 | gbdbg $DEV)
echo wr 0x3000 2    | gbdbg $DEV
echo wr 0x7555 0xaa | gbdbg $DEV
echo wr 0x3000 1    | gbdbg $DEV
echo wr 0x6aaa 0x55 | gbdbg $DEV
echo wr 0x3000 2    | gbdbg $DEV
echo wr 0x7555 0xf0 | gbdbg $DEV

blocks=0

if (( mfc_id == 0xc2 && dev_id == 0x81 )); then
	blocks=64
fi

echo $blocks Blocks -- $((blocks * 16)) KBytes

if [ $blocks -lt 1 ] || [ $blocks -gt 64 ]; then
	echo Unknown flash size >&2
	disable_flash
	exit 1
fi

for (( i = 0; i < blocks; i++ )); do
	echo Reading block $i...

	echo wr 0x2000 $(( (i & 0x3f) << 1 )) | gbdbg $DEV
	echo wr 0x3000 $(( ( (i & 0x3f) << 1) | 1 )) | gbdbg $DEV

	gbdbg $DEV <<EOF
buf a mem 0x4000+0x4000
buf a save $tmpfile
EOF
	cat "$tmpfile" >&3
done

disable_flash
