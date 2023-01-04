using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Soap;

namespace Aitex.Core.Util
{
	public class SerializeStatic
	{
		public static bool Save(Type static_class, string filename)
		{
			try
			{
				FieldInfo[] fields = static_class.GetFields(BindingFlags.Static | BindingFlags.Public);
				object[,] array = new object[fields.Length, 2];
				int num = 0;
				FieldInfo[] array2 = fields;
				foreach (FieldInfo fieldInfo in array2)
				{
					array[num, 0] = fieldInfo.Name;
					array[num, 1] = fieldInfo.GetValue(null);
					num++;
				}
				Stream stream = File.Open(filename, FileMode.Create);
				SoapFormatter soapFormatter = new SoapFormatter();
				soapFormatter.Serialize(stream, array);
				stream.Close();
				return true;
			}
			catch
			{
				return false;
			}
		}

		public static bool Load(Type static_class, string filename)
		{
			try
			{
				FieldInfo[] fields = static_class.GetFields(BindingFlags.Static | BindingFlags.Public);
				Stream stream = File.Open(filename, FileMode.Open);
				SoapFormatter soapFormatter = new SoapFormatter();
				object[,] array = soapFormatter.Deserialize(stream) as object[,];
				stream.Close();
				if (array.GetLength(0) != fields.Length)
				{
					return false;
				}
				int num = 0;
				FieldInfo[] array2 = fields;
				foreach (FieldInfo fieldInfo in array2)
				{
					if (fieldInfo.Name == array[num, 0] as string)
					{
						fieldInfo.SetValue(null, array[num, 1]);
					}
					num++;
				}
				return true;
			}
			catch
			{
				return false;
			}
		}
	}
}
