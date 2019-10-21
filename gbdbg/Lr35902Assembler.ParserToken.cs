namespace gbdbg
{
	public partial class Lr35902Assembler
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
