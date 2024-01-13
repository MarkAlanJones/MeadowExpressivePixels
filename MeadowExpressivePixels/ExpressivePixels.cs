using Meadow;
using Meadow.Foundation.Graphics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;

/// <summary>
/// Display the JSON representation of Microsoft Expressive Pixels using the Meadow GraphicsLibrary
/// These are small (typically 8x8 to 64x64) palleted multi frame animations
/// http://aka.ms/expressivepixels
/// 
/// The authoring app is available for Windows 10 from the Microsoft Store app 
/// The targeted display device is a array of RGB LEDs - possibly circular
/// Thus on a 240x240 meadow display the pixels are tiny, and probably require zooming
/// however drawing zoomed animations slows them down.
/// https://github.com/microsoft/ExpressivePixels
/// 
/// This Library by Mark Alan Jones - Sept 2020 - MIT License
/// </summary>
namespace Microsoft.ExpressivePixels
{
    #region Definitions
    public class FrameDef
    {
        public FrameType Type { get; set; }
        public ushort Count { get; set; } // Pixels or ms 
        public string Data { get; set; }
    }

    public class DecodedFrameDef
    {
        public FrameType Type { get; set; }
        public ushort Count { get; set; } // Pixels or ms 
        public IFrameDef Ifd { get; set; }
        public PFrameDef Pfd { get; set; }
    }

    public class PFrameItem
    {
        public byte X { get; set; }
        public byte Y { get; set; }
        public byte Pal { get; set; }
    }

    public class IFrameDef
    {
        public List<byte> PalIndex { get; set; }
    }

    public class PFrameDef
    {
        public List<PFrameItem> Pix { get; set; }
    }

    #endregion


    /// <summary>
    /// Expressive Pixels - Load a JSON file (from an embedded resource)
    /// then Display - Animations can be zoomed
    /// 
    /// Display a single frame with DisplayFrame
    /// </summary>
    public class ExpressivePixels
    {
        #region Properties

        // JSON is parsed into here by Load function
        public ExpressivePixelsJSON Data { get; set; }
        public List<Color> Pallette { get; set; }
        private List<FrameDef> Frames { get; set; }
        public List<DecodedFrameDef> DFrames { get; set; }

        // This library uses the Meadow MicroGraphics Library to render
        private MicroGraphics graphics;
        
        private Stopwatch FrameTimer;

        // computed by the number of pixels in PalIndex list from an IFrame;
        public ushort Width { get; set; }
        public ushort Height { get; set; }

        #endregion

        public ExpressivePixels(string resource, MicroGraphics g)
        {
            graphics = g;
            Load(resource);
        }

        public ExpressivePixels(MicroGraphics g)
        {
            graphics = g;
            //default ?
            Width = 18;
            Height = 18;
            Data = null;
        }

        #region Load ExpressivePixels

        /// <summary>
        /// Load JSON from an embedded resource file
        /// </summary>
        /// <param name="resource">the name of the embedded resource to load</param>
        public void Load(string resource)
        {
            Stopwatch LoadTimer = new Stopwatch();
            LoadTimer.Start();

            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"MeadowExpressivePixels.JSON.{resource}";  // If you change the application namespace or the folder this will need to be changed

            Console.WriteLine($"Loading {resource}...");

            //default ?
            Width = 18;
            Height = 18;

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                string jsonFile = reader.ReadToEnd();
                Data = LitJson.JsonMapper.ToObject<ExpressivePixelsJSON>(jsonFile);

                Console.WriteLine($"Success {Data.Name} - Loop {Data.LoopCount} Times : {Data.PaletteSize} Colours : {Data.FrameCount} Frames ");
                Console.WriteLine($"JSON Decode took {LoadTimer.ElapsedMilliseconds}ms");

                LoadPalette();
                LoadFrames();

                LoadTimer.Stop();
                Console.WriteLine($"Total Load time - {LoadTimer.ElapsedMilliseconds}ms");
            }
        }

        private void LoadPalette()
        {
            Pallette = new List<Color>();
            for (int i = 0; i < Data.PaletteSize; i++)
            {
                string c = Data.PaletteHex.Substring(i * 6, 6);
                Pallette.Add(Color.FromHex(c));
                //Console.WriteLine($"  Color {i}) {Color.FromHex(c)}");
            }
        }

