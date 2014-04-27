using Accelerated_Delivery_Win;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.Constraints.SingleEntity;
using BEPUphysics.UpdateableSystems;
using BEPUphysicsDemos.AlternateMovement.Character;
using ConversionHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MyExtensions = Accelerated_Delivery_Win.Extensions;

namespace LD29
{
    class Player
    {
        private CharacterControllerInput character;
        private GameTexture[] heldTextures = new GameTexture[5];
        private int textureIndex = 2;

        private GameTexture targetedTexture;

        private bool canTakeTextures { get { if(character.IsGrabbing) return false; for(int i = 0; i < heldTextures.Length; i++) if(heldTextures[i] == null) return true; return false; } }

        private Sprite dockTex;
        private Texture2D underwaterTex;
        private SpriteFont font;
        private Model missileModel;

        private Rectangle[] drawingRectangles = new Rectangle[5];
        private Rectangle screenRect; 

        private bool underwater;

        private List<TextureMissile> missiles = new List<TextureMissile>();

        private static CollisionGroup noCollisionGroup = new CollisionGroup();
        static Player()
        {
            CollisionGroup.DefineCollisionRule(noCollisionGroup, GameModel.GhostGroup, CollisionRule.NoBroadPhase);
            CollisionGroup.DefineCollisionRule(noCollisionGroup, GameModel.NormalGroup, CollisionRule.NoBroadPhase);
        }

        public Player(BaseGame g)
        {
            character = new CharacterControllerInput(GameManager.Space, Renderer.Camera, g);
            character.CharacterController.Body.CollisionInformation.CollisionRules.Group = GameModel.NormalGroup;

            dockTex = new Sprite(delegate { return g.Loader.Dock; }, new Vector2(RenderingDevice.Width / 2, RenderingDevice.Height * 0.95f), null, Sprite.RenderPoint.Center);
            font = g.Loader.Font;
            underwaterTex = g.Loader.TabletopDotPNG;
            missileModel = g.Loader.Burst;

            int add = 0;
            for(int i = 0; i < drawingRectangles.Length; i++)
            {
                add += 10;
                drawingRectangles[i] = new Rectangle((int)dockTex.UpperLeft.X + add, (int)dockTex.UpperLeft.Y - 140, 150, 150);
                add += 150;
                add += 10;
            }

            screenRect = new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height);
        }

        public void Draw()
        {
            RenderingDevice.SpriteBatch.Begin();

            if(underwater)
                RenderingDevice.SpriteBatch.Draw(underwaterTex, screenRect, Color.White);

            dockTex.Draw();
            if(targetedTexture != null && !character.IsGrabbing)
            {
                Vector2 length = font.MeasureString(targetedTexture.FriendlyName);
                RenderingDevice.SpriteBatch.DrawString(font, targetedTexture.FriendlyName, dockTex.UpperLeft - new Vector2(110, 5), Color.Black, 0, new Vector2(length.X / 2, 0), 1, SpriteEffects.None, 0);
            }
            if(heldTextures[textureIndex] != null)
            {
                Vector2 length = font.MeasureString(heldTextures[textureIndex].FriendlyName);
                RenderingDevice.SpriteBatch.DrawString(font, heldTextures[textureIndex].FriendlyName, new Vector2(dockTex.LowerRight.X, dockTex.UpperLeft.Y) + new Vector2(100, -10), Color.Black, 0, new Vector2(length.X / 2, 0), 1, SpriteEffects.None, 0);
            }

            for(int i = 0; i < heldTextures.Length; i++)
                if(heldTextures[i] != null && i != textureIndex)
                    RenderingDevice.SpriteBatch.Draw(heldTextures[i].ActualTexture, drawingRectangles[i], Color.White);
            if(heldTextures[textureIndex] != null)
            {
                Rectangle newRect = drawingRectangles[textureIndex];
                newRect.Inflate(20, 20);
                newRect.Y -= 20;
                RenderingDevice.SpriteBatch.Draw(heldTextures[textureIndex].ActualTexture, newRect, Color.White);
            }

            RenderingDevice.SpriteBatch.End();
        }

