using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Assistant.UI.Agents
{
    class AgentControls
    {
        public ComboBox AgentList { get; set; }
        public GroupBox Group { get; set; }
        public ListBox SubList { get; set; }
        public Button[] Buttons { get; set; }

        public void SetButtonState(int index, bool visible, LocString text)
        {
            Buttons[index].Visible = visible;
            Buttons[index].Text = Language.GetString(text);
        }

        public void Lock()
        {
            foreach (var button in Buttons)
            {
                Engine.MainWindow.SafeAction(s => s.LockControl(button));
            }

            Engine.MainWindow.SafeAction(s => s.LockControl(SubList));
        }

        public void Unlock()
        {
            foreach (var button in Buttons)
            {
                Engine.MainWindow.SafeAction(s => s.UnlockControl(button));
            }

            Engine.MainWindow.SafeAction(s => s.UnlockControl(SubList));
        }
    }
}
