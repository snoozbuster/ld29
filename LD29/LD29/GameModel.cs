using BEPUphysics;
using BEPUphysics.CollisionRuleManagement;
using BEPUphysics.CollisionShapes;
using BEPUphysics.Entities;
using BEPUphysics.Entities.Prefabs;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace LD29
{
    class GameModel : ISpaceObject
    {
        public readonly static CollisionGroup NormalGroup = new CollisionGroup();
        public readonly static CollisionGroup GhostGroup = new CollisionGroup();

        static GameModel()
        {
            CollisionGroup.DefineCollisionRule(NormalGroup, GhostGroup, CollisionRule.NoBroadPhase);
            CollisionGroup.DefineCollisionRule(NormalGroup, NormalGroup, CollisionRule.Normal);
            CollisionGroup.DefineCollisionRule(GhostGroup, GhostGroup, CollisionRule.Normal);
        }
        
        public Entity Entity { get; private set; }
        public Model Model { get; private set; }

        public GameTexture Texture { get { return texture; } private set { value.ApplyToModel(this, this.texture); texture = value; } }
        private GameTexture texture;

        public Vector3 Origin { get; protected set; }

        public readonly bool CanBeRipped;

        public GameModel(Vector3 origin, Model m, GameTexture t, bool rippable = true)
            :this(null, origin, m, t, rippable) { }

        public GameModel(Entity e, Vector3 origin, Model m, GameTexture t, bool rippable = true)
        {
            Model = m;
            texture = t;
            Origin = origin;
            CanBeRipped = rippable;

            Model.Tag = this;

            if(e == null)
            {
                BEPUutilities.Vector3[] verts;
                int[] indices;
                ModelDataExtractor.GetVerticesAndIndicesFromModel(Model, out verts, out indices);

                Entity = new MobileMesh(verts, indices, BEPUutilities.AffineTransform.Identity, MobileMeshSolidity.DoubleSided, Texture.PhysicsProperties.Mass);
            }
            else
                Entity = e;

            Entity.Tag = this;
            Entity.CollisionInformation.Tag = this;

            Entity.Position += ConversionHelper.MathConverter.Convert(Origin);

            Texture.ApplyToModel(this, null); // sets rest of physics/game props
        }

        public GameTexture RipTexture()
        {
            if(Texture.Wireframe)
                throw new InvalidOperationException("Can't rip a texture from a textureless model!");
            if(!CanBeRipped)
                throw new InvalidOperationException("Can't rip an unrippable texture!");

            GameTexture output = Texture;
            Texture = new GameTexture("Wireframe", Texture.ActualTexture) { Wireframe = true };
            return output;
        }

        public void GiveTexture(GameTexture texture)
        {
            if(!Texture.Wireframe)
                throw new InvalidOperationException("Can't give a texture to a textured model!");

            texture.Wireframe = false; // make sure we aren't being dumb
            Texture = texture;
        }

        public void OnAdditionToSpace(Space newSpace)
        {
            newSpace.Add(Entity);
            newSpace.DuringForcesUpdateables.Starting += Texture.GameProperties.Update;
        }

        public void OnRemovalFromSpace(Space oldSpace)
        {
            oldSpace.Remove(Entity);
            oldSpace.DuringForcesUpdateables.Starting -= Texture.GameProperties.Update;
        }

        public Space Space { get; set; }

        public object Tag { get; set; }
    }
}
