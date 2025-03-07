using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using static EldenRingPractice.ItemSpawner;

namespace EldenRingPractice
{
    class FlagManager
    {
        private Dictionary<string, uint> graceBasegameFlags = new Dictionary<string, uint>();
        private Dictionary<string, uint> graceDLCFlags = new Dictionary<string, uint>();

        public enum FlagCategory
        {
            Grace_Basegame,
            Grace_DLC,
        }

        public void loadFlagList()
        {

            string line;
            FlagCategory category = FlagCategory.Grace_Basegame;
            var assembly = Assembly.GetExecutingAssembly();
            string resource = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith("flags.erd"));
            StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(resource));

            //try
            //{
            while ((line = sr.ReadLine()) != null)
            {
                if (line.StartsWith("["))
                {
                    switch (line)
                    {
                        case "[GRACE BASEGAME]":
                            category = FlagCategory.Grace_Basegame; break;
                        case "[GRACE DLC]":
                            category = FlagCategory.Grace_DLC; break;
                    }
                }
                else
                {
                    string[] split = line.Split((char)0x09); // 0x09 is tab
                    if (split.Length == 2)
                    {
                        int numberBase = 10;
                        if (split[1].StartsWith("0x"))
                        {
                            numberBase = 16;
                        }

                        switch (category)
                        {
                            case FlagCategory.Grace_Basegame:
                                graceBasegameFlags.Add(split[0], Convert.ToUInt32(split[1], numberBase)); break;
                            case FlagCategory.Grace_DLC:
                                graceDLCFlags.Add(split[0], Convert.ToUInt32(split[1], numberBase)); break;
                        }

                    }
                }
            }
            /*}
            catch (Exception e)
            {
                MessageBox.Show("item id list fail?");
            }*/

        }

        public void populateFlagList(ListBox listBox)
        {
            listBox.ItemsSource = graceDLCFlags;
        }
    }
}
