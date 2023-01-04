using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Xml;
using System.Xml.Serialization;
using Aitex.Core.RT.Log;

namespace Aitex.Core.Util
{
	public sealed class ObjectSerializer
	{
		private ObjectSerializer()
		{
		}

		public static MemoryStream SerializeObjectToMemoryStream(object valueToSerializeToMemoryStream)
		{
			MemoryStream memoryStream = new MemoryStream();
			Type type = valueToSerializeToMemoryStream.GetType();
			XmlSerializer xmlSerializer = new XmlSerializer(type);
			xmlSerializer.Serialize(memoryStream, valueToSerializeToMemoryStream);
			memoryStream.Seek(0L, SeekOrigin.Begin);
			return memoryStream;
		}

		public static XmlDocument SerializeObjectToXmlDom(object obj)
		{
			Stream inStream = SerializeObjectToMemoryStream(obj);
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.Load(inStream);
			return xmlDocument;
		}

		private static Type[] RemoveDuplicateType(Type typeOfObject, IEnumerable<Type> types)
		{
			List<Type> list = new List<Type>();
			foreach (Type type in types)
			{
				if (!type.FullName.Equals(typeOfObject.FullName))
				{
					list.Add(type);
				}
			}
			return list.ToArray();
		}

		public static MemoryStream SerializeObjectToMemoryStream(object valueToSerializeToMemoryStream, Type[] types)
		{
			MemoryStream memoryStream = new MemoryStream();
			Type type = valueToSerializeToMemoryStream.GetType();
			types = RemoveDuplicateType(type, types);
			XmlSerializer xmlSerializer = new XmlSerializer(type, types);
			xmlSerializer.Serialize(memoryStream, valueToSerializeToMemoryStream);
			memoryStream.Seek(0L, SeekOrigin.Begin);
			return memoryStream;
		}

		public static MemoryStream SerializeObjectToBinaryStream(object valueToSerializeToMemoryStream)
		{
			MemoryStream memoryStream = new MemoryStream();
			IFormatter formatter = new BinaryFormatter();
			formatter.Serialize(memoryStream, valueToSerializeToMemoryStream);
			memoryStream.Seek(0L, SeekOrigin.Begin);
			return memoryStream;
		}

		public static bool SerializeObjectToBinaryFile(string fullFileName, object valueToSerialize)
		{
			try
			{
				using FileStream fileStream = new FileStream(fullFileName, FileMode.OpenOrCreate);
				IFormatter formatter = new BinaryFormatter();
				formatter.Serialize(fileStream, valueToSerialize);
				fileStream.Seek(0L, SeekOrigin.Begin);
			}
			catch (Exception ex)
			{
				LOG.Error(ex.Message);
				return false;
			}
			return true;
		}

		public static string SerializeObjectToXml(object valueToSerializeToMemoryStream)
		{
			using MemoryStream stream = SerializeObjectToMemoryStream(valueToSerializeToMemoryStream);
			using StreamReader streamReader = new StreamReader(stream);
			return streamReader.ReadToEnd();
		}

		public static bool SerializeObjectToXmlFile(string fullFileName, object valueToSerializeToFile)
		{
			try
			{
				using FileStream fileStream = new FileStream(fullFileName, FileMode.Create);
				using (MemoryStream memoryStream = SerializeObjectToMemoryStream(valueToSerializeToFile))
				{
					memoryStream.WriteTo(fileStream);
					memoryStream.Close();
				}
				fileStream.Close();
			}
			catch (Exception ex)
			{
				LOG.Error(ex.Message);
				return false;
			}
			return true;
		}

		public static T DeserializeObjectFromStream<T>(Stream stream)
		{
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
			return (T)xmlSerializer.Deserialize(stream);
		}

		public static T DeserializeObjectFromBinaryFile<T>(string binFilePath)
		{
			T val = default(T);
			using FileStream stream = new FileStream(binFilePath, FileMode.OpenOrCreate);
			return (T)DeserializeObjectFromBinaryStream(stream);
		}

		public static object DeserializeObjectFromBinaryStream(Stream stream)
		{
			IFormatter formatter = new BinaryFormatter();
			return formatter.Deserialize(stream);
		}

		public static object DeserializeObjectFromStream(Stream stream, Type typeOfObject)
		{
			try
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeOfObject);
				return xmlSerializer.Deserialize(stream);
			}
			catch (Exception ex)
			{
				LOG.Error($"界面发序列化数据出错：\n函数名：{MethodBase.GetCurrentMethod().Name}\n串行数据内容：{stream.ToString()}", ex);
				throw;
			}
		}

		public static object DeserializeObjectFromStream(Stream stream, Type typeOfObject, Type[] types)
		{
			types = RemoveDuplicateType(typeOfObject, types);
			XmlSerializer xmlSerializer = new XmlSerializer(typeOfObject, types);
			return xmlSerializer.Deserialize(stream);
		}

		public static object DeserializeObjectFromXml(string xml, Type typeToDeserialize)
		{
			if (string.IsNullOrWhiteSpace(xml))
			{
				return null;
			}
			using MemoryStream memoryStream = new MemoryStream();
			XmlDocument xmlDocument = new XmlDocument();
			xmlDocument.LoadXml(xml);
			xmlDocument.Save(memoryStream);
			memoryStream.Seek(0L, SeekOrigin.Begin);
			return DeserializeObjectFromStream(memoryStream, typeToDeserialize);
		}

		public static T DeserializeObjectFromXmlFile<T>(string xmlFilePath)
		{
			T result = default(T);
			FileInfo fileInfo = new FileInfo(xmlFilePath);
			if (!fileInfo.Exists)
			{
				return result;
			}
			using (StreamReader streamReader = new StreamReader(xmlFilePath))
			{
				if (string.IsNullOrWhiteSpace(streamReader.ReadToEnd()))
				{
					return result;
				}
			}
			using (FileStream stream = new FileStream(xmlFilePath, FileMode.Open))
			{
				result = DeserializeObjectFromStream<T>(stream);
			}
			return result;
		}
	}
}
