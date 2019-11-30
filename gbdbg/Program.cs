using System;
using System.IO;
using System.Collections.Generic;
using System.Globalization;

namespace gbdbg
{
	class MainClass
	{
		private static Lr35902Debugger debugger;

		private static bool ValidAdrRange(Range range)
		{
			return range != null &&
			       range.Start >= 0 && range.Start <= 0xffff &&
			       range.Length >= 0 && range.Length <= (0x10000 - range.Start);
		}

		private static bool ValidBufRange(Stream buf, Range range)
		{
			return range != null &&
			       range.Start >= 0 && range.Start <= buf.Length - 1 &&
			       range.Length >= 0 && range.Length <= (buf.Length - range.Start);
		}

		private static void Dump(Stream mem, int len, TextWriter sout)
		{
			int[] b = new int[16];
			while (len >= 0)
			{
				sout.Write(mem.Position.ToString("x4") + ": ");
				for (int i = 0; i < 16; i++)
				{
					if (i == 8)
						sout.Write(" ");
					b[i] = (i < len) ? mem.ReadByte() : -1;
					if (b[i] >= 0)
						sout.Write(" " + b[i].ToString("x2"));
					else
						sout.Write("   ");
				}
				sout.Write("  |");
				for (int i = 0; i < 16; i++)
				{
					if (b[i] < 0)
					{
						len = 0;
						break;
					}
					if (b[i] >= 32 && b[i] < 127)
						sout.Write((char)b[i]);
					else
						sout.Write(".");
				}
				sout.WriteLine("|");
				len -= 16;
			}
		}

