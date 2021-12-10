#region license

// Razor: An Ultima Online Assistant
// Copyright (C) 2021 Razor Development Community on GitHub <https://github.com/markdwags/Razor>
// 
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.

#endregion

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Assistant.Gumps.Internal;
using Assistant.Macros;
using Assistant.Scripts.Engine;

namespace Assistant.Scripts
{
    public static class ScriptManager
    {
        public static bool Recording { get; set; }

        public static bool Paused => ScriptPaused;

        private static bool ScriptPaused { get; set; }

        public static bool Running => ScriptRunning;

        private static bool ScriptRunning { get; set; }

        public static DateTime LastWalk { get; set; }

        public static bool SetLastTargetActive { get; set; }

        public static bool SetVariableActive { get; set; }
        
        public static bool TargetFound { get; set; }

        public static string ScriptPath => Config.GetUserDirectory("Scripts");

        private static Script _queuedScript;

        public static bool BlockPopupMenu { get; set; }

        public static RazorScript SelectedScript { get; set; }

        public delegate void OnScriptStartedCallback();
        public delegate void OnScriptStoppedCallback();
        public delegate void OnScriptErrorCallback(Exception ex);
        public delegate void OnScriptLineUpdateCallback(int line);
        public delegate void OnScriptPlayRequestedCallback();
        public delegate void OnAddToScriptCallback(string text);

        public static OnScriptStartedCallback OnScriptStarted { get; set; }
        public static OnScriptStoppedCallback OnScriptStopped { get; set; }
        public static OnScriptErrorCallback OnScriptError { get; set; }
        public static OnScriptLineUpdateCallback OnScriptLineUpdate { get; set; }
        public static OnScriptPlayRequestedCallback OnScriptPlayRequested { get; set; }
        public static OnAddToScriptCallback OnAddToScript { get; set; }

        private class ScriptTimer : Timer
        {
            // Only run scripts once every 25ms to avoid spamming.
            public ScriptTimer() : base(TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(25))
            {
            }

            protected override void OnTick()
            {
                try
                {
                    if (!Client.Instance.ClientRunning)
                    {
                        if (ScriptRunning)
                        {
                            ScriptRunning = false;
                            Interpreter.StopScript();
                        }

                        return;
                    }

                    bool running;

                    if (_queuedScript != null)
                    {
                        // Starting a new script. This relies on the atomicity for references in CLR
                        Script script = _queuedScript;

                        running = Interpreter.StartScript(script);
                        OnScriptLineUpdate?.Invoke(Interpreter.CurrentLine);

                        _queuedScript = null;
                    }
                    else
                    {
                        running = Interpreter.ExecuteScript();

                        if (running)
                        {
                            OnScriptLineUpdate?.Invoke(Interpreter.CurrentLine);
                        }
                    }


                    if (running)
                    {
                        if (ScriptManager.Running == false)
                        {
                            if (Config.GetBool("ScriptDisablePlayFinish"))
                                World.Player?.SendMessage(LocString.ScriptPlaying);

                            OnScriptStarted?.Invoke();
                            ScriptRunning = true;
                        }
                    }
                    else
                    {
                        if (ScriptManager.Running)
                        {
                            if (Config.GetBool("ScriptDisablePlayFinish"))
                                World.Player?.SendMessage(LocString.ScriptFinished);

                            OnScriptStopped?.Invoke();
                            ScriptRunning = false;
                        }
                    }
                }
                catch (Exception ex)
                {
                    World.Player?.SendMessage(MsgLevel.Error, $"Script Error: {ex.Message} (Line: {Interpreter.CurrentLine + 1})");
                    OnScriptError?.Invoke(ex);
                    StopScript();
                }
            }
        }

        /// <summary>
        /// This is called via reflection when the application starts up
        /// </summary>
        public static void Initialize()
        {
            HotKey.Add(HKCategory.Scripts, HKSubCat.None, LocString.StopScript, HotkeyStopScript);
            HotKey.Add(HKCategory.Scripts, HKSubCat.None, LocString.PauseScript, HotkeyPauseScript);
            HotKey.Add(HKCategory.Scripts, HKSubCat.None, LocString.ScriptDClickType, HotkeyDClickTypeScript);
            HotKey.Add(HKCategory.Scripts, HKSubCat.None, LocString.ScriptTargetType, HotkeyTargetTypeScript);


            _scriptList = new List<RazorScript>();

            ReloadScripts();
        }

