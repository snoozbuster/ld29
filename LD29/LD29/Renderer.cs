using BEPUphysics;
using BEPUphysics.Entities;
using BEPUphysicsDemos;
using ConversionHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Accelerated_Delivery_Win;

namespace LD29
{
    static class Renderer
    {
        public static Camera Camera { get; private set; }
        public static GraphicsDeviceManager GDM { get; private set; }
        public static GraphicsDevice GraphicsDevice { get { return GDM.GraphicsDevice; } }
        public static bool HiDef { get; private set; }

        /// <summary>
        /// Gets the screen's aspect ratio. Same as graphics.GraphicsDevice.Viewport.AspectRatio.
        /// </summary>
        public static float AspectRatio { get { return GraphicsDevice.Viewport.AspectRatio; } }

        private static Space Space;
        private static Effect shader;

        private static Dictionary<GameModel, bool> models = new Dictionary<GameModel, bool>();
        private static List<ModelData> multiMeshModel = new List<ModelData>();

        public static void Initialize(GraphicsDeviceManager gdm, BaseGame g, Space s, Effect sdr)
        {
            Space = s;
            GDM = gdm;
            OnGDMCreation(sdr);
            shader = sdr;
            HiDef = GDM.GraphicsProfile == GraphicsProfile.HiDef;
            Camera = new Camera(g);
        }

        public static void Clear()
        {
            models.Clear();
            multiMeshModel.Clear();
        }

        public static void Add(GameModel model)
        {
            if(models.ContainsKey(model))
            {
                models[model] = true;
                return;
            }

            foreach(ModelMesh mesh in model.Model.Meshes)
                for(int i = 0; i < mesh.Effects.Count; i++)
                    if(mesh.Effects[i] is BasicEffect)
                        foreach(ModelMeshPart meshPart in mesh.MeshParts)
                            meshPart.Effect = shader.Clone();
            models.Add(model, true);
        }

        public static void Add(Model model, bool transparent = false)
        {
            List<Texture2D> list = new List<Texture2D>();
            foreach(ModelMesh mesh in model.Meshes)
                for(int i = 0; i < mesh.Effects.Count; i++)
                {
                    BasicEffect currentEffect = mesh.Effects[i] as BasicEffect;
                    EffectParameter e;
                    if(currentEffect != null)
                        list.Add(currentEffect.Texture);
                    else if((e = mesh.Effects[i].Parameters.FirstOrDefault(v => { return v.Name == "Texture"; })) != null)
                        list.Add(e.GetValueTexture2D());

                    if(mesh.Effects[i] is BasicEffect)
                        foreach(ModelMeshPart meshPart in mesh.MeshParts)
                            meshPart.Effect = shader.Clone();
                }
            multiMeshModel.Add(new ModelData(model, list, transparent));
        }

        public static void Remove(GameModel model)
        {
            if(models.ContainsKey(model))
                models[model] = false;
        }

        public static void Remove(Model model)
        {
            foreach(ModelData m in multiMeshModel)
                if(m.Model == model)
                    m.Active = false;
        }

        public static void Draw()
        {
            GraphicsDevice.Clear(Color.Black);
            draw(null, MathConverter.Convert(Camera.ViewMatrix));
        }

        private static void draw(Plane? clipPlane, Matrix view)
        {
            List<GameModel> transparentMeshes = new List<GameModel>();

            setForTextured();
            foreach(KeyValuePair<GameModel, bool> m in models)
                if(m.Value && !m.Key.Texture.Wireframe)
                {
                    if(!m.Key.Texture.GraphicsProperties.Transparent)
                        drawMesh(m.Key.Model.Meshes[0], m.Key, "ShadowedScene", clipPlane, view);
                    else
                        transparentMeshes.Add(m.Key);
                }
            foreach(ModelData m in multiMeshModel)
                if(!m.Transparent)
                    drawMultiMesh(m, clipPlane, view);

            setForWireframe();
            foreach(KeyValuePair<GameModel, bool> m in models)
                if(m.Value && m.Key.Texture.Wireframe)
                    drawMesh(m.Key.Model.Meshes[0], m.Key, "ShadowedScene", clipPlane, view);

#if DEBUG
            drawAxes();
            if(Camera.Debug)
                foreach(Entity e in Space.Entities)
                    e.CollisionInformation.BoundingBox.Draw();
#endif

            setForTransparency();
            transparentMeshes.Sort(new Comparison<GameModel>(sortGlassList));
            transparentMeshes.Reverse();
            foreach(GameModel m in transparentMeshes)
                drawMesh(m.Model.Meshes[0], m, "ShadowedScene", clipPlane, view);
            foreach(ModelData m in multiMeshModel)
                if(m.Transparent)
                    drawMultiMesh(m, clipPlane, view);
        }

        private static int sortGlassList(GameModel x, GameModel y)
        {
            Vector3 pos1, pos2;
            pos1 = x.Model.Meshes[0].ParentBone.Transform.Translation;
            pos2 = y.Model.Meshes[0].ParentBone.Transform.Translation;
            float pos1Distance = Vector3.Distance(pos1, RenderingDevice.Camera.Position);
            float pos2Distance = Vector3.Distance(pos2, RenderingDevice.Camera.Position);
            return pos1Distance.CompareTo(pos2Distance);
        }

