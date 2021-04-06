using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript {
    partial class Program : MyGridProgram {
        readonly IMyTextSurface _drawingSurface;
        RectangleF _viewport;

        public Program() {
            _drawingSurface = Me.GetSurface(0);
            Runtime.UpdateFrequency = UpdateFrequency.Update100;
            _viewport = new RectangleF(
                (_drawingSurface.TextureSize - _drawingSurface.SurfaceSize) / 2f,
                _drawingSurface.SurfaceSize
            );
            PrepareTextSurfaceForSprites(_drawingSurface);
        }
        public void Main(string argument, UpdateType updateSource) {
            var frame = _drawingSurface.DrawFrame();

            // All sprites must be added to the frame here
            DrawSprites(ref frame);

            // We are done with the frame, send all the sprites to the text panel
            frame.Dispose();
        }
        // Drawing Sprites
        public void DrawSprites(ref MySpriteDrawFrame frame) {
            // Create background sprite
            var sprite = new MySprite() {
                Type = SpriteType.TEXTURE,
                Data = "Grid",
                Position = _viewport.Center,
                Size = _viewport.Size,
                Color = _drawingSurface.ScriptForegroundColor.Alpha(0.66f),
                Alignment = TextAlignment.CENTER
            };
            // Add the sprite to the frame
            frame.Add(sprite);

            // Set up the initial position - and remember to add our viewport offset
            var position = new Vector2(256, 20) + _viewport.Position;

            // Create our first line
            sprite = new MySprite() {
                Type = SpriteType.TEXT,
                Data = "Line 1",
                Position = position,
                RotationOrScale = 0.8f /* 80 % of the font's default size */,
                Color = Color.Red,
                Alignment = TextAlignment.CENTER /* Center the text on the position */,
                FontId = "White"
            };
            // Add the sprite to the frame
            frame.Add(sprite);

            // Move our position 20 pixels down in the viewport for the next line
            position += new Vector2(0, 20);

            // Here we add our clipping sprite. This is a simple rectangle. Nothing will be drawn outside it.
            // We create a rectangle that is covering the first half of the next line, cutting it off in the 
            // middle. 
            sprite = MySprite.CreateClipRect(new Rectangle(0, (int)position.Y - 16, (int)position.X, (int)position.Y + 16));
            // Add the sprite to the frame
            frame.Add(sprite);

            // Create our second line, we'll just reuse our previous sprite variable - this is not necessary, just
            // a simplification in this case.
            sprite = new MySprite() {
                Type = SpriteType.TEXT,
                Data = "Line 1",
                Position = position,
                RotationOrScale = 0.8f,
                Color = Color.Blue,
                Alignment = TextAlignment.CENTER,
                FontId = "White"
            };
            // Add the sprite to the frame
            frame.Add(sprite);
        }

        // Auto-setup text surface
        public void PrepareTextSurfaceForSprites(IMyTextSurface textSurface) {
            // Set the sprite display mode
            textSurface.ContentType = ContentType.SCRIPT;
            // Make sure no built-in script has been selected
            textSurface.Script = "";
        }
    }
}
