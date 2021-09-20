using System.Linq;
using System.Windows.Forms;
using Assistant.Agents;

namespace Assistant.UI.Agents
{
    class RestockAgentManager : IAgentManager, IRestockAgentEventHandler
    {
        private RestockAgent _agent;
        private AgentControls _controls;

        private LocString HotBagText => _agent.HotBagSet ? LocString.SetHB : LocString.ClearHB;
        private ListBox SubList => _controls.SubList;
        public RestockAgentManager(RestockAgent agent, AgentControls controls)
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
                    _agent.RemoveItemAt(SubList.SelectedIndex);
                    break;

                case 3:
                    SetItemAmount();
                    break;

                case 4:
                    ClearItems();
                    break;

                case 5:
                    _agent.ToggleHotBag();
                    break;

                case 6:
                    _agent.Restock();
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

        private void SetItemAmount()
        {
            if (!Utility.IndexInRange(_agent.Items, SubList.SelectedIndex))
            {
                return;
            }

            var index = SubList.SelectedIndex;
            var item = _agent.Items[index];
            if (!InputBox.Show(Engine.MainWindow, Language.GetString(LocString.EnterAmount),
                    Language.GetString(LocString.InputReq), item.Amount.ToString()))
            {
                return;
            }

            item.Amount = InputBox.GetInt(item.Amount);

            RefreshItemList();
            SubList.SafeAction(s => s.SelectedIndex = index);
        }

        private void RefreshItemList()
        {
            SubList.SafeAction(s =>
            {
                s.BeginUpdate();
                s.Items.Clear();
                s.Items.AddRange(_agent.Items.ToArray());
                s.EndUpdate();
            });
        }

        public void OnSelected()
        {
            _controls.SetButtonState(0, LocString.AddTarg);
            _controls.SetButtonState(1, LocString.Remove);
            _controls.SetButtonState(2, LocString.SetAmt);
            _controls.SetButtonState(3, LocString.ClearList);
            _controls.SetButtonState(4, HotBagText);
            _controls.SetButtonState(5, LocString.RestockNow);

            RefreshItemList();

            if (!Client.Instance.AllowBit(FeatureBit.RestockAgent) && Engine.MainWindow != null)
            {
                _controls.Lock();
            }
        }

        public void OnTargetAcquired()
        {
            Engine.MainWindow.SafeAction(s => s.ShowMe());
        }

        public void OnItemAdded(RestockAgent.RestockItem item)
        {
            SubList.SafeAction(s => s.Items.Add(item));
        }

        public void OnItemRemovedAt(int index)
        {
            if (Utility.IndexInRange(SubList.Items, index))
            {
                SubList.Items.RemoveAt(index);
            }
        }

        public void OnItemsCleared()
        {
            RefreshItemList();
        }

        public void OnHotBagChanged()
        {
            _controls.SetButtonText(5, HotBagText);
        }
    }
}
