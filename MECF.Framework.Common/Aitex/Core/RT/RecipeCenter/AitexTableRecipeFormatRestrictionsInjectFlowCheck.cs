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
	public class AitexTableRecipeFormatRestrictionsInjectFlowCheck
	{
		private string sourceField;

		private string diluteField;

		private string injectField;

		[XmlAttribute]
		public string Source
		{
			get
			{
				return sourceField;
			}
			set
			{
				sourceField = value;
			}
		}

		[XmlAttribute]
		public string Dilute
		{
			get
			{
				return diluteField;
			}
			set
			{
				diluteField = value;
			}
		}

		[XmlAttribute]
		public string Inject
		{
			get
			{
				return injectField;
			}
			set
			{
				injectField = value;
			}
		}
	}
}
