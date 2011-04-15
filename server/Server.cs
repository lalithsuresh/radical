using System;
using comm;
using common;
using System.Collections.Generic;

namespace server
{
	public class Server : PadicalObject
	{		
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
			
			DebugUncond ("Started. Available commands: \"exit\", \"status\"");
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

