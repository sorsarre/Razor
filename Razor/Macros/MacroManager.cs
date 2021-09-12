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
using Assistant.Scripts;

namespace Assistant.Macros
{
    public class MacroManager
    {
        private static List<Macro> m_MacroList;
        private static Macro m_Current, m_PrevPlay;
        private static bool m_Paused;
        private static MacroTimer m_Timer;

        public static void Initialize()
        {
            HotKey.Add(HKCategory.Macros, LocString.StopCurrent, new HotKeyCallback(HotKeyStop));
            HotKey.Add(HKCategory.Macros, LocString.PauseCurrent, new HotKeyCallback(HotKeyPause));

            ReloadMacros();
        }

        static MacroManager()
        {
            m_MacroList = new List<Macro>();
            m_Timer = new MacroTimer();
        }

        /// <summary>
        /// Saves all the macros and variable lists
        /// </summary>
        public static void Save()
        {
            Engine.EnsureDirectory(Config.GetUserDirectory("Macros"));

            foreach (Macro macro in m_MacroList)
            {
                macro.Save();
            }
        }

        public static List<Macro> List
        {
            get { return m_MacroList; }
        }

        public static bool Recording
        {
            get { return m_Current != null && m_Current.Recording; }
        }

        public static bool Playing
        {
            get { return m_Current != null && m_Current.Playing && m_Timer != null && m_Timer.Running; }
        }

        public static bool StepThrough
        {
            get { return m_Current != null && m_Current.StepThrough && m_Current.Playing; }
        }

        public static Macro Current
        {
            get { return m_Current; }
        }

        public static bool AcceptActions
        {
            get { return Recording || (Playing && m_Current.Waiting); }
        }
        //public static bool IsWaiting{ get{ return Playing && m_Current != null && m_Current.Waiting; } }

        public delegate void OnMacroTreeUpdatedCallback(IList<MacroNode> nodes);
        public delegate void OnMacroWaitResetCallback();
        public delegate void OnMacroWaitUpdateCallback(string text);
        public delegate void OnMacroPausedCallback();
        public delegate void OnMacroPlayCallback(Macro m);
        public delegate void OnMacroStopCallback();

        public static OnMacroTreeUpdatedCallback OnMacroTreeUpdated { get; set; }
        public static OnMacroWaitResetCallback OnMacroWaitReset { get; set; }
        public static OnMacroPausedCallback OnMacroPaused { get; set; }
        public static OnMacroPlayCallback OnMacroPlay { get; set; }
        public static OnMacroStopCallback OnMacroStop { get; set; }

        public static OnMacroWaitUpdateCallback OnMacroWaitUpdate { get; set; }

        public static void Add(Macro m)
        {
            HotKey.Add(HKCategory.Macros, HKSubCat.None, Language.Format(LocString.PlayA1, m),
                new HotKeyCallbackState(HotKeyPlay), m);
            m_MacroList.Add(m);
        }

        public static void Remove(Macro m)
        {
            HotKey.Remove(Language.Format(LocString.PlayA1, m));
            m_MacroList.Remove(m);
        }

        public static void RecordAt(Macro m, int at)
        {
            if (m_Current != null)
                m_Current.Stop();
            m_Current = m;
            m_Current.RecordAt(at);
        }

        public static void Record(Macro m)
        {
            if (m_Current != null)
                m_Current.Stop();
            m_Current = m;
            m_Current.Record();
        }

        public static void PlayAt(Macro m, int at)
        {
            ScriptManager.StopScript();

            if (m_Current != null)
            {
                if (m_Current.Playing && m_Current.Loop && !m.Loop)
                    m_PrevPlay = m_Current;
                else
                    m_PrevPlay = null;

                m_Current.Stop();
            }
            else
            {
                m_PrevPlay = null;
            }

            LiftAction.LastLift = null;
            m_Current = m;
            m_Current.PlayAt(at);

            m_Timer.Macro = m_Current;

            if (!Config.GetBool("StepThroughMacro"))
            {
                m_Timer.Start();
            }

            OnMacroWaitReset?.Invoke();
        }

        private static void HotKeyPlay(ref object state)
        {
            HotKeyPlay(state as Macro);
        }

        public static void HotKeyPlay(Macro m)
        {
            if (m != null)
            {
                Play(m);

                if (!Config.GetBool("DisableMacroPlayFinish"))
                    World.Player.SendMessage(LocString.PlayingA1, m);

                OnMacroPlay?.Invoke(m);
            }
        }

        public static void Play(Macro m)
        {
            ScriptManager.StopScript();

            if (m_Current != null)
            {
                if (m_Current.Playing && m_Current.Loop && !m.Loop)
                    m_PrevPlay = m_Current;
                else
                    m_PrevPlay = null;

                m_Current.Stop();
            }
            else
            {
                m_PrevPlay = null;
            }

            LiftAction.LastLift = null;
            m_Current = m;
            m_Current.Play();

            m_Timer.Macro = m_Current;

            if (!Config.GetBool("StepThroughMacro"))
            {
                m_Timer.Start();
            }

            OnMacroWaitReset?.Invoke();
        }

