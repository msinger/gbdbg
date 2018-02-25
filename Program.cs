using System;
using System.IO.Ports;
using System.Threading;

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
			for (int i = 0; i < buf.Length; i++)
			{
				bool ok = false;
				while (!ok)
				{
					ok = true;
					p.Write(buf, i, 1);
					try { if (p.Read(rbuf, 0, 1) != 1) ok = false; }
					catch (TimeoutException) { ok = false; }
					if (!ok)
						OpenPort();
				}
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

		public static void Step()
		{
			Send(new byte[] { 0x02 });
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

		public static void Execute(byte[] op)
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
			Step();
		}

		public static void Regs()
		{
			DbgState s = Read(0x0fff);
			if (s == null) return;

			string no_inc = s.no_inc ? "no_inc" : "inc";
			string halt   = s.halt ? "halt" : "running";
			string fZ = ((s.f & F.Z) != 0) ? "Z" : "-";
			string fN = ((s.f & F.N) != 0) ? "N" : "-";
			string fH = ((s.f & F.H) != 0) ? "H" : "-";
			string fC = ((s.f & F.C) != 0) ? "C" : "-";

			SetDbgState(true);

			Execute(new byte[] { 0x40 }); // LD B, B
			DbgState B = Read(0x000c);
			if (B == null) return;

			Execute(new byte[] { 0x49 }); // LD C, C
			DbgState C = Read(0x000c);
			if (C == null) return;

			Execute(new byte[] { 0x52 }); // LD D, D
			DbgState D = Read(0x000c);
			if (D == null) return;

			Execute(new byte[] { 0x5b }); // LD E, E
			DbgState E = Read(0x000c);
			if (E == null) return;

			Execute(new byte[] { 0x64 }); // LD H, H
			DbgState H = Read(0x000c);
			if (H == null) return;

			Execute(new byte[] { 0x6d }); // LD L, L
			DbgState L = Read(0x000c);
			if (L == null) return;

			Execute(new byte[] { 0x7f }); // LD A, A
			DbgState A = Read(0x000c);
			if (A == null) return;

			SetDbgState(false);
			SetDrvData(DrvData.Default, 0);

			Console.WriteLine("State: " + no_inc + ", " + halt + "   arg: 0x" + s.arg.ToString("x2"));
			Console.WriteLine("PC: 0x" + s.pc.ToString("x4") + "   SP: 0x" + s.sp.ToString("x4"));
			Console.WriteLine("B: 0x" + B.arg.ToString("x2") + "   C: 0x" + C.arg.ToString("x2") + "   BC: 0x" + B.arg.ToString("x2") + C.arg.ToString("x2"));
			Console.WriteLine("D: 0x" + D.arg.ToString("x2") + "   E: 0x" + E.arg.ToString("x2") + "   DE: 0x" + D.arg.ToString("x2") + E.arg.ToString("x2"));
			Console.WriteLine("H: 0x" + H.arg.ToString("x2") + "   L: 0x" + L.arg.ToString("x2") + "   HL: 0x" + H.arg.ToString("x2") + L.arg.ToString("x2"));
			Console.WriteLine("A: 0x" + A.arg.ToString("x2") + "   F: " + fZ + fN + fH + fC      + "   AF: 0x" + A.arg.ToString("x2") + ((byte)s.f).ToString("x2"));
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
					Regs();
					break;
				default:
					Console.WriteLine("Invalid command!");
					break;
				}
			}
		}
	}
}
