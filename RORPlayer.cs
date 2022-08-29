﻿using Microsoft.Xna.Framework;
using RORMod.Buffs;
using RORMod.Buffs.Debuff;
using RORMod.NPCs;
using Terraria;
using Terraria.Audio;
using Terraria.DataStructures;
using Terraria.GameContent;
using Terraria.ID;
using Terraria.Localization;
using Terraria.ModLoader;

namespace RORMod
{
    public class RORPlayer : ModPlayer
    {
        public Item accMonsterTooth;

        public float bootSpeed;

        public bool accGlubby;
        public bool glubbyHide;
        public byte glubbyActive;

        public bool gLegPlayedSound;
        public bool gLegSounds;

        public bool accDeathMark;
        public bool accShatterspleen;
        public bool accMedkit;
        public bool accTougherTimes;
        public bool accTriTipDagger;

        /// <summary>
        /// The closest 'enemy' NPC to the player. Updated in <see cref="PostUpdate"/> -> <see cref="DangerEnemy"/>
        /// </summary>
        public int dangerEnemy;
        public int dangerEnemyOld;

        /// <summary>
        /// Helper for whether or not the player is in danger
        /// </summary>
        public bool InDanger => dangerEnemy != -1;

        public override void ResetEffects()
        {
            accGlubby = false;
            gLegSounds = false;
            accDeathMark = false;
            accShatterspleen = false;
            accMedkit = false;
            accTougherTimes = false;
            accMonsterTooth = null;
            accTriTipDagger = false;
        }

        public override void UpdateLifeRegen()
        {
            UpdateCautiousSlug();
        }

        public void UpdateCautiousSlug()
        {
            if (accGlubby)
            {
                if (glubbyActive > 120)
                {
                    if (InDanger)
                    {
                        glubbyActive = 0;
                        if (!glubbyHide)
                            SoundEngine.PlaySound(RORMod.GetSound("glubbyhide").WithVolumeScale(0.4f));
                    }
                    Player.lifeRegen += 25;
                    return;
                }

                if (!InDanger || Player.Distance(Main.npc[dangerEnemy].Center) > 800f)
                {
                    glubbyActive++;
                    if (glubbyActive == 120)
                    {
                        if (!glubbyHide)
                            SoundEngine.PlaySound(RORMod.GetSound("glubby").WithVolumeScale(0.4f));
                    }
                }
            }
        }

        public override void PreUpdateMovement()
        {
            if (Player.accRunSpeed > 0f)
            {
                Player.accRunSpeed += bootSpeed;
            }
        }

        public override void PostUpdate()
        {
            if (gLegSounds)
            {
                UpdateGoatFootsteps();
            }
            DangerEnemy();
        }

        public void UpdateGoatFootsteps()
        {
            int legFrame = Player.legFrame.Y / 56;
            //Main.NewText(legFrame);
            if (legFrame == 5 || legFrame == 10 || legFrame == 17)
            {
                if (!gLegPlayedSound)
                    SoundEngine.PlaySound(RORMod.GetSounds("hoofstep_", 7, 0.33f, 0f, 0.1f));
                gLegPlayedSound = true;
            }
            else
            {
                gLegPlayedSound = false;
            }
        }

        /// <summary>
        /// Finds the closest enemy to the player, and caches its index in <see cref="Main.npc"/>
        /// </summary>
        public void DangerEnemy()
        {
            dangerEnemyOld = dangerEnemy;
            dangerEnemy = -1;

            var center = Player.Center;
            var checkTangle = new Rectangle((int)Player.position.X + Player.width / 2 - 1000, (int)Player.position.Y + Player.height / 2 - 500, 2000, 1000);
            float distance = 2000f;
            for (int i = 0; i < Main.maxNPCs; i++)
            {
                if (Main.npc[i].active && !Main.npc[i].friendly && Main.npc[i].type != NPCID.TargetDummy && Main.npc[i].CanBeChasedBy(Player) && !Main.npc[i].IsProbablyACritter())
                {
                    if (Main.npc[i].getRect().Intersects(checkTangle))
                    {
                        float d = Main.npc[i].Distance(center);
                        if (!Main.npc[i].noTileCollide && !Collision.CanHitLine(Main.npc[i].position, Main.npc[i].width, Main.npc[i].height, Player.position, Player.width, Player.height))
                        {
                            d *= 4f;
                        }
                        if (d < distance)
                        {
                            distance = d;
                            dangerEnemy = i;
                        }
                    }
                }
            }
        }

        public void TougherTimesDodge()
        {
            if (Main.netMode != NetmodeID.Server)
            {
                SoundEngine.PlaySound(RORMod.GetSound("toughertimes").WithVolumeScale(0.2f), Player.Center);
                int c = CombatText.NewText(new Rectangle((int)Player.position.X + Player.width / 2 - 1, (int)Player.position.Y, 2, 2), new Color(255, 255, 255, 255), 0, false, true);
                if (c != -1 && c != Main.maxCombatText)
                {
                    Main.combatText[c].text = Language.GetTextValue("Mods.RORMod.Blocked");
                    Main.combatText[c].rotation = 0f;
                    Main.combatText[c].scale *= 0.8f;
                    Main.combatText[c].alphaDir = 0;
                    Main.combatText[c].alpha = 0.99f;
                    Main.combatText[c].position.X = Player.position.X + Player.width / 2f - FontAssets.CombatText[0].Value.MeasureString(Main.combatText[c].text).X / 2f;
                }
            }
            Player.SetImmuneTimeForAllTypes(60);
        }

        public override bool PreHurt(bool pvp, bool quiet, ref int damage, ref int hitDirection, ref bool crit, ref bool customDamage, ref bool playSound, ref bool genGore, ref PlayerDeathReason damageSource, ref int cooldownCounter)
        {
            if (Player.whoAmI == Main.myPlayer && accTougherTimes && Main.rand.NextBool(10))
            {
                if (Main.netMode != NetmodeID.SinglePlayer)
                {
                    var p = RORMod.GetPacket(PacketType.TougherTimesDodge);
                    p.Write(Player.whoAmI);
                }
                TougherTimesDodge();
                return false;
            }
            return true;
        }

        public override void Hurt(bool pvp, bool quiet, double damage, int hitDirection, bool crit, int cooldownCounter)
        {
            if (accMedkit)
            {
                Player.AddBuff(ModContent.BuffType<MedkitBuff>(), 120);
            }
        }

        public override void OnHitNPC(Item item, NPC target, int damage, float knockback, bool crit)
        {
            OnHitEffects(target, damage, knockback, crit);
        }

        public override void OnHitNPCWithProj(Projectile proj, NPC target, int damage, float knockback, bool crit)
        {
            OnHitEffects(target, damage, knockback, crit);
        }

        public void OnHitEffects(NPC target, int damage, float knockback, bool crit)
        {
            if (accDeathMark)
            {
                int buffCount = 0;
                for (int i = 0; i < NPC.maxBuffs; i++)
                {
                    if (target.buffType[i] != 0 && Main.debuff[target.buffType[i]])
                    {
                        buffCount++;
                    }
                    if (buffCount >= 2)
                    {
                        target.AddBuff(ModContent.BuffType<DeathMarkDebuff>(), 420);
                        break;
                    }
                }
            }
            if (accShatterspleen && crit)
            {
                target.GetGlobalNPC<RORNPC>().bleedShatterspleen = true;
                BleedingDebuff.AddStack(target, 300, 1);
                target.netUpdate = true;
            }
            if (accTriTipDagger && Main.rand.NextBool(10))
            {
                BleedingDebuff.AddStack(target, 180, 1);
            }
        }
    }
}