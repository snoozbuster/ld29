using Accelerated_Delivery_Win;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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

        public Texture2D Crosshair;
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
        public Model Water2;
        public Model Burst;
        public Model Level1;
        public Model LeftDoor;
        public Model LeftDoorAlt;
        public Model RightDoor;
        public Model RightDoorAlt;
        public Model Button;
        public Model Rubble;
        public Model Fridge;
        public Model Board;
        public Model Trophy;
        public Model Cage;
        #endregion

        #region model textures
        public Texture2D BallTexture;
        public Texture2D TreeTexture;
        public Texture2D OrangeTexture;
        public Texture2D AnvilTexture;
        public Texture2D ButtonTexture;
        public Texture2D DoorTexture;
        public Texture2D FridgeTexture;
        public Texture2D RubbleTexture;
        public Texture2D EmancipatorTexture;
        public Texture2D PurpleDoorTexture;
        public Texture2D PurpleButtonTexture;
        public Texture2D SeecretTexture;
        public Texture2D TrophyTexture;
        #endregion

        #region sfx
        public SoundEffect ApplyTexture;
        public SoundEffect InvalidApplicationRemovalGrab;
        public SoundEffect RemoveTexture;
        public SoundEffect Splash;

        public SoundEffect AnimationCan;
        public SoundEffect AnimationHmm;
        public SoundEffect AnimationTyping;
        public SoundEffect AnimationWoosh;
        public SoundEffect AnimationFlash;
        public SoundEffect AnimationClatter;
        public SoundEffect AnimationRustle;
        #endregion

        public SoundEffect HappyTunes;

        public Texture2D[] AnimationFrames;

        public Loader(ContentManager content)
        {
            this.content = content;
        }

        public IEnumerator<float> GetEnumerator()
        {
            totalItems = 1 + 10 + 2 + 19 + 14 + 31 + 11;

            #region audio
            HappyTunes = content.Load<SoundEffect>("music/happy");
            yield return progress();
            #endregion

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
            Crosshair = content.Load<Texture2D>("gui/crosshair");
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
            Water2 = content.Load<Model>("models/water2");
            yield return progress();
            Burst = content.Load<Model>("models/burst");
            yield return progress();
            Button = content.Load<Model>("models/button");
            yield return progress();
            Level1 = content.Load<Model>("models/level1");
            yield return progress();
            LeftDoor = content.Load<Model>("models/door_left");
            yield return progress();
            RightDoor = content.Load<Model>("models/door_right");
            yield return progress();
            LeftDoorAlt = content.Load<Model>("models/door_left_alt");
            yield return progress();
            RightDoorAlt = content.Load<Model>("models/door_right_alt");
            yield return progress();
            Rubble = content.Load<Model>("models/rubble");
            yield return progress();
            Fridge = content.Load<Model>("models/fridge");
            yield return progress();
            Board = content.Load<Model>("models/board");
            yield return progress();
            Trophy = content.Load<Model>("models/trophy");
            yield return progress();
            Cage = content.Load<Model>("models/cage");
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
            EmancipatorTexture = content.Load<Texture2D>("textures/emancipator");
            yield return progress();
            DoorTexture = content.Load<Texture2D>("textures/door_tex");
            yield return progress();
            ButtonTexture = content.Load<Texture2D>("textures/button_tex");
            yield return progress();
            RubbleTexture = content.Load<Texture2D>("textures/scraps_tex");
            yield return progress();
            FridgeTexture = content.Load<Texture2D>("textures/fridge");
            yield return progress();
            PurpleDoorTexture = content.Load<Texture2D>("textures/door2_tex");
            yield return progress();
            PurpleButtonTexture = content.Load<Texture2D>("textures/button2_tex");
            yield return progress();
            SeecretTexture = content.Load<Texture2D>("textures/lololol");
            yield return progress();
            TrophyTexture = content.Load<Texture2D>("textures/trophy_tex");
            yield return progress();
            #endregion

            #region animation
            AnimationFrames = new Texture2D[33];
            AnimationFrames[0] = content.Load<Texture2D>("animation/blank");
            yield return progress();
            for(int i = 2; i <= 32; i++)
            {
                if(i == 21)
                    continue;
                AnimationFrames[i] = content.Load<Texture2D>("animation/" + (i < 10 ? "0" : "") + i);
                yield return progress();
            }
            #endregion

            #region extra sfx
            AnimationCan = content.Load<SoundEffect>("music/sfx/animation/can");
            yield return progress();
            AnimationHmm = content.Load<SoundEffect>("music/sfx/animation/hmm");
            yield return progress();
            AnimationFlash = content.Load<SoundEffect>("music/sfx/animation/ksh");
            yield return progress();
            AnimationTyping = content.Load<SoundEffect>("music/sfx/animation/typing");
            yield return progress();
            AnimationWoosh = content.Load<SoundEffect>("music/sfx/animation/whoosh");
            yield return progress();
            AnimationRustle = content.Load<SoundEffect>("music/sfx/animation/rustle");
            yield return progress();
            AnimationClatter = content.Load<SoundEffect>("music/sfx/animation/clatter");
            yield return progress();

            ApplyTexture = content.Load<SoundEffect>("music/sfx/apply");
            yield return progress();
            InvalidApplicationRemovalGrab = content.Load<SoundEffect>("music/sfx/invalid");
            yield return progress();
            RemoveTexture = content.Load<SoundEffect>("music/sfx/remove");
            yield return progress();
            Splash = content.Load<SoundEffect>("music/sfx/splash");
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
