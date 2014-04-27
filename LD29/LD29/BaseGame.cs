using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Accelerated_Delivery_Win;
using MyExtensions = Accelerated_Delivery_Win.Extensions;
using Microsoft.Win32;
using BEPUphysicsDemos;
using BEPUphysics.CollisionShapes;
using BEPUphysics.Entities.Prefabs;
using BEPUphysicsDemos.AlternateMovement.Character;
using BEPUphysics.Entities;
using BEPUphysics.UpdateableSystems;
using System.Collections.Generic;

namespace LD29
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class BaseGame : Microsoft.Xna.Framework.Game
    {
        public GraphicsDeviceManager Graphics;
        private LoadingScreen loadingScreen;

        public bool Loading { get; private set; }

        private bool locked = false;
        private bool beenDrawn = false;
        private Texture2D loadingSplash;
        public Loader Loader { get; private set; }

        public SoundEffectInstance BGM;
        Player character;

        List<FluidVolume> waters = new List<FluidVolume>();

        public BaseGame()
        {
            Graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            GameManager.FirstStageInitialization(this, Program.Cutter);
            Loading = true;
            Microsoft.Win32.SystemEvents.SessionSwitch += new Microsoft.Win32.SessionSwitchEventHandler(SystemEvents_SessionSwitch);

            Graphics.PreferredBackBufferHeight = 720;
            Graphics.PreferredBackBufferWidth = 1280;
            if(Graphics.GraphicsDevice.Adapter.IsProfileSupported(GraphicsProfile.HiDef))
                Graphics.GraphicsProfile = GraphicsProfile.HiDef;
            Graphics.ApplyChanges();

            Input.SetOptions(new WindowsOptions(), new XboxOptions());

            base.Initialize();

            Resources.Initialize(Content);
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            RenderingDevice.Initialize(Graphics, Program.Cutter, GameManager.Space, Content.Load<Effect>("shaders/shadowmap"));
            Renderer.Initialize(Graphics, this, GameManager.Space, Content.Load<Effect>("shaders/shadowmap"));
            MyExtensions.Initialize(GraphicsDevice);
            loadingScreen = new LoadingScreen(Content, GraphicsDevice);
            loadingSplash = Content.Load<Texture2D>("gui/loading");

            SoundEffect e = Content.Load<SoundEffect>("music/main");
            BGM = e.CreateInstance();
            BGM.IsLooped = true;
            BGM.Play();

            GameManager.Initialize(null, Content.Load<SpriteFont>("font/font"), null);
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            if((!IsActive && Loader != null) || locked)
            {
                base.Update(gameTime);
                return;
            }

            Input.Update(gameTime, false);
            MediaSystem.Update(gameTime, Program.Game.IsActive);

            if(Loading)
            {
                IsFixedTimeStep = true;
                Loader l = loadingScreen.Update(gameTime);
                if(l != null)
                {
                    IsFixedTimeStep = false;
                    Loader = l;
                    loadingScreen = null;
                    Loading = false;
                    MenuHandler.Create(Loader);
                    //createActors();
                }
            }
            else
            {
                GameState statePrior = GameManager.State;
                MenuHandler.Update(gameTime);
                bool stateChanged = GameManager.State != statePrior;

                if(GameManager.State == GameState.Running)
                {
                    IsMouseVisible = false;
                    if((Input.CheckKeyboardJustPressed(Keys.Escape) ||
                        Input.CheckXboxJustPressed(Buttons.Start)) && !stateChanged)
                    {
                        //MediaSystem.PlaySoundEffect(SFXOptions.Pause);
                        GameManager.State = GameState.Paused;
                    }
                    else
                    {
                        GameManager.Space.Update((float)(gameTime.ElapsedGameTime.TotalSeconds));

                        character.Update(gameTime, waters);

                        if(IsActive)
                            Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                    }
                }
                else if(GameManager.State != GameState.Ending && GameManager.State != GameState.GameOver && GameManager.State != GameState.Menuing_Lev)
                    IsMouseVisible = true;
            }

            base.Update(gameTime);
        }

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            if(!beenDrawn)
            {
                MediaSystem.LoadSoundEffects(Content);
                beenDrawn = true;
            }

            GraphicsDevice.Clear(Color.Black);

            if(Loading)
            {
                RenderingDevice.SpriteBatch.Begin();
                RenderingDevice.SpriteBatch.Draw(loadingSplash, new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.5f), Color.White);
                RenderingDevice.SpriteBatch.End();
                loadingScreen.Draw();
            }
            else
                MenuHandler.Draw(gameTime);

            if(GameManager.State == GameState.Running)
                DrawScene(gameTime);

            base.Draw(gameTime);
        }

        public void DrawScene(GameTime gameTime)
        {
            Renderer.Draw();
            character.Draw();
        }

        public void Start()
        {
            buildRoom();
            addModels();
            createCharacter();
        }

        private void buildRoom()
        {
            Renderer.Add(Loader.Room);

            BEPUutilities.Vector3[] verts;
            int[] indices;
            ModelDataExtractor.GetVerticesAndIndicesFromModel(Loader.Room, out verts, out indices);

            Entity e = new MobileMesh(verts, indices, BEPUutilities.AffineTransform.Identity, MobileMeshSolidity.DoubleSided);
            e.CollisionInformation.CollisionRules.Group = GameModel.NormalGroup;

            GameManager.Space.Add(e);
            
            var tris = new List<BEPUutilities.Vector3[]>();
            float basinWidth = 12;
            float basinLength = 12;
            float waterHeight = 2.5f;
            BEPUutilities.Vector3 offset = new BEPUutilities.Vector3(19.05975f, -18.05393f, 0);

            //Remember, the triangles composing the surface need to be coplanar with the surface.  In this case, this means they have the same height.
            tris.Add(new[]
                         {
                             new BEPUutilities.Vector3(-basinWidth / 2, -basinLength / 2, waterHeight) + offset, new BEPUutilities.Vector3(basinWidth / 2, -basinLength / 2, waterHeight) + offset,
                             new BEPUutilities.Vector3(-basinWidth / 2, basinLength / 2, waterHeight) + offset
                         });
            tris.Add(new[]
                         {
                             new BEPUutilities.Vector3(-basinWidth / 2, basinLength / 2, waterHeight) + offset, new BEPUutilities.Vector3(basinWidth / 2, -basinLength / 2, waterHeight) + offset,
                             new BEPUutilities.Vector3(basinWidth / 2, basinLength / 2, waterHeight) + offset
                         });
            FluidVolume water = new FluidVolume(Vector3.UnitZ, -9.81f, tris, waterHeight, 5f, 0.8f, 0.7f);
            water.CollisionRules.Group = GameModel.NormalGroup;
            Renderer.Add(Loader.Water, true);
            waters.Add(water);
            GameManager.Space.Add(water);
        }

        private void addModels()
        {
            GameModel tree = new GameModel(new Vector3(5, 5, 1.2f), Loader.Tree,
                new GameTexture("Tree", Loader.TreeTexture,
                    new PhysicsProperties(null, 0.9f, 0.8f, null, true, null),
                    new GameProperties(null, false),
                    new GraphicsProperties(null, true)));
            GameModel ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(3, -2, 3), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Beach ball", Loader.BallTexture,
                    new PhysicsProperties(2.5f, 0.3f, 0.2f, 5f, true, true)));
            GameModel orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(-7, 4, 1), 1), Vector3.Zero, Loader.Orange,
                new GameTexture("Orange", Loader.OrangeTexture,
                    new PhysicsProperties(0.1f, null, null, 7f, true, true)));
            GameModel anvil = new GameModel(new Vector3(-2, -6, 0.7f), Loader.Anvil,
                new GameTexture("Anvil", Loader.AnvilTexture,
                    new PhysicsProperties(null, null, null, 15000, true, true),
                    new GameProperties(null, false)));

            Renderer.Add(tree);
            Renderer.Add(ball);
            Renderer.Add(orange);
            Renderer.Add(anvil);
            GameManager.Space.Add(tree);
            GameManager.Space.Add(ball);
            GameManager.Space.Add(orange);
            GameManager.Space.Add(anvil);
        }

        private void createCharacter()
        {
            character = new Player(this);
            character.Activate();
        }

        public void End()
        {
            character.Deactivate();
            GameManager.Space.Clear();
            Renderer.Clear();
            waters.Clear();
        }

        #region windows
#if WINDOWS
        protected override void OnActivated(object sender, EventArgs args)
        {
            if(GameManager.PreviousState == GameState.Running)
                GameManager.State = GameState.Running;
            BGM.Resume();
            BGM.Volume = 1;

            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            if(GameManager.State == GameState.Running)
                GameManager.State = GameState.Paused;
            BGM.Pause();

            base.OnDeactivated(sender, args);
        }

        protected void SystemEvents_SessionSwitch(object sender, Microsoft.Win32.SessionSwitchEventArgs e)
        {
            if(e.Reason == SessionSwitchReason.SessionLock)
            {
                OnDeactivated(sender, e);
                locked = true;
            }
            else if(e.Reason == SessionSwitchReason.SessionUnlock)
            {
                OnActivated(sender, e);
                locked = false;
            }
        }
#endif
        #endregion
    }
}
