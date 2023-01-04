using System;

namespace Aitex.Core.RT.SCCore
{
	public class SCConfigItem
	{
		public string Name { get; set; }

		public string Path { get; set; }

		public string Default { get; set; }

		public string Min { get; set; }

		public string Max { get; set; }

		public string Unit { get; set; }

		public string Type { get; set; }

		public string Tag { get; set; }

		public string Parameter { get; set; }

		public string Description { get; set; }

		public object Value => Type switch
		{
			"Bool" => BoolValue, 
			"Double" => DoubleValue, 
			"String" => StringValue, 
			"Integer" => IntValue, 
			_ => null, 
		};

		public Type Typeof => Type switch
		{
			"Bool" => typeof(bool), 
			"Double" => typeof(double), 
			"String" => typeof(string), 
			"Integer" => typeof(int), 
			_ => null, 
		};

		public string PathName => string.IsNullOrEmpty(Path) ? Name : (Path + "." + Name);

		public int IntValue { get; set; }

		public double DoubleValue { get; set; }

		public bool BoolValue { get; set; }

		public string StringValue { get; set; }

		public SCConfigItem Clone()
		{
			return new SCConfigItem
			{
				Name = Name,
				Path = Path,
				Default = Default,
				Min = Min,
				Max = Max,
				Unit = Unit,
				Type = Type,
				Tag = Tag,
				Parameter = Parameter,
				Description = Description,
				StringValue = StringValue,
				IntValue = IntValue,
				DoubleValue = DoubleValue,
				BoolValue = BoolValue
			};
		}
	}
}
