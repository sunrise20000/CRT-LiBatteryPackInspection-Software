using Aitex.Core.Util;
using MECF.Framework.Common.IOCore;

namespace Aitex.Core.RT.IOCore
{
	public class Index<T> where T : class
	{
		public T this[string name] => Singleton<IoManager>.Instance.GetIO<T>(name);
	}
}
