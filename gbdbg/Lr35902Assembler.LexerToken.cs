namespace gbdbg
{
	public partial class Lr35902Assembler
	{
		protected class LexerToken
		{
			public readonly int            Pos;
			public readonly LexerTokenType Type;
			public readonly string         Name  = null;
			public readonly int            Value = 0;

			public LexerToken(int pos, LexerTokenType type)
			{
				Pos  = pos;
				Type = type;
			}

			public LexerToken(int pos, LexerTokenType type, string name)
			{
				Pos  = pos;
				Type = type;
				Name = name;
			}

			public LexerToken(int pos, LexerTokenType type, int val)
			{
				Pos   = pos;
				Type  = type;
				Value = val;
			}

			public override string ToString()
			{
				string s = null;
				switch (Type)
				{
				case LexerTokenType.EOT:   s = "<EOT>";            break;
				case LexerTokenType.Name:  s = "\"" + Name + "\""; break;
				case LexerTokenType.Value: s = Value.ToString();   break;
				case LexerTokenType.Comma: s = "<,>";              break;
				case LexerTokenType.Open:  s = "<(>";              break;
				case LexerTokenType.Close: s = "<)>";              break;
				case LexerTokenType.Plus:  s = "<+>";              break;
				case LexerTokenType.Minus: s = "<->";              break;
				case LexerTokenType.Colon: s = "<:>";              break;
				case LexerTokenType.Dot:   s = "<.>";              break;
				}
				return s;
			}
		}
	}
}
