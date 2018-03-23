using System;
using System.IO.Ports;

namespace gbdbg
{
	using Regs = Lr35902Registers;
	using F = Lr35902StatusFlags;

	public partial class Lr35902Debugger
	{
		[Serializable]
		[Flags]
		private enum Nibbles : ushort
		{
			State = 0x0001,
			Flags = 0x0002,
			Arg   = 0x000c,
			PC    = 0x00f0,
			SP    = 0x0f00,
			Regs  = Flags | PC | SP,
			Cpu   = Regs | Arg,
			All   = Cpu | State,
		}

		private struct State
		{
			public F F;
			public bool NoInc, Halt, IME;
			public byte Arg;
			public ushort PC, SP;
		}

		private struct DriveData
		{
			public bool Drive;
			public byte Data;
			public static readonly DriveData[] Default = new DriveData[4];
		}

		private string port;
		private SerialPort p;
		private readonly byte[] ret = new byte[16];

		private void ClearRet(Nibbles mask)
		{
			for (int i = 0, bit = 1; i < 16; i++, bit <<= 1)
				if (((int)mask & bit) != 0)
					ret[i] = 0;
		}

		private bool HasRet(Nibbles mask)
		{
			for (int i = 0, bit = 1; i < 16; i++, bit <<= 1)
				if (((int)mask & bit) != 0)
					if ((ret[i] & 0x80) == 0)
						return false;
			return true;
		}

		private byte[] ReceiveRet(Nibbles mask)
		{
			int cnt = 0;
			while (!HasRet(mask))
			{
				if (cnt++ > 64) throw new InvalidResponseException();
				Send(new byte[] { 0x01 });
			}
			byte[] buf = new byte[8];
			for (int i = 0, j = 0; i < 8; i++, j += 2)
				buf[i] = (byte)((ret[j] & 0xf) | ((ret[j + 1] & 0xf) << 4));
			return buf;
		}

		private State ReadState(Nibbles mask)
		{
			//Console.Write("B:");
			//for (int i = 0; i < 16; i++) Console.Write(" " + ret[i].ToString("x2"));
			//Console.WriteLine();
			byte[] buf = ReceiveRet(mask);
			//Console.Write("A:");
			//for (int i = 0; i < 16; i++) Console.Write(" " + ret[i].ToString("x2"));
			//Console.WriteLine();
			State s;
			s.IME   = (buf[0] & 8) != 0;
			s.NoInc = (buf[0] & 2) != 0;
			s.Halt  = (buf[0] & 1) != 0;
			s.F     = (F)(buf[0] & 0xf0);
			s.Arg   = buf[1];
			s.PC    = (ushort)(((int)buf[3] << 8) | buf[2]);
			s.SP    = (ushort)(((int)buf[5] << 8) | buf[4]);
			return s;
		}

		private void Send(byte[] buf)
		{
			byte[] rbuf = new byte[1];
			int cnt = 0;
		retry:
			if (cnt++ > 64) throw new InvalidResponseException();
			for (int i = 0; i < buf.Length; i++)
			{
				p.Write(buf, i, 1);
				try { if (p.Read(rbuf, 0, 1) != 1) goto retry; }
				catch (TimeoutException) { goto retry; }
				int k = rbuf[0] >> 4;
				ret[k] = (byte)(0x80 | (rbuf[0] & 0xf));
			}
		}

		public void Halt()
		{
			ClearRet(Nibbles.All);
			Send(new byte[] { 0x00 });
			if (!IsHalted) throw new InvalidResponseException();
		}

		public void Continue()
		{
			Send(new byte[] { 0x03 });
			ClearRet(Nibbles.All);
		}

		public bool IsHalted
		{
			get { return ReadState(Nibbles.State).Halt; }
		}

		private void CheckHalted()
		{
			if (!IsHalted) throw new NotHaltedException();
		}

		public void Step()
		{
			CheckHalted();
			Send(new byte[] { 0x02 });
			ClearRet(Nibbles.Cpu);
		}

		private bool NoIncrement
		{
			get { return ReadState(Nibbles.State).NoInc; }

			set
			{
				if (NoIncrement != value)
				{
					ClearRet(Nibbles.State);
					Send(new byte[] { (byte)(value ? 0x22 : 0x20) });
					if (NoIncrement != value) throw new InvalidResponseException();
				}
			}
		}

		private void SetDriveData(DriveData[] data)
		{
			for (int i = 0; i < 4; i++)
			{
				if (data[i].Drive)
					Send(new byte[] { (byte)(0x10 | (data[i].Data & 0xf)), (byte)(0x10 | (data[i].Data >> 4)) });
				Send(new byte[] { (byte)((data[i].Drive ? 0x60 : 0x40) | i) });
			}
		}

