/*
    Copyright 2011 MCForge
        
    Dual-licensed under the Educational Community License, Version 2.0 and
    the GNU General Public License, Version 3 (the "Licenses"); you may
    not use this file except in compliance with the Licenses. You may
    obtain a copy of the Licenses at
    
    http://www.opensource.org/licenses/ecl2.php
    http://www.gnu.org/licenses/gpl-3.0.html
    
    Unless required by applicable law or agreed to in writing,
    software distributed under the Licenses are distributed on an "AS IS"
    BASIS, WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express
    or implied. See the Licenses for the specific language governing
    permissions and limitations under the Licenses.
 */
using System;

namespace MCGalaxy.Commands.World {
    public sealed class CmdMap : Command2 {
        public override string name { get { return "Map"; } }
        public override string type { get { return CommandTypes.World; } }
        public override CommandPerm[] ExtraPerms {
            get { return new[] { new CommandPerm(LevelPermission.Operator, "can edit map options"),
                    new CommandPerm(LevelPermission.Admin, "can set realm owners") }; }
        }
        public override CommandAlias[] Aliases {
            get { return new[] { new CommandAlias("ps", LevelOptions.Speed),
                    new CommandAlias("AllowGuns", "{args} " + LevelOptions.Guns) }; }
        }

        public override void Use(Player p, string message, CommandData data) {
            if (CheckSuper(p, message, "level name")) return;
            if (message.Length == 0) message = p.level.name;
            string[] args = message.SplitSpaces(3);
            Level lvl = null;
            string optName = null, value = null;
            
            if (IsMapOption(args)) {
                if (p.IsSuper) { SuperRequiresArgs(p, "level name"); return; }
                lvl = p.level;
                
                optName = args[0];
                args = message.SplitSpaces(2);
                value = args.Length > 1 ? args[1] : "";
            } else if (args.Length == 1) {
                string map = Matcher.FindMaps(p, args[0]);
                if (map == null) return;
                
                PrintMapInfo(p, LevelInfo.GetConfig(map));
                return;
            } else {
                lvl = Matcher.FindLevels(p, args[0]);
                if (lvl == null) return;
                
                optName = args[1];
                value = args.Length > 2 ? args[2] : "";
            }
            
            if (!CheckExtraPerm(p, data, 1)) return;
            if (optName.CaselessEq(LevelOptions.RealmOwner) && !CheckExtraPerm(p, data, 2)) return;
            if (!LevelInfo.Check(p, data.Rank, lvl, "change map settings of this level")) return;
            
            LevelOption opt = LevelOptions.Find(optName);
            if (opt == null) {
                p.Message("Could not find option entered.");
            } else {
                opt.SetFunc(p, lvl, value);
                lvl.SaveSettings();
            }
        }
        
        static bool IsMapOption(string[] args) {
            LevelOption opt = LevelOptions.Find(args[0]);
            if (opt == null) return false;
            // In rare case someone uses /map motd motd My MOTD
            if (opt.Name == LevelOptions.MOTD && (args.Length == 1 || !args[1].CaselessStarts("motd "))) return true;
            
            int argsCount = HasArgument(opt.Name) ? 2 : 1;
            return args.Length == argsCount;
        }
        
        static bool HasArgument(string opt) {
            return
                opt == LevelOptions.Speed || opt == LevelOptions.Overload || opt == LevelOptions.TreeType ||
                opt == LevelOptions.Fall || opt == LevelOptions.Drown || opt == LevelOptions.RealmOwner || opt == LevelOptions.LoadDelay;
        }
        
