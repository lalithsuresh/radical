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
		void SetCallback(Server s); 	// for outgoing messages
		void Receive(Message m); 		// for incoming messages
	}
}

