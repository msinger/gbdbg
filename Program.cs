using System;
using System.IO.Ports;
using System.Threading;
using System.Globalization;

namespace gbdbg
{
	class MainClass
	{
		[Flags]
		public enum F
		{
			Z = 0x80,
			N = 0x40,
			H = 0x20,
			C = 0x10,
		}

		public class DbgState
		{
			public F f;
			public bool no_inc, halt;
			public byte arg;
			public ushort pc, sp;
		}

		private static readonly byte[] ret = new byte[16];

		private static SerialPort p;

		public static void ClearRet()
		{
			for (int i = 0; i < 16; i++)
				ret[i] = 0;
		}

		public static void Send(byte[] buf)
		{
			byte[] rbuf = new byte[1];
		repeat:
			for (int i = 0; i < buf.Length; i++)
			{
				p.Write(buf, i, 1);
				try { if (p.Read(rbuf, 0, 1) != 1) goto repeat; }
				catch (TimeoutException) { goto repeat; }
				int k = rbuf[0] >> 4;
				ret[k] = (byte)(0x80 | (rbuf[0] & 0xf));
			}
		}

		public static bool HasRet(ushort mask)
		{
			for (int i = 0; i < 16; i++)
				if ((mask & (1 << i)) != 0)
					if ((ret[i] & 0x80) == 0)
						return false;
			return true;
		}

		public static byte[] Receive(ushort mask)
		{
			while (!HasRet(mask))
				Send(new byte[] { 0x01 });
			byte[] buf = new byte[8];
			for (int i = 0, j = 0; i < 8; i++, j += 2)
				buf[i] = (byte)((ret[j] & 0xf) | ((ret[j + 1] & 0xf) << 4));
			return buf;
		}

		public static void Halt()
		{
			Send(new byte[] { 0x00 });
		}

		public static void Continue()
		{
			Send(new byte[] { 0x03 });
		}

		private static void StepInternal()
		{
			Send(new byte[] { 0x02 });
		}

		public static void Step()
		{
			DbgState s = Read(0x0001);
			if (s == null)
			{
				Console.WriteLine("Single step failed!");
				return;
			}
			if (!s.halt)
			{
				Console.WriteLine("Single step failed: Not halted!");
				return;
			}
			StepInternal();
		}

		public static DbgState Read(ushort mask)
		{
			ClearRet();
			byte[] buf = Receive(mask);
			DbgState s = new DbgState();
			s.no_inc = (buf[0] & 2) != 0;
			s.halt   = (buf[0] & 1) != 0;
			s.f      = (F)(buf[0] & 0xf0);
			s.arg    = buf[1];
			s.pc     = (ushort)(((int)buf[3] << 8) | buf[2]);
			s.sp     = (ushort)(((int)buf[5] << 8) | buf[4]);
			return s;
		}

		public static void SetDbgState(bool no_inc)
		{
			Send(new byte[] { (byte)(no_inc ? 0x22 : 0x20) });
		}

		public struct DrvData
		{
			public bool Drive;
			public byte Data;
			public static readonly DrvData[] Default = new DrvData[24];
		}

		public static void SetDrvData(DrvData[] data, int offset)
		{
			int prevd = -1;
			for (int i = 0; i < data.Length; i++)
			{
				if (data[i].Drive)
				{
					if (prevd != data[i].Data)
					{
						prevd = data[i].Data;
						Send(new byte[] { (byte)(0x10 | (prevd & 0xf)), (byte)(0x10 | (prevd >> 4)) });
					}
				}
				Send(new byte[] { (byte)((data[i].Drive ? 0x60 : 0x40) | (i + offset)) });
			}
		}

		private static void ExecuteInternal(byte[] op)
		{
			int i, j;
			DrvData[] d = new DrvData[24];
			for (i = 0, j = 2; i < op.Length; i++, j += 4)
			{
				d[j].Drive = true;
				d[j + 1].Drive = true;
				d[j].Data = op[i];
				d[j + 1].Data = op[i];
			}
			SetDrvData(d, 0);
			StepInternal();
		}

		public static void Execute(byte[] op)
		{
			DbgState s = Read(0x0001);
			if (s == null)
			{
				Console.WriteLine("Execute failed!");
				return;
			}
			if (!s.halt)
			{
				Console.WriteLine("Execute failed: Not halted!");
				return;
			}
			SetDbgState(true);
			ExecuteInternal(op);
			SetDbgState(false);
			SetDrvData(DrvData.Default, 0);
		}

