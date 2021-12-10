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

namespace Assistant.Agents
{
    public interface ISellAgentEventHandler
    {
        void OnItemAdded(ushort item);
        void OnItemRemovedAt(int index);
        void OnItemsCleared();
        void OnHotBagChanged();
        void OnAgentToggled();
        void OnAmountChanged();
        void OnTargetAcquired();
    }

    public class SellAgent : Agent
    {
        public static SellAgent Instance { get; private set; }

        public static void Initialize()
        {
            Agent.Add(Instance = new SellAgent());
        }

        private readonly List<ushort> m_Items;
        private Serial m_HotBag;
        private bool m_Enabled;

        public SellAgent()
        {
            m_Items = new List<ushort>();
            PacketHandler.RegisterServerToClientViewer(0x9E, new PacketViewerCallback(OnVendorSell));
            PacketHandler.RegisterClientToServerViewer(0x09, new PacketViewerCallback(OnSingleClick));

            Number = 0;

            HotKey.Add(HKCategory.Agents, HKSubCat.None, Language.GetString(LocString.SetSellAgentHotBag),
                new HotKeyCallback(SetHotBag));

            Agent.OnItemCreated += new ItemCreatedEventHandler(CheckHBOPL);
        }

        private void CheckHBOPL(Item item)
        {
            if (item.Serial == m_HotBag)
            {
                item.ObjPropList.Add(Language.GetString(LocString.SellHB));
            }
        }

        private void OnSingleClick(PacketReader pvSrc, PacketHandlerEventArgs args)
        {
            Serial serial = pvSrc.ReadUInt32();
            if (m_HotBag == serial)
            {
                ushort gfx = 0;
                Item c = World.FindItem(m_HotBag);
                if (c != null)
                {
                    gfx = c.ItemID.Value;
                }

                Client.Instance.SendToClient(new UnicodeMessage(m_HotBag, gfx, Assistant.MessageType.Label, 0x3B2, 3,
                    Language.CliLocName, "", Language.GetString(LocString.SellHB)));
            }
        }

        public override void Clear()
        {
            m_Items.Clear();
        }

        private void OnVendorSell(PacketReader pvSrc, PacketHandlerEventArgs args)
        {
            if (!m_Enabled || !Client.Instance.AllowBit(FeatureBit.SellAgent) ||
                (m_Items.Count == 0 && m_HotBag == Serial.Zero))
            {
                return;
            }

            Item hb = null;
            if (m_HotBag != Serial.Zero)
            {
                hb = World.FindItem(m_HotBag);
                if (hb == null)
                {
                    //m_HotBag = Serial.Zero;
                    //SetHBText();
                    World.Player.SendMessage(MsgLevel.Warning, "Sell Agent HotBag could not be found.");

                    if (m_Items.Count == 0)
                    {
                        return;
                    }
                }
            }

            int total = 0;

            uint serial = pvSrc.ReadUInt32();
            Mobile vendor = World.FindMobile(serial);
            if (vendor == null)
            {
                World.AddMobile(vendor = new Mobile(serial));
            }

            int count = pvSrc.ReadUInt16();

            int maxSell = Config.GetInt("SellAgentMax");
            int sold = 0;
            List<SellListItem> list = new List<SellListItem>(count);
            for (int i = 0; i < count && (sold < maxSell || maxSell <= 0); i++)
            {
                uint ser = pvSrc.ReadUInt32();
                ushort gfx = pvSrc.ReadUInt16();
                ushort hue = pvSrc.ReadUInt16();
                ushort amount = pvSrc.ReadUInt16();
                ushort price = pvSrc.ReadUInt16();

                pvSrc.ReadString(pvSrc.ReadUInt16()); //name

                Item item = World.FindItem(ser);

                if (m_Items.Contains(gfx) || (item != null && item != hb && item.IsChildOf(hb)))
                {
                    if (sold + amount > maxSell && maxSell > 0)
                    {
                        amount = (ushort) (maxSell - sold);
                    }

                    list.Add(new SellListItem(ser, amount));
                    total += amount * price;
                    sold += amount;
                }

                //if ( sold >= maxSell && maxSell > 0 ) break;
            }

            if (list.Count > 0)
            {
                Client.Instance.SendToServer(new VendorSellResponse(vendor, list));
                World.Player.SendMessage(MsgLevel.Force, LocString.SellTotals, sold, total);
                args.Block = true;
            }
        }