		public void Execute(byte[] op)
		{
			if (op.Length > 3) throw new ArgumentException("Max len is 3", "op");
			CheckHalted();
			bool prev_noinc = NoIncrement;
			int i;
			DriveData[] d = new DriveData[4];
			for (i = 0; i < op.Length; i++)
			{
				d[i].Drive = true;
				d[i].Data = op[i];
			}
			SetDriveData(d);
			NoIncrement = true;
			Step();
			NoIncrement = prev_noinc;
			SetDriveData(DriveData.Default);
		}

		public Regs Registers
		{
			get
			{
				CheckHalted();
				State s = ReadState(Nibbles.All);

				Regs r;
				r.PC = s.PC;
				r.SP = s.SP;
				r.F = s.F;
				r.IME = s.IME;

				bool prev_noinc = NoIncrement;
				NoIncrement = true;

				Execute(new byte[] { 0x40 }); // LD B, B
				r.B = ReadState(Nibbles.Arg).Arg;

				Execute(new byte[] { 0x49 }); // LD C, C
				r.C = ReadState(Nibbles.Arg).Arg;

				Execute(new byte[] { 0x52 }); // LD D, D
				r.D = ReadState(Nibbles.Arg).Arg;

				Execute(new byte[] { 0x5b }); // LD E, E
				r.E = ReadState(Nibbles.Arg).Arg;

				Execute(new byte[] { 0x64 }); // LD H, H
				r.H = ReadState(Nibbles.Arg).Arg;

				Execute(new byte[] { 0x6d }); // LD L, L
				r.L = ReadState(Nibbles.Arg).Arg;

				Execute(new byte[] { 0x7f }); // LD A, A
				r.A = ReadState(Nibbles.Arg).Arg;

				NoIncrement = prev_noinc;

				return r;
			}
		}

		public void SetBreakpoint(byte index, ushort address)
		{
			CheckHalted();
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

		private void SetAF(ushort af, bool restore_a)
		{
			CheckHalted();

			bool prev_noinc = NoIncrement;
			NoIncrement = true;

			ushort sp = ReadState(Nibbles.SP).SP;

			byte meml, memh;
			meml = ReadMem(0xdffe);
			memh = ReadMem(0xdfff);

			if (restore_a)
				Execute(new byte[] { 0xea, 0xff, 0xdf }); // LD (a16), A
			else
				WriteMem(0xdfff, (byte)(af >> 8));

			WriteMem(0xdffe, (byte)af);

			Execute(new byte[] { 0x31, 0xfe, 0xdf }); // LD SP, d16
			Execute(new byte[] { 0xf1 }); // POP AF

			WriteMem(0xdffe, meml);
			WriteMem(0xdfff, memh);
			Execute(new byte[] { 0x31, (byte)sp, (byte)(sp >> 8) }); // LD SP, d16

			NoIncrement = prev_noinc;
		}

		public void SetRegister(string reg, ushort val)
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
			case "af":
				SetAF(val, false);
				return;
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
			case "f":
				SetAF(val, true);
				return;
			default:
				throw new ArgumentException("invalid register name", "reg");
			}
			Execute(op);
		}

		public byte ReadMem(ushort address)
		{
			CheckHalted();

			bool prev_noinc = NoIncrement;
			NoIncrement = true;

			Execute(new byte[] { 0x7f }); // LD A, A
			byte a = ReadState(Nibbles.Arg).Arg;
			Execute(new byte[] { 0xfa, (byte)address, (byte)(address >> 8) }); // LD A, (a16)
			Execute(new byte[] { 0x7f }); // LD A, A
			byte m = ReadState(Nibbles.Arg).Arg;
			Execute(new byte[] { 0x3e, a }); // LD A, d8

			NoIncrement = prev_noinc;

			return m;
		}

		public void WriteMem(ushort address, byte val)
		{
			CheckHalted();

			bool prev_noinc = NoIncrement;
			NoIncrement = true;

			Execute(new byte[] { 0x7f }); // LD A, A
			byte a = ReadState(Nibbles.Arg).Arg;
			Execute(new byte[] { 0x3e, val }); // LD A, d8
			Execute(new byte[] { 0xea, (byte)address, (byte)(address >> 8) }); // LD (a16), A
			Execute(new byte[] { 0x3e, a }); // LD A, d8

			NoIncrement = prev_noinc;
		}

		private void OpenPort()
		{
			if (p != null)
			{
				p.Close();
			}

			p = new SerialPort(port, 1000000, Parity.None, 8, StopBits.One);
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

		public Lr35902Debugger(string port)
		{
			this.port = port;
			OpenPort();
		}

		public System.IO.Stream OpenMemory()
		{
			return new TargetMemoryStream(this);
		}

		public System.IO.Stream OpenMemory(ushort address)
		{
			TargetMemoryStream stm = new TargetMemoryStream(this);
			stm.Position = address;
			return stm;
		}

		public void RawSend(System.IO.Stream data)
		{
			data.CopyTo(p.BaseStream);
			p.BaseStream.Flush();
		}
	}
}
