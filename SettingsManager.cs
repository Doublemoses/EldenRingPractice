using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using System.Xml.Linq;
using Tomlyn;
using Tomlyn.Model;

namespace EldenRingPractice
{
    class SettingsManager
    {
        XmlDocument settingsXML = null;
        const string settingsFileName = "settings.xml";
        string settingsFullPath = "";

        public SettingsManager()
        {
            //settingsXML = new XmlDocument();
            settingsFullPath = System.IO.Path.GetDirectoryName(Assembly.GetEntryAssembly().Location) + @"\" + settingsFileName;

            //MessageBox.Show(currentDirectory + @"\" + settingsFileName);

            if (File.Exists(settingsFullPath))
            {

            }
            else
            {
                createNewSettingsFile();
            }
        }

        void createNewSettingsFile()
        {
            //System.IO.Path.GetDirectoryName(Application.ExecutablePath);
            //XmlDeclaration xmlDeclaration = settingsFile.CreateXmlDeclaration("1.0", "UTF-8", null);
            /*
            XElement settings =
                new XElement("Settings",
                    new XElement("General",
                        new XElement("SettingA", "1"),
                        new XElement("SettingB", "2")
                    ),
                    new XElement("Saves",
                        new XElement("ERSaveFile", "C:")
                    ),
                    new XElement("InfoPanel",
                        new XElement("SettingA", "1")
                    ),
                    new XElement("Hotkeys",
                        new XElement("SettingA", "1")
                    )
                );

            */


            var toml =
                @"[General]
                setting1 = 1
                [Saves]
                ERSaveFile = ""c:""
                [Hotkeys]
                NO_DEATH_PLAYER = [0,74]";

            // Parse the TOML string to the default runtime model `TomlTable`
            var model = Toml.ToModel(toml);

            

            //MessageBox.Show( ((TomlTable)model["Saves"])["ERSaveFile"].ToString() );

            // Generates a TOML string from the model
            var tomlOut = Toml.FromModel(model);



            //MessageBox.Show(toml);

            /*try
            {
                using (XmlTextWriter writer = new XmlTextWriter("product.xml", System.Text.Encoding.UTF8))
                {

                }
            }
            catch
            {
                // couldn't write file
            }*/

        }

    }
}
