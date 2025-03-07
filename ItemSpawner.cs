using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace EldenRingPractice
{
    class ItemSpawner
    {
        static Dictionary<string,uint> ashOfWarDB = new Dictionary<string, uint>();
        private SortedDictionary<string, (uint, ItemCategory)> itemIDs = new SortedDictionary<string, (uint, ItemCategory)>();

        public readonly string[] affinities =
        {
            "None",
            "Heavy",
            "Keen",
            "Quality",
            "Fire",
            "Flame Art",
            "Lightning",
            "Sacred",
            "Magic",
            "Cold",
            "Poison",
            "Blood",
            "Occult"
        };

        public ItemSpawner()
        {
            
        }

        public enum ItemCategory
        {
            Default,
            Smithing,
            Somber,
            Smithing_NoAsh,
            Smithing_Perfume,
            Armor,
            Ash,
        }

        public void loadItemList()
        {
            //try
            //{
                string line;
                ItemCategory category = ItemCategory.Default;
                var assembly = Assembly.GetExecutingAssembly();
                string resource = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith("items.erd"));
                StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(resource));

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.StartsWith("["))
                    {
                        switch (line)
                        {
                            case "[SMITHING]":
                                category = ItemCategory.Smithing; break;
                            case "[SOMBER]":
                                category = ItemCategory.Somber; break;
                            case "[SMITHING NO ASH]":
                                category = ItemCategory.Somber; break;
                            case "[SPIRIT ASHES]":
                                category = ItemCategory.Ash; break;
                            case "[ARMOR]":
                                category = ItemCategory.Armor; break;
                            case "[SMITHING PERFUME]":
                            category = ItemCategory.Smithing_Perfume; break;
                            case "[DEFAULT]":
                                category = ItemCategory.Default; break;
                        }
                    }
                    else
                    {
                        string[] split = line.Split(",");
                        if (split.Length == 2)
                        {
                            //var itemID = 0;
                            int numberBase = 10;
                            if (split[1].StartsWith("0x"))
                            {
                                numberBase = 16;
                            }
                            itemIDs.Add(split[0], (Convert.ToUInt32(split[1], numberBase), category));
                        }
                    }
                }
            /*}
            catch (Exception e)
            {
                MessageBox.Show("item id list fail?");
            }*/

        }

        public void loadAshOfWar()
        {
            try
            {
                string line;
                var assembly = Assembly.GetExecutingAssembly();
                string resource = Assembly.GetExecutingAssembly().GetManifestResourceNames().Single(str => str.EndsWith("ashofwar.erd"));
                StreamReader sr = new StreamReader(assembly.GetManifestResourceStream(resource));

                while ((line = sr.ReadLine()) != null)
                {
                    string[] split = line.Split(",");
                    if (split.Length == 2)
                        { ashOfWarDB.Add(split[0], Convert.ToUInt32(split[1], 16)); }
                }
            }
            catch (Exception e)
            {
                //MessageBox.Show("fail?");
            }
        }

        public void populateItemList(ListBox listBox)
        {
            listBox.ItemsSource = itemIDs;
        }

        public void populateAffinities(ComboBox comboBox)
        {   
            foreach (String affinity in affinities)
            {
                comboBox.Items.Add(affinity);
            }
        }

        public void populateAshOfWar(ComboBox comboBox)
        {
            //foreach (var ash in ashOfWarDB)
            //{
            //    comboBox.Items.Add(ash.Item1);
            //}
            comboBox.ItemsSource = ashOfWarDB;
        }

        public void populateWeaponLevel(ComboBox comboBox, int maxLevel)
        {
            for (int i = 0; i <= maxLevel; i++)
            {
                comboBox.Items.Add($"+{(int)i}");
            }
        }

        public int getListboxPositionByString(string searchTerm)
        {
            var searchChars = searchTerm.ToLower().Replace("'", "").ToCharArray();
            var bestMatchLength = 0;
            var currentEntry = 0;
            var bestMatch = 0;

            if (searchChars.Length == 0) { return 0; }

            foreach (KeyValuePair<string, (uint, ItemCategory)> entry in itemIDs)
            {
                if (entry.Key.ToLower().StartsWith(searchChars[0]))
                {
                    if (searchChars.Length == 1) { return currentEntry;  }

                    bool keepComparing = true;
                    int i = 1;
                    var compareChars = entry.Key.ToLower().Replace("'", "").ToCharArray();

                    while (keepComparing)
                    {
                        if (searchChars[i] == compareChars[i] )
                        {
                            if (i > bestMatchLength) {
                                bestMatch = currentEntry;
                                bestMatchLength = i;
                            }

                            i++;

                            if ( i == searchChars.Length ) { return currentEntry; }
                            if ( i == compareChars.Length) { keepComparing = false; }
                        }
                        else
                        { 
                            keepComparing = false;
                        }
                    }
                    if ((i - 1) < bestMatchLength) { return bestMatch; }
                }
                currentEntry++;
            }
            return bestMatch;
        }

        public (uint, uint, uint) getValidatedItemValues(KeyValuePair<string, (uint itemID, ItemCategory category)> selected, uint level, uint affinity, uint ash, uint quantity)
        {
            uint quantityReturn = 1;
            uint ashReturn = 0xFFFFFFFF;
            uint itemIDReturn = 0;
            uint levelModified = 0;

            switch (selected.Value.category)
            {
                case ItemCategory.Smithing:
                    itemIDReturn = calculateWeaponID(selected.Value.itemID, level, affinity);
                    ashReturn = ash;
                    break;
                case ItemCategory.Ash:
                case ItemCategory.Somber:
                    itemIDReturn = calculateWeaponID(selected.Value.itemID,
                        (level > 10) ? levelModified = 10 : levelModified = level);
                    break;
                case ItemCategory.Smithing_NoAsh:
                    itemIDReturn = calculateWeaponID(selected.Value.itemID, level);
                    break;
                case ItemCategory.Smithing_Perfume:
                    itemIDReturn = calculateWeaponID(selected.Value.itemID, level);
                    ashReturn = ash;
                    break;
                case ItemCategory.Armor:
                    itemIDReturn = selected.Value.itemID;
                    break;
                case ItemCategory.Default:
                    if (quantity > 99)
                        { quantityReturn = 99; }
                    else
                        { quantityReturn = quantity; }
                    itemIDReturn = selected.Value.itemID;
                    break;
            }

            return (itemIDReturn, ashReturn, quantityReturn);
        }

        private uint calculateWeaponID(uint weaponID, uint level, uint affinity = 0)
        {
            return weaponID + level + (affinity * 100);
        }

    }
}
