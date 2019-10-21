using System;

namespace gbdbg
{
	[Serializable]
	public class AsmFormatException : FormatException
	{
		private readonly int pos, line, col;

		public AsmFormatException(int pos, int line, int col, string message, Exception inner)
			: base(message, inner)
		{
			this.pos  = pos;
			this.line = line;
			this.col  = col;
		}

		public AsmFormatException(int pos, int line, int col, string message)
			: this(pos, line, col, message, null)
		{ }

		public AsmFormatException(int pos, string message, Exception inner)
			: this(pos, 0, pos + 1, message, inner)
		{ }

		public AsmFormatException(int pos, string message)
			: this(pos, message, null)
		{ }

		protected AsmFormatException(System.Runtime.Serialization.SerializationInfo info,
		                             System.Runtime.Serialization.StreamingContext  context)
			: base(info, context)
		{
			pos  = info.GetInt32("Position");
			line = info.GetInt32("Line");
			col  = info.GetInt32("Column");
		}

		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
		                                   System.Runtime.Serialization.StreamingContext  context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Position", pos,  typeof(int));
			info.AddValue("Line",     line, typeof(int));
			info.AddValue("Column",   col,  typeof(int));
		}

		public int Position
		{
			get { return pos; }
		}

		public int Line
		{
			get { return line; }
		}

		public int Column
		{
			get { return col; }
		}

		public override string Message
		{
			get
			{
				string s = base.Message;
				if (line != 0 && col != 0)
					s = line.ToString() + ":" + col.ToString() + ": " + s;
				else if (col != 0)
					s = col.ToString() + ": " + s;
				return s;
			}
		}
	}
}
