namespace gbdbg
{
	public partial class Sm83Assembler
	{
		protected class PostIncrement : Argument
		{
			public readonly Argument Arg;

			public PostIncrement(int pos, Argument arg) : base(pos)
			{
				Arg = arg;
			}

			public override string ToString()
			{
				return Arg.ToString() + "+";
			}
		}
	}
}
