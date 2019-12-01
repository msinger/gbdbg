#!/bin/bash

set -e

DEV=${DEV:-/dev/ttyUSB1}

if [ -z "$1" ]; then
	echo Usage: DEV=/dev/ttyUSB1 dump_ram.sh '<file>' >&2
	exit 2
fi

echo h | gbdbg $DEV

type=$(echo rd 0x147 | gbdbg $DEV)
size=$(echo rd 0x149 | gbdbg $DEV)

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

blocks=
is2k=

case "$size" in
0x00) blocks=0; ;;
0x01) blocks=1; is2k=y; ;;
0x02) blocks=1; ;;
0x03) blocks=4; ;;
0x04) blocks=16; ;;
0x05) blocks=8; ;;
esac

if [ -z "$blocks" ]; then
	echo Unknown RAM size >&2
	exit 1
fi

if [ -n "$is2k" ]; then
	echo 1/4 Blocks -- 2 KBytes
else
	echo $blocks Blocks -- $((blocks * 8)) KBytes
fi

if [ $blocks -eq 0 ]; then
	echo No RAM >&2
	exit 1
fi

if [ $blocks -lt 0 ] || [ $blocks -gt 4 ] || [ -z "$has_mbc1" -a $blocks -gt 1 ]; then
	echo Unknown RAM size >&2
	exit 1
fi

function disable_ram () {
	if [ -n "$has_mbc1" ]; then
		echo wr 0 0 | gbdbg $DEV
	fi
}

if [ -n "$has_mbc1" ]; then
	echo wr 0x6000 1 | gbdbg $DEV
	echo wr 0x4000 0 | gbdbg $DEV
	echo wr 0 0x0a | gbdbg $DEV
fi

if [ -n "$is2k" ]; then
	echo Reading block 0...
	gbdbg $DEV <<EOF
buf a mem 0xa000+0x2000
buf a save $1
EOF

	disable_ram
	exit 0
fi

rm -f $1

for (( i = 0; i < blocks; i++ )); do
	echo Reading block $i...

	if [ -n "$has_mbc1" ]; then
		echo wr 0x4000 $(( i & 3 )) | gbdbg $DEV
	fi

	gbdbg $DEV <<EOF
buf a mem 0xa000+0x2000
buf a save $1.blk
EOF

	cat $1.blk >>$1
done

disable_ram
rm -f $1.blk
