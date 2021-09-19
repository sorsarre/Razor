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
        private static ComboBox _agents;
        private static GroupBox _group;
        private static ListBox _subList;
        private static Button[] _buttons;

        public static void SetControls(ComboBox agents, GroupBox group, ListBox subList, params Button[] buttons)
        {
            _agents = agents;
            _group = group;
            _subList = subList;
            _buttons = buttons;
        }

        public static void Redraw()
        {
            int sel = _agents.SelectedIndex;
            _agents.Visible = true;
            _agents.BeginUpdate();
            _agents.Items.Clear();
            _agents.SelectedIndex = -1;

            foreach (var button in _buttons)
            {
                button.Visible = false;
            }

            _agents.Items.AddRange(Agent.List.ToArray());
            _agents.EndUpdate();

            _group.Visible = false;
            if (sel >= 0 && sel < _agents.Items.Count)
            {
                _agents.SelectedIndex = sel;
            }
        }

        public static void OnAgentSelected()
        {
            int idx = _agents.SelectedIndex;
            foreach (var button in _buttons)
            {
                button.Visible = false;
                button.Text = "";
                Engine.MainWindow.SafeAction(s => s.UnlockControl(button));
            }

            _group.Visible = false;
            _subList.Visible = false;
            Engine.MainWindow.SafeAction(s => s.UnlockControl(_subList));

            Agent a = null;
            if (idx >= 0 && idx < Agent.List.Count)
            {
                a = Agent.List[idx] as Agent;
            }

            if (a != null)
            {
                _group.Visible = true;
                _group.Text = a.Name;
                _subList.Visible = true;
                a.OnSelected(_subList, _buttons);
            }
        }
    }
}
