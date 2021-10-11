using System;
using System.Collections.Generic;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Util;
using Vintagestory.GameContent;

namespace VinterTweaks.Items.Tools
{

    class ItemVTAxe : Item
    {
        WorldInteraction[] interactions = null;
        private double choppingTime;
        private SimpleParticleProperties woodParticles;


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            interactions = ObjectCacheUtil.GetOrCreate(api, "vtaxeInteractions", () =>
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                        {
                            ActionLangCode = "vintertweaks:itemhelp-axe-chopwood",
                            HotKeyCode = "sneak",
                            MouseButton = EnumMouseButton.Right
                        },
                    };
            });
            choppingTime = 2.0;
            woodParticles = InitializeWoodParticles();
        }

        static ItemVTAxe()
        {
            dustParticles.ParticleModel = EnumParticleModel.Quad;
            dustParticles.AddPos.Set(1, 1, 1);
            dustParticles.MinQuantity = 2;
            dustParticles.AddQuantity = 12;
            dustParticles.LifeLength = 4f;
            dustParticles.MinSize = 0.2f;
            dustParticles.MaxSize = 0.5f;
            dustParticles.MinVelocity.Set(-0.4f, -0.4f, -0.4f);
            dustParticles.AddVelocity.Set(0.8f, 1.2f, 0.8f);
            dustParticles.DieOnRainHeightmap = false;
            dustParticles.WindAffectednes = 0.5f;
        }

        public override float OnBlockBreaking(IPlayer player, BlockSelection blockSel, ItemSlot itemslot, float remainingResistance, float dt, int counter)
        {
            ITreeAttribute tempAttr = itemslot.Itemstack.TempAttributes;
            int posx = tempAttr.GetInt("lastposX", -1);
            int posy = tempAttr.GetInt("lastposY", -1);
            int posz = tempAttr.GetInt("lastposZ", -1);
            float treeResistance = tempAttr.GetFloat("treeResistance", 1);

            BlockPos pos = blockSel.Position;

            if (pos.X != posx || pos.Y != posy || pos.Z != posz || counter % 30 == 0)
            {
                Stack<BlockPos> foundPositions = FindTree(player.Entity.World, pos);
                treeResistance = (float)Math.Max(1, Math.Sqrt(foundPositions.Count));

                tempAttr.SetFloat("treeResistance", treeResistance);
            }

            tempAttr.SetInt("lastposX", pos.X);
            tempAttr.SetInt("lastposY", pos.Y);
            tempAttr.SetInt("lastposZ", pos.Z);


            return base.OnBlockBreaking(player, blockSel, itemslot, remainingResistance, dt / treeResistance, counter);
        }

        public override bool OnBlockBrokenWith(IWorldAccessor world, Entity byEntity, ItemSlot itemslot, BlockSelection blockSel, float dropQuantityMultiplier = 1)
        {
            IPlayer byPlayer = null;
            if (byEntity is EntityPlayer) byPlayer = byEntity.World.PlayerByUid(((EntityPlayer)byEntity).PlayerUID);

            double windspeed = api.ModLoader.GetModSystem<WeatherSystemBase>()?.WeatherDataSlowAccess.GetWindSpeed(byEntity.SidedPos.XYZ) ?? 0;

            Stack<BlockPos> foundPositions = FindTree(world, blockSel.Position);

            if (foundPositions.Count == 0)
            {
                return base.OnBlockBrokenWith(world, byEntity, itemslot, blockSel, dropQuantityMultiplier);
            }

            bool damageable = DamagedBy != null && DamagedBy.Contains(EnumItemDamageSource.BlockBreaking);

            float leavesMul = 1;
            float leavesBranchyMul = 0.8f;
            int blocksbroken = 0;

            while (foundPositions.Count > 0)
            {
                BlockPos pos = foundPositions.Pop();
                blocksbroken++;

                Block block = world.BlockAccessor.GetBlock(pos);

                bool isLog = block.BlockMaterial == EnumBlockMaterial.Wood;
                bool isBranchy = block.Code.Path.Contains("branchy");
                bool isLeaves = block.BlockMaterial == EnumBlockMaterial.Leaves;

                world.BlockAccessor.BreakBlock(pos, byPlayer, isLeaves ? leavesMul : (isBranchy ? leavesBranchyMul : 1));

                if (world.Side == EnumAppSide.Client)
                {
                    dustParticles.Color = block.GetRandomColor(world.Api as ICoreClientAPI, pos, BlockFacing.UP);
                    dustParticles.Color |= 255 << 24;
                    dustParticles.MinPos.Set(pos.X, pos.Y, pos.Z);

                    if (block.BlockMaterial == EnumBlockMaterial.Leaves)
                    {
                        dustParticles.GravityEffect = (float)world.Rand.NextDouble() * 0.1f + 0.01f;
                        dustParticles.ParticleModel = EnumParticleModel.Quad;
                        dustParticles.MinVelocity.Set(-0.4f + 4 * (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + 4 * (float)windspeed, 1.2f, 0.8f);

                    }
                    else
                    {
                        dustParticles.GravityEffect = 0.8f;
                        dustParticles.ParticleModel = EnumParticleModel.Cube;
                        dustParticles.MinVelocity.Set(-0.4f + (float)windspeed, -0.4f, -0.4f);
                        dustParticles.AddVelocity.Set(0.8f + (float)windspeed, 1.2f, 0.8f);
                    }


                    world.SpawnParticles(dustParticles);
                }


                if (damageable && isLog)
                {
                    DamageItem(world, byEntity, itemslot);
                }

                if (itemslot.Itemstack == null) return true;

                if (isLeaves && leavesMul > 0.03f) leavesMul *= 0.85f;
                if (isBranchy && leavesBranchyMul > 0.015f) leavesBranchyMul *= 0.6f;
            }

            if (blocksbroken > 35)
            {
                Vec3d pos = blockSel.Position.ToVec3d().Add(0.5, 0.5, 0.5);
                api.World.PlaySoundAt(new AssetLocation("sounds/effect/treefell"), pos.X, pos.Y, pos.Z, byPlayer, false, 32, GameMath.Clamp(blocksbroken / 100f, 0.25f, 1));
            }

            return true;
        }

        public Stack<BlockPos> FindTree(IWorldAccessor world, BlockPos startPos)
        {
            Queue<Vec4i> queue = new Queue<Vec4i>();
            HashSet<BlockPos> checkedPositions = new HashSet<BlockPos>();
            Stack<BlockPos> foundPositions = new Stack<BlockPos>();

            Block block = world.BlockAccessor.GetBlock(startPos);
            if (block.Code == null) return foundPositions;

            string treeFellingGroupCode = block.Attributes?["treeFellingGroupCode"].AsString();
            int spreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;

            // Must start with a log
            if (spreadIndex < 2) return foundPositions;
            if (treeFellingGroupCode == null) return foundPositions;

            queue.Enqueue(new Vec4i(startPos.X, startPos.Y, startPos.Z, spreadIndex));
            foundPositions.Push(startPos);
            checkedPositions.Add(startPos);

            while (queue.Count > 0)
            {
                if (foundPositions.Count > 2500)
                {
                    break;
                }

                Vec4i pos = queue.Dequeue();

                for (int i = 0; i < Vec3i.DirectAndIndirectNeighbours.Length; i++)
                {
                    Vec3i facing = Vec3i.DirectAndIndirectNeighbours[i];
                    BlockPos neibPos = new BlockPos(pos.X + facing.X, pos.Y + facing.Y, pos.Z + facing.Z);

                    float hordist = GameMath.Sqrt(neibPos.HorDistanceSqTo(startPos.X, startPos.Z));
                    float vertdist = (neibPos.Y - startPos.Y);

                    // "only breaks blocks inside an upside down square base pyramid"
                    if (hordist - 1 >= 2 * vertdist) continue;
                    if (checkedPositions.Contains(neibPos)) continue;

                    block = world.BlockAccessor.GetBlock(neibPos);
                    if (block.Code == null || block.Id == 0) continue;

                    string ngcode = block.Attributes?["treeFellingGroupCode"].AsString();

                    // Only break the same type tree blocks
                    if (ngcode != treeFellingGroupCode) continue;

                    // Only spread from "high to low". i.e. spread from log to leaves, but not from leaves to logs
                    int nspreadIndex = block.Attributes?["treeFellingGroupSpreadIndex"].AsInt(0) ?? 0;
                    if (pos.W < nspreadIndex) continue;

                    foundPositions.Push(neibPos.Copy());
                    queue.Enqueue(new Vec4i(neibPos.X, neibPos.Y, neibPos.Z, nspreadIndex));


                    checkedPositions.Add(neibPos);
                }
            }
            return foundPositions;
        }

        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

            //-- Do not process the stripping action if the player is not sneaking, or no block is selected --//
            if (!byEntity.Controls.Sneak || blockSel == null)
                return;

            Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);

            if ((interactedBlock.FirstCodePart() == "log" && interactedBlock.Variant["type"] == "placed")
                || interactedBlock.FirstCodePart() == "strippedlog"
                || (interactedBlock.FirstCodePart() == "logsection" && interactedBlock.Variant["type"] == "placed"))
            {

                //-- Stripping time modifier increases the speed at which the wood is stripped. By default, it's based on tool tier --//
                //choppingTime = api.World.Config.GetDouble("BaseBarkStrippingSpeed", 1.0) * this.Attributes["strippingTimeModifier"].AsDouble();
                byEntity.StartAnimation("axechop");
                handling = EnumHandHandling.Handled;
            }

        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel != null)
            {
                if (secondsUsed >= choppingTime)
                {
                    Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
                    if (secondsUsed >= choppingTime && 
                        ((interactedBlock.FirstCodePart() == "log" && interactedBlock.Variant["type"] == "placed") 
                        || interactedBlock.FirstCodePart() == "strippedlog")
                        || (interactedBlock.FirstCodePart() == "logsection" && interactedBlock.Variant["type"] == "placed"))
                        SpawnLoot(blockSel, byEntity);
                    return false;
                }

            }
            return true;

        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            byEntity.StopAnimation("axechop");
            base.OnHeldInteractStop(secondsUsed, slot, byEntity, blockSel, entitySel);
        }
        //-- Spawn bark pieces when the player meets/exceeds the time it takes to strip the log. Also changes the interacted block to a stripped log variant --//
        private void SpawnLoot(BlockSelection blockSel, EntityAgent byEntity)
        {
            if (api.Side == EnumAppSide.Server)
            {
                Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);

                api.World.BlockAccessor.SetBlock(0, blockSel.Position);
                api.World.BlockAccessor.MarkBlockDirty(blockSel.Position);

                api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("firewood")), 4), blockSel.Position.ToVec3d() +
                        new Vec3d(0.5, 0.5, 0.5));

                if (byEntity is EntityPlayer player)
                    this.DamageItem(api.World, byEntity, player.RightHandItemSlot, 1);
            }


        }

        //Particle Handlers
        private SimpleParticleProperties InitializeWoodParticles()
        {
            return new SimpleParticleProperties()
            {
                MinPos = new Vec3d(),
                AddPos = new Vec3d(),
                MinQuantity = 0,
                AddQuantity = 3,
                Color = ColorUtil.ToRgba(100, 200, 200, 200),
                GravityEffect = 1f,
                WithTerrainCollision = true,
                ParticleModel = EnumParticleModel.Quad,
                LifeLength = 0.5f,
                MinVelocity = new Vec3f(-1, 2, -1),
                AddVelocity = new Vec3f(2, 0, 2),
                MinSize = 0.07f,
                MaxSize = 0.1f,
                WindAffected = true
            };
        }

        static SimpleParticleProperties dustParticles = new SimpleParticleProperties()
        {
            MinPos = new Vec3d(),
            AddPos = new Vec3d(),
            MinQuantity = 0,
            AddQuantity = 3,
            Color = ColorUtil.ToRgba(100, 200, 200, 200),
            GravityEffect = 1f,
            WithTerrainCollision = true,
            ParticleModel = EnumParticleModel.Quad,
            LifeLength = 0.5f,
            MinVelocity = new Vec3f(-1, 2, -1),
            AddVelocity = new Vec3f(2, 0, 2),
            MinSize = 0.07f,
            MaxSize = 0.1f,
            WindAffected = true
        };

        private void SetParticleColourAndPosition(int colour, Vec3d minpos)
        {
            SetParticleColour(colour);

            woodParticles.MinPos = minpos;
            woodParticles.AddPos = new Vec3d(1, 1, 1);
        }
        private void SetParticleColour(int colour)
        {
            woodParticles.Color = colour;
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return interactions;
        }
    }

}
