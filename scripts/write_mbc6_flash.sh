#!/bin/bash

. $(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null && pwd)/mbc6_functions

set -e

exec 3>&1 >&2

DEV=${DEV:-/dev/ttyUSB1}

echo h | gbdbg $DEV
echo wr 0xff50 1 | gbdbg $DEV

erase_hidden=
write_hidden=
erase_flash=
write_flash=y

case "$1" in
	--erase-hidden)
		erase_hidden=y
		write_flash=
		;;
	--write-hidden)
		write_hidden=y
		write_flash=
		;;
	--erase)
		erase_flash=y
		write_flash=
		;;
esac

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

if [ -n "$erase_hidden" ]; then
	echo Erasing hidden region... >&2
	mbc6_erase_hidden
fi

if [ -n "$erase_flash" ]; then
	echo Erasing flash... >&2
	mbc6_erase_flash
fi

if [ -n "$write_hidden" ]; then
	echo Writing hidden region... >&2
	mbc6_write_hidden
fi

if [ -n "$write_flash" ]; then
	mbc6_write_flash -v
fi

echo Resetting MBC6... >&2
mbc6_reset
