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
using System.Diagnostics;
using System.Linq;
using System.Xml;
using Assistant.UI;

namespace Assistant.Core
{
    public static class FriendsManager
    {
        private static GroupHotKeyManager _hotkeyManager = new GroupHotKeyManager();

        public static List<FriendGroup> FriendGroups = new List<FriendGroup>();

        public delegate void OnGroupsChangedCallback();
        public delegate void OnFriendsChangedCallback(FriendGroup group);
        public delegate void OnFriendTargetCallback();

        public static OnGroupsChangedCallback OnGroupsChanged { get; set; }
        public static OnFriendsChangedCallback OnFriendsChanged { get; set; }
        public static OnFriendTargetCallback OnFriendTarget { get; set; }

        public static void AddFriendToGroup(FriendGroup group)
        {
            World.Player.SendMessage(MsgLevel.Friend, LocString.TargFriendAdd);
            Targeting.OneTimeTarget((location, serial, loc, gfx) => {
                OnAddFriendTarget(group, location, serial, loc, gfx);
            });
        }

        public class Friend
        {
            public string Name { get; set; }
            public Serial Serial { get; set; }

            public override string ToString()
            {
                return $"{Name} ({Serial})";
            }
        }

        private class GroupHotKeyManager
        {
            private void SafeAction(Action action)
            {
                if (Engine.MainWindow == null)
                {
                    action?.Invoke();
                }
                else
                {
                    Engine.MainWindow.SafeAction(s => action?.Invoke());
                }
            }

            public void Add(FriendGroup group)
            {
                SafeAction(() =>
                {
                    HotKey.Add(HKCategory.Friends, HKSubCat.None, $"Add Target To: {group.GroupName}",
                        () => AddFriendToGroup(group));
                    HotKey.Add(HKCategory.Friends, HKSubCat.None, $"Toggle Group: {group.GroupName}", group.ToggleFriendGroup);
                    HotKey.Add(HKCategory.Friends, HKSubCat.None, $"Add All Mobiles: {group.GroupName}",
                        () => AddAllMobileAsFriends(group));
                    HotKey.Add(HKCategory.Friends, HKSubCat.None, $"Add All Humanoids: {group.GroupName}",
                        () => AddAllHumanoidsAsFriends(group));
                });
            }

            public void Remove(FriendGroup group)
            {
                SafeAction(() =>
                {
                    HotKey.Remove($"Add Target To: {group.GroupName}");
                    HotKey.Remove($"Toggle Group: {group.GroupName}");
                    HotKey.Remove($"Add All Mobiles: {group.GroupName}");
                    HotKey.Remove($"Add All Humanoids: {group.GroupName}");
                });
            }
        }

        public class FriendGroup
        {
            public string GroupName { get; set; }
            public bool Enabled { get; set; }
            public List<Friend> Friends { get; set; }

            public string OverheadFormat { get; set; }
            public int OverheadFormatHue { get; set; }
            public bool OverheadFormatEnabled { get; set; }

            public FriendGroup()
            {
                Friends = new List<Friend>();
            }

            public void ToggleFriendGroup()
            {
                if (Enabled)
                {
                    World.Player.SendMessage(MsgLevel.Warning,
                        $"Friend group '{GroupName}' ({Friends.Count} friends) has been 'Disabled'");
                    Enabled = false;
                }
                else
                {
                    World.Player.SendMessage(MsgLevel.Info,
                        $"Friend group '{GroupName}' ({Friends.Count} friends) has been 'Enabled'");
                    Enabled = true;
                }
            }

            public override string ToString()
            {
                return $"{GroupName}";
            }

            public bool AddFriend(string friendName, Serial friendSerial)
            {
                if (Friends.Any(f => f.Serial == friendSerial) == false)
                {
                    Friend newFriend = new Friend
                    {
                        Name = friendName,
                        Serial = friendSerial
                    };

                    Friends.Add(newFriend);
                    World.Player?.SendMessage(MsgLevel.Friend, $"Added '{friendName}' to '{GroupName}'");

                    return true;
                }

                return false;
            }
        }

