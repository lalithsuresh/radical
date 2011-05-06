using System;
using System.Collections.Generic;
using System.Threading;
using comm;
using common;


namespace puppet
{

	public delegate void ReceiveNotificationsCallbackType(NotificationEventArgs e);
	
	public class PuppetMaster : PadicalObject
	{	
		// GUI listener
		private ReceiveNotificationsCallbackType m_notificationCallback;
		
		// Services Layer components		
		public PuppetMasterService m_puppetMasterService; 
		private ConnectionServicePuppet m_connectionService;
		private LookupServicePuppet m_lookupService;
		
		// Communication Layer components
		public SendReceiveMiddleLayer m_sendReceiveMiddleLayer; 
		private PerfectPointToPointSend m_perfectPointToPointSend;
		
		public int PuppetPort {
			get;
			set;
		}
		
		public string UserName {
			get;
			set;
		}
		
		public string GenericConfig {
			get;
			set;
		}
		
		public string ClientExecutable {
			get;
			set;
		}
		
		public string ServerExecutable {
			get;
			set;
		}
		
		public string ServerConfigFolder
		{
			get;
			set;
		}
		
		public List<string> ServerList {
			get;
			set;
		}
				
		public Queue<PuppetInstruction> InstructionSet {
			get;
			set;
		}
		
		public PuppetMaster ()
		{			
		}
		
		public void LoadConfig (string config) 
		{
			PuppetPort = Int32.Parse (ConfigReader.GetConfigurationValue ("puppetport"));
			UserName = ConfigReader.GetConfigurationValue ("username");
			GenericConfig = ConfigReader.GetConfigurationValue ("generic_config");
			ClientExecutable = ConfigReader.GetConfigurationValue ("client_executable");
			ServerExecutable = ConfigReader.GetConfigurationValue ("server_executable");
			ServerConfigFolder = ConfigReader.GetConfigurationValue ("server_config_folder");
			
			ServerList = new List<string> ();
			ServerList.Add (ConfigReader.GetConfigurationValue ("central-1") + "/Radical");
			ServerList.Add (ConfigReader.GetConfigurationValue ("central-2") + "/Radical");
			ServerList.Add (ConfigReader.GetConfigurationValue ("central-3") + "/Radical");
		}
		
		public void InitPuppetMaster() 
		{			
			// Communication Layer
			m_sendReceiveMiddleLayer = new SendReceiveMiddleLayer ();
			m_perfectPointToPointSend = new PerfectPointToPointSend ();
			
			m_perfectPointToPointSend.Start (m_sendReceiveMiddleLayer, PuppetPort);
			m_sendReceiveMiddleLayer.SetPointToPointInterface (m_perfectPointToPointSend);
			
			// Services Layer
			m_puppetMasterService = new PuppetMasterService();
			m_puppetMasterService.SetPuppetMaster (this);
			
			m_lookupService = new LookupServicePuppet ();
			m_lookupService.SetPuppetMaster (this);
			m_sendReceiveMiddleLayer.SetLookupCallback (m_lookupService.Lookup);
			
			m_connectionService = new ConnectionServicePuppet();
			m_connectionService.SetPuppetMaster (this);
			
			DebugInfo ("Started puppet master on {0}", PuppetPort);			
			//m_connectionService.Connect ();
			DebugInfo ("Puppet master registered with servers");
			
		}	
		
		private PuppetInstruction instruction;
		
		public void Step ()
		{
			if (InstructionSet.Count > 0)
			{				
				instruction = InstructionSet.Dequeue ();
				
				NotifySubscribers (String.Format ("{0} {1}", instruction.Type, instruction.ApplyToUser));
				
				if (instruction.Type == PuppetInstructionType.WAIT)
				{
					Thread.Sleep (Int32.Parse (instruction.ApplyToUser));
					return;
				} 
				else
				{					
					m_puppetMasterService.CommandClient (instruction);
				}					
			} 
			else 
			{
				NotifySubscribers ("Instruction queue is empty.");
			}
						
		}		
		
		public void Play () 
		{		
			while (InstructionSet.Count > 0)			
			{
				Step ();
				if (instruction.Type != PuppetInstructionType.RESERVATION &&
				    instruction.Type != PuppetInstructionType.READ_CALENDAR)
				{
					Thread.Sleep (1000);
				}
			}
		}
		
		public void Shutdown () 
		{
			DebugLogic ("Shutdown.");
			m_puppetMasterService.Cleanup ();
			m_perfectPointToPointSend.Stop ();
		}
		
		public void RegisterNotificationSubscriber (ReceiveNotificationsCallbackType cb)
		{
			m_notificationCallback += cb;
		}
		
		public void NotifySubscribers (string msg) 
		{
			NotifySubscribers (msg, null);
		}
		
		public void NotifySubscribers (string msg, string username) 
		{
			if (!String.IsNullOrEmpty (msg) || !String.IsNullOrEmpty (username)) 
			{
				m_notificationCallback (new NotificationEventArgs (msg, username));
			}
		}
	}
}
