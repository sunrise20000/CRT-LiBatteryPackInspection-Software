using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Aitex.Core.RT.RecipeCenter
{
	[Serializable]
	[GeneratedCode("xsd", "4.0.30319.1")]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	public class AitexTableRecipeFormatCatalogGroupStep
	{
		private AitexTableRecipeFormatCatalogGroupStepItem[] itemField;

		private string moduleNameField;

		private string deviceTypeField;

		private string displayNameField;

		private string controlNameField;

		private string inputTypeField;

		private string descriptionField;

		private string minField;

		private string maxField;

		private string isMoRunModeStepField;

		[XmlElement("Item", Form = XmlSchemaForm.Unqualified)]
		public AitexTableRecipeFormatCatalogGroupStepItem[] Item
		{
			get
			{
				return itemField;
			}
			set
			{
				itemField = value;
			}
		}

		[XmlAttribute]
		public string ModuleName
		{
			get
			{
				return moduleNameField;
			}
			set
			{
				moduleNameField = value;
			}
		}

		[XmlAttribute]
		public string DeviceType
		{
			get
			{
				return deviceTypeField;
			}
			set
			{
				deviceTypeField = value;
			}
		}

		[XmlAttribute]
		public string DisplayName
		{
			get
			{
				return displayNameField;
			}
			set
			{
				displayNameField = value;
			}
		}

		[XmlAttribute]
		public string ControlName
		{
			get
			{
				return controlNameField;
			}
			set
			{
				controlNameField = value;
			}
		}

		[XmlAttribute]
		public string InputType
		{
			get
			{
				return inputTypeField;
			}
			set
			{
				inputTypeField = value;
			}
		}

		[XmlAttribute]
		public string Description
		{
			get
			{
				return descriptionField;
			}
			set
			{
				descriptionField = value;
			}
		}

		[XmlAttribute]
		public string Min
		{
			get
			{
				return minField;
			}
			set
			{
				minField = value;
			}
		}

		[XmlAttribute]
		public string Max
		{
			get
			{
				return maxField;
			}
			set
			{
				maxField = value;
			}
		}

		[XmlAttribute]
		public string IsMoRunModeStep
		{
			get
			{
				return isMoRunModeStepField;
			}
			set
			{
				isMoRunModeStepField = value;
			}
		}
	}
}
