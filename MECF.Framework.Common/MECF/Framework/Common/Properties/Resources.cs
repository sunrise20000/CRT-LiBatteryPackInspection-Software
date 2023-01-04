using System.CodeDom.Compiler;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Resources;
using System.Runtime.CompilerServices;

namespace MECF.Framework.Common.Properties
{
	[GeneratedCode("System.Resources.Tools.StronglyTypedResourceBuilder", "15.0.0.0")]
	[DebuggerNonUserCode]
	[CompilerGenerated]
	internal class Resources
	{
		private static ResourceManager resourceMan;

		private static CultureInfo resourceCulture;

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static ResourceManager ResourceManager
		{
			get
			{
				if (resourceMan == null)
				{
					ResourceManager resourceManager = (resourceMan = new ResourceManager("MECF.Framework.Common.Properties.Resources", typeof(Resources).Assembly));
				}
				return resourceMan;
			}
		}

		[EditorBrowsable(EditorBrowsableState.Advanced)]
		internal static CultureInfo Culture
		{
			get
			{
				return resourceCulture;
			}
			set
			{
				resourceCulture = value;
			}
		}

		internal static string AccountManager_Login_account_log_In => ResourceManager.GetString("AccountManager_Login_account_log_In", resourceCulture);

		internal static string Exit => ResourceManager.GetString("Exit", resourceCulture);

		internal static string IoRf_Initialize_RFInterlockIsNotSatisfiedCanNotPowerOn => ResourceManager.GetString("IoRf_Initialize_RFInterlockIsNotSatisfiedCanNotPowerOn", resourceCulture);

		internal static string IoRf_Initialize_SetRFMatchPositionC10 => ResourceManager.GetString("IoRf_Initialize_SetRFMatchPositionC10", resourceCulture);

		internal static string IoRf_Initialize_SetRFMatchPositionC20 => ResourceManager.GetString("IoRf_Initialize_SetRFMatchPositionC20", resourceCulture);

		internal static string IoRf_Initialize_SetRFPower => ResourceManager.GetString("IoRf_Initialize_SetRFPower", resourceCulture);

		internal static string IoRf_SetMatchMode_SystemDoNotSupportSettingMatchMode => ResourceManager.GetString("IoRf_SetMatchMode_SystemDoNotSupportSettingMatchMode", resourceCulture);

		internal static string IoRf_SetMatchPosition_MatchPositionC1SetTo0 => ResourceManager.GetString("IoRf_SetMatchPosition_MatchPositionC1SetTo0", resourceCulture);

		internal static string IoRf_SetMatchPosition_MatchPositionC2SetTo0 => ResourceManager.GetString("IoRf_SetMatchPosition_MatchPositionC2SetTo0", resourceCulture);

		internal static string IoRf_SetPower_RFInterlockIsNotSatisfiedCanNotBeOn => ResourceManager.GetString("IoRf_SetPower_RFInterlockIsNotSatisfiedCanNotBeOn", resourceCulture);

		internal static string RecipeFileManager_CheckRecipe_RecipeValidationFailed => ResourceManager.GetString("RecipeFileManager_CheckRecipe_RecipeValidationFailed", resourceCulture);

		internal static string RecipeFileManager_CreateFolder_RecipeFolder0Created => ResourceManager.GetString("RecipeFileManager_CreateFolder_RecipeFolder0Created", resourceCulture);

		internal static string RecipeFileManager_CreateFolder_RecipeFolder0CreateFailed => ResourceManager.GetString("RecipeFileManager_CreateFolder_RecipeFolder0CreateFailed", resourceCulture);

		internal static string RecipeFileManager_DeleteFolder_RecipeFolder0DeleteSucceeded => ResourceManager.GetString("RecipeFileManager_DeleteFolder_RecipeFolder0DeleteSucceeded", resourceCulture);

		internal static string RecipeFileManager_DeleteRecipe_RecipeFile0DeleteFailed => ResourceManager.GetString("RecipeFileManager_DeleteRecipe_RecipeFile0DeleteFailed", resourceCulture);

		internal static string RecipeFileManager_DeleteRecipe_RecipeFile0DeleteSucceeded => ResourceManager.GetString("RecipeFileManager_DeleteRecipe_RecipeFile0DeleteSucceeded", resourceCulture);

		internal static string RecipeFileManager_RenameFolder_RecipeFolder0renamed => ResourceManager.GetString("RecipeFileManager_RenameFolder_RecipeFolder0renamed", resourceCulture);

		internal static string RecipeFileManager_RenameFolder_RecipeFolder0RenameFailed => ResourceManager.GetString("RecipeFileManager_RenameFolder_RecipeFolder0RenameFailed", resourceCulture);

		internal static string RecipeFileManager_RenameRecipe_RecipeFile0FileExisted => ResourceManager.GetString("RecipeFileManager_RenameRecipe_RecipeFile0FileExisted", resourceCulture);

		internal static string RecipeFileManager_RenameRecipe_RecipeFile0Renamed => ResourceManager.GetString("RecipeFileManager_RenameRecipe_RecipeFile0Renamed", resourceCulture);

		internal static string RecipeFileManager_RenameRecipe_RecipeFile0RenameFailed => ResourceManager.GetString("RecipeFileManager_RenameRecipe_RecipeFile0RenameFailed", resourceCulture);

		internal static string RecipeFileManager_SaveAsRecipe_RecipeFile0savefailed => ResourceManager.GetString("RecipeFileManager_SaveAsRecipe_RecipeFile0savefailed", resourceCulture);

		internal static string RecipeFileManager_SaveRecipe_Barcode0IsDuplicatedIn1 => ResourceManager.GetString("RecipeFileManager_SaveRecipe_Barcode0IsDuplicatedIn1", resourceCulture);

		internal static string RecipeFileManager_SaveRecipe_RecipeFile0SaveCompleted => ResourceManager.GetString("RecipeFileManager_SaveRecipe_RecipeFile0SaveCompleted", resourceCulture);

		internal static string RecipeFileManager_SaveRecipe_RecipeFile0SaveFailed => ResourceManager.GetString("RecipeFileManager_SaveRecipe_RecipeFile0SaveFailed", resourceCulture);

		internal static string RecipeFileManager_SaveRecipe_SaveRecipeContentError => ResourceManager.GetString("RecipeFileManager_SaveRecipe_SaveRecipeContentError", resourceCulture);

		internal static string RecipeFileManager_ValidateRecipe_XMLSchemaValidateFailed => ResourceManager.GetString("RecipeFileManager_ValidateRecipe_XMLSchemaValidateFailed", resourceCulture);

		internal static string SCManager_SetItemValue_0SetpointShouldBeIn1And2SettingValue3IsNotValid => ResourceManager.GetString("SCManager_SetItemValue_0SetpointShouldBeIn1And2SettingValue3IsNotValid", resourceCulture);

		internal static string ViewManager_Exit_AreYouSureYouWantToExit => ResourceManager.GetString("ViewManager_Exit_AreYouSureYouWantToExit", resourceCulture);

		internal Resources()
		{
		}
	}
}
