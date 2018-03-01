using System;

namespace gbdbg
{
	[Serializable]
	public class NotHaltedException : ApplicationException
	{
		public NotHaltedException() : base("Not halted")
		{
		}

		public NotHaltedException(string message) : base(message)
		{
		}

		public NotHaltedException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected NotHaltedException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{
		}
	}
}
