using Aitex.Core.Util;

namespace MECF.Framework.Common.RecipeCenter
{
	public class RecipeClient : Singleton<RecipeClient>
	{
		private IRecipeService _service;

		public bool InProcess { get; set; }

		public IRecipeService Service
		{
			get
			{
				if (_service == null)
				{
					if (InProcess)
					{
						_service = new RecipeService();
					}
					else
					{
						_service = new RecipeServiceClient();
					}
				}
				return _service;
			}
		}
	}
}
