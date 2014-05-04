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
using BEPUphysics.CollisionRuleManagement;

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

        public readonly List<FluidVolume> Waters = new List<FluidVolume>();

        private AnimatedTexture animation;
        private bool animating;
        protected float screenAlpha = 255;
        protected const float deltaA = 3;

        private GameModel finalModel;
        private float angle = 0;

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
                }
            }
            else
            {
                GameState statePrior = GameManager.State;
                MenuHandler.Update(gameTime);
                bool stateChanged = GameManager.State != statePrior;

                if(GameManager.State == GameState.Running)
                {
                    if(animating)
                    {
                        if(animation == null)
                        {
                            if(screenAlpha - deltaA < 0)
                                animating = false;
                            else
                                screenAlpha -= deltaA;
                        }
                        else
                        {
                            animation.Update(gameTime);
                            if(animation.Done)
                            {
                                animation = null;
                                actualStart();
                            }
                        }
                    }
                    else
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

                            character.Update(gameTime, Waters);

                            if(IsActive)
                                Mouse.SetPosition(GraphicsDevice.Viewport.Width / 2, GraphicsDevice.Viewport.Height / 2);
                        }
                    }
                }
                else if(GameManager.State != GameState.Ending && GameManager.State != GameState.GameOver && GameManager.State != GameState.Menuing_Lev)
                    IsMouseVisible = true;
                if(GameManager.State == GameState.Ending)
                {
                    if(animating)
                    {
                        if(screenAlpha + deltaA * 0.5f > 255)
                        {
                            End();
                            animating = false;
                            finalModel.Entity.Position = Vector3.Zero;
                            finalModel.Entity.IsAffectedByGravity = false;
                            finalModel.Entity.Mass = float.NegativeInfinity;
                            finalModel.Entity.AngularVelocity = finalModel.Entity.LinearVelocity = Vector3.Zero;
                            Renderer.Camera.Position = new BEPUutilities.Vector3(0, 5, 0.5f);
                            Renderer.Camera.ViewDirection = new BEPUutilities.Vector3(0, -1, 0);
                            Renderer.Add(finalModel);
                            GameManager.Space.Add(finalModel);
                            BGM.Stop();
                            BGM = Loader.HappyTunes.CreateInstance();
                            BGM.IsLooped = true;
                            BGM.Play();
                            screenAlpha = 255;
                        }
                        else
                            screenAlpha += deltaA * 0.5f;
                    }
                    else
                    {
                        angle += 0.07f;
                        finalModel.Entity.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitZ, angle) * Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(20));
                        GameManager.Space.Update((float)gameTime.ElapsedGameTime.TotalSeconds);
                    }
                }
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
            else if(GameManager.State != GameState.Ending)
                MenuHandler.Draw(gameTime);

            if(GameManager.State == GameState.Running)
                DrawScene(gameTime);
            else if(GameManager.State == GameState.Ending)
                DrawEnding(gameTime);

            base.Draw(gameTime);
        }

        private void DrawEnding(GameTime gameTime)
        {
                Renderer.Draw();
                if(animating)
                {
                    RenderingDevice.SpriteBatch.Begin();
                    RenderingDevice.SpriteBatch.Draw(Loader.EmptyTex, new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height),
                        new Color(0, 0, 0, screenAlpha) * (screenAlpha / 255f));
                    RenderingDevice.SpriteBatch.End();
                }
                else
                    MenuHandler.Draw(gameTime);
        }

        public void DrawScene(GameTime gameTime)
        {
            if(animating && animation != null)
            {
                animation.Draw();
            }
            else
            {
                Renderer.Draw();
                character.Draw();
                if(animating)
                {
                    RenderingDevice.SpriteBatch.Begin();
                    RenderingDevice.SpriteBatch.Draw(Loader.EmptyTex, new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height),
                        new Color(255, 255, 255, screenAlpha) * (screenAlpha / 255f));
                    RenderingDevice.SpriteBatch.End();
                }
            }
        }

        public void Start()
        {
            List<Texture2D> frames = new List<Texture2D>();
            List<float> timings = new List<float>();
            List<SoundEffect> sounds = new List<SoundEffect>();
            
            for(int i = 0; i < 4; i++)
            {
                frames.Add(Loader.AnimationFrames[2]);
                frames.Add(Loader.AnimationFrames[3]);
                timings.Add(0.65f);
                timings.Add(0.65f); 
                sounds.Add(i == 0 ? Loader.AnimationTyping : null);
                sounds.Add(null);
            }
            frames.Add(Loader.AnimationFrames[2]);
            frames.Add(Loader.AnimationFrames[3]);
            timings.Add(0.75f);
            timings.Add(1.2f);
            sounds.Add(null);
            sounds.Add(null);
            frames.Add(Loader.AnimationFrames[4]);
            timings.Add(3.5f);
            sounds.Add(Loader.AnimationHmm);
            for(int i = 0; i < 3; i++)
            {
                frames.Add(Loader.AnimationFrames[2]);
                frames.Add(Loader.AnimationFrames[3]);
                timings.Add(0.65f);
                timings.Add(0.65f);
                sounds.Add(i == 0 ? Loader.AnimationTyping : null);
                sounds.Add(null);
            }
            for(int i = 5; i <= 10; i++)
            {
                frames.Add(Loader.AnimationFrames[i]);
                timings.Add(0.4f);
                sounds.Add(i == 5 ? Loader.AnimationRustle : null);
            }
            frames.Add(Loader.AnimationFrames[12]);
            timings.Add(1.5f);
            sounds.Add(null);
            frames.Add(Loader.AnimationFrames[13]);
            timings.Add(0.3f);
            sounds.Add(Loader.AnimationCan);
            for(int i = 14; i <= 19; i++)
            {
                frames.Add(Loader.AnimationFrames[0]);
                frames.Add(Loader.AnimationFrames[i]);
                timings.Add(0.1f);
                timings.Add(0.3f);
                sounds.Add(Loader.AnimationFlash);
                sounds.Add(i == 18 ? Loader.AnimationWoosh : null);
            }
            for(int i = 20; i <= 27; i++)
            {
                if(i == 21)
                    continue;
                frames.Add(Loader.AnimationFrames[i]);
                timings.Add(0.2f);
                sounds.Add(i == 27 ? Loader.AnimationClatter : null);
            }
            frames.Add(Loader.AnimationFrames[28]);
            timings.Add(1.8f);
            sounds.Add(null);
            frames.Add(Loader.AnimationFrames[0]);
            timings.Add(0.1f);
            sounds.Add(Loader.AnimationFlash);
            for(int i = 29; i <= 31; i++)
            {
                frames.Add(Loader.AnimationFrames[i]);
                timings.Add(1.7f);
                sounds.Add(i == 29 ? null : Loader.AnimationFlash);
            }
            frames.Add(Loader.AnimationFrames[32]);
            timings.Add(2.4f);
            sounds.Add(Loader.AnimationWoosh);
            animating = true;
            animation = new AnimatedTexture(frames, timings, sounds);
        }

        private void actualStart()
        {
            createCharacter();
            makeWater();
            addModels();
            character.Activate();
        }

        private void makeWater()
        {
            var tris = new List<BEPUutilities.Vector3[]>();
            float basinWidth = 22;
            float basinLength = 17;
            float waterHeight = 2f;
            BEPUutilities.Vector3 offset = new BEPUutilities.Vector3(40.42398f, 58.18201f, -5f);

            //Remember, the triangles composing the surface need to be coplanar with the surface.  In this case, this means they have the same height.
            tris.Add(new[]
                         {
                             new BEPUutilities.Vector3(-basinWidth / 2, -basinLength / 2, 0) + offset, new BEPUutilities.Vector3(basinWidth / 2, -basinLength / 2, 0) + offset,
                             new BEPUutilities.Vector3(-basinWidth / 2, basinLength / 2, 0) + offset
                         });
            tris.Add(new[]
                         {
                             new BEPUutilities.Vector3(-basinWidth / 2, basinLength / 2, 0) + offset, new BEPUutilities.Vector3(basinWidth / 2, -basinLength / 2, 0) + offset,
                             new BEPUutilities.Vector3(basinWidth / 2, basinLength / 2, 0) + offset
                         });
            FluidVolume water = new FluidVolume(Vector3.UnitZ, -9.81f, tris, waterHeight, 5f, 0.8f, 0.7f);
            water.CollisionRules.Group = GameModel.NormalGroup;
            Renderer.Add(Loader.Water, true);
            Waters.Add(water);
            GameManager.Space.Add(water);

            tris = new List<BEPUutilities.Vector3[]>();
            basinWidth = 12;
            basinLength = 22;
            waterHeight = 2;
            offset = new BEPUutilities.Vector3(72.36915f, 21.0182f, -5f);

            tris.Add(new[]
                         {
                             new BEPUutilities.Vector3(-basinWidth / 2, -basinLength / 2, 0) + offset, new BEPUutilities.Vector3(basinWidth / 2, -basinLength / 2, 0) + offset,
                             new BEPUutilities.Vector3(-basinWidth / 2, basinLength / 2, 0) + offset
                         });
            tris.Add(new[]
                         {
                             new BEPUutilities.Vector3(-basinWidth / 2, basinLength / 2, 0) + offset, new BEPUutilities.Vector3(basinWidth / 2, -basinLength / 2, 0) + offset,
                             new BEPUutilities.Vector3(basinWidth / 2, basinLength / 2, 0) + offset
                         });
            water = new FluidVolume(Vector3.UnitZ, -9.81f, tris, waterHeight, 5f, 0.8f, 0.7f);
            water.CollisionRules.Group = GameModel.NormalGroup;
            Renderer.Add(Loader.Water2, true);
            Waters.Add(water);
            GameManager.Space.Add(water);
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

            createTrees();
            createOranges();
            createAnvils();
            createBalls();
            createFridges();
            createRubble();

            createFirstSetOfDoors(e);
            createSecondSetOfDoors(e);
            createThirdSetOfDoors(e);
            createFourthSetOfDoors(e);

            createFinalRoom();
        }

        private void createFinalRoom()
        {
            createBoard();

            GameModel wireframe = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(79.73603f, 37.46315f, 6.9f), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Wireframe", Loader.BallTexture,
                    new PhysicsProperties(null, null, null, null, false, true)) { Wireframe = true }, false);
            Renderer.Add(wireframe);
            GameManager.Space.Add(wireframe);
            wireframe = new GameModel(new Vector3(74, 38.4f, 6.84868f), Loader.Fridge,
                new GameTexture("Wireframe", Loader.FridgeTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true }, false);
            Renderer.Add(wireframe);
            GameManager.Space.Add(wireframe);
            wireframe = new GameModel(new Vector3(75.92088f, 37.90436f, 7.41689f), Loader.Rubble,
                new GameTexture("Wireframe", Loader.RubbleTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true }, false);
            Renderer.Add(wireframe);
            GameManager.Space.Add(wireframe);
            wireframe = new GameModel(new Vector3(77.25307f, 38.5f, 8.36394f), Loader.Anvil,
                new GameTexture("Wireframe", Loader.AnvilTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true }, false);
            Renderer.Add(wireframe);
            GameManager.Space.Add(wireframe);
            wireframe = new GameModel(new Vector3(78.99118f, 38.5f, 9.56803f), Loader.Orange,
                new GameTexture("Wireframe", Loader.OrangeTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true }, false);
            Renderer.Add(wireframe);
            GameManager.Space.Add(wireframe);
            wireframe = new GameModel(new Vector3(80.3f, 37.85432f, 9.9f), Loader.Tree,
                new GameTexture("Wireframe", Loader.TreeTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true }, false);
            wireframe.Entity.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.PiOver2);
            Renderer.Add(wireframe);
            GameManager.Space.Add(wireframe);
            wireframe = new GameModel(new Vector3(72.27454f, 36.96674f, 6.69829f), Loader.LeftDoorAlt,
                new GameTexture("Wireframe", Loader.DoorTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true }, false);
            wireframe.Entity.Orientation = Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.PiOver2);
            Renderer.Add(wireframe);
            GameManager.Space.Add(wireframe);
            wireframe = new GameModel(new Vector3(72.29584f, 36.02619f, 6.1818f), Loader.Button,
                new GameTexture("Wireframe", Loader.ButtonTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true }, false);
            Renderer.Add(wireframe);
            GameManager.Space.Add(wireframe);

            GameModel cage = new GameModel(new Vector3(83, 29, 12.49821f), Loader.Cage,
                new GameTexture("Cage", Loader.AnvilTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true }, false);
            Renderer.Add(cage);
            GameManager.Space.Add(cage);

            GameModel trophy = new GameModel(new Vector3(83, 29, 12), Loader.Trophy,
                new GameTexture("Trophy!", Loader.TrophyTexture,
                    new PhysicsProperties(null, null, null, 10f, true, true),
                    new GameProperties(gameProps => { if(gameProps.OwningModel.Model != Loader.Trophy) gameProps.SetStateObject(true); },
                        gameProps => { if((bool)gameProps.UpdateStateObject == true) { GameManager.State = GameState.Ending; finalModel = gameProps.OwningModel; animating = true; } }, true, null, null)));
            trophy.Texture.GameProperties.SetStateObject(false);
            Renderer.Add(trophy);
            GameManager.Space.Add(trophy);
        }

        private void createFirstSetOfDoors(Entity e)
        {
            GameModel button = new GameModel(new Vector3(0, 31, 3.175f), Loader.Button,
                new GameTexture("Button", Loader.ButtonTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(null, null, false,
                        (sender, other, pair) =>
                        {
                            EntityCollidable otherEntity = other as EntityCollidable;
                            if(otherEntity == null || otherEntity.Entity == null)
                                return;

                            GameModel m = otherEntity.Entity.Tag as GameModel;
                            if(m != null && m.Texture.FriendlyName == "Orange" && !m.Texture.Wireframe)
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
                            if(m != null && m.Texture.FriendlyName == "Orange" && !m.Texture.Wireframe)
                            {
                                GameModel pThis = sender.Entity.Tag as GameModel;
                                PrismaticJoint pMotor = pThis.Texture.GameProperties.UpdateStateObject as PrismaticJoint;
                                pMotor.Motor.Settings.Servo.Goal = 0;
                            }
                        })), false);
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

            Action<GameProperties> doorUpdate = gameProps =>
            {
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
            RevoluteJoint angularJoint = new RevoluteJoint(e, leftDoor.Entity, leftDoor.Entity.Position + new BEPUutilities.Vector3(-0.5f, 0, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.IsActive = true;
            angularJoint.Motor.Basis.SetWorldAxes(-Vector3.UnitZ, Vector3.UnitY);
            GameManager.Space.Add(angularJoint);
            leftDoor.Texture.GameProperties.SetStateObject(angularJoint);

            GameModel rightDoor = new GameModel(new Vector3(0.5f, 36.8f, 7), Loader.RightDoor,
                new GameTexture("Door", Loader.DoorTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(doorUpdate, null, false, null, null)), false);
            rightDoor.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            angularJoint = new RevoluteJoint(e, rightDoor.Entity, rightDoor.Entity.Position - new BEPUutilities.Vector3(-0.5f, 0, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.Basis.SetWorldAxes(Vector3.UnitZ, Vector3.UnitY);
            angularJoint.Motor.IsActive = true;
            GameManager.Space.Add(angularJoint);
            rightDoor.Texture.GameProperties.SetStateObject(angularJoint);

            Renderer.Add(leftDoor);
            Renderer.Add(rightDoor);
            Renderer.Add(button);
            GameManager.Space.Add(leftDoor);
            GameManager.Space.Add(rightDoor);
            GameManager.Space.Add(button);
        }

        private void createSecondSetOfDoors(Entity e)
        {
            GameModel button1 = new GameModel(new Vector3(72, 14, -6.9f + 0.075f), Loader.Button,
                new GameTexture("Button", Loader.ButtonTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(null, null, false,
                        (sender, other, pair) =>
                        {
                            EntityCollidable otherEntity = other as EntityCollidable;
                            if(otherEntity == null || otherEntity.Entity == null)
                                return;

                            GameModel m = otherEntity.Entity.Tag as GameModel;
                            if(m != null && m.Texture.FriendlyName == "Anvil" && !m.Texture.Wireframe)
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
                            if(m != null && m.Texture.FriendlyName == "Anvil" && !m.Texture.Wireframe)
                            {
                                GameModel pThis = sender.Entity.Tag as GameModel;
                                PrismaticJoint pMotor = pThis.Texture.GameProperties.UpdateStateObject as PrismaticJoint;
                                pMotor.Motor.Settings.Servo.Goal = 0;
                            }
                        })), false);
            button1.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            PrismaticJoint lineJoint = new PrismaticJoint(e, button1.Entity, button1.Entity.Position, Vector3.UnitZ, button1.Entity.Position);
            lineJoint.IsActive = true;
            lineJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            lineJoint.Motor.IsActive = true;
            lineJoint.Limit.Minimum = -0.07f;
            lineJoint.Limit.Maximum = 0;
            lineJoint.Limit.IsActive = true;
            GameManager.Space.Add(lineJoint);
            button1.Texture.GameProperties.SetStateObject(lineJoint);

            GameModel button2 = new GameModel(new Vector3(72, 8, -3.9f + 0.085f), Loader.Button,
                new GameTexture("Button", Loader.ButtonTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(null, null, false,
                        (sender, other, pair) =>
                        {
                            EntityCollidable otherEntity = other as EntityCollidable;
                            if(otherEntity == null || otherEntity.Entity == null)
                                return;

                            GameModel m = otherEntity.Entity.Tag as GameModel;
                            if(m != null && m.Texture.FriendlyName == "Beach ball" && !m.Texture.Wireframe)
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
                            if(m != null && m.Texture.FriendlyName == "Beach ball" && !m.Texture.Wireframe)
                            {
                                GameModel pThis = sender.Entity.Tag as GameModel;
                                PrismaticJoint pMotor = pThis.Texture.GameProperties.UpdateStateObject as PrismaticJoint;
                                pMotor.Motor.Settings.Servo.Goal = 0;
                            }
                        })), false);
            button2.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            PrismaticJoint lineJoint2 = new PrismaticJoint(e, button2.Entity, button2.Entity.Position, Vector3.UnitZ, button2.Entity.Position);
            lineJoint2.IsActive = true;
            lineJoint2.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            lineJoint2.Motor.IsActive = true;
            lineJoint2.Limit.Minimum = -0.07f;
            lineJoint2.Limit.Maximum = 0;
            lineJoint2.Limit.IsActive = true;
            GameManager.Space.Add(lineJoint2);
            button2.Texture.GameProperties.SetStateObject(lineJoint2);

            Action<GameProperties> doorUpdate = gameProps =>
            {
                if(lineJoint.IsActive && lineJoint.Motor.Settings.Servo.Goal != 0 && 
                    lineJoint2.IsActive && lineJoint2.Motor.Settings.Servo.Goal != 0)
                    (gameProps.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = 0;
                else
                    (gameProps.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = MathHelper.PiOver2;
            };

            GameModel leftDoor = new GameModel(new Vector3(75.5f, 3.2f, -3), Loader.LeftDoor,
                new GameTexture("Door", Loader.DoorTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(doorUpdate, null, false, null, null)), false);
            leftDoor.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            RevoluteJoint angularJoint = new RevoluteJoint(e, leftDoor.Entity, leftDoor.Entity.Position + new BEPUutilities.Vector3(-0.5f, 0, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.IsActive = true;
            angularJoint.Motor.Basis.SetWorldAxes(-Vector3.UnitZ, Vector3.UnitY);
            GameManager.Space.Add(angularJoint);
            leftDoor.Texture.GameProperties.SetStateObject(angularJoint);

            GameModel rightDoor = new GameModel(new Vector3(76.5f, 3.2f, -3), Loader.RightDoor,
                new GameTexture("Door", Loader.DoorTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(doorUpdate, null, false, null, null)), false);
            rightDoor.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            angularJoint = new RevoluteJoint(e, rightDoor.Entity, rightDoor.Entity.Position - new BEPUutilities.Vector3(-0.5f, 0, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.Basis.SetWorldAxes(Vector3.UnitZ, Vector3.UnitY);
            angularJoint.Motor.IsActive = true;
            GameManager.Space.Add(angularJoint);
            rightDoor.Texture.GameProperties.SetStateObject(angularJoint);

            Renderer.Add(leftDoor);
            Renderer.Add(rightDoor);
            Renderer.Add(button1);
            Renderer.Add(button2);
            GameManager.Space.Add(leftDoor);
            GameManager.Space.Add(rightDoor);
            GameManager.Space.Add(button1);
            GameManager.Space.Add(button2);
        }

        private void createThirdSetOfDoors(Entity e)
        {
            Func<GameModel, GameModel, GameTexture> getButtonTex = (b, b2) => {
                return new GameTexture("Button", Loader.PurpleButtonTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(null, null, false,
                        (sender, other, pair) =>
                        {
                            EntityCollidable otherEntity = other as EntityCollidable;
                            if(otherEntity == null || otherEntity.Entity == null)
                                return;

                            GameModel m = otherEntity.Entity.Tag as GameModel;
                            if(m != null && m.Texture.FriendlyName == "Orange" && !m.Texture.Wireframe)
                            {
                                GameModel pThis = sender.Entity.Tag as GameModel;
                                if(pThis == b || pThis == b2)
                                {
                                    PrismaticJoint pMotor = pThis.Texture.GameProperties.UpdateStateObject as PrismaticJoint;
                                    pMotor.Motor.Settings.Servo.Goal = -0.07f;
                                }
                            }
                        },
                        (sender, other, pair) =>
                        {
                            EntityCollidable otherEntity = other as EntityCollidable;
                            if(otherEntity == null || otherEntity.Entity == null)
                                return;

                            GameModel m = otherEntity.Entity.Tag as GameModel;
                            if(m != null && m.Texture.FriendlyName == "Orange" && !m.Texture.Wireframe)
                            {
                                GameModel pThis = sender.Entity.Tag as GameModel;
                                if(pThis == b || pThis == b2)
                                {
                                    PrismaticJoint pMotor = pThis.Texture.GameProperties.UpdateStateObject as PrismaticJoint;
                                    pMotor.Motor.Settings.Servo.Goal = 0;
                                }
                            }
                        },
                        rippedFrom =>
                        {
                            PrismaticJoint pMotor = rippedFrom.Texture.GameProperties.UpdateStateObject as PrismaticJoint;
                            pMotor.Motor.Settings.Servo.Goal = 0;
                        },
                        onApplied => {
                            onApplied.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
                        }));
            };

            Entity button1Ent = new BEPUphysics.Entities.Prefabs.Box(new Vector3(17, 5, 6.1f + 0.075f), 0.55f, 0.55f, 0.1f);
            PrismaticJoint lineJoint = new PrismaticJoint(e, button1Ent, button1Ent.Position, Vector3.UnitZ, button1Ent.Position);
            lineJoint.IsActive = true;
            lineJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            lineJoint.Motor.IsActive = true;
            lineJoint.Limit.Minimum = -0.07f;
            lineJoint.Limit.Maximum = 0;
            lineJoint.Limit.IsActive = true;

            Entity button2Ent = new BEPUphysics.Entities.Prefabs.Box(new Vector3(17, 7, 6.1f + 0.075f), 0.55f, 0.55f, 0.1f);
            PrismaticJoint lineJoint2 = new PrismaticJoint(e, button2Ent, button2Ent.Position, Vector3.UnitZ, button2Ent.Position);
            lineJoint2.IsActive = true;
            lineJoint2.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            lineJoint2.Motor.IsActive = true;
            lineJoint2.Limit.Minimum = -0.07f;
            lineJoint2.Limit.Maximum = 0;
            lineJoint2.Limit.IsActive = true;

            Action<GameProperties> doorUpdate = gameProps =>
            {
                if(gameProps.OwningModel.Model == Loader.LeftDoorAlt || gameProps.OwningModel.Model == Loader.RightDoorAlt)
                {
                    if(lineJoint.IsActive && lineJoint.Motor.Settings.Servo.Goal != 0 &&
                        lineJoint2.IsActive && lineJoint2.Motor.Settings.Servo.Goal != 0)
                        (gameProps.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = 0;
                    else
                        (gameProps.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = MathHelper.PiOver2;
                }
            };

            Func<GameTexture> getDoorTexture = () =>
            {
                return new GameTexture("Door", Loader.PurpleDoorTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(doorUpdate, null, false, null, null,
                        onRipped => {
                            (onRipped.Texture.GameProperties.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = MathHelper.PiOver2;
                        },
                        onApplied => {
                            onApplied.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
                        }));
            };

            GameModel button1 = new GameModel(button1Ent, Vector3.Zero, Loader.Button,
                getDoorTexture());
            button1.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            GameManager.Space.Add(lineJoint);

            GameModel button2 = new GameModel(button2Ent, Vector3.Zero, Loader.Button,
                getDoorTexture());
            button2.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            GameManager.Space.Add(lineJoint2);

            GameModel leftDoor = new GameModel(new Vector3(28.8f, 5.5f, 7), Loader.LeftDoorAlt,
                getButtonTex(button1, button2));
            leftDoor.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            RevoluteJoint angularJoint = new RevoluteJoint(e, leftDoor.Entity, leftDoor.Entity.Position + new BEPUutilities.Vector3(0, -0.5f, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Servo.Goal = MathHelper.PiOver2;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.Basis.SetWorldAxes(Vector3.UnitZ, Vector3.UnitX);
            angularJoint.Motor.IsActive = true;
            GameManager.Space.Add(angularJoint);
            leftDoor.Texture.GameProperties.SetStateObject(lineJoint);
            button1.Texture.GameProperties.SetStateObject(angularJoint);

            GameModel rightDoor = new GameModel(new Vector3(28.8f, 6.5f, 7), Loader.RightDoorAlt,
                getButtonTex(button1, button2));
            rightDoor.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            angularJoint = new RevoluteJoint(e, rightDoor.Entity, rightDoor.Entity.Position - new BEPUutilities.Vector3(0, -0.5f, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Servo.Goal = MathHelper.PiOver2;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.Basis.SetWorldAxes(-Vector3.UnitZ, Vector3.UnitX);
            angularJoint.Motor.IsActive = true;
            GameManager.Space.Add(angularJoint);
            rightDoor.Texture.GameProperties.SetStateObject(lineJoint2);
            button2.Texture.GameProperties.SetStateObject(angularJoint);

            Renderer.Add(leftDoor);
            Renderer.Add(rightDoor);
            Renderer.Add(button1);
            Renderer.Add(button2);
            GameManager.Space.Add(leftDoor);
            GameManager.Space.Add(rightDoor);
            GameManager.Space.Add(button1);
            GameManager.Space.Add(button2);
        }

        private void createFourthSetOfDoors(Entity e)
        {
            Func<GameTexture> getButtonTex = () =>
            {
                return new GameTexture("Button", Loader.ButtonTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(null, null, false,
                        (sender, other, pair) =>
                        {
                            EntityCollidable otherEntity = other as EntityCollidable;
                            if(otherEntity == null || otherEntity.Entity == null)
                                return;

                            GameModel m = otherEntity.Entity.Tag as GameModel;
                            if(m != null && m.Texture.FriendlyName == "Orange" && !m.Texture.Wireframe)
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
                            if(m != null && m.Texture.FriendlyName == "Orange" && !m.Texture.Wireframe)
                            {
                                GameModel pThis = sender.Entity.Tag as GameModel;
                                PrismaticJoint pMotor = pThis.Texture.GameProperties.UpdateStateObject as PrismaticJoint;
                                pMotor.Motor.Settings.Servo.Goal = 0;
                            }
                        }));
            };

            Entity button1Ent = new BEPUphysics.Entities.Prefabs.Box(new Vector3(53, 37, 6.1f + 0.075f), 0.55f, 0.55f, 0.1f);
            PrismaticJoint lineJoint = new PrismaticJoint(e, button1Ent, button1Ent.Position, Vector3.UnitZ, button1Ent.Position);
            lineJoint.IsActive = true;
            lineJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            lineJoint.Motor.IsActive = true;
            lineJoint.Limit.Minimum = -0.07f;
            lineJoint.Limit.Maximum = 0;
            lineJoint.Limit.IsActive = true;

            Entity button2Ent = new BEPUphysics.Entities.Prefabs.Box(new Vector3(53, 27, 6.1f + 0.075f), 0.55f, 0.55f, 0.1f);
            PrismaticJoint lineJoint2 = new PrismaticJoint(e, button2Ent, button2Ent.Position, Vector3.UnitZ, button2Ent.Position);
            lineJoint2.IsActive = true;
            lineJoint2.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            lineJoint2.Motor.IsActive = true;
            lineJoint2.Limit.Minimum = -0.07f;
            lineJoint2.Limit.Maximum = 0;
            lineJoint2.Limit.IsActive = true;

            bool forceCloseDoors = false;
            Action<GameProperties> doorUpdate = gameProps => {
                if(gameProps.OwningModel.Model == Loader.LeftDoorAlt || gameProps.OwningModel.Model == Loader.RightDoorAlt)
                {
                    if(forceCloseDoors)
                    {
                        (gameProps.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = MathHelper.PiOver2;
                        return;
                    }

                    if(lineJoint.IsActive && lineJoint.Motor.Settings.Servo.Goal != 0 &&
                        lineJoint2.IsActive && lineJoint2.Motor.Settings.Servo.Goal != 0)
                        (gameProps.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = 0;
                    else
                        (gameProps.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = MathHelper.PiOver2;
                }
            };

            Func<GameTexture> getDoorTexture = () => {
                return new GameTexture("Door", Loader.PurpleDoorTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(doorUpdate, null, false, null, null,
                        onRipped =>
                        {
                            (onRipped.Texture.GameProperties.UpdateStateObject as RevoluteJoint).Motor.Settings.Servo.Goal = MathHelper.PiOver2;
                        },
                        onApplied =>
                        {
                            onApplied.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
                        }));
            };

            GameModel button1 = new GameModel(button1Ent, Vector3.Zero, Loader.Button,
                getButtonTex(), false);
            button1.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            GameManager.Space.Add(lineJoint);

            GameModel button2 = new GameModel(button2Ent, Vector3.Zero, Loader.Button,
                getButtonTex(), false);
            button2.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            GameManager.Space.Add(lineJoint2);

            GameModel sphere1 = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(15, 38, 11.5f), 1), Vector3.Zero, Loader.Ball,
                getDoorTexture());

            GameModel sphere2 = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(15, 36, 11.5f), 1), Vector3.Zero, Loader.Ball,
                getDoorTexture());

            GameModel leftDoor = new GameModel(new Vector3(54.8f, 31.5f, 7), Loader.LeftDoorAlt,
                new GameTexture("Wireframe", Loader.PurpleDoorTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true });
            leftDoor.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            RevoluteJoint angularJoint = new RevoluteJoint(e, leftDoor.Entity, leftDoor.Entity.Position + new BEPUutilities.Vector3(0, -0.5f, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Servo.Goal = MathHelper.PiOver2;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.Basis.SetWorldAxes(Vector3.UnitZ, Vector3.UnitX);
            angularJoint.Motor.IsActive = true;
            GameManager.Space.Add(angularJoint);
            sphere1.Texture.GameProperties.SetStateObject(angularJoint);
            button1.Texture.GameProperties.SetStateObject(lineJoint);

            GameModel rightDoor = new GameModel(new Vector3(54.8f, 32.5f, 7), Loader.RightDoorAlt,
                new GameTexture("Wireframe", Loader.PurpleDoorTexture,
                    new PhysicsProperties(null, null, null, 900, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true });
            rightDoor.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            angularJoint = new RevoluteJoint(e, rightDoor.Entity, rightDoor.Entity.Position - new BEPUutilities.Vector3(0, -0.5f, 0), Vector3.UnitZ);
            angularJoint.IsActive = true;
            angularJoint.Motor.Settings.Servo.Goal = MathHelper.PiOver2;
            angularJoint.Motor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
            angularJoint.Motor.Basis.SetWorldAxes(-Vector3.UnitZ, Vector3.UnitX);
            angularJoint.Motor.IsActive = true;
            GameManager.Space.Add(angularJoint);
            sphere2.Texture.GameProperties.SetStateObject(angularJoint);
            button2.Texture.GameProperties.SetStateObject(lineJoint2);

            Renderer.Add(leftDoor);
            Renderer.Add(rightDoor);
            Renderer.Add(button1);
            Renderer.Add(button2);
            Renderer.Add(sphere1);
            Renderer.Add(sphere2);
            GameManager.Space.Add(leftDoor);
            GameManager.Space.Add(rightDoor);
            GameManager.Space.Add(button1);
            GameManager.Space.Add(button2);
            GameManager.Space.Add(sphere1);
            GameManager.Space.Add(sphere2);

            Entity doorCloser = new BEPUphysics.Entities.Prefabs.Box(new Vector3(61.38556f, 31.87945f, 7.78949f), 0.1f, 4, 4);
            doorCloser.CollisionInformation.Events.InitialCollisionDetected += (sender, other, pair) => {
                var otherEntity = other as EntityCollidable;
                if(otherEntity != null)
                {
                    var player = otherEntity.Entity.Tag as Player;
                    if(player != null)
                    {
                        forceCloseDoors = true;
                        player.EmancipateTextures();
                        Loader.Zap.Play();
                    }
                    else
                    {
                        var model = otherEntity.Entity.Tag as GameModel;
                        if(model != null)
                        {
                            // EMANCIPATE
                            model.Space.Remove(model);
                            Renderer.Remove(model);
                            character.StopGrabbing();
                        }
                    }
                }
            };
            CollisionRules.AddRule(doorCloser, character.Entity, CollisionRule.NoSolver);
            GameModel closer = new GameModel(doorCloser, Vector3.Zero, Loader.DoorCloser,
                new GameTexture("Emancipator", Loader.EmancipatorTexture,
                    new PhysicsProperties(null, null, null, null, false, null),
                    new GameProperties(null, null, false, null, null),
                    new GraphicsProperties(null, true)), false);
            GameManager.Space.Add(closer);
            Renderer.Add(closer);
        }

        private void createRubble()
        {
            GameModel rubble = new GameModel(new BEPUphysics.Entities.Prefabs.Box(new Vector3(6.77804f, 56.03858f, -3.90114f), 2.236f, 1.943f, 1.148f), Vector3.Zero, Loader.Rubble,
                new GameTexture("Rubble", Loader.RubbleTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)));
            rubble.Entity.CollisionInformation.CollisionRules.Group = GameModel.FunctionalGroup;
            Renderer.Add(rubble);
            GameManager.Space.Add(rubble);
        }

        private void createBoard()
        {
            GameModel board = new GameModel(new Vector3(80.9f, 32, 7), Loader.Board,
                new GameTexture("Wireframe", Loader.SeecretTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)) { Wireframe = true }, false);
            Renderer.Add(board);
            GameManager.Space.Add(board);
        }

        private void createFridges()
        {
            GameModel fridge = new GameModel(new Vector3(65, -14, -3 + 0.3f), Loader.Fridge,
                new GameTexture("Refridgerator", Loader.FridgeTexture,
                    new PhysicsProperties(null, null, null, 9001, true, true),
                    new GameProperties(null, null, false, null, null)));
            GameManager.Space.Add(fridge);
            Renderer.Add(fridge);
            fridge = new GameModel(new Vector3(58, -25, -1), Loader.Fridge,
                new GameTexture("Refridgerator", Loader.FridgeTexture,
                    new PhysicsProperties(null, null, null, 9001, true, true),
                    new GameProperties(null, null, false, null, null)));
            fridge.Entity.Orientation = BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitY, MathHelper.PiOver2) * BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.ToRadians(-10));
            GameManager.Space.Add(fridge);
            Renderer.Add(fridge);
            fridge = new GameModel(new Vector3(68.696f, -20.17903f, -1), Loader.Fridge,
                new GameTexture("Refridgerator", Loader.FridgeTexture,
                    new PhysicsProperties(null, null, null, 9001, true, true),
                    new GameProperties(null, null, false, null, null)));
            fridge.Entity.Orientation = BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitX, MathHelper.PiOver2);
            GameManager.Space.Add(fridge);
            Renderer.Add(fridge);
        }

        private void createBalls()
        {
            GameModel ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(33, 57, -3), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Beach ball", Loader.BallTexture,
                    new PhysicsProperties(2.5f, 0.3f, 0.2f, 3f, true, true)));
            Renderer.Add(ball);
            GameManager.Space.Add(ball);
            ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(35, 58, -3), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Beach ball", Loader.BallTexture,
                    new PhysicsProperties(2.5f, 0.3f, 0.2f, 3f, true, true)));
            Renderer.Add(ball);
            GameManager.Space.Add(ball);
            ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(37, 56, -3), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Beach ball", Loader.BallTexture,
                    new PhysicsProperties(2.5f, 0.3f, 0.2f, 3f, true, true)));
            Renderer.Add(ball);
            GameManager.Space.Add(ball);
            ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(36, 60, -3), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Beach ball", Loader.BallTexture,
                    new PhysicsProperties(2.5f, 0.3f, 0.2f, 3f, true, true)));
            Renderer.Add(ball);
            GameManager.Space.Add(ball);
            ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(38, 59, -3), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Beach ball", Loader.BallTexture,
                    new PhysicsProperties(2.5f, 0.3f, 0.2f, 3f, true, true)));
            Renderer.Add(ball);
            GameManager.Space.Add(ball);
            ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(72, 14, -3), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Beach ball", Loader.BallTexture,
                    new PhysicsProperties(2.5f, 0.3f, 0.2f, 3f, true, true)));
            Renderer.Add(ball);
            GameManager.Space.Add(ball);
            ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(78.68232f, -20.89572f, -3), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Beach ball", Loader.BallTexture,
                    new PhysicsProperties(2.5f, 0.3f, 0.2f, 3f, true, true)));
            Renderer.Add(ball);
            GameManager.Space.Add(ball);
            ball = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(37, 37, 7), 1), Vector3.Zero, Loader.Ball,
                new GameTexture("Beach ball", Loader.BallTexture,
                    new PhysicsProperties(2.5f, 0.3f, 0.2f, 3f, true, true)));
            Renderer.Add(ball);
            GameManager.Space.Add(ball);
        }

        private void createAnvils()
        {
            Vector3 anvilOffset = Vector3.UnitZ * 0.6282f;
            GameModel anvil;
            for(int i = 0; i < 5; i++)
            {
                anvil = new GameModel(new Vector3(47 - (i * 3), 53, -7) + anvilOffset, Loader.Anvil,
                    new GameTexture("Anvil", Loader.AnvilTexture,
                        new PhysicsProperties(null, null, null, 15000, true, true),
                        new GameProperties(null, null, false, null, null)));
                Renderer.Add(anvil);
                GameManager.Space.Add(anvil);
            }
            anvil = new GameModel(new Vector3(69.40233f, -23.71829f, -3) + anvilOffset, Loader.Anvil,
                new GameTexture("Anvil", Loader.AnvilTexture,
                    new PhysicsProperties(null, null, null, 15000, true, true),
                    new GameProperties(null, null, false, null, null)));
            Renderer.Add(anvil);
            GameManager.Space.Add(anvil);
            anvil = new GameModel(new Vector3(57.77859f, -16.25565f, -3) + anvilOffset, Loader.Anvil,
                new GameTexture("Anvil", Loader.AnvilTexture,
                    new PhysicsProperties(null, null, null, 15000, true, true),
                    new GameProperties(null, null, false, null, null)));
            Renderer.Add(anvil);
            GameManager.Space.Add(anvil);
            anvil = new GameModel(new Vector3(71.8497f, -16.28787f, -3) + anvilOffset, Loader.Anvil,
                new GameTexture("Anvil", Loader.AnvilTexture,
                    new PhysicsProperties(null, null, null, 15000, true, true),
                    new GameProperties(null, null, false, null, null)));
            Renderer.Add(anvil);
            GameManager.Space.Add(anvil);
            anvil = new GameModel(new Vector3(72, 33, -3.9f) + anvilOffset, Loader.Anvil,
                new GameTexture("Anvil", Loader.AnvilTexture,
                    new PhysicsProperties(null, null, null, 15000, true, true),
                    new GameProperties(null, null, false, null, null)));
            anvil.Entity.Orientation = BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.PiOver4);
            Renderer.Add(anvil);
            GameManager.Space.Add(anvil);
            anvil = new GameModel(new Vector3(18, 27, 10) + anvilOffset, Loader.Anvil,
                new GameTexture("Anvil", Loader.AnvilTexture,
                    new PhysicsProperties(null, null, null, 15000, true, true),
                    new GameProperties(null, null, false, null, null)));
            anvil.Entity.Orientation = BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(200));
            Renderer.Add(anvil);
            GameManager.Space.Add(anvil);
            anvil = new GameModel(new Vector3(15, 31, 10) + anvilOffset, Loader.Anvil,
                new GameTexture("Anvil", Loader.AnvilTexture,
                    new PhysicsProperties(null, null, null, 15000, true, true),
                    new GameProperties(null, null, false, null, null)));
            anvil.Entity.Orientation = BEPUutilities.Quaternion.CreateFromAxisAngle(Vector3.UnitZ, MathHelper.ToRadians(130));
            Renderer.Add(anvil);
            GameManager.Space.Add(anvil);
        }

        private void createOranges()
        {
            GameModel orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(-2, 32, 3.3f), 0.2f), Vector3.Zero, Loader.Orange,
                new GameTexture("Orange", Loader.OrangeTexture,
                    new PhysicsProperties(0.1f, null, null, 5f, true, true)));
            Renderer.Add(orange);
            GameManager.Space.Add(orange);
            orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(0, 0, 0.3f), 0.2f), Vector3.Zero, Loader.Orange,
                new GameTexture("???", Loader.SeecretTexture,
                    new PhysicsProperties(null, null, null, null, false, true),
                    new GameProperties(null, null, false, null, null)));
            Renderer.Add(orange);
            GameManager.Space.Add(orange);
            orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(22, 11, 6.3f), 0.2f), Vector3.Zero, Loader.Orange,
                new GameTexture("Orange", Loader.OrangeTexture,
                    new PhysicsProperties(0.1f, null, null, 5f, true, true)));
            Renderer.Add(orange);
            GameManager.Space.Add(orange);
            orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(28, 1, 6.3f), 0.2f), Vector3.Zero, Loader.Orange,
                new GameTexture("Orange", Loader.OrangeTexture,
                    new PhysicsProperties(0.1f, null, null, 5f, true, true)));
            Renderer.Add(orange);
            GameManager.Space.Add(orange);
            orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(39, 26, 6.3f), 0.2f), Vector3.Zero, Loader.Orange,
                new GameTexture("Orange", Loader.OrangeTexture,
                    new PhysicsProperties(0.1f, null, null, 5f, true, true)));
            Renderer.Add(orange);
            GameManager.Space.Add(orange);
            orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(49, 38, 6.3f), 0.2f), Vector3.Zero, Loader.Orange,
                new GameTexture("Orange", Loader.OrangeTexture,
                    new PhysicsProperties(0.1f, null, null, 5f, true, true)));
            Renderer.Add(orange);
            GameManager.Space.Add(orange);
            orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(-5, 52, -3f), 0.2f), Vector3.Zero, Loader.Orange,
                new GameTexture("Orange", Loader.OrangeTexture,
                    new PhysicsProperties(0.1f, null, null, 5f, true, true)));
            Renderer.Add(orange);
            GameManager.Space.Add(orange);

            // wireframe oranges in third room
            for(int i = 0; i < 3; i++)
                for(int j = 0; j < 3; j++)
                {
                    orange = new GameModel(new BEPUphysics.Entities.Prefabs.Sphere(new Vector3(76 - i, -22 - j, -3.5f), 0.2f), Vector3.Zero, Loader.Orange,
                        new GameTexture("Wireframe", Loader.OrangeTexture,
                            PhysicsProperties.WireframeProperties) { Wireframe = true });
                    Renderer.Add(orange);
                    GameManager.Space.Add(orange);
                }
        }

        private void createTrees()
        {
            GameModel tree = new GameModel(new Vector3(-5, 36, 7.1269f), Loader.Tree,
                new GameTexture("Tree", Loader.TreeTexture,
                    new PhysicsProperties(null, 0.9f, 0.8f, null, true, null),
                    new GameProperties(null, null, false, null, null),
                    new GraphicsProperties(null, true)));
            tree.Entity.CollisionInformation.Shape.Volume = 0.5f;
            Renderer.Add(tree);
            GameManager.Space.Add(tree);
            tree = new GameModel(new Vector3(5, 36, 7.1269f), Loader.Tree,
                new GameTexture("Tree", Loader.TreeTexture,
                    new PhysicsProperties(null, 0.9f, 0.8f, null, true, null),
                    new GameProperties(null, null, false, null, null),
                    new GraphicsProperties(null, true)));
            tree.Entity.CollisionInformation.Shape.Volume = 0.5f;
            Renderer.Add(tree);
            GameManager.Space.Add(tree);
            tree = new GameModel(new Vector3(5, 62, -2.8731f), Loader.Tree,
                new GameTexture("Tree", Loader.TreeTexture,
                    new PhysicsProperties(null, 0.9f, 0.8f, null, true, null),
                    new GameProperties(null, null, false, null, null),
                    new GraphicsProperties(null, true)));
            tree.Entity.CollisionInformation.Shape.Volume = 0.5f;
            Renderer.Add(tree);
            GameManager.Space.Add(tree);
            tree = new GameModel(new Vector3(62.53115f, -23.71829f, -3), Loader.Tree,
                new GameTexture("Tree", Loader.TreeTexture,
                    new PhysicsProperties(null, 0.9f, 0.8f, null, true, null),
                    new GameProperties(null, null, false, null, null),
                    new GraphicsProperties(null, true)));
            tree.Entity.CollisionInformation.Shape.Volume = 0.5f;
            Renderer.Add(tree);
            GameManager.Space.Add(tree);
            tree = new GameModel(new Vector3(81.54448f, -15.96822f, -3), Loader.Tree,
                new GameTexture("Tree", Loader.TreeTexture,
                    new PhysicsProperties(null, 0.9f, 0.8f, null, true, null),
                    new GameProperties(null, null, false, null, null),
                    new GraphicsProperties(null, true)));
            tree.Entity.CollisionInformation.Shape.Volume = 0.5f;
            Renderer.Add(tree);
            GameManager.Space.Add(tree);
        }

        private void createCharacter()
        {
            character = new Player(this);
        }

        public void End()
        {
            character.Deactivate();
            GameManager.Space.Clear();
            Renderer.Clear();
            Waters.Clear();
        }

        #region windows
#if WINDOWS
        protected override void OnActivated(object sender, EventArgs args)
        {
            if(GameManager.PreviousState == GameState.Running && GameManager.State != GameState.Ending)
                GameManager.State = GameState.Running;
            BGM.Resume();
            BGM.Volume = 1;

            base.OnActivated(sender, args);
        }

        protected override void OnDeactivated(object sender, EventArgs args)
        {
            if(GameManager.State == GameState.Running && GameManager.State != GameState.Ending)
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
