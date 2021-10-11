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
    class ItemVTSaw : Item
    {
        WorldInteraction[] interactions = null;
        private double sawingTime;
        private SimpleParticleProperties woodParticles;


        public override void OnLoaded(ICoreAPI api)
        {
            base.OnLoaded(api);

            interactions = ObjectCacheUtil.GetOrCreate(api, "vtaxeInteractions", () =>
            {
                return new WorldInteraction[] {
                    new WorldInteraction()
                        {
                            ActionLangCode = "vintertweaks:itemhelp-saw-sawwood",
                            HotKeyCode = "sneak",
                            MouseButton = EnumMouseButton.Right
                        },
                    };
            });
            sawingTime = 2.0;
            woodParticles = InitializeWoodParticles();
        }

        static ItemVTSaw()
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
        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

            //-- Do not process the stripping action if the player is not sneaking, or no block is selected --//
            if (!byEntity.Controls.Sneak || blockSel == null)
                return;

            Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);


            if ((interactedBlock.FirstCodePart() == "log" && interactedBlock.Variant["type"] == "placed")
                || interactedBlock.FirstCodePart() == "strippedlog"
                || (interactedBlock.FirstCodePart() == "logsection" && interactedBlock.Variant["type"] == "placed")
                || interactedBlock.FirstCodePart() == "planks"
                || interactedBlock.FirstCodePart() == "plankstairs"
                || interactedBlock.FirstCodePart() == "plankslab"
                )
            {

                //-- Stripping time modifier increases the speed at which the wood is stripped. By default, it's based on tool tier --//
                //choppingTime = api.World.Config.GetDouble("BaseBarkStrippingSpeed", 1.0) * this.Attributes["strippingTimeModifier"].AsDouble();
                //byEntity.StartAnimation("axechop");
                handling = EnumHandHandling.Handled;
            }
        }
        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            if (blockSel != null)
            {
                if (secondsUsed >= sawingTime)
                {
                    Block interactedBlock = api.World.BlockAccessor.GetBlock(blockSel.Position);
                    if (secondsUsed >= sawingTime &&
                        ((interactedBlock.FirstCodePart() == "log" && interactedBlock.Variant["type"] == "placed")
                        || interactedBlock.FirstCodePart() == "strippedlog"
                        || (interactedBlock.FirstCodePart() == "logsection" && interactedBlock.Variant["type"] == "placed")
                        || interactedBlock.FirstCodePart() == "planks"
                        || interactedBlock.FirstCodePart() == "plankstairs"
                        || interactedBlock.FirstCodePart() == "plankslab"))
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

                if ((interactedBlock.FirstCodePart() == "log" && interactedBlock.Variant["type"] == "placed")
                        || interactedBlock.FirstCodePart() == "strippedlog"
                        || (interactedBlock.FirstCodePart() == "logsection" && interactedBlock.Variant["type"] == "placed"))
                {
                    api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("plank-" + interactedBlock.Variant["wood"])), 12), blockSel.Position.ToVec3d() +
                            new Vec3d(0.5, 0.5, 0.5));
                }
                else if (interactedBlock.FirstCodePart() == "planks")
                {
                    api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("plank-"+interactedBlock.Variant["wood"])),4), blockSel.Position.ToVec3d() +
                            new Vec3d(0.5, 0.5, 0.5));
                }
                else if (interactedBlock.FirstCodePart() == "plankstairs")
                {
                    api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("plank-" + interactedBlock.Variant["wood"])), 3), blockSel.Position.ToVec3d() +
                            new Vec3d(0.5, 0.5, 0.5));
                }
                else if (interactedBlock.FirstCodePart() == "plankslab")
                {
                    api.World.SpawnItemEntity(new ItemStack(api.World.GetItem(new AssetLocation("plank-" + interactedBlock.Variant["wood"])), 2), blockSel.Position.ToVec3d() +
                            new Vec3d(0.5, 0.5, 0.5));
                }
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
