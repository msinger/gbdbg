using System;
using System.Collections.Generic;

namespace gbdbg
{
	public class Range : Sm83LexerBase, ICloneable
	{
		public int Start, Length;

		public Range(int start, int? length, int? end)
		{
			Start = start;
			Length = 1;
			if (length.HasValue)
			{
				Length = length.Value;
				if (end.HasValue && end.Value != End)
					throw new ArgumentException("length/end conflict", "end");
			}
			else if (end.HasValue)
			{
				End = end.Value;
			}
		}

		public Range(int start, int length) : this(start, length, null) { }
		public Range(int start) : this(start, null, null) { }

		public static Range Parse(string str, int defaultLength)
		{
			Range val;
			if (!TryParse(str, out val, defaultLength))
				throw new ArgumentException("Not a range");
			return val;
		}

		public static Range Parse(string str)
		{
			return Parse(str, 1);
		}

		public static bool TryParse(string str, out Range val, int defaultLength)
		{
			val = null;
			LinkedListNode<LexerToken> l = Lex(str);
			if (l.Value.Type != LexerTokenType.Value)
				return false;
			if (l.Next.Value.Type == LexerTokenType.EOT)
			{
				val = new Range(l.Value.Value, defaultLength);
				return true;
			}
			if (l.Next.Value.Type == LexerTokenType.Plus &&
			    l.Next.Next.Value.Type == LexerTokenType.Value &&
			    l.Next.Next.Next.Value.Type == LexerTokenType.EOT)
			{
				val = new Range(l.Value.Value, l.Next.Next.Value.Value);
				return true;
			}
			if (l.Next.Value.Type == LexerTokenType.Minus &&
			    l.Next.Next.Value.Type == LexerTokenType.Value &&
			    l.Next.Next.Next.Value.Type == LexerTokenType.EOT)
			{
				val = new Range(l.Value.Value, null, l.Next.Next.Value.Value);
				return true;
			}
			return false;
		}

		public static bool TryParse(string str, out Range val)
		{
			return TryParse(str, out val, 1);
		}

		public virtual Range Clone()
		{
			return new Range(Start, Length);
		}

		object ICloneable.Clone()
		{
			return Clone();
		}

		public int End
		{
			get { return Start + Length - 1; }
			set { Length = value - Start + 1; }
		}
	}
}
