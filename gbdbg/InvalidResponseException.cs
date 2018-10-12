using System;

namespace gbdbg
{
	[Serializable]
	public class InvalidResponseException : ApplicationException
	{
		public InvalidResponseException() : base("Invalid response")
		{
		}

		public InvalidResponseException(string message) : base(message)
		{
		}

		public InvalidResponseException(string message, Exception innerException) : base(message, innerException)
		{
		}

		protected InvalidResponseException(System.Runtime.Serialization.SerializationInfo info,
			System.Runtime.Serialization.StreamingContext context) : base(info, context)
		{
		}
	}
}
