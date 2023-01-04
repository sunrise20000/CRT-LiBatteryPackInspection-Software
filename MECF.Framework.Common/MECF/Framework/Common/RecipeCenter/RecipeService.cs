using System;
using System.Collections.Generic;
using Aitex.Core.RT.Event;
using Aitex.Core.RT.Key;
using Aitex.Core.RT.RecipeCenter;
using Aitex.Core.Util;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.RecipeCenter
{
	internal class RecipeService : IRecipeService
	{
		public string LoadRecipe(ModuleName chamId, string recipeName)
		{
			EV.PostInfoLog(chamId.ToString(), $"Read {chamId} recipe {recipeName}.");
			return Singleton<RecipeFileManager>.Instance.LoadRecipe(chamId.ToString(), recipeName, needValidation: false);
		}

		public bool DeleteRecipe(ModuleName chamId, string recipeName)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return false;
			}
			EV.PostInfoLog(chamId.ToString(), $"Delete {chamId} recipe {recipeName}.");
			return Singleton<RecipeFileManager>.Instance.DeleteRecipe(chamId.ToString(), recipeName);
		}

		public string GetXmlRecipeList(ModuleName chamId, bool includingUsedRecipe)
		{
			return Singleton<RecipeFileManager>.Instance.GetXmlRecipeList(chamId.ToString(), includingUsedRecipe);
		}

		public IEnumerable<string> GetRecipes(ModuleName chamId, bool includingUsedRecipe)
		{
			return Singleton<RecipeFileManager>.Instance.GetRecipes(chamId.ToString(), includingUsedRecipe);
		}

		public bool RenameRecipe(ModuleName chamId, string oldName, string newName)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return false;
			}
			EV.PostInfoLog(chamId.ToString(), $"Rename {chamId} recipe {oldName} to {newName}.");
			return Singleton<RecipeFileManager>.Instance.RenameRecipe(chamId.ToString(), oldName, newName);
		}

		public bool DeleteFolder(ModuleName chamId, string folderName)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return false;
			}
			EV.PostInfoLog(chamId.ToString(), $"Delete {chamId} recipe folder {folderName}.");
			return Singleton<RecipeFileManager>.Instance.DeleteFolder(chamId.ToString(), folderName);
		}

		public bool SaveRecipe(ModuleName chamId, string recipeName, string recipeContent)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return false;
			}
			EV.PostInfoLog(chamId.ToString(), $"Modify and save {chamId} recipe {recipeName}.");
			return Singleton<RecipeFileManager>.Instance.SaveRecipe(chamId.ToString(), recipeName, recipeContent, clearBarcode: false, notifyUI: false);
		}

		public bool SaveAsRecipe(ModuleName chamId, string recipeName, string recipeContent)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return false;
			}
			EV.PostInfoLog(chamId.ToString(), $"Modify and save as {chamId} recipe {recipeName}.");
			return Singleton<RecipeFileManager>.Instance.SaveAsRecipe(chamId.ToString(), recipeName, recipeContent);
		}

		public bool CreateFolder(ModuleName chamId, string folderName)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return false;
			}
			EV.PostInfoLog(chamId.ToString(), $"Create {chamId} recipe foler {folderName}.");
			return Singleton<RecipeFileManager>.Instance.CreateFolder(chamId.ToString(), folderName);
		}

		public bool RenameFolder(ModuleName chamId, string oldName, string newName)
		{
			if (Singleton<KeyManager>.Instance.IsExpired)
			{
				EV.PostMessage("System", EventEnum.DefaultWarning, "Software is expired. Can not do the operation");
				return false;
			}
			EV.PostInfoLog(chamId.ToString(), $"Rename {chamId} recipe folder {oldName} to {newName}.");
			return Singleton<RecipeFileManager>.Instance.RenameFolder(chamId.ToString(), oldName, newName);
		}

		public string GetRecipeFormatXml(ModuleName chamId)
		{
			return Singleton<RecipeFileManager>.Instance.GetRecipeFormatXml(chamId.ToString());
		}

		public string GetRecipeTemplate(ModuleName chamId)
		{
			return Singleton<RecipeFileManager>.Instance.GetRecipeTemplate(chamId.ToString());
		}

		public Tuple<string, string> LoadRunTimeRecipeInfo(ModuleName chamId)
		{
			return null;
		}

		public string GetRecipeByBarcode(ModuleName chamId, string barcode)
		{
			return "";
		}

		public string GetXmlSequenceList(ModuleName chamId)
		{
			return Singleton<RecipeFileManager>.Instance.GetXmlSequenceList(chamId.ToString());
		}

		public string GetSequence(string sequenceName)
		{
			return Singleton<RecipeFileManager>.Instance.GetSequence(sequenceName, needValidation: true);
		}

		public List<string> GetSequenceNameList()
		{
			return Singleton<RecipeFileManager>.Instance.GetSequenceNameList();
		}

		public bool DeleteSequence(string sequenceName)
		{
			return Singleton<RecipeFileManager>.Instance.DeleteSequence(sequenceName);
		}

		public bool SaveSequence(string sequenceName, string sequenceContent)
		{
			return Singleton<RecipeFileManager>.Instance.SaveSequence(sequenceName, sequenceContent, notifyUI: false);
		}

		public bool SaveAsSequence(string sequenceName, string sequenceContent)
		{
			return Singleton<RecipeFileManager>.Instance.SaveAsSequence(sequenceName, sequenceContent);
		}

		public bool RenameSequence(string oldName, string newName)
		{
			return Singleton<RecipeFileManager>.Instance.RenameSequence(oldName, newName);
		}

		public string GetSequenceFormatXml()
		{
			return Singleton<RecipeFileManager>.Instance.GetSequenceFormatXml();
		}

		public bool RenameSequenceFolder(string oldName, string newName)
		{
			return Singleton<RecipeFileManager>.Instance.RenameSequenceFolder(oldName, newName);
		}

		public bool CreateSequenceFolder(string folderName)
		{
			return Singleton<RecipeFileManager>.Instance.CreateSequenceFolder(folderName);
		}

		public bool DeleteSequenceFolder(string folderName)
		{
			return Singleton<RecipeFileManager>.Instance.DeleteSequenceFolder(folderName);
		}

		public string LoadRecipeByPath(string pathName, string recipeName)
		{
			return Singleton<RecipeFileManager>.Instance.LoadRecipe(pathName, recipeName, needValidation: false);
		}

		public bool DeleteRecipeByPath(string pathName, string recipeName)
		{
			return Singleton<RecipeFileManager>.Instance.DeleteRecipe(pathName, recipeName);
		}

		public string GetXmlRecipeListByPath(string pathName, bool includingUsedRecipe)
		{
			return Singleton<RecipeFileManager>.Instance.GetXmlRecipeList(pathName, includingUsedRecipe);
		}

		public IEnumerable<string> GetRecipesByPath(string pathName, bool includingUsedRecipe)
		{
			return Singleton<RecipeFileManager>.Instance.GetRecipes(pathName, includingUsedRecipe);
		}

		public bool RenameRecipeByPath(string pathName, string oldName, string newName)
		{
			return Singleton<RecipeFileManager>.Instance.RenameRecipe(pathName, oldName, newName);
		}

		public bool DeleteFolderByPath(string pathName, string folderName)
		{
			return Singleton<RecipeFileManager>.Instance.DeleteFolder(pathName, folderName);
		}

		public bool SaveRecipeByPath(string pathName, string recipeName, string recipeContent)
		{
			return Singleton<RecipeFileManager>.Instance.SaveRecipe(pathName, recipeName, recipeContent, clearBarcode: false, notifyUI: false);
		}

		public bool SaveAsRecipeByPath(string pathName, string recipeName, string recipeContent)
		{
			return Singleton<RecipeFileManager>.Instance.SaveAsRecipe(pathName, recipeName, recipeContent);
		}

		public bool CreateFolderByPath(string pathName, string folderName)
		{
			return Singleton<RecipeFileManager>.Instance.CreateFolder(pathName, folderName);
		}

		public bool RenameFolderByPath(string pathName, string oldName, string newName)
		{
			return Singleton<RecipeFileManager>.Instance.RenameFolder(pathName, oldName, newName);
		}

		public string GetRecipeFormatXmlByPath(string pathName)
		{
			return Singleton<RecipeFileManager>.Instance.GetRecipeFormatXml(pathName);
		}

		public string GetRecipeTemplateByPath(string pathName)
		{
			return Singleton<RecipeFileManager>.Instance.GetRecipeTemplate(pathName);
		}

		public Tuple<string, string> LoadRunTimeRecipeInfoByPath(string pathName)
		{
			return null;
		}

		public string GetRecipeByBarcodeByPath(string pathName, string barcode)
		{
			return "";
		}
	}
}
