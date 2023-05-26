using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using AdvancedTooltip.Settings;
using ExileCore;
using ExileCore.PoEMemory;
using ExileCore.PoEMemory.MemoryObjects;
using ExileCore.Shared.Enums;
using ExileCore.Shared.Helpers;
using GameOffsets;
using SharpDX;
using Vector2 = System.Numerics.Vector2;

namespace AdvancedTooltip
{
    //it shows the suffix/refix tier directly near mod on hover item
    public class FastModsModule
    {
        private readonly Graphics _graphics;
        private readonly ItemModsSettings _modsSettings;
        private long _lastTooltipAddress;
        private Element _regularModsElement;
        private List<ModTierInfo> _mods = new List<ModTierInfo>();
        private readonly Regex _modTypeRegex = new Regex(@"\<rgb\(\d+\,\d+\,\d+\)\>\{([\w ]+)\}", RegexOptions.Compiled);

        public FastModsModule(Graphics graphics, ItemModsSettings modsSettings)
        {
            _graphics = graphics;
            _modsSettings = modsSettings;
        }

        public void DrawUiHoverFastMods(Element tooltip)
        {
            try
            {
                InitializeElements(tooltip);

                if (_regularModsElement is not { IsVisibleLocal: true })
                    return;

                var rect = _regularModsElement.GetClientRectCache;
                var drawPos = new Vector2(tooltip.GetClientRectCache.X - 3, rect.TopLeft.Y);
                var height = rect.Height / _mods.Count;

                for (var i = 0; i < _mods.Count; i++)
                {
                    var modTierInfo = _mods[i];
                    var boxHeight = height * modTierInfo.ModLines;

                    var textPos = drawPos.Translate(0, boxHeight / 2);

                    var textSize = _graphics.DrawText(modTierInfo.DisplayName,
                        textPos, modTierInfo.Color,
                        FontAlign.Right | FontAlign.VerticalCenter);

                    textSize.X += 5;
                    textPos.X -= textSize.X + 5;

                    var initialTextSize = textSize;

                    if (_modsSettings.EnableFastModsTags)
                    {
                        foreach (var modType in modTierInfo.ModTypes)
                        {
                            var modTypeTextSize = _graphics.DrawText(modType.Name, textPos, modType.Color,
                                FontAlign.Right | FontAlign.VerticalCenter);

                            textSize.X += modTypeTextSize.X + 5;
                            textPos.X -= modTypeTextSize.X + 5;
                        }

                        if (modTierInfo.ModTypes.Count > 0)
                            textSize.X += 5;
                    }

                    var rectangleF = new RectangleF(drawPos.X - textSize.X - 3, drawPos.Y, textSize.X + 6,
                        height * modTierInfo.ModLines);
                    _graphics.DrawBox(rectangleF, Color.Black);
                    _graphics.DrawFrame(rectangleF, Color.Gray, 1);

                    _graphics.DrawFrame(new RectangleF(drawPos.X - initialTextSize.X - 3, drawPos.Y, initialTextSize.X + 6,
                        height * modTierInfo.ModLines), Color.Gray, 1);

                    drawPos.Y += boxHeight;
                    i += modTierInfo.ModLines - 1;
                }
            }
            catch
            {
                //ignored   
            }
        }

        private void InitializeElements(Element tooltip)
        {
            var modsRoot = tooltip.GetChildAtIndex(1);

            if (modsRoot == null)
                return;

            Element extendedModsElement = null;
            Element regularModsElement = null;
            for (var i = modsRoot.Children.Count - 1; i >= 0; i--)
            {
                var element = modsRoot.Children[i];
                var elementText = element.Text;
                if (!string.IsNullOrEmpty(elementText) &&
                    (elementText.StartsWith("<smaller>", StringComparison.Ordinal) ||
                     elementText.StartsWith("<fractured>{<smaller>", StringComparison.Ordinal)) &&
                    !element.TextNoTags.StartsWith("Allocated Crucible", StringComparison.Ordinal))
                {
                    extendedModsElement = element;
                    regularModsElement = modsRoot.Children[i - 1];
                    break;
                }
            }

            if (regularModsElement == null)
            {
                _regularModsElement = null;
                _lastTooltipAddress = default;
                return;
            }
            if (_lastTooltipAddress != tooltip.Address ||
                _regularModsElement?.Address != regularModsElement.Address)
            {
                _lastTooltipAddress = tooltip.Address;
                _regularModsElement = regularModsElement;
                ParseItemHover(tooltip, extendedModsElement);
            }
        }

        private static readonly Regex FracturedRegex = new Regex(@"\<fractured\>\{([^\n]*\n[^\n]*)(?:\n\<italic\>\{[^\n]*\})?\}(?=\n|$)", RegexOptions.Compiled);

        private static string RemoveFractured(string x)
        {
            return FracturedRegex.Replace(x, "$1");
        }

