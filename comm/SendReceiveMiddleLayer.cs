using System;
using System.Collections.Generic;
namespace comm
{
	public delegate void ReceiveCallbackType(object sender, EventArgs e);
	
	public class SendReceiveMiddleLayer : PadicalObject
	{
		private Dictionary<string, ReceiveCallbackType> m_registerMap = new Dictionary<string, ReceiveCallbackType> ();
		
		public SendReceiveMiddleLayer ()
		{
		}
		
		public void Send () 
		{
		}
		
		public void RegisterReceiveCallback (String service, ReceiveCallbackType cb)
		{
			m_registerMap.Add (service, cb);
		}
	}
}