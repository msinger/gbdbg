#!/bin/bash

set -e

exec >&2

DEV=${DEV:-/dev/ttyUSB1}

echo h | gbdbg $DEV

type=$(echo rd 0x147 | gbdbg $DEV)
size=$(echo rd 0x149 | gbdbg $DEV)

no_mbc=
has_mbc1=
has_mbc2=
has_mbc3=

case "$type" in
0x00|0x08|0x09)
	echo No MBC
	no_mbc=y
	;;
0x01|0x02|0x03)
	echo Has MBC1
	has_mbc1=y
	;;
0x05|0x06)
	echo Has MBC2
	has_mbc2=y
	;;
0x0f|0x10|0x11|0x12|0x13)
	echo Has MBC3
	has_mbc3=y
	;;
*)
	echo Unsupported MBC >&2
	exit 1
	;;
esac

blocks=
is2k=

case "$size" in
0x00) blocks=0; ;;
0x01) blocks=0; is2k=y; ;;
0x02) blocks=1; ;;
0x03) blocks=4; ;;
0x04) blocks=16; ;;
0x05) blocks=8; ;;
esac

if [ -z "$blocks" ]; then
	echo Unknown RAM size >&2
	exit 1
fi

if [ -n "$has_mbc2" ]; then
	echo 512x4 Bits -- 256 Bytes
	is2k=
elif [ -n "$is2k" ]; then
	echo 1/4 Blocks -- 2 KBytes
else
	echo $blocks Blocks -- $((blocks * 8)) KBytes

	if [ $blocks -eq 0 ]; then
		echo No RAM >&2
		exit 1
	fi
fi

if [ $blocks -lt 0 ] ||
   [ -n "$has_mbc1" -a $blocks -gt 4 ] ||
   [ -n "$has_mbc3" -a $blocks -gt 8 ] ||
   [ -n "$no_mbc" -a $blocks -gt 1 ]; then
	echo Unknown RAM size >&2
	exit 1
fi

tmpfile=
function cleanup () {
	if [ -n "$tmpfile" ]; then
		rm -f "$tmpfile"
	fi
}
trap cleanup EXIT
tmpfile=$(mktemp)

function disable_ram () {
	if [ -n "$has_mbc1" ] || [ -n "$has_mbc2" ] || [ -n "$has_mbc3" ]; then
		echo wr 0 0 | gbdbg $DEV
	fi
}

if [ -n "$has_mbc1" ]; then
	echo wr 0x6000 1 | gbdbg $DEV
	echo wr 0x4000 0 | gbdbg $DEV
	echo wr 0 0x0a | gbdbg $DEV
elif [ -n "$has_mbc2" ]; then
	echo wr 0 0x0a | gbdbg $DEV
elif [ -n "$has_mbc3" ]; then
	echo wr 0x4000 0 | gbdbg $DEV
	echo wr 0 0x0a | gbdbg $DEV
fi

if [ -n "$has_mbc2" ]; then
	echo Writing MBC2 internal RAM...
	dd of="$tmpfile" bs=512 count=1
	gbdbg $DEV <<EOF
buf a file $tmpfile
buf a store 0xa000+0x200
EOF

	disable_ram
	exit 0
elif [ -n "$is2k" ]; then
	echo Writing 2KB block...
	dd of="$tmpfile" bs=2048 count=1
	gbdbg $DEV <<EOF
buf a file $tmpfile
buf a store 0xa000+0x800
EOF

	disable_ram
	exit 0
fi

for (( i = 0; i < blocks; i++ )); do
	echo Writing block $i...

	if [ -n "$has_mbc1" ]; then
		echo wr 0x4000 $(( i & 3 )) | gbdbg $DEV
	elif [ -n "$has_mbc3" ]; then
		echo wr 0x4000 $(( i & 7 )) | gbdbg $DEV
	fi

	dd of="$tmpfile" bs=8192 count=1
	gbdbg $DEV <<EOF
buf a file $tmpfile
buf a store 0xa000+0x2000
EOF
done

disable_ram
