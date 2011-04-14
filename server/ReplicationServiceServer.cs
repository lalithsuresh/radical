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
		private List<string> m_replicationList;
		private PingTimer m_pingTimer;
		private Thread m_pingTimerThread;
		private Dictionary<string, bool> m_serverStatus;
		private Dictionary<string, string> m_serverUriToServerNameMap;
		
		// buffer
		private string m_masterReplyBuffer;
				
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
			m_serverStatus = new Dictionary<string, bool> ();
			m_serverUriToServerNameMap = new Dictionary<string, string> ();
			m_replicationList = new List<string> ();
		}
		
		public void SetServer (Server server) 
		{
			if (server != null) 
			{
				m_server = server;
			}
			
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("replicate",
			                                                           new ReceiveCallbackType(Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("ping",
			                                                           new ReceiveCallbackType(Receive));
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("whoismaster",
			                                                           new ReceiveCallbackType(Receive));						
			m_server.m_sendReceiveMiddleLayer.RegisterReceiveCallback ("whoismaster_ack",
			                                                           new ReceiveCallbackType(Receive));
			
			// TODO: This map should be done elsewhere and less statically
			m_serverUriToServerNameMap.Add (m_server.ServerList[0], "server1");			
			m_serverUriToServerNameMap.Add (m_server.ServerList[1], "server2");
			m_serverUriToServerNameMap.Add (m_server.ServerList[2], "server3");
		}
		
		public void Start () 
		{			
			// Build replication list
			StartPingService();
			
			// Sleep for a while to see if there are other servers alive
			Thread.Sleep (m_pingTimer.Interval*2);
			
			// determine who is master
			if (!MasterExists ())
			{
				ChooseNewMaster (null);
			} 
			else
			{
				DetermineExistingMaster ();
			}
			
			// I am alive
			UpdateServerStatus (m_server.UserName, true);
			
			if (!IsMaster) 
			{				
				DebugInfo ("Synchronizing with Master {0}", CurrentMaster);
			}
			
			DebugInfo ("[{0}] Replication Service activated. Master is: {1}", m_server.UserName, CurrentMaster);
		}
		
		public void Stop ()
		{
			m_pingTimer.Stop ();
		}
		
		public void ReplicateUserConnect (string username, string uri) 
		{
			if (IsMaster) 
			{
				DebugInfo ("Sending UserConnect replication request");
				Message m = new Message ();
				m.PushString (uri);
				m.PushString (username);
				m.PushString ("user_connect");
				DistributeReplicationMessage (m);
			}
		}
				
		public void ReplicateUserDisconnect (string username) 
		{
			if (IsMaster) 
			{
				DebugInfo ("Sending UserDisconnect replication request");
				Message m = new Message ();
				m.PushString (username);
				m.PushString ("user_disconnect");
				DistributeReplicationMessage (m);
			}			
		}
		
		public void ReplicateSequenceNumber (long number) 
		{
			if (IsMaster) 
			{
				DebugInfo ("Sending sequence number ({0}) replication request", number);			
				Message m = new Message ();
				m.PushString (number.ToString ());
				m.PushString ("sequencenumber"); // subtyped message								
				DistributeReplicationMessage (m);
			}
		}
		

		public void Receive (ReceiveMessageEventArgs e) 
		{							
			Message m = e.m_message;
			
			if (m.GetMessageType ().Equals ("replicate")) 
			{
				string subtype = m.PopString ();				
				
#region sequencenumber
				if (subtype.Equals ("sequencenumber")) 
				{
					DebugLogic ("Got sequence number replication request from {0}", m.GetSourceUserName ());
					HandleSequenceNumberReplication (m.PopString (), m.GetSourceUserName ());					
				}
				else if (subtype.Equals ("sequencenumber_ack")) 
				{							
					DebugLogic ("ACK replication request: SequenceNumber");
								
					m_oSignalEvent.Set ();					
				}
#endregion
#region user_connect
				else if (subtype.Equals ("user_connect"))
				{
					DebugLogic ("Got user_connect replication request for {0}", m.GetSourceUserName ());
					string username = m.PopString ();
					string uri = m.PopString ();
					HandleUserConnectReplication (username, uri, m.GetSourceUserName ());
				}
				else if (subtype.Equals ("user_connect_ack"))
				{
					DebugLogic ("ACK replication request: UserConnect");
					m_oSignalEvent.Set ();
				}
#endregion
#region user_disconnect
				else if (subtype.Equals ("user_disconnect"))
				{
					DebugLogic ("Got user_disconnect replication request for {0}", m.GetSourceUserName ());
					string username = m.PopString ();
					HandleUserDisconnectReplication (username, m.GetSourceUserName ());
				}
				else if (subtype.Equals ("user_disconnect_ack"))
				{
					DebugLogic ("ACK replication request: UserDisconnect");
					m_oSignalEvent.Set ();
				}
#endregion
			} 
			else if (m.GetMessageType ().Equals ("whoismaster"))
			{		
				string reply; 
				Message ack = new Message ();
				m.SetMessageType ("whoismaster_ack");
				m.SetDestinationUsers (m.GetSourceUserName ());
				m.SetSourceUserName (m_server.UserName);
				if (String.IsNullOrEmpty (CurrentMaster)) 
				{
					m.PushString ("empty");
					reply = "empty";
				}
				else 
				{
					m.PushString (CurrentMaster);
					reply = CurrentMaster;
				}
				m_server.m_sendReceiveMiddleLayer.Send (m);
				DebugInfo ("Got question: Who is master? Replying: {0}", reply);
			}
			else if (m.GetMessageType ().Equals ("whoismaster_ack"))
			{
				m_masterReplyBuffer = m.PopString ();
				m_oSignalEvent.Set ();
			}
		
		}
				
		
		private void DistributeReplicationMessage (Message m) 
		{
			lock (this) 
			{
				foreach (string replicationServer in m_replicationList)
				{		
					Message replMsg = (Message) m.Clone ();
					replMsg.SetMessageType ("replicate");
					replMsg.SetSourceUserName (m_server.UserName);
					replMsg.SetDestinationUsers (replicationServer);					
					m_server.m_sendReceiveMiddleLayer.Send (replMsg);
					Block ();
				}
			}
		}

		private void HandleUserConnectReplication (string username, string uri, string replyTo)
		{
			m_server.m_userTableService.UserConnect (username, uri);
			Message ack = new Message ();
			ack.SetMessageType ("replicate");
			ack.SetDestinationUsers (replyTo);
			ack.SetSourceUserName (m_server.UserName);
			ack.PushString (username);
			ack.PushString ("user_connect_ack");	
			m_server.m_sendReceiveMiddleLayer.Send (ack);
		}
		
		private void HandleUserDisconnectReplication (string username, string replyTo)
		{
			m_server.m_userTableService.UserDisconnect (username);
			Message ack = new Message ();
			ack.SetMessageType ("replicate");
			ack.SetDestinationUsers (replyTo);
			ack.SetSourceUserName (m_server.UserName);
			ack.PushString (username);
			ack.PushString ("user_disconnect_ack");	
			m_server.m_sendReceiveMiddleLayer.Send (ack);
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
		
		
		private void StartPingService () 
		{
			m_pingTimer = new PingTimer ();
			m_pingTimer.Interval = 1000;
			m_pingTimer.OnPing += Ping;			
			m_pingTimerThread = m_pingTimer.Start ();			
		}
		
		private void Ping ()
		{
			foreach (string server in m_server.ServerList)
			{
				string serverName = m_serverUriToServerNameMap[server];
				try 
				{
					Message m = new Message ();
					m.SetSourceUserName (m_server.UserName);
					m.SetMessageType ("ping");
					m.SetDestinationUsers (serverName);
					m_server.m_sendReceiveMiddleLayer.UnreliableSend (m, server);									
					
					// if server was previously unavailable
					//   add to replication list
					if (!m_serverStatus[serverName])
					{
						AddToReplicationList (serverName);
					}
					
					UpdateServerStatus (serverName, true);
				}
				catch (Exception)
				{
					DebugInfo ("Could not reach: {0}", server);
					
					// update serverStatus list
					UpdateServerStatus (serverName, false);		
					RemoveFromReplicationList (serverName);
										
					// if server is master, update current master
					if (serverName.Equals (CurrentMaster)) 
					{
						ChooseNewMaster (serverName);
						
						// if I became master..? 
						if (IsMaster) 
						{
							DebugInfo ("I am master");
						}
					} 
					
				}
			}
		}
		
		private void DetermineExistingMaster () 
		{
			foreach (string server in m_serverStatus.Keys)
			{
				if (m_serverStatus[server])
				{
					Message m = new Message ();
					m.SetMessageType ("whoismaster");
					m.SetDestinationUsers (server);
					m.SetSourceUserName (m_server.UserName);
					m_server.m_sendReceiveMiddleLayer.Send (m);
					Block ();
					if (!m_masterReplyBuffer.Equals ("empty"))
					{
						CurrentMaster = m_masterReplyBuffer;
						m_masterReplyBuffer = "empty";
						return;
					}
				}
			}
			
			DebugFatal ("Could not find existing master.");
			
			// for all servers alive
			// request master status
			// block until replies arrive
			// pick master
		}
		
		private bool MasterExists ()
		{
			foreach (bool status in m_serverStatus.Values) 
			{
				if (status)
					return true;
			}
			return false;
		}
		
		/*
		 * Deterministic but non-scalable way of electing a new master
		 */
		private void ChooseNewMaster (string oldMaster) 
		{
			if (String.IsNullOrEmpty (oldMaster))
			{
				// be sure...				
				if (!MasterExists ()) 
				{
					CurrentMaster = "server1";					
				}
				else 
				{
					DebugFatal ("Could not determine master with accuracy.");
				}
				return;
			}
			
			if (oldMaster.Equals ("server1")) 
			{
				CurrentMaster = "server2";		
			}
			else if (oldMaster.Equals ("server2"))
			{
				CurrentMaster = "server3";
			}
			else if (oldMaster.Equals ("server3"))
			{
				CurrentMaster = "server1";
			}		
			
			DebugInfo ("New master is {0}, old was {1}", CurrentMaster, oldMaster);
		}
		
		private void AddToReplicationList (string server) 
		{
			m_replicationList.Add (server);
		}
		
		private void RemoveFromReplicationList (string server) 
		{
			m_replicationList.Remove (server);
		}
		
		private void UpdateServerStatus (string server, bool status) 
		{
			if (m_serverStatus.ContainsKey (server))
			{
				m_serverStatus.Remove (server);
				m_serverStatus.Add (server, status);
			} 
			else
			{
				m_serverStatus.Add (server, status);
			}
		}
	}
	
	class PingTimer
	{
		public delegate void Ping ();
		
		public int Interval;
		public Ping OnPing;
		
		Thread m_pingThread;
		volatile bool m_stop;
		
		public void Run ()
		{
			while (!m_stop)
			{
				Thread.Sleep (Interval);
				OnPing ();
			}
		}
		
		public Thread Start () 
		{
			m_stop = false;
			m_pingThread = new Thread (new ThreadStart (Run));
			m_pingThread.IsBackground = true;
			m_pingThread.Start ();
			return m_pingThread;
		}
		
		public void Stop () 
		{
			m_stop = true;
			m_pingThread.Join (1000);
			m_pingThread.Abort ();
		}
	}

}
