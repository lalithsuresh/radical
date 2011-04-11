using System;
using System.Collections.Generic;
using System.Threading;
using comm;
using common; 

namespace server
{
	public class ReplicationServiceServer : PadicalObject, IServiceServer
	{
		ManualResetEvent m_oSignalEvent = new ManualResetEvent (false);
		
		// members
		private Server m_server;
		
		// buffers
		private List<string> m_pendingReplicationAcks;
		
		// properties
		public bool IsMaster {
			get 
			{
				return (CurrentMaster.Equals (m_server.UserName));
			}
		}
		
		public string CurrentMaster {
			get;
			set;
		}

		public ReplicationServiceServer ()
		{			
			m_pendingReplicationAcks = new List<string> ();
		}
		
		public void SetServer (Server server) 
		{
			if (server != null) 
			{
				m_server = server;
			}
			
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback("replicate",
			                                                          new ReceiveCallbackType(Receive));			
		}
		
		public void Start () 
		{
			// determine who is master
			CurrentMaster = "server1";
			
			if (!IsMaster) 
			{
				// TODO: make sure to test that master is reachable, otherwise rotate?
				DebugInfo ("I am not master");
			}
			
			DebugInfo ("[{0}] Replication Service activated. Master is: {1}", m_server.UserName, CurrentMaster);
		}
		
		public void ReplicateSequenceNumber (long number) 
		{			
			
			List<string> replicationList = new List<string> ();
			replicationList.Add ("server2");
			replicationList.Add ("server3");
			
			foreach (string replicationServer in replicationList) 
			{
				DebugInfo ("Sending sequence number ({0}) replication request to {1}", number, replicationServer);
				
				Message m = new Message ();
				m.SetMessageType ("replicate");
				m.SetDestinationUsers (replicationServer);
				m.SetSourceUserName (m_server.UserName);
				
				m.PushString (number.ToString ());
				m.PushString ("sequencenumber"); // subtyped message	
				m_server.m_sendReceiveMiddleLayer.Send (m);
				
				m_pendingReplicationAcks.Add (replicationServer);
				Block ();
			}
		}
		

		public void Receive (ReceiveMessageEventArgs e) 
		{							
			Message m = e.m_message;
			
			if (m.GetMessageType ().Equals ("replicate")) 
			{
				string subtype = m.PopString ();				
				
				if (subtype.Equals ("sequencenumber")) 
				{
					DebugLogic ("Got sequence number replication request from {0}", m.GetSourceUserName ());
					HandleSequenceNumberReplication (m.PopString (), m.GetSourceUserName ());					
				}
				else if (subtype.Equals ("sequencenumber_ack")) 
				{					
					// TODO: Improve to check that we get acks for the replication request 					
					DebugLogic ("Got ack for replication request (sequencenumber)");
					m_pendingReplicationAcks.Remove (m.GetSourceUserName ());
					
					m_oSignalEvent.Set ();					
				}
			}
		
		}
		
		private void HandleSequenceNumberReplication (string number, string replyTo) 
		{			
			m_server.m_sequenceNumberService.SetSequenceNumber (Int32.Parse (number));				
			Message ack = new Message ();
			ack.SetMessageType ("replicate");
			ack.SetDestinationUsers (replyTo);
			ack.SetSourceUserName (m_server.UserName);
			ack.PushString (number);
			ack.PushString ("sequencenumber_ack");	
			m_server.m_sendReceiveMiddleLayer.Send (ack);			
		}
		

		private void Block ()
		{			
			//This thread will block here until the reset event is sent.
			m_oSignalEvent.WaitOne();
			m_oSignalEvent.Reset ();
		}
	}
}
