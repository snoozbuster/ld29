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
        public string FriendlyName { get; private set; }

        public GameModel CurrentModel;

        public GameTexture(string name, Texture2D texture, PhysicsProperties pp = new PhysicsProperties(), GameProperties gp = null, GraphicsProperties ggp = new GraphicsProperties())
        {
            FriendlyName = name;
            ActualTexture = texture;
            PhysicsProperties = pp;
            if(gp == null)
                GameProperties = new GameProperties(null, null, null, null, null);
            else
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
                m.Entity.CollisionInformation.Events.InitialCollisionDetected -= oldTex.GameProperties.CollisionHandler;
                m.Entity.CollisionInformation.Events.CollisionEnded -= oldTex.GameProperties.EndCollisionHandler;
                if(m.Space != null)
                    m.Space.DuringForcesUpdateables.Starting -= oldTex.GameProperties.SpaceUpdate;
            }
            m.Entity.CollisionInformation.Events.InitialCollisionDetected += GameProperties.CollisionHandler;
            m.Entity.CollisionInformation.Events.CollisionEnded += GameProperties.EndCollisionHandler;
            if(m.Space != null)
                m.Space.DuringForcesUpdateables.Starting += GameProperties.SpaceUpdate;

            CurrentModel = m;

            GameProperties.OnTextureApplied(m);
        }
    }
}