        private void LoadFrames()
        {
            Frames = new List<FrameDef>();
            int i = 0;
            do
            {
                string t = Data.FramesHex.Substring(i, 2);
                int num = int.Parse(t, NumberStyles.AllowHexSpecifier);
                i += 2;

                if (Enum.IsDefined(typeof(FrameType), num))
                {
                    FrameType FT = (FrameType)num;

                    t = Data.FramesHex.Substring(i, 4);
                    num = int.Parse(t, NumberStyles.AllowHexSpecifier);
                    i += 4;

                    int mult = 1;
                    if (FT == FrameType.P)
                    {
                        // how to tell 8bit or 16bit position - if row is the same 6 bytes later then 16bits
                        if (Data.FramesHex.Substring(i, 2) == Data.FramesHex.Substring(i + 6, 2))
                            mult = 6;
                        else
                            mult = 4;
                        Frames.Add(new FrameDef() { Type = FT, Count = (ushort)num, Data = Data.FramesHex.Substring(i, num * mult) });
                    }
                    else if (FT == FrameType.I)
                    {
                        mult = 2;
                        Frames.Add(new FrameDef() { Type = FT, Count = (ushort)num, Data = Data.FramesHex.Substring(i, num * mult) });
                    }
                    else if (FT == FrameType.D || FT == FrameType.F)
                    {
                        Frames.Add(new FrameDef() { Type = FT, Count = (ushort)num, Data = "" });
                    }
                    i += num * mult;

                    Console.WriteLine($"  Frame {Frames.Count} is {FT} {num}");
                }
                else
                {
                    Console.WriteLine($"  WARNING!! Unexpected Frame Type {num:X2}");
                    i += 4; // ? how many bytes to skip for unknown frame type
                }

            } while (i < Data.FramesHexLength);

            // further decode the frames to remove all the Hex strings
            DFrames = new List<DecodedFrameDef>();
            foreach (var F in Frames)
            {
                DecodedFrameDef F2 = new DecodedFrameDef() { Type = F.Type, Count = F.Count };

                switch (F.Type)
                {
                    case FrameType.I:
                        F2.Ifd = GetIFrame(F);
                        F2.Pfd = null;
                        break;

                    case FrameType.P:
                        F2.Pfd = GetPFrame(F);
                        F2.Ifd = null;
                        break;

                    case FrameType.D:
                    case FrameType.F:
                        F2.Ifd = null;
                        F2.Pfd = null;
                        break;
                }

                DFrames.Add(F2);
            }

            if (Frames.Count == Data.FrameCount)
                Console.WriteLine($"  Loaded {Frames.Count} Frames");
            else
                Console.WriteLine($"  WARNING !!! - Loaded {Frames.Count} Frames but expected {Data.FrameCount} !!");

            Frames = null; // GC
        }

        #endregion

        #region Display ExpressivePixels

        /// <summary>
        /// Display the animation - position of upper left corner
        /// </summary>
        /// <param name="xPos">pixels from left</param>
        /// <param name="yPos">pixels from top</param>
        /// <param name="zoom">pixels to draw per pixel</param>
        public void Display(ushort xPos = 0, ushort yPos = 0, ushort zoom = 1)
        {
            if (Data == null)
                Console.WriteLine("Must Load First!!");
            else
            {
                FrameTimer = new Stopwatch();

                // expected ms between frames
                long FrameRate = (long)((1000.0 / Data.FrameRate));

                // Note you could modify the LoopCount (or any data) after the JSON is loaded 
                for (int loop = 1; loop <= Data.LoopCount; loop++)
                {
                    Console.WriteLine($"Stating Loop {loop} - {Data.Name} has {Data.FrameCount} Frames");

                    foreach (var F in DFrames)
                    {
                        FrameTimer.Reset();
                        FrameTimer.Start();

                        switch (F.Type)
                        {
                            case FrameType.I:
                                Display(F.Ifd, xPos, yPos, zoom);
                                graphics.Show();
                                break;

                            case FrameType.P:
                                Display(F.Pfd, xPos, yPos, zoom);
                                graphics.Show();
                                break;

                            case FrameType.D:
                                // Delay
                                Console.WriteLine($"  Frame Delay {F.Count}ms");
                                Thread.Sleep(F.Count);
                                break;

                            case FrameType.F:
                                // How to fade ?
                                Console.WriteLine($"  Frame Fade {F.Count}ms");
                                Thread.Sleep(F.Count);
                                break;
                        }

                        FrameTimer.Stop();
                        Console.WriteLine($"  Frame {F.Type} in {FrameTimer.ElapsedMilliseconds}ms");
                        int wait = (int)(FrameRate - FrameTimer.ElapsedMilliseconds);
                        if (wait > 0)
                        {
                            Console.WriteLine($"  waiting {wait}ms");
                            Thread.Sleep(wait);
                        }
                        else
                            Console.WriteLine($"  WARNING !! Frame took too long to render by {wait}ms");

                    }
                }
            }
        }

