using System;
using comm;
using common;
using System.Collections.Generic;

namespace server
{
	public class Server : PadicalObject
	{		
		// Puppet control
		private bool m_isPuppetControlled; 
		public PuppetServerService m_puppetService;
		public PerfectPointToPointSend m_puppetPerfectPointToPointSend;
		public SendReceiveMiddleLayer m_puppetSendReceiveMiddleLayer;
		
		// Services Layer components
		public UserTableServiceServer m_userTableService; 
		public SequenceNumberServiceServer m_sequenceNumberService;
		public ReplicationServiceServer m_replicationService;
		
		// Communication Layer components
		public SendReceiveMiddleLayer m_sendReceiveMiddleLayer; 
		private PerfectPointToPointSend m_perfectPointToPointSend;
		
		public string UserName {
			get;
			set;
		}
		
		public int ServerPort {
			get;
			set;
		}
		
		public List<string> ServerList {
			get;
			set;
		}
		
		public string PuppetMasterAddress {
			get;
			set;
		}
		
		public Server ()
		{
		}
		
		public void LoadConfig (string config)
		{
			if (!ConfigReader.ReadFile (config))
				DebugFatal ("Can't read config file {0}", config);
			
			UserName = ConfigReader.GetConfigurationValue ("username");
			ServerPort = Int32.Parse (ConfigReader.GetConfigurationValue ("serverport"));
			ServerList = new List<string> ();
			ServerList.Add (ConfigReader.GetConfigurationValue ("server1") + "/Radical");
			ServerList.Add (ConfigReader.GetConfigurationValue ("server2") + "/Radical");
			ServerList.Add (ConfigReader.GetConfigurationValue ("server3") + "/Radical");
			
			string puppetMaster = ConfigReader.GetConfigurationValue ("puppetmaster");
			if (puppetMaster != null)
			{
				m_isPuppetControlled = true;
				PuppetMasterAddress = puppetMaster + "/Radical";
			}
		}
		
		public void InitServer () 
		{
			// Communication Layer
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer ();
			m_perfectPointToPointSend = new PerfectPointToPointSend ();
			
			m_perfectPointToPointSend.Start (m_sendReceiveMiddleLayer, ServerPort);
			m_sendReceiveMiddleLayer.SetPointToPointInterface (m_perfectPointToPointSend);
			
			// Services Layer
			m_userTableService = new UserTableServiceServer ();
			m_userTableService.SetServer (this);
			
			m_sendReceiveMiddleLayer.SetLookupCallback (m_userTableService.Lookup);
			
			m_sequenceNumberService = new SequenceNumberServiceServer ();
			m_sequenceNumberService.SetServer (this);
			
			m_replicationService = new ReplicationServiceServer ();
			m_replicationService.SetServer (this);
			
			// Register servers
			m_userTableService.UserConnect ("server1", ServerList [0]);	
			m_userTableService.UserConnect ("server2", ServerList [1]);	
			m_userTableService.UserConnect ("server3", ServerList [2]);	
			
			m_replicationService.Start ();	
			
			if (m_isPuppetControlled)
			{
				m_puppetSendReceiveMiddleLayer = new SendReceiveMiddleLayer ();
				m_puppetPerfectPointToPointSend = new PerfectPointToPointSend (true);
				m_puppetPerfectPointToPointSend.Start (m_puppetSendReceiveMiddleLayer, ServerPort-100);
				m_puppetSendReceiveMiddleLayer.SetPointToPointInterface (m_puppetPerfectPointToPointSend);
				m_puppetSendReceiveMiddleLayer.SetLookupCallback (m_userTableService.Lookup);
					
				m_puppetService = new PuppetServerService ();
				m_puppetService.SetServer (this);
				m_puppetService.RegisterAsPuppet ();			
				
				m_puppetService.SendInfoMsgToPuppetMaster ("Ready to serve");
			}
			
		}
		
		public void Start ()
		{
			m_replicationService.Start ();
			DebugUncond ("Started. Available commands: \"exit\", \"status\"");			
		}
		
		public void Pause ()
		{
			m_replicationService.Stop ();
			m_perfectPointToPointSend.Pause ();
		}				
		
		public void Unpause ()
		{
			m_perfectPointToPointSend.Unpause ();
			m_replicationService.Start ();
		}
		
		/**
		 * Relays messages from services layer to communication layer
		 */ 
		public void Send (Message m) 
		{
			m_sendReceiveMiddleLayer.Send (m);
		}
		
		public void Shutdown () 
		{			
			m_replicationService.Stop ();
			m_perfectPointToPointSend.Stop ();	
			DebugLogic ("Shutdown complete");
			Environment.Exit(1);
		}
	}
}

