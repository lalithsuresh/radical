using System;

namespace comm
{
	public class PointToPointInterface : MarshalByRefObject
	{
		protected SendReceiveMiddleLayer m_demuxer; 
			
		public PointToPointInterface () 
		{
			// empty
		}
		
		public void Init (SendReceiveMiddleLayer demuxer) 
		{
			if (demuxer != null) 
				m_demuxer = demuxer;
		}
		
		virtual public void Deliver (Message m) {
			// deliver to incoming queue in SendRecvMiddleLayer
			m_demuxer.Deliver(m);
		}
	}
}

