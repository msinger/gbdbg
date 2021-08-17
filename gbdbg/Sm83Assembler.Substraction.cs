namespace gbdbg
{
	public partial class Sm83Assembler
	{
		protected class Substraction : Argument
		{
			public readonly Argument LeftOp, RightOp;

			public Substraction(int pos, Argument op1, Argument op2) : base(pos)
			{
				LeftOp  = op1;
				RightOp = op2;
			}

			public override string ToString()
			{
				return LeftOp.ToString() + "-" + RightOp.ToString();
			}
		}
	}
}
