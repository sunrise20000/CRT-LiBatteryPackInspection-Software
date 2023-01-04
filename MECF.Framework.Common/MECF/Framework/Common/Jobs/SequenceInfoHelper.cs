using System;
using System.Xml;
using Aitex.Core.RT.Log;
using Aitex.Core.RT.RecipeCenter;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.Jobs
{
	public class SequenceInfoHelper
	{
		public static SequenceInfo GetInfo(string seqFile)
		{
			SequenceInfo sequenceInfo = new SequenceInfo(seqFile);
			string sequence = Singleton<RecipeFileManager>.Instance.GetSequence(seqFile, needValidation: false);
			if (!string.IsNullOrEmpty(sequence))
			{
				try
				{
					XmlDocument xmlDocument = new XmlDocument();
					xmlDocument.LoadXml(sequence);
					XmlNodeList xmlNodeList = xmlDocument.SelectNodes("Aitex/TableSequenceData/Step");
					if (xmlNodeList == null)
					{
						LOG.Error(seqFile + " has no step");
						return null;
					}
					foreach (object item in xmlNodeList)
					{
						XmlElement xmlElement = item as XmlElement;
						SequenceStepInfo sequenceStepInfo = new SequenceStepInfo();
						foreach (XmlAttribute attribute in xmlElement.Attributes)
						{
							if (attribute.Name == "Position" || attribute.Name == "LLSelection" || attribute.Name == "PMSelection" || attribute.Name == "PMSelection25")
							{
								if (attribute.Value == "LL" || attribute.Value == "PM" || !Enum.TryParse<ModuleName>(attribute.Value, out var _))
								{
									continue;
								}
								string[] array = attribute.Value.Split(',');
								if (array.Length < 1)
								{
									LOG.Error(seqFile + " Position " + attribute.Value + " can not be empty");
									return null;
								}
								string[] array2 = array;
								foreach (string text in array2)
								{
									ModuleName moduleName = ModuleHelper.Converter(text);
									if (moduleName == ModuleName.System)
									{
										LOG.Error(seqFile + " Position " + text + " not valid");
										return null;
									}
									sequenceStepInfo.StepModules.Add(moduleName);
								}
							}
							else if (attribute.Name == "AlignerAngle")
							{
								if (!double.TryParse(attribute.Value, out var result2))
								{
									LOG.Error(seqFile + " AlignAngle " + attribute.Value + " not valid");
									return null;
								}
								sequenceStepInfo.AlignAngle = result2;
							}
							else if (attribute.Name == "Recipe" || attribute.Name == "ProcessRecipe")
							{
								sequenceStepInfo.RecipeName = attribute.Value;
							}
							else
							{
								sequenceStepInfo.StepParameter[attribute.Name] = attribute.Value;
							}
						}
						sequenceInfo.Steps.Add(sequenceStepInfo);
					}
				}
				catch (Exception ex)
				{
					LOG.Write(ex);
					return null;
				}
			}
			return sequenceInfo;
		}
	}
}
