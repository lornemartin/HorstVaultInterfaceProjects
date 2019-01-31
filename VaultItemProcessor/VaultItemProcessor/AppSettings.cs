using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Configuration;
using System.Xml.Serialization;
using System.Xml;



public static class AppSettings
{
    //public static readonly string SettingsFilePath = new FileInfo(Assembly.GetExecutingAssembly().Location).DirectoryName + @"\AppSettings.xml";
    public static readonly string SettingsFilePath = Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData) + @"\Vault Item Processor 2019\AppSettings.xml";

    #region Get setting
    public static object Get(string key)
    {
        var fiFile = new FileInfo(SettingsFilePath);
        if (!System.IO.File.Exists(SettingsFilePath) || fiFile.Length <= 3)
        { // UTF-8 preamble.
            throw new KeyNotFoundException("Key not found: " + key);
        }

        using (var reader = new XmlTextReader(SettingsFilePath))
        {
            var settings = (List<Setting>)new XmlSerializer(typeof(List<Setting>)).Deserialize(reader);
            var setting = settings.SingleOrDefault(s => s.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
            if (setting != null)
            {
                return setting.Value;
            }
            else
            {
                throw new KeyNotFoundException("Key not found: " + key);
            }
        }
    }
    #endregion

    #region Set setting
    public static void Set(string key, object value)
    {
        List<Setting> settings;

        // Get list.
        var fiFile = new FileInfo(SettingsFilePath);
        if (!System.IO.File.Exists(SettingsFilePath) || fiFile.Length <= 3)
        { // UTF-8 preamble.
            settings = new List<Setting>();
        }
        else
        {
            using (var reader = new XmlTextReader(SettingsFilePath))
            {
                settings = (List<Setting>)new XmlSerializer(typeof(List<Setting>)).Deserialize(reader);
            }
        }

        // Update item.
        var setting = settings.SingleOrDefault(s => s.Key.Equals(key, StringComparison.InvariantCultureIgnoreCase));
        if (setting != null)
        {
            setting.Value = value;
        }
        else
        {
            settings.Add(new Setting { Key = key, Value = value });
        }

        // Save list.
        using (var writer = new XmlTextWriter(SettingsFilePath, Encoding.UTF8))
        {
            writer.Formatting = Formatting.Indented;
            new XmlSerializer(typeof(List<Setting>)).Serialize(writer, settings);
        }
    }
    #endregion

    #region Setting helper class
    public class Setting
    {
        public string Key;
        public object Value;
    }
    #endregion
}

#region Exceptions
public class KeyNotFoundException : ApplicationException
{
    public KeyNotFoundException()
    {
    }

    public KeyNotFoundException(string message)
        : base(message)
    {
    }
}
#endregion
