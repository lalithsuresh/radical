using System;
using comm;
using common;
using System.Collections.Generic;

namespace server
{
	public class Server : PadicalObject
	{		
		// Services Layer components
		private UserTableServiceServer m_userTableService; 
		private SequenceNumberServiceServer m_sequenceNumberService;
		
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
			if (!m_configReader.ReadFile (config))
				DebugFatal ("Can't read config file {0}", config);
			
			UserName = m_configReader.GetConfigurationValue ("username");
			ServerPort = Int32.Parse (m_configReader.GetConfigurationValue ("serverport"));
			ServerList = new List<string> ();
			ServerList.Add (m_configReader.GetConfigurationValue ("server1"));
			ServerList.Add (m_configReader.GetConfigurationValue ("server2"));
			ServerList.Add (m_configReader.GetConfigurationValue ("server3"));
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
			
			// TODO: Determine current master
			// (now it's only me) 
			m_userTableService.UserConnect ("server1", ServerList [0]);
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
			DebugLogic ("Shutdown.");
			m_perfectPointToPointSend.Stop ();	
			Environment.Exit(1);
		}
	}
}

