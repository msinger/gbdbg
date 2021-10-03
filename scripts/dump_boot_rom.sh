#!/bin/bash

. $(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null && pwd)/shiva_functions

set -e

DEV=${DEV:-/dev/ttyUSB1}

DISASSEMBLE=
BINARYOUT=
if [ "$1" == -d ]; then
	DISASSEMBLE=y
fi
if [ "$1" == -b ]; then
	BINARYOUT=y
fi

echo Initialize... >&2
init

led 1

cur_date=$(date -u +'%F %T.%N%z')

# Add string of current date at DUT 0x400
dut_code $((0x400)) >/dev/null <<EOF
	$(asmgen_string $cur_date)
EOF

echo Booting DUT with boot ROM unlocked... >&2
boot_dut_unlocked

led 2

# DUT code that dumps bootrom
echo Dumping... >&2
dut_run $((0x200)) 5 <<EOF
	; Dump 0x0000-0x00ff to 0xa000
	ld hl, 0
	ld bc, 0xa000
loop1:
	ld a, (hli)
	ld (bc), a
	inc bc
	bit 0, h
	jr z, loop1

	; Copy date string to 0xa100
	ld hl, 0x0400
	ld bc, 0xa100
loop2:
	ld a, (hli)
	ld (bc), a
	inc bc
	bit 0, h
	jr z, loop2
EOF

echo Checking timestamp of dump... >&2
dump=$(run "dump $((DUTRAM_START + 0x100))+${#cur_date}" 2>/dev/null)
str=$(echo "$dump" | sed -e '
	/:[^|]*|.*|/!d
	s/^[^|]*:[^|]*|\(.*\)|[^|]*$/\1/
' | sed -e '
	:next
	N
	$! b next
	s,\n,,g
')
if [ "$cur_date" != "$str" ]; then
	echo Failed >&2
	exit 1
fi
echo "$str" >&2

echo Receiving dump... >&2
if [ -n "$BINARYOUT" ]; then
	dump=$(run "dump $DUTRAM_START+256" 2>/dev/null)
	elements=( $(echo "$dump" | sed -e '
		/:[^|]*|.*|/!d
		s/^[^|]*:\([^|]*\)|.*|[^|]*$/\1/
	') )
	echo "${elements[*]}" | xxd -r -ps
elif [ -n "$DISASSEMBLE" ]; then
	run "dis $DUTRAM_START+256" 2>/dev/null
else
	run "dump $DUTRAM_START+256" 2>/dev/null
fi

