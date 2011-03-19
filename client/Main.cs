using System;
using comm; 

namespace client
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Hello World!");
			// instantiate stack 
			// register service components with send-recv layer
			// run gui
			// start the client
			
			int port = Int32.Parse(args[1]);
			SendReceiveMiddleLayer sm = new SendReceiveMiddleLayer();
			sm.Start(port);
			
			if (port != 8081) {
				
				Message m = new Message();
				m.PushString("hej");
				sm.Send(m);
				
			}
		}
	}
}

