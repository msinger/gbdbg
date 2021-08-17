using System;
using System.Collections.Generic;

namespace gbdbg
{
	public abstract class NameParser : Sm83LexerBase
	{
		public static string Parse(string str)
		{
			string name = TryParse(str);
			if (name == null)
				throw new ArgumentException("Not a name");
			return name;
		}

		public static string TryParse(string str)
		{
			LinkedListNode<LexerToken> l = Lex(str);
			if (l.Value.Type != LexerTokenType.Name || l.Next.Value.Type != LexerTokenType.EOT)
				return null;
			return l.Value.Name;
		}
	}
}