		public class RegSet
		{
			public ushort pc;
			public ushort sp;
			public ushort bc;
			public ushort de;
			public ushort hl;
			public ushort af;
			public byte b;
			public byte c;
			public byte d;
			public byte e;
			public byte h;
			public byte l;
			public byte a;
			public F f;
		}

		public static RegSet Regs()
		{
			DbgState s = Read(0x0fff);
			if (s == null) return null;

			if (!s.halt) return null;

			SetDbgState(true);

			ExecuteInternal(new byte[] { 0x40 }); // LD B, B
			DbgState B = Read(0x000c);
			if (B == null) return null;

			ExecuteInternal(new byte[] { 0x49 }); // LD C, C
			DbgState C = Read(0x000c);
			if (C == null) return null;

			ExecuteInternal(new byte[] { 0x52 }); // LD D, D
			DbgState D = Read(0x000c);
			if (D == null) return null;

			ExecuteInternal(new byte[] { 0x5b }); // LD E, E
			DbgState E = Read(0x000c);
			if (E == null) return null;

			ExecuteInternal(new byte[] { 0x64 }); // LD H, H
			DbgState H = Read(0x000c);
			if (H == null) return null;

			ExecuteInternal(new byte[] { 0x6d }); // LD L, L
			DbgState L = Read(0x000c);
			if (L == null) return null;

			ExecuteInternal(new byte[] { 0x7f }); // LD A, A
			DbgState A = Read(0x000c);
			if (A == null) return null;

			SetDbgState(false);
			SetDrvData(DrvData.Default, 0);

			RegSet r = new RegSet();
			r.pc = s.pc;
			r.sp = s.sp;
			r.bc = (ushort)(((int)B.arg << 8) | C.arg);
			r.de = (ushort)(((int)D.arg << 8) | E.arg);
			r.hl = (ushort)(((int)H.arg << 8) | L.arg);
			r.af = (ushort)(((int)A.arg << 8) | (int)s.f);
			r.b = B.arg;
			r.c = C.arg;
			r.d = D.arg;
			r.e = E.arg;
			r.h = H.arg;
			r.l = L.arg;
			r.a = A.arg;
			r.f = s.f;

			return r;
		}

		public static void ShowRegs()
		{
			DbgState s = Read(0x0001);
			if (s == null)
			{
				Console.WriteLine("Show regs failed!");
				return;
			}
			if (!s.halt)
			{
				Console.WriteLine("Show regs failed: Not halted!");
				return;
			}

			RegSet r = Regs();
			if (r == null) return;

			string fZ = ((r.f & F.Z) != 0) ? "Z" : "-";
			string fN = ((r.f & F.N) != 0) ? "N" : "-";
			string fH = ((r.f & F.H) != 0) ? "H" : "-";
			string fC = ((r.f & F.C) != 0) ? "C" : "-";

			Console.WriteLine("PC: 0x" + r.pc.ToString("x4") + "   SP: 0x" + r.sp.ToString("x4"));
			Console.WriteLine("B: 0x" + r.b.ToString("x2") + "   C: 0x" + r.c.ToString("x2") + "   BC: 0x" + r.bc.ToString("x4"));
			Console.WriteLine("D: 0x" + r.d.ToString("x2") + "   E: 0x" + r.e.ToString("x2") + "   DE: 0x" + r.de.ToString("x4"));
			Console.WriteLine("H: 0x" + r.h.ToString("x2") + "   L: 0x" + r.l.ToString("x2") + "   HL: 0x" + r.hl.ToString("x4"));
			Console.WriteLine("A: 0x" + r.a.ToString("x2") + "   F: " + fZ + fN + fH + fC      + "   AF: 0x" + r.af.ToString("x4"));
		}

		public static void SetBreakpoint(byte index, ushort address)
		{
			DbgState s = Read(0x0001);
			if (s == null)
			{
				Console.WriteLine("Set breakpoint failed!");
				return;
			}
			if (!s.halt)
			{
				Console.WriteLine("Set breakpoint failed: Not halted!");
				return;
			}
			byte[] buf = new byte[4];
			buf[0] = (byte)(0x80 | (index << 4) | (address & 0xf));
			address >>= 4;
			buf[1] = (byte)(0x80 | (index << 4) | (address & 0xf));
			address >>= 4;
			buf[2] = (byte)(0x80 | (index << 4) | (address & 0xf));
			address >>= 4;
			buf[3] = (byte)(0x80 | (index << 4) | (address & 0xf));
			Send(buf);
		}

