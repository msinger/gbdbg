using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace gbdbg
{
	public abstract partial class Lr35902LexerBase
	{
		protected Lr35902LexerBase() { }

		protected static LinkedListNode<LexerToken> Lex(string line)
		{
			LinkedList<LexerToken> t = new LinkedList<LexerToken>();
			StringBuilder sb = new StringBuilder();
			int pos = -1;
			char c = '\0';

		read:
			pos++;
		next:
			if (line.Length == pos)
				goto eot;

			c = line[pos];

			// The easy symbols:
			switch (c)
			{
			case ';':
				goto eot;
			case '/':
				if (pos + 1 == line.Length || line[pos + 1] != '/')
					goto unknown;
				goto eot;
			case ',':
				t.AddLast(new LexerToken(pos, LexerTokenType.Comma));
				goto read;
			case '(':
				t.AddLast(new LexerToken(pos, LexerTokenType.Open));
				goto read;
			case ')':
				t.AddLast(new LexerToken(pos, LexerTokenType.Close));
				goto read;
			case '+':
				t.AddLast(new LexerToken(pos, LexerTokenType.Plus));
				goto read;
			case '-':
				t.AddLast(new LexerToken(pos, LexerTokenType.Minus));
				goto read;
			case ':':
				t.AddLast(new LexerToken(pos, LexerTokenType.Colon));
				goto read;
			case '.':
				t.AddLast(new LexerToken(pos, LexerTokenType.Dot));
				goto read;
			}

			// Space, Tab, ...?
			if (char.GetUnicodeCategory(c) == UnicodeCategory.SpaceSeparator || c == '\t')
				goto read;

			// Start of name?
			if (char.IsLetter(c) || c == '_')
			{
				int name_pos = pos;
				sb.Clear();
				while (char.IsLetterOrDigit(c) || c == '_')
				{
					sb.Append(c);
					pos++;
					if (line.Length == pos)
						break;
					c = line[pos];
				}
				t.AddLast(new LexerToken(name_pos, LexerTokenType.Name, sb.ToString()));
				goto next;
			}

			// Start of value?
			if (char.IsDigit(c) || c == '$')
			{
				int value_pos = pos;
				bool hex = false;
				bool bin = false;
				bool oct = false;
				sb.Clear();
				if (c == '$')
				{
					hex = true;
					c = '_';
				}
				else if (c == '0')
				{
					if (pos + 1 != line.Length)
					{
						if (line[pos + 1] == 'x')
						{
							hex = true;
							pos++;
							c = '_';
						}
						else if (line[pos + 1] == 'b')
						{
							bin = true;
							pos++;
							c = '_';
						}
						else
							oct = true;
					}
				}
				while (char.IsLetterOrDigit(c) || c == '_')
				{
					if (c != '_') // Underscore is ignored in numbers; can be used as separator for nibbles or thousands.
						sb.Append(c);
					pos++;
					if (line.Length == pos)
						break;
					c = line[pos];
				}
				string str = sb.ToString();
				int val;
				if (hex)
				{
					if (!int.TryParse(str, NumberStyles.AllowHexSpecifier, NumberFormatInfo.InvariantInfo, out val))
						throw new AsmFormatException(value_pos, "Invalid hex number encountered by lexer.");
				}
				else if (bin)
				{
					try
					{
						val = Convert.ToInt32(str, 2);
					}
					catch (Exception e)
					{
						throw new AsmFormatException(value_pos, "Invalid binary number encountered by lexer.", e);
					}
				}
				else if (oct)
				{
					try
					{
						val = Convert.ToInt32(str, 8);
					}
					catch (Exception e)
					{
						throw new AsmFormatException(value_pos, "Invalid octal number encountered by lexer.", e);
					}
				}
				else
				{
					if (!int.TryParse(str, NumberStyles.None, NumberFormatInfo.InvariantInfo, out val))
						throw new AsmFormatException(value_pos, "Invalid decimal number encountered by lexer.");
				}
				t.AddLast(new LexerToken(value_pos, LexerTokenType.Value, val));
				goto next;
			}

		unknown:
			throw new AsmFormatException(pos, "Unknown input character encountered by lexer.");

		eot:
			t.AddLast(new LexerToken(pos, LexerTokenType.EOT));
			return t.First;
		}
	}
}
