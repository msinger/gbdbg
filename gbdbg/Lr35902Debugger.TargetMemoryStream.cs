using System;
using System.IO;

namespace gbdbg
{
	public partial class Lr35902Debugger
	{
		protected class TargetMemoryStream : Stream
		{
			private Lr35902Debugger dbg;
			private long adr;

			public TargetMemoryStream(Lr35902Debugger dbg)
			{
				this.dbg = dbg;
			}

			public override bool CanRead
			{
				get { return true; }
			}

			public override bool CanWrite
			{
				get { return true; }
			}

			public override bool CanSeek
			{
				get { return true; }
			}

			public override long Length
			{
				get { return 0x10000; }
			}

			public override void Flush()
			{
			}

			public override void SetLength(long value)
			{
				throw new NotImplementedException();
			}

			public override long Position
			{
				get { return adr; }
				set { adr = value; }
			}

			public override long Seek(long offset, SeekOrigin origin)
			{
				switch (origin)
				{
				case SeekOrigin.Begin:   adr = offset;          return adr;
				case SeekOrigin.Current: adr += offset;         return adr;
				case SeekOrigin.End:     adr = offset + Length; return adr;
				default: throw new ArgumentException();
				}
			}

			public override void Write(byte[] buffer, int offset, int count)
			{
				if (adr + count > Length)
					count = (int)(Length - adr);
				if (count <= 0)
					return;
				if (count == 1)
					dbg.WriteMem((ushort)adr, buffer[offset]);
				else
					dbg.WriteMemRange(buffer, offset, (ushort)adr, count);
				adr += count;
			}

			public override int Read(byte[] buffer, int offset, int count)
			{
				if (adr + count > Length)
					count = (int)(Length - adr);
				if (count <= 0)
					return 0;
				if (count == 1)
					buffer[offset] = dbg.ReadMem((ushort)adr);
				else
					dbg.ReadMemRange(buffer, offset, (ushort)adr, count);
				adr += count;
				return count;
			}
		}
	}
}
