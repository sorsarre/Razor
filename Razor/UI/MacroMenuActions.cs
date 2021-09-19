using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Assistant.Macros;
using System.Threading.Tasks;

namespace Assistant.UI
{
    public delegate void MacroMenuCallback();

    public class MacroMenuItem : ToolStripMenuItem
    {
        private MacroMenuCallback _callback;

        public MacroMenuItem(LocString name, MacroMenuCallback call) : base(Language.GetString(name))
        {
            _callback = call;
            base.Click += OnMenuClick;
        }

        private void OnMenuClick(object sender, System.EventArgs e)
        {
            _callback?.Invoke();
        }
    }

    public class MacroMenuItemFactory
    {
        public static ToolStripMenuItem[] GetContextMenuItems(MacroAction a)
        {
            switch(a)
            {
                case MacroComment mc: return GetMenuItems(mc);
                case DoubleClickAction dc: return GetMenuItems(dc);
                case DoubleClickTypeAction dct: return GetMenuItems(dct);
                case LiftAction la: return GetMenuItems(la);
                case LiftTypeAction lta: return GetMenuItems(lta);
                case DropAction da: return GetMenuItems(da);
                case GumpResponseAction gra: return GetMenuItems(gra);
                case AbsoluteTargetAction ata: return GetMenuItems(ata);
                case TargetTypeAction tta: return GetMenuItems(tta);
                case TargetRelLocAction trla: return GetMenuItems(trla);
                case SpeechAction sa: return GetMenuItems(sa);
                case OverheadMessageAction oma: return GetMenuItems(oma);
                case WaitForMenuAction wma: return GetMenuItems(wma);
                case WaitForGumpAction wga: return GetMenuItems(wga);
                case WaitForTargetAction wta: return GetMenuItems(wta);
                case PauseAction pa: return GetMenuItems(pa);
                case WaitForStatAction wsa: return GetMenuItems(wsa);
                case IfAction ia: return GetMenuItems(ia);
                case ForAction fa: return GetMenuItems(fa);
                case WhileAction wa: return GetMenuItems(wa);
                case DoWhileAction dwa: return GetMenuItems(dwa);
                case PromptAction pa: return GetMenuItems(pa);
                case WaitForPromptAction wpa: return GetMenuItems(wpa);
                default:
                    return null;
            }
        }

