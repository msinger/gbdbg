#!/bin/bash

byte0=$((0x$1))
byte1=$((0x$2))
byte2=$((0x$3))

if (((byte0 < 0) || (byte0 >= 256))); then
	echo Byte 0 invalid! >&2
	exit 1
fi

if (((byte1 < 0) || (byte1 >= 256))); then
	echo Byte 1 invalid! >&2
	exit 1
fi

if (((byte2 < 0) || (byte2 >= 256))); then
	echo Byte 2 invalid! >&2
	exit 1
fi

mbc=$((byte0 >> 5))

if (((mbc == 6) || (mbc == 7))); then
	echo Invalid MBC, fallback to 00 00 00. >&2
	byte0=0
	byte1=0
	byte2=0
	mbc=0
fi

if ((((byte1 & 0x40) != 0) || ((byte2 & 0xc0) != 0))); then
	((byte1 &= ~0x40))
	((byte2 &= ~0xc0))
	printf $'Clear unused bits: %02x %02x %02x\n' $byte0 $byte1 $byte2 >&2
fi

rom_size=$(((byte0 >> 2) & 7))
ram_size=$((((byte1 >> 7) & 1) | ((byte0 << 1) & 6)))
rom_offset=$((byte1 & 0x3f))
ram_offset=$((byte2 & 0x3f))

case $mbc in
	0)
		echo MBC type: 0 - no MBC
		;;
	1)
		echo MBC type: 1 - MBC1
		;;
	2)
		echo MBC type: 2 - MBC2
		;;
	3)
		echo MBC type: 3 - MBC3
		;;
	4)
		echo MBC type: 4 - MBC5 like
		;;
	5)
		echo MBC type: 5 - MBC5
		;;
esac

case $rom_size in
	0)
		echo ROM size: 0 - 32 KiB
		;;
	1)
		echo ROM size: 1 - 64 KiB
		;;
	2)
		echo ROM size: 2 - 128 KiB
		;;
	3)
		echo ROM size: 3 - 256 KiB
		;;
	4)
		echo ROM size: 4 - 512 KiB
		;;
	5)
		echo ROM size: 5 - 1 MiB
		;;
	6)
		echo ROM size: 6 - 1 MiB alternative
		;;
	7)
		echo ROM size: 7 - 16 KiB
		;;
esac

case $ram_size in
	0)
		echo RAM size: 0 - no RAM
		;;
	1)
		if ((mbc == 2)); then
			echo RAM size: 1 - 512 B
		else
			echo RAM size: 1 - 2 KiB
		fi
		;;
	2)
		echo RAM size: 2 - 8 KiB
		;;
	3)
		echo RAM size: 3 - 32 KiB
		;;
	4)
		echo RAM size: 4 - 64 KiB
		;;
	5)
		echo RAM size: 5 - 128 KiB
		;;
	6)
		echo RAM size: 6 - no RAM alternative1
		;;
	7)
		echo RAM size: 7 - no RAM alternative2
		;;
esac

printf $'ROM offset: %d - %d KiB @0x%05x\n' $rom_offset $((rom_offset * 32)) $(((rom_offset * 32 * 1024) & 0xfffff))
printf $'RAM offset: %d - %d KiB @0x%05x\n' $ram_offset $((ram_offset * 2)) $((ram_offset * 2 * 1024))