        private static bool IsValidFriendTarget(Mobile mobile)
        {
            return !IsFriend(mobile.Serial) && mobile.Serial.IsMobile && mobile.Serial != World.Player.Serial;
        }

        private static void OnAddFriendTarget(FriendGroup group, bool location, Serial serial, Point3D loc, ushort gfx)
        {
            OnFriendTarget?.Invoke();

            if (!location && serial.IsMobile && serial != World.Player.Serial)
            {
                Mobile m = World.FindMobile(serial);

                if (m == null)
                    return;

                if (group.AddFriend(m.Name, serial))
                {
                    m.ObjPropList.Add(Language.GetString(LocString.RazorFriend));
                    m.OPLChanged();

                    OnFriendsChanged?.Invoke(group);
                }
                else
                {
                    World.Player.SendMessage(MsgLevel.Warning, $"'{m.Name}' is already in '{group.GroupName}'");
                }
            }
        }

        public static void AddAllMobileAsFriends(FriendGroup group)
        {
            List<Mobile> mobiles = World.MobilesInRange(12);

            foreach (Mobile mobile in mobiles)
            {
                if (IsValidFriendTarget(mobile))
                {
                    if (group.AddFriend(mobile.Name, mobile.Serial))
                    {
                        mobile.ObjPropList.Add(Language.GetString(LocString.RazorFriend));
                        mobile.OPLChanged();
                    }
                }
            }

            OnFriendsChanged?.Invoke(group);
        }

        public static void AddAllHumanoidsAsFriends(FriendGroup group)
        {
            List<Mobile> mobiles = World.MobilesInRange(12);

            foreach (Mobile mobile in mobiles)
            {
                if (IsValidFriendTarget(mobile) && mobile.IsHuman)
                {
                    if (group.AddFriend(mobile.Name, mobile.Serial))
                    {
                        mobile.ObjPropList.Add(Language.GetString(LocString.RazorFriend));
                        mobile.OPLChanged();
                    }
                }
            }

            OnFriendsChanged?.Invoke(group);
        }

        public static bool IsFriendOverhead(Serial serial, ref FriendGroup group)
        {
            // Check if they have treat party as friends enabled and check the party if so
            if (Config.GetBool("AutoFriend") && PacketHandlers.Party.Contains(serial))
                return true;
            
            // Loop through each friends group that is enabled
            foreach (FriendGroup friendGroup in FriendGroups)
            {
                if (friendGroup.Enabled && friendGroup.Friends.Any(f => f.Serial == serial))
                {
                    group = friendGroup;

                    return true;
                }
            }

            return false;
        }

        public static bool IsFriend(Serial serial)
        {
            // Check if they have treat party as friends enabled and check the party if so
            if (Config.GetBool("AutoFriend") && PacketHandlers.Party.Contains(serial))
                return true;

            // Loop through each friends group that is enabled
            foreach (FriendGroup friendGroup in FriendGroups)
            {
                if (friendGroup.Enabled && friendGroup.Friends.Any(f => f.Serial == serial))
                {
                    return true;
                }
            }

            return false;
        }

        public static void EnableFriendsGroup(FriendGroup group, bool enabled)
        {
            if (group != null)
            {
                group.Enabled = enabled;
            }
        }

        public static bool FriendsGroupExists(string group)
        {
            return FriendGroups.Any(g =>
            {
                return g.GroupName.ToLower().Equals(group.ToLower());
            });
        }

        public static bool RemoveFriend(FriendGroup group, int index)
        {
            if (group != null)
            {
                group.Friends.RemoveAt(index);
                OnFriendsChanged?.Invoke(group);
                return true;
            }

            return false;
        }

        public static bool DeleteFriendGroup(FriendGroup group)
        {
            _hotkeyManager.Remove(group);
            if (FriendGroups.Remove(group))
            {
                OnGroupsChanged?.Invoke();
                return true;
            }

            return false;
        }

