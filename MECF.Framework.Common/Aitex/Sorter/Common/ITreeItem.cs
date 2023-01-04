namespace Aitex.Sorter.Common
{
	public interface ITreeItem<T> where T : ITreeItem<T>, new()
	{
		string ID { get; set; }
	}
}
