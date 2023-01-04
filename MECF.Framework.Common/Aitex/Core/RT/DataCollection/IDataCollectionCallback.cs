namespace Aitex.Core.RT.DataCollection
{
	public interface IDataCollectionCallback
	{
		void PostDBFailedEvent();

		string GetDBName();

		string GetSqlUpdateFile();

		string GetDataTablePrefix();
	}
}
