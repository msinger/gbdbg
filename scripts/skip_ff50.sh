#!/bin/bash

DEV=${DEV:-/dev/ttyUSB1}

gbdbg $DEV <<EOF
h

# LEDs
wr 0xff00 1
wr 0xff01 0

# Disconnect routes to Port A Pin 0 trigger inputs to make sure DUT gets no clock
wr 0xff10 0
wr 0xff11 0
wr 0xff12 0
wr 0xff13 0
wr 0xff43 1
wr 0xff42 1

# Set Port A Pin 0 to high, others to low
wr 0xff40 1
wr 0xff41 0xfe

# Disable Interrupts
wr 0xffff 0

# Zero out DUT RAM
wr 0x1000-0x1fff 0

# Place jump for safety at the end of DUT RAM
buf a asm
jr -2
end
buf a store 0x1ffe+2

# Place jump at DUT entry point
buf a asm
nop
jr -2
end
buf a store 0x1100+3

# Place Nintendo logo
buf a asm
.db 0xCE 0xED 0x66 0x66 0xCC 0x0D 0x00 0x0B 0x03 0x73 0x00 0x83 0x00 0x0C 0x00 0x0D
.db 0x00 0x08 0x11 0x1F 0x88 0x89 0x00 0x0E 0xDC 0xCC 0x6E 0xE6 0xDD 0xDD 0xD9 0x99
.db 0xBB 0xBB 0x67 0x63 0x6E 0x0E 0xEC 0xCC 0xDD 0xDC 0x99 0x9F 0xBB 0xB9 0x33 0x3E
end
buf a store 0x1104+48

# Place DUT game title
buf a asm
.db 0x73 0x6b 0x69 0x70 0x5f 0x66 0x66 0x35 0x30
end
buf a store 0x1134+9

# Place DUT manufacturer code
buf a asm
.db 0x54 0x45 0x53 0x54
end
buf a store 0x113f+4

# Place DUT new license code
wr 0x1144 0x5a
wr 0x1145 0x5a

# Place DUT destination code
wr 0x114a 1

# Place DUT old license code
wr 0x114b 0x33

# Place DUT header checksum
wr 0x114d 0x78

# DUT code that dumps bootrom
buf a asm
.org 0x200
ld hl, 0
ld bc, 0xa000
ld a, (hl+)
ld (bc), a
inc bc
bit 0, h
jr z, -7
jr -2
end
buf a store 0x1200

# Add some patterns to DUT 0x0000-0x00ff
wr 0x1000 0xc7
wr 0x1008 0xcf
wr 0x1010 0xd7
wr 0x1018 0xdf
wr 0x1020 0xe7
wr 0x1028 0xef
wr 0x1030 0xf7
wr 0x1038 0xff
wr 0x10fd 0xc7
wr 0x10fe 0x55
wr 0x10ff 0xaa

# Reset peripherals
wr 0xff14 0
wr 0xff22 0xff
wr 0xff23 0xff
wr 0xff20 6
wr 0xff26 0xff
wr 0xff27 0xff
wr 0xff24 6
wr 0xff51 1
wr 0xff50 0x0c

# Reset interrupt flags
wr 0xfffe 0

# Assert DUT reset
wr 0xff50 1
# Clock DUT under reset
wr 0xff41 1
wr 0xff40 1
wr 0xff41 1
wr 0xff40 1
wr 0xff41 1
wr 0xff40 1
wr 0xff41 1
wr 0xff40 1
# Deassert DUT reset
wr 0xff50 4

# Make route 3 always one
wr 0xff10 8
wr 0xff14 0

# Route 3 to Counter 0 COUNT
wr 0xff22 0x10

# Configure ~4.28 MHz on counter 0 compare regs 0&1
wr 0xff10 6
wr 0xff21 1
wr 0xff10 13
wr 0xff21 2

# Configure 30 MHz on counter 0 compare regs 2&3&4
wr 0xff10 16
wr 0xff21 4
wr 0xff10 17
wr 0xff21 8
wr 0xff10 18
wr 0xff21 0x10

# Use Route 0 to reset Port A Pin 0 when compare reg 0 or 3 triggers
wr 0xff10 1
wr 0xff23 9
wr 0xff43 1

# Use Route 1 to set Port A Pin 0 and reset counter 0 when compare reg 1 or 4 triggers
wr 0xff10 2
wr 0xff23 0x12
wr 0xff22 4
wr 0xff42 1

# Connect Route 2 to compare reg 2
wr 0xff10 4
wr 0xff23 4

# Trigger when DUT reads address 0x014c
wr 0xff10 0x4c
wr 0xff11 0x81
wr 0xff13 0x04
wr 0xff53 1
wr 0xff10 0xff
wr 0xff11 0xff
#wr 0xff13 0x2f
wr 0xff13 0
wr 0xff53 2

# Route 2 carries DUT bus compare match to counter 0 STOP
wr 0xff10 4
wr 0xff11 0
wr 0xff13 0
wr 0xff51 1
wr 0xff22 2

buf a asm
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
ld (hl-), a
ld (hl+), a
ld (hl-), a
ld (hl+), a
ld (hl-), a
ld (hl+), a
ld (hl-), a
ld (hl+), a
ld (hl-), a
ld (hl+), a
ld (hl-), a
ld (hl+), a
ld (hl-), a

; Start Counter 0 again
ld a, 1
ld (0xff20), a

jr -2

end

buf a store 0x100
set pc 0x100
c
EOF
