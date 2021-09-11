using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Assistant.Scripts.Engine;
using Assistant.Scripts;
using FastColoredTextBoxNS;

namespace Assistant.UI
{
    class ScriptTabManager
    {
        private static FastColoredTextBox _scriptEditor;
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
            _scriptEditor = scriptEditor;
            _scriptTree = scriptTree;
            _variableList = variableList;

            ScriptManager.OnScriptError += OnScriptError;
            ScriptManager.OnScriptStarted += OnScriptStarted;
            ScriptManager.OnScriptStopped += OnScriptStopped;
            ScriptManager.OnScriptLineUpdate += OnScriptLineUpdate;
            ScriptManager.OnScriptPlayRequested += OnScriptPlayRequested;
            ScriptManager.OnAddToScript += EditorManager.AddToScript;
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

        private static TreeNode GetScriptDirNode()
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
    }
}
