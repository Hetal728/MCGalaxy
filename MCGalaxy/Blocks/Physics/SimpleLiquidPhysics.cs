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
using MCGalaxy.Games;
using BlockID = System.UInt16;

namespace MCGalaxy.Blocks.Physics {
    
    public static class SimpleLiquidPhysics {
        
        public static void DoWater(Level lvl, ref PhysInfo C) {
            if (lvl.Config.FiniteLiquids) {
                if (lvl.Config.FiniteHighWater) {
                    ushort y = C.Y;
                    if (y >= lvl.Config.EdgeLevel) {
                        FinitePhysics.DoWaterOrLava(lvl, ref C);
                        return;
                    }
                } else {
                    FinitePhysics.DoWaterOrLava(lvl, ref C);
                    return;
                }
            }
            if (lvl.Config.RandomFlow) {
                DoWaterRandowFlow(lvl, ref C);
            } else {
                DoWaterUniformFlow(lvl, ref C);
            }
        }
        
        public static void DoLava(Level lvl, ref PhysInfo C) {
            // upper 3 bits are time delay
            if (C.Data.Data < (4 << 5)) {
                C.Data.Data += (1 << 5); return;
            }
            
            if (lvl.Config.FiniteLiquids) {
                FinitePhysics.DoWaterOrLava(lvl, ref C);
            } else if (lvl.Config.RandomFlow) {
                DoLavaRandowFlow(lvl, ref C, true);
            } else {
                DoLavaUniformFlow(lvl, ref C, true);
            }
        }
        
        public static void DoFastLava(Level lvl, ref PhysInfo C) {
            if (lvl.Config.RandomFlow) {
                DoLavaRandowFlow(lvl, ref C, false);
                if (C.Data.Data != PhysicsArgs.RemoveFromChecks)
                    C.Data.Data = 0; // no lava delay
            } else {
                DoLavaUniformFlow(lvl, ref C, false);
            }
        }
        
        
        const int flowed_xMax = (1 << 0);
        const int flowed_xMin = (1 << 1);
        const int flowed_zMax = (1 << 2);
        const int flowed_zMin = (1 << 3);
        const int flowed_yMin = (1 << 4);
        const int flowed_maskAll = 0x1F;
        
        
        static void DoWaterRandowFlow(Level lvl, ref PhysInfo C) {
            Random rand = lvl.physRandom;
            ushort x = C.X, y = C.Y, z = C.Z;
            
            if (!lvl.CheckSpongeWater(x, y, z)) {
                byte data = C.Data.Data;
                BlockID block = C.Block;
                if (y < lvl.Height - 1) {
                    CheckFallingBlocks(lvl, C.Index + lvl.Width * lvl.Length);
                }
                
                if ((data & flowed_xMax) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysWater(lvl, (ushort)(x + 1), y, z, block);
                    data |= flowed_xMax;
                }
                if ((data & flowed_xMin) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysWater(lvl, (ushort)(x - 1), y, z, block);
                    data |= flowed_xMin;
                }
                if ((data & flowed_zMax) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysWater(lvl, x, y, (ushort)(z + 1), block);
                    data |= flowed_zMax;
                }
                if ((data & flowed_zMin) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysWater(lvl, x, y, (ushort)(z - 1), block);
                    data |= flowed_zMin;
                }
                if ((data & flowed_yMin) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysWater(lvl, x, (ushort)(y - 1), z, block);
                    data |=  flowed_yMin;
                }

                if ((data & flowed_xMax) == 0 && WaterBlocked(lvl, (ushort)(x + 1), y, z)) {
                    data |= flowed_xMax;
                }
                if ((data & flowed_xMin) == 0 && WaterBlocked(lvl, (ushort)(x - 1), y, z)) {
                    data |= flowed_xMin;
                }
                if ((data & flowed_zMax) == 0 && WaterBlocked(lvl, x, y, (ushort)(z + 1))) {
                    data |= flowed_zMax;
                }
                if ((data & flowed_zMin) == 0 && WaterBlocked(lvl, x, y, (ushort)(z - 1))) {
                    data |= flowed_zMin;
                }
                if ((data & flowed_yMin) == 0 && WaterBlocked(lvl, x, (ushort)(y - 1), z)) {
                    data |= flowed_yMin;
                }
                
                // Have we spread now (or been blocked from spreading) in all directions?
                C.Data.Data = data;
                if (!C.Data.HasWait && (data & 0x1F) == 0x1F) {
                    C.Data.Data = PhysicsArgs.RemoveFromChecks;
                }
            } else { //was placed near sponge
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
                if (!C.Data.HasWait) {
                    C.Data.Data = PhysicsArgs.RemoveFromChecks;
                }
            }
        }
        
        static void DoWaterUniformFlow(Level lvl, ref PhysInfo C) {
            ushort x = C.X, y = C.Y, z = C.Z;
            
            if (!lvl.CheckSpongeWater(x, y, z)) {
                BlockID block = C.Block;
                if (y < lvl.Height - 1) {
                    CheckFallingBlocks(lvl, C.Index + lvl.Width * lvl.Length);
                }
                
                LiquidPhysics.PhysWater(lvl, (ushort)(x + 1), y, z, block);
                LiquidPhysics.PhysWater(lvl, (ushort)(x - 1), y, z, block);
                LiquidPhysics.PhysWater(lvl, x, y, (ushort)(z + 1), block);
                LiquidPhysics.PhysWater(lvl, x, y, (ushort)(z - 1), block);
                LiquidPhysics.PhysWater(lvl, x, (ushort)(y - 1), z, block);
            } else { //was placed near sponge
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
            }
            if (!C.Data.HasWait) C.Data.Data = PhysicsArgs.RemoveFromChecks;
        }
        
