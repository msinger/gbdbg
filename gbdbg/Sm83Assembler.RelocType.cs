using System;

namespace gbdbg
{
	public partial class Sm83Assembler
	{
		[Serializable]
		public enum RelocType
		{
			AbsoluteAddress16, /* 0x0000 - 0xffff */
			AbsoluteAddress8,  /* 0xff00 - 0xffff */
			RelativeAddress8,  /* -128   - +127 */
		}
	}
}
