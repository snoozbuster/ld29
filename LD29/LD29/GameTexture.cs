using BEPUphysics.Materials;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD29
{
    class GameTexture
    {
        public bool Wireframe;
        public Texture2D ActualTexture { get; private set; }
        public GameProperties GameProperties { get; private set; }
        public GraphicsProperties GraphicsProperties { get; private set; }
        public PhysicsProperties PhysicsProperties { get; private set; }

        public GameTexture(Texture2D texture, PhysicsProperties pp = new PhysicsProperties(), GameProperties gp = new GameProperties(), GraphicsProperties ggp = new GraphicsProperties())
        {
            ActualTexture = texture;
            PhysicsProperties = pp;
            GameProperties = gp;
            GraphicsProperties = ggp;
        }

        public void ApplyToModel(GameModel m, GameTexture oldTex)
        {
            m.Entity.Mass = PhysicsProperties.Mass;
            m.Entity.Material = new Material(PhysicsProperties.StaticFriction, PhysicsProperties.KineticFriction, PhysicsProperties.Bounciness);
            m.Entity.IsAffectedByGravity = PhysicsProperties.IsAffectedByGravity;
            m.Entity.CollisionInformation.CollisionRules.Group = PhysicsProperties.HasCollision ? GameModel.NormalGroup : GameModel.GhostGroup;

            if(oldTex != null)
            {
                foreach(var d in oldTex.GameProperties.CollisionHandlers)
                    m.Entity.CollisionInformation.Events.InitialCollisionDetected -= d;
                if(m.Space != null)
                    m.Space.DuringForcesUpdateables.Starting -= oldTex.GameProperties.Update;
            }
            foreach(var d in GameProperties.CollisionHandlers)
                m.Entity.CollisionInformation.Events.InitialCollisionDetected += d;
            if(m.Space != null)
                m.Space.DuringForcesUpdateables.Starting += GameProperties.Update;
        }
    }
}