        /// <summary>
        /// Display a specific sigle frame - 1 based
        /// I frame can be shown immediately, otherwise mulitple frames must be decoded
        /// backuo to find the I frame or start with P 
        /// </summary>
        public void DisplayFrame(ushort frame, ushort xPos = 0, ushort yPos = 0, ushort zoom = 1)
        {
            Console.WriteLine($"Display Frame {frame} - {Data.Name}");
            bool displayed = false;

            DecodedFrameDef F = DFrames[frame - 1];
            if (F.Type == FrameType.I)
            {
                Display(F.Ifd, xPos, yPos, zoom);
                displayed = true;
            }
            else
            {
                var look = frame - 1;
                while (look >= 0)
                {
                    F = DFrames[look];
                    if (F.Type == FrameType.I)
                    {
                        Console.WriteLine($"starting at frame {look + 1}");
                        Display(F.Ifd, xPos, yPos, zoom);
                        displayed = true;

                        // Show the interveneing frames to get to the desired one
                        for (var f = look + 1; f < frame; f++)
                        {
                            var FF = DFrames[f];
                            Console.WriteLine($"  then frame {f + 1} {FF.Type}");
                            if (FF.Type == FrameType.P)
                                Display(FF.Pfd, xPos, yPos, zoom);
                        }
                        look = -1;
                    }
                    else
                        look -= 1;
                }
            }

            if (!displayed)
            {
                F = DFrames[frame - 1];
                if (F.Type == FrameType.P)
                {
                    Display(F.Pfd, xPos, yPos, zoom);
                    displayed = true;
                }
            }

            if (displayed)
                graphics.Show();
            else
                Console.WriteLine($"WARNING !! Frame {frame} could not be displayed - {Data.Name}");
        }

        /// <summary>
        /// Use the meadow graphics library to draw the expressive pixels - I Frame
        /// </summary>
        private void Display(IFrameDef ifd, ushort xPos, ushort yPos, ushort zoom)
        {
            int x = xPos;
            int y = yPos;
            foreach (var p in ifd.PalIndex)
            {
                if (zoom == 1)
                {
                    graphics.DrawPixel(x, y, Pallette[p]);
                    x += 1;
                    if (x - xPos >= Width)
                    {
                        x = xPos;
                        y += 1;
                    }
                }
                else if (zoom == 2 || zoom == 3)
                {
                    graphics.Stroke = zoom;
                    graphics.DrawLine(x, y, x + zoom - 1, y, Pallette[p]);
                    x += zoom;
                    if (x - xPos >= Width * zoom)
                    {
                        x = xPos;
                        y += zoom;
                    }
                }
                else
                {
                    graphics.DrawRectangle(x, y, zoom, zoom, Pallette[p], true);
                    x += zoom;
                    if (x - xPos >= Width * zoom)
                    {
                        x = xPos;
                        y += zoom;
                    }
                }
            }
        }

