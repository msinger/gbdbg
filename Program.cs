using System;
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
						Console.WriteLine(debugger.Registers);
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
							if (!int.TryParse(a[1], NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out index) ||
								index < 0 || index >= 8)
							{
								Console.WriteLine("Invalid breakpoint index");
								break;
							}
							if (!int.TryParse(a[2], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out address) ||
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
							if (!int.TryParse(a[2], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out val) ||
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
							if (!int.TryParse(a[1], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out address) ||
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
							Console.WriteLine("Usage: wr <address> <value>");
							break;
						}
						{
							int address, val;
							if (!int.TryParse(a[1], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out address) ||
								address < 0 || address > 0xffff)
							{
								Console.WriteLine("Invalid address");
								break;
							}
							if (!int.TryParse(a[2], NumberStyles.HexNumber, NumberFormatInfo.InvariantInfo, out val) ||
								val < 0 || val > 0xff)
							{
								Console.WriteLine("Invalid value");
								break;
							}
							debugger.WriteMem((ushort)address, (byte)val);
						}
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
			}
		}
	}
}
