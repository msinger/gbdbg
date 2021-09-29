#!/bin/bash

. $(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" >/dev/null && pwd)/shiva_functions

DEV=${DEV:-/dev/ttyUSB1}

echo Initialize...
init

led 1

# DUT code that dumps bootrom
dut_code $((0x200)) <<"EOF"
	.org 0x200
	ld hl, 0
	ld bc, 0xa000
	ld a, (hli)
	ld (bc), a
	inc bc
	bit 0, h
	jr z, -7
	jr -2
EOF

# Add some patterns to DUT 0x0000-0x00ff
run <<EOF
	wr $(($DUTRAM_START + 0x00)) 0xc7
	wr $(($DUTRAM_START + 0x08)) 0xcf
	wr $(($DUTRAM_START + 0x10)) 0xd7
	wr $(($DUTRAM_START + 0x18)) 0xdf
	wr $(($DUTRAM_START + 0x20)) 0xe7
	wr $(($DUTRAM_START + 0x28)) 0xef
	wr $(($DUTRAM_START + 0x30)) 0xf7
	wr $(($DUTRAM_START + 0x38)) 0xff
	wr $(($DUTRAM_START + 0xfd)) 0xc7
	wr $(($DUTRAM_START + 0xfe)) 0x55
	wr $(($DUTRAM_START + 0xff)) 0xaa
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

sys_code $((0x100)) <<"EOF"
	.org 0x100

	; LED
	ld a, 2
	ld (0xff00), a

	; Start Counter 0
	ld a, 1
	ld (0xff20), a

	; Wait for match
	ld hl, 0xfffe
	ld a, (hl)
	bit 2, a
	jr z, -5

	; If there is no delay in the comparator signal, then the clock
	; output line (bit 0 in 0xff40) should be high at this point.
	; But with an 1 tick delay it should be low already. In this case
	; the first resetting of the clock line during "manually" clocking
	; below would be unnecessary, but it doesn't hurt either.

	; LED
	ld a, 3
	ld (0xff00), a

	; Disconnect route 2 on both ends
	xor a, a
	ld (0xff10), a
	inc a
	ld (0xff51), a
	inc a
	ld (0xff22), a

	; Load 15 to counter 0
	ld a, 15
	ld (0xff10), a
	ld a, 0x80
	ld (0xff20), a
	ld a, 8
	ld (0xff20), a

	; Use Route 1&2 to set Port A Pin 0
	ld a, 6
	ld (0xff10), a
	ld a, 1
	ld (0xff42), a

	; LED
	ld a, 4
	ld (0xff00), a

	; Clock "manually" until right spot
	ld hl, 0xff41
	ld a, 1
	ld (hld), a
	ld (hli), a
	ld (hld), a
	ld (hli), a
	ld (hld), a
	ld (hli), a
	ld (hld), a
	ld (hli), a
	ld (hld), a
	ld (hli), a
	ld (hld), a
	ld (hli), a
	ld (hld), a

	; Start Counter 0 again
	ld a, 1
	ld (0xff20), a

	jr -2
EOF

run "set pc 0x100"
run c

