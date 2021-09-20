using Assistant.Agents;
using System.Windows.Forms;

namespace Assistant.UI.Agents
{
    class SellAgentManager : IAgentManager, ISellAgentEventHandler
    {
        private SellAgent _agent;
        private AgentControls _controls;

        private Button EnableButton => _controls.Buttons[5];
        private Button HotButton => _controls.Buttons[2];
        private Button AmountButton => _controls.Buttons[4];

        private ListBox SubList => _controls.SubList;

        private string AmountText => Language.Format(LocString.SellAmount, Config.GetInt("SellAgentMax"));
        private LocString ToggleText => _agent.Enabled ? LocString.PushDisable : LocString.PushEnable;

        private LocString HotBagText => _agent.HotBagSet ? LocString.SetHB : LocString.ClearHB;

        public SellAgentManager(SellAgent agent, AgentControls controls)
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
            switch (num)
            {
                case 1:
                    _agent.AddItem();
                    break;

                case 2:
                    if (SubList.SelectedIndex >= 0)
                    {
                        _agent.RemoveItemAt(SubList.SelectedIndex);
                    }
                    break;

                case 3:
                    _agent.ToggleHotBag();
                    break;

                case 4:
                    if (MessageBox.Show(Language.GetString(LocString.Confirm), Language.GetString(LocString.ClearList),
                           MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        _agent.ClearItems();
                    }
                    break;

                case 5:
                    if (InputBox.Show(Language.GetString(LocString.EnterAmount)))
                    {
                        _agent.SetAmount(InputBox.GetInt(100));
                    }
                    break;

                case 6:
                    _agent.Toggle();
                    break;
            }
        }

        public void OnSelected()
        {
            _controls.SetButtonState(0, LocString.AddTarg);
            _controls.SetButtonState(1, LocString.Remove);
            _controls.SetButtonState(2, HotBagText);
            _controls.SetButtonState(3, LocString.Clear);
            _controls.SetButtonState(4, AmountText);
            _controls.SetButtonState(5, ToggleText);

            SubList.BeginUpdate();
            SubList.Items.Clear();
            foreach (var item in _agent.Items)
            {
                SubList.Items.Add((ItemID)item);
            }
            SubList.EndUpdate();

            if (!Client.Instance.AllowBit(FeatureBit.SellAgent) && Engine.MainWindow != null)
            {
                _controls.Lock();
            }
        }

        public void OnItemAdded(ushort item)
        {
            SubList.SafeAction(s => s.Items.Add((ItemID)item));
        }

        public void OnItemsCleared()
        {
            SubList.SafeAction(s => s.Items.Clear());
        }

        public void OnItemRemovedAt(int index)
        {
            SubList.SafeAction(s =>
            {
                if (Utility.IndexInRange(s.Items, index))
                {
                    s.Items.RemoveAt(index);
                    s.SelectedIndex = -1;
                }
            });
        }

        public void OnHotBagChanged()
        {
            HotButton.SafeAction(s => s.Text = Language.GetString(HotBagText));
        }

        public void OnAgentToggled()
        {
            EnableButton.SafeAction(s => s.Text = Language.GetString(ToggleText));
        }

        public void OnAmountChanged()
        {
            AmountButton.SafeAction(s => s.Text = AmountText);
        }

        public void OnTargetAcquired()
        {
            Engine.MainWindow.SafeAction(s => s.ShowMe());
        }
    }
}
