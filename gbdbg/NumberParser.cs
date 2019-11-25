using System;
using System.Collections.Generic;

namespace gbdbg
{
	public abstract class NumberParser : Lr35902LexerBase
	{
		public static int Parse(string str)
		{
			int val;
			if (!TryParse(str, out val))
				throw new ArgumentException("Not a number");
			return val;
		}

		public static bool TryParse(string str, out int val)
		{
			val = -1;
			LinkedListNode<LexerToken> l = Lex(str);
			if (l.Value.Type != LexerTokenType.Value || l.Next.Value.Type != LexerTokenType.EOT)
				return false;
			val = l.Value.Value;
			return true;
		}
	}
}
