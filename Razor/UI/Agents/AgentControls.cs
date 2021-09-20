using System.Windows.Forms;

namespace Assistant.UI.Agents
{
    class AgentControls
    {
        public ComboBox AgentList { get; set; }
        public GroupBox Group { get; set; }
        public ListBox SubList { get; set; }
        public Button[] Buttons { get; set; }

        public void SetButtonState(int index, LocString text)
        {

            SetButtonState(index, Language.GetString(text));
        }

        public void SetButtonState(int index, string text)
        {
            Buttons[index].SafeAction(s =>
            {
                s.Visible = true;
                s.Text = text;
            });
        }

        public void SetButtonText(int index, string text)
        {
            Buttons[index].SafeAction(s => s.Text = text);
        }

        public void SetButtonText(int index, LocString text)
        {
            SetButtonText(index, Language.GetString(text));
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
