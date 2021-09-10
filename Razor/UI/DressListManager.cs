using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Assistant.UI
{
    class DressListManager
    {
        private static Form m_dialogOwner;
        private static ListBox m_dressList;
        private static ListBox m_dressItems;
        private static DressList m_undressBagList = null;

        public static void SetControls(Form dialogOwner, ListBox dressList, ListBox dressItems)
        {
            m_dialogOwner = dialogOwner;
            m_dressList = dressList;
            m_dressItems = dressItems;
            DressList.OnItemsReset += OnItemsReset;
        }

        public static void Display()
        {
            m_dressList?.SafeAction(s =>
            {
                int selected = s.SelectedIndex;
                Refresh();
                if (selected >= 0 && selected < s.Items.Count)
                {
                    s.SelectedIndex = selected;
                }
            });
        }

        public static void Refresh()
        {
            m_dressList?.SafeAction(s => s.Items.Clear());
            m_dressItems?.SafeAction(s => s.Items.Clear());

            m_dressList?.SafeAction(s =>
            {
                foreach (var list in DressList.DressLists)
                {
                    s.Items.Add(list);
                }
            });
        }

        public static void AddDress()
        {
            if (InputBox.Show(m_dialogOwner, Language.GetString(LocString.DressName), Language.GetString(LocString.EnterAName)))
            {
                string str = InputBox.GetString();
                if (str == null || str == "")
                    return;
                DressList list = new DressList(str);
                DressList.Add(list);

                m_dressList.SafeAction(s =>
                {
                    s.Items.Add(list);
                    s.SelectedItem = list;
                });
            }
        }

        public static void RemoveDress()
        {
            var dress = m_dressList.SelectedItem as DressList;

            if (dress != null && MessageBox.Show(m_dialogOwner, Language.GetString(LocString.DelDressQ), "Confirm",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                dress.Items.Clear();

                m_dressList.SafeAction(s =>
                {
                    s.Items.Remove(dress);
                    s.SelectedIndex = -1;
                });

                m_dressItems.SafeAction(s => s.Items.Clear());

                DressList.Remove(dress);
            }
        }

        public static void DressNow()
        {
            var dress = m_dressList.SelectedItem as DressList;
            if (dress != null && World.Player != null)
                dress.Dress();
        }

        public static void UndressNow()
        {
            var dress = m_dressList.SelectedItem as DressList;
            if (dress != null && World.Player != null && World.Player.Backpack != null)
                dress.Undress();
        }

        public static void AddItem()
        {
            Targeting.OneTimeTarget(OnDressItemTarget);
        }

        public static void RemoveItem()
        {
            var list = m_dressList.SelectedItem as DressList;
            if (list == null)
                return;

            int sel = m_dressItems.SelectedIndex;
            if (sel < 0 || sel >= list.Items.Count)
                return;

            if (MessageBox.Show(m_dialogOwner, Language.GetString(LocString.DelDressItemQ), "Confirm", MessageBoxButtons.YesNo,
                    MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    list.Items.RemoveAt(sel);
                    m_dressItems.SafeAction(s => s.Items.RemoveAt(sel));
                }
                catch
                {
                }
            }
        }

        public static void ClearDress()
        {
            var list = m_dressList.SelectedItem as DressList;
            if (list == null)
                return;

            if (MessageBox.Show(m_dialogOwner, Language.GetString(LocString.Confirm), Language.GetString(LocString.ClearList),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                list.Items.Clear();
                m_dressItems.SafeAction(s => s.Items.Clear());
            }
        }

        public static void ConvertItemToType()
        {
            var list = m_dressList.SelectedItem as DressList;
            if (list == null)
                return;
            int sel = m_dressItems.SelectedIndex;
            if (sel < 0 || sel >= list.Items.Count)
                return;

            if (list.Items[sel] is Serial)
            {
                Serial s = (Serial)list.Items[sel];
                Item item = World.FindItem(s);
                if (item != null)
                {
                    list.Items[sel] = item.ItemID;
                    m_dressItems.SafeAction(di =>
                    {
                        di.BeginUpdate();
                        di.Items[sel] = item.ItemID.ToString();
                        di.EndUpdate();
                    });
                }
            }
        }

        public static void SetUndressBag()
        {
            if (World.Player == null)
                return;

            var list = m_dressList.SelectedItem as DressList;
            if (list == null)
                return;

            m_undressBagList = list;
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnDressBagTarget));
            World.Player.SendMessage(MsgLevel.Force, LocString.TargUndressBag, list.Name);
        }

        private static void FillDressItemsUnsafe(ListBox dressItems, DressList list)
        {
            dressItems.BeginUpdate();
            dressItems.Items.Clear();
            if (list != null)
            {
                for (int i = 0; i < list.Items.Count; i++)
                {
                    if (list.Items[i] is Serial)
                    {
                        Serial serial = (Serial)list.Items[i];
                        Item item = World.FindItem(serial);

                        if (item != null)
                            dressItems.Items.Add(item.ToString());
                        else
                            dressItems.Items.Add(Language.Format(LocString.OutOfRangeA1, serial));
                    }
                    else if (list.Items[i] is ItemID)
                    {
                        dressItems.Items.Add(list.Items[i].ToString());
                    }
                }
            }

            dressItems.EndUpdate();
        }

        public static void OnListSelected()
        {
            var list = m_dressList.SelectedItem as DressList;
            m_dressItems.SafeAction(s => FillDressItemsUnsafe(s, list));
        }

        public static void UseCurrent()
        {
            var list = m_dressList.SelectedItem as DressList;
            if (World.Player == null)
                return;
            if (list == null)
                return;

            for (int i = 0; i < World.Player.Contains.Count; i++)
            {
                Item item = (Item)World.Player.Contains[i];
                if (item.Layer <= Layer.LastUserValid && item.Layer != Layer.Backpack && item.Layer != Layer.Hair &&
                    item.Layer != Layer.FacialHair)
                    list.Items.Add(item.Serial);
            }

            m_dressList.SafeAction(s =>
            {
                s.SelectedItem = null;
                s.SelectedItem = list;
            });
        }

        private static void OnDressBagTarget(bool location, Serial serial, Point3D p, ushort gfxid)
        {
            if (m_undressBagList == null)
                return;

            Engine.MainWindow.ShowMe();
            if (serial.IsItem)
            {
                Item item = World.FindItem(serial);
                if (item != null)
                {
                    m_undressBagList.SetUndressBag(item.Serial);
                    World.Player.SendMessage(MsgLevel.Force, LocString.UB_Set);
                }
                else
                {
                    m_undressBagList.SetUndressBag(Serial.Zero);
                    World.Player.SendMessage(MsgLevel.Force, LocString.ItemNotFound);
                }
            }
            else
            {
                World.Player.SendMessage(MsgLevel.Force, LocString.ItemNotFound);
            }

            m_undressBagList = null;
        }

        private static void OnDressItemTarget(bool loc, Serial serial, Point3D pt, ushort itemid)
        {
            if (loc)
                return;

            // TODO: Refactor targeting from UI
            Engine.MainWindow.ShowMe();
            if (serial.IsItem)
            {
                var list = m_dressList.SelectedItem as DressList;

                if (list == null)
                    return;

                list.Items.Add(serial);
                Item item = World.FindItem(serial);

                m_dressItems.SafeAction(s =>
                {
                    if (item == null)
                        s.Items.Add(Language.Format(LocString.OutOfRangeA1, serial));
                    else
                        s.Items.Add(item.ToString());
                });
            }
        }

        private static void OnItemsReset()
        {
            m_dressList?.SafeAction(s => s.Items.Clear());
            m_dressItems?.SafeAction(s => s.Items.Clear());
        }
    }
}
