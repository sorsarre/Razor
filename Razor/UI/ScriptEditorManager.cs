using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FastColoredTextBoxNS;
using Assistant.Scripts;
using System.Drawing;

namespace Assistant.UI
{
    public class ToolTipDescriptions
    {
        public string Title;
        public string[] Parameters;
        public string Returns;
        public string Description;
        public string Example;

        public ToolTipDescriptions(string title, string[] parameter, string returns, string description,
            string example)
        {
            Title = title;
            Parameters = parameter;
            Returns = returns;
            Description = description;
            Example = example;
        }

        public string ToolTipDescription()
        {
            string complete_description = string.Empty;

            complete_description += "Parameter(s): ";

            foreach (string parameter in Parameters)
                complete_description += "\n\t" + parameter;

            complete_description += "\nDescription:";

            complete_description += "\n\t" + Description;

            complete_description += "\nExample(s):";

            complete_description += "\n\t" + Example;

            return complete_description;
        }
    }

    public class MethodAutocompleteItemAdvance : MethodAutocompleteItem
    {
        string firstPart;
        string lastPart;

        public MethodAutocompleteItemAdvance(string text)
            : base(text)
        {
            var i = text.LastIndexOf(' ');
            if (i < 0)
                firstPart = text;
            else
            {
                firstPart = text.Substring(0, i);
                lastPart = text.Substring(i + 1);
            }
        }

        public override CompareResult Compare(string fragmentText)
        {
            int i = fragmentText.LastIndexOf(' ');

            if (i < 0)
            {
                if (firstPart.StartsWith(fragmentText) && string.IsNullOrEmpty(lastPart))
                    return CompareResult.VisibleAndSelected;
                //if (firstPart.ToLower().Contains(fragmentText.ToLower()))
                //  return CompareResult.Visible;
            }
            else
            {
                var fragmentFirstPart = fragmentText.Substring(0, i);
                var fragmentLastPart = fragmentText.Substring(i + 1);


                if (firstPart != fragmentFirstPart)
                    return CompareResult.Hidden;

                if (lastPart != null && lastPart.StartsWith(fragmentLastPart))
                    return CompareResult.VisibleAndSelected;

                if (lastPart != null && lastPart.ToLower().Contains(fragmentLastPart.ToLower()))
                    return CompareResult.Visible;
            }

            return CompareResult.Hidden;
        }

        public override string GetTextForReplace()
        {
            if (lastPart == null)
                return firstPart;

            return firstPart + " " + lastPart;
        }

        public override string ToString()
        {
            if (lastPart == null)
                return firstPart;

            return lastPart;
        }
    }

    class ScriptEditorManager
    {
        private FastColoredTextBox _scriptEditor;
        private bool _popoutEditor;
        private AutocompleteMenu _autoCompleteMenu;

        public enum HighlightType
        {
            Error,
            Execution
        }

        public ScriptEditorManager()
        {
            foreach (HighlightType type in GetHighlightTypes())
            {
                HighlightLines[type] = new List<int>();
            }
        }

        private Dictionary<HighlightType, List<int>> HighlightLines { get; } = new Dictionary<HighlightType, List<int>>();
        private static Dictionary<HighlightType, Brush> HighlightLineColors { get; } = new Dictionary<HighlightType, Brush>()
        {
            { HighlightType.Error, new SolidBrush(Color.Red) },
            { HighlightType.Execution, new SolidBrush(Color.Blue) }
        };

        private static HighlightType[] GetHighlightTypes()
        {
            return (HighlightType[])Enum.GetValues(typeof(HighlightType));
        }

        public void SetEditor(FastColoredTextBox scriptEditor, bool popoutEditor)
        {
            _scriptEditor = scriptEditor;
            _scriptEditor.Visible = true;

            _popoutEditor = popoutEditor;

            InitScriptEditor();

            if (ScriptManager.SelectedScript != null)
            {
                SetEditorText(ScriptManager.SelectedScript);
            }
        }
        
        public void SetEditorText(RazorScript script)
        {
            _scriptEditor.Text = string.Join("\n", script.Lines);
        }

