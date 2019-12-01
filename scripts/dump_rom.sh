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

has_mbc1=

case "$type" in
0x00|0x08|0x09)
	echo No MBC
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

if [ $blocks -lt 2 ] || [ $blocks -gt 128 ] || [ -z "$has_mbc1" -a $blocks -gt 2 ]; then
	echo Unknown ROM size >&2
	exit 1
fi

echo Reading block 0...
gbdbg $DEV <<EOF
buf a mem 0+0x4000
buf a save $1
EOF

if [ -n "$has_mbc1" ]; then
	echo wr 0x6000 0 | gbdbg $DEV
fi

for (( i = 1; i < blocks; i++ )); do
	echo Reading block $i...

	if [ -n "$has_mbc1" ]; then
		echo wr 0x4000 $(( (i >> 5) & 3 )) | gbdbg $DEV
		echo wr 0x2000 $(( i & 0x1f )) | gbdbg $DEV
	fi

	gbdbg $DEV <<EOF
buf a mem 0x4000+0x4000
buf a save $1.blk
EOF

	cat $1.blk >>$1
done

rm -f $1.blk
