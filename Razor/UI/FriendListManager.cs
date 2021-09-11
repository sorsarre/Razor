using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Assistant.Core;
using Ultima;

namespace Assistant.UI
{
    class FriendListManager
    {
        private static ComboBox _friendGroups;
        private static ListBox _friendList;
        private static Label _friendFormat;
        private static CheckBox _friendListEnabled;
        private static CheckBox _friendOverheadEnabled;
        private static TextBox _friendOverheadFormat;
        private static TextBox _targetIndicatorFormat;
        private static Form _dialogOwner;

        public static void SetControls(
            Form dialogOwner,
            ComboBox friendGroups,
            ListBox friendList,
            Label friendFormat,
            CheckBox friendListEnabled,
            CheckBox friendOverheadEnabled,
            TextBox friendOverheadFormat,
            TextBox targetIndicatorFormat)
        {
            _dialogOwner = dialogOwner;
            _friendGroups = friendGroups;
            _friendList = friendList;
            _friendFormat = friendFormat;
            _friendListEnabled = friendListEnabled;
            _friendOverheadEnabled = friendOverheadEnabled;
            _friendOverheadFormat = friendOverheadFormat;
            _targetIndicatorFormat = targetIndicatorFormat;
        }

        public static void AddFriendGroup()
        {
            if (InputBox.Show(_dialogOwner, "Add Friend Group", "Enter the name of this new Friend Group"))
            {
                string name = InputBox.GetString();

                if (!string.IsNullOrEmpty(name) && !FriendsManager.FriendsGroupExists(name))
                {
                    FriendsManager.AddFriendGroup(name);
                }
                else
                {
                    MessageBox.Show(_dialogOwner, "Invalid name, or friends group already exists", "Error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        public static void OnFriendGroupSelected()
        {
            if (_friendGroups.SelectedIndex < 0)
                return;

            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;

            _friendListEnabled.SafeAction(s => s.Checked = group.Enabled);

            _friendFormat.SafeAction(s =>
            {
                int hueIdx = group.OverheadFormatHue;

                if (hueIdx > 0 && hueIdx < 3000)
                    s.BackColor = Hues.GetHue(hueIdx - 1).GetColor(HueEntry.TextHueIDX);
                else
                    s.BackColor = SystemColors.Control;

                s.ForeColor = (s.BackColor.GetBrightness() < 0.35 ? Color.White : Color.Black);
            });

            _friendFormat.SafeAction(s => s.Text = group.OverheadFormat);
            _friendOverheadEnabled.SafeAction(s => s.Checked = group.OverheadFormatEnabled);

            RedrawList(group);
        }

        public static void RemoveFriendGroup()
        {
            if (_friendGroups.SelectedIndex < 0)
                return;

            if (MessageBox.Show(_dialogOwner, Language.GetString(LocString.Confirm), Language.GetString(LocString.ClearList),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
                if (FriendsManager.DeleteFriendGroup(group))
                {
                    FriendListManager.RedrawGroups();

                    if (_friendGroups.Items.Count > 0)
                    {
                        _friendGroups.SafeAction(s => s.SelectedIndex = 0);
                    }
                    else
                    {
                        _friendGroups.SafeAction(s => s.Items.Clear());
                    }
                }
            }
        }

        public static void AddFriend()
        {
            if (World.Player == null)
                return;

            if (_friendGroups.SelectedIndex < 0)
                return;

            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
            if (group != null)
            {
                FriendsManager.AddFriendToGroup(group);
            }   
        }

        public static void RemoveFriend()
        {
            if (_friendGroups.SelectedIndex < 0 || _friendList.SelectedIndex < 0)
                return;

            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
            FriendsManager.RemoveFriend(group, _friendList.SelectedIndex);
        }

        public static void ClearFriends()
        {
            if (_friendGroups.SelectedIndex < 0)
                return;

            if (MessageBox.Show(_dialogOwner, Language.GetString(LocString.Confirm), Language.GetString(LocString.ClearList),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
                group.Friends.Clear();

                FriendListManager.RedrawList(group);
            }
        }

        public static void ToggleFriendList(bool enable)
        {
            if (_friendGroups.SelectedIndex < 0)
                return;

            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
            FriendsManager.EnableFriendsGroup(group, enable);
        }

        public static void SetOverheadFormat()
        {
            if (_friendGroups.SelectedIndex < 0)
                return;

            //FriendOverheadFormat
            if (string.IsNullOrEmpty(_friendOverheadFormat.Text))
            {
                _targetIndicatorFormat.SafeAction(s => s.Text = "[Friend]");
            }

            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
            _friendOverheadFormat.SafeAction(s =>
            {
                FriendsManager.SetOverheadFormat(group, s.Text);
            });
        }

        public static void SetOverheadFormatEnabled(bool enable)
        {
            if (_friendGroups.SelectedIndex < 0)
            {
                return;
            }

            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
            FriendsManager.SetOverheadFormatEnabled(group, enable);
        }

        public static void SetOverheadHue()
        {
            if (_friendGroups.SelectedIndex < 0)
            {
                return;
            }

            _friendFormat.SafeAction(s =>
            {
                var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;

                HueEntry h = new HueEntry(group.OverheadFormatHue);

                if (h.ShowDialog(_dialogOwner) != DialogResult.OK)
                {
                    return;
                }

                int hueIdx = h.Hue;

                if (hueIdx > 0 && hueIdx < 3000)
                    s.BackColor = Hues.GetHue(hueIdx - 1).GetColor(HueEntry.TextHueIDX);
                else
                    s.BackColor = Color.White;

                s.ForeColor = (s.BackColor.GetBrightness() < 0.35 ? Color.White : Color.Black);

                FriendsManager.SetOverheadHue(group, hueIdx);
            });
        }

        public static void AddAllMobileAsFriends()
        {
            if (_friendGroups.SelectedIndex < 0)
                return;

            if (World.Player == null)
                return;

            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
            FriendsManager.AddAllMobileAsFriends(group);
        }

        public static void AddAllHumanoidsAsFriends()
        {
            if (_friendGroups.SelectedIndex < 0)
                return;

            if (World.Player == null)
                return;

            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
            FriendsManager.AddAllHumanoidsAsFriends(group);
        }

        public static void ImportFriends()
        {
            if (_friendGroups.SelectedIndex < 0)
                return;

            try
            {
                if (Clipboard.GetText().Contains("!Razor.Friends.Import"))
                {
                    List<string> friendsImport = Clipboard.GetText()
                        .Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None).ToList();

                    friendsImport.RemoveAt(0);

                    foreach (string import in friendsImport)
                    {
                        if (string.IsNullOrEmpty(import))
                            continue;

                        string[] friend = import.Split('#');

                        var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
                        group.AddFriend(friend[0], Serial.Parse(friend[1]));
                    }

                    Clipboard.Clear();
                }
            }
            catch
            {
            }
        }

        public static void ExportFriends()
        {
            if (_friendGroups.SelectedIndex < 0 || _friendList.Items.Count == 0)
                return;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("!Razor.Friends.Import");
            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;

            foreach (var friend in group.Friends)
            {
                sb.AppendLine($"{friend.Name}#{friend.Serial}");
            }

            Clipboard.SetDataObject(sb.ToString(), true);
        }

        public static void RedrawGroups()
        {
            _friendGroups?.SafeAction(s =>
            {
                s.BeginUpdate();
                s.Items.Clear();

                foreach (var friendGroup in FriendsManager.FriendGroups)
                {
                    s.Items.Add(friendGroup);
                }

                s.EndUpdate();

                if (s.Items.Count > 0)
                {
                    s.SelectedIndex = 0;
                }
            });
        }

        public static void RedrawList(FriendsManager.FriendGroup group)
        {
            _friendList?.SafeAction(s =>
            {
                s.BeginUpdate();
                s.Items.Clear();

                if (group != null)
                {
                    foreach (var friend in group.Friends)
                    {
                        s.Items.Add(friend);
                    }
                }

                s.EndUpdate();
            });
        }
    }
}