        public void InitScriptEditor()
        {
            _autoCompleteMenu = new AutocompleteMenu(_scriptEditor)
            {
                SearchPattern = @"[\w\.:=!<>]",
                AllowTabKey = true,
                ToolTipDuration = 5000,
                AppearInterval = 100
            };

            #region Keywords

            string[] keywords =
            {
                    "if", "elseif", "else", "endif", "while", "endwhile", "for", "endfor", "break", "continue", "stop",
                    "replay", "not", "and", "or"
                };

            #endregion

            #region Commands auto-complete

            string[] commands =
            {
                    "attack", "cast", "dclick", "dclicktype", "dress", "drop", "droprelloc", "gumpresponse", "gumpclose",
                    "hotkey", "lasttarget", "lift", "lifttype", "menu", "menuresponse", "organizer", "overhead", "potion",
                    "promptresponse", "restock", "say", "whisper", "yell", "emote", "script", "scavenger", "sell", "setability",
                    "setlasttarget",
                    "setvar", "skill", "sysmsg", "target", "targettype", "targetrelloc", "undress", "useonce", "walk",
                    "wait", "pause", "waitforgump", "waitformenu", "waitforprompt", "waitfortarget", "clearsysmsg", "clearjournal",
                    "waitforsysmsg", "clearhands", "clearall", "virtue", "random"

                };

            #endregion

            Dictionary<string, ToolTipDescriptions> descriptionCommands = new Dictionary<string, ToolTipDescriptions>();

            #region CommandToolTips

            var tooltip = new ToolTipDescriptions("attack", new[] { "attack (serial) or attack ('variablename')" },
                "N/A", "Attack a specific serial or variable tied to a serial.", "attack 0x2AB4\n\tattack 'attackdummy'");
            descriptionCommands.Add("attack", tooltip);

            tooltip = new ToolTipDescriptions("clearall", new[] { "clearall" }, "N/A", "Clear target, clear queues, drop anything you're holding",
                "clearall");
            descriptionCommands.Add("clearall", tooltip);

            tooltip = new ToolTipDescriptions("clearhands", new[] { "clearhands ('right'/'left'/'hands')" }, "N/A", "Use the item in your hands",
                "clearhands");
            descriptionCommands.Add("clearhands", tooltip);

            tooltip = new ToolTipDescriptions("virtue", new[] { "virtue ('honor'/'sacrifice'/'valor')" }, "N/A", "Invoke a specific virtue",
                "virtue 'honor'");
            descriptionCommands.Add("virtue", tooltip);

            tooltip = new ToolTipDescriptions("cast", new[] { "cast ('name of spell')" }, "N/A", "Cast a spell by name",
                "cast 'blade spirits'");
            descriptionCommands.Add("cast", tooltip);

            tooltip = new ToolTipDescriptions("dclick", new[] { "dclick (serial) or useobject (serial)" }, "N/A",
                "This command will use (double-click) a specific item or mobile.", "dclick 0x34AB");
            descriptionCommands.Add("dclick", tooltip);

            tooltip = new ToolTipDescriptions("dclicktype",
                new[]
                {
                        "dclicktype ('name of item') OR (graphicID) [inrange] or usetype ('name of item') OR (graphicID) [inrange]"
                }, "N/A",
                "This command will use (double-click) an item type either provided by the name or the graphic ID.\n\t\tIf you include the optional true parameter, items within range (2 tiles) will only be considered.",
                "dclicktype 'dagger'\n\t\twaitfortarget\n\t\ttargettype 'robe'");
            descriptionCommands.Add("dclicktype", tooltip);

            tooltip = new ToolTipDescriptions("dress", new[] { "dress ('name of dress list')" }, "N/A",
                "This command will execute a spec dress list you have defined in Razor.", "dress 'My Sunday Best'");
            descriptionCommands.Add("dress", tooltip);

            tooltip = new ToolTipDescriptions("drop", new[] { "drop (serial) (x/y/z/layername)" }, "N/A",
                "This command will drop the item you are holding either at your feet,\n\t\ton a specific layer or at a specific X / Y / Z location.",
                "lift 0x400D54A7 1\n\t\tdrop 0x6311 InnerTorso");
            descriptionCommands.Add("drop", tooltip);

            tooltip = new ToolTipDescriptions("", new[] { "" }, "N/A", "",
                "lift 0x400D54A7 1\n\twait 5000\n\tdrop 0xFFFFFFFF 5926 1148 0");
            descriptionCommands.Add("", tooltip);

            tooltip = new ToolTipDescriptions("droprelloc", new[] { "droprelloc (x) (y)" }, "N/A",
                "This command will drop the item you're holding to a location relative to your position.",
                "lift 0x400EED2A 1\n\twait 1000\n\tdroprelloc 1 1");
            descriptionCommands.Add("droprelloc", tooltip);

            tooltip = new ToolTipDescriptions("gumpresponse", new[] { "gumpresponse (buttonID)" }, "N/A",
                "Responds to a specific gump button", "gumpresponse 4");
            descriptionCommands.Add("gumpresponse", tooltip);

            tooltip = new ToolTipDescriptions("gumpclose", new[] { "gumpclose" }, "N/A",
                "This command will close the last gump that opened.", "gumpclose");
            descriptionCommands.Add("gumpclose", tooltip);

            tooltip = new ToolTipDescriptions("hotkey", new[] { "hotkey ('name of hotkey')" }, "N/A",
                "This command will execute any Razor hotkey by name.",
                "skill 'detect hidden'\n\twaitfortarget\n\thotkey 'target self'");
            descriptionCommands.Add("hotkey", tooltip);

            tooltip = new ToolTipDescriptions("lasttarget", new[] { "lasttarget" }, "N/A",
                "This command will target your last target set in Razor.",
                "cast 'magic arrow'\n\twaitfortarget\n\tlasttarget");
            descriptionCommands.Add("lasttarget", tooltip);

            tooltip = new ToolTipDescriptions("lift", new[] { "lift (serial) [amount]" }, "N/A",
                "This command will lift a specific item and amount. If no amount is provided, 1 is defaulted.",
                "lift 0x400EED2A 1\n\twait 1000\n\tdroprelloc 1 1 0");
            descriptionCommands.Add("lift", tooltip);

            tooltip = new ToolTipDescriptions("lifttype",
                new[] { "lifttype (gfx) [amount] or lifttype ('name of item') [amount]" }, "N/A",
                "This command will lift a specific item by type either by the graphic id or by the name.\n\tIf no amount is provided, 1 is defaulted.",
                "lifttype 'robe'\n\twait 1000\n\tdroprelloc 1 1 0\n\tlifttype 0x1FCD\n\twait 1000\n\tdroprelloc 1 1");
            descriptionCommands.Add("lifttype", tooltip);

            tooltip = new ToolTipDescriptions("menu", new[] { "menu (serial) (index) [false]" }, "N/A",
                "Selects a specific index within a context menu", "# open backpack\n\tmenu 0 1");
            descriptionCommands.Add("menu", tooltip);

            tooltip = new ToolTipDescriptions("menuresponse", new[] { "menuresponse (index) (menuId) [hue]" }, "N/A",
                "Responds to a specific menu and menu ID (not a context menu)", "menuresponse 3 4");
            descriptionCommands.Add("menuresponse", tooltip);

            tooltip = new ToolTipDescriptions("organizer", new[] { "organizer (number) ['set']" }, "N/A",
                "This command will execute a specific organizer agent. If the set parameter is included,\n\tyou will instead be prompted to set the organizer agent's hotbag.",
                "organizer 1\n\torganizer 4 'set'");
            descriptionCommands.Add("organizer", tooltip);

            tooltip = new ToolTipDescriptions("overhead", new[] { "overhead ('text') [color] [serial]" }, "N/A",
                "This command will display a message over your head. Only you can see this.",
                "if stam = 100\n\t    overhead 'ready to go!'\n\tendif");
            descriptionCommands.Add("overhead", tooltip);

            tooltip = new ToolTipDescriptions("potion", new[] { "potion ('potion type')" }, "N/A",
                "This command will use a specific potion based on the type.", "potion 'agility'\n\tpotion 'heal'");
            descriptionCommands.Add("potion", tooltip);

            tooltip = new ToolTipDescriptions("promptresponse", new[] { "promptresponse ('prompt response')" }, "N/A",
                "This command will respond to a prompt triggered from actions such as renaming runes or giving a guild title.",
                "dclicktype 'rune'\n\twaitforprompt\n\tpromptresponse 'to home'");
            descriptionCommands.Add("promptresponse", tooltip);

            tooltip = new ToolTipDescriptions("restock", new[] { "restock (number) ['set']" }, "N/A",
                "This command will execute a specific restock agent.\n\tIf the set parameter is included, you will instead be prompted to set the restock agent's hotbag.",
                "restock 1\n\trestock 4 'set'");
            descriptionCommands.Add("restock", tooltip);

            tooltip = new ToolTipDescriptions("say",
                new[] { "say ('message to send') [hue] or msg ('message to send') [hue]" }, "N/A",
                "This command will force your character to say the message passed as the parameter.",
                "say 'Hello world!'\n\tsay 'Hello world!' 454");
            descriptionCommands.Add("say", tooltip);

            tooltip = new ToolTipDescriptions("whisper",
                new[] { "whisper ('message to send') [hue]" }, "N/A",
                "This command will force your character to whisper the message passed as the parameter.",
                "whisper 'Hello world!'\n\twhisper 'Hello world!' 454");
            descriptionCommands.Add("whisper", tooltip);

            tooltip = new ToolTipDescriptions("yell",
                new[] { "yell ('message to send') [hue]" }, "N/A",
                "This command will force your character to yell the message passed as the parameter.",
                "yell 'Hello world!'\n\tyell 'Hello world!' 454");
            descriptionCommands.Add("yell", tooltip);

            tooltip = new ToolTipDescriptions("emote",
                new[] { "emote ('message to send') [hue]" }, "N/A",
                "This command will force your character to emote the message passed as the parameter.",
                "emote 'Hello world!'\n\temote 'Hello world!' 454");
            descriptionCommands.Add("emote", tooltip);

            tooltip = new ToolTipDescriptions("script", new[] { "script 'name'" }, "N/A",
                "This command will call another script.", "if hp = 40\n\t   script 'healself'\n\tendif");
            descriptionCommands.Add("script", tooltip);

            tooltip = new ToolTipDescriptions("scavenger", new[] { "scavenger ['clear'/'add'/'on'/'off'/'set']" },
                "N/A", "This command will control the scavenger agent.", "scavenger 'off'");
            descriptionCommands.Add("scavenger", tooltip);

            tooltip = new ToolTipDescriptions("sell", new[] { "sell" }, "N/A",
                "This command will set the Sell agent's hotbag.", "sell");
            descriptionCommands.Add("sell", tooltip);

            tooltip = new ToolTipDescriptions("setability",
                new[] { "setability ('primary'/'secondary'/'stun'/'disarm') ['on'/'off']" }, "N/A",
                "This will set a specific ability on or off. If on or off is missing, on is defaulted.",
                "setability stun");
            descriptionCommands.Add("setability", tooltip);

            tooltip = new ToolTipDescriptions("setlasttarget", new[] { "setlasttarget" }, "N/A",
                "This command will pause the script until you select a target to be set as Last Target.",
                "overhead 'set last target'\n\tsetlasttarget\n\toverhead 'set!'\n\tcast 'magic arrow'\n\twaitfortarget\n\ttarget 'last'");
            descriptionCommands.Add("setlasttarget", tooltip);

            tooltip = new ToolTipDescriptions("setvar", new[] { "setvar ('variable') or setvariable ('variable')" },
                "N/A",
                "This command will pause the script until you select a target to be assigned a variable.\n\tPlease note, the variable must exist before you can assign values to it.",
                "setvar 'dummy'\n\tcast 'magic arrow'\n\twaitfortarget\n\ttarget 'dummy'");
            descriptionCommands.Add("setvar", tooltip);

            tooltip = new ToolTipDescriptions("skill", new[] { "skill 'name of skill' or skill last" }, "N/A",
                "This command will use a specific skill (assuming it's a usable skill).",
                "while mana < maxmana\n\t    say 'mediation!'\n\t    skill 'meditation'\n\t    wait 11000\n\tendwhile");
            descriptionCommands.Add("skill", tooltip);

            tooltip = new ToolTipDescriptions("sysmsg", new[] { "sysmsg ('message to display in system message')" },
                "N/A", "This command will display a message in the lower-left of the client.",
                "if stam = 100\n\t    sysmsg 'ready to go!'\n\tendif");
            descriptionCommands.Add("sysmsg", tooltip);

            tooltip = new ToolTipDescriptions("target", new[] { "target (serial) or target (x) (y) (z)" }, "N/A",
                "This command will target a specific mobile or item or target a specific location based on X/Y/Z coordinates.",
                "cast 'lightning'\n\twaitfortarget\n\ttarget 0xBB3\n\tcast 'fire field'\n\twaitfortarget\n\ttarget 5923 1145 0");
            descriptionCommands.Add("target", tooltip);

            tooltip = new ToolTipDescriptions("targettype",
                new[] { "targettype (graphic) or targettype ('name of item or mobile type') [inrangecheck]" }, "N/A",
                "This command will target a specific type of mobile or item based on the graphic id or based on\n\tthe name of the item or mobile. If the optional parameter is passed\n\tin as true only items within the range of 2 tiles will be considered.",
                "usetype 'dagger'\n\twaitfortarget\n\ttargettype 'robe'\n\tuseobject 0x4005ECAF\n\twaitfortarget\n\ttargettype 0x1f03\n\tuseobject 0x4005ECAF\n\twaitfortarget\n\ttargettype 0x1f03 true");
            descriptionCommands.Add("targettype", tooltip);

            tooltip = new ToolTipDescriptions("targetrelloc", new[] { "targetrelloc (x-offset) (y-offset)" }, "N/A",
                "This command will target a specific location on the map relative to your position.",
                "cast 'fire field'\n\twaitfortarget\n\ttargetrelloc 1 1");
            descriptionCommands.Add("targetrelloc", tooltip);

            tooltip = new ToolTipDescriptions("undress",
                new[] { "undress ['name of dress list']' or undress 'LayerName'" }, "N/A",
                "This command will either undress you completely if no dress list is provided.\n\tIf you provide a dress list, only those specific items will be undressed. Lastly, you can define a layer name to undress.",
                "undress\n\tundress 'My Sunday Best'\n\tundress 'Shirt'\n\tundrsss 'Pants'");
            descriptionCommands.Add("undress", tooltip);

            tooltip = new ToolTipDescriptions("useonce", new[] { "useonce ['add'/'addcontainer']" }, "N/A",
                "This command will execute the UseOnce agent. If the add parameter is included, you can add items to your UseOnce list.\n\tIf the addcontainer parameter is included, you can add all items in a container to your UseOnce list.",
                "useonce\n\tuseonce 'add'\n\tuseonce 'addcontainer'");
            descriptionCommands.Add("useonce", tooltip);

            tooltip = new ToolTipDescriptions("walk", new[] { "walk ('direction')" }, "N/A",
                "This command will turn and/or walk your player in a certain direction.",
                "walk 'North'\n\twalk 'Up'\n\twalk 'West'\n\twalk 'Left'\n\twalk 'South'\n\twalk 'Down'\n\twalk 'East'\n\twalk 'Right'");
            descriptionCommands.Add("walk", tooltip);

            tooltip = new ToolTipDescriptions("wait",
                new[] { "wait [time in milliseconds or pause [time in milliseconds]" }, "N/A",
                "This command will pause the execution of a script for a given time.",
                "while stam < 100\n\t    wait 5000\n\tendwhile");
            descriptionCommands.Add("wait", tooltip);

            tooltip = new ToolTipDescriptions("pause",
                new[] { "pause [time in milliseconds or pause [time in milliseconds]" }, "N/A",
                "This command will pause the execution of a script for a given time.",
                "while stam < 100\n\t    wait 5000\n\tendwhile");
            descriptionCommands.Add("pause", tooltip);

            tooltip = new ToolTipDescriptions("waitforgump", new[] { "waitforgump [gump id]" }, "N/A",
                "This command will wait for a gump. If no gump id is provided, it will wait for **any * *gump.",
                "waitforgump\n\twaitforgump 4");
            descriptionCommands.Add("waitforgump", tooltip);

            tooltip = new ToolTipDescriptions("waitformenu", new[] { "waitformenu [menu id]" }, "N/A",
                "This command will wait for menu (not a context menu). If no menu id is provided, it will wait for **any * *menu.",
                "waitformenu\n\twaitformenu 4");
            descriptionCommands.Add("waitformenu", tooltip);

            tooltip = new ToolTipDescriptions("waitforprompt", new[] { "waitforprompt" }, "N/A",
                "This command will wait for a prompt before continuing.",
                "dclicktype 'rune'\n\twaitforprompt\n\tpromptresponse 'to home'");
            descriptionCommands.Add("waitforprompt", tooltip);

            tooltip = new ToolTipDescriptions("waitfortarget",
                new[] { "waitfortarget [pause in milliseconds] or wft [pause in milliseconds]" }, "N/A",
                "This command will cause the script to pause until you have a target cursor.\n\tBy default it will wait 30 seconds but you can define a specific wait time if you prefer.",
                "cast 'energy bolt'\n\twaitfortarget\n\thotkey 'Target Closest Enemy'");
            descriptionCommands.Add("waitfortarget", tooltip);

            tooltip = new ToolTipDescriptions("clearsysmsg",
                new[] { "clearsysmsg" }, "N/A",
                "This command will clear the internal system message queue used with insysmsg.",
                "clearsysmsg\n");
            descriptionCommands.Add("clearsysmsg", tooltip);

            tooltip = new ToolTipDescriptions("clearjournal",
                new[] { "clearjournal" }, "N/A",
                "This command (same as clearjournal) will clear the internal system message queue used with insysmsg.",
                "clearjournal\n");
            descriptionCommands.Add("clearjournal", tooltip);

            tooltip = new ToolTipDescriptions("waitforsysmsg",
                new[] { "waitforsysmsg" }, "N/A",
                "This command will pause the script until the message defined is in the system message queue.",
                "waitforsysmsg 'message here'\n");
            descriptionCommands.Add("waitforsysmsg", tooltip);

            tooltip = new ToolTipDescriptions("random",
                new[] { "random [max number]" }, "N/A",
                "This command output a random number between 1 and the max number provided.",
                "random '15'\n");
            descriptionCommands.Add("random", tooltip);

            #endregion

            if (!Config.GetBool("DisableScriptTooltips"))
            {
                List<AutocompleteItem> items = new List<AutocompleteItem>();

                foreach (var item in keywords)
                {
                    items.Add(new AutocompleteItem(item));
                }

                foreach (var item in commands)
                {
                    descriptionCommands.TryGetValue(item, out ToolTipDescriptions element);

                    if (element != null)
                    {
                        items.Add(new MethodAutocompleteItemAdvance(item)
                        {
                            ImageIndex = 2,
                            ToolTipTitle = element.Title,
                            ToolTipText = element.ToolTipDescription()
                        });
                    }
                    else
                    {
                        items.Add(new MethodAutocompleteItemAdvance(item)
                        {
                            ImageIndex = 2
                        });
                    }
                }

                _autoCompleteMenu.Items.SetAutocompleteItems(items);
                _autoCompleteMenu.Items.MaximumSize =
                    new Size(_autoCompleteMenu.Items.Width + 20, _autoCompleteMenu.Items.Height);
                _autoCompleteMenu.Items.Width = _autoCompleteMenu.Items.Width + 20;
            }
            else
            {
                _autoCompleteMenu.Items.SetAutocompleteItems(new List<AutocompleteItem>());
            }

            _scriptEditor.Language = FastColoredTextBoxNS.Language.Razor;
        }