        private static void HotkeyTargetTypeScript()
        {
            if (World.Player != null)
            {
                World.Player.SendMessage(MsgLevel.Force, LocString.ScriptTargetType);
                Targeting.OneTimeTarget(OnTargetTypeScript);
            }
        }

        private static void OnTargetTypeScript(bool loc, Serial serial, Point3D pt, ushort itemId)
        {
            Item item = World.FindItem(serial);

            if (item != null && item.Serial.IsItem && item.Movable && item.Visible)
            {
                string cmd = $"targettype '{item.ItemID.ItemData.Name}'";

                Clipboard.SetDataObject(cmd);
                World.Player.SendMessage(MsgLevel.Force, Language.Format(LocString.ScriptCopied, cmd), false);
            }
            else
            {
                Mobile m = World.FindMobile(serial);

                if (m != null)
                {
                    string cmd = $"targettype '{m.Body}'";

                    Clipboard.SetDataObject(cmd);
                    World.Player.SendMessage(MsgLevel.Force, Language.Format(LocString.ScriptCopied, cmd), false);
                }
            }
        }

        private static void HotkeyDClickTypeScript()
        {
            if (World.Player != null)
            {
                World.Player.SendMessage(MsgLevel.Force, LocString.ScriptTargetType);
                Targeting.OneTimeTarget(OnDClickTypeScript);
            }
        }

        private static void OnDClickTypeScript(bool loc, Serial serial, Point3D pt, ushort itemId)
        {
            Item item = World.FindItem(serial);

            if (item != null && item.Serial.IsItem && item.Movable && item.Visible)
            {
                string cmd = $"dclicktype '{item.ItemID.ItemData.Name}'";

                Clipboard.SetDataObject(cmd);
                World.Player.SendMessage(MsgLevel.Force, Language.Format(LocString.ScriptCopied, cmd), false);
            }
            else
            {
                Mobile m = World.FindMobile(serial);

                if (m != null)
                {
                    string cmd = $"dclicktype '{m.Body}'";

                    Clipboard.SetDataObject(cmd);
                    World.Player.SendMessage(MsgLevel.Force, Language.Format(LocString.ScriptCopied, cmd), false);
                }
            }
        }

        private static void HotkeyStopScript()
        {
            StopScript();
        }

        private static void HotkeyPauseScript()
        {
            if (!ScriptRunning)
            {
                return;
            }

            if (ScriptPaused)
            {
                ResumeScript();
                World.Player.SendMessage(MsgLevel.Force, Language.Format(LocString.ResumeScriptMessage, Interpreter.CurrentLine), false);
            }
            else
            {
                World.Player.SendMessage(MsgLevel.Force, Language.Format(LocString.PauseScriptMessage, Interpreter.CurrentLine), false);
                PauseScript();
            }
        }

        private static void AddHotkey(RazorScript script)
        {
            HotKey.Add(HKCategory.Scripts, HKSubCat.None, Language.Format(LocString.PlayScript, script), OnHotKey,
                script);
        }

        private static void RemoveHotkey(RazorScript script)
        {
            HotKey.Remove(Language.Format(LocString.PlayScript, script.ToString()));
        }

        public static void OnHotKey(ref object state)
        {
            RazorScript script = (RazorScript) state;

            PlayScript(script.Lines);
        }

        public static void StopScript()
        {
            _queuedScript = null;
            ScriptPaused = false;

            Interpreter.StopScript();
        }

        public static void PauseScript()
        {
            ScriptPaused = true;
            Interpreter.PauseScript();
        }

        public static void ResumeScript()
        {
            ScriptPaused = false;
            Interpreter.ResumeScript();
        }

        public static RazorScript AddScript(string file)
        {
            RazorScript script = new RazorScript
            {
                Lines = File.ReadAllLines(file),
                Name = Path.GetFileNameWithoutExtension(file),
                Path = file
            };

            if (Path.GetDirectoryName(script.Path).Equals(Config.GetUserDirectory("Scripts")))
            {
                script.Category = string.Empty;
            }
            else
            {
                string cat = file.Replace(Config.GetUserDirectory("Scripts"), "").Substring(1);
                script.Category = Path.GetDirectoryName(cat).Replace("/", "\\");
            }

            AddHotkey(script);

            _scriptList.Add(script);

            return script;
        }

        public static void RemoveScript(RazorScript script)
        {
            RemoveHotkey(script);

            _scriptList.Remove(script);
        }
        