        public void Update(GameTime gameTime, List<FluidVolume> water)
        {
            character.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            underwater = false;
            foreach(FluidVolume f in water)
                if(f.BoundingBox.Max.X > Renderer.Camera.Position.X && f.BoundingBox.Min.X < Renderer.Camera.Position.X &&
                    f.BoundingBox.Max.Y > Renderer.Camera.Position.Y && f.BoundingBox.Min.Y < Renderer.Camera.Position.Y &&
                    f.BoundingBox.Max.Z > Renderer.Camera.Position.Z && f.BoundingBox.Min.Z < Renderer.Camera.Position.Z)
                {
                    underwater = true;
                    break;
                }

            targetedTexture = null;
            GameModel targetedModel = null;
            //Find the earliest ray hit
            RayCastResult raycastResult;
            if(GameManager.Space.RayCast(new BEPUutilities.Ray(Renderer.Camera.Position, Renderer.Camera.WorldMatrix.Forward), 10, RayCastFilter, out raycastResult))
            {
                var entityCollision = raycastResult.HitObject as EntityCollidable;
                if(entityCollision != null && entityCollision.Entity.Tag is GameModel)
                {
                    var tag = entityCollision.Entity.Tag as GameModel;
                    targetedModel = tag;
                    if(tag.CanBeRipped && !tag.Texture.Wireframe)
                        targetedTexture = tag.Texture;
                }
            }

            int mouseScroll = Input.MouseState.ScrollWheelValue - Input.MouseLastFrame.ScrollWheelValue;
            int indexDirection = -Math.Sign(mouseScroll);
            if(indexDirection != 0)
            {
                int originalIndex = textureIndex;
                do
                {
                    if(textureIndex == 0 && indexDirection == -1)
                        textureIndex = 4;
                    else
                        textureIndex = (textureIndex + indexDirection) % heldTextures.Length;
                } while(heldTextures[textureIndex] == null && originalIndex != textureIndex);
            }

            if(targetedModel != null && canTakeTextures && targetedTexture != null && Input.CheckForMouseJustReleased(1))
            {
                GameTexture t = targetedModel.RipTexture();
                GameTexture burstTex = new GameTexture("Burst", t.ActualTexture,
                    new PhysicsProperties(0, 0.5f, 0.5f, 10, false, true),
                    new GameProperties(null, null, false));
                GameModel m = new GameModel(targetedModel.Entity.Position, missileModel, burstTex, false);
                m.Entity.CollisionInformation.CollisionRules.Group = noCollisionGroup;
                CollisionRules.AddRule(m.Entity, character.CharacterController.Body, CollisionRule.NoSolver);
                TextureMissile missile = new TextureMissile(m, character.CharacterController.Body.Position, t, this);
                missiles.Add(missile);
                Renderer.Add(missile.Model);
                GameManager.Space.Add(missile);
            }
            else if(heldTextures[textureIndex] != null && targetedModel != null && targetedTexture == null && Input.CheckForMouseJustReleased(2)) // looking at wireframe
            {
                GameTexture t = heldTextures[textureIndex];
                GameTexture burstTex = new GameTexture("Burst", t.ActualTexture,
                    new PhysicsProperties(0, 0.5f, 0.5f, 10, false, true),
                    new GameProperties(null, null, false));
                GameModel m = new GameModel(character.CharacterController.Body.Position, missileModel, burstTex, false);
                m.Entity.CollisionInformation.CollisionRules.Group = noCollisionGroup;
                CollisionRules.AddRule(m.Entity, targetedModel.Entity, CollisionRule.NoSolver);
                TextureMissile missile = new TextureMissile(m, targetedModel.Entity.Position, t, this, targetedModel);
                missiles.Add(missile);
                Renderer.Add(missile.Model);
                GameManager.Space.Add(missile);

                heldTextures[textureIndex] = null;
                compressTextureList();
            }
        }

        private void giveTexture(TextureMissile missile)
        {
            addTextureToList(missile.CarriedTex);
            remove(missile);
        }

        private void remove(TextureMissile missile)
        {
            missiles.Remove(missile);
            Renderer.Remove(missile.Model);
            missile.Space.Remove(missile);
        }

        private void addTextureToList(GameTexture t)
        {
            int[] indexes = new[] { 2, 3, 1, 4, 0 };
            for(int i = 0; i < indexes.Length; i++)
                if(heldTextures[indexes[i]] == null)
                {
                    heldTextures[indexes[i]] = t;
                    return;
                }
            throw new InvalidOperationException("The texture list is full!");
        }

