using System.Collections.Generic;

namespace gbdbg
{
	public partial class Sm83Assembler
	{
		protected class Instruction : ParserToken
		{
			public readonly string          Name;
			public readonly IList<Argument> Arg;

			public Instruction(int pos, string name, IList<Argument> args) : base(pos)
			{
				Name = name;
				Arg  = args;
			}

			public Instruction(int pos, string name) : this(pos, name, new Argument[] { })
			{ }

			public override string ToString()
			{
				string s = Name + "(";
				for (int i = 0; i < Arg.Count; i++)
				{
					s += Arg[i].ToString();
					if (i < Arg.Count - 1)
						s += ", ";
				}
				s += ")";
				return s;
			}
		}
	}
}
