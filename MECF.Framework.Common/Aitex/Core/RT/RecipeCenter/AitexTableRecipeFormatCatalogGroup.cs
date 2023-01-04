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
	public class AitexTableRecipeFormatCatalogGroup
	{
		private AitexTableRecipeFormatCatalogGroupStep[] stepField;

		private string displayNameField;

		public bool HasPermission { get; set; }

		[XmlElement("Step", Form = XmlSchemaForm.Unqualified)]
		public AitexTableRecipeFormatCatalogGroupStep[] Step
		{
			get
			{
				return stepField;
			}
			set
			{
				stepField = value;
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
