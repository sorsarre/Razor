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


        private static Agent CurrentAgent => Agents.SelectedItem as Agent;

        public static void OnAgentSelected()
        {
            int idx = Agents.SelectedIndex;
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
            }
        }
    }
}
