using System;
using comm;
using common;
using System.Collections.Generic;

namespace client
{
	public class Client : PadicalObject
	{
		// puppet properties
		private bool m_isPuppetControlled;
		private bool m_isStressTestClient = false;
		public PuppetClientService m_puppetService;
		public PerfectPointToPointSend m_puppetPerfectPointToPointSend;
		public SendReceiveMiddleLayer m_puppetSendReceiveMiddleLayer;
		
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
		
		public string CurrentMasterServer {
			get;
			set;
		}
		
		public string PuppetMasterAddress {
			get;
			set;
		}
		
		public Client ()
		{			
		}
		
		public void RotateMaster ()
		{
			int i = ServerList.IndexOf (CurrentMasterServer);		
			i = i + 1;
			CurrentMasterServer = ServerList[i % ServerList.Count];
		}
		
		public void LoadConfig () 
		{
			LoadConfig (ConfigReader.GetConfigurationValue ("username"),
			            Int32.Parse (ConfigReader.GetConfigurationValue ("clientport")));
		}
		
		public void LoadConfig (string username, int port)
		{
			UserName = username;
			ClientPort = port;
			ServerList = new List<string> ();
			ServerList.Add (ConfigReader.GetConfigurationValue ("server1") + "/Radical");
			ServerList.Add (ConfigReader.GetConfigurationValue ("server2") + "/Radical");
			ServerList.Add (ConfigReader.GetConfigurationValue ("server3") + "/Radical");
			CurrentMasterServer = ServerList[0];
			string puppetControlled = ConfigReader.GetConfigurationValue ("puppetmaster");
			if (puppetControlled != null)
			{
				m_isPuppetControlled = true;
				PuppetMasterAddress = puppetControlled + "/Radical";
			}
			else 
			{
				m_isPuppetControlled = false;
			}
			
			if (!String.IsNullOrEmpty (ConfigReader.GetConfigurationValue ("stresstest")))
			{
				m_isStressTestClient = true;
			}
		}
		
		public void InitClient ()
		{
			// Communication Layer
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer();
			if (m_isStressTestClient) 
			{
				m_perfectPointToPointSend = new PerfectPointToPointSend (UserName);
			} 
			else
			{
				m_perfectPointToPointSend = new PerfectPointToPointSend();
			}
			
			m_perfectPointToPointSend.Start(m_sendReceiveMiddleLayer, ClientPort);
			m_sendReceiveMiddleLayer.SetPointToPointInterface(m_perfectPointToPointSend);
			
			DebugInfo ("Started communication services");	
			
			// Services
			//m_groupMulticast = new GroupMulticast ();
			m_calendarService = new CalendarServiceClient ();
			m_calendarService.SetClient (this);			
			DebugInfo ("Started calendar service");
			
			m_lookupService = new LookupServiceClient ();
			m_lookupService.SetClient (this);
			m_sendReceiveMiddleLayer.SetLookupCallback (m_lookupService.Lookup);			
			DebugInfo ("Started lookup service");
			
			m_sequenceNumberService = new SequenceNumberServiceClient (); 
			m_sequenceNumberService.SetClient (this);
			DebugInfo ("Started numbering service");
			
			m_connectionServiceClient = new ConnectionServiceClient ();
			m_connectionServiceClient.SetClient (this);
			DebugInfo ("Started connection service");
			
			if (m_isPuppetControlled) {
				DebugInfo ("Client may be controlled by Puppet Master");
				m_puppetSendReceiveMiddleLayer = new SendReceiveMiddleLayer ();
				m_puppetPerfectPointToPointSend = new PerfectPointToPointSend (true);
				m_puppetPerfectPointToPointSend.Start (m_puppetSendReceiveMiddleLayer, ClientPort+2000);
				m_puppetSendReceiveMiddleLayer.SetPointToPointInterface (m_puppetPerfectPointToPointSend);
				m_puppetSendReceiveMiddleLayer.SetLookupCallback (m_lookupService.Lookup);
					
				m_puppetService = new PuppetClientService ();
				m_puppetService.SetClient (this);
				m_puppetService.RegisterAsPuppet ();
				
				// Automatically connect when spawned
				Connect ();
				
				DebugInfo ("Started puppet service");
			}
			
		
			
			/*
			m_connectionServiceClient.Connect ();			
			System.Threading.Thread.Sleep (3000);
			DebugUncond ("I can haz sequence number... {0}", GetSequenceNumber ());
			
			
			System.Threading.Thread.Sleep (30000);
			m_connectionServiceClient.Disconnect ();
			
			DebugUncond ("lookup() returned this: {0}", m_lookupService.Lookup ("testclient1"));
			
			System.Threading.Thread.Sleep (3000);
			
			m_connectionServiceClient.Disconnect ();
			*/
			/*
			System.Threading.Thread testthread = new System.Threading.Thread (bleh);
			testthread.Start ();
			string lookupresonse = m_lookupService.Lookup ("user1");
			
			DebugUncond ("Received lookupresponse {0}", lookupresonse);*/
			
			//Console.WriteLine ("My username {0}", UserName);
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
		
		public string ReadCalendar () 
		{
			return m_calendarService.ReadCalendar ();
		}
		
		public void Stop ()
		{
			m_perfectPointToPointSend.Stop ();
		}
	}
}

