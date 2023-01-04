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
	public class AitexTableRecipeFormatCatalog
	{
		private AitexTableRecipeFormatCatalogGroup[] groupField;

		private string displayNameField;

		[XmlElement("Group", Form = XmlSchemaForm.Unqualified)]
		public AitexTableRecipeFormatCatalogGroup[] Group
		{
			get
			{
				return groupField;
			}
			set
			{
				groupField = value;
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
	}
}