        public static void PlayScript(string scriptName)
        {
            foreach (RazorScript razorScript in _scriptList)
            {
                if (razorScript.ToString().IndexOf(scriptName, StringComparison.OrdinalIgnoreCase) != -1)
                {
                    PlayScript(razorScript.Lines);
                    break;
                }
            }
        }

        public static void PlayScript(string[] lines)
        {
            if (World.Player == null || lines == null)
                return;

            OnScriptPlayRequested?.Invoke();

            if (MacroManager.Playing || MacroManager.StepThrough)
                MacroManager.Stop();

            StopScript(); // be sure nothing is running

            SetLastTargetActive = false;
            SetVariableActive = false;

            if (_queuedScript != null)
                return;

            if (!Client.Instance.ClientRunning)
                return;

            if (World.Player == null)
                return;

            Script script = new Script(Lexer.Lex(lines));

            _queuedScript = script;
        }

        private static ScriptTimer Timer { get; }

        static ScriptManager()
        {
            Timer = new ScriptTimer();
        }

        public static void OnLogin()
        {
            Commands.Register();
            AgentCommands.Register();
            SpeechCommands.Register();
            TargetCommands.Register();

            Aliases.Register();
            Expressions.Register();

            Timer.Start();
        }

        public static void OnLogout()
        {
            StopScript();
            Timer.Stop();
            OnScriptStopped?.Invoke();
        }

        private static List<RazorScript> _scriptList { get; set; }

        public static bool AddToScript(string command)
        {
            if (Recording)
            {
                OnAddToScript?.Invoke(command);
                return true;
            }

            return false;
        }

        public class ScriptNode
        {
            public ScriptNode(string text)
            {
                Text = text;
            }

            public IList<ScriptNode> Children { get; set; }
            public object Tag { get; set; }
            public string Text { get; set; }

            public bool IsDirectory
            {
                get
                {
                    return (Tag != null) && (Tag is string);
                }
            }
        }

        public delegate void OnScriptsLoadedCallback(IList<ScriptNode> treeNodes);
        public static OnScriptsLoadedCallback OnScriptsLoaded { get; set; }

        public static void ReloadScripts()
        {
            var nodes = ScanDirectory(Config.GetUserDirectory("Scripts"));
            OnScriptsLoaded?.Invoke(nodes);
        }

        private static IList<ScriptNode> ScanDirectory(string path)
        {
            var nodes = new List<ScriptNode>();

            try
            {
                var razorFiles = Directory.GetFiles(path, "*.razor");
                razorFiles = razorFiles.OrderBy(fn => fn).ToArray();

                foreach (var file in razorFiles)
                {
                    RazorScript script = null;

                    foreach(var razorScript in _scriptList)
                    {
                        if (razorScript.Path.Equals(file))
                        {
                            script = razorScript;
                        }
                    }

                    if (script == null)
                    {
                        script = AddScript(file);
                    }

                    var node = new ScriptNode(script.Name)
                    {
                        Tag = script
                    };

                    nodes.Add(node);
                }
            }
            catch
            {
                // ignored
            }

            try
            {
                foreach (string directory in Directory.GetDirectories(path))
                {
                    if (!string.IsNullOrEmpty(directory) && !directory.Equals(".") && !directory.Equals(".."))
                    {
                        var node = new ScriptNode($"[{Path.GetFileName(directory)}]")
                        {
                            Tag = directory
                        };

                        node.Children = ScanDirectory(directory);
                        nodes.Add(node);
                    }
                }
            }
            catch
            {
                // ignored
            }

            return nodes;
        }

        public static void GetGumpInfo(string[] param)
        {
            Targeting.OneTimeTarget(OnGetItemInfoTarget);
            Client.Instance.SendToClient(new UnicodeMessage(0xFFFFFFFF, -1, MessageType.Regular, 0x3B2, 3,
                Language.CliLocName, "System", "Select an item or mobile to view/inspect"));
        }

        private static void OnGetItemInfoTarget(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            Item item = World.FindItem(serial);

            if (item == null)
            {
                Mobile mobile = World.FindMobile(serial);

                if (mobile == null)
                    return;

                MobileInfoGump gump = new MobileInfoGump(mobile);
                gump.SendGump();
            }
            else
            {
                ItemInfoGump gump = new ItemInfoGump(item);
                gump.SendGump();
            }
        }
    }
}