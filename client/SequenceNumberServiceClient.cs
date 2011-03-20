using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace client
{
	public class SequenceNumberServiceClient : comm.PadicalObject
	{
		ManualResetEvent oSignalEvent = new ManualResetEvent (false);
		
		public SequenceNumberServiceClient ()
		{
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void RequestSequenceNumber ()
		{
			//This thread will block here until the reset event is sent.
			oSignalEvent.WaitOne();
			oSignalEvent.Reset ();
		}
		
		public void Receive ()
		{
			oSignalEvent.Set ();
		}
	}
}

