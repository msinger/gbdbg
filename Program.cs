using System;
using System.IO.Ports;
using System.Threading;

namespace gbdbg
{
	class MainClass
	{
		private static SerialPort p;
		
		public static void Halt()
		{
			p.Write(new byte[] { 0x00 }, 0, 1);
		}

		public static void Continue()
		{
			p.Write(new byte[] { 0x10 }, 0, 1);
		}

		public static void Step()
		{
			p.Write(new byte[] { 0x40 }, 0, 1);
		}

		public static void Regs()
		{
			byte[] buf = new byte[6];
			int i = 10000;
			p.DiscardInBuffer();
			p.Write(new byte[] { 0x50 }, 0, 1);
			while (p.BytesToRead < 6 && i-- > 0)
				Thread.Sleep(1);
			if (p.BytesToRead != 6)
			{
				p.DiscardInBuffer();
				Console.WriteLine("RX failed!");
				return;
			}
			int l = p.Read(buf, 0, 6);
			if (l != 6)
			{
				p.DiscardInBuffer();
				Console.WriteLine("RX failed!");
				return;
			}
			string no_inc = ((buf[0] & 2) != 0) ? "no_inc" : "inc";
			string halt   = ((buf[0] & 1) != 0) ? "halt" : "running";
			Console.WriteLine("State: " + no_inc + ", " + halt);
			string Z = ((buf[0] & 0x80) != 0) ? "Z" : "-";
			string N = ((buf[0] & 0x40) != 0) ? "N" : "-";
			string H = ((buf[0] & 0x20) != 0) ? "H" : "-";
			string C = ((buf[0] & 0x10) != 0) ? "C" : "-";
			Console.WriteLine("F: " + Z + N + H + C);
			Console.WriteLine("arg: 0x" + buf[1].ToString("x2"));
			Console.WriteLine("PC: 0x" + buf[3].ToString("x2") + buf[2].ToString("x2"));
			Console.WriteLine("SP: 0x" + buf[5].ToString("x2") + buf[4].ToString("x2"));
		}

		public static int Main(string[] args)
		{
			Console.CancelKeyPress += (object sender, ConsoleCancelEventArgs e) => e.Cancel = true;
			Console.TreatControlCAsInput = false;

			p = new SerialPort(args[0], 1000000, Parity.None, 8, StopBits.One);
			p.Handshake = Handshake.RequestToSend;
			p.ReadBufferSize  = 64;
			p.WriteBufferSize = 64;
			p.WriteTimeout = 500;
			try { p.DiscardNull = false; } catch { }
			try { p.ReceivedBytesThreshold = 1; } catch { }
			p.Open();
			p.DiscardOutBuffer();
			p.DiscardInBuffer();

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
