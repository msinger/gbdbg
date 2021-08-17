namespace gbdbg
{
	public partial class Sm83Assembler
	{
		protected class Negation : Argument
		{
			public readonly Argument Arg;

			public Negation(int pos, Argument arg) : base(pos)
			{
				Arg = arg;
			}

			public override string ToString()
			{
				return "-" + Arg.ToString();
			}
		}
	}
}
