﻿using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using Assistant;
using System.Windows.Forms;
using System.Threading;
using System.Text.RegularExpressions;

namespace RazorEnhanced
{
    public class Dress
    {
        [Serializable]
        public class DressItem
        {
            private int m_Layer;
            public int Layer { get { return m_Layer; } }

            private string m_Name;
			public string Name { get { return m_Name; } }

			private int m_serial;
            public int Serial { get { return m_serial; } }

			private bool m_Selected;
			internal bool Selected { get { return m_Selected; } }

            public DressItem(string name, int layer, int serial, bool selected)
			{
				m_Name = name;
                m_Layer = layer;
                m_serial = serial;
				m_Selected = selected;
			}
            
        }
        internal class DressList
        {
            private string m_Description;
            internal string Description { get { return m_Description; } }

            private int m_Delay;
            internal int Delay { get { return m_Delay; } }

            private int m_Bag;
            internal int Bag { get { return m_Bag; } }

            private bool m_Conflict;
            internal bool Conflict { get { return m_Conflict; } }

            private bool m_Selected;
            internal bool Selected { get { return m_Selected; } }

            public DressList(string description, int delay, int bag, bool conflict, bool selected)
            {
                m_Description = description;
                m_Delay = delay;
                m_Bag = bag;
                m_Conflict = conflict;
                m_Selected = selected;
            }
        }

        internal static void AddLog(string addlog)
        {
            Engine.MainWindow.DressLogBox.Invoke(new Action(() => Engine.MainWindow.DressLogBox.Items.Add(addlog)));
            Engine.MainWindow.DressLogBox.Invoke(new Action(() => Engine.MainWindow.DressLogBox.SelectedIndex = Engine.MainWindow.DressLogBox.Items.Count - 1));
            if (Assistant.Engine.MainWindow.DressLogBox.Items.Count > 300)
                Assistant.Engine.MainWindow.DressLogBox.Invoke(new Action(() => Assistant.Engine.MainWindow.DressLogBox.Items.Clear()));
        }

        internal static int DressDelay
        {
            get
            {
                int delay = 100;
                Assistant.Engine.MainWindow.DressDragDelay.Invoke(new Action(() => Int32.TryParse(Assistant.Engine.MainWindow.DressDragDelay.Text, out delay)));
                return delay;
            }
            set
            {
                Assistant.Engine.MainWindow.DressDragDelay.Invoke(new Action(() => Assistant.Engine.MainWindow.DressDragDelay.Text = value.ToString()));
            }
        }

        internal static int DressBag
        {
            get
            {
                int serialBag = 0;

                try
                {
                    serialBag = Convert.ToInt32(Assistant.Engine.MainWindow.DressBagLabel.Text, 16);

                    if (serialBag == 0)
                    {
                        serialBag = (int)World.Player.Backpack.Serial.Value;
                    }
                    else
                    {
                        Item bag = RazorEnhanced.Items.FindBySerial(serialBag);
                        if (bag == null)
                            serialBag = (int)World.Player.Backpack.Serial.Value;
                        else
                            serialBag = bag.Serial;
                    }
                }
                catch 
                {
                }

                return serialBag;
            }
            set
            {
                Assistant.Engine.MainWindow.DressBagLabel.Text = "0x" + value.ToString("X8");
            }
        }
        internal static bool DressConflict
        {
            get
            {
                return Assistant.Engine.MainWindow.DressCheckBox.Checked;
            }
            set
            {
                Assistant.Engine.MainWindow.DressCheckBox.Checked = value;
            }
        }
        internal static string DressListName
        {
            get
            {
                return (string)Assistant.Engine.MainWindow.DressListSelect.Invoke(new Func<string>(() => Assistant.Engine.MainWindow.DressListSelect.Text));
            }

            set
            {
                Assistant.Engine.MainWindow.DressListSelect.Invoke(new Action(() => Assistant.Engine.MainWindow.DressListSelect.Text = value));
            }
        }

        internal static void RefreshLists()
        {
            List<DressList> lists;
            RazorEnhanced.Settings.Dress.ListsRead(out lists);

            DressList selectedList = lists.Where(l => l.Selected).FirstOrDefault();
            if (selectedList != null && selectedList.Description == Assistant.Engine.MainWindow.DressListSelect.Text)
                return;

            Assistant.Engine.MainWindow.DressListSelect.Items.Clear();
            foreach (DressList l in lists)
            {
                Assistant.Engine.MainWindow.DressListSelect.Items.Add(l.Description);

                if (l.Selected)
                {
                    Assistant.Engine.MainWindow.DressListSelect.SelectedIndex = Assistant.Engine.MainWindow.DressListSelect.Items.IndexOf(l.Description);
                    DressDelay = l.Delay;
                    DressBag = l.Bag;
                    DressConflict = l.Conflict;
                }
            }
        }

