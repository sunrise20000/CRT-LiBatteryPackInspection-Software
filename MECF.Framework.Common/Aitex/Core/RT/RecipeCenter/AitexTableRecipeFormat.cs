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
	public class AitexTableRecipeFormat
	{
		private AitexTableRecipeFormatCatalog[] catalogField;

		private AitexTableRecipeFormatRestrictionsInjectFlowCheck[][] restrictionsField;

		private string recipeVersionField;

		[XmlElement("Catalog", Form = XmlSchemaForm.Unqualified)]
		public AitexTableRecipeFormatCatalog[] Catalog
		{
			get
			{
				return catalogField;
			}
			set
			{
				catalogField = value;
			}
		}

		[XmlArray(Form = XmlSchemaForm.Unqualified)]
		[XmlArrayItem("InjectFlowCheck", typeof(AitexTableRecipeFormatRestrictionsInjectFlowCheck), Form = XmlSchemaForm.Unqualified, IsNullable = false)]
		public AitexTableRecipeFormatRestrictionsInjectFlowCheck[][] Restrictions
		{
			get
			{
				return restrictionsField;
			}
			set
			{
				restrictionsField = value;
			}
		}

		[XmlAttribute]
		public string RecipeVersion
		{
			get
			{
				return recipeVersionField;
			}
			set
			{
				recipeVersionField = value;
			}
		}
	}
}
