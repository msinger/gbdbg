#!/bin/bash

. $(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null && pwd)/shiva_functions

set -e

DEV=${DEV:-/dev/ttyUSB1}

MODE=--hex
if [ "$1" == -d ]; then
	MODE=--dis
fi
if [ "$1" == -b ]; then
	MODE=--bin
fi

echo Initialize... >&2
init

led 1

echo Booting DUT with boot ROM unlocked... >&2
boot_dut_unlocked

led 2

echo Receiving dump... >&2
dut_dump $MODE 0 256

led 4
