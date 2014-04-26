using Accelerated_Delivery_Win;
using BEPUphysics;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysicsDemos.AlternateMovement.Character;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD29
{
    class Player
    {
        private CharacterControllerInput character;
        private GameTexture[] heldTextures = new GameTexture[5];
        private int textureIndex = 2;

        private bool canTakeTextures { get { if(character.IsGrabbing) return false; for(int i = 0; i < heldTextures.Length; i++) if(heldTextures[i] == null) return true; return false; } }

        public Player(BaseGame g)
        {
            character = new CharacterControllerInput(GameManager.Space, Renderer.Camera, g);
        }

        public void Draw()
        {
            // draw ui
        }

        public void Update(GameTime gameTime)
        {
            character.Update((float)gameTime.ElapsedGameTime.TotalSeconds);

            GameTexture targetedTexture = null;
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
            int indexDirection = Math.Sign(mouseScroll);
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
                addTextureToList(targetedModel.RipTexture());
            else if(heldTextures[textureIndex] != null && targetedModel != null && targetedTexture == null && Input.CheckForMouseJustReleased(2)) // looking at wireframe
            {
                targetedModel.GiveTexture(heldTextures[textureIndex]);
                heldTextures[textureIndex] = null;
                compressTextureList();
            }
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
            character.Activate();
        }

        public void Deactivate()
        {
            character.Deactivate();
            for(int i = 0; i < heldTextures.Length; i++)
                heldTextures[i] = null;
            textureIndex = 2;
        }

        bool RayCastFilter(BroadPhaseEntry entry)
        {
            return entry != character.CharacterController.Body.CollisionInformation && entry.CollisionRules.Personal <= CollisionRule.Normal;
        }
    }
}
