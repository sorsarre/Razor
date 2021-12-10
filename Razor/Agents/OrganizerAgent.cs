#region license

// Razor: An Ultima Online Assistant
// Copyright (C) 2021 Razor Development Community on GitHub <https://github.com/markdwags/Razor>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.Xml;
using Assistant.Scripts;

namespace Assistant.Agents
{
    public interface IOrganizerAgentEventHandler
    {
        void OnItemAdded(ItemID item);
        void OnItemRemovedAt(int index);
        void OnItemsCleared();
        void OnHotBagChanged();
        void OnTargetAcquired();
    }

    public class OrganizerAgent : Agent
    {
        public static List<OrganizerAgent> Agents { get; set; }

        public static void Initialize()
        {
            int maxAgents = Config.GetAppSetting<int>("MaxOrganizerAgents") == 0
                ? 20
                : Config.GetAppSetting<int>("MaxOrganizerAgents");

            Agents = new List<OrganizerAgent>();

            for (int i = 1; i <= maxAgents; i++)
            {
                OrganizerAgent organizerAgent = new OrganizerAgent(i);

                Agent.Add(organizerAgent);

                Agents.Add(organizerAgent);
            }
        }

        private readonly List<ItemID> m_Items;
        private uint m_Cont;

        public OrganizerAgent(int num)
        {
            m_Items = new List<ItemID>();
            Number = num;
            HotKey.Add(HKCategory.Agents, HKSubCat.None,
                $"{Language.GetString(LocString.OrganizerAgent)}-{Number:D2}",
                new HotKeyCallback(Organize));
            HotKey.Add(HKCategory.Agents, HKSubCat.None,
                $"{Language.GetString(LocString.SetOrganizerHB)}-{Number:D2}",
                new HotKeyCallback(SetHotBag));
            PacketHandler.RegisterClientToServerViewer(0x09, new PacketViewerCallback(OnSingleClick));

            Agent.OnItemCreated += new ItemCreatedEventHandler(CheckContOPL);
        }

        public void CheckContOPL(Item item)
        {
            if (item.Serial == m_Cont)
            {
                item.ObjPropList.Add(Language.Format(LocString.OrganizerHBA1, Number));
            }
        }

        private void OnSingleClick(PacketReader pvSrc, PacketHandlerEventArgs args)
        {
            uint serial = pvSrc.ReadUInt32();
            if (m_Cont == serial)
            {
                ushort gfx = 0;
                Item c = World.FindItem(m_Cont);
                if (c != null)
                {
                    gfx = c.ItemID.Value;
                }

                Client.Instance.SendToClient(new UnicodeMessage(m_Cont, gfx, Assistant.MessageType.Label, 0x3B2, 3,
                    Language.CliLocName, "", Language.Format(LocString.OrganizerHBA1, Number)));
            }
        }

        public override string Name
        {
            get { return $"{Language.GetString(LocString.Organizer)}-{Number}"; }
        }

        public override string Alias { get; set; }

        public override int Number { get; }

        public bool HotBagSet => m_Cont != 0;

        public IReadOnlyList<ItemID> Items => m_Items;

        public IOrganizerAgentEventHandler EventHandler { get; set; }

