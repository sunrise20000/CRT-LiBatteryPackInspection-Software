using System;
using System.Collections.Generic;
using System.ServiceModel;
using MECF.Framework.Common.Equipment;

namespace MECF.Framework.Common.RecipeCenter
{
	[ServiceContract]
	public interface IRecipeService
	{
		[OperationContract]
		string LoadRecipe(ModuleName chamId, string recipeName);

		[OperationContract]
		Tuple<string, string> LoadRunTimeRecipeInfo(ModuleName chamId);

		[OperationContract]
		IEnumerable<string> GetRecipes(ModuleName chamId, bool includeUsedRecipe);

		[OperationContract]
		string GetXmlRecipeList(ModuleName chamId, bool includeUsedRecipe);

		[OperationContract]
		bool DeleteRecipe(ModuleName chamId, string recipeName);

		[OperationContract]
		bool DeleteFolder(ModuleName chamId, string folderName);

		[OperationContract]
		bool SaveAsRecipe(ModuleName chamId, string recipeName, string recipeContent);

		[OperationContract]
		bool SaveRecipe(ModuleName chamId, string recipeName, string recipeContent);

		[OperationContract]
		bool CreateFolder(ModuleName chamId, string folderName);

		[OperationContract]
		bool RenameRecipe(ModuleName chamId, string oldName, string newName);

		[OperationContract]
		bool RenameFolder(ModuleName chamId, string oldName, string newName);

		[OperationContract]
		string GetRecipeFormatXml(ModuleName chamId);

		[OperationContract]
		string GetRecipeTemplate(ModuleName chamId);

		[OperationContract]
		string GetRecipeByBarcode(ModuleName chamId, string barcode);

		[OperationContract]
		string GetXmlSequenceList(ModuleName chamId);

		[OperationContract]
		string GetSequence(string sequenceName);

		[OperationContract]
		List<string> GetSequenceNameList();

		[OperationContract]
		bool DeleteSequence(string sequenceName);

		[OperationContract]
		bool SaveSequence(string sequenceName, string sequenceContent);

		[OperationContract]
		bool SaveAsSequence(string sequenceName, string sequenceContent);

		[OperationContract]
		bool RenameSequence(string oldName, string newName);

		[OperationContract]
		string GetSequenceFormatXml();

		[OperationContract]
		bool RenameSequenceFolder(string oldName, string newName);

		[OperationContract]
		bool CreateSequenceFolder(string folderName);

		[OperationContract]
		bool DeleteSequenceFolder(string folderName);

		[OperationContract]
		string LoadRecipeByPath(string pathName, string recipeName);

		[OperationContract]
		Tuple<string, string> LoadRunTimeRecipeInfoByPath(string pathName);

		[OperationContract]
		IEnumerable<string> GetRecipesByPath(string pathName, bool includeUsedRecipe);

		[OperationContract]
		string GetXmlRecipeListByPath(string pathName, bool includeUsedRecipe);

		[OperationContract]
		bool DeleteRecipeByPath(string pathName, string recipeName);

		[OperationContract]
		bool DeleteFolderByPath(string pathName, string folderName);

		[OperationContract]
		bool SaveAsRecipeByPath(string pathName, string recipeName, string recipeContent);

		[OperationContract]
		bool SaveRecipeByPath(string pathName, string recipeName, string recipeContent);

		[OperationContract]
		bool CreateFolderByPath(string pathName, string folderName);

		[OperationContract]
		bool RenameRecipeByPath(string pathName, string oldName, string newName);

		[OperationContract]
		bool RenameFolderByPath(string pathName, string oldName, string newName);

		[OperationContract]
		string GetRecipeFormatXmlByPath(string pathName);

		[OperationContract]
		string GetRecipeTemplateByPath(string pathName);

		[OperationContract]
		string GetRecipeByBarcodeByPath(string pathName, string barcode);
	}
}
