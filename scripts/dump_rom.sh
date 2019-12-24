#!/bin/bash

set -e

DEV=${DEV:-/dev/ttyUSB1}

if [ -z "$1" ]; then
	echo Usage: DEV=/dev/ttyUSB1 dump_rom.sh '<file>' >&2
	exit 2
fi

echo h | gbdbg $DEV

type=$(echo rd 0x147 | gbdbg $DEV)
size=$(echo rd 0x148 | gbdbg $DEV)

no_mbc=
has_mbc1=

case "$type" in
0x00|0x08|0x09)
	echo No MBC
	no_mbc=y
	;;
0x01|0x02|0x03)
	echo Has MBC1
	has_mbc1=y
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
   [ -n "$no_mbc" -a $blocks -gt 2 ]; then
	echo Unknown ROM size >&2
	exit 1
fi

if [ -n "$has_mbc1" ]; then
	echo wr 0x0000 0 | gbdbg $DEV
	echo wr 0x6000 0 | gbdbg $DEV
	echo wr 0x4000 0 | gbdbg $DEV
	echo wr 0x2000 0 | gbdbg $DEV
fi

>$1

for (( i = 0; i < blocks; i++ )); do
	echo Reading block $i...

	if [ -n "$has_mbc1" ]; then
		echo wr 0x4000 $(( (i >> 5) & 3 )) | gbdbg $DEV
		echo wr 0x2000 $(( i & 0x1f )) | gbdbg $DEV
	fi

	if (( i % 32 == 0 )); then
	gbdbg $DEV <<EOF
buf a mem 0+0x4000
buf a save $1.blk
EOF
	else
	gbdbg $DEV <<EOF
buf a mem 0x4000+0x4000
buf a save $1.blk
EOF
	fi

	cat $1.blk >>$1
done

rm -f $1.blk
