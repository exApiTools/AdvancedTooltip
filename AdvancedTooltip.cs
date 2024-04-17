using System;
using System.Collections.Generic;
using System.Linq;
using AdvancedTooltip.Settings;
using ExileCore;
using ExileCore.PoEMemory.Components;
using ExileCore.PoEMemory.Elements;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace AdvancedTooltip
{
    public class AdvancedTooltip : BaseSettingsPlugin<AdvancedTooltipSettings>
    {
        private Dictionary<int, Color> TColors;
        private FastModsModule _fastMods;

        public override void OnLoad()
        {
            Graphics.InitImage("menu-colors.png");
            Graphics.InitImage("preload-end.png");
        }

        public override bool Initialise()
        {
            _fastMods = new FastModsModule(Graphics, Settings.ItemMods);
            TColors = new Dictionary<int, Color>
            {
                { 1, Settings.ItemMods.T1Color },
                { 2, Settings.ItemMods.T2Color },
                { 3, Settings.ItemMods.T3Color },
            };

            return true;
        }

        public override void Render()
        {
            var inventoryItemIcon = GameController?.Game?.IngameState?.UIHover?.AsObject<HoverItemIcon>();

            if (inventoryItemIcon is not { ToolTipType: not ToolTipType.None, ItemFrame: { } tooltip })
            {
                return;
            }

            var poeEntity = inventoryItemIcon.Item;
            var modsComponent = poeEntity?.GetComponent<Mods>();
            if (Settings.ItemMods.EnableFastMods &&
                (modsComponent == null ||
                 modsComponent.ItemRarity == ItemRarity.Magic ||
                 modsComponent.ItemRarity == ItemRarity.Rare))
                _fastMods.DrawUiHoverFastMods(tooltip);
            if (poeEntity == null || poeEntity.Address == 0)
            {
                return;
            }

            var tooltipRect = tooltip.GetClientRect();

            var itemMods = modsComponent?.ItemMods;

            if (itemMods == null ||
                itemMods.Any(x => string.IsNullOrEmpty(x.RawName) && string.IsNullOrEmpty(x.Name)))
                return;

            var mods = itemMods.Select(item => new ModValue(item, GameController.Files, modsComponent.ItemLevel,
                GameController.Files.BaseItemTypes.Translate(poeEntity.Path))).ToList();

            var startPosition = tooltipRect.TopLeft.TranslateToNum(20, 56);
            var t1 = mods.Count(item => item.CouldHaveTiers() && item.Tier == 1);
            var t2 = mods.Count(item => item.CouldHaveTiers() && item.Tier == 2);
            var t3 = mods.Count(item => item.CouldHaveTiers() && item.Tier == 3);
            var tierNoteHeight = Graphics.MeasureText("T").Y * (Math.Sign(t1) + Math.Sign(t2) + Math.Sign(t3));
            var width = Graphics.MeasureText("T1 x6").X;
            Graphics.DrawBox(startPosition, startPosition + new Vector2(width, tierNoteHeight), Color.Black);
            if (t1 > 0)
            {
                startPosition.Y += Graphics.DrawText($"T1 x{t1}", startPosition, Settings.ItemMods.T1Color).Y;
            }

            if (t2 > 0)
            {
                startPosition.Y += Graphics.DrawText($"T2 x{t2}", startPosition, Settings.ItemMods.T2Color).Y;
            }

            if (t3 > 0)
            {
                startPosition.Y += Graphics.DrawText($"T3 x{t3}", startPosition, Settings.ItemMods.T3Color).Y;
            }

            if (Settings.ItemLevel.Enable.Value)
            {
                var itemLevel = Convert.ToString(modsComponent?.ItemLevel ?? 0);
                var imageSize = Settings.ItemLevel.TextSize + 10;
                Graphics.DrawImage("menu-colors.png",
                    new RectangleF(tooltipRect.TopLeft.X - 2, tooltipRect.TopLeft.Y - 2, imageSize, imageSize),
                    Settings.ItemLevel.BackgroundColor);
                Graphics.DrawText(itemLevel, tooltipRect.TopLeft.TranslateToNum(2, 2), Settings.ItemLevel.TextColor);
            }

            if (Settings.ItemMods.Enable.Value)
            {
                var bottomTooltip = tooltipRect.Bottom + 5;
                var modPosition = new Vector2(tooltipRect.X + 20, bottomTooltip + 4);

                var height = mods.Where(x => x.Record.StatNames.Any(y => y != null))
                                 .Aggregate(modPosition, (position, item) => DrawMod(item, position)).Y -
                             bottomTooltip;

                if (height > 4)
                {
                    var modsRect = new RectangleF(tooltipRect.X + 1, bottomTooltip, tooltipRect.Width, height);
                    Graphics.DrawBox(modsRect, Settings.ItemMods.BackgroundColor);
                }
            }

            if (Settings.WeaponDps.Enable && poeEntity.TryGetComponent<Weapon>(out var weaponComponent))
                DrawWeaponDps(tooltipRect, poeEntity, mods, weaponComponent);
        }

        private Vector2 DrawMod(ModValue item, Vector2 position)
        {
            const float epsilon = 0.001f;
            const int marginBottom = 4;
            var oldPosition = position;
            var settings = Settings.ItemMods;

            var (affixTypeText, color) = item.AffixType switch
            {
                ModType.Prefix => ("[P]", settings.PrefixColor.Value),
                ModType.Suffix => ("[S]", settings.SuffixColor.Value),
                ModType.Corrupted => ("[C]", new Color(220, 20, 60)),
                ModType.Unique => ("[U]", new Color(255, 140, 0)),
                ModType.Enchantment => ("[E]", new Color(255, 0, 255)),
                ModType.Nemesis => ("[NEM]", new Color(255, 20, 147)),
                ModType.BloodLines => ("[BLD]", new Color(0, 128, 0)),
                ModType.Torment => ("[TOR]", new Color(178, 34, 34)),
                ModType.Tempest => ("[TEM]", new Color(65, 105, 225)),
                ModType.Talisman => ("[TAL]", new Color(218, 165, 32)),
                ModType.EssenceMonster => ("[ESS]", new Color(139, 0, 139)),
                ModType.Bestiary => ("[BES]", new Color(255, 99, 71)),
                ModType.DelveArea => ("[DEL]", new Color(47, 79, 79)),
                ModType.SynthesisA => ("[SYN]", new Color(255, 105, 180)),
                ModType.SynthesisGlobals => ("[SGS]", new Color(186, 85, 211)),
                ModType.SynthesisBonus => ("[SYB]", new Color(100, 149, 237)),
                ModType.Blight => ("[BLI]", new Color(0, 100, 0)),
                ModType.BlightTower => ("[BLT]", new Color(0, 100, 0)),
                ModType.MonsterAffliction => ("[MAF]", new Color(123, 104, 238)),
                ModType.FlaskEnchantmentEnkindling => ("[FEE]", new Color(255, 165, 0)),
                ModType.FlaskEnchantmentInstilling => ("[FEI]", new Color(255, 165, 0)),
                ModType.ExpeditionLogbook => ("[LOG]", new Color(218, 165, 32)),
                ModType.ScourgeUpside => ("[SCU]", new Color(218, 165, 32)),
                ModType.ScourgeDownside => ("[SCD]", new Color(218, 165, 32)),
                ModType.ScourgeMap => ("[SCM]", new Color(218, 165, 32)),
                ModType.ExarchImplicit => ("[EXI]", new Color(255, 69, 0)),
                ModType.EaterImplicit => ("[EAT]", new Color(255, 69, 0)),
                ModType.WeaponTree => ("[CRU]", new Color(254, 114, 53)),
                ModType.WeaponTreeRecombined => ("[CRC]", new Color(254, 114, 53)),
                _ => ("[?]", new Color(211, 211, 211))
            };

            var affixTypeWidth = Graphics.DrawText(affixTypeText, position, color).X;

            if (item.AffixType != ModType.Unique)
            {
                var totalTiers = item.TotalTiers;
                Color affixTextColor = (item.AffixType, totalTiers > 0) switch
                {
                    (ModType.Prefix, true) => TColors.GetValueOrDefault(item.Tier, settings.PrefixColor),
                    (ModType.Suffix, true) => TColors.GetValueOrDefault(item.Tier, settings.SuffixColor),
                    (ModType.Prefix, false) => settings.PrefixColor,
                    (ModType.Suffix, false) => settings.SuffixColor,
                    _ => default
                };
                var affix = (totalTiers > 0 ? $" T{item.Tier}({totalTiers}) " : string.Empty).PadLeft(" T11(11) ".Length);

                var affixTypeTextSize = item.AffixType switch
                {
                    ModType.Prefix => Graphics.DrawText(affix, position.Translate(affixTypeWidth), settings.PrefixColor),
                    ModType.Suffix => Graphics.DrawText(affix, position.Translate(affixTypeWidth), settings.SuffixColor),
                    _ => default
                };

                var affixTextSize = Settings.ItemMods.ShowModNames
                    ? Graphics.DrawText(item.AffixText, position.Translate(affixTypeWidth + affixTypeTextSize.X), affixTextColor)
                    : Vector2.Zero;
                if (Settings.ItemMods.StartStatsOnSameLine)
                {
                    position.X += affixTextSize.X + affixTypeTextSize.X;
                }
                else
                {
                    position.Y += Math.Max(affixTextSize.Y, affixTypeTextSize.Y);
                }
            }

            var longestValueLength = Graphics.MeasureText("+12345").X;
            foreach (var (stat, range, value) in item.Record.StatNames.Zip(item.Record.StatRange, item.StatValue))
            {
                if (range.Min == 0 && range.Max == 0) continue;
                if (value <= -1000 || stat == null) continue;

                var noSpread = !range.HasSpread();
                var statRangeAndName = string.Format(noSpread ? "{0}" : "[{1}] {0}", stat, range);
                var statValue = stat.ValueToString(value);
                Vector2 txSize;

                if (item.AffixType == ModType.Unique || Settings.ItemMods.StartStatsOnSameLine)
                {
                    txSize = Graphics.DrawText(statValue, position.Translate(affixTypeWidth + longestValueLength),
                        Color.Gainsboro, FontAlign.Right);
                    Graphics.DrawText(statRangeAndName, position.Translate(affixTypeWidth + longestValueLength + 5), Color.Gainsboro);
                }
                else
                {
                    txSize = Graphics.DrawText(statValue, position.Translate(affixTypeWidth), Color.Gainsboro, FontAlign.Right);
                    Graphics.DrawText(statRangeAndName, position.Translate(+40), Color.Gainsboro);
                }

                position.Y += txSize.Y;
            }

            return Math.Abs(position.Y - oldPosition.Y) > epsilon
                ? oldPosition with { Y = position.Y + marginBottom }
                : oldPosition;
        }

        private void DrawWeaponDps(RectangleF clientRect, Entity itemEntity, List<ModValue> modValues, Weapon weaponComponent)
        {
            if (weaponComponent == null) return;
            if (!itemEntity.IsValid) return;
            var aSpd = (float)Math.Round(1000f / weaponComponent.AttackTime, 2);
            var cntDamages = Enum.GetValues(typeof(DamageType)).Length;
            var doubleDpsPerStat = new float[cntDamages];
            float physDmgMultiplier = 1;
            var physHi = weaponComponent.DamageMax;
            var physLo = weaponComponent.DamageMin;

            foreach (var mod in modValues)
            {
                foreach (var (stat, range, value) in mod.Record.StatNames.Zip(mod.Record.StatRange, mod.StatValue))
                {
                    if (range.Min == 0 && range.Max == 0) continue;
                    if (stat == null) continue;

                    switch (stat.Key)
                    {
                        case "physical_damage_+%":
                        case "local_physical_damage_+%":
                            physDmgMultiplier += value / 100f;
                            break;

                        case "local_attack_speed_+%":
                            aSpd *= (100f + value) / 100;
                            break;

                        case "local_minimum_added_physical_damage":
                            physLo += value;
                            break;
                        case "local_maximum_added_physical_damage":
                            physHi += value;
                            break;

                        case "local_minimum_added_fire_damage":
                        case "local_maximum_added_fire_damage":
                        case "unique_local_minimum_added_fire_damage_when_in_main_hand":
                        case "unique_local_maximum_added_fire_damage_when_in_main_hand":
                            doubleDpsPerStat[(int)DamageType.Fire] += value;
                            break;

                        case "local_minimum_added_cold_damage":
                        case "local_maximum_added_cold_damage":
                        case "unique_local_minimum_added_cold_damage_when_in_off_hand":
                        case "unique_local_maximum_added_cold_damage_when_in_off_hand":
                            doubleDpsPerStat[(int)DamageType.Cold] += value;
                            break;

                        case "local_minimum_added_lightning_damage":
                        case "local_maximum_added_lightning_damage":
                            doubleDpsPerStat[(int)DamageType.Lightning] += value;
                            break;

                        case "unique_local_minimum_added_chaos_damage_when_in_off_hand":
                        case "unique_local_maximum_added_chaos_damage_when_in_off_hand":
                        case "local_minimum_added_chaos_damage":
                        case "local_maximum_added_chaos_damage":
                            doubleDpsPerStat[(int)DamageType.Chaos] += value;
                            break;
                    }
                }
            }

            var settings = Settings.WeaponDps;

            Color[] elementalDmgColors =
            {
                Color.White, settings.DmgFireColor, settings.DmgColdColor, settings.DmgLightningColor,
                settings.DmgChaosColor
            };

            var component = itemEntity.GetComponent<Quality>();
            if (component == null) return;
            physDmgMultiplier += component.ItemQuality / 100f;
            physLo = (int)Math.Round(physLo * physDmgMultiplier);
            physHi = (int)Math.Round(physHi * physDmgMultiplier);
            doubleDpsPerStat[(int)DamageType.Physical] = physLo + physHi;

            aSpd = (float)Math.Round(aSpd, 2);
            var pDps = doubleDpsPerStat[(int)DamageType.Physical] / 2 * aSpd;
            float eDps = 0;
            var firstEmg = 0;
            Color dpsColor = settings.PhysicalDamageColor;

            for (var i = 1; i < cntDamages; i++)
            {
                eDps += doubleDpsPerStat[i] / 2 * aSpd;
                if (!(doubleDpsPerStat[i] > 0)) continue;

                if (firstEmg == 0)
                {
                    firstEmg = i;
                    dpsColor = elementalDmgColors[i];
                }
                else
                    dpsColor = settings.ElementalDamageColor;
            }

            var textPosition = new Vector2(clientRect.Right - 15, clientRect.Y + 1);
            Graphics.DrawImage("preload-end.png", new RectangleF(textPosition.X - 100, textPosition.Y - 6, 100, 78),
                settings.BackgroundColor);
            var pDpsSize = pDps > 0
                ? Graphics.DrawText(pDps.ToString("#.#"), textPosition, FontAlign.Right)
                : Vector2.Zero;

            var eDpsSize = eDps > 0
                ? Graphics.DrawText(eDps.ToString("#.#"), textPosition.Translate(0, pDpsSize.Y), dpsColor,
                    FontAlign.Right)
                : Vector2.Zero;

            var dps = pDps + eDps;

            var dpsSize = dps > 0
                ? Graphics.DrawText(dps.ToString("#.#"), textPosition.Translate(0, pDpsSize.Y + eDpsSize.Y),
                    Color.White, FontAlign.Right)
                : Vector2.Zero;

            var dpsTextPosition = textPosition.Translate(0, pDpsSize.Y + eDpsSize.Y + dpsSize.Y);
            Graphics.DrawText("dps", dpsTextPosition, settings.TextColor, FontAlign.Right);
        }
    }
}