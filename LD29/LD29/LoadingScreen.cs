using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Accelerated_Delivery_Win;

namespace LD29
{
    public class LoadingScreen
    {
        public Loader loader;
        SpriteBatch sb;
        IEnumerator<float> enumerator;

        //Texture2D backgroundTex;
        //Color screenBackgroundColor;

        // Loading screen colors.
        Color barColor = Color.DeepSkyBlue;//new Color(0, 148, 255);
        Color barBackgroundColor = Color.Gray;
        int barBackgroundExpand = 2; // Width of loading bar border in pixels
        Texture2D barTex;
        Rectangle screenRect;
        //Texture2D reflectionTex;

        // Size of of the loading bar and position relative to top left of bitmap.
        Rectangle loadingBarPos;

        public LoadingScreen(ContentManager content, GraphicsDevice gd)
        {
            loader = new Loader(content);
            sb = new SpriteBatch(gd);
            enumerator = loader.GetEnumerator();

            // This texture will be drawn behind the loading bar, centred on screen.
            // The rest of the screen will be filled with the top left pixel color.

            //screenBackgroundColor = topLeftPixelColor(backgroundTex);

            // 1-pixel white texture for solid bars
            barTex = new Texture2D(gd, 1, 1);
            uint[] texData = { 0xffffffff };
            barTex.SetData<uint>(texData);

            screenRect = new Rectangle(0, 0, sb.GraphicsDevice.Viewport.Width, sb.GraphicsDevice.Viewport.Height);

            loadingBarPos = new Rectangle();
            loadingBarPos.Width = 200; // pixels
            loadingBarPos.Height = 10;
            loadingBarPos.X = RenderingDevice.GraphicsDevice.Viewport.TitleSafeArea.Width - loadingBarPos.Width - 30;
            loadingBarPos.Y = (int)RenderingDevice.Height - RenderingDevice.GraphicsDevice.Viewport.TitleSafeArea.Height + 30;
        }

        public Loader Update(GameTime gameTime)
        {
            // enumerator.MoveNext() will load one item and return false when all done.
            return enumerator.MoveNext() ? null : loader;
        }

        public void Draw()
        {
            sb.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend);

            Rectangle barBackground = loadingBarPos;
            barBackground.Inflate(barBackgroundExpand, barBackgroundExpand);
            sb.Draw(barTex, barBackground, barBackgroundColor);

            Rectangle bar = loadingBarPos;
            float completeness = enumerator.Current;
            bar.Width = (int)(loadingBarPos.Width * completeness);

            sb.Draw(barTex, bar, barColor);
            sb.End();
        }
    }
}
