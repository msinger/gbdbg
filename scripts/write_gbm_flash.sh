#!/bin/bash

. $(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null && pwd)/gbm_functions

set -e

exec 3>&1 >&2

DEV=${DEV:-/dev/ttyUSB1}

echo h | gbdbg $DEV
echo wr 0xff50 1 | gbdbg $DEV

erase_map=
write_map=
erase_flash=
write_flash=y

case "$1" in
	--erase-map)
		erase_map=y
		write_flash=
		;;
	--write-map)
		write_map=y
		write_flash=
		;;
	--erase)
		erase_flash=y
		write_flash=
		;;
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

echo Reading flash sector 0 protection... >&2
if gbm_is_sector0_protected; then
	echo Sector 0 protected: yes >&2
else
	echo Sector 0 protected: no >&2
fi

echo Disable mapping\; makes entire flash\&RAM accessible... >&2
gbm_unmap

if [ -n "$erase_map" ]; then
	echo Erasing map... >&2
	gbm_erase_map
fi

if [ -n "$erase_flash" ]; then
	echo Erasing flash... >&2
	gbm_erase_flash
fi

if [ -n "$write_map" ]; then
	echo Writing map... >&2
	gbm_write_map
fi

if [ -n "$write_flash" ]; then
	gbm_write_flash -v
fi

echo Resetting GBM... >&2
gbm_reset
