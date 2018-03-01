using System;

namespace gbdbg
{
	[Serializable]
	[Flags]
	public enum Lr35902StatusFlags : byte
	{
		Z = 0x80,
		N = 0x40,
		H = 0x20,
		C = 0x10,
	}
}
