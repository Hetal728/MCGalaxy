/*
	Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
	
	Dual-licensed under the	Educational Community License, Version 2.0 and
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
using System.IO;
using System.Threading;
namespace MCGalaxy.Commands
{
    public sealed class CmdLoad : Command
    {
        public override string name { get { return "load"; } }
        public override string shortcut { get { return ""; } }
        public override string type { get { return CommandTypes.World; } }
        public override bool museumUsable { get { return true; } }
        public override LevelPermission defaultRank { get { return LevelPermission.Operator; } }
        public override CommandAlias[] Aliases {
            get { return new[] { new CommandAlias("mapload"), new CommandAlias("wload") }; }
        }
        public CmdLoad() { }

        public override void Use(Player p, string message)
        {
            try
            {
                if (message == "") { Help(p); return; }
                if (message.Split(' ').Length > 2) { Help(p); return; }
                int pos = message.IndexOf(' ');
                string phys = "0";
                if (pos != -1)
                {
                    phys = message.Substring(pos + 1);
                    message = message.Substring(0, pos).ToLower();
                }
                else
                {
                    message = message.ToLower();
                }

                Level[] loaded = LevelInfo.Loaded.Items;
                foreach (Level l in loaded)
                {
                    if (l.name == message) { Player.SendMessage(p, message + " is already loaded!"); return; }
                }

                if (!LevelInfo.ExistsOffline(message)) {
                    Player.SendMessage(p, "Level \"" + message + "\" doesn't exist!"); return;
                }

                Level level = Level.Load(message);

                if (level == null)
                {
                	if (File.Exists(LevelInfo.LevelPath(message) + ".backup"))
                    {
                    	if (LevelInfo.ExistsOffline(message))
                        {
                            Server.s.Log(message + ".lvl file is corrupt. Deleting and replacing with " + message + ".lvl.backup file.");
                            File.Delete(LevelInfo.LevelPath(message));
                        }
                        Server.s.Log("Attempting to load backup");
                        File.Copy(LevelInfo.LevelPath(message) + ".backup", LevelInfo.LevelPath(message), true);
                        level = Level.Load(message);
                        if (level == null)
                        {
                            Player.SendMessage(p, "Loading backup failed.");
                            string backupPath = Server.backupLocation;
                            if (Directory.Exists(backupPath + "/" + message))
                            {
                                int backupNumber = Directory.GetDirectories(backupPath + "/" + message).Length;
                                Server.s.Log("Attempting to load latest backup, number " + backupNumber + " instead.");
                                File.Copy(LevelInfo.BackupPath(message, backupNumber.ToString()), LevelInfo.LevelPath(message), true);
                                level = Level.Load(message);
                                if (level == null)
                                {
                                    Player.SendMessage(p, "Loading latest backup failed as well.");
                                }
                            } 
                            return;
                        }
                    }
                    else
                    {
                        Player.SendMessage(p, "Backup of " + message + " does not exist.");
                        return;
                    }
                }

                if (p != null) if (level.permissionvisit > p.group.Permission)
                    {
                        Player.SendMessage(p, "This map is for " + Level.PermissionToName(level.permissionvisit) + " only!");
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        return;
                    }

                loaded = LevelInfo.Loaded.Items;
                foreach (Level l in loaded)
                {
                    if (l.name == message)
                    {
                        Player.SendMessage(p, message + " is already loaded!");
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        return;
                    }
                }

                LevelInfo.Loaded.Add(level);
                if (p == null || !p.hidden) { 
                	Player.GlobalMessage("Level \"" + level.name + "\" loaded."); 
                }
                /*try
                {
                    Gui.Window.thisWindow.UpdatePlayerMapCombo();
                    Gui.Window.thisWindow.UnloadedlistUpdate();
                    Gui.Window.thisWindow.UpdateMapList("'");
                   
                    
                }
                catch { }*/
                try
                {
                    int temp = int.Parse(phys);
                    if (temp >= 1 && temp <= 5)
                    {
                        level.setPhysics(temp);
                    }
                }
                catch
                {
                    Player.SendMessage(p, "Physics variable invalid");
                }
            }
            catch (Exception e)
            {
                Player.GlobalMessage("An error occured with /load");
                Server.ErrorLog(e);
            }
            finally
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
            }
        }
        public override void Help(Player p)
        {
            Player.SendMessage(p, "/load <level> <physics> - Loads a level.");
        }
    }
}
