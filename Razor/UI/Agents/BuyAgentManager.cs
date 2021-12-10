using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant.Agents;
using Assistant.Gumps.Internal;

namespace Assistant.UI.Agents
{
    class BuyAgentManager : IAgentManager, IBuyAgentEventHandler
    {
        private BuyAgent _agent;
        private AgentControls _controls;

        private ListBox SubList => _controls.SubList;

        private LocString EnableText => _agent.Enabled ? LocString.PushDisable : LocString.PushEnable;

        public BuyAgentManager(BuyAgent agent, AgentControls controls)
        {
            _agent = agent;
            _controls = controls;
            _agent.EventHandler = this;
        }

        public void OnSelected()
        {
            _controls.SetButtonState(0, LocString.AddTarg);
            _controls.SetButtonState(1, LocString.Edit);
            _controls.SetButtonState(2, LocString.Remove);
            _controls.SetButtonState(3, LocString.ClearList);
            _controls.SetButtonState(4, EnableText);

            RefreshItems();

            if (!Client.Instance.AllowBit(FeatureBit.BuyAgent) && Engine.MainWindow != null)
            {
                _controls.Lock();
            }
        }

        private void RefreshItems()
        {
            SubList.SafeAction(s =>
            {
                s.BeginUpdate();
                s.Items.Clear();
                s.Items.AddRange(_agent.Items.ToArray());
                s.EndUpdate();
            });
        }

        public void OnButtonPress(int num)
        {
            switch (num)
            {
                case 1:
                    _agent.AddItem();
                    break;

                case 2:
                    EditItem();
                    break;

                case 3:
                    RemoveItem();
                    break;

                case 4:
                    ClearItems();
                    break;

                case 5:
                    _agent.Toggle();
                    break;
            }
        }

        private bool OnItemTargetAmountResponse(int gfx, string input)
        {
            if (ushort.TryParse(input, out ushort count))
            {
                if (count <= 0)
                {
                    return false;
                }

                _agent.Add(new BuyAgent.BuyEntry((ushort)gfx, count));

                return true;
            }

            return false;
        }

        private bool OnItemChangeAmountResponse(int entryId, string input)
        {
            if (ushort.TryParse(input, out ushort amount))
            {
                if (amount <= 0)
                {
                    return false;
                }

                var e = SubList.Items[entryId] as BuyAgent.BuyEntry;
                e.Amount = amount;
                RefreshItems();

                return true;
            }

            return false;
        }

        private void EditItem()
        {
            if (!Utility.IndexInRange(SubList.Items, SubList.SelectedIndex)
                || !Utility.IndexInRange(_agent.Items, SubList.SelectedIndex))
            {
                return;
            }

            var entry = _agent.Items[SubList.SelectedIndex];
            ushort amount = entry.Amount;
            InputDialogGump inputGump = new InputDialogGump(OnItemChangeAmountResponse, m_SubList.SelectedIndex, Language.GetString(LocString.EnterAmount), entry.Amount.ToString());
            inputGump.SendGump();
        }

        private void RemoveItem()
        {
            if (MessageBox.Show(Language.GetString(LocString.Confirm), Language.GetString(LocString.ClearList),
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                _agent.RemoveItemAt(SubList.SelectedIndex);
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

        public void OnItemAdded(BuyAgent.BuyEntry item)
        {
            InputDialogGump inputGump = new InputDialogGump(OnItemTargetAmountResponse, item.Id, Language.GetString(LocString.EnterAmount), "0");
            inputGump.SendGump();
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

        public void OnItemsChanged()
        {
            RefreshItems();
        }

        public void OnAgentToggled()
        {
            _controls.SetButtonText(4, EnableText);
        }

        public void OnTargetAcquired(BuyAgent.BuyEntry item)
        {
            OnTargetAcquired();

            if (InputBox.Show(Engine.MainWindow, Language.GetString(LocString.EnterAmount),
                Language.GetString(LocString.InputReq)))
            {
                ushort count = (ushort)InputBox.GetInt(0);
                if (count <= 0)
                {
                    return;
                }

                item.Amount = count;
                _agent.Add(item);
            }
        }
    }
}
