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
			LoadConfig ();
		}
		
		public void LoadConfig ()
		{
			UserName = ConfigReader.GetConfigurationValue ("username");
			ServerPort = Int32.Parse (ConfigReader.GetConfigurationValue ("serverport"));
			ServerList = new List<string> ();
			ServerList.Add (ConfigReader.GetConfigurationValue ("server1"));
			ServerList.Add (ConfigReader.GetConfigurationValue ("server2"));
			ServerList.Add (ConfigReader.GetConfigurationValue ("server3"));
		}
		
		public void InitServer (int port) 
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

