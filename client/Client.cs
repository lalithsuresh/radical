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
		}
		
		public void LoadConfig (string config)
		{
			
			if (!m_configReader.ReadFile (config))
				DebugFatal ("Can't read config file {0}", config);
			
			UserName = m_configReader.GetConfigurationValue ("username");
			ClientPort = Int32.Parse (m_configReader.GetConfigurationValue ("clientport"));
			ServerList = new List<string> ();
			ServerList.Add (m_configReader.GetConfigurationValue ("server1") + "/Radical");
			ServerList.Add (m_configReader.GetConfigurationValue ("server2") + "/Radical");
			ServerList.Add (m_configReader.GetConfigurationValue ("server3") + "/Radical");
		}
		
		public void InitClient ()
		{
			// Communication Layer
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer();
			m_perfectPointToPointSend = new PerfectPointToPointSend();
			
			m_perfectPointToPointSend.Start(m_sendReceiveMiddleLayer, ClientPort);
			m_sendReceiveMiddleLayer.SetPointToPointInterface(m_perfectPointToPointSend);
			
			// Services
			//m_groupMulticast = new GroupMulticast ();
			m_calendarService = new CalendarServiceClient ();
			m_calendarService.SetClient (this);
			
			m_lookupService = new LookupServiceClient ();
			m_lookupService.SetClient (this);
			m_sendReceiveMiddleLayer.SetLookupCallback (m_lookupService.Lookup);
			
			m_sequenceNumberService = new SequenceNumberServiceClient (); 
			m_sequenceNumberService.SetClient (this);
			m_connectionServiceClient = new ConnectionServiceClient ();
			m_connectionServiceClient.SetClient (this);
			
			/*
			m_connectionServiceClient.Connect ();
			
			System.Threading.Thread.Sleep (3000);
			
			DebugUncond ("lookup() returned this: {0}", m_lookupService.Lookup ("testclient1"));
			
			System.Threading.Thread.Sleep (3000);
			
			m_connectionServiceClient.Disconnect ();
			*/
			/*
			System.Threading.Thread testthread = new System.Threading.Thread (bleh);
			testthread.Start ();
			string lookupresonse = m_lookupService.Lookup ("user1");
			
			DebugUncond ("Received lookupresponse {0}", lookupresonse);*/
		}
		
		// All client side APIs are listed belowS
		
		public bool Connect () 
		{
			return m_connectionServiceClient.Connect ();
		}
		
		public bool Disconnect () 
		{
			return m_connectionServiceClient.Disconnect ();
		}
		
		public int GetSequenceNumber ()
		{
			return m_sequenceNumberService.RequestSequenceNumber ();
		}
		
		public void Reserve (string description, List<string> userlist, List<int> slotlist)
		{
			// TODO: Don't forget to do Sanity checks
			m_calendarService.Reserve (description, userlist, slotlist);
		}
	}
}

