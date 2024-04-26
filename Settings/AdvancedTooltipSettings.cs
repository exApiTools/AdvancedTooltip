using ExileCore.Shared.Interfaces;
using ExileCore.Shared.Nodes;

namespace AdvancedTooltip.Settings;

public class AdvancedTooltipSettings : ISettings
{
    public ItemLevelSettings ItemLevel { get; set; } = new();
    public ItemModsSettings ItemMods { get; set; } = new();
    public WeaponDpsSettings WeaponDps { get; set; } = new();
    public ToggleNode Enable { get; set; } = new(false);
}