using BEPUphysics.BroadPhaseEntries;
using BEPUphysics.BroadPhaseEntries.Events;
using BEPUphysics.BroadPhaseEntries.MobileCollidables;
using BEPUphysics.NarrowPhaseSystems.Pairs;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD29
{
    struct PhysicsProperties
    {
        public float Bounciness { get { return bounciness.GetValueOrDefault();  } } // returns 0 if null
        private float? bounciness;

        public float StaticFriction { get { return staticFriction.HasValue ? staticFriction.Value : 0.6f; } }
        private float? staticFriction;

        public float KineticFriction { get { return kineticFriction.HasValue ? kineticFriction.Value : 0.5f; } }
        private float? kineticFriction;

        public float Mass { get { return mass.HasValue ? mass.Value : float.NegativeInfinity; } }
        private float? mass;

        public bool IsAffectedByGravity { get { return isAffectedByGravity.GetValueOrDefault(); } }
        private bool? isAffectedByGravity;

        public bool HasCollision { get { return hasCollision.HasValue ? hasCollision.Value : true; } }
        private bool? hasCollision;

        public PhysicsProperties(float? bounciness, float? staticFriction, float? kineticFriction, float? mass, bool? gravity, bool? hasCollision)
        {
            this.bounciness = bounciness;
            this.staticFriction = staticFriction;
            this.kineticFriction = kineticFriction;
            this.mass = mass;
            this.isAffectedByGravity = gravity;
            this.hasCollision = hasCollision;
        }

        // all defaults
        public PhysicsProperties WireframeProperties { get { return new PhysicsProperties(null, null, null, null, null, null); } }
    }

    struct GraphicsProperties
    {
        public bool Reflective { get { return reflective.GetValueOrDefault(); } }
        private bool? reflective;

        public bool Transparent { get { return transparent.GetValueOrDefault(); } }
        private bool? transparent;

        public GraphicsProperties(bool? reflective, bool? transparent)
        {
            this.reflective = reflective;
            this.transparent = transparent;
        }

        public GraphicsProperties WireframeProperties { get { return new GraphicsProperties(null, null); } }
    }

    class GameProperties
    {
        private static Action emptyFunction = delegate { };

        /// <summary>
        /// Called when collision between two objects starts.
        /// </summary>
        public InitialCollisionDetectedEventHandler<EntityCollidable> CollisionHandler { get { if(collisionHandler == null) collisionHandler = (x, y, z) => { }; return collisionHandler; } }
        private InitialCollisionDetectedEventHandler<EntityCollidable> collisionHandler;

        /// <summary>
        /// Called when collision between two objects ends.
        /// </summary>
        public CollisionEndedEventHandler<EntityCollidable> EndCollisionHandler { get { if(endCollisionHandler == null) endCollisionHandler = (x, y, z) => { }; return endCollisionHandler; } }
        private CollisionEndedEventHandler<EntityCollidable> endCollisionHandler;

        /// <summary>
        /// This is the update function you hook to Space.DuringForcesUpdatables.Starting. It's a wrapper to call Update(this).
        /// </summary>
        public Action SpaceUpdate { get { if(spaceUpdate == null) { GameProperties pThis = this; spaceUpdate = delegate { pThis.Update(pThis); }; } return spaceUpdate; } }
        private Action spaceUpdate;

        /// <summary>
        /// The actual update function that will be called. Can't be hooked to Space.DuringForcesUpdatables.Starting directly;
        /// hook SpaceUpdate instead.
        /// </summary>
        public Action<GameProperties> Update { get { if(update == null) update = x => { }; return update; } }
        private Action<GameProperties> update;

        /// <summary>
        /// Called when the player 'interacts' with this object. Useful for switches or other non-grabbable objects.
        /// </summary>
        public Action RayCastHit { get { return rayCastHit == null ? emptyFunction : rayCastHit; } }
        private Action rayCastHit;

        public bool Grabbable { get { return grabbable.HasValue ? grabbable.Value : true; } }
        private bool? grabbable;

        /// <summary>
        /// Called when a texture is ripped from a model, while the texture is still applied to the model.
        /// </summary>
        public Action<GameModel> OnTextureRipped { get { if(onTextureRipped == null) onTextureRipped = x => { }; return onTextureRipped; } }
        public Action<GameModel> onTextureRipped;

        /// <summary>
        /// Called when a texture is applied to a model, after the texture is applied to the model.
        /// </summary>
        public Action<GameModel> OnTextureApplied { get { if(onTextureApplied == null) onTextureApplied = x => { }; return onTextureApplied; } }
        public Action<GameModel> onTextureApplied;

        /// <summary>
        /// State object to store information in.
        /// </summary>
        public object UpdateStateObject;

        public GameProperties(Action<GameProperties> update, Action rayCast, bool? grabbable, 
            InitialCollisionDetectedEventHandler<EntityCollidable> collisionHandler, 
            CollisionEndedEventHandler<EntityCollidable> collisionEndingHandler,
            Action<GameModel> onTextureRipped = null,
            Action<GameModel> onTextureApplied = null)
        {
            this.update = update;
            this.collisionHandler = collisionHandler;
            this.endCollisionHandler = collisionEndingHandler;
            this.UpdateStateObject = null;
            this.spaceUpdate = null;
            this.grabbable = grabbable;
            this.rayCastHit = rayCast;
            this.onTextureRipped = onTextureRipped;
            this.onTextureApplied = onTextureApplied;
        }

        public void SetStateObject(object obj)
        {
            UpdateStateObject = obj;
        }

        public GameProperties WireframeProperties { get { return new GameProperties(null, null, null, null, null); } }
    }
}
