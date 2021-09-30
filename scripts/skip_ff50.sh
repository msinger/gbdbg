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

# DUT code that dumps bootrom
dut_code $((0x200)) >/dev/null <<EOF
	.org 0x200

	; Dump 0x0000-0x00ff to 0xa000
	ld hl, 0
	ld bc, 0xa000
	ld a, (hli)
	ld (bc), a
	inc bc
	bit 0, h
	jr z, -7

	; Copy date string to 0xa100
	ld hl, 0x0400
	ld bc, 0xa100
	ld a, (hli)
	ld (bc), a
	inc bc
	bit 0, h
	jr z, -7

	; Loop here
	jr -2
EOF

# Add some patterns to DUT 0x0000
dut_code 0 >/dev/null <<"EOF"
	.db 0xde 0xad 0xc0 0xde
EOF

# Make route 3 always one
set_always_one 3

# Route 3 to Counter 0 COUNT
set_counter_count 0 3

# Configure ~4.28 MHz on counter 0 compare regs 0&1
set_counter_comparator 0 0 6
set_counter_comparator 0 1 13

# Configure 30 MHz on counter 0 compare regs 2&3&4
set_counter_comparator 0 2 16
set_counter_comparator 0 3 17
set_counter_comparator 0 4 18

# Use Route 0 to reset Port A Pin 0 when compare reg 0 or 3 triggers
set_counter_match 0 0 0
set_counter_match 0 3 0
set_porta_reset   0   0

# Use Route 1 to set Port A Pin 0 and reset counter 0 when compare reg 1 or 4 triggers
set_counter_match 0 1 1
set_counter_match 0 4 1
set_counter_reset 0   1
set_porta_set     0   1

# Connect Route 2 to compare reg 2
set_counter_match 0 2 2

# Trigger when DUT reads address 0x014c
#set_dut_comparator 0 $((0x0400814c)) $((0x2f00ffff))
set_dut_comparator 0 $((0x0400814c)) $((0x0000ffff))

# Route 2 carries DUT bus compare match to counter 0 STOP
set_dut_match    0 2
set_counter_stop 0 2

len=$(sys_code $((0x100)) <<EOF
	.org 0x100

	; LED
	$(sysgen_led 2)

	; Start Counter 0
	$(sysgen_start_counter 0)

	; Wait for match
	ld hl, $IFLAG
	ld a, (hl)
	bit 2, a
	jr z, -5

	; If there is no delay in the comparator signal, then the clock
	; output line (bit 0 in 0xff40) should be high at this point.
	; But with an 1 tick delay it should be low already. In this case
	; the first resetting of the clock line during "manually" clocking
	; below would be unnecessary, but it doesn't hurt either.

	; LED
	$(sysgen_led 3)

	; Disconnect route 2 on both ends
	$(sysgen_set_dut_match 0)
	$(sysgen_set_counter_stop 0)

	; Load 15 to counter 0
	$(sysgen_set_counter_load 0 15)
	$(sysgen_reload_counter 0)

	; Use Route 1&2 to set Port A Pin 0
	$(sysgen_set_porta_set 0 1 2)

	; LED
	$(sysgen_led 4)

	; Clock "manually" until right spot
	$(
		sysgen_reset_porta $((0x01))
		for ((i = 0; i < 6; i++)); do
			sysgen_set_porta   $((0x01))
			sysgen_reset_porta $((0x01))
		done
	)

	; Start Counter 0 again
	$(sysgen_start_counter 0)

	jr -2
EOF
)

run "set pc 0x100"

# Set breakpoint on "jr -2" instruction at the end
set_breakpoint 0 $((0x100 + len - 2))

echo Booting DUT... >&2
run c

echo Waiting for trigger... >&2
wait_for_halt 7
run h
set_breakpoint 0

echo 'Overclocked "INC HL" instruction.' >&2

echo Waiting for dump... >&2
timeout=5
while true; do
	sleep 1
	if ((timeout > 0)); then
		((timeout--))
	fi
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
	if [ "$cur_date" == "$str" ]; then
		echo Dump complete. >&2
		break
	fi
	if ((timeout != 0)); then
		continue
	fi
	echo Timeout! >&2
	exit 1
done

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

