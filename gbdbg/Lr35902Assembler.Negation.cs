namespace gbdbg
{
	public partial class Lr35902Assembler
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
