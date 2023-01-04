using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using Aitex.Common.Util;

namespace MECF.Framework.Common.Utilities
{
	public class BinarySerializer<T> where T : new()
	{
		private static string pathName = PathManager.GetDirectory("Objects");

		private static object thisLock = new object();

		public static void ToStream(T stuff)
		{
			lock (thisLock)
			{
				if (!Directory.Exists(pathName))
				{
					Directory.CreateDirectory(pathName);
				}
				string fullName = typeof(T).FullName;
				IFormatter formatter = new BinaryFormatter();
				Stream stream = new FileStream(pathName + "\\" + fullName + ".obj", FileMode.Create);
				try
				{
					using (stream)
					{
						formatter.Serialize(stream, stuff);
						stream.Close();
					}
				}
				catch (SerializationException ex)
				{
					Console.WriteLine(ex.Message);
					throw;
				}
				catch (Exception ex2)
				{
					Console.WriteLine(ex2.Message);
					throw;
				}
			}
		}

		public static void ToStream(T stuff, string fileName)
		{
			lock (thisLock)
			{
				if (!Directory.Exists(pathName))
				{
					Directory.CreateDirectory(pathName);
				}
				IFormatter formatter = new BinaryFormatter();
				Stream stream = new FileStream(pathName + "\\" + fileName + ".obj", FileMode.Create);
				try
				{
					using (stream)
					{
						formatter.Serialize(stream, stuff);
						stream.Close();
					}
				}
				catch (SerializationException ex)
				{
					Console.WriteLine(ex.Message);
					throw;
				}
				catch (Exception ex2)
				{
					Console.WriteLine(ex2.Message);
					throw;
				}
			}
		}

		public static T FromStream()
		{
			lock (thisLock)
			{
				if (!Directory.Exists(pathName))
				{
					Directory.CreateDirectory(pathName);
				}
				T result = default(T);
				string fullName = typeof(T).FullName;
				string path = pathName + "\\" + fullName + ".obj";
				if (!File.Exists(path))
				{
					return new T();
				}
				IFormatter formatter = new BinaryFormatter();
				Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
				try
				{
					using (stream)
					{
						result = (T)formatter.Deserialize(stream);
						stream.Close();
					}
				}
				catch (SerializationException ex)
				{
					Console.WriteLine(ex.Message);
				}
				return result;
			}
		}

		public static T FromStream(string fileName)
		{
			lock (thisLock)
			{
				if (!Directory.Exists(pathName))
				{
					Directory.CreateDirectory(pathName);
				}
				T result = default(T);
				string path = pathName + "\\" + fileName + ".obj";
				if (!File.Exists(path))
				{
					return new T();
				}
				IFormatter formatter = new BinaryFormatter();
				Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read);
				try
				{
					using (stream)
					{
						result = (T)formatter.Deserialize(stream);
						stream.Close();
					}
				}
				catch (SerializationException ex)
				{
					Console.WriteLine(ex.Message);
				}
				return result;
			}
		}
	}
}
