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

        public static void SetControls(TreeView treeView)
        {
            _treeView = treeView;
            MacroManager.OnMacroTreeUpdated += OnMacroTreeUpdated;
        }

        public static void OnMacroTreeUpdated(IList<MacroManager.MacroNode> nodes)
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