        public void SetHotBag()
        {
            World.Player.SendMessage(MsgLevel.Force, LocString.TargCont);
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnTargetBag));
        }

        public void AddItem()
        {
            World.Player.SendMessage(MsgLevel.Force, LocString.TargItemAdd);
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnTarget));
        }

        public void RemoveItemAt(int index)
        {
            if (Utility.IndexInRange(m_Items, index))
            {
                m_Items.RemoveAt(index);
                EventHandler?.OnItemRemovedAt(index);
            }
        }

        public void ClearItems()
        {
            Item bag = World.FindItem(m_Cont);
            if (bag != null)
            {
                bag.ObjPropList.Remove(Language.Format(LocString.OrganizerHBA1, Number));
                bag.OPLChanged();
            }

            m_Items.Clear();
            m_Cont = 0;
            EventHandler?.OnItemsCleared();
            EventHandler?.OnHotBagChanged();
        }

        public void Stop()
        {
            DragDropManager.GracefulStop();
        }

        public void Organize()
        {
            if (m_Cont == 0 || m_Cont > 0x7FFFFF00)
            {
                World.Player.SendMessage(MsgLevel.Force, LocString.ContNotSet);
                return;
            }

            Item pack = World.Player.Backpack;
            if (pack == null)
            {
                World.Player.SendMessage(MsgLevel.Warning, LocString.NoBackpack);
                return;
            }

            int count = OrganizeChildren(pack);

            if (count > 0)
            {
                World.Player.SendMessage(LocString.OrgQueued, count);
            }
            else
            {
                World.Player.SendMessage(LocString.OrgNoItems);
            }
        }

        private int OrganizeChildren(Item container)
        {
            object dest = World.FindItem(m_Cont);
            if (dest == null)
            {
                dest = World.FindMobile(m_Cont);
                if (dest == null)
                {
                    return 0;
                }
            }

            /*else if ( World.Player.Backpack != null && ((Item)dest).IsChildOf( World.Player ) && !((Item)dest).IsChildOf( World.Player.Backpack ) )
            {
                 return 0;
            }*/

            return OrganizeChildren(container, dest);
        }

        private int OrganizeChildren(Item container, object dest)
        {
            int count = 0;
            for (int i = 0; i < container.Contains.Count; i++)
            {
                Item item = (Item) container.Contains[i];
                if (item.Serial != m_Cont && !item.IsChildOf(dest))
                {
                    count += OrganizeChildren(item, dest);
                    if (m_Items.Contains(item.ItemID.Value))
                    {
                        if (dest is Item)
                        {
                            DragDropManager.DragDrop(item, (Item) dest);
                        }
                        else if (dest is Mobile)
                        {
                            DragDropManager.DragDrop(item, ((Mobile) dest).Serial);
                        }

                        count++;
                    }
                }
            }

            return count;
        }

        private void OnTarget(bool location, Serial serial, Point3D loc, ushort gfx)
        {
            EventHandler?.OnTargetAcquired();

            if (!location && serial.IsItem && World.Player != null)
            {
                Add(gfx);
            }
        }

        public void Add(ushort gfx)
        {
            if (m_Items != null && m_Items.Contains(gfx))
            {
                World.Player?.SendMessage(MsgLevel.Force, LocString.ItemExists);
            }
            else
            {
                m_Items?.Add(gfx);
                EventHandler?.OnItemAdded((ItemID)gfx);

                World.Player?.SendMessage(MsgLevel.Force, LocString.ItemAdded);
            }
        }

        private void OnTargetBag(bool location, Serial serial, Point3D loc, ushort gfx)
        {
            if (!ScriptManager.Running)
            {
                EventHandler?.OnTargetAcquired();
            }

            if (!location && serial > 0 && serial <= 0x7FFFFF00)
            {
                Item bag = World.FindItem(m_Cont);
                if (bag != null && bag.ObjPropList != null)
                {
                    bag.ObjPropList.Remove(Language.Format(LocString.OrganizerHBA1, Number));
                    bag.OPLChanged();
                }

                m_Cont = serial;
                EventHandler?.OnHotBagChanged();

                if (World.Player != null)
                {
                    World.Player.SendMessage(MsgLevel.Force, LocString.ContSet);
                }

                bag = World.FindItem(m_Cont);
                if (bag != null && bag.ObjPropList != null)
                {
                    bag.ObjPropList.Add(Language.Format(LocString.OrganizerHBA1, Number));
                    bag.OPLChanged();
                }
            }
        }

        public override void Clear()
        {
            m_Items.Clear();
            m_Cont = 0;
            EventHandler?.OnItemsCleared();
            EventHandler?.OnHotBagChanged();
        }

        public override void Save(XmlTextWriter xml)
        {
            xml.WriteAttributeString("hotbag", m_Cont.ToString());
            xml.WriteAttributeString("alias", Alias);

            for (int i = 0; i < m_Items.Count; i++)
            {
                xml.WriteStartElement("item");
                xml.WriteAttributeString("id", m_Items[i].Value.ToString());
                xml.WriteEndElement();
            }
        }

        public override void Load(XmlElement node)
        {
            try
            {
                m_Cont = Convert.ToUInt32(node.GetAttribute("hotbag"));
            }
            catch
            {
                // ignored
            }

            try
            {
                Alias = node.GetAttribute("alias");
            }
            catch
            {
                Alias = string.Empty;
            }

            EventHandler?.OnHotBagChanged();

            foreach (XmlElement el in node.GetElementsByTagName("item"))
            {
                try
                {
                    string gfx = el.GetAttribute("id");
                    m_Items.Add(Convert.ToUInt16(gfx));
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