        static bool WaterBlocked(Level lvl, ushort x, ushort y, ushort z) {
            BlockID block = lvl.GetBlock(x, y, z);
            if (LSGame.Instance.Running && LSGame.Instance.Map == lvl && LSGame.Instance.InSafeZone(x, y, z))
                return true;

            switch (block) {
                case Block.Air:
                case Block.Lava:
                case Block.FastLava:
                case Block.Deadly_ActiveLava:
                    if (!lvl.CheckSpongeWater(x, y, z)) return false;
                    break;

                case Block.Sand:
                case Block.Gravel:
                case Block.FloatWood:
                    return false;
                    
                case Block.Invalid:
                    return true;
                    
                default:
                    // Adv physics kills flowers, mushroom blocks in water
                    if (!lvl.Props[block].WaterKills) break;
                    
                    if (lvl.physics > 1 && !lvl.CheckSpongeWater(x, y, z)) return false;
                    break;
            }
            return true;
        }
        
        
        static void DoLavaRandowFlow(Level lvl, ref PhysInfo C, bool checkWait) {
            Random rand = lvl.physRandom;
            ushort x = C.X, y = C.Y, z = C.Z;

            if (!lvl.CheckSpongeLava(x, y, z)) {
                byte data = C.Data.Data;
                // Upper 3 bits are time flags - reset random delay
                data &= flowed_maskAll;
                data |= (byte)(rand.Next(3) << 5);
                BlockID block = C.Block;

                if ((data & flowed_xMax) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysLava(lvl, (ushort)(x + 1), y, z, block);
                    data |= flowed_xMax;
                }
                if ((data & flowed_xMin) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysLava(lvl, (ushort)(x - 1), y, z, block);
                    data |= flowed_xMin;
                }
                if ((data & flowed_zMax) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysLava(lvl, x, y, (ushort)(z + 1), block);
                    data |= flowed_zMax;
                }
                if ((data & flowed_zMin) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysLava(lvl, x, y, (ushort)(z - 1), block);
                    data |= flowed_zMin;
                }
                if ((data & flowed_yMin) == 0 && rand.Next(4) == 0) {
                    LiquidPhysics.PhysLava(lvl, x, (ushort)(y - 1), z, block);
                    data |= flowed_yMin;
                }

                if ((data & flowed_xMax) == 0 && LavaBlocked(lvl, (ushort)(x + 1), y, z)) {
                    data |= flowed_xMax;
                }
                if ((data & flowed_xMin) == 0 && LavaBlocked(lvl, (ushort)(x - 1), y, z)) {
                    data |= flowed_xMin;
                }
                if ((data & flowed_zMax) == 0 && LavaBlocked(lvl, x, y, (ushort)(z + 1))) {
                    data |= flowed_zMax;
                }
                if ((data & flowed_zMin) == 0 && LavaBlocked(lvl, x, y, (ushort)(z - 1))) {
                    data |= flowed_zMin;
                }
                if ((data & flowed_yMin) == 0 && LavaBlocked(lvl, x, (ushort)(y - 1), z)) {
                    data |= flowed_yMin;
                }
                
                // Have we spread now (or been blocked from spreading) in all directions?
                C.Data.Data = data;
                if ((!checkWait || !C.Data.HasWait) && (data & flowed_maskAll) == flowed_maskAll) {
                    C.Data.Data = PhysicsArgs.RemoveFromChecks;
                }
            } else { //was placed near sponge
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
                if (!checkWait || !C.Data.HasWait) {
                    C.Data.Data = PhysicsArgs.RemoveFromChecks;
                }
            }
        }
        
        static void DoLavaUniformFlow(Level lvl, ref PhysInfo C, bool checkWait) {
            ushort x = C.X, y = C.Y, z = C.Z;
            
            if (!lvl.CheckSpongeLava(x, y, z)) {
                BlockID block = C.Block;
                LiquidPhysics.PhysLava(lvl, (ushort)(x + 1), y, z, block);
                LiquidPhysics.PhysLava(lvl, (ushort)(x - 1), y, z, block);
                LiquidPhysics.PhysLava(lvl, x, y, (ushort)(z + 1), block);
                LiquidPhysics.PhysLava(lvl, x, y, (ushort)(z - 1), block);
                LiquidPhysics.PhysLava(lvl, x, (ushort)(y - 1), z, block);
            } else { //was placed near sponge
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
            }
            
            if (!checkWait || !C.Data.HasWait) {
                C.Data.Data = PhysicsArgs.RemoveFromChecks;
            }
        }
        
        static bool LavaBlocked(Level lvl, ushort x, ushort y, ushort z) {
            BlockID block = lvl.GetBlock(x, y, z);
            if (LSGame.Instance.Running && LSGame.Instance.Map == lvl && LSGame.Instance.InSafeZone(x, y, z))
                return true;
            
            switch (block) {
                case Block.Air:
                    return false;

                case Block.Water:
                case Block.Deadly_ActiveWater:
                    if (!lvl.CheckSpongeLava(x, y, z)) return false;
                    break;

                case Block.Sand:
                case Block.Gravel:
                    return false;
                    
                case Block.Invalid:
                    return true;
                    
                default:
                    // Adv physics kills flowers, wool, mushrooms, and wood type blocks in lava
                    if (!lvl.Props[block].LavaKills) break;

                    if (lvl.physics > 1 && !lvl.CheckSpongeLava(x, y, z)) return false;
                    break;
            }
            return true;
        }
        
        static void CheckFallingBlocks(Level lvl, int b) {
            switch (lvl.blocks[b]) {
                case Block.Sand:
                case Block.Gravel:
                case Block.FloatWood:
                    lvl.AddCheck(b); break;
                default:
                    break;
            }
        }
    }
}
