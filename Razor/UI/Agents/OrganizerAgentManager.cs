using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant.Agents;

namespace Assistant.UI.Agents
{
    class OrganizerAgentManager : IAgentManager, IOrganizerAgentEventHandler
    {
        private OrganizerAgent _agent;
        private AgentControls _controls;

        private ListBox SubList => _controls.SubList;

        private LocString HotBagText => _agent.HotBagSet ? LocString.ClearHB : LocString.SetHB;

        public OrganizerAgentManager(OrganizerAgent agent, AgentControls controls)
        {
            _agent = agent;
            _controls = controls;
            _agent.EventHandler = this;
        }

        public void OnSelected()
        {
            _controls.SetButtonState(0, LocString.AddTarg);
            _controls.SetButtonState(1, HotBagText);
            _controls.SetButtonState(2, LocString.OrganizeNow);
            _controls.SetButtonState(3, LocString.Remove);
            _controls.SetButtonState(4, LocString.Clear);
            _controls.SetButtonState(5, LocString.StopNow);

            RefreshItems();
        }

        private void RefreshItems()
        {
            SubList.SafeAction(s =>
            {
                s.BeginUpdate();
                s.Items.Clear();
                foreach (var item in _agent.Items)
                {
                    s.Items.Add(item);
                }
                s.EndUpdate();
            });
        }

        public void OnButtonPress(int num)
        {
            switch(num)
            {
                case 1:
                    _agent.AddItem();
                    break;

                case 2:
                    _agent.SetHotBag();
                    break;

                case 3:
                    _agent.Organize();
                    break;

                case 4:
                    _agent.RemoveItemAt(SubList.SelectedIndex);
                    break;

                case 5:
                    ClearItems();
                    break;

                case 6:
                    _agent.Stop();
                    break;
            }
        }

        public void ClearItems()
        {
            if (MessageBox.Show(Language.GetString(LocString.Confirm), Language.GetString(LocString.ClearList),
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _agent.ClearItems();
            }
        }

        public void Detach()
        {
            _agent.EventHandler = null;
        }

        public void OnItemAdded(ItemID item)
        {
            SubList.SafeAction(s => s.Items.Add(item));
        }

        public void OnItemRemovedAt(int index)
        {
            SubList.SafeAction(s =>
            {
                if (Utility.IndexInRange(s.Items, index))
                {
                    s.Items.RemoveAt(index);
                }
            });
            
        }

        public void OnItemsCleared()
        {
            RefreshItems();
        }

        public void OnHotBagChanged()
        {
            _controls.SetButtonText(1, HotBagText);
        }

        public void OnTargetAcquired()
        {
            Engine.MainWindow?.SafeAction(s => s.ShowMe());
        }
    }
}
