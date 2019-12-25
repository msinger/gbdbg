#!/bin/bash

set -e

exec 3>&1 >&2

DEV=${DEV:-/dev/ttyUSB1}

echo h | gbdbg $DEV

type=$(echo rd 0x147 | gbdbg $DEV)
size=$(echo rd 0x148 | gbdbg $DEV)

no_mbc=
has_mbc1=
has_mbc2=
has_mbc3=
has_mbc5=

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
0x19|0x1a|0x1b)
	echo Has MBC5
	has_mbc5=y
	;;
*)
	echo Unsupported MBC >&2
	exit 1
	;;
esac

blocks=0

case "$size" in
0x00) blocks=2; ;;
0x01) blocks=4; ;;
0x02) blocks=8; ;;
0x03) blocks=16; ;;
0x04) blocks=32; ;;
0x05) blocks=64; ;;
0x06) blocks=128; ;;
0x07) blocks=256; ;;
0x08) blocks=512; ;;
esac

echo $blocks Blocks -- $((blocks * 16)) KBytes

if [ $blocks -lt 2 ] ||
   [ -n "$has_mbc1" -a $blocks -gt 128 ] ||
   [ -n "$has_mbc2" -a $blocks -gt 16 ] ||
   [ -n "$has_mbc3" -a $blocks -gt 128 ] ||
   [ -n "$has_mbc5" -a $blocks -gt 65536 ] ||
   [ -n "$no_mbc" -a $blocks -gt 2 ]; then
	echo Unknown ROM size >&2
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

if [ -n "$has_mbc1" ] ||
   [ -n "$has_mbc2" ] ||
   [ -n "$has_mbc3" ] ||
   [ -n "$has_mbc5" ]; then
	echo wr 0x0000 0 | gbdbg $DEV
fi
if [ -n "$has_mbc1" ]; then
	echo wr 0x6000 0 | gbdbg $DEV
fi
if [ -n "$has_mbc5" ]; then
	echo wr 0x4000 0 | gbdbg $DEV
fi

for (( i = 0; i < blocks; i++ )); do
	echo Reading block $i...

	if [ -n "$has_mbc1" ]; then
		echo wr 0x4000 $(( (i >> 5) & 3 )) | gbdbg $DEV
		echo wr 0x2000 $(( i & 0x1f )) | gbdbg $DEV
	elif [ -n "$has_mbc2" ]; then
		echo wr 0x2100 $(( i & 0xf )) | gbdbg $DEV
	elif [ -n "$has_mbc3" ]; then
		echo wr 0x2000 $(( i & 0x7f )) | gbdbg $DEV
	elif [ -n "$has_mbc5" ]; then
		echo wr 0x3000 $(( (i >> 8) & 0xff )) | gbdbg $DEV
		echo wr 0x2000 $(( i & 0xff )) | gbdbg $DEV
	fi

	srcadr=0x4000
	if (( i == 0 )); then
		srcadr=0
	elif [ -n "$has_mbc1" ] && (( i % 32 == 0 )); then
		srcadr=0
	fi

	gbdbg $DEV <<EOF
buf a mem $srcadr+0x4000
buf a save $tmpfile
EOF
	cat "$tmpfile" >&3
done
