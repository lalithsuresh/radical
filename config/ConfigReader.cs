using System;
using System.Collections.Generic;
using System.IO;

namespace config
{
	public class ConfigReader
	{
		private static Dictionary<string, string> m_configurationItems = m_configurationItems = new Dictionary<string, string>(); 
		
		public ConfigReader ()
		{
		}
		
		public static string GetConfigurationValue (string key) 
		{
			if (m_configurationItems.ContainsKey(key))
				return m_configurationItems[key];
			
			return null;
		}
		
		public static bool ReadFile (string filename) 
		{
			if (File.Exists (filename)) 
			{
				using (StreamReader reader = new StreamReader(filename))
				{
					string line = String.Empty;
					while ((line = reader.ReadLine()) != null) 
					{
						if ((line.Trim().Length != 0) && (String.Compare(line.Substring (0,1), "#") != 0))
						{
							string[] keyval = line.Split(' ');
							m_configurationItems.Add(keyval[0], keyval[1]);
						}
					}
				}
			}
			else 
			{
				Console.WriteLine ("No configuration file found. Exiting.");	
				return false; 
			}
			
			return true; 
		}
	}
}

