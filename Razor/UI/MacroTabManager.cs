using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant.Macros;

namespace Assistant.UI
{
    class MacroTabManager
    {
        private static TreeView _treeView;
        private static ListBox _variablesList;
        private static ListBox _actionList;
        private static Macro _displayedMacro;

        public static void SetControls(TreeView treeView, ListBox variablesList, ListBox actionList)
        {
            _treeView = treeView;
            _variablesList = variablesList;
            _actionList = actionList;
            MacroManager.OnMacroTreeUpdated += OnMacroTreeUpdated;
            MacroManager.OnMacroWaitReset += ResetWaitDisplay;
            MacroManager.OnMacroPaused += OnMacroPaused;
            MacroManager.OnMacroPlay += OnMacroPlay;
            MacroManager.OnMacroStop += OnMacroStop;
            MacroManager.OnMacroWaitUpdate += SetWaitDisplay;
            Macro.OnMacroUpdated += OnMacroUpdated;
            Macro.OnMacroCurrentAction += OnMacroCurrentAction;
            Macro.OnMacroActionAdded += OnMacroActionAdded;
            MacroVariables.OnItemsChanged += DisplayMacroVariables;
        }

        private static void SetWaitDisplay(string text)
        {
            Engine.MainWindow.SafeAction(s =>
            {
                s.WaitDisplay.Text = text;
            });
        }

        private static void ResetWaitDisplay()
        {
            SetWaitDisplay(String.Empty);
        }

        private static void OnMacroPaused()
        {
            SetWaitDisplay("Paused");
        }

        private static void OnMacroPlay(Macro m)
        {
            Engine.MainWindow.SafeAction(s => s.PlayMacro(m));
        }

        private static void OnMacroStop()
        {
            ResetWaitDisplay();
            Engine.MainWindow.SafeAction(s => s.OnMacroStop());
        }

        private static void OnMacroActionAdded(Macro m, int at, MacroAction action)
        {
            if (m != _displayedMacro)
            {
                return;
            }

            _actionList.SafeAction(s =>
            {
                s.Items.Insert(at, action);
            });
        }

        private static void OnMacroCurrentAction(Macro m, int index)
        {
            if (m != _displayedMacro)
            {
                return;
            }

            _actionList.SafeAction(s =>
            {
                if (index >= 0 && index < s.Items.Count)
                {
                    s.SelectedIndex = index;
                }
                else
                {
                    s.SelectedIndex = -1;
                }
            });
            
        }

        public static void DisplayMacro(Macro m)
        {
            _displayedMacro = m;
            _actionList.SafeAction(s => s.Items.Clear());

            if (!m.Loaded)
                m.Load();

            _actionList.SafeAction(s =>
            {
                s.BeginUpdate();
                if (m.Actions.Count > 0)
                    s.Items.AddRange((object[])m.Actions.ToArray(typeof(object)));
                if (m.Playing && m.CurrentAction >= 0 && m.CurrentAction < m.Actions.Count)
                    s.SelectedIndex = m.CurrentAction;
                else
                    s.SelectedIndex = -1;
                s.EndUpdate();
            });
        }

        public static void OnMacroUpdated(Macro m)
        {
            if (m != _displayedMacro)
            {
                return;
            }

            _actionList.SafeAction(list =>
            {
                var index = list.SelectedIndex;
                DisplayMacro(m);
                try
                {
                    list.SelectedIndex = index;
                }
                catch
                {
                }
            });
        }

        public static void Select(Macro m, ListBox actionList, Button play, Button rec, CheckBox loop)
        {
            if (m == null)
                return;

            DisplayMacro(m);

            if (MacroManager.Recording)
            {
                play.Enabled = false;
                play.Text = "Play";
                rec.Enabled = true;
                rec.Text = "Stop";
            }
            else
            {
                play.Enabled = true;
                if (m.Playing)
                {
                    play.Text = "Stop";
                    rec.Enabled = false;
                }
                else
                {
                    play.Text = "Play";
                    rec.Enabled = true;
                }

                rec.Text = "Record";
                loop.Checked = m.Loop;
            }
        }

        public static void DisplayMacroVariables()
        {
            _variablesList.SafeAction(list =>
            {
                list.BeginUpdate();
                list.Items.Clear();

                foreach (MacroVariables.MacroVariable at in MacroVariables.MacroVariableList)
                {
                    list.Items.Add($"${at.Name} ({at.TargetInfo.Serial})");
                }

                list.EndUpdate();
                list.Refresh();
                list.Update();
            });
        }

        private static void OnMacroTreeUpdated(IList<MacroManager.MacroNode> nodes)
        {
            _treeView.SafeAction(tree =>
            {
                tree.BeginUpdate();
                tree.Nodes.Clear();
                Recurse(tree.Nodes, nodes);
                tree.EndUpdate();
                tree.Refresh();
                tree.Update();
            });
        }

        private static void Recurse(TreeNodeCollection nodes, IList<MacroManager.MacroNode> macroNodes)
        {
            foreach (var macroNode in macroNodes)
            {
                var node = new TreeNode(macroNode.Text);
                node.Tag = macroNode.Tag;
                nodes.Add(node);

                if (macroNode.IsDirectory)
                {
                    Recurse(node.Nodes, macroNode.Children);
                }
            }
        }
    }
}