        internal static void AddList(string newList)
        {
            RazorEnhanced.Settings.Dress.ListInsert(newList, RazorEnhanced.Dress.DressDelay, (int)0, false);
            RazorEnhanced.Dress.RefreshLists();
            RazorEnhanced.Dress.RefreshItems();
        }

        internal static void RemoveList(string list)
        {
            if (RazorEnhanced.Settings.Dress.ListExists(list))
            {
                RazorEnhanced.Settings.Dress.ListDelete(list);
            }

            RazorEnhanced.Dress.RefreshLists();
            RazorEnhanced.Dress.RefreshItems();
        }

        internal static void UpdateSelectedItems()
        {
            List<DressItem> items;
            RazorEnhanced.Settings.Dress.ItemsRead(DressListName, out items);

            if (items.Count != Assistant.Engine.MainWindow.DressListView.Items.Count)
            {
                return;
            }

            for (int i = 0; i < Assistant.Engine.MainWindow.DressListView.Items.Count; i++)
            {
                ListViewItem lvi = Assistant.Engine.MainWindow.DressListView.Items[i];
                DressItem old = items[i];

                if (lvi != null && old != null)
                {
                    DressItem item = new Dress.DressItem(old.Name, old.Layer, old.Serial, lvi.Checked);
                    RazorEnhanced.Settings.Dress.ItemReplace(RazorEnhanced.Dress.DressListName, i, item);
                }
            }
        }

        internal static void RefreshItems()
        {
            List<DressList> lists;
            RazorEnhanced.Settings.Dress.ListsRead(out lists);

            Assistant.Engine.MainWindow.DressListView.Items.Clear();
            foreach (DressList l in lists)
            {
                if (l.Selected)
                {
                    List<Dress.DressItem> items;
                    RazorEnhanced.Settings.Dress.ItemsRead(l.Description, out items);
                    foreach (DressItem item in items)
                    {
                        ListViewItem listitem = new ListViewItem();
                        listitem.Checked = item.Selected;
                        listitem.SubItems.Add(LayerIntToLayerString(item.Layer));
                        if (item.Name != "UNDRESS")
                        {
                            listitem.SubItems.Add(item.Name);
                            listitem.SubItems.Add("0x" + item.Serial.ToString("X8"));
                        }
                        else
                        {
                            listitem.SubItems.Add("UNDRESS");
                            listitem.SubItems.Add("UNDRESS");
                        }
                        Assistant.Engine.MainWindow.DressListView.Items.Add(listitem);  
                    }        
                }
            }
        }

