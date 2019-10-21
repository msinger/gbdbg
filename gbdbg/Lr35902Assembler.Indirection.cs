namespace gbdbg
{
	public partial class Lr35902Assembler
	{
		protected class Indirection : Argument
		{
			public readonly Argument Arg;

			public Indirection(int pos, Argument arg) : base(pos)
			{
				Arg = arg;
			}

			public override string ToString()
			{
				return "*(" + Arg.ToString() + ")";
			}
		}
	}
}
