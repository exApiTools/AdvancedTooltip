using ExileCore.Shared.Attributes;
using ExileCore.Shared.Nodes;
using SharpDX;

namespace AdvancedTooltip.Settings
{
    [Submenu]
    public class ItemModsSettings
    {
        public ToggleNode Enable { get; set; } = new(true);
        public ToggleNode EnableFastMods { get; set; } = new ToggleNode(true);
        public ToggleNode EnableFastModsTags { get; set; } = new ToggleNode(true);
        public ToggleNode ShowModNames { get; set; } = new ToggleNode(true);
        public ToggleNode StartStatsOnSameLine { get; set; } = new ToggleNode(false);
        public ColorNode BackgroundColor { get; set; } = new ColorBGRA(0, 0, 0, 220);
        public ColorNode PrefixColor { get; set; } = new ColorBGRA(136, 136, 255, 255);
        public ColorNode SuffixColor { get; set; } = new ColorBGRA(0, 206, 209, 255);
        public ColorNode T1Color { get; set; } = new ColorBGRA(255, 0, 255, 255);
        public ColorNode T2Color { get; set; } = new ColorBGRA(255, 255, 0, 255);
        public ColorNode T3Color { get; set; } = new ColorBGRA(0, 255, 0, 255);
    }
}