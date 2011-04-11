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
		private Dictionary<string, bool> m_serverStatuses;
		
		// buffers
		private bool m_serverPingStatus = false;
		private List<string> m_sentReplicationTo;
		
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
			m_serverStatuses = new Dictionary<string, bool> ();
			m_sentReplicationTo = new List<string> ();
		}
		
		public void SetServer (Server server) 
		{
			if (server != null) 
			{
				m_server = server;
			}
			
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback("replicate",
			                                                          new ReceiveCallbackType(Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback("ping",
			                                                          new ReceiveCallbackType(Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback("ping_ack",
			                                                          new ReceiveCallbackType(Receive));
		}
		
		public void Start () 
		{
			// wait a little for other servers to start
			DebugInfo ("Waiting for servers to start...");
			Thread.Sleep (1000);
			
			// check if other servers are up
			GetStatusForAllServers ();
			
			if (!AreAllServersUp()) 
			{
				DebugFatal ("Server not able to start. Not all servers are up.");
			}
			
			// determine who is master
			CurrentMaster = "server1";
			
			// if I not master 
			//	synchronize	
			
			DebugInfo ("Replication Service activated. Master is: {0}", CurrentMaster);
		}
		
		public void ReplicateSequenceNumber (long number) 
		{
			lock (this) 
			{
				if (IsMaster) 
				{					
					// send to all 		
					foreach (var server in m_server.ServerList) {
						DebugInfo ("Sending sequence number ({0}) replication request to {1}",
						           number, server);
						Message m = new Message ();
						m.SetMessageType ("replicate");
						m.SetDestinationUsers (server);
						m.SetSourceUserName (m_server.UserName);
						m.PushString (number.ToString ());
						m.PushString ("sequencenumber"); // subtyped message	
						m_server.m_sendReceiveMiddleLayer.Send (m);
						m_sentReplicationTo.Add (server);
					}
					
					// wait N-1 acks (unsure this works properly, might lead to race conditions)
					Block ();
					
					// return (synchronous call)
				}
			}
		}
		
		public bool Ping (string server) 
		{
			Message m = new Message ();
			m.SetMessageType ("ping");
			m.SetDestinationUsers (server);
			m.SetSourceUserName (m_server.UserName);
			m_server.m_sendReceiveMiddleLayer.Send (m);
			
			Block ();
			
			return m_serverPingStatus;
		}
		
		public void Receive (ReceiveMessageEventArgs e) 
		{
			Message m = e.m_message;
			
			if (m.Equals ("ping")) 
			{
				DebugLogic ("Got ping from {0}", m.GetSourceUserName ());
				Message ack = new Message ();
				ack.SetMessageType ("ping_ack");
				ack.SetDestinationUsers (m.GetSourceUserName ());
				ack.SetSourceUserName (m_server.UserName);
				m_server.m_sendReceiveMiddleLayer.Send (ack);
			} 
			else if (m.Equals ("ping_ack")) 
			{
				DebugLogic ("Got ping ack from {0}", m.GetSourceUserName ());
				m_serverPingStatus = true;
				m_oSignalEvent.Set ();
			}
			else if (m.Equals ("replicate")) 
			{
				string subtype = m.PopString ();
				if (subtype.Equals ("sequencenumber")) 
				{
					DebugLogic ("Got sequence number replication request from {0}", m.GetSourceUserName ());
					HandleSequenceNumberReplication (m.PopString (), m.GetSourceUserName ());					
				}
				else if (subtype.Equals ("sequencenumber_ack")) 
				{
					m_sentReplicationTo.Remove (m.GetSourceUserName ());
					if (m_sentReplicationTo.Count == 0) // TODO: requires all to reply... not optimal :)
					{
						// TODO: Improve to check that we get acks for the replication request 
						DebugLogic ("Got acks for replication request (sequencenumber)");
						// got all acks, safe to reset block
						m_oSignalEvent.Set ();
					}
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
		
		private bool AreAllServersUp () 
		{
			foreach (bool serverStatus in m_serverStatuses.Values) 
			{
				if (!serverStatus) 
				{
					return false;
				}
			}
			
			DebugInfo ("All servers are up");
			
			return true;
		}
		
		private void GetStatusForAllServers () 
		{
			// TODO: make nicer
			m_serverStatuses.Add ("server1", Ping ("server1"));
			m_serverStatuses.Add ("server2", Ping ("server2"));
			m_serverStatuses.Add ("server3", Ping ("server3"));
		}
		
		private void Block ()
		{
			//This thread will block here until the reset event is sent.
			m_oSignalEvent.WaitOne();
			m_oSignalEvent.Reset ();
		}
	}
}
