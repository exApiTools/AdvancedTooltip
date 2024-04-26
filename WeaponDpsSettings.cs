using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace AdvancedTooltip;

[Submenu]
public class WeaponDpsSettings
{
    public ToggleNode Enable { get; set; } = new(false);

    public ColorNode TextColor { get; set; } = new ColorBGRA(254, 192, 118, 255);
    public RangeNode<int> DpsTextSize { get; set; } = new(16, 10, 50);
    public RangeNode<int> DpsNameTextSize { get; set; } = new(13, 10, 50);
    public ColorNode BackgroundColor { get; set; } = new ColorBGRA(255, 255, 0, 255);
    public ColorNode DmgFireColor { get; set; } = new ColorBGRA(150, 0, 0, 255);
    public ColorNode DmgColdColor { get; set; } = new ColorBGRA(54, 100, 146, 255);
    public ColorNode DmgLightningColor { get; set; } = new ColorBGRA(255, 215, 0, 255);
    public ColorNode DmgChaosColor { get; set; } = new ColorBGRA(208, 31, 144, 255);
    public ColorNode PhysicalDamageColor { get; set; } = new ColorBGRA(255, 255, 255, 255);
    public ColorNode ElementalDamageColor { get; set; } = new ColorBGRA(0, 255, 255, 255);
}