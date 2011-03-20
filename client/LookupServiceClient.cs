using System;
using System.Threading;
using System.Runtime.CompilerServices;

namespace client
{
	public class LookupServiceClient : comm.PadicalObject
	{
		ManualResetEvent oSignalEvent = new ManualResetEvent (false);
		
		public LookupServiceClient ()
		{
		}
		
		[MethodImpl(MethodImplOptions.Synchronized)]
		public void Lookup ()
		{
			//This thread will block here until the reset event is sent.
			Console.WriteLine ("Sent Lookup request");
			oSignalEvent.WaitOne();
			Console.WriteLine ("Releasing block");
			oSignalEvent.Reset ();
		}
		
		public void Receive ()
		{
			Console.WriteLine ("Response received");
			oSignalEvent.Set ();
		}
	}
}