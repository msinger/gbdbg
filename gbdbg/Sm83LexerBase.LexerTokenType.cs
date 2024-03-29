namespace gbdbg
{
	public abstract partial class Sm83LexerBase
	{
		[System.Serializable]
		protected enum LexerTokenType
		{
			EOT,     // End of Text
			Name,    // LD, ADD, JP, Z, NC, ...
			Value,   // 255, 0xff, $ff, ...
			Comma,   // ,
			Open,    // (
			Close,   // )
			Plus,    // +
			Minus,   // -
			Colon,   // :
			Dot,     // .
		}
	}
}
