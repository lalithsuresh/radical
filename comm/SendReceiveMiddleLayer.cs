using System;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Channels;
using System.Runtime.Remoting.Channels.Tcp;

namespace comm
{
	public class SendReceiveMiddleLayer : PadicalObject
	{
		private PointToPointInterface m_transmitter; 
		private GroupMulticast m_groupMulticast; 
		
		public SendReceiveMiddleLayer ()
		{
			init();
		}
		
		private void init () 
		{
			//m_transmitter = new PerfectPointToPointSend(this);
			//m_groupMulticast = new GroupMulticast(m_transmitter);
			
		}
		
		public void Deliver (Message m) 
		{
			// this function needs synchronization
			Console.WriteLine("SendRecv got: {0}", Message.GetType(m));
		}
	}
}

