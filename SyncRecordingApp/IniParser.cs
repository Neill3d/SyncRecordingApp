using System;
using System.Collections;
using System.IO;

namespace SyncRecordingApp
{
    
    public class IniParser
    {
        private const string BOOL_VALUE_YES = "Yes";
        private const string BOOL_VALUE_NO = "No";

        private struct SectionPair
        {
            public string section;
            public string key;
        }

        private Hashtable keyPairs = new Hashtable();
        private string iniFilePath;

        public bool isEmpty => keyPairs.Count == 0;

        /// <summary>
        /// Opens the INI file at the given path and enumerates the values in the IniParser.
        /// </summary>
        /// <param name="iniPath">Full path to INI file.</param>
        public IniParser(string iniPath)
        {
            TextReader iniFile = null;
            string strLine = null;
            string currentRoot = null;
            string[] keyPair = null;

            iniFilePath = iniPath;

            if (!File.Exists(iniPath))
            {
                Console.WriteLine("Unable to locate config file " + iniPath);
                return;
            }
             
            try
            {
                iniFile = new StreamReader(iniPath);

                while ((strLine = iniFile.ReadLine()) != null)
                {
                    strLine = strLine.Trim();
                    if (strLine.Length == 0)
                        continue;
                    
                    if (strLine.StartsWith("[") && strLine.EndsWith("]"))
                    {
                        currentRoot = strLine.Substring(1, strLine.Length - 2);
                    }
                    else
                    {
                        keyPair = strLine.Split(new char[] { '=' }, 2);
                        
                        if (currentRoot == null)
                            currentRoot = "ROOT";

                        SectionPair sectionPair = new SectionPair() { section = currentRoot, key = keyPair[0] };

                        string value = (keyPair.Length > 1) ? keyPair[1] : null;
                        keyPairs.Add(sectionPair, value);
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                iniFile?.Close();
            }
        }

        /// <summary>
        /// Returns the value for the given section, key pair.
        /// </summary>
        /// <param name="sectionName">Section name.</param>
        /// <param name="settingName">Key name.</param>
        public string GetSetting(string sectionName, string settingName, string defaultValue)
        {
            SectionPair sectionPair = new SectionPair() { section = sectionName, key = settingName };
            
            if (keyPairs.ContainsKey(sectionPair))
                return (string)keyPairs[sectionPair];

            return defaultValue;
        }

        public bool GetSetting(string sectionName, string settingName, bool defaultValue)
        {
            SectionPair sectionPair = new SectionPair() { section = sectionName, key = settingName };

            if (keyPairs.ContainsKey(sectionPair))
                return ((string)keyPairs[sectionPair] == BOOL_VALUE_YES);

            return defaultValue;
        }

        public int GetSetting(string sectionName, string settingName, int defaultValue)
        {
            SectionPair sectionPair = new SectionPair() { section = sectionName, key = settingName };

            if (keyPairs.ContainsKey(sectionPair))
                return int.Parse((string)keyPairs[sectionPair]);

            return defaultValue;
        }

        /// <summary>
        /// Enumerates all lines for given section.
        /// </summary>
        /// <param name="sectionName">Section to enum.</param>
        public string[] EnumSection(string sectionName)
        {
            ArrayList tmpArray = new ArrayList();

            foreach (SectionPair pair in keyPairs.Keys)
            {
                if (pair.section == sectionName)
                    tmpArray.Add(pair.key);
            }

            return (string[])tmpArray.ToArray(typeof(string));
        }

        /// <summary>
        /// Adds or replaces a setting to the table to be saved.
        /// </summary>
        /// <param name="sectionName">Section to add under.</param>
        /// <param name="settingName">Key name to add.</param>
        /// <param name="settingValue">Value of key.</param>
        public void AddSetting(string sectionName, string settingName, string settingValue)
        {
            SectionPair sectionPair = new SectionPair() { section = sectionName, key = settingName };

            if (keyPairs.ContainsKey(sectionPair))
                keyPairs.Remove(sectionPair);

            keyPairs.Add(sectionPair, settingValue);
        }

        public void AddSetting(string sectionName, string settingName, bool settingValue)
        {
            SectionPair sectionPair = new SectionPair() { section = sectionName, key = settingName };

            if (keyPairs.ContainsKey(sectionPair))
                keyPairs.Remove(sectionPair);

            keyPairs.Add(sectionPair, (settingValue) ? BOOL_VALUE_YES : BOOL_VALUE_NO);
        }

        public void AddSetting(string sectionName, string settingName, int settingValue)
        {
            SectionPair sectionPair = new SectionPair() { section = sectionName, key = settingName };

            if (keyPairs.ContainsKey(sectionPair))
                keyPairs.Remove(sectionPair);

            keyPairs.Add(sectionPair, settingValue.ToString());
        }

        /// <summary>
        /// Adds or replaces a setting to the table to be saved with a null value.
        /// </summary>
        /// <param name="sectionName">Section to add under.</param>
        /// <param name="settingName">Key name to add.</param>
        public void AddSetting(string sectionName, string settingName)
        {
            AddSetting(sectionName, settingName, null);
        }

        /// <summary>
        /// Remove a setting.
        /// </summary>
        /// <param name="sectionName">Section to add under.</param>
        /// <param name="settingName">Key name to add.</param>
        public void DeleteSetting(string sectionName, string settingName)
        {
            SectionPair sectionPair = new SectionPair() { section = sectionName, key = settingName };

            if (keyPairs.ContainsKey(sectionPair))
                keyPairs.Remove(sectionPair);
        }

        /// <summary>
        /// Save settings to new file.
        /// </summary>
        /// <param name="newFilePath">New file path.</param>
        public void SaveSettings(string newFilePath)
        {
            ArrayList sections = new ArrayList();
            string tmpValue = "";
            string strToSave = "";

            foreach (SectionPair sectionPair in keyPairs.Keys)
            {
                if (!sections.Contains(sectionPair.section))
                    sections.Add(sectionPair.section);
            }

            foreach (string section in sections)
            {
                strToSave += ("[" + section + "]\r\n");

                foreach (SectionPair sectionPair in keyPairs.Keys)
                {
                    if (sectionPair.section == section)
                    {
                        tmpValue = (string)keyPairs[sectionPair];

                        if (tmpValue != null)
                            tmpValue = "=" + tmpValue;

                        strToSave += (sectionPair.key + tmpValue + "\r\n");
                    }
                }

                strToSave += "\r\n";
            }

            try
            {
                TextWriter tw = new StreamWriter(newFilePath);
                tw.Write(strToSave);
                tw.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Save settings back to ini file.
        /// </summary>
        public void SaveSettings()
        {
            SaveSettings(iniFilePath);
        }
    }
}