        private void compressTextureList()
        {
            List<GameTexture> temp = new List<GameTexture>();
            int i;
            for(i = 0; i < heldTextures.Length; i++)
            {
                if(heldTextures[i] != null)
                    temp.Add(heldTextures[i]);
                heldTextures[i] = null;
            }
            int startIndex = 2 - (temp.Count / 2); // integer division by design
            for(i = startIndex; i < startIndex + temp.Count; i++)
                heldTextures[i] = temp[i - startIndex];
            while(heldTextures[textureIndex] == null && textureIndex != 2)
            {
                int dir = Math.Sign(2 - textureIndex);
                textureIndex += dir;
            }
        }

        public void Activate()
        {
            Renderer.Camera.Position = new BEPUutilities.Vector3(0, 0, 5);
            character.Activate();
        }

        public void Deactivate()
        {
            character.Deactivate();
            missiles.Clear();
            for(int i = 0; i < heldTextures.Length; i++)
                heldTextures[i] = null;
            textureIndex = 2;
        }

        bool RayCastFilter(BroadPhaseEntry entry)
        {
            return entry != character.CharacterController.Body.CollisionInformation && entry.CollisionRules.Personal <= CollisionRule.Normal;
        }

        private class TextureMissile : ISpaceObject
        {
            public GameModel Model;
            public Vector3 Target;
            public bool FollowPlayer;
            private Player player;
            private GameModel targetModel;

            public GameTexture CarriedTex;

            private SingleEntityLinearMotor linearMotor;

            public TextureMissile(GameModel model, Vector3 target, GameTexture carriedTex, Player p, GameModel targetModel = null)
            {
                FollowPlayer = targetModel == null;
                player = p;
                this.targetModel = targetModel;

                Model = model;
                Target = target;
                CarriedTex = carriedTex;

                Vector3 up = Vector3.UnitZ;
                Vector3 forward = Target - MathConverter.Convert(model.Entity.Position);
                forward.Normalize();
                Vector3 left = Vector3.Cross(up, forward);

                BEPUutilities.Matrix3x3 m = new BEPUutilities.Matrix3x3();
                m.Left = left;
                m.Forward = forward;
                m.Up = up;
                Model.Entity.OrientationMatrix = m;

                linearMotor = new SingleEntityLinearMotor(model.Entity, model.Entity.Position);
                linearMotor.Settings.Mode = BEPUphysics.Constraints.TwoEntity.Motors.MotorMode.Servomechanism;
                linearMotor.Settings.Servo.Goal = Target;
                linearMotor.IsActive = true;
            }

            private void update()
            {
                if(FollowPlayer)
                    Target = linearMotor.Settings.Servo.Goal = player.character.CharacterController.Body.Position;

                Vector3 up = Vector3.UnitZ;
                Vector3 forward = Target - MathConverter.Convert(Model.Entity.Position);
                forward.Normalize();
                Vector3 left = Vector3.Cross(up, forward);

                BEPUutilities.Matrix3x3 m = new BEPUutilities.Matrix3x3();
                m.Left = left;
                m.Forward = forward;
                m.Up = up;
                Model.Entity.OrientationMatrix = m;
            }

            private void onCollision(EntityCollidable sender, Collidable other, BEPUphysics.NarrowPhaseSystems.Pairs.CollidablePairHandler pair)
            {
                var otherEntity = other as EntityCollidable;
                if(otherEntity != null && FollowPlayer && otherEntity == player.character.CharacterController.Body.CollisionInformation)
                    player.giveTexture(this);
                else if(otherEntity != null && !FollowPlayer)
                {
                    targetModel.GiveTexture(CarriedTex);
                    player.remove(this);
                }
            }

            public void OnAdditionToSpace(Space newSpace)
            {
                newSpace.Add(linearMotor);
                newSpace.Add(Model.Entity);
                Model.Entity.CollisionInformation.Events.DetectingInitialCollision += onCollision;
                Model.Entity.Space.DuringForcesUpdateables.Starting += update;
            }

            public void OnRemovalFromSpace(Space oldSpace)
            {
                Model.Entity.CollisionInformation.Events.DetectingInitialCollision -= onCollision;
                Model.Entity.Space.DuringForcesUpdateables.Starting -= update;
                oldSpace.Remove(linearMotor);
                oldSpace.Remove(Model.Entity);
            }

            public Space Space { get; set; }

            public object Tag { get; set; }
        }
    }
}
