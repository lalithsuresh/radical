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
			ServerList = new List<string> ();
			ServerList.Add (ConfigReader.GetConfigurationValue ("server1") + "/Radical");
			ServerList.Add (ConfigReader.GetConfigurationValue ("server2") + "/Radical");
			ServerList.Add (ConfigReader.GetConfigurationValue ("server3") + "/Radical");
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
			m_connectionService.Connect ();
			DebugInfo ("Puppet master registered with servers");
			
		}	
		
		public void Step ()
		{
			if (InstructionSet.Count > 0)
			{
				PuppetInstruction instruction = InstructionSet.Dequeue ();
				NotifySubscribers (String.Format ("{0} {1}", instruction.Type, instruction.ApplyToUser));
				m_puppetMasterService.CommandClient (instruction);
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
				Thread.Sleep (1000);
			}
		}
		
		public void Shutdown () 
		{
			DebugLogic ("Shutdown.");
			m_perfectPointToPointSend.Stop ();
		}
		
		public void RegisterNotificationSubscriber (ReceiveNotificationsCallbackType cb)
		{
			m_notificationCallback = cb;
		}
		
		public void NotifySubscribers (string msg) 
		{
			if (!String.IsNullOrEmpty (msg)) 
			{
				m_notificationCallback(new NotificationEventArgs (msg));
			}
		}
	}
}
