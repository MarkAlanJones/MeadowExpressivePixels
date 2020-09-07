# Meadow Expressive Pixels

Display the Microsoft Expressive Pixels JSON format using the Meadow GraphicsLibrary on supported RGB displays.

[Expressive Pixels](http://aka.ms/expressivepixels) is a Windows 10 app - available from the Microsoft Store, for creating small animations (typically 8x8 to 64x64 pixels) for display (typically) on large pixel RGB displays. They seem particually interested in the round displays from [SiliconSquared](https://siliconsquared.com/sparkletallinone/).

It is also an open source project on [Github](https://github.com/microsoft/ExpressivePixels). The key peice of information on the format is in the [Wiki](https://github.com/microsoft/ExpressivePixels/wiki/Animation-Format).

In summary, the format contains an RGB pallette of colours, stored as packed Hexidecimal strings, as well as frames. 

Each frame can be one of 4 types:
1. **I** - contains all the pixels in a frame - as a packed list of Hexidecimal palette entries. The dimensions can be determined by taking the square root of the number of pixels. The explicit dimensions of the animation are not stored in the JSON directly.
2. **P** - contains specific pixels to change from the previous frame. The pixel location can be stored as 8bit for small dimensions - Row and Coloumn each 4 bits fits into a byte, strored as a Hexidecimal string (2 characters). For larger images the encoding is 16 bits, and stores it in long rows of 256 pixels. So you must know the dimensions of the animation - hopefully you had an I frame first. The library assumes 18x18 if not. If there are no I frames you can specify the Width and Height after loading.
3. **D** - Delay in ms - just wait before going to the next frame.
4. **F** - Fade in ms - I don't know how to fade a portion of the display - Presumably on some of the devices, the entire display shows the image, and can be faded in hardware. In this library it is treated as a delay for now.
