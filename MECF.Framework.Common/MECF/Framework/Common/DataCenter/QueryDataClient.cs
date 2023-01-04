using Aitex.Core.Util;

namespace MECF.Framework.Common.DataCenter
{
	public class QueryDataClient : Singleton<QueryDataClient>
	{
		private IQueryDataService _service;

		public bool InProcess { get; set; }

		public IQueryDataService Service
		{
			get
			{
				if (_service == null)
				{
					if (InProcess)
					{
						_service = new QueryDataService();
					}
					else
					{
						_service = new QueryDataServiceClient();
					}
				}
				return _service;
			}
		}
	}
}
