#!/bin/bash

. $(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null && pwd)/mbc6_functions

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

opt_reset=
dump_hidden=
dump_flash=y

case "$1" in
	--reset)
		opt_reset=y
		dump_flash=
		;;
	--dump-hidden)
		dump_hidden=y
		dump_flash=
esac

echo Resetting MBC6... >&2
mbc6_reset

echo Reading flash ID... >&2
flashid=$(mbc6_read_flash_id)
echo Flash ID: $flashid >&2
if ((flashid != 0xc281)); then
	echo Unknown flash ID! >&2
	exit 1
fi

echo Reading flash sector 0 protection... >&2
if mbc6_is_sector0_protected; then
	echo Sector 0 protected: yes >&2
else
	echo Sector 0 protected: no >&2
fi

if [ -n "$opt_reset" ]; then
	echo Resetting MBC6... >&2
	mbc6_reset
fi

if [ -n "$dump_hidden" ]; then
	echo Unlock reading hidden region... >&2
	mbc6_unlock_hidden

	echo Reading hidden region...
	gbdbg $DEV <<EOF
buf a mem 0x4000+256
buf a save $tmpfile
EOF
	cat "$tmpfile" >&3

	echo Resetting MBC6... >&2
	mbc6_reset
fi

if [ -n "$dump_flash" ]; then
	mbc6_enable_flash

	for (( i = 0; i < 64; i++ )); do
		echo Reading block $i...

		echo wr 0x2000 $(( (i & 0x3f) << 1 )) | gbdbg $DEV
		echo wr 0x3000 $(( ( (i & 0x3f) << 1) | 1 )) | gbdbg $DEV

		gbdbg $DEV <<EOF
buf a mem 0x4000+0x4000
buf a save $tmpfile
EOF
		cat "$tmpfile" >&3
	done

	echo Resetting MBC6... >&2
	mbc6_reset
fi
