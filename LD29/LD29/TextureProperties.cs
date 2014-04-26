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

        public float StaticFriction { get { return staticFriction.HasValue ? staticFriction.Value : 0.5f; } }
        private float? staticFriction;

        public float KineticFriction { get { return kineticFriction.HasValue ? kineticFriction.Value : 0.5f; } }
        private float? kineticFriction;

        public float Mass { get { return mass.HasValue ? mass.Value : 5; } }
        private float? mass;

        public bool IsAffectedByGravity { get { return isAffectedByGravity.HasValue ? isAffectedByGravity.Value : true; } }
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

    struct GameProperties
    {
        private static Action emptyFunction = delegate { };

        public InitialCollisionDetectedEventHandler<EntityCollidable>[] CollisionHandlers { get { return collisionHandlers; } }
        private InitialCollisionDetectedEventHandler<EntityCollidable>[] collisionHandlers;

        public Action Update { get { return update == null ? emptyFunction : update; } }
        private Action update;

        public bool Grabbable { get { return grabbable.HasValue ? grabbable.Value : true; } }
        private bool? grabbable;

        // an object containing state for the update function (locals, etc) that you can use and cast as necessary
        public object UpdateStateObject;

        public GameProperties(Action update, bool? grabbable, params InitialCollisionDetectedEventHandler<EntityCollidable>[] collisionHandlers)
        {
            this.update = update;
            this.collisionHandlers = collisionHandlers;
            this.UpdateStateObject = null;
            this.grabbable = grabbable;
        }

        public GameProperties WireframeProperties { get { return new GameProperties(null, null); } }
    }
}