        static void PrintMapInfo(Player p, LevelConfig cfg) {
            p.Message("&TPhysics settings:");
            p.Message("  Finite mode: {0}&S, Random flow: {1}",
                           GetBool(cfg.FiniteLiquids), GetBool(cfg.RandomFlow));
            p.Message("  Finite high water {0}&S",
                           GetBool(cfg.FiniteHighWater));
            p.Message("  Animal hunt AI: {0}&S, Edge water: {1}",
                           GetBool(cfg.AnimalHuntAI), GetBool(cfg.EdgeWater));
            p.Message("  Grass growing: {0}&S, {1} tree growing: {2}",
                           GetBool(cfg.GrassGrow), cfg.TreeType.Capitalize(), GetBool(cfg.GrowTrees));
            p.Message("  Leaf decay: {0}&S, Physics overload: {1}",
                           GetBool(cfg.LeafDecay), cfg.PhysicsOverload);
            p.Message("  Physics speed: &b{0} &Smilliseconds between ticks",
                           cfg.PhysicsSpeed);
            
            p.Message("&TSurvival settings:");
            p.Message("  Survival death: {0} &S(Fall: {1}, Drown: {2})",
                           GetBool(cfg.SurvivalDeath), cfg.FallHeight, cfg.DrownTime);
            p.Message("  Guns: {0}&S, Killer blocks: {1}",
                           GetBool(cfg.Guns), GetBool(cfg.KillerBlocks));
            
            p.Message("&TGeneral settings:");
            p.Message("  MOTD: &b" + cfg.MOTD);
            p.Message("  Local level only chat: " + GetBool(!cfg.ServerWideChat));
            p.Message("  Load on /goto: {0}&S, Auto unload: {1}",
                           GetBool(cfg.LoadOnGoto), GetBool(cfg.AutoUnload));
            p.Message("  Buildable: {0}&S, Deletable: {1}&S, Drawing: {2}",
                           GetBool(cfg.Buildable), GetBool(cfg.Deletable), GetBool(cfg.Drawing));
        }
        
        static string GetBool(bool value) { return value ? "&aON" : "&cOFF"; }

        public override void Help(Player p) {
            p.Message("&T/Map [level] [option] <value> &H- Sets [option] on that level");
            p.Message("&HUse &T/Help map options &Hfor a list of options");
            p.Message("&HUse &T/Help map [option] &Hto see description for that option");
        }
        
        public override void Help(Player p, string message) {
            if (message.CaselessEq("options")) {
                p.Message("&HOptions: &f{0}", LevelOptions.Options.Join(o => o.Name));
                p.Message("&HUse &T/Help map [option] &Hto see description for that option");
                return;
            }
            
            LevelOption opt = LevelOptions.Find(message);
            if (opt == null) {
                p.Message("Unrecognised option \"{0}\".", message); return;
            }
            
            bool isMotd = opt.Name == LevelOptions.MOTD;
            string suffix = isMotd ? " <value>" : (HasArgument(opt.Name) ? " [value]" : "");
            
            p.Message("&T/Map [level] {0}{1}", opt.Name, suffix);
            p.Message("&H" + opt.Help);
            if (isMotd) ShowMotdRules(p);
        }
        
        static void ShowMotdRules(Player p) {
            p.Message("&HSpecial rules that can be put in a motd:");
            p.Message("&T-/+hax &H- disallows/allows all hacks");
            p.Message("&T-/+fly &H- disallows/allows flying");
            p.Message("&T-/+noclip &H- disallows/allows noclipping");
            p.Message("&T-/+respawn &H- disallows/allows respawning");
            p.Message("&T-/+thirdperson &H- disallows/allows third person camera");
            p.Message("&T-/+speed &H- disallows/allows speeding");
            p.Message("&T-/+ophax &H- disallows/allows hacks for {0}&S+",
                           Group.GetColoredName(LevelPermission.Operator));
            p.Message("&T-/+push &H- disallows/allows player pushing");
            p.Message("&Tjumpheight=[height] &H- sets max height users can jump up to");
            p.Message("&Thorspeed=[speed] &H- sets base horizontal speed users move at");
            p.Message("&Tjumps=[number] &H- sets max number of consecutive jumps");
        }
    }
}