        private static ToolStripMenuItem[] GetMenuItems(MacroComment a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditComment(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(DoubleClickAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.ReTarget, () => a.ReTarget(OnTarget)),
                new MacroMenuItem(LocString.Conv2DCT, () => a.ConvertToByType())
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(DoubleClickTypeAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.ReTarget, () => a.ReTarget(OnTarget))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(LiftAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.ReTarget, () => a.ReTarget(OnTarget)),
                new MacroMenuItem(LocString.ConvLiftByType, () => a.ConvertToByType()),
                new MacroMenuItem(LocString.Edit, () => EditAmount(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(LiftTypeAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.ReTarget, () => a.ReTarget(OnTarget)),
                new MacroMenuItem(LocString.Edit, () => EditAmount(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(DropAction a)
        {
            if (a.IsDestinationValid)
            {
                return null;
            }

            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.ConvRelLoc, () => a.ConvertToRelLoc())
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(GumpResponseAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.UseLastGumpResponse, () => a.UseLastResponse()),
                new MacroMenuItem(LocString.Edit, () => EditButtonID(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(AbsoluteTargetAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.ReTarget, () => a.ReTarget(OnTarget)),
                new MacroMenuItem(LocString.ConvLT, () => a.ConvertToLastTarget()),
                new MacroMenuItem(LocString.ConvTargType, () => a.ConvertToByType()),
                new MacroMenuItem(LocString.ConvRelLoc, () => a.ConvertToRelLoc())
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(TargetTypeAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.ReTarget, () => a.ReTarget(OnTarget)),
                new MacroMenuItem(LocString.ConvLT, () => a.ConvertToLastTarget())
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(TargetRelLocAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.ReTarget, () => a.ReTarget(OnTarget))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(SpeechAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditSpeech(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(OverheadMessageAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditMessage(a)),
                new MacroMenuItem(LocString.SetHue, () => EditHue(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(WaitForMenuAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditWaitAction(a)),
                new MacroMenuItem(LocString.EditTimeout, () => EditTimeout(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(WaitForGumpAction a)
        {
            var items = new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditWaitAction(a)),
                new MacroMenuItem(LocString.Null, () => ToggleStrict(a)),
                new MacroMenuItem(LocString.EditTimeout, () => EditTimeout(a))
            };

            if (!a.Strict)
            {
                items[1].Text =
                    $"Change to \"{Language.Format(LocString.WaitGumpA1, a.GumpID)}\"";
            }
            else
            {
                items[1].Text = $"Change to \"{Language.GetString(LocString.WaitAnyGump)}\"";
            }
                
            items[1].Enabled = a.GumpID != 0 || a.Strict;
            return items;
        }

        private static ToolStripMenuItem[] GetMenuItems(WaitForTargetAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditWaitAction(a)),
                new MacroMenuItem(LocString.EditTimeout, () => EditTimeout(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(PauseAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditWaitAction(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(WaitForStatAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditWaitAction(a)),
                new MacroMenuItem(LocString.EditTimeout, () => EditTimeout(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(IfAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditIfAction(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(ForAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditForAction(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(WhileAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditWhileAction(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(DoWhileAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditDoWhileAction(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(PromptAction a)
        {
            return new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditPromptAction(a))
            };
        }

        private static ToolStripMenuItem[] GetMenuItems(WaitForPromptAction a)
        {
            var items = new MacroMenuItem[]
            {
                new MacroMenuItem(LocString.Edit, () => EditWaitAction(a)),
                new MacroMenuItem(LocString.Null, () => ToggleStrict(a)),
                new MacroMenuItem(LocString.EditTimeout, () => EditTimeout(a))
            };

            if (!a.Strict)
            {
                items[1].Text = $"Change to \"Wait For Prompt ({a.PromptID})\"";
            }
            else
            {
                items[1].Text = $"Change to \"Wait For Prompt (Any)\"";
            }
            
            items[1].Enabled = a.PromptID != 0 || a.Strict;

            return items;
        }

        private static void OnTarget(bool ground, Serial serial, Point3D pt, ushort gfx)
        {
            Engine.MainWindow.SafeAction(s => s.ShowMe());
        }

        private static void EditTimeout(MacroWaitAction a)
        {
            var currentValue = a.Timeout.TotalSeconds.ToString("F0");
            if (InputBox.Show(LocString.NewTimeout, LocString.ChangeTimeout, currentValue))
            {
                a.Timeout = TimeSpan.FromSeconds(InputBox.GetInt(60));
            }
        }

        private static void EditComment(MacroComment a)
        {
            if (InputBox.Show(LocString.InsComment, LocString.InputReq, a.Comment))
            {
                a.Comment = InputBox.GetString();
            }
        }

        private static void EditAmount(LiftAction a)
        {
            if (InputBox.Show(LocString.EnterAmount, LocString.InputReq, a.Amount.ToString()))
            {
                a.Amount = (ushort)InputBox.GetInt(a.Amount);
            }
        }

        private static void EditAmount(LiftTypeAction a)
        {
            if (InputBox.Show(LocString.EnterAmount, LocString.InputReq, a.Amount.ToString()))
            {
                a.Amount = (ushort)InputBox.GetInt(a.Amount);
            }
        }

        private static void EditButtonID(GumpResponseAction a)
        {
            if (InputBox.Show(Language.GetString(LocString.EnterNewText), "Input Box", a.ButtonID.ToString()))
            {
                a.ButtonID = InputBox.GetInt();
            }
        }

        private static void EditSpeech(SpeechAction a)
        {
            if (InputBox.Show(Language.GetString(LocString.EnterNewText), "Input Box", a.Speech))
            {
                a.Speech = InputBox.GetString();
            }
        }

        private static void EditMessage(OverheadMessageAction a)
        {
            if (InputBox.Show(Language.GetString(LocString.EnterNewText), "Input Box", a.Message))
            {
                a.Message = InputBox.GetString();
            }
        }

        private static void EditHue(OverheadMessageAction a)
        {
            HueEntry h = new HueEntry(a.Hue);

            if (h.ShowDialog(Engine.MainWindow) == DialogResult.OK)
            {
                a.Hue = (ushort)h.Hue;
            }
        }

        private static void EditWaitAction(MacroAction a)
        {
            new MacroInsertWait(a).ShowDialog(Engine.MainWindow);
        }

        private static void ToggleStrict(WaitForGumpAction a)
        {
            a.Strict = !a.Strict;
        }

        private static void ToggleStrict(WaitForPromptAction a)
        {
            a.Strict = !a.Strict;
        }

        private static void EditIfAction(MacroAction a)
        {
            new MacroInsertIf(a).ShowDialog(Engine.MainWindow);
        }

        private static void EditForAction(ForAction a)
        {
            if (InputBox.Show(Language.GetString(LocString.NumIter), "Input Box", a.Max.ToString()))
            {
                a.Max = InputBox.GetInt();
            }
        }

        private static void EditWhileAction(WhileAction a)
        {
            new MacroInsertWhile(a).ShowDialog(Engine.MainWindow);
        }

        private static void EditDoWhileAction(DoWhileAction a)
        {
            new MacroInsertDoWhile(a).ShowDialog(Engine.MainWindow);
        }

        private static void EditPromptAction(PromptAction a)
        {
            if (InputBox.Show(Language.GetString(LocString.EnterNewText), "Input Box", a.Response))
            {
                a.Response = InputBox.GetString();
            }
        }
    }
}
