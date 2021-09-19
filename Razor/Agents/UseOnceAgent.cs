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
using System.Collections;
using System.Xml;

namespace Assistant.Agents
{
    public interface IUseOnceAgentEventHandler
    {
        void OnItemAdded(Item item);
        void OnItemRemovedAt(int index);
        void OnTargetAcquired();
    }

    public class UseOnceAgent : Agent
    {
        public static UseOnceAgent Instance { get; private set; }

        public static void Initialize()
        {
            Agent.Add(Instance = new UseOnceAgent());
        }

        public UseOnceAgent()
        {
            Items = new ArrayList();
            PacketHandler.RegisterClientToServerViewer(0x09, new PacketViewerCallback(OnSingleClick));
            HotKey.Add(HKCategory.Agents, LocString.UseOnceAgent, new HotKeyCallback(OnHotKey));
            HotKey.Add(HKCategory.Agents, LocString.AddUseOnce, new HotKeyCallback(AddItem));
            HotKey.Add(HKCategory.Agents, LocString.AddUseOnceContainer, new HotKeyCallback(AddContainer));

            Number = 0;

            Agent.OnItemCreated += new ItemCreatedEventHandler(CheckItemOPL);
        }

        public override void Clear()
        {
            Items.Clear();
        }

        private void CheckItemOPL(Item newItem)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is Serial)
                {
                    if (newItem.Serial == (Serial) Items[i])
                    {
                        Items[i] = newItem;
                        newItem.ObjPropList.Add(Language.GetString(LocString.UseOnce));
                        break;
                    }
                }
            }
        }

        private void OnSingleClick(PacketReader pvSrc, PacketHandlerEventArgs args)
        {
            Serial serial = pvSrc.ReadUInt32();
            for (int i = 0; i < Items.Count; i++)
            {
                Item item;
                if (Items[i] is Serial)
                {
                    item = World.FindItem((Serial) Items[i]);
                    if (item != null)
                    {
                        Items[i] = item;
                    }
                }

                item = Items[i] as Item;
                if (item == null)
                {
                    continue;
                }

                if (item.Serial == serial)
                {
                    Client.Instance.SendToClient(new UnicodeMessage(item.Serial, item.ItemID,
                        Assistant.MessageType.Label, 0x3B2, 3, Language.CliLocName, "",
                        Language.Format(LocString.UseOnceHBA1, i + 1)));
                    break;
                }
            }
        }

        public override string Name
        {
            get { return Language.GetString(LocString.UseOnce); }
        }

        public override int Number { get; }

        public ArrayList Items { get; }
        public IUseOnceAgentEventHandler EventHandler { get; set; }

        public void RefreshItems()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is Serial)
                {
                    Item item = World.FindItem((Serial)Items[i]);
                    if (item != null)
                    {
                        Items[i] = item;
                    }
                }
            }
        }

        public void AddItem()
        {
            World.Player.SendMessage(MsgLevel.Force, LocString.TargItemAdd);
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnTarget));
        }

        public void AddContainer()
        {
            World.Player.SendMessage(MsgLevel.Force, LocString.TargCont);
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnTargetBag));
        }

        public void RemoveItem()
        {
            World.Player.SendMessage(MsgLevel.Force, LocString.TargItemRem);
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnTargetRemove));
        }

        public void ClearItems()
        {
            for (int i = 0; i < Items.Count; i++)
            {
                if (Items[i] is Item item)
                {
                    item.ObjPropList.Remove(Language.GetString(LocString.UseOnce));
                    item.OPLChanged();
                }
            }
            Items.Clear();
        }

        private void OnTarget(bool location, Serial serial, Point3D loc, ushort gfx)
        {
            EventHandler?.OnTargetAcquired();

            if (!location && serial.IsItem)
            {
                Item item = World.FindItem(serial);
                if (item == null)
                {
                    World.Player.SendMessage(MsgLevel.Force, LocString.ItemNotFound);
                    return;
                }

                item.ObjPropList.Add(Language.GetString(LocString.UseOnce));
                item.OPLChanged();

                Items.Add(item);
                EventHandler?.OnItemAdded(item);

                World.Player.SendMessage(MsgLevel.Force, LocString.ItemAdded);
            }
        }

        private void OnTargetRemove(bool location, Serial serial, Point3D loc, ushort gfx)
        {
            EventHandler?.OnTargetAcquired();

            if (!location && serial.IsItem)
            {
                for (int i = 0; i < Items.Count; i++)
                {
                    bool rem = false;
                    if (Items[i] is Item)
                    {
                        if (((Item) Items[i]).Serial == serial)
                        {
                            ((Item) Items[i]).ObjPropList.Remove(Language.GetString(LocString.UseOnce));
                            ((Item) Items[i]).OPLChanged();

                            rem = true;
                        }
                    }
                    else if (Items[i] is Serial)
                    {
                        if (((Serial) Items[i]) == serial)
                        {
                            rem = true;
                        }
                    }

                    if (rem)
                    {
                        Items.RemoveAt(i);
                        EventHandler?.OnItemRemovedAt(i);
                        World.Player.SendMessage(MsgLevel.Force, LocString.ItemRemoved);
                        return;
                    }
                }

                World.Player.SendMessage(MsgLevel.Force, LocString.ItemNotFound);
            }
        }

        private void OnTargetBag(bool location, Serial serial, Point3D loc, ushort gfx)
        {
            EventHandler?.OnTargetAcquired();

            if (!location && serial.IsItem)
            {
                Item i = World.FindItem(serial);
                if (i != null && i.Contains.Count > 0)
                {
                    for (int ci = 0; ci < i.Contains.Count; ci++)
                    {
                        Item toAdd = i.Contains[ci] as Item;

                        if (toAdd != null)
                        {
                            toAdd.ObjPropList.Add(Language.GetString(LocString.UseOnce));
                            toAdd.OPLChanged();
                            Items.Add(toAdd);
                            EventHandler?.OnItemAdded(toAdd);
                        }
                    }

                    World.Player.SendMessage(MsgLevel.Force, LocString.ItemsAdded, i.Contains.Count);
                }
            }
        }

        public override void Save(XmlTextWriter xml)
        {
            for (int i = 0; i < Items.Count; i++)
            {
                xml.WriteStartElement("item");
                if (Items[i] is Item)
                {
                    xml.WriteAttributeString("serial", ((Item) Items[i]).Serial.Value.ToString());
                }
                else
                {
                    xml.WriteAttributeString("serial", ((Serial) Items[i]).Value.ToString());
                }

                xml.WriteEndElement();
            }
        }

        public override void Load(XmlElement node)
        {
            foreach (XmlElement el in node.GetElementsByTagName("item"))
            {
                try
                {
                    string ser = el.GetAttribute("serial");
                    Items.Add((Serial) Convert.ToUInt32(ser));
                }
                catch
                {
                    // ignored
                }
            }
        }

        public void OnHotKey()
        {
            if (World.Player == null || !Client.Instance.AllowBit(FeatureBit.UseOnceAgent))
            {
                return;
            }

            if (Items.Count <= 0)
            {
                World.Player.SendMessage(MsgLevel.Error, LocString.UseOnceEmpty);
            }
            else
            {
                Item item = null;
                if (Items[0] is Item)
                {
                    item = (Item) Items[0];
                }
                else if (Items[0] is Serial)
                {
                    item = World.FindItem((Serial) Items[0]);
                }

                try
                {
                    Items.RemoveAt(0);
                    EventHandler?.OnItemRemovedAt(0);
                }
                catch
                {
                }

                if (item != null)
                {
                    item.ObjPropList.Remove(Language.GetString(LocString.UseOnce));
                    item.OPLChanged();

                    World.Player.SendMessage(LocString.UseOnceStatus, item, Items.Count);
                    PlayerData.DoubleClick(item);
                }
                else
                {
                    World.Player.SendMessage(LocString.UseOnceError);
                    OnHotKey();
                }
            }
        }
    }
}