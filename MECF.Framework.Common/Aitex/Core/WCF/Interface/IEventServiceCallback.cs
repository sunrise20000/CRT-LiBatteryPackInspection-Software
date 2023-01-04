using System.ServiceModel;
using Aitex.Core.RT.Event;

namespace Aitex.Core.WCF.Interface
{
	[ServiceContract]
	public interface IEventServiceCallback
	{
		[OperationContract(IsOneWay = true)]
		void SendEvent(EventItem ev);
	}
}
