#!/bin/bash

set -e

exec 3>&1 >&2

DEV=${DEV:-/dev/ttyUSB1}

echo h | gbdbg $DEV
echo wr 0xff50 1 | gbdbg $DEV

type=$(echo rd 0x147 | gbdbg $DEV)
size=$(echo rd 0x148 | gbdbg $DEV)

no_mbc=
has_mbc1=
has_mbc2=
has_mmm01=
has_mbc3=
has_mbc5=
has_mbc6=
has_mbc7=
has_macgbd=
has_tama5=
has_huc3=
has_huc1=

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
0x0b|0x0c|0x0d)
	echo Has MMM01
	has_mmm01=y
	;;
0x0f|0x10|0x11|0x12|0x13)
	echo Has MBC3
	has_mbc3=y
	;;
0x19|0x1a|0x1b|0x1c|0x1d|0x1e)
	echo Has MBC5
	has_mbc5=y
	;;
0x20)
	echo Has MBC6
	has_mbc6=y
	;;
0x22)
	echo Has MBC7
	has_mbc7=y
	;;
0xfc)
	echo Has MAC-GBD '(Game Boy Camera)'
	has_macgbd=y
	;;
0xfd)
	echo Has TAMA5
	has_tama5=y
	;;
0xfe)
	echo Has HuC3
	has_huc3=y
	;;
0xff)
	echo Has HuC1
	has_huc1=y
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
0x52) blocks=72; ;;
0x53) blocks=80; ;;
0x54) blocks=96; ;;
esac

echo $blocks Blocks -- $((blocks * 16)) KBytes

if [ $blocks -lt 2 ] ||
   [ -n "$has_mbc1" -a $blocks -gt 128 ] ||
   [ -n "$has_mbc2" -a $blocks -gt 16 ] ||
   [ -n "$has_mmm01" -a $blocks -gt 512 ] ||
   [ -n "$has_mbc3" -a $blocks -gt 128 ] ||
   [ -n "$has_mbc5" -a $blocks -gt 512 ] ||
   [ -n "$has_mbc6" -a $blocks -gt 64 ] ||
   [ -n "$has_mbc7" -a $blocks -gt 128 ] ||
   [ -n "$has_macgbd" -a $blocks -gt 64 ] ||
   [ -n "$has_tama5" -a $blocks -gt 32 ] ||
   [ -n "$has_huc3" -a $blocks -gt 128 ] ||
   [ -n "$has_huc1" -a $blocks -gt 64 ] ||
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
   [ -n "$has_mmm01" ] ||
   [ -n "$has_mbc3" ] ||
   [ -n "$has_mbc5" ] ||
   [ -n "$has_mbc6" ] ||
   [ -n "$has_mbc7" ] ||
   [ -n "$has_macgbd" ] ||
   [ -n "$has_huc3" ] ||
   [ -n "$has_huc1" ]; then
	echo wr 0x0000 0 | gbdbg $DEV
fi
if [ -n "$has_mbc6" ]; then
	echo wr 0x1000 1 | gbdbg $DEV
	echo wr 0x0c00 0 | gbdbg $DEV
	echo wr 0x1000 0 | gbdbg $DEV
	echo wr 0x2800 0 | gbdbg $DEV
	echo wr 0x3800 0 | gbdbg $DEV
fi
if [ -n "$has_mbc1" ] ||
   [ -n "$has_mmm01" ]; then
	echo wr 0x6000 0 | gbdbg $DEV
fi
if [ -n "$has_mmm01" ] ||
   [ -n "$has_mbc5" ]; then
	echo wr 0x4000 0 | gbdbg $DEV
fi
if [ -n "$has_mmm01" ]; then
	echo wr 0x2000 0 | gbdbg $DEV
fi
if [ -n "$has_tama5" ]; then
	echo wr 0xa001 0xa | gbdbg $DEV
	sleep 1
	if (( ( 0xf & $(echo rd 0xa000 | gbdbg $DEV) ) != 1 )); then
		echo TAMA5 does not read 1 from 0xA000 >&2
		exit 1
	fi
fi

function setup_mmm01 () {
	local bank=$1
	echo wr 0x2000 $(( bank & 0x7f )) | gbdbg $DEV
	echo wr 0x6000 1 | gbdbg $DEV
	echo wr 0x4000 $(( 0x40 | ( (bank & 0x180) >> 3 ) )) | gbdbg $DEV
	echo wr 0x0000 0x40 | gbdbg $DEV
}

for (( i = 0; i < blocks; i++ )); do
	if [ -n "$has_mmm01" ] && (( (i & 0x1f) == 0 )); then
		if (( i > 0 )); then
			echo MMM01 needs reset: Please reset target, then press enter... >&2
			read
			echo h | gbdbg $DEV
			echo wr 0xff50 1 | gbdbg $DEV
		fi
		setup_mmm01 $i
	fi

	echo Reading block $i...

	if [ -n "$has_mbc1" ]; then
		echo wr 0x4000 $(( (i >> 5) & 3 )) | gbdbg $DEV
		echo wr 0x2000 $(( i & 0x1f )) | gbdbg $DEV
	elif [ -n "$has_mbc2" ]; then
		echo wr 0x2100 $(( i & 0xf )) | gbdbg $DEV
	elif [ -n "$has_mmm01" ]; then
		echo wr 0x2000 $(( i & 0x1f )) | gbdbg $DEV
	elif [ -n "$has_mbc3" ] || [ -n "$has_mbc7" ] || [ -n "$has_huc3" ]; then
		echo wr 0x2000 $(( i & 0x7f )) | gbdbg $DEV
	elif [ -n "$has_mbc5" ]; then
		echo wr 0x3000 $(( (i >> 8) & 1 )) | gbdbg $DEV
		echo wr 0x2000 $(( i & 0xff )) | gbdbg $DEV
	elif [ -n "$has_mbc6" ]; then
		echo wr 0x2000 $(( (i & 0x3f) << 1 )) | gbdbg $DEV
		echo wr 0x3000 $(( ( (i & 0x3f) << 1) | 1 )) | gbdbg $DEV
	elif [ -n "$has_macgbd" ] || [ -n "$has_huc1" ]; then
		echo wr 0x2000 $(( i & 0x3f )) | gbdbg $DEV
	elif [ -n "$has_tama5" ]; then
		echo wr 0xa001 1 | gbdbg $DEV
		echo wr 0xa000 $(( (i >> 4) & 1 )) | gbdbg $DEV
		echo wr 0xa001 0 | gbdbg $DEV
		echo wr 0xa000 $(( i & 0xf )) | gbdbg $DEV
	fi

	srcadr=0x4000
	if (( i == 0 )); then
		srcadr=0
	elif [ -n "$has_mmm01" ] && (( (i & 0x1f) == 0 )); then
		srcadr=0
	fi

	gbdbg $DEV <<EOF
buf a mem $srcadr+0x4000
buf a save $tmpfile
EOF
	cat "$tmpfile" >&3
done
