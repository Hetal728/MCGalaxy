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
using System.Collections.Generic;
using BlockID = System.UInt16;

namespace MCGalaxy.Blocks.Physics {
    
    public static class FinitePhysics {
        
        public unsafe static void DoWaterOrLava(Level lvl, ref PhysInfo C) {
            Random rand = lvl.physRandom;            
            ushort x = C.X, y = C.Y, z = C.Z;
            int index;
            BlockID below = lvl.GetBlock(x, (ushort)(y - 1), z, out index);
            
            if (below == Block.Air) {
                lvl.AddUpdate(index, C.Block, C.Data);
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
                CheckAround(lvl, x, y, z, index);
                C.Data.ResetTypes();
            } else if (below == Block.StillWater || below == Block.StillLava) {
                lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
                CheckAround(lvl, x, y, z, index);
                C.Data.ResetTypes();
            } else {
                const int count = 25;
                int* indices = stackalloc int[count];
                for (int i = 0; i < count; ++i)
                    indices[i] = i;

                for (int k = count - 1; k > 1; --k) {
                    int randIndx = rand.Next(k);
                    int temp = indices[k];
                    indices[k] = indices[randIndx]; // move random num to end of list.
                    indices[randIndx] = temp;
                }

                for (int j = 0; j < count; j++) {
                    int i = indices[j];
                    ushort posX = (ushort)(x + (i / 5) - 2);
                    ushort posZ = (ushort)(z + (i % 5) - 2);
                    
                    if (lvl.IsAirAt(posX, (ushort)(y - 1), posZ) && lvl.IsAirAt(posX, y, posZ)) {
                        if (posX < x) {
                            posX = (ushort)((posX + x) / 2);
                        } else {
                            posX = (ushort)((posX + x + 1) / 2); // ceiling division
                        }
                        
                        if (posZ < z) {
                            posZ = (ushort)((posZ + z) / 2);
                        } else {
                            posZ = (ushort)((posZ + z + 1) / 2);
                        }

                        if (lvl.IsAirAt(posX, y, posZ, out index) && lvl.AddUpdate(index, C.Block, C.Data)) {
                            lvl.AddUpdate(C.Index, Block.Air, default(PhysicsArgs));
                            CheckAround(lvl, x, y, z, index);
                            C.Data.ResetTypes();
                            return;
                        }
                    }
                }

                // Not moving - don't retry.
                C.Data.Data = PhysicsArgs.RemoveFromChecks;
            }
        }

        static void CheckAround(Level lvl, ushort x, ushort y, ushort z, int newind) {
            int index, dx, dy, dz;
            for (dx = -2; dx<3; dx++)
                for (dy = -2; dy<3; dy++)
                    for (dz = -2; dz<3; dz++)
                    {
                        if (dx==0 && dy==0 && dz==0) continue;

                        BlockID block = lvl.GetBlock((ushort)(x+dx), (ushort)(y+dy), (ushort)(z+dz), out index);
                        if (index == newind) continue;

                        if (block == Block.FiniteWater || block == Block.FiniteLava)
                            lvl.AddCheck(index);
                    }
        }

        static bool Expand(Level lvl, ushort x, ushort y, ushort z) {
            int index;
            return lvl.IsAirAt(x, y, z, out index) && lvl.AddUpdate(index, Block.FiniteWater, default(PhysicsArgs));
        }
        
        public unsafe static void DoFaucet(Level lvl, ref PhysInfo C) {
            Random rand = lvl.physRandom;            
            ushort x = C.X, y = C.Y, z = C.Z;
            
            const int count = 6;
            int* indices = stackalloc int[count];
            for (int i = 0; i < count; ++i)
                indices[i] = i;

            for (int k = count - 1; k > 1; --k) {
                int randIndx = rand.Next(k);
                int temp = indices[k];
                indices[k] = indices[randIndx]; // move random num to end of list.
                indices[randIndx] = temp;
            }

            for (int j = 0; j < count; j++) {
                int i = indices[j];
                switch (i) {
                    case 0:
                        if (Expand(lvl, (ushort)(x - 1), y, z)) return;
                        break;
                    case 1:
                        if (Expand(lvl, (ushort)(x + 1), y, z)) return;
                        break;
                    case 2:
                        if (Expand(lvl, x, (ushort)(y - 1), z)) return;
                        break;
                    case 3:
                        if (Expand(lvl, x, (ushort)(y + 1), z)) return;
                        break;
                    case 4:
                        if (Expand(lvl, x, y, (ushort)(z - 1))) return;
                        break;
                    case 5:
                        if (Expand(lvl, x, y, (ushort)(z + 1))) return;
                        break;
                }
            }
        }
    }
}
