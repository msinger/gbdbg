namespace gbdbg
{
	public partial class Sm83Assembler
	{
		protected abstract class ParserToken
		{
			public readonly int Pos;

			protected ParserToken(int pos)
			{
				Pos = pos;
			}
		}
	}
}
