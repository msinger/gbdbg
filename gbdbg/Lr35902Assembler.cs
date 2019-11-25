using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace gbdbg
{
	public partial class Lr35902Assembler : Lr35902LexerBase
	{
		private readonly BinaryWriter bw;

		private ushort                      org;
		private IDictionary<string, ushort> lbl;

		public Lr35902Assembler(Stream output, ushort origin, IDictionary<string, ushort> labels)
		{
			bw  = new BinaryWriter(output);
			org = origin;
			lbl = labels;
		}

		public Lr35902Assembler(Stream output, ushort origin)
			: this(output, origin, new Dictionary<string, ushort>())
		{ }

		public Lr35902Assembler(Stream output, IDictionary<string, ushort> labels)
			: this(output, 0, labels)
		{ }

		public Lr35902Assembler(Stream output)
			: this(output, 0)
		{ }

		protected static void ParseEOT(LinkedListNode<LexerToken> n)
		{
			if (n.Value.Type != LexerTokenType.EOT)
				throw new AsmFormatException(n.Value.Pos, "No more input expected by parser.");
		}

		protected static ParserToken ParseDirective(LinkedListNode<LexerToken> n)
		{
			List<LexerToken> d;
			LinkedListNode<LexerToken> nn;

			if (n.Value.Type != LexerTokenType.Dot)
				throw new AsmFormatException(n.Value.Pos, "Not a directive.");

			n = n.Next;
			if (n.Value.Type != LexerTokenType.Name)
				throw new AsmFormatException(n.Previous.Value.Pos, "Invalid directive.");

			switch (n.Value.Name.ToUpper())
			{
			case "ORG":
				n = n.Next;
				if (n.Value.Type != LexerTokenType.Value)
					throw new AsmFormatException(n.Value.Pos, "Address expected.");
				ParseEOT(n.Next);
				return new OriginDirective(n.Previous.Value.Pos, n.Value.Value);
			case "DB":
				d = new List<LexerToken>();
				nn = n.Next;
				while (nn.Value.Type == LexerTokenType.Value)
				{
					d.Add(nn.Value);
					nn = nn.Next;
				}
				ParseEOT(nn);
				return new DataDirective(n.Previous.Value.Pos, d);
			}

			throw new AsmFormatException(n.Previous.Value.Pos, "Unknown directive.");
		}

		protected static Argument ParseArgument(ref LinkedListNode<LexerToken> n)
		{
			LinkedListNode<LexerToken> me = n;
			Argument t;

			switch (n.Value.Type)
			{
			case LexerTokenType.Open:
				n = n.Next;
				t = ParseArgument(ref n);
				if (n.Value.Type != LexerTokenType.Close)
					throw new AsmFormatException(n.Value.Pos, "Closing bracket for indirection expected.");
				n = n.Next;
				t = new Indirection(me.Value.Pos, t);
				break;
			case LexerTokenType.Name:
			case LexerTokenType.Value:
				t = new Terminal(n.Value.Pos, n.Value);
				n = n.Next;
				break;
			case LexerTokenType.Plus:
				if (n.Next.Value.Type != LexerTokenType.Value)
					throw new AsmFormatException(n.Value.Pos, "Unary plus operator requires numerical operand.");
				n = n.Next;
				t = ParseArgument(ref n);
				break;
			case LexerTokenType.Minus:
				if (n.Next.Value.Type != LexerTokenType.Value)
					throw new AsmFormatException(n.Value.Pos, "Unary negation operator requires numerical operand.");
				n = n.Next;
				t = new Terminal(n.Value.Pos, n.Value);
				n = n.Next;
				t = new Negation(me.Value.Pos, t);
				break;
			default:
				throw new AsmFormatException(n.Value.Pos, "Invalid instruction argument.");
			}

			switch (n.Value.Type)
			{
			case LexerTokenType.Plus:
				n = n.Next;
				switch (n.Value.Type)
				{
				case LexerTokenType.Open:
				case LexerTokenType.Name:
				case LexerTokenType.Value:
				case LexerTokenType.Plus:
				case LexerTokenType.Minus:
					t = new Addition(n.Previous.Value.Pos, t, ParseArgument(ref n));
					break;
				default:
					t = new PostIncrement(n.Previous.Value.Pos, t);
					break;
				}
				break;
			case LexerTokenType.Minus:
				n = n.Next;
				switch (n.Value.Type)
				{
				case LexerTokenType.Open:
				case LexerTokenType.Name:
				case LexerTokenType.Value:
				case LexerTokenType.Plus:
				case LexerTokenType.Minus:
					t = new Substraction(n.Previous.Value.Pos, t, ParseArgument(ref n));
					break;
				default:
					t = new PostDecrement(n.Previous.Value.Pos, t);
					break;
				}
				break;
			}

			return t;
		}

		protected static IList<Argument> ParseArgumentList(ref LinkedListNode<LexerToken> n)
		{
			Argument t = ParseArgument(ref n);
			List<Argument> l = new List<Argument>();
			l.Add(t);
			if (n.Value.Type == LexerTokenType.Comma)
			{
				n = n.Next;
				l.AddRange(ParseArgumentList(ref n));
			}
			ParseEOT(n);
			return l;
		}

		protected static Instruction ParseInstruction(LinkedListNode<LexerToken> n)
		{
			if (n.Value.Type != LexerTokenType.Name)
				throw new AsmFormatException(n.Value.Pos, "Not an instruction.");
			if (n.Next.Value.Type != LexerTokenType.EOT)
			{
				n = n.Next;
				return new Instruction(n.Previous.Value.Pos, n.Previous.Value.Name, ParseArgumentList(ref n));
			}
			return new Instruction(n.Value.Pos, n.Value.Name);
		}

		protected static IList<ParserToken> Parse(LinkedListNode<LexerToken> n)
		{
			switch (n.Value.Type)
			{
			case LexerTokenType.EOT:
				ParseEOT(n);
				return new ParserToken[] { };
			case LexerTokenType.Dot:
				return new ParserToken[] { ParseDirective(n) };
			case LexerTokenType.Name:
				if (n.Next.Value.Type == LexerTokenType.Colon)
				{
					Label l = new Label(n.Value.Pos, n.Value.Name);
					n = n.Next.Next;
					switch (n.Value.Type)
					{
					case LexerTokenType.Dot:
						return new ParserToken[] { l, ParseDirective(n) };
					case LexerTokenType.Name:
						return new ParserToken[] { l, ParseInstruction(n) };
					}
					ParseEOT(n);
					return new ParserToken[] { l };
				}
				return new ParserToken[] { ParseInstruction(n) };
			}

			throw new AsmFormatException(n.Value.Pos, "Unexpected token encountered by top level parser.");
		}

		protected static IList<ParserToken> Parse(string line)
		{
			return Parse(Lex(line));
		}

		private static void CheckArgCount(Instruction i, int min, int max)
		{
			IList<Argument> arg = i.Arg;
			if (arg.Count < min)
				throw new AsmFormatException((arg.Count > 0) ? arg[arg.Count - 1].Pos : i.Pos, "Too few arguments.");
			if (arg.Count > max)
				throw new AsmFormatException(arg[max].Pos, "Too many arguments.");
		}

		private static byte AluOp(string op)
		{
			switch (op)
			{
			default:
			case "ADD": return 0x00;
			case "ADC": return 0x08;
			case "SUB": return 0x10;
			case "SBC": return 0x18;
			case "AND": return 0x20;
			case "XOR": return 0x28;
			case "OR":  return 0x30;
			case "CP":  return 0x38;
			}
		}

		private static byte? TryRegArg(Argument arg)
		{
			if (arg is Indirection)
			{
				Indirection ind = (Indirection)arg;
				if (ind.Arg is Terminal)
				{
					Terminal t = (Terminal)ind.Arg;
					if (t.Token.Type == LexerTokenType.Name && t.Token.Name.ToUpper() == "HL")
						return 6;
				}
			}
			else if (arg is Terminal)
			{
				Terminal t = (Terminal)arg;
				if (t.Token.Type == LexerTokenType.Name)
				{
					switch (t.Token.Name.ToUpper())
					{
					case "B": return 0;
					case "C": return 1;
					case "D": return 2;
					case "E": return 3;
					case "H": return 4;
					case "L": return 5;
					case "A": return 7;
					}
				}
			}
			return null;
		}

		private static byte RegArg(Argument arg)
		{
			byte? r = TryRegArg(arg);
			if (!r.HasValue)
				throw new AsmFormatException(arg.Pos, "One of A, B, C, D, E, H, L or (HL) expected.");
			return r.Value;
		}

		private static byte? TryRegArg16(Argument arg)
		{
			if (arg is Terminal)
			{
				Terminal t = (Terminal)arg;
				if (t.Token.Type == LexerTokenType.Name)
				{
					switch (t.Token.Name.ToUpper())
					{
					case "BC": return 0x00;
					case "DE": return 0x10;
					case "HL": return 0x20;
					case "SP": return 0x30;
					}
				}
			}
			return null;
		}

		private static byte RegArg16(Argument arg)
		{
			byte? r = TryRegArg16(arg);
			if (!r.HasValue)
				throw new AsmFormatException(arg.Pos, "One of BC, DE, HL or SP expected.");
			return r.Value;
		}

		private static byte? TryRegArg16PP(Argument arg)
		{
			if (arg is Terminal)
			{
				Terminal t = (Terminal)arg;
				if (t.Token.Type == LexerTokenType.Name)
				{
					switch (t.Token.Name.ToUpper())
					{
					case "BC": return 0x00;
					case "DE": return 0x10;
					case "HL": return 0x20;
					case "AF": return 0x30;
					}
				}
			}
			return null;
		}

		private static byte RegArg16PP(Argument arg)
		{
			byte? r = TryRegArg16PP(arg);
			if (!r.HasValue)
				throw new AsmFormatException(arg.Pos, "One of BC, DE, HL or AF expected.");
			return r.Value;
		}

		private static byte? TryIndRegArg16(Argument arg)
		{
			if (!(arg is Indirection))
				return null;
			byte neg = 0;
			Indirection ind = (Indirection)arg;
			arg = ind.Arg;
			if (arg is Terminal)
			{
				Terminal t = (Terminal)arg;
				if (t.Token.Type == LexerTokenType.Name)
				{
					switch (t.Token.Name.ToUpper())
					{
					case "BC": return 0x00;
					case "DE": return 0x10;
					}
				}
				return null;
			}
			else if (arg is PostIncrement)
			{
				PostIncrement pi = (PostIncrement)arg;
				arg = pi.Arg;
			}
			else if (arg is PostDecrement)
			{
				PostDecrement pd = (PostDecrement)arg;
				arg = pd.Arg;
				neg = 0x10;
			}
			if (arg is Terminal)
			{
				Terminal t = (Terminal)arg;
				if (t.Token.Type == LexerTokenType.Name && t.Token.Name.ToUpper() == "HL")
					return (byte)(0x20 + neg);
			}
			return null;
		}

		private static byte IndRegArg16(Argument arg)
		{
			byte? r = TryIndRegArg16(arg);
			if (!r.HasValue)
				throw new AsmFormatException(arg.Pos, "One of (BC), (DE), (HL+) or (HL-) expected.");
			return r.Value;
		}

		private static int? TryValArg(Argument arg, int min, int max)
		{
			int pos = arg.Pos;
			bool neg = false;
			if (arg is Negation)
			{
				neg = true;
				Negation n = (Negation)arg;
				arg = n.Arg;
			}
			if (arg is Terminal)
			{
				Terminal t = (Terminal)arg;
				if (t.Token.Type == LexerTokenType.Value)
				{
					int v = t.Token.Value;
					if (neg) v *= -1;
					if (v < min || v > max)
						throw new AsmFormatException(pos, "Value out of range " + min.ToString() + "-" + max.ToString() + ".");
					return v;
				}
			}
			return null;
		}

		private static int ValArg(Argument arg, int min, int max)
		{
			int? v = TryValArg(arg, min, max);
			if (!v.HasValue)
				throw new AsmFormatException(arg.Pos, "Numerical value expected.");
			return v.Value;
		}

		private static int? TryIndValArg(Argument arg, bool w16)
		{
			int max = w16 ? ushort.MaxValue : byte.MaxValue;
			if (!(arg is Indirection))
				return null;
			Indirection ind = (Indirection)arg;
			arg = ind.Arg;
			if (arg is Terminal)
			{
				Terminal t = (Terminal)arg;
				if (t.Token.Type == LexerTokenType.Value)
				{
					int v = t.Token.Value;
					if (v < 0 || v > max)
						throw new AsmFormatException(t.Pos, "Value out of range $0-$" + max.ToString("x") + ".");
					return v;
				}
			}
			return null;
		}

		private static byte? TryCondArg(Argument arg)
		{
			if (arg is Terminal)
			{
				Terminal t = (Terminal)arg;
				if (t.Token.Type == LexerTokenType.Name)
				{
					switch (t.Token.Name.ToUpper())
					{
					case "NZ": return 0x00;
					case "Z":  return 0x08;
					case "NC": return 0x10;
					case "C":  return 0x18;
					}
				}
			}
			return null;
		}

		private static byte CondArg(Argument arg)
		{
			byte? r = TryCondArg(arg);
			if (!r.HasValue)
				throw new AsmFormatException(arg.Pos, "One of Z, C, NZ or NC expected.");
			return r.Value;
		}

		// Handles special cases of "LD A, ?" and "LD ?, A"
		private bool HandleLdA(Argument arg, bool load)
		{
			byte load08 = load ? (byte)0x08 : (byte)0;
			byte load10 = load ? (byte)0x10 : (byte)0;
			int? adr = TryIndValArg(arg, true);
			if (adr.HasValue) // (a16)
			{
				bw.Write(new byte[] { (byte)(0xea + load10), (byte)(adr.Value & 0xff), (byte)(adr.Value >> 8) });
				return true;
			}
			if (arg is Indirection) // (?)
			{
				Indirection ind = (Indirection)arg;
				if (ind.Arg is Addition) // (? + ?)
				{
					Addition ad = (Addition)ind.Arg;
					if (ad.LeftOp is Terminal)
					{
						Terminal l = (Terminal)ad.LeftOp;
						if (l.Token.Type == LexerTokenType.Value && l.Token.Value == 0xff00) // ($ff00 + ?)
						{
							int? adr8 = TryValArg(ad.RightOp, 0, 0xff);
							if (adr8.HasValue) // ($ff00 + a8)
							{
								bw.Write(new byte[] { (byte)(0xe0 + load10), (byte)adr8.Value });
								return true;
							}
							if (ad.RightOp is Terminal)
							{
								Terminal r = (Terminal)ad.RightOp;
								if (r.Token.Type == LexerTokenType.Name && r.Token.Name.ToUpper() == "C") // ($ff00 + C)
								{
									bw.Write((byte)(0xe2 + load10));
									return true;
								}
							}
						}
					}
				}
			}
			byte? ireg16 = TryIndRegArg16(arg);
			if (ireg16.HasValue) // (BC), (DE), (HL+) or (HL-)
			{
				bw.Write((byte)(0x02 + load08 + ireg16.Value));
				return true;
			}
			return false;
		}

		protected void WriteInstruction(Instruction t)
		{
			Dictionary<string, byte> op0 = new Dictionary<string, byte>();
			op0.Add("NOP",  0x00);
			op0.Add("STOP", 0x10);
			op0.Add("HALT", 0x76);
			op0.Add("DI",   0xf3);
			op0.Add("EI",   0xfb);
			op0.Add("RLCA", 0x07);
			op0.Add("RRCA", 0x0f);
			op0.Add("RLA",  0x17);
			op0.Add("RRA",  0x1f);
			op0.Add("DAA",  0x27);
			op0.Add("CPL",  0x2f);
			op0.Add("SCF",  0x37);
			op0.Add("CCF",  0x3f);
			op0.Add("RETI", 0xd9);

			Dictionary<string, byte> cb1 = new Dictionary<string, byte>();
			cb1.Add("RLC",  0x00);
			cb1.Add("RRC",  0x08);
			cb1.Add("RL",   0x10);
			cb1.Add("RR",   0x18);
			cb1.Add("SLA",  0x20);
			cb1.Add("SRA",  0x28);
			cb1.Add("SWAP", 0x30);
			cb1.Add("SRL",  0x38);

			Dictionary<string, byte> cb2 = new Dictionary<string, byte>();
			cb2.Add("BIT",  0x40);
			cb2.Add("RES",  0x80);
			cb2.Add("SET",  0xc0);

			string n = t.Name.ToUpper();
			int c = t.Arg.Count;
			byte neg = 0;

			switch (n)
			{
			case "NOP":  case "RETI":
			case "STOP": case "HALT":
			case "DI":   case "EI":
			case "RLCA": case "RRCA":
			case "RLA":  case "RRA":
			case "DAA":  case "CPL":
			case "SCF":  case "CCF":
				CheckArgCount(t, 0, 0);
				bw.Write(op0[n]);
				break;

			case "RLC":  case "RRC":
			case "RL":   case "RR":
			case "SLA":  case "SRA":
			case "SWAP": case "SRL":
				CheckArgCount(t, 1, 1);
				bw.Write(new byte[] { 0xcb, (byte)(cb1[n] | RegArg(t.Arg[0])) });
				break;

			case "BIT":
			case "RES":
			case "SET":
				CheckArgCount(t, 2, 2);
				bw.Write(new byte[] { 0xcb, (byte)(cb2[n] | (ValArg(t.Arg[0], 0, 7) << 3) | RegArg(t.Arg[1])) });
				break;

			case "LD":
				CheckArgCount(t, 2, 2);
				{
					if (t.Arg[0] is Terminal)
					{
						Terminal k = (Terminal)t.Arg[0];
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "A") // A, ?
						{
							if (HandleLdA(t.Arg[1], true))
								break;
						}
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "HL") // HL, ?
						{
							if (t.Arg[1] is Terminal)
							{
								Terminal l = (Terminal)t.Arg[1];
								if (l.Token.Type == LexerTokenType.Name && l.Token.Name.ToUpper() == "SP") // HL, SP
								{
									bw.Write(new byte[] { 0xf8, 0x00 });
									break;
								}
							}
							if (t.Arg[1] is Addition) // HL, ? + ?
							{
								Addition ad = (Addition)t.Arg[1];
								if (ad.LeftOp is Terminal)
								{
									Terminal l = (Terminal)ad.LeftOp;
									if (l.Token.Type == LexerTokenType.Name && l.Token.Name.ToUpper() == "SP") // HL, SP + ?
									{
										int? rel8 = TryValArg(ad.RightOp, sbyte.MinValue, sbyte.MaxValue);
										if (rel8.HasValue) // HL, SP + r8
										{
											bw.Write(new byte[] { 0xf8, unchecked((byte)rel8.Value) });
											break;
										}
									}
								}
							}
							if (t.Arg[1] is Substraction) // HL, ? - ?
							{
								Substraction su = (Substraction)t.Arg[1];
								if (su.LeftOp is Terminal)
								{
									Terminal l = (Terminal)su.LeftOp;
									if (l.Token.Type == LexerTokenType.Name && l.Token.Name.ToUpper() == "SP") // HL, SP - ?
									{
										int? rel8 = TryValArg(su.RightOp, -sbyte.MaxValue, -sbyte.MinValue);
										if (rel8.HasValue) // HL, SP - r8
										{
											bw.Write(new byte[] { 0xf8, unchecked((byte)-rel8.Value) });
											break;
										}
									}
								}
							}
						}
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "SP") // SP, ?
						{
							if (t.Arg[1] is Terminal)
							{
								Terminal l = (Terminal)t.Arg[1];
								if (l.Token.Type == LexerTokenType.Name && l.Token.Name.ToUpper() == "HL") // SP, HL
								{
									bw.Write((byte)0xf9);
									break;
								}
							}
						}
					}
					if (t.Arg[1] is Terminal)
					{
						Terminal k = (Terminal)t.Arg[1];
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "A") // ?, A
						{
							if (HandleLdA(t.Arg[0], false))
								break;
						}
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "SP") // ?, SP
						{
							int? adr = TryIndValArg(t.Arg[0], true);
							if (adr.HasValue) // (a16), SP
							{
								bw.Write(new byte[] { 0x08, (byte)(adr.Value & 0xff), (byte)(adr.Value >> 8) });
								break;
							}
						}
					}
					byte? a0reg = TryRegArg(t.Arg[0]);
					if (a0reg.HasValue) // { A, B, C, D, E, H, L, (HL) }, ?
					{
						byte? a1reg = TryRegArg(t.Arg[1]);
						if (a1reg.HasValue) // { A, B, C, D, E, H, L, (HL) }, { A, B, C, D, E, H, L, (HL) }
						{
							if (a0reg.Value == 6 && a1reg.Value == 6) // (HL), (HL)
								throw new AsmFormatException(t.Pos, "Illegal load indirect to indirect.");
							bw.Write((byte)(0x40 | (a0reg.Value << 3) | a1reg.Value));
							break;
						}
						int? a1val = TryValArg(t.Arg[1], byte.MinValue, byte.MaxValue);
						if (a1val.HasValue) // { A, B, C, D, E, H, L, (HL) }, d8
						{
							bw.Write(new byte[] { (byte)(0x06 | (a0reg.Value << 3)), (byte)a1val.Value });
							break;
						}
					}
					a0reg = TryRegArg16(t.Arg[0]);
					if (a0reg.HasValue) // { BC, DE, HL, SP }, ?
					{
						int? a1val = TryValArg(t.Arg[1], ushort.MinValue, ushort.MaxValue);
						if (a1val.HasValue) // { BC, DE, HL, SP }, d16
						{
							bw.Write(new byte[] { (byte)(0x01 | a0reg.Value), (byte)(a1val.Value & 0xff), (byte)(a1val.Value >> 8) });
							break;
						}
					}
				}
				throw new AsmFormatException(t.Pos, "Illegal load argument combination.");

			case "LDH":
				CheckArgCount(t, 2, 2);
				{
					if (t.Arg[0] is Terminal)
					{
						Terminal k = (Terminal)t.Arg[0];
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "A") // A, ?
						{
							if (t.Arg[1] is Indirection) // A, (?)
							{
								Indirection ind = (Indirection)t.Arg[1];
								int? val = TryValArg(ind.Arg, byte.MinValue, byte.MaxValue);
								if (val.HasValue) // A, (a8)
								{
									bw.Write(new byte[] { 0xf0, (byte)val.Value });
									break;
								}
								if (ind.Arg is Terminal)
								{
									Terminal l = (Terminal)ind.Arg;
									if (l.Token.Type == LexerTokenType.Name && l.Token.Name.ToUpper() == "C") // A, (C)
									{
										bw.Write((byte)0xf2);
										break;
									}
								}
							}
						}
					}
					if (t.Arg[1] is Terminal)
					{
						Terminal k = (Terminal)t.Arg[1];
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "A") // ?, A
						{
							if (t.Arg[0] is Indirection) // (?), A
							{
								Indirection ind = (Indirection)t.Arg[0];
								int? val = TryValArg(ind.Arg, byte.MinValue, byte.MaxValue);
								if (val.HasValue) // (a8), A
								{
									bw.Write(new byte[] { 0xe0, (byte)val.Value });
									break;
								}
								if (ind.Arg is Terminal)
								{
									Terminal l = (Terminal)ind.Arg;
									if (l.Token.Type == LexerTokenType.Name && l.Token.Name.ToUpper() == "C") // (C), A
									{
										bw.Write((byte)0xe2);
										break;
									}
								}
							}
						}
					}
				}
				throw new AsmFormatException(t.Pos, "Illegal load high argument combination.");

			case "LDD":
				neg = 0x10;
				goto case "LDI";
			case "LDI":
				CheckArgCount(t, 2, 2);
				{
					if (t.Arg[0] is Terminal)
					{
						Terminal k = (Terminal)t.Arg[0];
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "A") // A, ?
						{
							if (t.Arg[1] is Indirection) // A, (?)
							{
								Indirection ind = (Indirection)t.Arg[1];
								if (ind.Arg is Terminal)
								{
									Terminal l = (Terminal)ind.Arg;
									if (l.Token.Type == LexerTokenType.Name && l.Token.Name.ToUpper() == "HL") // A, (HL)
									{
										bw.Write((byte)(0x2a | neg));
										break;
									}
								}
							}
						}
					}
					if (t.Arg[1] is Terminal)
					{
						Terminal k = (Terminal)t.Arg[1];
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "A") // ?, A
						{
							if (t.Arg[0] is Indirection) // (?), A
							{
								Indirection ind = (Indirection)t.Arg[0];
								if (ind.Arg is Terminal)
								{
									Terminal l = (Terminal)ind.Arg;
									if (l.Token.Type == LexerTokenType.Name && l.Token.Name.ToUpper() == "HL") // (HL), A
									{
										bw.Write((byte)(0x22 | neg));
										break;
									}
								}
							}
						}
					}
				}
				throw new AsmFormatException(t.Pos, (neg == 0) ? "Illegal load post increment argument combination." :
				                                                 "Illegal load post decrement argument combination.");

			case "LDHL":
				CheckArgCount(t, 2, 2);
				if (t.Arg[0] is Terminal)
				{
					Terminal k = (Terminal)t.Arg[0];
					if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "SP")
					{
						bw.Write(new byte[] { 0xf8, unchecked((byte)ValArg(t.Arg[1], sbyte.MinValue, sbyte.MaxValue)) });
						break;
					}
				}
				throw new AsmFormatException(t.Arg[0].Pos, "SP expected.");

			case "PUSH":
				neg = 0x04;
				goto case "POP";
			case "POP":
				CheckArgCount(t, 1, 1);
				bw.Write((byte)(0xc1 | neg | RegArg16PP(t.Arg[0])));
				break;

			case "ADD":  case "ADC":
			case "SUB":  case "SBC":
			case "AND":  case "XOR":
			case "OR":   case "CP":
				CheckArgCount(t, 1, 2);
				{
					if (n == "ADD" && t.Arg.Count == 2 && t.Arg[0] is Terminal)
					{
						Terminal k = (Terminal)t.Arg[0];
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "SP")
						{
							bw.Write(new byte[] { 0xe8, unchecked((byte)ValArg(t.Arg[1], sbyte.MinValue, sbyte.MaxValue)) });
							break;
						}
						if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "HL")
						{
							bw.Write((byte)(0x09 | RegArg16(t.Arg[1])));
							break;
						}
					}
					Argument a0 = null;
					Argument a1 = t.Arg[0];
					if (t.Arg.Count == 2)
					{
						a0 = t.Arg[0];
						a1 = t.Arg[1];
					}
					if (a0 == null || a0 is Terminal)
					{
						Terminal k = a0 as Terminal;
						if (k == null || k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "A")
						{
							byte? a1reg = TryRegArg(a1);
							if (a1reg.HasValue)
							{
								bw.Write((byte)(0x80 | AluOp(n) | a1reg.Value));
								break;
							}
							int? a1val = TryValArg(a1, byte.MinValue, byte.MaxValue);
							if (a1val.HasValue)
							{
								bw.Write(new byte[] { (byte)(0xc6 | AluOp(n)), (byte)a1val.Value });
								break;
							}
						}
					}
				}
				throw new AsmFormatException(t.Pos, "Illegal ALU operation argument combination.");

			case "DEC":
				neg = 0x01;
				goto case "INC";
			case "INC":
				CheckArgCount(t, 1, 1);
				{
					byte? a0reg = TryRegArg(t.Arg[0]);
					if (a0reg.HasValue)
					{
						bw.Write((byte)(0x04 | neg | (a0reg.Value << 3)));
						break;
					}
					neg <<= 3;
					a0reg = TryRegArg16(t.Arg[0]);
					if (a0reg.HasValue)
					{
						bw.Write((byte)(0x03 | neg | a0reg.Value));
						break;
					}
				}
				throw new AsmFormatException(t.Arg[0].Pos, "One of A, B, C, D, E, H, L, BC, DE, HL, SP or (HL) expected.");

			case "JR":
				CheckArgCount(t, 1, 2);
				{
					Argument a0 = null;
					Argument a1 = t.Arg[0];
					if (t.Arg.Count == 2)
					{
						a0 = t.Arg[0];
						a1 = t.Arg[1];
					}
					byte adr = unchecked((byte)ValArg(a1, sbyte.MinValue, sbyte.MaxValue));
					if (a0 == null)
					{
						bw.Write(new byte[] { 0x18, adr });
						break;
					}
					bw.Write(new byte[] { (byte)(0x20 | CondArg(a0)), adr });
					break;
				}

			case "JP":
				CheckArgCount(t, 1, 2);
				{
					if (t.Arg.Count == 1 && t.Arg[0] is Indirection)
					{
						Indirection ind = (Indirection)t.Arg[0];
						if (ind.Arg is Terminal)
						{
							Terminal k = (Terminal)ind.Arg;
							if (k.Token.Type == LexerTokenType.Name && k.Token.Name.ToUpper() == "HL")
							{
								bw.Write((byte)0xe9);
								break;
							}
						}
					}
					Argument a0 = null;
					Argument a1 = t.Arg[0];
					if (t.Arg.Count == 2)
					{
						a0 = t.Arg[0];
						a1 = t.Arg[1];
					}
					ushort adr = (ushort)ValArg(a1, ushort.MinValue, ushort.MaxValue);
					if (a0 == null)
					{
						bw.Write(new byte[] { 0xc3, (byte)(adr & 0xff), (byte)(adr >> 8) });
						break;
					}
					bw.Write(new byte[] { (byte)(0xc2 | CondArg(a0)), (byte)(adr & 0xff), (byte)(adr >> 8) });
					break;
				}

			case "CALL":
				CheckArgCount(t, 1, 2);
				{
					Argument a0 = null;
					Argument a1 = t.Arg[0];
					if (t.Arg.Count == 2)
					{
						a0 = t.Arg[0];
						a1 = t.Arg[1];
					}
					ushort adr = (ushort)ValArg(a1, ushort.MinValue, ushort.MaxValue);
					if (a0 == null)
					{
						bw.Write(new byte[] { 0xcd, (byte)(adr & 0xff), (byte)(adr >> 8) });
						break;
					}
					bw.Write(new byte[] { (byte)(0xc4 | CondArg(a0)), (byte)(adr & 0xff), (byte)(adr >> 8) });
					break;
				}

			case "RET":
				CheckArgCount(t, 0, 1);
				if (t.Arg.Count == 0)
				{
					bw.Write((byte)0xc9);
					break;
				}
				bw.Write((byte)(0xc0 | CondArg(t.Arg[0])));
				break;

			case "RST":
				CheckArgCount(t, 1, 1);
				if (t.Arg[0] is Terminal)
				{
					Terminal k = (Terminal)t.Arg[0];
					if (k.Token.Type == LexerTokenType.Value)
					{
						int adr = k.Token.Value;
						if ((adr & ~0x38) == 0)
						{
							bw.Write((byte)(0xc7 | adr));
							break;
						}
					}
				}
				throw new AsmFormatException(t.Arg[0].Pos, "One of $00, $08, $10, $18, $20, $28, $30 or $38 expected.");
			}
		}

		public void WriteLine(string line)
		{
			foreach (ParserToken t in Parse(line))
			{
				if (t is OriginDirective)
				{
					OriginDirective od = (OriginDirective)t;
					if (od.Address < ushort.MinValue || od.Address > ushort.MaxValue)
						throw new AsmFormatException(od.Pos, "Address out of range.");
					org = (ushort)od.Address;
				}

				else if (t is DataDirective)
				{
					DataDirective dd = (DataDirective)t;
					foreach (LexerToken lt in dd.Data)
					{
						if (lt.Value < byte.MinValue || lt.Value > byte.MaxValue)
							throw new AsmFormatException(lt.Pos, "Data byte out of range.");
						bw.Write((byte)lt.Value);
						unchecked(org)++;
					}
				}

				else if (t is Label)
				{
					Label l = (Label)t;
					if (lbl.ContainsKey(l.Name))
						throw new AsmFormatException(l.Pos, "Reuse of existing label \"" + l.Name + "\".");
					lbl.Add(l.Name, org);
				}

				else if (t is Instruction)
				{
					Instruction instr = (Instruction)t;
					WriteInstruction(instr);
				}

				else throw new AsmFormatException(t.Pos, "Unknown parser token.");
			}
		}
	}
}
