/*
    Copyright 2015 MCGalaxy
    
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
using System.Collections.Generic;
using MCGalaxy.Commands;
using MCGalaxy.Generator.Foliage;

namespace MCGalaxy {
    
    public sealed class LevelOption {
        public string Name, Help;
        public LevelOptions.OptionSetter SetFunc;
        
        public LevelOption(string name, LevelOptions.OptionSetter func, string help) {
            Name = name; SetFunc = func; Help = help;
        }
    }
    
    public static class LevelOptions {        
        public delegate void OptionSetter(Player p, Level lvl, string value);
        public const string MOTD = "motd", RealmOwner = "RealmOwner", TreeType = "TreeType", Speed = "Speed";
        public const string Overload = "Overload", Fall = "Fall", Drown = "Drown", Finite = "Finite", AI = "AI";
        public const string Edge = "Edge", Grass = "Grass", Death = "Death", Killer = "Killer", Unload = "Unload";
        public const string Goto = "LoadOnGoto", Decay = "LeafDecay", Flow = "RandomFlow", Trees = "GrowTrees";
        public const string Chat = "Chat", Guns = "Guns", Buildable = "Buildable", Deletable = "Deletable";
        public const string LoadDelay = "LoadDelay", Drawing = "Drawing", Authors = "Authors";
        public const string FiniteHighWater = "FiniteHighWater";
        
        public static List<LevelOption> Options = new List<LevelOption>() {
             new LevelOption(MOTD,       SetMotd,  "&HSets the motd for this map. (leave blank to use default motd)"),
             new LevelOption(RealmOwner, SetOwner, "&HSets the players allowed to use /realm on this map."),
             new LevelOption(TreeType,   SetTree,  "&HSets the type of trees saplings grow into."),
             new LevelOption(Speed,      SetSpeed, "&HSets the delay (in milliseconds) between physics ticks. " +
                             "E.g. a delay of 250 milliseconds means 4 ticks per second."),
             new LevelOption(Overload, SetOverload, "&HSets how long (in milliseconds) a physics tick can run over before physics shuts off. " +
                             "E.g a speed of 250 and overload of 500 means physics shuts off if a physics tick takes over 750 milliseconds."),
             new LevelOption(Fall,   SetFall,   "&HSets how many blocks you can fall before dying."),
             new LevelOption(Drown,  SetDrown,  "&HSets how long you can stay underwater (in tenths of a second) before drowning."),
             new LevelOption(Finite, SetFinite, "&HWhether all liquids are finite."),
             new LevelOption(FiniteHighWater, SetFiniteHighWater, "&HWhether Only active water above edge level is finite."),
             new LevelOption(AI,     SetAI,     "&HAI will make animals hunt or flee."),
             new LevelOption(Edge,   SetEdge,   "&HWhether water flows from the map edges."),
             new LevelOption(Grass,  SetGrass,  "&HWhether grass auto grows or not."),
             new LevelOption(Death,  SetDeath,  "&HWhether you can die from falling or drowning."),
             new LevelOption(Killer, SetKiller, "&HWhether certain blocks (e.g. nerve_gas) kill you."),
             new LevelOption(Unload, SetUnload, "&HWhether the map auto unloads when no one's there."),
             new LevelOption(Goto,   SetGoto,   "&HWhether the map auto loads when /goto is used."),
             new LevelOption(Decay,  SetDecay,  "&HWhether leaves not connected to log blocks within 4 blocks randomly disappear."),
             new LevelOption(Flow,   SetFlow,   "&HWhether flooding liquids flow less uniformly."),
             new LevelOption(Trees,  SetTrees,  "&HWhether saplings grow into trees after a while."),
             new LevelOption(Chat,   SetChat,   "&HWhether chat is only seen from and sent to players in the map."),
             new LevelOption(Guns,   SetGuns,   "&HWhether guns and missiles can be used"),
             new LevelOption(Buildable, SetBuildable, "&HWhether any blocks can be placed by players."),
             new LevelOption(Deletable, SetDeletable, "&HWhether any blocks can be deleted by players."),
             new LevelOption(Drawing,   SetDrawing,   "&HWhether drawing commands (e.g /z) can be used on this map."),
             new LevelOption(LoadDelay, SetLoadDelay, "&HSets the delay before the end of the map is sent. " +
                             "Only useful for forcing players to see the map's MOTD at the loading screen."),
             new LevelOption(Authors, SetAuthors, "&HSets authors of map. Only shown when running games"),
        };

        public static LevelOption Find(string opt) {
            if (opt.CaselessEq("ps"))   opt = Speed;
            if (opt.CaselessEq("load")) opt = Goto;
            if (opt.CaselessEq("leaf")) opt = Decay;
            if (opt.CaselessEq("flow")) opt = Flow;
            if (opt.CaselessEq("tree")) opt = Trees;
            
            foreach (LevelOption option in Options) {
                if (option.Name.CaselessEq(opt)) return option;
            }
            return null;
        }

        static void SetMotd(Player p, Level lvl, string value) {
            lvl.Config.MOTD = value.Length == 0 ? "ignore" : value;
            lvl.Message("Map's MOTD was changed to: &b" + lvl.Config.MOTD);
            
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                // Some clients will freeze or crash if we send a MOTD packet, but don't follow it up by a new map.
                // Although checking for CPE extension support is preferred, also send to whitelisted clients for maximum compatibility
                bool motdOnly = pl.Supports(CpeExt.InstantMOTD) || (pl.appName != null && pl.appName.CaselessStarts("classicalsharp"));
                if (motdOnly) {
                    pl.SendMapMotd();
                } else {
                    PlayerActions.ReloadMap(pl);
                }
            }
        }
        
        static void SetOwner(Player p, Level lvl, string value) {
            lvl.Config.RealmOwner = value.Replace(", ", ",").Replace(" ", ",");
            if (value.Length == 0) p.Message("Removed realm owner for this level.");
            else p.Message("Set realm owner/owners of this level to {0}.", value);
        }
        
        static void SetTree(Player p, Level lvl, string value) {
            if (value.Length == 0) {
                p.Message("Reset tree type to default.");
                lvl.Config.TreeType = "fern";
                return;
            }
            
            Tree tree = Tree.Find(value);
            if (tree == null) {
                p.Message("Tree type {0} not found.", value);
                p.Message("Tree types: {0}", Tree.TreeTypes.Join(t => t.Key));
                return;
            }
            
            lvl.Config.TreeType = value.ToLower();
            p.Message("Set tree type that saplings grow into to {0}.", value);
        }
        
        static void SetFinite(Player p, Level l, string v) { Toggle(p, l, ref l.Config.FiniteLiquids, "Finite mode"); }
        static void SetFiniteHighWater(Player p, Level l, string v) { Toggle(p, l, ref l.Config.FiniteHighWater, "FiniteHighWater"); }
        static void SetAI(Player p,     Level l, string v) { Toggle(p, l, ref l.Config.AnimalHuntAI, "Animal AI"); }
        static void SetEdge(Player p,   Level l, string v) { Toggle(p, l, ref l.Config.EdgeWater, "Edge water"); }
        static void SetGrass(Player p,  Level l, string v) { Toggle(p, l, ref l.Config.GrassGrow, "Growing grass"); }
        static void SetDeath(Player p,  Level l, string v) { Toggle(p, l, ref l.Config.SurvivalDeath, "Survival death"); }
        static void SetKiller(Player p, Level l, string v) { Toggle(p, l, ref l.Config.KillerBlocks, "Killer blocks"); }
        static void SetUnload(Player p, Level l, string v) { Toggle(p, l, ref l.Config.AutoUnload, "Auto unload"); }
        static void SetGoto(Player p,   Level l, string v) { Toggle(p, l, ref l.Config.LoadOnGoto, "Load on goto"); }
        static void SetDecay(Player p,  Level l, string v) { Toggle(p, l, ref l.Config.LeafDecay, "Leaf decay"); }
        static void SetFlow(Player p,   Level l, string v) { Toggle(p, l, ref l.Config.RandomFlow, "Random flow"); }
        static void SetTrees(Player p,  Level l, string v) { Toggle(p, l, ref l.Config.GrowTrees, "Tree growing"); }
        static void SetBuildable(Player p, Level l, string v) { TogglePerms(p, l, ref l.Config.Buildable, "Buildable"); }
        static void SetDeletable(Player p, Level l, string v) { TogglePerms(p, l, ref l.Config.Deletable, "Deletable"); }
        
        static void SetChat(Player p, Level l, string v) {
            Toggle(p, l, ref l.Config.ServerWideChat, "Local level only chat", true);
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                if (pl.level == l) TabList.Update(pl, true);
            }
        }
        
        static void SetDrawing(Player p, Level l, string v) { 
            Toggle(p, l, ref l.Config.Drawing, "Drawing commands"); 
        }
        
        static void SetLoadDelay(Player p, Level l, string value) {
            int raw = 0;
            if (!CommandParser.GetInt(p, value, "Load delay", ref raw, 0, 2000)) return;
            SetInt(l, raw, ref l.Config.LoadDelay, "Load delay");
        }
        
        static void SetSpeed(Player p, Level l, string value) {
            int raw = 0;
            if (!CommandParser.GetInt(p, value, "Physics speed", ref raw, 10)) return;
            SetInt(l, raw, ref l.Config.PhysicsSpeed, "Physics speed");
        }
        
        static void SetOverload(Player p, Level l, string value) {
            int raw = 0;
            if (!CommandParser.GetInt(p, value, "Physics overload", ref raw, 500)) return;
            
            if (p.Rank < LevelPermission.Admin && raw > 2500) {
                p.Message("Only SuperOPs may set physics overload higher than 2500"); return;
            }
            SetInt(l, raw, ref l.Config.PhysicsOverload, "Physics overload");
        }
        
        static void SetFall(Player p, Level l, string value) {
            int raw = 0;
            if (!CommandParser.GetInt(p, value, "Fall distance", ref raw)) return;
            SetInt(l, raw, ref l.Config.FallHeight, "Fall distance");
        }
        
        static void SetDrown(Player p, Level l, string value) {
            int raw = 0;
            if (!CommandParser.GetInt(p, value, "Drown time (in tenths of a second)", ref raw)) return;
            SetInt(l, raw, ref l.Config.DrownTime, "Drown time (in tenths of a second)");
        }
        
        static void SetInt(Level lvl, int raw, ref int target, string name) {
            target = raw;
            lvl.Message(name + ": &b" + target);
        }
        
        
        static void SetGuns(Player p, Level lvl, string value) {
            Toggle(p, lvl, ref lvl.Config.Guns, "Guns allowed");
            if (lvl.Config.Guns) return;
            
            Player[] players = PlayerInfo.Online.Items;
            foreach (Player pl in players) {
                if (pl.level != lvl || pl.weapon == null) continue;
                pl.weapon.Disable();
            }
        }
        
        static void SetAuthors(Player p, Level lvl, string value) {
            lvl.Config.Authors = value.Replace(", ", ",").Replace(" ", ",");
            p.Message("Map authors set to: &b" + lvl.Config.Authors);
        }
        
        static void TogglePerms(Player p, Level lvl, ref bool target, string name) {
            Toggle(p, lvl, ref target, name);
            lvl.UpdateBlockPermissions();
        }
        
        static void Toggle(Player p, Level lvl, ref bool target, string name, bool not = false) {
            target = !target;
            bool display = not ? !target : target;
            string targetStr = display ? "&aON" : "&cOFF";
            
            lvl.Message(name + ": " + targetStr);          
            if (p.level != lvl) p.Message(name + ": " + targetStr);
        }
    }
}
