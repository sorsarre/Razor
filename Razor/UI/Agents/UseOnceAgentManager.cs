using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant.Agents;

namespace Assistant.UI.Agents
{
    class UseOnceAgentManager : IAgentManager, IUseOnceAgentEventHandler
    {
        private UseOnceAgent _agent;
        private AgentControls _controls;

        public UseOnceAgentManager(UseOnceAgent agent, AgentControls controls)
        {
            _agent = agent;
            _controls = controls;
            agent.EventHandler = this;
        }

        public void Detach()
        {
            _agent.EventHandler = null;
        }

        public void OnButtonPress(int num)
        {
            switch (num)
            {
                case 1:
                    _agent?.AddItem();
                    break;

                case 2:
                    _agent?.AddContainer();
                    break;

                case 3:
                    _agent?.RemoveItem();
                    break;

                case 4:
                    if (MessageBox.Show(Language.GetString(LocString.Confirm), Language.GetString(LocString.ClearList),
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _controls.SubList.Items.Clear();
                        _agent?.ClearItems();
                    }
                    break;
            }
        }

        public void OnItemAdded(Item item)
        {
            _controls.SubList.Items.Add(item);
        }

        public void OnItemRemovedAt(int index)
        {
            if (0 <= index && index < _controls.SubList.Items.Count)
            {
                _controls.SubList.Items.RemoveAt(index);
            }
        }

        public void OnSelected()
        {
            _controls.SetButtonState(0, true, LocString.AddTarg);
            _controls.SetButtonState(1, true, LocString.AddContTarg);
            _controls.SetButtonState(2, true, LocString.RemoveTarg);
            _controls.SetButtonState(3, true, LocString.ClearList);

            _controls.SubList.BeginUpdate();
            _controls.SubList.Items.Clear();

            _agent?.RefreshItems();

            _controls.SubList.Items.AddRange(_agent?.Items.ToArray());
            _controls.SubList.EndUpdate();

            if (!Client.Instance.AllowBit(FeatureBit.UseOnceAgent) && Engine.MainWindow != null)
            {
                _controls.Lock();
            }
        }

        public void OnTargetAcquired()
        {
            if (Config.GetBool("AlwaysOnTop"))
            {
                Engine.MainWindow.SafeAction(s => s.ShowMe());
            }
        }
    }
}