        public override string Name
        {
            get { return Language.GetString(LocString.Sell); }
        }

        public override string Alias { get; set; }

        public override int Number { get; }

        public bool Enabled => m_Enabled;
        public bool HotBagSet => (m_HotBag != Serial.Zero);

        public IReadOnlyList<ushort> Items => m_Items;

        public ISellAgentEventHandler EventHandler { get; set; }

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

        public void ToggleHotBag()
        {
            if (m_HotBag == Serial.Zero)
            {
                SetHotBag();
            }
            else
            {
                Item hb = World.FindItem(m_HotBag);
                if (hb != null)
                {
                    if (hb.ObjPropList.Remove(Language.GetString(LocString.SellHB)))
                    {
                        hb.OPLChanged();
                    }
                }

                m_HotBag = Serial.Zero;
                EventHandler?.OnHotBagChanged();
            }
        }

        public void ClearItems()
        {
            m_Items.Clear();
            EventHandler?.OnItemsCleared();
        }

        public void SetAmount(int amount)
        {
            Config.SetProperty("SellAgentMax", amount);
            EventHandler?.OnAmountChanged();
        }

        public void Toggle()
        {
            m_Enabled = !m_Enabled;
            EventHandler?.OnAgentToggled();
        }

        private void OnTarget(bool location, Serial serial, Point3D loc, ushort gfx)
        {
            EventHandler?.OnTargetAcquired();
            if (!location && serial.IsItem)
            {
                Add(gfx);
            }
        }

        public void Add(ItemID itemId)
        {
            m_Items?.Add(itemId);
            EventHandler?.OnItemAdded(itemId.Value);

            World.Player?.SendMessage(MsgLevel.Force, LocString.ItemAdded);
        }

        private void OnHBTarget(bool location, Serial serial, Point3D loc, ushort gfx)
        {
            EventHandler?.OnTargetAcquired();
            if (!location && serial.IsItem)
            {
                m_HotBag = serial;
                EventHandler?.OnHotBagChanged();

                Item hb = World.FindItem(m_HotBag);
                if (hb != null)
                {
                    hb.ObjPropList.Add(Language.GetString(LocString.SellHB));
                    hb.OPLChanged();
                }
            }
        }

        public void SetHotBag()
        {
            World.Player.SendMessage(MsgLevel.Force, LocString.TargCont);
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnHBTarget));
        }

        public override void Save(XmlTextWriter xml)
        {
            if (m_Items == null)
            {
                return;
            }

            xml.WriteAttributeString("enabled", m_Enabled.ToString());

            if (m_HotBag != Serial.Zero)
            {
                xml.WriteStartElement("hotbag");
                xml.WriteString(m_HotBag.ToString());
                xml.WriteEndElement();
            }

            foreach (ushort iid in m_Items)
            {
                xml.WriteStartElement("item");
                xml.WriteAttributeString("id", iid.ToString());
                xml.WriteEndElement();
            }
        }


        public override void Load(XmlElement node)
        {
            try
            {
                m_Enabled = bool.Parse(node.GetAttribute("enabled"));
            }
            catch
            {
                m_Enabled = false;
            }

            try
            {
                m_HotBag = node["hotbag"] != null ? Serial.Parse(node["hotbag"].InnerText) : Serial.Zero;
            }
            catch
            {
                m_HotBag = Serial.Zero;
            }

            foreach (XmlElement el in node.GetElementsByTagName("item"))
            {
                try
                {
                    string str = el.GetAttribute("id");
                    m_Items.Add(Convert.ToUInt16(str));
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
