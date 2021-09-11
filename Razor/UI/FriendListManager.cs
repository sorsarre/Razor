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
        private static Form _dialogOwner;

        public static void SetControls(Form dialogOwner, ComboBox friendGroups, ListBox friendList, Label friendFormat)
        {
            _dialogOwner = dialogOwner;
            _friendGroups = friendGroups;
            _friendList = friendList;
            _friendFormat = friendFormat;
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

        public static void RemoveFriend()
        {
            if (_friendGroups.SelectedIndex < 0 || _friendList.SelectedIndex < 0)
                return;

            var group = _friendGroups.SelectedItem as FriendsManager.FriendGroup;
            FriendsManager.RemoveFriend(group, _friendList.SelectedIndex);
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
