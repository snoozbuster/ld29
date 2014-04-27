using BEPUphysics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using ConversionHelper;
using BEPUutilities;
using LD29;
using BEPUphysicsDemos.SampleCode;
using Accelerated_Delivery_Win;
using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.CollisionRuleManagement;

namespace BEPUphysicsDemos.AlternateMovement.Character
{
    /// <summary>
    /// Handles input and movement of a character in the game.
    /// Acts as a simple 'front end' for the bookkeeping and math of the character controller.
    /// </summary>
    public class CharacterControllerInput
    {
        /// <summary>
        /// Gets the camera to use for input.
        /// </summary>
        public Camera Camera { get; private set; }

        /// <summary>
        /// Physics representation of the character.
        /// </summary>
        public CharacterController CharacterController;

        /// <summary>
        /// Gets the camera control scheme used by the character input.
        /// </summary>
        public CharacterCameraControlScheme CameraControlScheme { get; private set; }

        /// <summary>
        /// Gets whether the character controller's input management is being used.
        /// </summary>
        public bool IsActive { get; private set; }

        public bool IsGrabbing { get { return grabber.IsGrabbing || grabber.IsUpdating; } }

        /// <summary>
        /// Owning space of the character.
        /// </summary>
        public Space Space { get; private set; }

        protected float grabDistance;
        protected MotorizedGrabSpring grabber;

        /// <summary>
        /// Constructs the character and internal physics character controller.
        /// </summary>
        /// <param name="owningSpace">Space to add the character to.</param>
        /// <param name="camera">Camera to attach to the character.</param>
        /// <param name="game">The running game.</param>
        public CharacterControllerInput(Space owningSpace, Camera camera, BaseGame game)
        {
            CharacterController = new CharacterController();
            Camera = camera;
            CameraControlScheme = new CharacterCameraControlScheme(CharacterController, camera, game);
            grabber = new MotorizedGrabSpring();
            Space = owningSpace;
        }

        /// <summary>
        /// Gives the character control over the Camera and movement input.
        /// </summary>
        public void Activate()
        {
            if (!IsActive)
            {
                IsActive = true;
                Space.Add(CharacterController);
                Space.Add(grabber);
                //Offset the character start position from the camera to make sure the camera doesn't shift upward discontinuously.
                CharacterController.Body.Position = Camera.Position - new Vector3(0, 0, CameraControlScheme.StandingCameraOffset);
            }
        }

        /// <summary>
        /// Returns input control to the Camera.
        /// </summary>
        public void Deactivate()
        {
            if (IsActive)
            {
                IsActive = false;
                Space.Remove(CharacterController);
                Space.Remove(grabber);
            }
        }


        /// <summary>
        /// Handles the input and movement of the character.
        /// </summary>
        /// <param name="dt">Time since last frame in simulation seconds.</param>
        /// <param name="previousKeyboardInput">The last frame's keyboard state.</param>
        /// <param name="keyboardInput">The current frame's keyboard state.</param>
        /// <param name="previousGamePadInput">The last frame's gamepad state.</param>
        /// <param name="gamePadInput">The current frame's keyboard state.</param>
        public void Update(float dt)
        {
            if (IsActive)
            {
                //Note that the character controller's update method is not called here; this is because it is handled within its owning space.
                //This method's job is simply to tell the character to move around.

                CameraControlScheme.Update(dt);

                Vector2 totalMovement = Vector2.Zero;


                if(Input.ControlScheme == ControlScheme.XboxController)
                {
                    totalMovement += new Vector2(Input.CurrentPad.ThumbSticks.Left.X, Input.CurrentPad.ThumbSticks.Left.Y);

                    CharacterController.HorizontalMotionConstraint.SpeedScale = Math.Min(totalMovement.Length(), 1); //Don't trust the game pad to output perfectly normalized values.
                    CharacterController.HorizontalMotionConstraint.MovementDirection = totalMovement;

                    CharacterController.StanceManager.DesiredStance = Input.CurrentPad.IsButtonDown(Buttons.B) ? Stance.Crouching : Stance.Standing;

                    //Jumping
                    if(Input.CurrentPadLastFrame.IsButtonUp(Buttons.A) && Input.CurrentPad.IsButtonDown(Buttons.A))
                    {
                        CharacterController.Jump();
                    }
                }
                else if(Input.ControlScheme == ControlScheme.Keyboard)
                {
                    //Collect the movement impulses.

                    if(Input.KeyboardState.IsKeyDown(Keys.W))
                    {
                        totalMovement += new Vector2(0, 1);
                    }
                    if(Input.KeyboardState.IsKeyDown(Keys.S))
                    {
                        totalMovement += new Vector2(0, -1);
                    }
                    if(Input.KeyboardState.IsKeyDown(Keys.A))
                    {
                        totalMovement += new Vector2(-1, 0);
                    }
                    if(Input.KeyboardState.IsKeyDown(Keys.D))
                    {
                        totalMovement += new Vector2(1, 0);
                    }
                    if(totalMovement == Vector2.Zero)
                        CharacterController.HorizontalMotionConstraint.MovementDirection = Vector2.Zero;
                    else
                        CharacterController.HorizontalMotionConstraint.MovementDirection = Vector2.Normalize(totalMovement);


                    CharacterController.StanceManager.DesiredStance = Input.KeyboardState.IsKeyDown(Keys.LeftShift) ? Stance.Crouching : Stance.Standing;

                    //Jumping
                    if(Input.KeyboardLastFrame.IsKeyUp(Keys.Space) && Input.KeyboardState.IsKeyDown(Keys.Space))
                    {
                        CharacterController.Jump();
                    }
                }
                CharacterController.ViewDirection = Camera.WorldMatrix.Forward;

                #region Grabber Input

                //Update grabber

            if (((Input.ControlScheme == ControlScheme.XboxController && Input.CurrentPad.IsButtonDown(Buttons.X)) ||
                 (Input.ControlScheme == ControlScheme.Keyboard && Input.CheckKeyboardPress(Keys.E))) && !grabber.IsUpdating)
                {
                    //Find the earliest ray hit
                    RayCastResult raycastResult;
                    if(Space.RayCast(new Ray(Renderer.Camera.Position, Renderer.Camera.WorldMatrix.Forward), 10, RayCastFilter, out raycastResult))
                    {
                        var entityCollision = raycastResult.HitObject as EntityCollidable;
                        //If there's a valid ray hit, then grab the connected object!
                        if(entityCollision != null)
                        {
                            var tag = entityCollision.Entity.Tag as GameModel;
                            if(tag != null)
                            {
                                tag.Texture.GameProperties.RayCastHit();
                                if(tag.Texture.GameProperties.Grabbable)
                                {
                                    grabber.Setup(entityCollision.Entity, raycastResult.HitData.Location);
                                    grabDistance = 3.5f;
                                }
                            }
                        }
                    }

                }
                else if(grabber.IsUpdating)
                {
                    if((Input.ControlScheme == ControlScheme.XboxController && Input.CurrentPad.IsButtonDown(Buttons.X)) ||
                        (Input.ControlScheme == ControlScheme.Keyboard && Input.CheckKeyboardPress(Keys.E)))
                        grabber.Release();
                    grabber.GoalPosition = Renderer.Camera.Position + Renderer.Camera.WorldMatrix.Forward * grabDistance;
                }
                #endregion
            }
        }

        bool RayCastFilter(BroadPhaseEntry entry)
        {
            return entry != CharacterController.Body.CollisionInformation && entry.CollisionRules.Personal <= CollisionRule.Normal;
        }
    }
}