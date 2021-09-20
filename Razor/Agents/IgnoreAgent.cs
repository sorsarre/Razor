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
using System.Linq;
using System.Xml;

namespace Assistant.Agents
{
    public interface IIgnoreAgentEventHandler
    {
        void OnTargetAcquired();

        void OnItemAdded(IgnoreAgent.NamedSerial item);
        void OnItemsChanged();
        void OnItemRemovedAt(int index);
        void OnAgentToggled();
    }

    public class IgnoreAgent : Agent
    {
        public static IgnoreAgent Instance { get; private set; }

        public class NamedSerial
        {
            public Serial Serial { get; set; }
            public string Name { get; set; }
        }

        public static void Initialize()
        {
            Agent.Add(Instance = new IgnoreAgent());
        }

        public static bool IsIgnored(Serial ser)
        {
            return Instance?.IsSerialIgnored(ser) ?? false;
        }

        private readonly List<Serial> m_Chars;
        private readonly Dictionary<Serial, string> m_Names;
        private static bool m_Enabled;

        public IgnoreAgent()
        {
            m_Chars = new List<Serial>();
            m_Names = new Dictionary<Serial, string>();

            Number = 0;

            HotKey.Add(HKCategory.Targets, LocString.AddToIgnore, new HotKeyCallback(AddToIgnoreList));
            HotKey.Add(HKCategory.Targets, LocString.RemoveFromIgnore, new HotKeyCallback(RemoveFromIgnoreList));

            Agent.OnMobileCreated += new MobileCreatedEventHandler(OPLCheckIgnore);
        }

        public override void Clear()
        {
            m_Chars.Clear();
            m_Names.Clear();
        }

        public bool IsSerialIgnored(Serial ser)
        {
            if (m_Enabled)
            {
                return m_Chars.Contains(ser);
            }
            else
            {
                return false;
            }
        }

        public override string Name
        {
            get { return Language.GetString(LocString.IgnoreAgent); }
        }

        public bool Enabled => m_Enabled;

        public override int Number { get; }

        public IIgnoreAgentEventHandler EventHandler { get; set; }

        public IEnumerable<NamedSerial> Items => from c in m_Chars select ToNamedSerial(c);

        public void AddToIgnoreList()
        {
            World.Player.SendMessage(MsgLevel.Force, LocString.AddToIgnore);
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnAddTarget));
        }

        public void RemoveFromIgnoreList()
        {
            World.Player.SendMessage(MsgLevel.Force, LocString.RemoveFromIgnore);
            Targeting.OneTimeTarget(new Targeting.TargetResponseCallback(OnRemoveTarget));
        }

        public void RemoveItemAt(int index)
        {
            if (Utility.IndexInRange(m_Chars, index))
            {
                try
                {
                    m_Names.Remove(m_Chars[index]);
                }
                catch
                {
                }

                m_Chars.RemoveAt(index);
                EventHandler?.OnItemRemovedAt(index);
            }
        }

        public void ClearItems()
        {
            foreach (Serial s in m_Chars)
            {
                Mobile m = World.FindMobile(s);
                if (m != null)
                {
                    if (m.ObjPropList.Remove(Language.GetString(LocString.RazorIgnored)))
                    {
                        m.OPLChanged();
                    }
                }
            }

            m_Chars.Clear();
            EventHandler?.OnItemsChanged();
        }

        public void ToggleAgent()
        {
            m_Enabled = !m_Enabled;
            EventHandler?.OnAgentToggled();
        }

        private void OPLCheckIgnore(Mobile m)
        {
            if (IsIgnored(m.Serial))
            {
                m.ObjPropList.Add(Language.GetString(LocString.RazorIgnored));
            }
        }

        private void OnAddTarget(bool location, Serial serial, Point3D loc, ushort gfx)
        {
            EventHandler?.OnTargetAcquired();

            if (!location && serial.IsMobile && serial != World.Player.Serial)
            {
                World.Player.SendMessage(MsgLevel.Force, LocString.AddToIgnore);
                if (!m_Chars.Contains(serial))
                {
                    m_Chars.Add(serial);

                    NotifyNewSerial(serial);

                    Mobile m = World.FindMobile(serial);
                    if (m != null)
                    {
                        m.ObjPropList.Add(Language.GetString(LocString.RazorIgnored));
                        m.OPLChanged();
                    }
                }
            }
        }

        private string GetName(Serial s)
        {
            Mobile m = World.FindMobile(s);
            string name = null;

            if (m_Names.ContainsKey(s))
            {
                name = m_Names[s] as string;
            }

            if (m != null && m.Name != null && m.Name != "")
            {
                name = m.Name;
            }

            if (name == null)
            {
                name = "(Name Unknown)";
            }

            m_Names[s] = name;

            return name;
        }

        private NamedSerial ToNamedSerial(Serial s)
        {
            return new NamedSerial
            {
                Name = GetName(s),
                Serial = s
            };
        }

        private void NotifyNewSerial(Serial s)
        {
            EventHandler?.OnItemAdded(ToNamedSerial(s));
        }

        private void OnRemoveTarget(bool location, Serial serial, Point3D loc, ushort gfx)
        {
            EventHandler?.OnTargetAcquired();

            if (!location && serial.IsMobile && serial != World.Player.Serial)
            {
                m_Chars.Remove(serial);
                m_Names.Remove(serial);

                World.Player.SendMessage(MsgLevel.Force, LocString.RemoveFromIgnore);

                EventHandler?.OnItemsChanged();

                Mobile m = World.FindMobile(serial);
                if (m != null)
                {
                    if (m.ObjPropList.Remove(Language.GetString(LocString.RazorIgnored)))
                    {
                        m.OPLChanged();
                    }
                }
            }
        }

        public override void Save(XmlTextWriter xml)
        {
            xml.WriteAttributeString("enabled", m_Enabled.ToString());
            for (int i = 0; i < m_Chars.Count; i++)
            {
                xml.WriteStartElement("ignore");
                xml.WriteAttributeString("serial", m_Chars[i].ToString());
                try
                {
                    if (m_Names.ContainsKey((Serial) m_Chars[i]))
                    {
                        xml.WriteAttributeString("name", m_Names[(Serial) m_Chars[i]].ToString());
                    }
                }
                catch
                {
                }

                xml.WriteEndElement();
            }
        }

        public override void Load(XmlElement node)
        {
            try
            {
                m_Enabled = Convert.ToBoolean(node.GetAttribute("enabled"));
            }
            catch
            {
                // ignored
            }

            foreach (XmlElement el in node.GetElementsByTagName("ignore"))
            {
                try
                {
                    Serial toAdd = Serial.Parse(el.GetAttribute("serial"));

                    if (!m_Chars.Contains(toAdd))
                    {
                        m_Chars.Add(toAdd);
                    }

                    string name = el.GetAttribute("name");
                    if (!string.IsNullOrEmpty(name))
                    {
                        m_Names.Add(toAdd, name.Trim());
                    }
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}