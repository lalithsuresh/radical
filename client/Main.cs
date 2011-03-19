using System;
using comm; 

namespace client
{
	class MainClass
	{
		public static void Main (string[] args)
		{
			Console.WriteLine ("Client on {0}", args[0]);
			// instantiate stack 
			// register service components with send-recv layer
			// run gui
			// start the client
			
			int port = Int32.Parse(args[0]);
			SendReceiveMiddleLayer sm = new SendReceiveMiddleLayer();
			sm.Start(port);
			
			if (port != 8081) {
				Console.WriteLine("I'm not 8081, so I will try to send a message to 8081");
				
				Message m = Message.Create("test");
				m.PushString("hej");
				
				sm.Send(m);
				
				Console.WriteLine("sent a message");
				
			}
			
			Console.ReadLine();
		}
	}
}

