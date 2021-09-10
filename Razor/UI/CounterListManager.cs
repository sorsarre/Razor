using System;
using System.Collections;
using System.Windows.Forms;

namespace Assistant.UI
{
    public class CounterListItem : ListViewItem, ICounterEventSink
    {
        public CounterListItem(Counter counter) : base(new string[2])
        {
            Tag = counter;
            Checked = counter.Enabled;
            OnCounterChanged();
            OnCounterUpdated();
            counter.SetEventSink(this);
        }

        private Counter Counter
        {
            get { return Tag as Counter; }
        }

        public void OnCounterChanged()
        {
            SubItems[0].Text = this.Counter.ToString();
        }

        public void OnCounterToggled(bool enabled)
        {
            this.Checked = enabled;
            OnCounterUpdated();
        }

        public void OnCounterUpdated()
        {
            SubItems[1].Text = this.Counter.Enabled ? this.Counter.Amount.ToString() : "";
        }
    }

    public class CounterLVIComparer : IComparer
    {
        private static CounterLVIComparer m_Instance;

        public static CounterLVIComparer Instance
        {
            get
            {
                if (m_Instance == null)
                    m_Instance = new CounterLVIComparer();
                return m_Instance;
            }
        }

        public CounterLVIComparer()
        {
        }

        public int Compare(object a, object b)
        {
            return ((IComparable)(((ListViewItem)a).Tag)).CompareTo(((ListViewItem)b).Tag);
        }
    }

    public class CounterListManager
    {
        private static ListView m_ListView;
        private static IWin32Window m_DialogOwner;

        public static void SetControls(IWin32Window dialogOwner, ListView counterListView)
        {
            m_DialogOwner = dialogOwner;
            m_ListView = counterListView;

            // Fill counters list
            Counter.SupressChecks = true;
            m_ListView.SafeAction(listView =>
            {
                listView.BeginUpdate();
                listView.Items.Clear();
                foreach (var counter in Counter.List)
                {
                    listView.Items.Add(new CounterListItem(counter));
                }
                listView.EndUpdate();
            });
            Counter.SupressChecks = false;
        }

        public static void Configure()
        {
            m_ListView.SafeAction(s =>
            {
                s.BeginUpdate();

                if (Config.GetBool("SortCounters"))
                {
                    s.Sorting = SortOrder.None;
                    s.ListViewItemSorter = CounterLVIComparer.Instance;
                    s.Sort();
                }
                else
                {
                    s.ListViewItemSorter = null;
                    s.Sorting = SortOrder.Ascending;
                }

                s.EndUpdate();
                s.Refresh();
            });
        }

        public static void AddCounter()
        {
            AddCounter dialog = new AddCounter();

            if (dialog.ShowDialog(m_DialogOwner) != DialogResult.OK)
            {
                return;
            }

            var counter = new Counter(
                dialog.NameStr,
                dialog.FmtStr,
                (ushort)dialog.ItemID,
                dialog.Hue,
                dialog.DisplayImage);
            Counter.Register(counter);

            m_ListView.SafeAction(s =>
            {
                s.Items.Add(new CounterListItem(counter));
                s.Sort();
            });
        }

        public static void RemoveCounter()
        {
            m_ListView.SafeAction(listView =>
            {
                if (listView.SelectedItems.Count <= 0)
                    return;

                var selectedItem = listView.SelectedItems[0];

                if (selectedItem.Tag is Counter counter)
                {
                    AddCounter ac = new AddCounter(counter);
                    switch (ac.ShowDialog(m_DialogOwner))
                    {
                        case DialogResult.Abort:
                            listView.Items.Remove(selectedItem);
                            Counter.List.Remove(counter);
                            break;

                        case DialogResult.OK:
                            counter.Set((ushort)ac.ItemID, ac.Hue, ac.NameStr, ac.FmtStr, ac.DisplayImage);
                            break;
                    }
                }
            });
        }

        public static void ToggleCounter(int index, CheckState newValue)
        {
            if (index < 0 || index >= Counter.List.Count || Counter.SupressChecks)
            {
                return;
            }

            m_ListView.SafeAction(s =>
            {
                var item = m_ListView.Items[index];
                var counter = item.Tag as Counter;
                counter.Enabled = (newValue == CheckState.Checked);
                Client.Instance.RequestTitlebarUpdate();
                m_ListView.Sort();
            });
        }
    }
}
