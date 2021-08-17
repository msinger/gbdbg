namespace gbdbg
{
	public partial class Sm83Assembler
	{
		protected class Label : ParserToken
		{
			public readonly string Name;

			public Label(int pos, string name) : base(pos)
			{
				Name = name;
			}

			public override string ToString()
			{
				return Name + ":";
			}
		}
	}
}
