namespace gbdbg
{
	public partial class Lr35902Assembler
	{
		protected class Addition : Argument
		{
			public readonly Argument LeftOp, RightOp;

			public Addition(int pos, Argument op1, Argument op2) : base(pos)
			{
				LeftOp  = op1;
				RightOp = op2;
			}

			public override string ToString()
			{
				return LeftOp.ToString() + "+" + RightOp.ToString();
			}
		}
	}
}
