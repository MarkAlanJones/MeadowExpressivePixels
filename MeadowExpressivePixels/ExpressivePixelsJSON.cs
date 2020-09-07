namespace Microsoft.ExpressivePixels
{
    public class ExpressivePixelsJSON
    {
        public string Command { get; set; }
        public string Id { get; set; }
        public string Name { get; set; }

        public ushort PaletteSize { get; set; }
        // should be PaletteSize * 6 RRGGBB 
        public int PaletteHexLength { get; set; }
        // Parse to get the Colours used by the animation
        public string PaletteHex { get; set; }

        public int FrameCount { get; set; }
        public byte FrameRate { get; set; }
        public byte LoopCount { get; set; }

        public int FramesHexLength { get; set; }
        public string FramesHex { get; set; }
    }

    public enum FrameType
    {
        D = 0x43,  // Delay ms
        F = 0x46,  // Fade ms
        I = 0x49,  // Intra-coded - all pixels
        P = 0x50   // Predictive - selected pixels
    }
}
