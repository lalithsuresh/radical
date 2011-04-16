using System;
using System.Collections.Generic;
using Gtk;

public partial class MainWindow : Gtk.Window
{

	// external
	private puppet.PuppetMaster m_puppetMaster;
	
	// gui elements
	protected Gtk.ListStore m_userStore;
	
	// data sources
	
	
	public MainWindow () : base(Gtk.WindowType.Toplevel)
	{
		Build ();
		
		// add column to tree
		treeViewUsers.AppendColumn ("Users", new Gtk.CellRendererText (), "text", 0);
		
		// set model for tree		
		m_userStore = new Gtk.ListStore(typeof (string));
		treeViewUsers.Model = m_userStore;
		this.ShowAll ();
	}
	
	public void SetPuppetMaster (puppet.PuppetMaster master) 
	{
		m_puppetMaster = master;
		
		m_puppetMaster.RegisterNotificationSubscriber (new puppet.ReceiveNotificationsCallbackType 
		                                               (NotificationUpdate));
	}
		
	protected void OnDeleteEvent (object sender, DeleteEventArgs a)
	{		
		Application.Quit ();
		a.RetVal = true;
	}
	
	protected virtual void OnButtonStepClicked (object sender, System.EventArgs e)
	{		
		
		m_puppetMaster.Step ();
	}
	
	protected virtual void OnButtonPlayClicked (object sender, System.EventArgs e)
	{
		m_puppetMaster.Play ();
	}
	
	public void NotificationUpdate (puppet.NotificationEventArgs msg) 
	{		
		Console.WriteLine ("MSG: {0}", msg.Notification);
		if (msg.UserName != null) 
		{
			if (msg.Notification.StartsWith ("REGISTERED")) 
			{
				m_userStore.AppendValues(msg.UserName);				
			}
			else if (msg.Notification.StartsWith ("DISCONNECT")) 
			{								
				// TODO: mark user disconnected
			}
		} 
		
		Gtk.Application.Invoke (delegate {
			TextIter iterator = textMain.Buffer.EndIter;
			textMain.Buffer.Insert(ref iterator, String.Format ("\n{0}", msg.Notification));
			textMain.ScrollToIter(textMain.Buffer.EndIter, 0, false, 0, 0);					
		});	
	}
	
	protected virtual void OnButtonExitClicked (object sender, System.EventArgs e)
	{
		Application.Quit ();
	}
		
}