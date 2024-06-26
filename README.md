# Meadow Expressive Pixels

Display the Microsoft Expressive Pixels JSON format using the [Meadow](https://store.wildernesslabs.co/collections/frontpage/products/meadow-f7) [GraphicsLibrary](https://github.com/WildernessLabs/Meadow.Foundation/tree/master/Source/Meadow.Foundation.Libraries_and_Frameworks/Displays.GraphicsLibrary) on supported RGB displays.

[Expressive Pixels](http://aka.ms/expressivepixels) is a Windows 10 app - available from the Microsoft Store, for creating small animations (typically 8x8 to 64x64 pixels) for display (typically) on large pixel RGB displays. They seem particularly interested in the round displays from [SiliconSquared](https://siliconsquared.com/sparkletallinone/).

It is also an open source project on [Github](https://github.com/microsoft/ExpressivePixels). The key piece of information on the format is in the [Wiki](https://github.com/microsoft/ExpressivePixels/wiki/Animation-Format).

In summary, the format contains an RGB pallette of colours, stored as packed Hexidecimal strings, as well as the frames (also stored a one big packed hexidecimal string). 

Each frame can be one of 4 types:
1. **I** - contains all the pixels in a frame - as a packed list of hexidecimal palette entries. The dimensions can be determined by taking the square root of the number of pixels. The explicit dimensions of the animation are not stored in the JSON directly.
2. **P** - contains specific pixels to change from the previous frame. The pixel location can be stored as 8bit for small dimensions - Row and Column each 4 bits fits into a byte, strored as a hexidecimal string (2 characters). For larger images the encoding is 16 bits, and stores it in long rows of 256 pixels. So you must know the dimensions of the animation - hopefully you had an I frame first. The library assumes 18x18 if not. If there are no I frames you can specify the Width and Height after loading.
3. **D** - Delay in ms - just wait before going to the next frame.
4. **F** - Fade in ms - I don't know how to fade a portion of the display - Presumably on some of the devices, the entire display shows the image, and can be faded in hardware. In this library it is treated as a delay for now.

Microsoft says they choose between I and P format depending on which one uses less space.

## Example Output
**All graphics originate as Expressive Pixels JSON**
1 | 2 | 3 | 4
-------- | ---- | ---- | ---- 
![1](/ScreenShots/EP1.png) | ![2](/ScreenShots/EP2.png)| ![3](/ScreenShots/EP3.png)| ![4](/ScreenShots/EP4.png)
8x8 Hearts and logo | Samples | Emojis | 1 Frame zoomed

## Implementation Notes

* Expressive Pixels is a accessible application, attempting to simplify animations on small devices, but it is hard to draw a good animation by hand. This can be seen in the low quality of many examples in the default and community examples. 

* On Meadow the 240x240 st7789 display is huge, when you are displaying 16 pixels. So most animations will be displayed by zooming (the Display function takes a zoom parameter). This means 4 times the pixels for 2x, 16 times for 4x etc. The Meadow will have trouble keeping up with the frame rate if the animations are displayed at higher zooms.

* Including the Emoji library means that Expressive Pixels is a great vector to display full colour Emojis (much better than monochrome truetype files at lower resolutions). The Maximum size of 64x64 works great for circles, but emojis at 48x48 look good if not zoomed too much. 32x32 can also be effective (all 3 resolutions are demonstrated in the example code)

* When exporting to JSON you need to know how many pixels the animation was created for. The interface defaults to 16x16. You can experiment, but typically you want to match the size of the authored animation, and not take the default.

* Since the emojis seem useful, I wanted to support a library of emojis where you would display any specific one, without the animation. **DrawFrame** is most effective if it can draw the I frame - but we do not have control of the encoding as I or P, so often multiple frames have to be rendered in the background to display the specific desired frame. This can be slower than desired.

* JSON needs to be stored as an "embedded resource". It should be possible to load as a file as well.

* Microsoft *System.Text.Json* nuget was used originally to decode the JSON, but it stopped working. So it was swapped for *LitJson*, which worked fine, but 
now meadow ships with a built in *MicroJson* so the latest version uses that, to reduce the footprint  

![Expressive Pixels Logo](/ScreenShots/ExpressivePixelsSplash.png)
