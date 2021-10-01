using System;

namespace gbdbg
{
	[Serializable]
	public class LabelTooFarException : Exception
	{
		private readonly string label;

		public LabelTooFarException(string label, string message, Exception inner)
			: base(string.Format(message, label), inner)
		{
			this.label = label;
		}

		public LabelTooFarException(string label, string message)
			: this(label, message, null)
		{ }

		public LabelTooFarException(string label)
			: this(label, "Label too far away for relative addressing: {0}", null)
		{ }

		protected LabelTooFarException(System.Runtime.Serialization.SerializationInfo info,
		                               System.Runtime.Serialization.StreamingContext  context)
			: base(info, context)
		{
			label = info.GetString("Label");
		}

		public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info,
		                                   System.Runtime.Serialization.StreamingContext  context)
		{
			base.GetObjectData(info, context);
			info.AddValue("Label", label, typeof(string));
		}

		public string Label
		{
			get { return label; }
		}
	}
}
