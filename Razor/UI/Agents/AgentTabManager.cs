using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant.Agents;

namespace Assistant.UI.Agents
{
    public class AgentTabManager
    {
        private static AgentControls _controls;
        private static readonly AgentManagerFactory _factory = new AgentManagerFactory();
        private static IAgentManager _currentManager;
        private static ComboBox Agents => _controls.AgentList;
        private static GroupBox Group => _controls.Group;
        private static ListBox SubList => _controls.SubList;
        private static Button[] Buttons => _controls.Buttons;

        public static void SetControls(ComboBox agents, GroupBox group, ListBox subList, params Button[] buttons)
        {
            _controls = new AgentControls
            {
                AgentList = agents,
                Group = group,
                SubList = subList,
                Buttons = buttons
            };
        }

        public static void Redraw()
        {
            int sel = Agents.SelectedIndex;
            Agents.Visible = true;
            Agents.BeginUpdate();
            Agents.Items.Clear();
            Agents.SelectedIndex = -1;

            foreach (var button in Buttons)
            {
                button.Visible = false;
            }

            Agents.Items.AddRange(Agent.List.ToArray());
            Agents.EndUpdate();

            Group.Visible = false;
            if (sel >= 0 && sel < Agents.Items.Count)
            {
                Agents.SelectedIndex = sel;
            }
        }

        public static void OnButtonPress(int index)
        {
            if (_currentManager != null)
                _currentManager.OnButtonPress(index);
            else
                CurrentAgent.OnButtonPress(index);
        }

        private static Agent CurrentAgent => Agents.SelectedItem as Agent;

        public static void OnAgentSelected()
        {
            foreach (var button in Buttons)
            {
                button.Visible = false;
                button.Text = "";
            }

            Group.Visible = false;
            SubList.Visible = false;

            _controls.Unlock();

            if (CurrentAgent != null)
            {
                Group.Visible = true;
                Group.Text = CurrentAgent.Name;
                SubList.Visible = true;
                _currentManager?.Detach();
                _currentManager = _factory.CreateAgentManager(CurrentAgent, _controls);
                if (_currentManager != null)
                    _currentManager?.OnSelected();
                else
                    CurrentAgent.OnSelected(SubList, Buttons);
            }
        }
    }
}
