#!/bin/bash

mbc=$1
size=$2
offset=$3
adr_in=$4
bank=$5

case $mbc in
	0)
		((adr_in &= 0x7fff))
		bank=0
		;;
	2)
		((adr_in &= 0x3fff))
		((bank &= 0x0f))
		;;
	*)
		((adr_in &= 0x3fff))
		((bank &= 0x3f))
		;;
esac

case $size in
	0)
		((bank &= 0x01))
		;;
	1)
		((bank &= 0x03))
		;;
	2)
		((bank &= 0x07))
		;;
	3)
		((bank &= 0x0f))
		;;
	4)
		((bank &= 0x1f))
		;;
	5)
		((bank &= 0x3f))
		;;
	6)
		((bank &= 0x3f))
		;;
	7)
		((adr_in &= 0x3fff))
		bank=0
		;;
esac

adr_out=$((adr_in + (offset << 15) + (bank << 14)))

((adr_out &= 0xfffff))

printf $'0x%05x\n' $adr_out
