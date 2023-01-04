namespace MECF.Framework.Common.RecipeCenter
{
	public interface ISequenceFileContext
	{
		string GetConfigXml();

		bool Validation(string content);
	}
}
