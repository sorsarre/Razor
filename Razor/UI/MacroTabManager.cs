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

        public static void SetControls(TreeView treeView, ListBox variablesList)
        {
            _treeView = treeView;
            _variablesList = variablesList;
            MacroManager.OnMacroTreeUpdated += OnMacroTreeUpdated;
            MacroManager.OnMacroWaitReset += ResetWaitDisplay;
            MacroManager.OnMacroPaused += OnMacroPaused;
            MacroManager.OnMacroPlay += OnMacroPlay;
            MacroManager.OnMacroStop += OnMacroStop;
            MacroManager.OnMacroWaitUpdate += SetWaitDisplay;
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

        public static void Select(Macro m, ListBox actionList, Button play, Button rec, CheckBox loop)
        {
            if (m == null)
                return;

            m.DisplayTo(actionList);

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