        private void ParseItemHover(Element tooltip, Element extendedModsElement)
        {
            _mods.Clear();
            var extendedModsStr = extendedModsElement.GetText(2500);
            var extendedModsLines = RemoveFractured(extendedModsStr.Replace("\r\n", "\n")).Split('\n');

            var regularModsStr = _regularModsElement.GetTextWithNoTags(2500);
            var regularModsLines = regularModsStr.Replace("\r\n", "\n").Split('\n');

            ModTierInfo currentModTierInfo = null;

            var modsDict = new Dictionary<string, ModTierInfo>();

            foreach (var extendedModsLine in extendedModsLines)
            {
                if (extendedModsLine.StartsWith("<italic>", StringComparison.Ordinal))
                {
                    continue;
                }

                if (extendedModsLine.StartsWith("<smaller>", StringComparison.Ordinal) || extendedModsLine.StartsWith("<crafted>", StringComparison.Ordinal))
                {
                    var isPrefix = extendedModsLine.Contains("Prefix");
                    var isSuffix = extendedModsLine.Contains("Suffix");

                    if (!isPrefix && !isSuffix)
                    {
                        DebugWindow.LogMsg($"Cannot extract Affix type from mod text: {extendedModsLine}", 4);
                        return;
                    }

                    var affix = isPrefix ? "P" : "S";
                    Color color = isPrefix ? _modsSettings.PrefixColor : _modsSettings.SuffixColor;

                    var isRank = false;
                    const string tierPrefix = "(Tier: ";
                    var tierPos = extendedModsLine.IndexOf(tierPrefix, StringComparison.Ordinal);
                    if (tierPos != -1)
                    {
                        tierPos += tierPrefix.Length;
                    }
                    else
                    {
                        const string rankPrefix = "(Rank: ";
                        tierPos = extendedModsLine.IndexOf(rankPrefix, StringComparison.Ordinal);

                        if (tierPos != -1)
                        {
                            tierPos += rankPrefix.Length;
                            isRank = true;
                        }
                    }

                    if (tierPos != -1 &&
                        (int.TryParse(extendedModsLine.Substring(tierPos, 2), out var tier) ||//try parse number 10 and up
                         int.TryParse(extendedModsLine.Substring(tierPos, 1), out tier))
                        )
                    {
                        if (isRank)
                            affix += $" Rank{tier}";
                        else
                            affix += tier;

                        color = tier switch
                        {
                            1 => _modsSettings.T1Color,
                            2 => _modsSettings.T2Color,
                            3 => _modsSettings.T3Color,
                            _ => color
                        };
                    }
                    else if (extendedModsLine.Contains("Essence"))
                    {
                        affix += "(Ess)";
                    }


                    currentModTierInfo = new ModTierInfo(affix, color);


                    var modTypesMatches = _modTypeRegex.Matches(extendedModsLine);
                    if (modTypesMatches.Count > 0)
                    {
                        foreach (Match modTypeMatch in modTypesMatches)
                        {
                            var modTypeValue = modTypeMatch.Groups[1].Value;
                            var modTypeColor = modTypeValue switch
                            {
                                "Fire" => Color.Red,
                                "Cold" => new Color(41, 102, 241),
                                "Life" => Color.Magenta,
                                "Lightning" => Color.Yellow,
                                "Physical" => new Color(225, 170, 20),
                                "Critical" => new Color(168, 220, 26),
                                "Mana" => new Color(20, 240, 255),
                                "Attack" => new Color(240, 100, 30),
                                "Speed" => new Color(0, 255, 192),
                                "Caster" => new Color(216, 0, 255),
                                "Elemental" => Color.White,
                                "Gem Level" => new Color(200, 230, 160),
                                _ => Color.Gray
                            };

                            currentModTierInfo.ModTypes.Add(new ModType(modTypeValue, modTypeColor));
                        }
                    }

                    continue;
                }


                if (extendedModsLine.StartsWith("<", StringComparison.Ordinal) && !char.IsLetterOrDigit(extendedModsLine[0]))
                {
                    currentModTierInfo = null;
                    continue;
                }

                if (currentModTierInfo != null)
                {
                    var modLine = Regex.Replace(extendedModsLine, @"\([\d-.]+\)", string.Empty);
                    modLine = Regex.Replace(modLine, @"[\d-.]+", "#");
                    modLine = Regex.Replace(modLine, @"\s\([\d]+% Increased\)", string.Empty);
                    modLine = modLine.Replace(" (#% Increased)", string.Empty);
                    if (modLine.StartsWith("+", StringComparison.Ordinal))
                        modLine = modLine.Substring(1);

                    if (!modsDict.ContainsKey(modLine))
                    {
                        modsDict[modLine] = currentModTierInfo;
                    }
                }
            }

            var modTierInfos = new List<ModTierInfo>();
            foreach (var regularModsLine in regularModsLines)
            {
                var modFixed = regularModsLine;
                if (modFixed.StartsWith("+", StringComparison.Ordinal))
                    modFixed = modFixed.Substring(1);

                modFixed = Regex.Replace(modFixed, @"[\d-.]+", "#");

                var found = false;
                foreach (var keyValuePair in modsDict)
                {
                    if (modFixed.Contains(keyValuePair.Key))
                    {
                        found = true;
                        modTierInfos.Add(keyValuePair.Value);
                        break;
                    }
                }

                if (!found)
                {
                    DebugWindow.LogMsg($"Cannot extract mod from parsed mods: {modFixed}", 4);
                    var modTierInfo = new ModTierInfo("?", Color.Gray);
                    modTierInfos.Add(modTierInfo);
                    //return;
                }
            }

            if (modTierInfos.Count > 1)
            {
                for (var i = 1; i < modTierInfos.Count; i++)
                {
                    var info = modTierInfos[i];
                    var prevInfo = modTierInfos[i - 1];

                    if (info == prevInfo)
                    {
                        info.ModLines++;
                    }
                }
            }

            _mods = modTierInfos;
        }

        private class ModTierInfo
        {
            public ModTierInfo(string displayName, Color color)
            {
                DisplayName = displayName;
                Color = color;
            }

            public string DisplayName { get; }
            public Color Color { get; }
            public List<ModType> ModTypes { get; set; } = new List<ModType>();

            /// <summary>
            /// Mean twinned mod
            /// </summary>
            public int ModLines { get; set; } = 1;
        }

        public class ModType
        {
            public ModType(string name, Color color)
            {
                Name = name;
                Color = color;
            }

            public string Name { get; }
            public Color Color { get; }
        }
    }
}