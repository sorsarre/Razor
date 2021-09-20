using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant.Agents;

namespace Assistant.UI.Agents
{
    class IgnoreAgentManager: IAgentManager, IIgnoreAgentEventHandler
    {
        private IgnoreAgent _agent;
        private AgentControls _controls;

        private ListBox SubList => _controls.SubList;

        private LocString EnableText => _agent.Enabled ? LocString.PushDisable : LocString.PushEnable;

        public IgnoreAgentManager(IgnoreAgent agent, AgentControls controls)
        {
            _agent = agent;
            _controls = controls;
            _agent.EventHandler = this;
        }

        public void OnSelected()
        {
            _controls.SetButtonState(0, LocString.AddTarg);
            _controls.SetButtonState(1, LocString.Remove);
            _controls.SetButtonState(2, LocString.RemoveTarg);
            _controls.SetButtonState(3, LocString.ClearList);
            _controls.SetButtonState(4, EnableText);

            RefreshItems();
        }

        private string NamedSerialToString(IgnoreAgent.NamedSerial ns)
        {
            return $"\"{ns.Name}\" {ns.Serial}";
        }

        private void RefreshItems()
        {
            SubList.SafeAction(s =>
            {
                s.BeginUpdate();
                s.Items.Clear();
                foreach (var item in _agent.Items)
                {
                    s.Items.Add(NamedSerialToString(item));
                }
                s.EndUpdate();
            });
        }

        public void OnButtonPress(int num)
        {
            switch(num)
            {
                case 1:
                    _agent.AddToIgnoreList();
                    break;

                case 2:
                    _agent.RemoveItemAt(SubList.SelectedIndex);
                    break;

                case 3:
                    _agent.RemoveFromIgnoreList();
                    break;

                case 4:
                    ClearItems();
                    break;

                case 5:
                    _agent.ToggleAgent();
                    break;
            }
        }

        private void ClearItems()
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

        public void OnTargetAcquired()
        {
            Engine.MainWindow?.SafeAction(s => s.ShowMe());
        }

        public void OnItemAdded(IgnoreAgent.NamedSerial item)
        {
            SubList.SafeAction(s => s.Items.Add(NamedSerialToString(item)));
        }

        public void OnItemsChanged()
        {
            RefreshItems();
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

        public void OnAgentToggled()
        {
            _controls.SetButtonText(4, EnableText);
        }
    }
}
