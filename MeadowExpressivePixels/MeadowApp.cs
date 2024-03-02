using Meadow;
using Meadow.Devices;
using Meadow.Foundation.Displays;
using Meadow.Foundation.Graphics;
using Meadow.Peripherals.Displays;
using Microsoft.ExpressivePixels;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MeadowExpressivePixels
{
    public class MeadowApp : App<F7FeatherV1>
    {
        St7789 display;
        MicroGraphics graphics;
        const int displayWidth = 240;
        const int displayHeight = 240;

        public override Task Run()
        {       
            // Show the small heart 4 times - zoom 2 = 16x16
            ExpressivePixels expix = new ExpressivePixels("Heart8x8.json", graphics);
            expix.Display(20, 20, 2);
            expix.Display(40, 40, 2);
            expix.Display(60, 60, 2);
            expix.Display(80, 80, 2);
            expix.Load("ExpressivePixelsTinyLogo.json");
            expix.Display(110, 110, 2);

            Thread.Sleep(1000);
            graphics.Clear();

            // Show 4 of the 16x16 or 18x18 animations at zoom 4 = 72x72
            expix.Load("HeartBeat.json");
            expix.Display(30, 30, 4);

            expix.Load("FishTank.json");
            expix.Display(30, 125, 4);

            expix.Load("RocketJourney.json");
            expix.Display(125, 30, 4);

            expix.Load("RainbowHeartbeats.json");
            expix.Display(125, 125, 4);

            Thread.Sleep(1000);
            graphics.Clear();

            // Larger Emojis 48x48 64x64 32x32 - zoom = 1
            expix.Load("BigEmoji16.json");
            expix.Display(10, 10, 1);

            expix.Load("MoonPhase64.json");
            expix.Display(110, 10, 1);

            expix.Load("WeatherEmojis.json");
            expix.Display(64, 110, 2);  // Zoom 2 to see better 

            Thread.Sleep(1000);
            graphics.Clear();

            // select a single frame - zoom 4 
            // (note you should start at 64x64 if you want to display that large)
            expix.Load("BigEmoji16.json");
            expix.DisplayFrame(7, 20, 20, 4);

            Console.WriteLine("Done");

            return base.Run();
        }

        public override Task Initialize()
        {
            Console.WriteLine("Initializing...");

            var spiBus = Device.CreateSpiBus(Device.Pins.SCK, Device.Pins.MOSI, Device.Pins.MISO);

            display = new St7789(
                spiBus: spiBus,
                chipSelectPin: null,
                dcPin: Device.Pins.D01,
                resetPin: Device.Pins.D00,
                width: displayWidth, height: displayHeight);

            graphics = new MicroGraphics(display)
            {
                Rotation = RotationType._90Degrees,
                Stroke = 1
            };
            graphics.Clear(true);
            return base.Initialize();
        }
    }
}