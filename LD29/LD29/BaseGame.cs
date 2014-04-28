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
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.Constraints.SingleEntity;
using BEPUphysics.Constraints.TwoEntity.Joints;
using BEPUphysics.Constraints.SolverGroups;

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
            Program.Initialize(GraphicsDevice);
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
                RenderingDevice.SpriteBatch.Draw(loadingSplash, new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.5f), null, Color.White, 0, new Vector2(loadingSplash.Width, loadingSplash.Height) * 0.5f, 1, SpriteEffects.None, 0);
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
            
            //var tris = new List<BEPUutilities.Vector3[]>();
            //float basinWidth = 12;
            //float basinLength = 12;
            //float waterHeight = 2.5f;
            //BEPUutilities.Vector3 offset = new BEPUutilities.Vector3(19.05975f, -18.05393f, 0);

            ////Remember, the triangles composing the surface need to be coplanar with the surface.  In this case, this means they have the same height.
            //tris.Add(new[]
            //             {
            //                 new BEPUutilities.Vector3(-basinWidth / 2, -basinLength / 2, waterHeight) + offset, new BEPUutilities.Vector3(basinWidth / 2, -basinLength / 2, waterHeight) + offset,
            //                 new BEPUutilities.Vector3(-basinWidth / 2, basinLength / 2, waterHeight) + offset
            //             });
            //tris.Add(new[]
            //             {
            //                 new BEPUutilities.Vector3(-basinWidth / 2, basinLength / 2, waterHeight) + offset, new BEPUutilities.Vector3(basinWidth / 2, -basinLength / 2, waterHeight) + offset,
            //                 new BEPUutilities.Vector3(basinWidth / 2, basinLength / 2, waterHeight) + offset
            //             });
            //FluidVolume water = new FluidVolume(Vector3.UnitZ, -9.81f, tris, waterHeight, 5f, 0.8f, 0.7f);
            //water.CollisionRules.Group = GameModel.NormalGroup;
            //Renderer.Add(Loader.Water, true);
            //waters.Add(water);
            //GameManager.Space.Add(water);
        }

        private void addModels()
        {
            Renderer.Add(Loader.Level1);

            BEPUutilities.Vector3[] verts;
            int[] indices;
            ModelDataExtractor.GetVerticesAndIndicesFromModel(Loader.Level1, out verts, out indices);

            Entity e = new MobileMesh(verts, indices, BEPUutilities.AffineTransform.Identity, MobileMeshSolidity.DoubleSided);
            e.CollisionInformation.CollisionRules.Group = GameModel.TerrainGroup;

            GameManager.Space.Add(e);

            GameModel tree = new GameModel(new Vector3(-5, 36, 7.1269f), Loader.Tree,
                new GameTexture("Tree", Loader.TreeTexture,
                    new PhysicsProperties(null, 0.9f, 0.8f, null, true, null),
                    new GameProperties(null, null, false, null, null),
                    new GraphicsProperties(null, true)));
            tree.Entity.CollisionInformation.Shape.Volume = 0.5f;
            GameModel tree2 = new GameModel(new Vector3(5, 36, 7.1269f), Loader.Tree,
                new GameTexture("Tree", Loader.TreeTexture,
                    new PhysicsProperties(null, 0.9f, 0.8f, null, true, null),
                    new GameProperties(null, null, false, null, null),
                    new GraphicsProperties(null, true)));
            tree2.Entity.CollisionInformation.Shape.Volume = 0.5f;
            //GameModel ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(3, -2, 3), 1), Vector3.Zero, Loader.Ball,
            //    new GameTexture("Beach ball", Loader.BallTexture,
            //        new PhysicsProperties(2.5f, 0.3f, 0.2f, 5f, true, true)));
            GameModel orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(-6, 31, 7), 0.2f), Vector3.Zero, Loader.Orange,
                new GameTexture("Orange", Loader.OrangeTexture,
                    new PhysicsProperties(0.1f, null, null, 5f, true, true)));
            //GameModel anvil = new GameModel(new Vector3(-2, -6, 0.7f), Loader.Anvil,
            //    new GameTexture("Anvil", Loader.AnvilTexture,
            //        new PhysicsProperties(null, null, null, 15000, true, true),
            //        new GameProperties(null, null, false)));
            
            GameModel button = new GameModel(new Vector3(0, 31, 3.175f), Loader.Button,
                new GameTexture("Button", Loader.ButtonTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(null, null, false,
                        (sender, other, pair) => {
                            EntityCollidable otherEntity = other as EntityCollidable;
                            if(otherEntity == null || otherEntity.Entity == null)
                                return;

                            GameModel m = otherEntity.Entity.Tag as GameModel;
                            if(m != null && m.Texture.FriendlyName == "Orange")
                            {
                                GameModel pThis = sender.Entity.Tag as GameModel;
                                PrismaticJoint pMotor = pThis.Texture.GameProperties.UpdateStateObject as PrismaticJoint;
                                pMotor.Motor.Settings.Servo.Goal = -0.07f;
                            }
                        },
                        (sender, other, pair) =>
                        {
                            EntityCollidable otherEntity = other as EntityCollidable;
                            if(otherEntity == null || otherEntity.Entity == null)
                                return;

                            GameModel m = otherEntity.Entity.Tag as GameModel;
                            if(m != null && m.Texture.FriendlyName == "Orange")
                            {
                                GameModel pThis = sender.Entity.Tag as GameModel;
                                PrismaticJoint pMotor = pThis.Texture.GameProperties.UpdateStateObject as PrismaticJoint;
                                pMotor.Motor.Settings.Servo.Goal = 0;
                            }
                        }/*,
                        rippedFrom => {
                            SingleEntityLinearMotor pMotor = rippedFrom.Texture.GameProperties.UpdateStateObject as SingleEntityLinearMotor;
                            pMotor.IsActive = false;
                        },
                        appliedTo => {
                            SingleEntityLinearMotor pMotor = appliedTo.Texture.GameProperties.UpdateStateObject as SingleEntityLinearMotor;
                            pMotor.Entity = appliedTo.Entity;
                            pMotor.Point = appliedTo.Entity.Position;
                            pMotor.Settings.Servo.Goal = appliedTo.Entity.Position;
                            pMotor.IsActive = true;
                        }*/)), false);
            button.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            PrismaticJoint lineJoint = new PrismaticJoint(e, button.Entity, button.Entity.Position, Vector3.UnitZ, button.Entity.Position);
            lineJoint.IsActive = true;
            lineJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            lineJoint.Motor.IsActive = true;
            lineJoint.Limit.Minimum = -0.07f;
            lineJoint.Limit.Maximum = 0;
            lineJoint.Limit.IsActive = true;
            GameManager.Space.Add(lineJoint);
            button.Texture.GameProperties.SetStateObject(lineJoint);

            Action<GameProperties> doorUpdate = gameProps => {
                if(lineJoint.IsActive && lineJoint.Motor.Settings.Servo.Goal != 0)
                    (gameProps.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = 0;
                else
                    (gameProps.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = MathHelper.PiOver2;
            };

            GameModel leftDoor = new GameModel(new Vector3(-0.5f, 36.8f, 7), Loader.LeftDoor,
                new GameTexture("Door", Loader.DoorTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(doorUpdate, null, false, null, null)), false);
            leftDoor.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            //leftDoor.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(0.5f, 0, 0);
            RevoluteJoint angularJoint = new RevoluteJoint(e, leftDoor.Entity, leftDoor.Entity.Position + new BEPUutilities.Vector3(-0.5f, 0, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.IsActive = true;
            angularJoint.Motor.Basis.SetWorldAxes(-Vector3.UnitZ, Vector3.UnitY);
            //angularJoint.Limit.MinimumAngle = 0;
            //angularJoint.Limit.MaximumAngle = MathHelper.PiOver2;
            //angularJoint.Limit.IsActive = true;
            GameManager.Space.Add(angularJoint);
            leftDoor.Texture.GameProperties.SetStateObject(angularJoint);

            GameModel rightDoor = new GameModel(new Vector3(0.5f, 36.8f, 7), Loader.RightDoor,
                new GameTexture("Door", Loader.DoorTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(doorUpdate, null, false, null, null)), false);
            rightDoor.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            //rightDoor.Entity.CollisionInformation.LocalPosition = new BEPUutilities.Vector3(-0.5f, 0, 0);
            angularJoint = new RevoluteJoint(e, rightDoor.Entity, rightDoor.Entity.Position - new BEPUutilities.Vector3(-0.5f, 0, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.IsActive = true;
            //angularJoint.Limit.MinimumAngle = 0;
            //angularJoint.Limit.MaximumAngle = MathHelper.PiOver2;
            //angularJoint.Limit.IsActive = true;
            GameManager.Space.Add(angularJoint);
            rightDoor.Texture.GameProperties.SetStateObject(angularJoint);

            Renderer.Add(tree);
            Renderer.Add(tree2);
            //Renderer.Add(ball);
            Renderer.Add(orange);
            //Renderer.Add(anvil);
            Renderer.Add(leftDoor);
            Renderer.Add(rightDoor);
            Renderer.Add(button);
            GameManager.Space.Add(tree);
            GameManager.Space.Add(tree2);
            //GameManager.Space.Add(ball);
            GameManager.Space.Add(orange);
            GameManager.Space.Add(leftDoor);
            GameManager.Space.Add(rightDoor);
            GameManager.Space.Add(button);
            //GameManager.Space.Add(anvil);
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
