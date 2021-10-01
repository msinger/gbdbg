using System;

namespace gbdbg
{
	[Serializable]
	public class LabelNotFoundException : Exception
	{
		private readonly string label;

		public LabelNotFoundException(string label, string message, Exception inner)
			: base(string.Format(message, label), inner)
		{
			this.label = label;
		}

		public LabelNotFoundException(string label, string message)
			: this(label, message, null)
		{ }

		public LabelNotFoundException(string label)
			: this(label, "Label not found: {0}", null)
		{ }

		protected LabelNotFoundException(System.Runtime.Serialization.SerializationInfo info,
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