        internal static void ReadPlayerDress()
        {
            RazorEnhanced.Settings.Dress.ItemClear(Assistant.Engine.MainWindow.DressListSelect.Text);

            Assistant.Item layeritem = Assistant.World.Player.GetItemOnLayer(Layer.RightHand);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 0, layeritem.Serial,true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.LeftHand);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 1, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Shoes);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 2, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Pants);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 3, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Shirt);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 4, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Head);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 5, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Gloves);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 6, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Ring);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 7, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Neck);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 8, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Waist);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 9, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.InnerTorso);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 10, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Bracelet);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 11, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.MiddleTorso);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 12, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Earrings);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 13, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Arms);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 14, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Cloak);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 15, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.OuterTorso);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 16, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.OuterLegs);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 17, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.InnerLegs);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 18, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }

            layeritem = Assistant.World.Player.GetItemOnLayer(Layer.Unused_x9);
            if (layeritem != null)
            {
                RazorEnhanced.Dress.DressItem itemtoinsert = new DressItem(layeritem.Name, 19, layeritem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsert(Assistant.Engine.MainWindow.DressListSelect.Text, itemtoinsert);
            }
            RazorEnhanced.Dress.RefreshItems();
        }
        internal static void AddItemByTarger(Assistant.Item dressItem)
        {
            int layertoinsert = LayerLayerToInt(dressItem.Layer);
            if (layertoinsert != -1)
            {
                RazorEnhanced.Dress.DressItem toinsert = new RazorEnhanced.Dress.DressItem(dressItem.Name, LayerLayerToInt(dressItem.Layer), dressItem.Serial, true);
                RazorEnhanced.Settings.Dress.ItemInsertByLayer(Assistant.Engine.MainWindow.DressListSelect.Text, toinsert);
                RazorEnhanced.Dress.RefreshItems();
            }
            else
            {
                Misc.SendMessage("This item not have valid layer: " + layertoinsert);
            }
        }
        private static Assistant.Layer LayerNumberToLayer(int layer)
        {  
            switch (layer)
            { 
                case 0:
                    return Assistant.Layer.RightHand;
                case 1:
                    return Assistant.Layer.LeftHand;
                case 2:
                    return Assistant.Layer.Shoes;
                case 3:
                    return Assistant.Layer.Pants;
                case 4:
                    return Assistant.Layer.Shirt;
                case 5:
                    return Assistant.Layer.Head;
                case 6:
                    return Assistant.Layer.Gloves;
                case 7:
                    return Assistant.Layer.Ring;
                case 8:
                    return Assistant.Layer.Neck;
                case 9:
                    return Assistant.Layer.Waist;
                case 10:
                    return Assistant.Layer.InnerTorso;
                case 11:
                    return Assistant.Layer.Bracelet;
                case 12:
                    return Assistant.Layer.MiddleTorso;
                case 13:
                    return Assistant.Layer.Earrings;
                case 14:
                    return Assistant.Layer.Arms;
                case 15:
                    return Assistant.Layer.Cloak;
                case 16:
                    return Assistant.Layer.OuterTorso;
                case 17:
                    return Assistant.Layer.OuterLegs;
                case 18:
                    return Assistant.Layer.InnerLegs;
                case 19:
                    return Assistant.Layer.Unused_x9;
            }
            return 0;
        }
        private static string LayerIntToLayerString(int layer)
        {
            switch (layer)
            {
                case 0:
                    return "RightHand";
                case 1:
                    return "LeftHand";
                case 2:
                    return "Shoes";
                case 3:
                    return "Pants";
                case 4:
                    return "Shirt";
                case 5:
                    return "Head";
                case 6:
                    return "Gloves";
                case 7:
                    return "Ring";
                case 8:
                    return "Neck";
                case 9:
                    return "Waist";
                case 10:
                    return "InnerTorso";
                case 11:
                    return "Bracelet";
                case 12:
                    return "MiddleTorso";
                case 13:
                    return "Earrings";
                case 14:
                    return "Arms";
                case 15:
                    return "Cloak";
                case 16:
                    return "OuterTorso";
                case 17:
                    return "OuterLegs";
                case 18:
                    return "InnerLegs";
                case 19:
                    return "Unused_x9";
            }
            return null;
        }
        private static int LayerLayerToInt(Assistant.Layer layer)
        {
            switch (layer)
            {
                case Assistant.Layer.RightHand:
                    return 0;
                case Assistant.Layer.LeftHand:
                    return 1;
                case Assistant.Layer.Shoes:
                    return 2;
                case Assistant.Layer.Pants:
                    return 3;
                case Assistant.Layer.Shirt:
                    return 4;
                case Assistant.Layer.Head:
                    return 5;
                case Assistant.Layer.Gloves:
                    return 6;
                case Assistant.Layer.Ring:
                    return 7;
                case Assistant.Layer.Neck:
                    return 8;
                case Assistant.Layer.Waist:
                    return 9;
                case Assistant.Layer.InnerTorso:
                    return 10;
                case Assistant.Layer.Bracelet:
                    return 11;
                case Assistant.Layer.MiddleTorso:
                    return 12;
                case Assistant.Layer.Earrings:
                    return 13;
                case Assistant.Layer.Arms:
                    return 14;
                case Assistant.Layer.Cloak:
                    return 15;
                case Assistant.Layer.OuterTorso:
                    return 16;
                case Assistant.Layer.OuterLegs:
                    return 17;
                case Assistant.Layer.InnerLegs:
                    return 18;
                case Assistant.Layer.Unused_x9:
                    return 19;
                default:
                    return -1;
            }
        }
        internal static int LayerStringToInt(string layer)
        {
            switch (layer)
            {
                case "RightHand":
                    return 0;
                case "LeftHand":
                    return 1;
                case "Shoes":
                    return 2;
                case "Pants":
                    return 3;
                case "Shirt":
                    return 4;
                case "Head":
                    return 5;
                case "Gloves":
                    return 6;
                case "Ring":
                    return 7;
                case "Neck":
                    return 8;
                case "Waist":
                    return 9;
                case "InnerTorso":
                    return 10;
                case "Bracelet":
                    return 11;
                case "MiddleTorso":
                    return 12;
                case "Earrings":
                    return 13;
                case "Arms":
                    return 14;
                case "Cloak":
                    return 15;
                case "OuterTorso":
                    return 16;
                case "OuterLegs":
                    return 17;
                case "InnerLegs":
                    return 18;
                case "Unused_x9":
                    return 19;
            }
            return 1;
        }
    }
}
