using System;
using System.IO;

namespace gbdbg
{
	public class Lr35902Disassembler
	{
		private BinaryReader br;

		public Lr35902Disassembler(Stream input)
		{
			br = new BinaryReader(input);
		}

		private string GetRegName(int num)
		{
			switch (num)
			{
			case 0: return "B";
			case 1: return "C";
			case 2: return "D";
			case 3: return "E";
			case 4: return "H";
			case 5: return "L";
			case 6: return "(HL)";
			case 7: return "A";
			default: return null;
			}
		}

		private string GetOpName(int num)
		{
			switch (num)
			{
			case 0: return "ADD";
			case 1: return "ADC";
			case 2: return "SUB";
			case 3: return "SBC";
			case 4: return "AND";
			case 5: return "XOR";
			case 6: return "OR";
			case 7: return "CP";
			default: return null;
			}
		}

		private string GetCbName(int num)
		{
			switch (num)
			{
			case 0: return "RLC";
			case 1: return "RRC";
			case 2: return "RL";
			case 3: return "RR";
			case 4: return "SLA";
			case 5: return "SRA";
			case 6: return "SWAP";
			case 7: return "SRL";
			default: return null;
			}
		}

		public string ReadLine()
		{
			byte instr = br.ReadByte();

			string src = GetRegName(instr & 7);
			string dst = GetRegName((instr >> 3) & 7);
			string op  = GetOpName((instr >> 3) & 7);

			byte? imm0 = null, imm1 = null;

			try
			{
				switch (instr)
				{
				case 0x00: return "00        NOP";
				case 0x10: return "10        STOP";
				case 0x76: return "76        HALT";
				case 0xf3: return "f3        DI";
				case 0xfb: return "fb        EI";
				case 0xcb: return ReadCB();

				case 0x08:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  LD (${2:x2}{1:x2}), SP", instr, imm0, imm1);

				case 0x01:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  LD BC, ${2:x2}{1:x2}", instr, imm0, imm1);
				case 0x11:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  LD DE, ${2:x2}{1:x2}", instr, imm0, imm1);
				case 0x21:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  LD HL, ${2:x2}{1:x2}", instr, imm0, imm1);
				case 0x31:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  LD SP, ${2:x2}{1:x2}", instr, imm0, imm1);
				}

				if (instr >= 0x40 && instr < 0x80)
					return string.Format("{0:x2}        LD {1}, {2}", instr, dst, src);
				if (instr >= 0x80 && instr < 0xc0)
					return string.Format("{0:x2}        {1} A, {2}", instr, op, src);
			}
			catch (EndOfStreamException) { }

			if (imm1.HasValue)
				return string.Format("{0:x2} {1:x2} {2:x2}", instr, imm0, imm1);
			else if (imm0.HasValue)
				return string.Format("{0:x2} {1:x2}", instr, imm0);
			else
				return instr.ToString("x2");
		}

		private string ReadCB()
		{
			byte instr = br.ReadByte();

			string src = GetRegName(instr & 7);
			string op  = GetCbName((instr >> 3) & 7);
			int bit    = (instr >> 3) & 7;

			if (instr < 0x40)
				return string.Format("cb {0:x2}     {1} {2}", instr, op, src);
			if (instr < 0x80)
				return string.Format("cb {0:x2}     BIT {1}, {2}", instr, bit, src);
			if (instr < 0xc0)
				return string.Format("cb {0:x2}     RES {1}, {2}", instr, bit, src);
			return string.Format("cb {0:x2}     SET {1}, {2}", instr, bit, src);
		}
	}
}
