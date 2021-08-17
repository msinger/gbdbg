using System;

namespace gbdbg
{
	using F = Sm83StatusFlags;

	public struct Sm83Registers
	{
		public ushort PC;
		public ushort SP;
		public ushort WZ;
		public byte B;
		public byte C;
		public byte D;
		public byte E;
		public byte H;
		public byte L;
		public byte A;
		public F F;
		public bool IME;

		public ushort BC
		{
			get { return (ushort)(((int)B << 8) | C); }
			set { B = (byte)(value >> 8); C = (byte)value; }
		}

		public ushort DE
		{
			get { return (ushort)(((int)D << 8) | E); }
			set { D = (byte)(value >> 8); E = (byte)value; }
		}

		public ushort HL
		{
			get { return (ushort)(((int)H << 8) | L); }
			set { H = (byte)(value >> 8); L = (byte)value; }
		}

		public ushort AF
		{
			get { return (ushort)(((int)A << 8) | (int)F); }
			set { A = (byte)(value >> 8); F = (F)(value & 0xf0); }
		}

		public override string ToString()
		{
			string z = ((F & F.Z) != 0) ? "Z" : "-";
			string n = ((F & F.N) != 0) ? "N" : "-";
			string h = ((F & F.H) != 0) ? "H" : "-";
			string c = ((F & F.C) != 0) ? "C" : "-";

			return
				"PC: 0x" + PC.ToString("x4") + "   SP: 0x" + SP.ToString("x4") + "   WZ: 0x" + WZ.ToString("x4") + "   IME: " + (IME ? "1" : "0") + "\n" +
				"B: 0x" + B.ToString("x2") + "   C: 0x" + C.ToString("x2") + "   BC: 0x" + BC.ToString("x4") + "\n" +
				"D: 0x" + D.ToString("x2") + "   E: 0x" + E.ToString("x2") + "   DE: 0x" + DE.ToString("x4") + "\n" +
				"H: 0x" + H.ToString("x2") + "   L: 0x" + L.ToString("x2") + "   HL: 0x" + HL.ToString("x4") + "\n" +
				"A: 0x" + A.ToString("x2") + "   F: " + z + n + h + c      + "   AF: 0x" + AF.ToString("x4");
		}
	}
}
