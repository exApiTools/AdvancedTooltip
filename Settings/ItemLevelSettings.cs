using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace AdvancedTooltip.Settings
{
    [Submenu]
    public class ItemLevelSettings
    {
        public ToggleNode Enable { get; set; } = new(true);

        public RangeNode<int> TextSize { get; set; } = new(16, 10, 50);
        public ColorNode TextColor { get; set; } = new ColorBGRA(255, 255, 0, 255);
        public ColorNode BackgroundColor { get; set; } = new ColorBGRA(0, 0, 0, 230);
    }
}