namespace gbdbg
{
	public partial class Sm83Assembler
	{
		protected class OriginDirective : ParserToken
		{
			public readonly int Address;

			public OriginDirective(int pos, int adr) : base(pos)
			{
				Address = adr;
			}

			public override string ToString()
			{
				return ".ORG($" + Address.ToString("x") + ")";
			}
		}
	}
}
