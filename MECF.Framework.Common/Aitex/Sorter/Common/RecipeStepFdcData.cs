using System;
using System.Runtime.Serialization;

namespace Aitex.Sorter.Common
{
	[Serializable]
	[DataContract]
	public class RecipeStepFdcData
	{
		[DataMember]
		public int StepNumber { get; set; }

		[DataMember]
		public string Name { get; set; }

		[DataMember]
		public float SetPoint { get; set; }

		[DataMember]
		public int SampleCount { get; set; }

		[DataMember]
		public float MinValue { get; set; }

		[DataMember]
		public float MaxValue { get; set; }

		[DataMember]
		public float StdValue { get; set; }

		[DataMember]
		public float MeanValue { get; set; }
	}
}
