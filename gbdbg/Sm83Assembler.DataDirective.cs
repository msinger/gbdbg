using System.Collections.Generic;

namespace gbdbg
{
	public partial class Sm83Assembler
	{
		protected class DataDirective : ParserToken
		{
			public readonly IList<LexerToken> Data;

			public DataDirective(int pos, IList<LexerToken> data) : base(pos)
			{
				Data = data;
			}

			public override string ToString()
			{
				string s = ".DB(";
				for (int i = 0; i < Data.Count; i++)
				{
					s += Data[i].ToString();
					if (i < Data.Count - 1)
						s += ", ";
				}
				s += ")";
				return s;
			}
		}
	}
}
