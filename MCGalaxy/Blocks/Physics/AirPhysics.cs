/*
    Copyright 2010 MCSharp team (Modified for use with MCZall/MCLawl/MCGalaxy)
        
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
using BlockID = System.UInt16;

namespace MCGalaxy.Blocks.Physics {
    
    public enum AirFlood { Full, Layer, Down, Up, }
    public static class AirPhysics {
        
        public static void DoAir(Level lvl, ref PhysInfo C) {
            if (C.Data.Type1 == PhysicsArgs.Custom) {
                DoorPhysics.Do(lvl, ref C); return;
            }
            
            ushort x = C.X, y = C.Y, z = C.Z;
            ActivateablePhysics.CheckNeighbours(lvl, x, y, z);
            ActivateablePhysics.CheckAt(lvl, x, (ushort)(y - 1), z); 

            //Edge of map water
            if (lvl.Config.EdgeWater && (y < lvl.Config.EdgeLevel && y >= (lvl.Config.EdgeLevel + lvl.Config.SidesOffset))) {
                if (x == 0 || x == lvl.Width - 1 || z == 0 || z == lvl.Length - 1) {
                    BlockID horizon = lvl.Config.HorizonBlock;
                    lvl.AddUpdate(C.Index, horizon == Block.Invalid ? Block.Water : horizon);
                }
            }
            if (!C.Data.HasWait) C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }
        
        public static void DoFlood(Level lvl, ref PhysInfo C, AirFlood mode, BlockID block) {           
            if (C.Data.Data >= 1) {
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
                C.Data.Data = PhysicsArgs.RemoveFromChecks; return;
            }
            
            ushort x = C.X, y = C.Y, z = C.Z;            
            FloodAir(lvl, (ushort)(x + 1), y, z, block);
            FloodAir(lvl, (ushort)(x - 1), y, z, block);
            FloodAir(lvl, x, y, (ushort)(z + 1), block);
            FloodAir(lvl, x, y, (ushort)(z - 1), block);
            
            switch (mode) {
                case AirFlood.Full:
                    FloodAir(lvl, x, (ushort)(y - 1), z, block);
                    FloodAir(lvl, x, (ushort)(y + 1), z, block);
                    break;
                case AirFlood.Layer:
                    break;
                case AirFlood.Down:
                    FloodAir(lvl, x, (ushort)(y - 1), z, block);
                    break;
                case AirFlood.Up:
                    FloodAir(lvl, x, (ushort)(y + 1), z, block);
                    break;
            }
            C.Data.Data++;
        }
        
        static void FloodAir(Level lvl, ushort x, ushort y, ushort z, BlockID block) {
            int index;
            BlockID curBlock = lvl.GetBlock(x, y, z, out index);
            if (curBlock == Block.WaterDown || curBlock == Block.FiniteWater ||
                curBlock == Block.LavaDown || curBlock == Block.FiniteLava ) {
                lvl.AddUpdate(index, block);
            }
        }
    }
}