        /// <summary>
        /// Use the meadow graphics library to draw the expressive pixels - P Frame
        /// </summary>
        private void Display(PFrameDef pfd, ushort xPos, ushort yPos, ushort zoom)
        {
            int x = xPos;
            int y = yPos;
            foreach (var p in pfd.Pix)
            {
                if (zoom == 1)
                {
                    x = xPos + p.X;
                    y = yPos + p.Y;
                    graphics.DrawPixel(x, y, Pallette[p.Pal]);
                }
                else if (zoom == 2 || zoom == 3)
                {
                    x = xPos + p.X * zoom;
                    y = yPos + p.Y * zoom;
                    graphics.Stroke = zoom;
                    graphics.DrawLine(x, y, x + zoom - 1, y, Pallette[p.Pal]);
                }
                else
                {
                    x = xPos + p.X * zoom;
                    y = yPos + p.Y * zoom;
                    graphics.DrawRectangle(x, y, zoom, zoom, Pallette[p.Pal], true);
                }
            }
        }

        /// <summary>
        /// The IFrame is a colour setting for EVERY Pixel - the dimensions can be guessed at
        /// </summary>
        /// <param name="f">I Framedef</param>
        /// <returns>Parsed IFrame</returns>
        private IFrameDef GetIFrame(FrameDef f)
        {
            IFrameDef result = new IFrameDef() { PalIndex = new List<byte>() };

            if (f.Type != FrameType.I)
                return result;
            if (f.Count != f.Data.Length / 2)
            {
                Console.WriteLine($"  ERROR IFrame length mismatch !! {f.Count} vs actual {f.Data.Length / 2}");
                return result;
            }

            Width = (ushort)Math.Sqrt(f.Count);
            Height = (ushort)(f.Count / Width);
            Console.WriteLine($"  IFrame dimension {Width}x{Height}");

            for (int i = 0; i < f.Count; i++)
            {
                result.PalIndex.Add(byte.Parse(f.Data.Substring(i * 2, 2), NumberStyles.AllowHexSpecifier));
            }

            return result;
        }

        /// <summary>
        /// The P Frame addresses specific pixels - but encodes the position in a single byte - or in 2 bytes
        /// for 8bit - Row is in the high nybble, column is in the low nybble
        /// </summary>
        /// <param name="f">P Framedef</param>
        /// <returns>Parsed PFrame</returns>
        private PFrameDef GetPFrame(FrameDef f)
        {
            PFrameDef result = new PFrameDef() { Pix = new List<PFrameItem>() };
            bool b8 = (f.Count * 4 == f.Data.Length);
            if (b8)
                Console.WriteLine($"  PFrame 8 bit position");
            else
                Console.WriteLine($"  PFrame 16 bit position");


            if (f.Type != FrameType.P)
                return result;

            for (int i = 0; i < f.Count; i++)
            {
                PFrameItem item = new PFrameItem();

                if (b8)
                {
                    item.Y = byte.Parse(f.Data.Substring(i * 4, 1), NumberStyles.AllowHexSpecifier);
                    item.X = byte.Parse(f.Data.Substring(i * 4 + 1, 1), NumberStyles.AllowHexSpecifier);
                    item.Pal = byte.Parse(f.Data.Substring(i * 4 + 2, 2), NumberStyles.AllowHexSpecifier);
                    //Console.WriteLine($"{item.X},{item.Y} = {item.Pal}");
                }
                else  // long 256 bits per line ?
                {
                    var y = byte.Parse(f.Data.Substring(i * 6, 2), NumberStyles.AllowHexSpecifier);
                    var x = byte.Parse(f.Data.Substring(i * 6 + 2, 2), NumberStyles.AllowHexSpecifier);

                    // we know the width/height if we had an Iframe - or assume 18x18 ?
                    item.Y = (byte)((x + y * 256) / Width);
                    item.X = (byte)((x + y * 256) % Width);
                    item.Pal = byte.Parse(f.Data.Substring(i * 6 + 4, 2), NumberStyles.AllowHexSpecifier);
                    //Console.WriteLine($"{item.X},{item.Y} = {item.Pal}   {f.Data.Substring(i * 6 + 2, 2)},{f.Data.Substring(i * 6, 2)}={f.Data.Substring(i * 6 + 4, 2)}");
                }
                result.Pix.Add(item);
            }

            return result;
        }

        #endregion
    }
}
