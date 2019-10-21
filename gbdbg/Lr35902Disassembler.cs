using System;
using System.IO;

namespace gbdbg
{
	public class Lr35902Disassembler
	{
		private readonly BinaryReader br;

		public Lr35902Disassembler(Stream input)
		{
			br = new BinaryReader(input);
		}

		private static string GetRegName(int num)
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

		private static string GetOpName(int num)
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

		private static string GetCbName(int num)
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

		private static string GetR16Name(int num)
		{
			switch (num)
			{
			case 0: return "BC";
			case 1: return "DE";
			case 2: return "HL";
			case 3: return "SP";
			default: return null;
			}
		}

		private static string GetR16PPName(int num)
		{
			switch (num)
			{
			case 0: return "BC";
			case 1: return "DE";
			case 2: return "HL";
			case 3: return "AF";
			default: return null;
			}
		}

		private static string GetR16IndName(int num)
		{
			switch (num)
			{
			case 0: return "BC";
			case 1: return "DE";
			case 2: return "HL+";
			case 3: return "HL-";
			default: return null;
			}
		}

		public string ReadLine()
		{
			return ReadLine(0);
		}

		public string ReadLine(long? offset)
		{
			bool comment_rel = br.BaseStream.CanSeek && offset.HasValue;
			ushort pc = 0;
			if (comment_rel)
				pc = (ushort)(br.BaseStream.Position + offset);

			byte instr = br.ReadByte();

			string src    = GetRegName(instr & 7);
			string dst    = GetRegName((instr >> 3) & 7);
			string op     = GetOpName((instr >> 3) & 7);
			string r16    = GetR16Name((instr >> 4) & 3);
			string r16pp  = GetR16PPName((instr >> 4) & 3);
			string r16ind = GetR16IndName((instr >> 4) & 3);

			string cond = null;
			switch (instr & 0x18)
			{
			case 0x00: cond = "NZ"; break;
			case 0x10: cond = "NC"; break;
			case 0x08: cond = "Z";  break;
			case 0x18: cond = "C";  break;
			}

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
				case 0x07: return "07        RLCA";
				case 0x17: return "17        RLA";
				case 0x27: return "27        DAA";
				case 0x37: return "37        SCF";
				case 0x0f: return "0f        RRCA";
				case 0x1f: return "1f        RRA";
				case 0x2f: return "2f        CPL";
				case 0x3f: return "3f        CCF";
				case 0xe9: return "e9        JP (HL)";
				case 0xc9: return "c9        RET";
				case 0xd9: return "d9        RETI";
				case 0xf9: return "f9        LD SP, HL";
				case 0xe2: return "e2        LD ($ff00 + C), A";
				case 0xf2: return "f2        LD A, ($ff00 + C)";
				case 0xcb: return ReadCB();

				case 0x08:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  LD (${2:x2}{1:x2}), SP", instr, imm0, imm1);

				case 0x01: case 0x11: case 0x21: case 0x31:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  LD {3}, ${2:x2}{1:x2}", instr, imm0, imm1, r16);

				case 0x06: case 0x16: case 0x26: case 0x36:
				case 0x0e: case 0x1e: case 0x2e: case 0x3e:
					imm0 = br.ReadByte();
					return string.Format("{0:x2} {1:x2}     LD {2}, ${1:x2}", instr, imm0, dst);

				case 0x02: case 0x12: case 0x22: case 0x32:
					return string.Format("{0:x2}        LD ({1}), A", instr, r16ind);

				case 0x0a: case 0x1a: case 0x2a: case 0x3a:
					return string.Format("{0:x2}        LD A, ({1})", instr, r16ind);

				case 0xea:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  LD (${2:x2}{1:x2}), A", instr, imm0, imm1);

				case 0xfa:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  LD A, (${2:x2}{1:x2})", instr, imm0, imm1);

				case 0xf8:
					imm0 = br.ReadByte();
					return string.Format("{0:x2} {1:x2}     LD HL, SP + ${1:x2}", instr, imm0);

				case 0xe0:
					imm0 = br.ReadByte();
					return string.Format("{0:x2} {1:x2}     LD ($ff00 + ${1:x2}), A", instr, imm0);

				case 0xf0:
					imm0 = br.ReadByte();
					return string.Format("{0:x2} {1:x2}     LD A, ($ff00 + ${1:x2})", instr, imm0);

				case 0xc1: case 0xd1: case 0xe1: case 0xf1:
					return string.Format("{0:x2}        POP {1}", instr, r16pp);

				case 0xc5: case 0xd5: case 0xe5: case 0xf5:
					return string.Format("{0:x2}        PUSH {1}", instr, r16pp);

				case 0x04: case 0x14: case 0x24: case 0x34:
				case 0x0c: case 0x1c: case 0x2c: case 0x3c:
					return string.Format("{0:x2}        INC {1}", instr, dst);

				case 0x05: case 0x15: case 0x25: case 0x35:
				case 0x0d: case 0x1d: case 0x2d: case 0x3d:
					return string.Format("{0:x2}        DEC {1}", instr, dst);

				case 0x03: case 0x13: case 0x23: case 0x33:
					return string.Format("{0:x2}        INC {1}", instr, r16);

				case 0x0b: case 0x1b: case 0x2b: case 0x3b:
					return string.Format("{0:x2}        DEC {1}", instr, r16);

				case 0x09: case 0x19: case 0x29: case 0x39:
					return string.Format("{0:x2}        ADD HL, {1}", instr, r16);

				case 0xe8:
					imm0 = br.ReadByte();
					return string.Format("{0:x2} {1:x2}     ADD SP, ${1:x2}", instr, imm0);

				case 0xc6: case 0xd6: case 0xe6: case 0xf6:
				case 0xce: case 0xde: case 0xee: case 0xfe:
					imm0 = br.ReadByte();
					return string.Format("{0:x2} {1:x2}     {2} A, ${1:x2}", instr, imm0, op);

				case 0x20: case 0x30: case 0x28: case 0x38:
					imm0 = br.ReadByte();
					if (comment_rel)
						return string.Format("{0:x2} {1:x2}     JR {2}, {1}   ; ${3:x4}", instr, (sbyte)imm0, cond, pc + 2 + (sbyte)imm0);
					return string.Format("{0:x2} {1:x2}     JR {2}, {1}", instr, (sbyte)imm0, cond);

				case 0x18:
					imm0 = br.ReadByte();
					if (comment_rel)
						return string.Format("{0:x2} {1:x2}     JR {1}   ; ${2:x4}", instr, (sbyte)imm0, pc + 2 + (sbyte)imm0);
					return string.Format("{0:x2} {1:x2}     JR {1}", instr, (sbyte)imm0);

				case 0xc2: case 0xd2: case 0xca: case 0xda:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  JP {3}, ${2:x2}{1:x2}", instr, imm0, imm1, cond);

				case 0xc3:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  JP ${2:x2}{1:x2}", instr, imm0, imm1);

				case 0xc4: case 0xd4: case 0xcc: case 0xdc:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  CALL {3}, ${2:x2}{1:x2}", instr, imm0, imm1, cond);

				case 0xcd:
					imm0 = br.ReadByte(); imm1 = br.ReadByte();
					return string.Format("{0:x2} {1:x2} {2:x2}  CALL ${2:x2}{1:x2}", instr, imm0, imm1);

				case 0xc0: case 0xd0: case 0xc8: case 0xd8:
					return string.Format("{0:x2}        RET {1}", instr, cond);

				case 0xc7: case 0xd7: case 0xe7: case 0xf7:
				case 0xcf: case 0xdf: case 0xef: case 0xff:
					return string.Format("{0:x2}        RST ${1:x2}", instr, instr & 0x38);
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
