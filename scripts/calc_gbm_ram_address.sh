#!/bin/bash

mbc=$1
size=$2
offset=$3
adr_in=$4
bank=$5

case $mbc in
	0)
		((adr_in &= 0xffff))
		bank=0
		;;
	1)
		((adr_in &= 0x1fff))
		((bank &= 0x03))
		;;
	2)
		((adr_in &= 0x1fff))
		bank=0
		;;
	3)
		((adr_in &= 0x1fff))
		((bank &= 0x03))
		;;
	*)
		((adr_in &= 0x1fff))
		((bank &= 0x0f))
		;;
esac

case $size in
	1)
		((adr_in &= 0x07ff))
		if ((mbc == 2)); then
			((adr_in &= 0x01ff))
		fi
		bank=0
		;;
	2)
		((adr_in &= 0x1fff))
		bank=0
		;;
	3)
		((adr_in &= 0x7fff))
		((bank &= 0x03))
		;;
	4)
		((bank &= 0x07))
		;;
	5)
		((bank &= 0x0f))
		;;
	*)
		((adr_in &= 0x00ff)) # plus no chip enable, and no read/write signal
		bank=0
		offset=0
		;;
esac

adr_out=$((adr_in + (offset << 11) + (bank << 13)))

((adr_out &= 0x1ffff))

printf $'0x%05x\n' $adr_out
