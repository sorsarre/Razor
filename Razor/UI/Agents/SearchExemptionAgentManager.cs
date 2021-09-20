using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant.Agents;

namespace Assistant.UI.Agents
{
    class SearchExemptionAgentManager : IAgentManager, ISearchExemptionAgentEventHandler
    {
        private SearchExemptionAgent _agent;
        private AgentControls _controls;

        private ListBox SubList => _controls.SubList;

        public SearchExemptionAgentManager(SearchExemptionAgent agent, AgentControls controls)
        {
            _agent = agent;
            _controls = controls;
            _agent.EventHandler = this;
        }

        public void Detach()
        {
            _agent.EventHandler = null;
        }

        public void OnButtonPress(int num)
        {
            switch(num)
            {
                case 1:
                    _agent.AddItem();
                    break;

                case 2:
                    _agent.AddItemType();
                    break;

                case 3:
                    _agent.RemoveItemAt(SubList.SelectedIndex);
                    break;

                case 4:
                    _agent.RemoveItem();
                    break;

                case 5:
                    if (MessageBox.Show(Language.GetString(LocString.Confirm), Language.GetString(LocString.ClearList),
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _agent.ClearItems();
                    }
                    break;
            }
        }

        public void OnSelected()
        {
            _controls.SetButtonState(0, LocString.AddTarg);
            _controls.SetButtonState(1, LocString.AddTargType);
            _controls.SetButtonState(2, LocString.Remove);
            _controls.SetButtonState(3, LocString.RemoveTarg);
            _controls.SetButtonState(4, LocString.ClearList);

            SubList.BeginUpdate();
            SubList.Items.Clear();

            foreach (var item in _agent.Items)
            {
                Item actualItem = null;
                if (item is Serial serial)
                {
                    actualItem = World.FindItem(serial);
                }

                if (actualItem != null)
                {
                    SubList.Items.Add(actualItem.ToString());
                }
                else
                {
                    SubList.Items.Add(item.ToString());
                }
            }

            SubList.EndUpdate();
        }

        public void OnItemAdded(string item)
        {
            SubList.SafeAction(s => s.Items.Add(item));
        }

        public void OnItemRemovedAt(int index)
        {
            SubList.SafeAction(s => {
                if (Utility.IndexInRange(s.Items, index))
                {
                    s.Items.RemoveAt(index);
                }
            });
        }

        public void OnItemsCleared()
        {
            SubList.SafeAction(s => s.Items.Clear());
        }

        public void OnTargetAcquired()
        {
            Engine.MainWindow.SafeAction(s => s.ShowMe());
        }
    }
}
