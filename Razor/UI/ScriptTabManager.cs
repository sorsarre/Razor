using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Assistant.Scripts;
using FastColoredTextBoxNS;

namespace Assistant.UI
{
    class ScriptTabManager
    {
        private static TreeView _scriptTree;
        private static ListBox _variableList;
        private static ScriptEditorManager _editorManager = new ScriptEditorManager();
        private static int _currentLine = 0;

        public static ScriptEditorManager EditorManager => _editorManager;

        public static void SetControls(
            FastColoredTextBox scriptEditor,
            TreeView scriptTree,
            ListBox variableList)
        {
            _scriptTree = scriptTree;
            _variableList = variableList;

            EditorManager.SetControl(scriptEditor);

            ScriptManager.OnScriptError += OnScriptError;
            ScriptManager.OnScriptStarted += OnScriptStarted;
            ScriptManager.OnScriptStopped += OnScriptStopped;
            ScriptManager.OnScriptLineUpdate += OnScriptLineUpdate;
            ScriptManager.OnScriptPlayRequested += OnScriptPlayRequested;
            ScriptManager.OnAddToScript += EditorManager.AddToScript;
            ScriptManager.OnScriptsLoaded += OnScriptsLoaded;
            ScriptVariables.OnItemsChanged += RedrawScriptVariables;
        }

        public static void OnScriptPlayRequested()
        {
            EditorManager.ClearAllHighlightLines();
        }

        public static void OnScriptStarted()
        {
            Assistant.Engine.MainWindow.LockScriptUI(true);
            Assistant.Engine.RazorScriptEditorWindow?.LockScriptUI(true);
        }

        public static void OnScriptStopped()
        {
            Assistant.Engine.MainWindow.LockScriptUI(false);
            Assistant.Engine.RazorScriptEditorWindow?.LockScriptUI(false);
            EditorManager.ClearHighlightLine(ScriptEditorManager.HighlightType.Execution);
        }

        public static void OnScriptError(Exception ex)
        {
            EditorManager.SetHighlightLine(_currentLine, ScriptEditorManager.HighlightType.Error);
        }

        public static void OnScriptLineUpdate(int line)
        {
            _currentLine = line;
            EditorManager.UpdateLineNumber(line);
        }

        private static void Recurse(TreeNodeCollection nodes, IList<ScriptManager.ScriptTreeNode> treeNodes)
        {
            foreach (var scriptNode in treeNodes)
            {
                TreeNode node = null;

                if (scriptNode.Tag != null)
                {
                    node = new TreeNode(scriptNode.Text)
                    {
                        Tag = scriptNode.Tag
                    };

                    nodes.Add(node);

                    if (scriptNode.Tag is string)
                    {
                        Recurse(node.Nodes, scriptNode.Children);
                    }
                }
            }
        }

        public static void OnScriptsLoaded(IList<ScriptManager.ScriptTreeNode> treeNodes)
        {
            _scriptTree.SafeAction(s =>
            {
                s.BeginUpdate();
                s.Nodes.Clear();
                Recurse(s.Nodes, treeNodes);
                s.EndUpdate();
                s.Refresh();
                s.Update();
            });
        }

        public static void RedrawScripts()
        {
            ScriptManager.ReloadScripts();
            RedrawScriptVariables();
        }

        public static TreeNode GetScriptDirNode()
        {
            if (_scriptTree?.SelectedNode == null)
            {
                return null;
            }

            if (_scriptTree.SelectedNode.Tag is string)
            {
                return _scriptTree.SelectedNode;
            }

            if (!(_scriptTree.SelectedNode.Parent?.Tag is string))
            {
                return null;
            }

            return _scriptTree.SelectedNode.Parent;
        }

        private static void AddScriptNode(TreeNode node)
        {
            if (node == null)
            {
                _scriptTree.Nodes.Add(node);
            }
            else
            {
                node.Nodes.Add(node);
            }

            _scriptTree.SelectedNode = node;
        }

        public static void RedrawScriptVariables()
        {
            _variableList?.SafeAction(s =>
            {
                s.BeginUpdate();
                s.Items.Clear();

                foreach (ScriptVariables.ScriptVariable at in ScriptVariables.ScriptVariableList)
                {
                    s.Items.Add($"'{at.Name}' ({at.TargetInfo.Serial})");
                }

                s.EndUpdate();
                s.Refresh();
                s.Update();
            });
        }
    }
}
