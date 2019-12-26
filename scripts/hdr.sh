#!/bin/bash

dd status=none of=/dev/null count=1 bs=256
dd status=none of=/dev/null count=1 bs=4
dd status=none of=/dev/null count=3 bs=16

echo -n "Game Title:        "
dd status=none count=11 bs=1 | od -tc -An

echo -n "Game Code:         "
dd status=none count=4 bs=1 | od -tc -An

cgb_sup=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
cgb_sup_str=$(printf 0x%02x $cgb_sup)
if (( cgb_sup == 0x00 )); then
	cgb_sup_str="$cgb_sup_str (CGB incompatible)"
elif (( cgb_sup == 0x80 )); then
	cgb_sup_str="$cgb_sup_str (CGB compatible)"
elif (( cgb_sup == 0xc0 )); then
	cgb_sup_str="$cgb_sup_str (CGB exclusive)"
fi
echo "CGB Support Code:  $cgb_sup_str"

echo -n "Maker Code:        "
dd status=none count=2 bs=1 | od -tc -An

sgb_sup=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
sgb_sup_str=$(printf 0x%02x $sgb_sup)
if (( sgb_sup == 0x00 )); then
	sgb_sup_str="$sgb_sup_str (Game Boy)"
elif (( sgb_sup == 0x03 )); then
	sgb_sup_str="$sgb_sup_str (Uses Super Game Boy Functions)"
fi
echo "SGB Support Code:  $sgb_sup_str"

cart_type=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
cart_type_str=$(printf 0x%02x $cart_type)
if (( cart_type == 0x00 )); then
	cart_type_str="$cart_type_str (ROM)"
elif (( cart_type == 0x01 )); then
	cart_type_str="$cart_type_str (ROM, MBC1)"
elif (( cart_type == 0x02 )); then
	cart_type_str="$cart_type_str (ROM, MBC1, SRAM)"
elif (( cart_type == 0x03 )); then
	cart_type_str="$cart_type_str (ROM, MBC1, SRAM, BAT)"
elif (( cart_type == 0x05 )); then
	cart_type_str="$cart_type_str (ROM, MBC2)"
elif (( cart_type == 0x06 )); then
	cart_type_str="$cart_type_str (ROM, MBC2, BAT)"
elif (( cart_type == 0x08 )); then
	cart_type_str="$cart_type_str (ROM, SRAM)"
elif (( cart_type == 0x09 )); then
	cart_type_str="$cart_type_str (ROM, SRAM, BAT)"
elif (( cart_type == 0x0f )); then
	cart_type_str="$cart_type_str (ROM, MBC3, RTC, BAT)"
elif (( cart_type == 0x10 )); then
	cart_type_str="$cart_type_str (ROM, MBC3, RTC, SRAM, BAT)"
elif (( cart_type == 0x11 )); then
	cart_type_str="$cart_type_str (ROM, MBC3)"
elif (( cart_type == 0x12 )); then
	cart_type_str="$cart_type_str (ROM, MBC3, SRAM)"
elif (( cart_type == 0x13 )); then
	cart_type_str="$cart_type_str (ROM, MBC3, SRAM, BAT)"
elif (( cart_type == 0x19 )); then
	cart_type_str="$cart_type_str (ROM, MBC5)"
elif (( cart_type == 0x1a )); then
	cart_type_str="$cart_type_str (ROM, MBC5, SRAM)"
elif (( cart_type == 0x1b )); then
	cart_type_str="$cart_type_str (ROM, MBC5, SRAM, BAT)"
fi
echo "Cartridge Type:    $cart_type_str"

rom_size=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
rom_size_str=$(printf 0x%02x $rom_size)
if (( rom_size == 0x00 )); then
	rom_size_str="$rom_size_str (32 KByte)"
elif (( rom_size == 0x01 )); then
	rom_size_str="$rom_size_str (64 KByte)"
elif (( rom_size == 0x02 )); then
	rom_size_str="$rom_size_str (128 KByte)"
elif (( rom_size == 0x03 )); then
	rom_size_str="$rom_size_str (256 KByte)"
elif (( rom_size == 0x04 )); then
	rom_size_str="$rom_size_str (512 KByte)"
elif (( rom_size == 0x05 )); then
	rom_size_str="$rom_size_str (1024 KByte)"
elif (( rom_size == 0x06 )); then
	rom_size_str="$rom_size_str (2048 KByte)"
elif (( rom_size == 0x07 )); then
	rom_size_str="$rom_size_str (4096 KByte)"
elif (( rom_size == 0x08 )); then
	rom_size_str="$rom_size_str (8192 KByte)"
fi
echo "ROM Size:          $rom_size_str"

ram_size=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
ram_size_str=$(printf 0x%02x $ram_size)
if (( ram_size == 0x00 )); then
	ram_size_str="$ram_size_str (No RAM or MBC2)"
elif (( ram_size == 0x01 )); then
	ram_size_str="$ram_size_str (2 KByte)"
elif (( ram_size == 0x02 )); then
	ram_size_str="$ram_size_str (8 KByte)"
elif (( ram_size == 0x03 )); then
	ram_size_str="$ram_size_str (32 KByte)"
elif (( ram_size == 0x04 )); then
	ram_size_str="$ram_size_str (128 KByte)"
elif (( ram_size == 0x05 )); then
	ram_size_str="$ram_size_str (64 KByte)"
fi
echo "RAM Size:          $ram_size_str"

dest_code=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
dest_code_str=$(printf 0x%02x $dest_code)
if (( dest_code == 0x00 )); then
	dest_code_str="$dest_code_str (Japan)"
elif (( dest_code == 0x01 )); then
	dest_code_str="$dest_code_str (All others)"
fi
echo "Destination Code:  $dest_code_str"

lic_code=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
lic_code_str=$(printf 0x%02x $lic_code)
if (( lic_code == 0x33 )); then
	lic_code_str="$lic_code_str (Use Maker Code)"
fi
echo "Old License Code:  $lic_code_str"

rom_version=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
echo "Mask ROM Version:  $rom_version"

complement=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
complement_str=$(printf 0x%02x $complement)
echo "Complement Check:  $complement_str"

chksum_hi=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
chksum_lo=$(( $(dd status=none count=1 bs=1 | od -td -An) ))
chksum=$(( chksum_lo | (chksum_hi << 8) ))
chksum_str=$(printf 0x%04x $chksum)
echo "Check Sum:         $chksum_str"
