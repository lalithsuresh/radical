using System;
using comm;
using common;
using System.Collections.Generic;

namespace client
{
	public class Client : PadicalObject
	{
		// Eventually make these not-so-public
		public PerfectPointToPointSend m_perfectPointToPointSend;
		public SendReceiveMiddleLayer m_sendReceiveMiddleLayer;
		public GroupMulticast m_groupMulticast;
		public CalendarServiceClient m_calendarService;
		public LookupServiceClient m_lookupService;
		public SequenceNumberServiceClient m_sequenceNumberService;
		public ConnectionServiceClient m_connectionServiceClient;
		
		// Client properties.
		// C# naming conventions require us to abandon
		// our own naming conventions. -- Lalith x-(
		public string UserName {
			get;
			set;
		}
		
		public int ClientPort {
			get;
			set;
		}
		
		public List<string> ServerList {
			get;
			set;
		}
		
		public Client ()
		{
			LoadConfig ();
		}
		
		public void LoadConfig ()
		{
			UserName = ConfigReader.GetConfigurationValue ("username");
			ClientPort = Int32.Parse (ConfigReader.GetConfigurationValue ("clientport"));
			ServerList = new List<string> ();
			ServerList.Add (ConfigReader.GetConfigurationValue ("server1"));
			ServerList.Add (ConfigReader.GetConfigurationValue ("server2"));
			ServerList.Add (ConfigReader.GetConfigurationValue ("server3"));
		}
		
		public void InitClient ()
		{
			// Communication Layer
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer();
			m_perfectPointToPointSend = new PerfectPointToPointSend();
			
			m_perfectPointToPointSend.Start(m_sendReceiveMiddleLayer, 9090);
			m_sendReceiveMiddleLayer.SetPointToPointInterface(m_perfectPointToPointSend);
			
			// Services
			//m_groupMulticast = new GroupMulticast ();
			m_calendarService = new CalendarServiceClient ();
			m_lookupService = new LookupServiceClient ();
			m_lookupService.SetClient (this);
			m_sequenceNumberService = new SequenceNumberServiceClient (); 
			m_connectionServiceClient = new ConnectionServiceClient ();
			
			/*
			System.Threading.Thread testthread = new System.Threading.Thread (bleh);
			testthread.Start ();
			string lookupresonse = m_lookupService.Lookup ("user1");
			
			DebugUncond ("Received lookupresponse {0}", lookupresonse);*/
		}
		
		public void bleh ()
		{
			System.Threading.Thread.Sleep (2000);
			Message m = new Message ();
			m.SetSource ("SERVER");
			m.SetDestination (UserName);
			m.SetMessageType ("lookup");
			m.PushString ("uriforrequesteduser");
			m_lookupService.Receive (new ReceiveMessageEventArgs (m));
		}
		
	}
}

