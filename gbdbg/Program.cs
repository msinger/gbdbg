using System;
using System.IO;
using System.Globalization;

namespace gbdbg
{
	class MainClass
	{
		public static int Main(string[] args)
		{
			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => e.Cancel = true;
			Console.TreatControlCAsInput = false;

			Lr35902Debugger debugger = new Lr35902Debugger(args[0]);

			debugger.Unlock();

			while (true)
			{
				Console.Write("gbdbg# ");
				string cmd = Console.ReadLine();

				string[] a = cmd.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (a.Length == 0) continue;

				try
				{
					switch (a[0])
					{
					case "quit":
						return 0;
					case "h":
						debugger.Halt();
						break;
					case "c":
						debugger.Continue();
						break;
					case "s":
						debugger.Step();
						break;
					case "r":
						{
							Lr35902Registers regs = debugger.Registers;
							Console.WriteLine(regs);
							System.IO.Stream mem = debugger.OpenMemory(regs.PC);
							Lr35902Disassembler dis = new Lr35902Disassembler(mem);
							Console.WriteLine("->" + mem.Position.ToString("x4") + ": " + dis.ReadLine());
							Console.WriteLine("  " + mem.Position.ToString("x4") + ": " + dis.ReadLine());
							Console.WriteLine("  " + mem.Position.ToString("x4") + ": " + dis.ReadLine());
						}
						break;
					case "b":
						if (a.Length != 3)
						{
							Console.WriteLine("Set breakpoint");
							Console.WriteLine("Usage: b <index> <address>");
							break;
						}
						{
							int index, address;
							if (!NumberParser.TryParse(a[1], out index) ||
								index < 0 || index >= 8)
							{
								Console.WriteLine("Invalid breakpoint index");
								break;
							}
							if (!NumberParser.TryParse(a[2], out address) ||
								address < 0 || address > 0xffff)
							{
								Console.WriteLine("Invalid breakpoint address");
								break;
							}
							debugger.SetBreakpoint((byte)index, (ushort)address);
						}
						break;
					case "set":
						if (a.Length != 3)
						{
							Console.WriteLine("Set register");
							Console.WriteLine("Usage: set <reg> <val>");
							break;
						}
						{
							int val;
							if (!NumberParser.TryParse(a[2], out val) ||
								val < 0 || val > 0xffff || (a[1].Length == 1 && val > 0xff))
							{
								Console.WriteLine("Invalid value");
								break;
							}
							try { debugger.SetRegister(a[1], (ushort)val); }
							catch(ArgumentException)
							{
								Console.WriteLine("Invalid register name!");
							}
						}
						break;
					case "rd":
						if (a.Length != 2)
						{
							Console.WriteLine("Read byte");
							Console.WriteLine("Usage: rd <address>");
							break;
						}
						{
							int address;
							if (!NumberParser.TryParse(a[1], out address) ||
								address < 0 || address > 0xffff)
							{
								Console.WriteLine("Invalid address");
								break;
							}
							byte b = debugger.ReadMem((ushort)address);
							Console.WriteLine("0x" + b.ToString("x2"));
						}
						break;
					case "wr":
						if (a.Length != 3)
						{
							Console.WriteLine("Write byte");
							Console.WriteLine("Usage: wr <address>[+<length>] <value>");
							Console.WriteLine("   or: wr <first>[-<last>] <value>");
							break;
						}
						{
							Range range;
							if (!Range.TryParse(a[1], out range) || !ValidAdrRange(range))
							{
								Console.WriteLine("Invalid address range");
								break;
							}
							int val;
							if (!NumberParser.TryParse(a[2], out val) ||
								val < 0 || val > 0xff)
							{
								Console.WriteLine("Invalid value");
								break;
							}
							debugger.WriteMemRange((ushort)range.Start, range.Length, (byte)val);
						}
						break;
					case "xx":
						if (a.Length > 4 || a.Length < 2)
						{
							Console.WriteLine("Execute instruction");
							Console.WriteLine("Usage: xx <hex0> [<hex1> [<hex2>]]");
							break;
						}
						{
							byte[] op;
							int hex0, hex1, hex2;
							if (!int.TryParse(a[1], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex0) ||
								hex0 < 0 || hex0 > 0xff)
							{
								Console.WriteLine("Invalid instruction");
								break;
							}
							op = new byte[] { (byte)hex0 };
							if (a.Length > 2)
							{
								if (!int.TryParse(a[2], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex1) ||
									hex1 < 0 || hex1 > 0xff)
								{
									Console.WriteLine("Invalid instruction");
									break;
								}
								op = new byte[] { (byte)hex0, (byte)hex1 };
								if (a.Length > 3)
								{
									if (!int.TryParse(a[3], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex2) ||
										hex2 < 0 || hex2 > 0xff)
									{
										Console.WriteLine("Invalid instruction");
										break;
									}
									op = new byte[] { (byte)hex0, (byte)hex1, (byte)hex2 };
								}
							}
							debugger.Execute(op);
						}
						break;
					case "x":
						if (a.Length < 2)
						{
							Console.WriteLine("Assemble and execute instruction");
							Console.WriteLine("Usage: x <asm>");
							break;
						}
						{
							string asmline = cmd.TrimStart().Substring(2);
							MemoryStream m = new MemoryStream();
							Lr35902Assembler asm = new Lr35902Assembler(m);
							try
							{
								asm.WriteLine(asmline);
							}
							catch(AsmFormatException e)
							{
								Console.WriteLine(e.Message);
								Console.WriteLine("> " + asmline);
								for (int i = 0; i < e.Column; i++)
									Console.Write(" ");
								Console.WriteLine(" ^");
								break;
							}
							catch
							{
								Console.WriteLine("Failed to assemble line");
								break;
							}
							m.Position = 0;
							while (true)
							{
								int b = m.ReadByte();
								if (b == -1)
									break;
								Console.Write(b.ToString("X2") + " ");
							}
							Console.WriteLine();
							debugger.Execute(m.ToArray());
						}
						break;
					case "loadrom":
						if (a.Length < 2)
						{
							Console.WriteLine("Load file into ROM while device is under reset");
							Console.WriteLine("Usage: loadrom <path>");
							break;
						}
						{
							string path = cmd.TrimStart().Substring(8);
							MemoryStream m = null;
							try
							{
								using (FileStream f = new FileStream(path, FileMode.Open, FileAccess.Read))
								{
									m = new MemoryStream();
									f.CopyTo(m);
								}
							}
							catch
							{
								Console.WriteLine("Failed to read file \"" + path + "\"");
								break;
							}
							Console.WriteLine("Sending file \"" + path + "\"...");
							m.Seek(0, SeekOrigin.Begin);
							debugger.RawSend(m);
						}
						break;
					case "dump":
						if (a.Length != 2)
						{
							Console.WriteLine("Dump memory");
							Console.WriteLine("Usage: dump <address>[+<length>]");
							Console.WriteLine("   or: dump <first>[-<last>]");
							break;
						}
						{
							Range range;
							if (!Range.TryParse(a[1], out range, 256) || !ValidAdrRange(range))
							{
								Console.WriteLine("Invalid address range");
								break;
							}
							int[] b = new int[16];
							System.IO.Stream mem = debugger.OpenMemory((ushort)range.Start);
							int len = range.Length;
							while (len >= 0)
							{
								Console.Write(mem.Position.ToString("x4") + ": ");
								for (int i = 0; i < 16; i++)
								{
									if (i == 8)
										Console.Write(" ");
									b[i] = (i < len) ? mem.ReadByte() : -1;
									if (b[i] >= 0)
										Console.Write(" " + b[i].ToString("x2"));
									else
										Console.Write("   ");
								}
								Console.Write("  |");
								for (int i = 0; i < 16; i++)
								{
									if (b[i] < 0)
									{
										len = 0;
										break;
									}
									if (b[i] >= 32 && b[i] < 127)
										Console.Write((char)b[i]);
									else
										Console.Write(".");
								}
								Console.WriteLine("|");
								len -= 16;
							}
						}
						break;
					case "dis":
						if (a.Length != 2)
						{
							Console.WriteLine("Disassemble memory");
							Console.WriteLine("Usage: dis <address>[+<length>]");
							Console.WriteLine("   or: dis <first>[-<last>]");
							break;
						}
						{
							Range range;
							if (!Range.TryParse(a[1], out range, 16) || !ValidAdrRange(range))
							{
								Console.WriteLine("Invalid address range");
								break;
							}
							System.IO.Stream mem = debugger.OpenMemory((ushort)range.Start);
							Lr35902Disassembler dis = new Lr35902Disassembler(mem);
							long start = mem.Position;
							long len = range.Length;
							while (mem.Position < 0xffff && mem.Position - start < len)
							{
								Console.WriteLine("  " + mem.Position.ToString("x4") + ": " + dis.ReadLine());
							}
						}
						break;
					case "reset":
						debugger.ResetTarget(a.Length >= 2 && a[1] == "hold");
						break;
					default:
						Console.WriteLine("Invalid command!");
						break;
					}
				}
				catch (NotHaltedException)
				{
					Console.WriteLine("Target not halted!");
				}
				catch (InvalidResponseException)
				{
					Console.WriteLine("Invalid response from target!");
				}
				catch (EndOfStreamException)
				{
					Console.WriteLine("End of stream!");
				}
			}
		}

		private static bool ValidAdrRange(Range range)
		{
			return range != null &&
			       range.Start >= 0 && range.Start <= 0xffff &&
			       range.Length >= 0 && range.Length <= (0x10000 - range.Start);
		}
	}
}
