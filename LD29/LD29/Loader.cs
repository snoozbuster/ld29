using Accelerated_Delivery_Win;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections;
using System.Collections.Generic;

namespace LD29
{
    public class Loader : IEnumerable<float>
    {
        private ContentManager content;
        private int loadedItems;
        private int totalItems;

        #region font
        public SpriteFont BiggerFont;
        public SpriteFont Font;
        #endregion

        #region gui
        public Texture2D halfBlack;
        public Texture2D EmptyTex;

        public Texture2D pressStart;
        public Texture2D mainMenuLogo;
        public Texture2D mainMenuBackground;

        public Texture2D Instructions_Xbox;
        public Texture2D Instructions_PC;

        public Texture2D Dock;
        public Texture2D TabletopDotPNG;
        #endregion

        #region Buttons
        public Sprite resumeButton;
        public Sprite startButton;
        public Sprite quitButton;
        public Sprite mainMenuButton;
        public Sprite yesButton;
        public Sprite noButton;
        public Sprite pauseQuitButton;
        public Sprite instructionsButton;
        #endregion

        #region Models
        public Model Ball;
        public Model Orange;
        public Model Room;
        public Model Tree;
        public Model Anvil;
        public Model Water;
        #endregion

        #region model textures
        public Texture2D BallTexture;
        public Texture2D TreeTexture;
        public Texture2D OrangeTexture;
        public Texture2D AnvilTexture;
        #endregion

        public Loader(ContentManager content)
        {
            this.content = content;
        }

        public IEnumerator<float> GetEnumerator()
        {
            totalItems = 9 + 2 + 6 + 5;

            #region Font
            Font = content.Load<SpriteFont>("font/font");
            yield return progress();
            BiggerFont = content.Load<SpriteFont>("font/bigfont");
            yield return progress();
            #endregion

            #region gui
            EmptyTex = new Texture2D(RenderingDevice.GraphicsDevice, 1, 1);
            EmptyTex.SetData(new[] { Color.White });
            yield return progress();
            halfBlack = new Texture2D(RenderingDevice.GraphicsDevice, 1, 1);
            halfBlack.SetData(new[] { new Color(0, 0, 0, 178) }); //set the color data on the texture
            yield return progress();

            pressStart = content.Load<Texture2D>("gui/start");
            yield return progress();
            Texture2D buttonsTex = content.Load<Texture2D>("gui/buttons");
            Rectangle buttonRect = new Rectangle(0, 0, 210, 51);
            resumeButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.23f, RenderingDevice.Height * 0.75f), buttonRect, Sprite.RenderPoint.UpLeft);
            mainMenuButton = new Sprite(delegate { return buttonsTex; }, new Vector2((RenderingDevice.Width * 0.415f), (RenderingDevice.Height * 0.75f)), new Rectangle(0, buttonRect.Height, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            pauseQuitButton = new Sprite(delegate { return buttonsTex; }, new Vector2((RenderingDevice.Width * 0.6f), (RenderingDevice.Height * 0.75f)), new Rectangle(0, buttonRect.Height * 3, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            instructionsButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.415f, RenderingDevice.Height * 0.75f), new Rectangle(buttonRect.Width, buttonRect.Height * 2, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            quitButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.6f, RenderingDevice.Height * 0.75f), new Rectangle(0, buttonRect.Height * 3, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            startButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.23f, RenderingDevice.Height * 0.75f), new Rectangle(0, buttonRect.Height * 2, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            yesButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.315f, RenderingDevice.Height * 0.65f), new Rectangle(buttonRect.Width, 0, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            noButton = new Sprite(delegate { return buttonsTex; }, new Vector2(RenderingDevice.Width * 0.515f, RenderingDevice.Height * 0.65f), new Rectangle(buttonRect.Width, buttonRect.Height, buttonRect.Width, buttonRect.Height), Sprite.RenderPoint.UpLeft);
            yield return progress();
            mainMenuBackground = content.Load<Texture2D>("gui/background");
            yield return progress();
            mainMenuLogo = content.Load<Texture2D>("gui/logo");
            yield return progress();

            Instructions_Xbox = content.Load<Texture2D>("gui/instructions_xbox");
            yield return progress();
            Instructions_PC = content.Load<Texture2D>("gui/instructions_pc");
            yield return progress();

            Dock = content.Load<Texture2D>("gui/dock");
            yield return progress();
            #endregion

            #region models
            Ball = content.Load<Model>("models/ball");
            yield return progress();
            Orange = content.Load<Model>("models/orange");
            yield return progress();
            Room = content.Load<Model>("models/room");
            yield return progress();
            Tree = content.Load<Model>("models/tree");
            yield return progress();
            Anvil = content.Load<Model>("models/anvil");
            yield return progress();
            Water = content.Load<Model>("models/water");
            yield return progress();
            #endregion

            #region model textures
            TreeTexture = content.Load<Texture2D>("textures/tree_tex");
            yield return progress();
            OrangeTexture = content.Load<Texture2D>("textures/orange_tex");
            yield return progress();
            BallTexture = content.Load<Texture2D>("textures/ball_tex");
            yield return progress();
            AnvilTexture = content.Load<Texture2D>("textures/anvil_tex");
            yield return progress();
            TabletopDotPNG = content.Load<Texture2D>("textures/underwater");
            yield return progress();
            #endregion
        }

        float progress()
        {
            ++loadedItems;
            return (float)loadedItems / totalItems;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
