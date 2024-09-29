#!/bin/bash

mbc=$1
rom_size=$2
ram_size=$3
rom_offset=$(($4))
ram_offset=$(($5))

case "$mbc" in
	0|no|none|"no mbc"|"no MBC")
		mbc=0
		;;
	1|mbc1|MBC1)
		mbc=1
		;;
	2|mbc2|MBC2)
		mbc=2
		;;
	3|mbc3|MBC3)
		mbc=3
		;;
	4)
		mbc=4
		;;
	5|mbc5|MBC5)
		mbc=5
		;;
	6)
		mbc=6
		;;
	7)
		mbc=7
		;;
	*)
		echo Invalid MBC >&2
		exit 1
		;;
esac

case "$rom_size" in
	0|32k|32K)
		rom_size=0
		;;
	1|64k|64K)
		rom_size=1
		;;
	2|128k|128K)
		rom_size=2
		;;
	3|256k|256K)
		rom_size=3
		;;
	4|512k|512K)
		rom_size=4
		;;
	5|1m|1M)
		rom_size=5
		;;
	6)
		rom_size=6
		;;
	7|16k|16K)
		rom_size=7
		;;
	*)
		echo Invalid ROM size >&2
		exit 1
		;;
esac

case "$ram_size" in
	0|no|none|"no ram"|"no RAM")
		ram_size=0
		;;
	1|2k|2K|512)
		ram_size=1
		;;
	2|8k|8K)
		ram_size=2
		;;
	3|32k|32K)
		ram_size=3
		;;
	4|64k|64K)
		ram_size=4
		;;
	5|128k|128K)
		ram_size=5
		;;
	6)
		ram_size=6
		;;
	7)
		ram_size=7
		;;
	*)
		echo Invalid RAM size >&2
		exit 1
		;;
esac

if (((rom_offset < 0) || (rom_offset >= 64))); then
	echo Invalid ROM offset >&2
	exit 1
fi

if (((ram_offset < 0) || (ram_offset >= 64))); then
	echo Invalid RAM offset >&2
	exit 1
fi

byte0=$(((mbc << 5) | (rom_size << 2) | (ram_size >> 1)))
byte1=$((((ram_size & 1) << 7) | rom_offset))
byte2=$ram_offset

printf $'%02x %02x %02x\n' $byte0 $byte1 $byte2