        public static void PlayNext()
        {
            if (m_Current == null)
                return;

            m_Timer.PerformNextAction();
        }

        private static void HotKeyPause()
        {
            Pause();
        }

        private static void HotKeyStop()
        {
            Stop();
        }

        public static void Stop()
        {
            Stop(false);
        }

        public static void Stop(bool restartPrev)
        {
            m_Timer.Stop();
            if (m_Current != null)
            {
                m_Current.Stop();
                m_Current = null;
            }

            UOAssist.PostMacroStop();

            OnMacroStop?.Invoke();

            //if ( restartPrev )
            //	Play( m_PrevPlay );
            m_PrevPlay = null;
        }

        public static void Pause()
        {
            if (m_Current == null)
                return;

            if (m_Paused)
            {
                // unpause
                int sel = m_Current.CurrentAction;
                if (sel < 0 || sel > m_Current.Actions.Count)
                    sel = m_Current.Actions.Count;

                //m_Current.PlayAt(sel);
                m_Timer.Start();

                m_Paused = false;

                World.Player.SendMessage(LocString.MacroResuming);
            }
            else
            {
                // pause
                m_Timer.Stop();

                OnMacroPaused?.Invoke();

                World.Player.SendMessage(LocString.MacroPaused);

                m_Paused = true;
            }
        }

        public class MacroNode
        {
            public IList<MacroNode> Children { get; set; }
            public string Text { get; set; }
            public object Tag { get; set; }

            public bool IsDirectory
            {
                get
                {
                    return (Tag != null) && (Tag is string);
                }
            }

            public MacroNode(string text)
            {
                Text = text;
            }
        }

        public static void ReloadMacros()
        {
            var nodes = Recurse(Config.GetUserDirectory("Macros"));
            OnMacroTreeUpdated?.Invoke(nodes);
        }

        private static IList<MacroNode> Recurse(string path)
        {
            var nodes = new List<MacroNode>();

            try
            {
                string[] macros = Directory.GetFiles(path, "*.macro");
                for (int i = 0; i < macros.Length; i++)
                {
                    Macro m = null;
                    for (int j = 0; j < m_MacroList.Count; j++)
                    {
                        Macro check = m_MacroList[j];

                        if (check.Filename == macros[i])
                        {
                            m = check;
                            break;
                        }
                    }

                    if (m == null)
                        Add(m = new Macro(macros[i]));

                    var node = new MacroNode(Path.GetFileNameWithoutExtension(m.Filename));
                    node.Tag = m;
                    nodes.Add(node);
                }
            }
            catch
            {
            }

            try
            {
                string[] dirs = Directory.GetDirectories(path);
                for (int i = 0; i < dirs.Length; i++)
                {
                    if (dirs[i] != "" && dirs[i] != "." && dirs[i] != "..")
                    {
                        var node = new MacroNode($"[{Path.GetFileName(dirs[i])}]");
                        node.Tag = dirs[i];
                        nodes.Add(node);
                        node.Children = Recurse(dirs[i]);
                    }
                }
            }
            catch
            {
            }

            return nodes;
        }

        public static bool Action(MacroAction a)
        {
            if (m_Current != null)
                return m_Current.Action(a);
            else
                return false;
        }

        private class MacroTimer : Timer
        {
            private Macro m_Macro;

            // The default Razor delay has always been 50ms, but for CUO, that delay isn't needed since it isn't
            // passing messages back and forth.
            public MacroTimer() : base(TimeSpan.FromMilliseconds(Config.GetBool("MacroActionDelay") ? 50 : 0),
                TimeSpan.FromMilliseconds(Config.GetBool("MacroActionDelay") ? 50 : 0))
            {
            }

            public Macro Macro
            {
                get { return m_Macro; }
                set { m_Macro = value; }
            }

            public void PerformNextAction()
            {
                ExecuteNextAction();
            }

            protected override void OnTick()
            {
                ExecuteNextAction();
            }

            private void ExecuteNextAction()
            {
                try
                {
                    if (m_Macro == null || World.Player == null)
                    {
                        this.Stop();
                        MacroManager.Stop();
                    }
                    else if (!m_Macro.ExecNext())
                    {
                        this.Stop();
                        MacroManager.Stop(true);

                        if (!Config.GetBool("DisableMacroPlayFinish"))
                            World.Player.SendMessage(LocString.MacroFinished, m_Macro);
                    }
                }
                catch
                {
                    this.Stop();
                    MacroManager.Stop();
                }
            }
        }
    }
}