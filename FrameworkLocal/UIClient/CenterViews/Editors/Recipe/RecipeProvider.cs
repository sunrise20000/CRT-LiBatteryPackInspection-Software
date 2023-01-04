using MECF.Framework.Common.Equipment;
using MECF.Framework.Common.RecipeCenter;
using System.Collections.Generic;
using System.Linq;

namespace MECF.Framework.UI.Client.CenterViews.Editors.Recipe
{
    public class RecipeProvider
    {
        public List<string> GetRecipes(string path)
        {
            return RecipeClient.Instance.Service.GetRecipesByPath(path, false).ToList();
        }

        public string GetXmlRecipeList(string path)
        {
            return RecipeClient.Instance.Service.GetXmlRecipeListByPath(path, false);
        }

        public List<string> GetRecipeLists()
        {
            return RecipeClient.Instance.Service.GetRecipes(ModuleName.System, true).ToList();
        }

        public string GetRecipeFormatXml(string path)
        {
            return RecipeClient.Instance.Service.GetRecipeFormatXmlByPath(path);
        }

        public bool WriteRecipeFile(string recipeName, string recipeContent)
        {
            return RecipeClient.Instance.Service.SaveRecipe(ModuleName.System, recipeName, recipeContent);
        }

        public bool WriteRecipeFile(string prefix, string recipeName, string recipeContent)
        {
            return RecipeClient.Instance.Service.SaveRecipeByPath(prefix, recipeName, recipeContent);
        }

 
        public string ReadRecipeFile(string prefix, string recipeName)
        {
            return RecipeClient.Instance.Service.LoadRecipeByPath(prefix, recipeName);
        }

        public bool CreateRecipeFolder(string prefix, string foldername)
        {
            return RecipeClient.Instance.Service.CreateFolderByPath(prefix, foldername);
        }

        public bool DeleteRecipe(string prefix, string name)
        {
            return RecipeClient.Instance.Service.DeleteRecipeByPath(prefix, name);
        }

        public bool DeleteRecipeFolder(string prefix, string foldername)
        {
            return RecipeClient.Instance.Service.DeleteFolderByPath(prefix, foldername);
        }

        public bool SaveAsRecipe(string prefix, string recipeName, string recipeContent)
        {
            return RecipeClient.Instance.Service.SaveAsRecipeByPath(prefix, recipeName, recipeContent);
        }

        public bool RenameRecipe(string prefix, string beforename, string currentname)
        {
            return RecipeClient.Instance.Service.RenameRecipeByPath(prefix, beforename, currentname);
        }

        public bool RenameFolder(string prefix, string beforename, string currentname)
        {
            return RecipeClient.Instance.Service.RenameFolderByPath(prefix, beforename, currentname);
        }
    }
}