		public static void SetReg(string reg, ushort val)
		{
			byte [] op;
			switch (reg)
			{
			case "bc":
				op = new byte[] { 0x01, (byte)val, (byte)(val >> 8) }; // LD BC, d16
				break;
			case "de":
				op = new byte[] { 0x11, (byte)val, (byte)(val >> 8) }; // LD DE, d16
				break;
			case "hl":
				op = new byte[] { 0x21, (byte)val, (byte)(val >> 8) }; // LD HL, d16
				break;
			case "sp":
				op = new byte[] { 0x31, (byte)val, (byte)(val >> 8) }; // LD SP, d16
				break;
			case "pc":
				op = new byte[] { 0xc3, (byte)val, (byte)(val >> 8) }; // JP a16
				break;
			case "b":
				op = new byte[] { 0x06, (byte)val }; // LD B, d8
				break;
			case "c":
				op = new byte[] { 0x0e, (byte)val }; // LD C, d8
				break;
			case "d":
				op = new byte[] { 0x16, (byte)val }; // LD D, d8
				break;
			case "e":
				op = new byte[] { 0x1e, (byte)val }; // LD E, d8
				break;
			case "h":
				op = new byte[] { 0x26, (byte)val }; // LD H, d8
				break;
			case "l":
				op = new byte[] { 0x2e, (byte)val }; // LD L, d8
				break;
			case "a":
				op = new byte[] { 0x3e, (byte)val }; // LD A, d8
				break;
			default:
				Console.WriteLine("Invalid register");
				return;
			}
			Execute(op);
		}

		public static byte ReadMem(ushort address)
		{
			SetDbgState(true);
			ExecuteInternal(new byte[] { 0x7f }); // LD A, A
			DbgState A = Read(0x000c);
			ExecuteInternal(new byte[] { 0xfa, (byte)address, (byte)(address >> 8) }); // LD A, (a16)
			ExecuteInternal(new byte[] { 0x7f }); // LD A, A
			DbgState m = Read(0x000c);
			ExecuteInternal(new byte[] { 0x3e, A.arg }); // LD A, d8
			SetDbgState(false);
			SetDrvData(DrvData.Default, 0);
			return m.arg;
		}

		public static void ShowMem(ushort address)
		{
			DbgState s = Read(0x0001);
			if (s == null)
			{
				Console.WriteLine("Read mem failed!");
				return;
			}
			if (!s.halt)
			{
				Console.WriteLine("Read mem failed: Not halted!");
				return;
			}

			byte b = ReadMem(address);
			Console.WriteLine("0x" + b.ToString("x2"));
		}

		private static string portfile;

		public static void OpenPort()
		{
			if (p != null)
			{
				p.Close();
			}

			p = new SerialPort(portfile, 1000000, Parity.None, 8, StopBits.One);
			p.Handshake = Handshake.RequestToSend;
			p.ReadBufferSize  = 16;
			p.WriteBufferSize = 16;
			p.WriteTimeout = SerialPort.InfiniteTimeout;
			p.ReadTimeout = 100;
			try { p.DiscardNull = false; } catch { }
			try { p.ReceivedBytesThreshold = 1; } catch { }
			p.Open();
			p.DiscardOutBuffer();
			p.DiscardInBuffer();
		}

		public static int Main(string[] args)
		{
			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => e.Cancel = true;
			Console.TreatControlCAsInput = false;

			portfile = args[0];
			OpenPort();

			while (true)
			{
				Console.Write("gbdbg# ");
				string cmd = Console.ReadLine();

				string[] a = cmd.Split(new char[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries);
				if (a.Length == 0) continue;

				switch (a[0])
				{
				case "quit":
					return 0;
				case "h":
					Halt();
					break;
				case "c":
					Continue();
					break;
				case "s":
					Step();
					break;
				case "r":
					ShowRegs();
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
						SetBreakpoint((byte)index, (ushort)address);
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
						SetReg(a[1], (ushort)val);
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
						ShowMem((ushort)address);
					}
					break;
				default:
					Console.WriteLine("Invalid command!");
					break;
				}
			}
		}
	}
}