        private static void drawMultiMesh(ModelData m, Plane? clipPlane, Matrix view)
        {
            int i = 0;
            foreach(ModelMesh mesh in m.Model.Meshes)
            {
                foreach(Effect currentEffect in mesh.Effects)
                {
                    currentEffect.CurrentTechnique = currentEffect.Techniques["ShadowedScene"];

                    currentEffect.Parameters["Texture"].SetValue(m.Textures[i++]);

                    currentEffect.Parameters["xCamerasViewProjection"].SetValue(view * MathConverter.Convert(Camera.ProjectionMatrix));
                    currentEffect.Parameters["xWorld"].SetValue(mesh.ParentBone.Transform);// * Camera.World);
                    currentEffect.Parameters["xPassThroughLighting"].SetValue(true);

                    if(clipPlane.HasValue)
                    {
                        currentEffect.Parameters["xEnableClipping"].SetValue(true);
                        currentEffect.Parameters["xClipPlane"].SetValue(new Vector4(clipPlane.Value.Normal, clipPlane.Value.D));
                    }
                    else
                        currentEffect.Parameters["xEnableClipping"].SetValue(false);
                }
                mesh.Draw();
            }
        }

        private static void drawMesh(ModelMesh mesh, GameModel model, string tech, Plane? clipPlane, Matrix view)
        {
            Matrix entityWorld = ConversionHelper.MathConverter.Convert(model.Entity.CollisionInformation.WorldTransform.Matrix);
            foreach(Effect currentEffect in mesh.Effects)
            {
                currentEffect.CurrentTechnique = currentEffect.Techniques[tech];

                currentEffect.Parameters["Texture"].SetValue(model.Texture.ActualTexture);

                currentEffect.Parameters["xCamerasViewProjection"].SetValue(view * MathConverter.Convert(Camera.ProjectionMatrix));
                currentEffect.Parameters["xWorld"].SetValue(mesh.ParentBone.Transform * model.Transform * entityWorld);// * Camera.World);
                currentEffect.Parameters["xPassThroughLighting"].SetValue(true);
                //currentEffect.Parameters["xLightPos"].SetValue(lights.LightPosition);
                //currentEffect.Parameters["xLightPower"].SetValue(0.4f);
                //currentEffect.Parameters["xAmbient"].SetValue(lights.AmbientPower);
                //currentEffect.Parameters["xLightDir"].SetValue(lights.LightDirection);

                if(clipPlane.HasValue)
                {
                    currentEffect.Parameters["xEnableClipping"].SetValue(true);
                    currentEffect.Parameters["xClipPlane"].SetValue(new Vector4(clipPlane.Value.Normal, clipPlane.Value.D));
                }
                else
                    currentEffect.Parameters["xEnableClipping"].SetValue(false);
            }
            mesh.Draw();
        }

        private static void setForTextured()
        {
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
            GraphicsDevice.SamplerStates[0] = SamplerState.LinearWrap;
            GraphicsDevice.SamplerStates[1] = SamplerState.LinearWrap;
        }

        private static void setForWireframe()
        {
            GraphicsDevice.BlendState = BlendState.Opaque;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            GraphicsDevice.RasterizerState = new RasterizerState() { FillMode = FillMode.WireFrame };
        }

        private static void setForTransparency()
        {
            GraphicsDevice.DepthStencilState = DepthStencilState.DepthRead;
            GraphicsDevice.BlendState = BlendState.AlphaBlend;
            GraphicsDevice.RasterizerState = RasterizerState.CullNone;
        }

        private class ModelData
        {
            public Model Model;
            public List<Texture2D> Textures;
            public bool Active;
            public bool Transparent;

            public ModelData(Model m, List<Texture2D> t, bool transparent)
            {
                Active = true;
                Model = m;
                Transparent = transparent;
                Textures = t;
            }
        }

        /// <summary>
        /// Be careful when you use this.
        /// </summary>
        public static void OnGDMCreation(Effect shader)
        {
#if DEBUG
            vertices = new VertexPositionColor[6];
            vertices[0] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Red);
            vertices[1] = new VertexPositionColor(new Vector3(10000, 0, 0), Color.Red);
            vertices[2] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Green);
            vertices[3] = new VertexPositionColor(new Vector3(0, 10000, 0), Color.Green);
            vertices[4] = new VertexPositionColor(new Vector3(0, 0, 0), Color.Blue);
            vertices[5] = new VertexPositionColor(new Vector3(0, 0, 10000), Color.Blue);
            xyz = new BasicEffect(GraphicsDevice);
            vertexBuff = new VertexBuffer(GraphicsDevice, VertexPositionColor.VertexDeclaration, vertices.Length, BufferUsage.None);
#endif
        }

        #region Debug - Axes
#if DEBUG
        /// <summary>
        /// This is for drawing axes. Handy.
        /// </summary>
        private static VertexPositionColor[] vertices;
        /// <summary>
        /// This is for drawing axes. Handy.
        /// </summary>
        private static VertexBuffer vertexBuff;
        /// <summary>
        /// This is for drawing axes. Handy.
        /// </summary>
        private static BasicEffect xyz;

        private static void drawAxes()
        {
            GraphicsDevice.SetVertexBuffer(vertexBuff);
            xyz.VertexColorEnabled = true;
            xyz.World = Matrix.Identity;
            xyz.View = MathConverter.Convert(Camera.ViewMatrix);
            xyz.Projection = MathConverter.Convert(Camera.ProjectionMatrix);
            xyz.TextureEnabled = false;
            GraphicsDevice.DepthStencilState = DepthStencilState.Default;
            foreach(EffectPass pass in xyz.CurrentTechnique.Passes)
            {
                pass.Apply();
                GraphicsDevice.DrawUserPrimitives(
                    PrimitiveType.LineList, vertices, 0, 3);
            }
        }
#endif
        #endregion
    }
}
