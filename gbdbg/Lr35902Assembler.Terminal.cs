namespace gbdbg
{
	public partial class Lr35902Assembler
	{
		protected class Terminal : Argument
		{
			public readonly LexerToken Token;

			public Terminal(int pos, LexerToken token) : base(pos)
			{
				Token = token;
			}

			public override string ToString()
			{
				return Token.ToString();
			}
		}
	}
}