		private static int Shell(TextReader cmdin, TextReader sin, TextWriter sout, TextWriter eout, bool interactive)
		{
			IDictionary<string, Stream> buffers = new Dictionary<string, Stream>();
			int last_error = 0;

			while (true)
			{
				if (interactive)
					sout.Write("gbdbg# ");
				string cmd = cmdin.ReadLine();

				if (cmd == null)
				{
					if (interactive)
						sout.WriteLine();
					return last_error;
				}

				string[] a = cmd.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (a.Length == 0) continue;

				try
				{
					switch (a[0])
					{
					case "exit":
						if (a.Length >= 2)
						{
							if (!NumberParser.TryParse(a[1], out last_error))
							{
								eout.WriteLine("Exit code not a number");
								last_error = 1;
							}
						}
						return last_error;
					case "run":
						if (a.Length < 2)
						{
							eout.WriteLine("Run debugger script in sub shell");
							eout.WriteLine("Usage: run <path>");
							last_error = 2;
							break;
						}
						{
							string path = cmd.TrimStart().Substring(4);
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
								eout.WriteLine("Failed to read file \"" + path + "\"");
								last_error = 4;
								break;
							}
							m.Seek(0, SeekOrigin.Begin);
							last_error = Shell(new StreamReader(m), sin, sout, eout, false);
						}
						break;
					case "h":
						debugger.Halt();
						last_error = 0;
						break;
					case "c":
						debugger.Continue();
						last_error = 0;
						break;
					case "s":
						debugger.Step();
						last_error = 0;
						break;
					case "r":
						{
							Lr35902Registers regs = debugger.Registers;
							sout.WriteLine(regs);
							System.IO.Stream mem = debugger.OpenMemory(regs.PC);
							Lr35902Disassembler dis = new Lr35902Disassembler(mem);
							sout.WriteLine("->" + mem.Position.ToString("x4") + ": " + dis.ReadLine());
							sout.WriteLine("  " + mem.Position.ToString("x4") + ": " + dis.ReadLine());
							sout.WriteLine("  " + mem.Position.ToString("x4") + ": " + dis.ReadLine());
						}
						last_error = 0;
						break;
					case "b":
						if (a.Length != 3)
						{
							eout.WriteLine("Set breakpoint");
							eout.WriteLine("Usage: b <index> <address>");
							last_error = 2;
							break;
						}
						{
							int index, address;
							if (!NumberParser.TryParse(a[1], out index) ||
								index < 0 || index >= 8)
							{
								eout.WriteLine("Invalid breakpoint index");
								last_error = 1;
								break;
							}
							if (!NumberParser.TryParse(a[2], out address) ||
								address < 0 || address > 0xffff)
							{
								eout.WriteLine("Invalid breakpoint address");
								last_error = 1;
								break;
							}
							debugger.SetBreakpoint((byte)index, (ushort)address);
						}
						last_error = 0;
						break;
					case "set":
						if (a.Length != 3)
						{
							eout.WriteLine("Set register");
							eout.WriteLine("Usage: set <reg> <val>");
							last_error = 2;
							break;
						}
						{
							int val;
							if (!NumberParser.TryParse(a[2], out val) ||
								val < 0 || val > 0xffff || (a[1].Length == 1 && val > 0xff))
							{
								eout.WriteLine("Invalid value");
								last_error = 1;
								break;
							}
							try { debugger.SetRegister(a[1], (ushort)val); }
							catch(ArgumentException)
							{
								eout.WriteLine("Invalid register name!");
								last_error = 1;
							}
						}
						last_error = 0;
						break;
					case "rd":
						if (a.Length != 2)
						{
							eout.WriteLine("Read byte");
							eout.WriteLine("Usage: rd <address>");
							last_error = 2;
							break;
						}
						{
							int address;
							if (!NumberParser.TryParse(a[1], out address) ||
								address < 0 || address > 0xffff)
							{
								eout.WriteLine("Invalid address");
								last_error = 1;
								break;
							}
							byte b = debugger.ReadMem((ushort)address);
							sout.WriteLine("0x" + b.ToString("x2"));
						}
						last_error = 0;
						break;
					case "wr":
						if (a.Length != 3)
						{
							eout.WriteLine("Write byte");
							eout.WriteLine("Usage: wr <address>[+<length>] <value>");
							eout.WriteLine("   or: wr <first>[-<last>] <value>");
							last_error = 2;
							break;
						}
						{
							Range range;
							if (!Range.TryParse(a[1], out range) || !ValidAdrRange(range))
							{
								eout.WriteLine("Invalid address range");
								last_error = 1;
								break;
							}
							int val;
							if (!NumberParser.TryParse(a[2], out val) ||
								val < 0 || val > 0xff)
							{
								eout.WriteLine("Invalid value");
								last_error = 1;
								break;
							}
							debugger.WriteMemRange((ushort)range.Start, range.Length, (byte)val);
						}
						last_error = 0;
						break;
					case "xx":
						if (a.Length > 4 || a.Length < 2)
						{
							eout.WriteLine("Execute instruction");
							eout.WriteLine("Usage: xx <hex0> [<hex1> [<hex2>]]");
							last_error = 2;
							break;
						}
						{
							byte[] op;
							int hex0, hex1, hex2;
							if (!int.TryParse(a[1], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex0) ||
								hex0 < 0 || hex0 > 0xff)
							{
								eout.WriteLine("Invalid instruction");
								last_error = 1;
								break;
							}
							op = new byte[] { (byte)hex0 };
							if (a.Length > 2)
							{
								if (!int.TryParse(a[2], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex1) ||
									hex1 < 0 || hex1 > 0xff)
								{
									eout.WriteLine("Invalid instruction");
									last_error = 1;
									break;
								}
								op = new byte[] { (byte)hex0, (byte)hex1 };
								if (a.Length > 3)
								{
									if (!int.TryParse(a[3], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out hex2) ||
										hex2 < 0 || hex2 > 0xff)
									{
										eout.WriteLine("Invalid instruction");
										last_error = 1;
										break;
									}
									op = new byte[] { (byte)hex0, (byte)hex1, (byte)hex2 };
								}
							}
							debugger.Execute(op);
						}
						last_error = 0;
						break;
					case "x":
						if (a.Length < 2)
						{
							eout.WriteLine("Assemble and execute instruction");
							eout.WriteLine("Usage: x <asm>");
							last_error = 2;
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
								eout.WriteLine(e.Message);
								eout.WriteLine("> " + asmline);
								for (int i = 0; i < e.Column; i++)
									eout.Write(" ");
								eout.WriteLine(" ^");
								last_error = 3;
								break;
							}
							catch
							{
								eout.WriteLine("Failed to assemble line");
								last_error = 3;
								break;
							}
							m.Position = 0;
							if (interactive)
							{
								while (true)
								{
									int b = m.ReadByte();
									if (b == -1)
										break;
									eout.Write(b.ToString("X2") + " ");
								}
								eout.WriteLine();
							}
							debugger.Execute(m.ToArray());
						}
						last_error = 0;
						break;
					case "loadrom":
						if (a.Length < 2)
						{
							eout.WriteLine("Load file into ROM while device is under reset");
							eout.WriteLine("Usage: loadrom <path>");
							last_error = 2;
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
								eout.WriteLine("Failed to read file \"" + path + "\"");
								last_error = 4;
								break;
							}
							eout.WriteLine("Sending file \"" + path + "\"...");
							m.Seek(0, SeekOrigin.Begin);
							debugger.RawSend(m);
							debugger.Unlock();
						}
						last_error = 0;
						break;
					case "dump":
						if (a.Length != 2)
						{
							eout.WriteLine("Dump memory");
							eout.WriteLine("Usage: dump <address>[+<length>]");
							eout.WriteLine("   or: dump <first>[-<last>]");
							last_error = 2;
							break;
						}
						{
							Range range;
							if (!Range.TryParse(a[1], out range, 256) || !ValidAdrRange(range))
							{
								eout.WriteLine("Invalid address range");
								last_error = 1;
								break;
							}
							Dump(debugger.OpenMemory((ushort)range.Start), range.Length, sout);
						}
						last_error = 0;
						break;
					case "dis":
						if (a.Length != 2)
						{
							eout.WriteLine("Disassemble memory");
							eout.WriteLine("Usage: dis <address>[+<length>]");
							eout.WriteLine("   or: dis <first>[-<last>]");
							last_error = 2;
							break;
						}
						{
							Range range;
							if (!Range.TryParse(a[1], out range, 16) || !ValidAdrRange(range))
							{
								eout.WriteLine("Invalid address range");
								last_error = 1;
								break;
							}
							System.IO.Stream mem = debugger.OpenMemory((ushort)range.Start);
							Lr35902Disassembler dis = new Lr35902Disassembler(mem);
							long start = mem.Position;
							long len = range.Length;
							while (mem.Position <= 0xffff && mem.Position - start < len)
							{
								sout.WriteLine("  " + mem.Position.ToString("x4") + ": " + dis.ReadLine());
							}
						}
						last_error = 0;
						break;
					case "reset":
						if (a.Length >= 2 && a[1] == "hold")
						{
							debugger.ResetTarget(true);
						}
						else
						{
							debugger.ResetTarget();
							debugger.Unlock();
						}
						last_error = 0;
						break;
					case "unlock":
						debugger.Unlock();
						last_error = 0;
						break;
					case "buf":
						if (a.Length < 3)
						{
							eout.WriteLine("Performs actions on memory buffers");
							eout.WriteLine("Usage: buf <buffer> <action> [<args>...]");
							eout.WriteLine("Actions:");
							eout.WriteLine("  asm");
							eout.WriteLine("  dis [<range>]");
							eout.WriteLine("  drop");
							eout.WriteLine("  dump [<range>]");
							last_error = 2;
							break;
						}
						{
							string buf = NameParser.TryParse(a[1]);
							if (buf == null)
							{
								eout.WriteLine("Invalid buffer name!");
								last_error = 1;
								break;
							}
							switch (a[2])
							{
							case "asm":
								MemoryStream m = new MemoryStream();
								Lr35902Assembler asm = new Lr35902Assembler(m);
								last_error = 0;
								while(true)
								{
									if (interactive)
										sout.Write("> ");
									string asmline = cmdin.ReadLine();

									if (asmline == null || asmline == "end")
									{
										if (interactive)
											sout.WriteLine();
										break;
									}

									try
									{
										asm.WriteLine(asmline);
									}
									catch(AsmFormatException e)
									{
										eout.WriteLine(e.Message);
										eout.WriteLine("> " + asmline);
										for (int i = 0; i < e.Column; i++)
											eout.Write(" ");
										eout.WriteLine(" ^");
										last_error = 3;
										break;
									}
									catch
									{
										eout.WriteLine("Failed to assemble line");
										last_error = 3;
										break;
									}
								}
								if (last_error == 0)
									buffers[buf] = m;
								break;
							case "dis":
								if (a.Length != 3 && a.Length != 4)
								{
									eout.WriteLine("Disassemble buffer");
									eout.WriteLine("Usage: buf <name> dis");
									eout.WriteLine("   or: buf <name> dis <address>[+<length>]");
									eout.WriteLine("   or: buf <name> dis <first>[-<last>]");
									last_error = 2;
									break;
								}
								{
									Stream mem;
									if (!buffers.TryGetValue(buf, out mem))
									{
										eout.WriteLine("Buffer does not exist: " + buf);
										last_error = 4;
										break;
									}
									Range range = new Range(0, (int)mem.Length);
									if (a.Length > 3 && (!Range.TryParse(a[3], out range, 16) || !ValidBufRange(mem, range)))
									{
										eout.WriteLine("Invalid range within buffer");
										last_error = 1;
										break;
									}
									mem.Position = range.Start;
									Lr35902Disassembler dis = new Lr35902Disassembler(mem);
									long start = mem.Position;
									long len = range.Length;
									while (mem.Position - start < len)
									{
										sout.WriteLine("  " + mem.Position.ToString("x4") + ": " + dis.ReadLine());
									}
								}
								break;
							case "drop":
								last_error = buffers.Remove(buf) ? 0 : 1;
								break;
							case "dump":
								if (a.Length != 3 && a.Length != 4)
								{
									eout.WriteLine("Dump buffer");
									eout.WriteLine("Usage: buf <name> dump");
									eout.WriteLine("   or: buf <name> dump <address>[+<length>]");
									eout.WriteLine("   or: buf <name> dump <first>[-<last>]");
									last_error = 2;
									break;
								}
								{
									Stream mem;
									if (!buffers.TryGetValue(buf, out mem))
									{
										eout.WriteLine("Buffer does not exist: " + buf);
										last_error = 4;
										break;
									}
									Range range = new Range(0, (int)mem.Length);
									if (a.Length > 3 && (!Range.TryParse(a[3], out range, 256) || !ValidBufRange(mem, range)))
									{
										eout.WriteLine("Invalid range within buffer");
										last_error = 1;
										break;
									}
									mem.Position = range.Start;
									Dump(mem, range.Length, sout);
								}
								break;
							default:
								eout.WriteLine("Invalid buffer action!");
								last_error = 5;
								break;
							}
						}
						break;
					default:
						eout.WriteLine("Invalid command!");
						last_error = 5;
						break;
					}
				}
				catch (NotHaltedException)
				{
					eout.WriteLine("Target not halted!");
					last_error = 6;
				}
				catch (InvalidResponseException)
				{
					eout.WriteLine("Invalid response from target!");
					last_error = 7;
				}
				catch (EndOfStreamException)
				{
					eout.WriteLine("End of stream!");
					last_error = 8;
				}
			}
		}

		private static void Usage(TextWriter o)
		{
			o.WriteLine("Iceboy Game Boy Debugger");
			o.WriteLine("USAGE: gbdbg [OPTIONS] [--] DEVICE");
			o.WriteLine("OPTIONS:");
			o.WriteLine("  --help        Prints this text.");
			o.WriteLine("  --run FILE    Runs debugger script FILE, then exits.");
		}

		public static int Main(string[] args)
		{
			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => e.Cancel = true;
			Console.TreatControlCAsInput = false;

			bool isTTY = !Console.IsInputRedirected && !Console.IsOutputRedirected;

			bool parseOpts = true;
			string dev = null;
			string run = null;

			for (int i = 0; i < args.Length; i++)
			{
				if (args[i][0] == '-' && parseOpts)
				{
					switch (args[i])
					{
					case "--":
						parseOpts = false;
						break;
					case "--help":
						Usage(Console.Out);
						return 0;
					case "--run":
						if (i + 1 >= args.Length)
						{
							Usage(Console.Error);
							return 2;
						}
						run = args[++i];
						break;
					default:
						Console.Error.WriteLine("Invalid option: " + args[i]);
						return 2;
					}
					continue;
				}

				if (dev == null)
				{
					dev = args[i];
					continue;
				}

				Console.Error.WriteLine("Too many arguments: " + args[i]);
				return 2;
			}

			if (dev == null)
			{
				Usage(Console.Error);
				return 2;
			}

			try
			{
				debugger = new Lr35902Debugger(args[0]);
				debugger.Unlock();
			}
			catch (InvalidResponseException)
			{
				Console.Error.WriteLine("Invalid response from target.");
				return 7;
			}
			catch (EndOfStreamException)
			{
				Console.Error.WriteLine("End of stream.");
				return 8;
			}
			catch (IOException)
			{
				Console.Error.WriteLine("Failed to open port.");
				return 8;
			}

			TextReader cmdin = Console.In;

			if (run != null)
			{
				MemoryStream m = null;
				try
				{
					using (FileStream f = new FileStream(run, FileMode.Open, FileAccess.Read))
					{
						m = new MemoryStream();
						f.CopyTo(m);
					}
				}
				catch
				{
					Console.Error.WriteLine("Failed to read file \"" + run + "\"");
					return 4;
				}
				m.Seek(0, SeekOrigin.Begin);
				cmdin = new StreamReader(m);
			}

			return Shell(cmdin, Console.In, Console.Out, Console.Error, isTTY && cmdin == Console.In);
		}
	}
}