        public void AddToScript(string command)
        {
            _scriptEditor?.AppendText(command + Environment.NewLine);
        }

        public void UpdateLineNumber(int lineNum)
        {
            if (_popoutEditor)
            {
                SetHighlightLine(lineNum, HighlightType.Execution);
                // Scrolls to relevant line, per this suggestion: https://github.com/PavelTorgashov/FastColoredTextBox/issues/115
                _scriptEditor.Selection.Start = new Place(0, lineNum);
                _scriptEditor.DoSelectionVisible();
            }
        }

        public void SetHighlightLine(int iline, HighlightType type)
        {
            if (!_popoutEditor)
                return;

            ClearHighlightLine(type);
            AddHighlightLine(iline, type);
        }

        public void ClearHighlightLine(HighlightType type)
        {
            if (!_popoutEditor)
                return;

            HighlightLines[type].Clear();
            RefreshHighlightLines();
        }

        private void AddHighlightLine(int iline, HighlightType type)
        {
            HighlightLines[type].Add(iline);
            RefreshHighlightLines();
        }

        private void RefreshHighlightLines()
        {
            for (int i = 0; i < _scriptEditor.LinesCount; i++)
            {
                _scriptEditor[i].BackgroundBrush = _scriptEditor.BackBrush;
            }

            foreach (HighlightType type in GetHighlightTypes())
            {
                foreach (int lineNum in HighlightLines[type])
                {
                    _scriptEditor[lineNum].BackgroundBrush = HighlightLineColors[type];
                }
            }

            _scriptEditor.Invalidate();
        }

        public void ClearAllHighlightLines()
        {
            if (!_popoutEditor)
                return;

            foreach (HighlightType type in GetHighlightTypes())
            {
                HighlightLines[type].Clear();
            }

            RefreshHighlightLines();
        }
    }
}
