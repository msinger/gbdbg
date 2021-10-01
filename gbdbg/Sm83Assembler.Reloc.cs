namespace gbdbg
{
	public partial class Sm83Assembler
	{
		protected class Reloc
		{
			public readonly string    Name;
			public readonly RelocType Type;
			public readonly ushort    RefAdr;

			public Reloc(string name, RelocType type, ushort refAdr)
			{
				Name   = name;
				Type   = type;
				RefAdr = refAdr;
			}

			public Reloc(string name, RelocType type)
				: this(name, type, 0)
			{ }

			public Reloc(string name, ushort refAdr)
				: this(name, RelocType.RelativeAddress8, refAdr)
			{ }
		}
	}
}
