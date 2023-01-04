using System;
using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Xml.Serialization;

namespace Aitex.Core.RT.RecipeCenter
{
	[Serializable]
	[GeneratedCode("xsd", "4.0.30319.1")]
	[DebuggerStepThrough]
	[DesignerCategory("code")]
	[XmlType(AnonymousType = true)]
	public class AitexTableRecipeFormatCatalogGroupStepItem
	{
		private string displayNameField;

		private string controlNameField;

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
	}
}
