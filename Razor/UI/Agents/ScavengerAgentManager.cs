using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant.Agents;

namespace Assistant.UI.Agents
{
    class ScavengerAgentManager : IAgentManager, IScavengerAgentEventHandler
    {
        private ScavengerAgent _agent;
        private AgentControls _controls;

        private ListBox SubList => _controls.SubList;

        private LocString EnableText => _agent.Enabled ? LocString.PushDisable : LocString.PushEnable;

        private Button EnableButton => _controls.Buttons[5];

        public ScavengerAgentManager(ScavengerAgent agent, AgentControls controls)
        {
            _agent = agent;
            _controls = controls;
            _agent.EventHandler = this;
        }

        public void OnSelected()
        {
            _controls.SetButtonState(0, LocString.AddTarg);
            _controls.SetButtonState(1, LocString.Remove);
            _controls.SetButtonState(2, LocString.SetHB);
            _controls.SetButtonState(3, LocString.ClearList);
            _controls.SetButtonState(4, LocString.ClearScavCache);
            _controls.SetButtonState(5, EnableText);

            SubList.BeginUpdate();
            SubList.Items.Clear();

            foreach (var item in _agent.Items)
            {
                SubList.Items.Add(item);
            }

            SubList.EndUpdate();
        }

        public void OnButtonPress(int num)
        {
            switch (num)
            {
                case 1:
                    _agent.AddItem();
                    break;

                case 2:
                    _agent.RemoveItemAt(SubList.SelectedIndex);
                    break;

                case 3:
                    _agent.SetHotBag();
                    break;

                case 4:
                    if (MessageBox.Show(Language.GetString(LocString.Confirm), Language.GetString(LocString.ClearList),
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _agent.ClearItems();
                    }
                    break;

                case 5:
                    _agent.ClearCache();
                    break;

                case 6:
                    _agent.ToggleAgent();
                    break;
            }
        }

        public void Detach()
        {
            _agent.EventHandler = null;
        }

        public void OnAgentToggled()
        {
            EnableButton.SafeAction(s => s.Text = Language.GetString(EnableText));
        }

        public void OnItemAdded(ItemID itemID)
        {
            SubList.SafeAction(s => s.Items.Add(itemID));
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
            SubList.SafeAction(s => s.Items.Clear());
        }

        public void OnTargetAcquired()
        {
            Engine.MainWindow.SafeAction(s => s.ShowMe());
        }
    }
}
