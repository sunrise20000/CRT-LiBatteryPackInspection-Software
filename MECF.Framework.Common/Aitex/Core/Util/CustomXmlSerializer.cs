using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace Aitex.Core.Util
{
	public class CustomXmlSerializer
	{
		public static void Serialize<T>(T t, string fileName)
		{
			using FileStream output = new FileStream(fileName, FileMode.Create);
			using XmlWriter xmlWriter = XmlWriter.Create(output);
			XmlSerializer xmlSerializer = new XmlSerializer(t.GetType());
			xmlSerializer.Serialize(xmlWriter, t);
			xmlWriter.Flush();
		}

		public static string Serialize<T>(T t)
		{
			StringBuilder stringBuilder = new StringBuilder();
			using (StringWriter textWriter = new StringWriter(stringBuilder))
			{
				XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
				xmlSerializer.Serialize(textWriter, t);
			}
			return stringBuilder.ToString();
		}

		public static T Deserialize<T>(string xmlString)
		{
			if (string.IsNullOrWhiteSpace(xmlString))
			{
				throw new ArgumentNullException("xmlString");
			}
			using StringReader textReader = new StringReader(xmlString);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
			return (T)xmlSerializer.Deserialize(textReader);
		}

		public static T Deserialize<T>(Stream streamReader)
		{
			if (streamReader == null)
			{
				throw new ArgumentNullException("streamReader");
			}
			if (streamReader.Length <= 0)
			{
				throw new EndOfStreamException("The stream is blank");
			}
			using XmlReader xmlReader = XmlReader.Create(streamReader);
			XmlSerializer xmlSerializer = new XmlSerializer(typeof(T));
			return (T)xmlSerializer.Deserialize(xmlReader);
		}

		public static T Deserialize<T>(FileInfo fi)
		{
			if (fi == null)
			{
				throw new ArgumentNullException("fi");
			}
			if (!fi.Exists)
			{
				throw new FileNotFoundException(fi.FullName);
			}
			using FileStream streamReader = new FileStream(fi.FullName, FileMode.Open, FileAccess.Read);
			return Deserialize<T>(streamReader);
		}
	}
}