        public static void AddFriendGroup(string group)
        {
            FriendGroup friendGroup = new FriendGroup
            {
                Enabled = true,
                GroupName = group,
                Friends = new List<Friend>(),
                OverheadFormatHue = 63,
                OverheadFormat = "[Friend]",
                OverheadFormatEnabled = true
            };

            _hotkeyManager.Add(friendGroup);
            FriendGroups.Add(friendGroup);

            OnGroupsChanged?.Invoke();
        }

        public static void SetOverheadFormat(FriendGroup group, string format)
        {
            if (group != null)
            {
                group.OverheadFormat = format;
            }
        }

        public static void SetOverheadHue(FriendGroup group, int hue)
        {
            if (group != null)
            {
                group.OverheadFormatHue = hue;
            }
        }

        public static void SetOverheadFormatEnabled(FriendGroup group, bool enabled)
        {
            if (group != null)
            {
                group.OverheadFormatEnabled = enabled;
            }
        }

        public static void ShowOverhead(Mobile mobile)
        {
            FriendGroup group = null;

            if (IsFriendOverhead(mobile.Serial, ref group))
            {
                if (group == null && Config.GetBool("ShowPartyFriendOverhead")) // If they are a friend with no group, must be a party member
                {
                    mobile.OverheadMessage(63, "[Party-Friend]");
                }
                else if (group != null && group.OverheadFormatEnabled)
                {
                    mobile.OverheadMessage(group.OverheadFormatHue, group.OverheadFormat);
                }
            }
        }

        public static void Save(XmlTextWriter xml)
        {
            foreach (var friendGroup in FriendGroups)
            {
                xml.WriteStartElement("group");
                xml.WriteAttributeString("name", friendGroup.GroupName);
                xml.WriteAttributeString("enabled", friendGroup.Enabled.ToString());
                xml.WriteAttributeString("overheadformat", friendGroup.OverheadFormat);
                xml.WriteAttributeString("overheadhue", friendGroup.OverheadFormatHue.ToString());
                xml.WriteAttributeString("overheadenabled", friendGroup.OverheadFormatEnabled.ToString());

                foreach (var friend in friendGroup.Friends)
                {
                    xml.WriteStartElement("friend");
                    xml.WriteAttributeString("name", friend.Name);
                    xml.WriteAttributeString("serial", friend.Serial.ToString());
                    xml.WriteEndElement();
                }

                xml.WriteEndElement();
            }
        }

        public static void Load(XmlElement node)
        {
            ClearAll();

            try
            {
                foreach (XmlElement el in node.GetElementsByTagName("group"))
                {
                    FriendGroup friendGroup = new FriendGroup
                    {
                        GroupName = el.GetAttribute("name"),
                        Enabled = Convert.ToBoolean(el.GetAttribute("enabled"))
                    };

                    // Newer versions didn't have these, so it will cause an error when loading for the first time
                    // If any fail, just set defaults
                    try
                    {
                        friendGroup.OverheadFormat = el.GetAttribute("overheadformat");
                        friendGroup.OverheadFormatHue = Convert.ToInt32(el.GetAttribute("overheadhue"));
                        friendGroup.OverheadFormatEnabled = Convert.ToBoolean(el.GetAttribute("overheadenabled"));
                    }
                    catch
                    {
                        friendGroup.OverheadFormat = "[Friend]";
                        friendGroup.OverheadFormatHue = 63;
                        friendGroup.OverheadFormatEnabled = true;
                    }

                    _hotkeyManager.Add(friendGroup);

                    foreach (XmlElement friendEl in el.GetElementsByTagName("friend"))
                    {
                        try
                        {
                            Friend friend = new Friend
                            {
                                Name = friendEl.GetAttribute("name"),
                                Serial = Serial.Parse(friendEl.GetAttribute("serial"))
                            };

                            friendGroup.Friends.Add(friend);
                        }
                        catch
                        {
                            // ignore this bad record, most likely a bad serial
                        }
                    }

                    FriendGroups.Add(friendGroup);
                }

                OnGroupsChanged?.Invoke();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
        }

        public static void ClearAll()
        {
            foreach (FriendGroup friendGroup in FriendGroups)
            {
                _hotkeyManager.Remove(friendGroup);
            }

            FriendGroups.Clear();
        }
    }
}