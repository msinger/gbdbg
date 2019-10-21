namespace gbdbg
{
	public partial class Lr35902Assembler
	{
		protected class PostDecrement : Argument
		{
			public readonly Argument Arg;

			public PostDecrement(int pos, Argument arg) : base(pos)
			{
				Arg = arg;
			}

			public override string ToString()
			{
				return Arg.ToString() + "-";
			}
		}
	}
}
