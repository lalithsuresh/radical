using System;
using comm;
namespace server
{
	/**
	 * All service components implements this interface such 
	 * that there are always callback functions available 
	 * for the SendReceiveMiddleLayer (up and down)
	 */
	public interface IServiceServer
	{
		void SetServer (Server s); 	// for outgoing messages
		void Receive (ReceiveMessageEventArgs eventargs); 		// for incoming messages
	}
}

