using System;
using Gtk;

public partial class MainWindow : Gtk.Window
{

	private puppet.PuppetMaster m_puppetMaster;
	
	public MainWindow () : base(Gtk.WindowType.Toplevel)
	{
		Build ();
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
		textMain.Buffer.Text = "running...";
		
		m_puppetMaster.Play ();
	}
	
	public void NotificationUpdate (puppet.NotificationEventArgs msg) 
	{
		textMain.Buffer.Text = msg.Notification;
	}
	
	protected virtual void OnButtonExitClicked (object sender, System.EventArgs e)
	{
		m_puppetMaster.Shutdown ();
		Application.Quit ();	
	}
	
	
	
	
}

