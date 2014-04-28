using Accelerated_Delivery_Win;
using BEPUutilities;
using ConversionHelper;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace LD29
{
#if WINDOWS || XBOX
    static class Program
    {
        public static string SavePath { get; private set; }
        public static BoxCutter Cutter { get; private set; }
        public static BaseGame Game { get; private set; }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main(string[] args)
        {
#if !DEBUG
            try
            {
#endif
#if WINDOWS
            SavePath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\LD28\\";
            if(!Directory.Exists(SavePath))
                Directory.CreateDirectory(SavePath);
#endif
            Cutter = new BoxCutter(false, false, SavePath);

            using(Game = new BaseGame())
                Game.Run();
#if !DEBUG
            }
            catch(Exception ex)
            {
                using(CrashDebugGame game = new CrashDebugGame(ex, Cutter))
                    game.Run();
            }
#endif
            Cutter.Close();
        }

        public static bool EpsilonEqual(this BEPUutilities.Vector3 lhs, BEPUutilities.Vector3 rhs)
        {
            float epsilon = 0.0001f;
            return Math.Abs(lhs.X - rhs.X) < epsilon && Math.Abs(lhs.Y - rhs.Y) < epsilon && Math.Abs(lhs.Z - rhs.Z) < epsilon;
        }
        #region BoundingBox extension - Draw()

        public static void Initialize(GraphicsDevice device)
        {
#if DEBUG
            effect = new BasicEffect(device);
            effect.VertexColorEnabled = true;
            effect.LightingEnabled = false;
#endif
        }

#if DEBUG
        static VertexPositionColor[] verts = new VertexPositionColor[8];
        static BasicEffect effect;

        static int[] indices = new int[]
        {
            0, 1,
            1, 2,
            2, 3,
            3, 0,
            0, 4,
            1, 5,
            2, 6,
            3, 7,
            4, 5,
            5, 6,
            6, 7,
            7, 4,
        };

        public static void Draw(this BEPUutilities.BoundingBox box)
        {
            if(RenderingDevice.HiDef)
            {
                effect.View = MathConverter.Convert(Renderer.Camera.ViewMatrix);
                effect.Projection = MathConverter.Convert(Renderer.Camera.ProjectionMatrix);

                BEPUutilities.Vector3[] corners = box.GetCorners();
                for(int i = 0; i < 8; i++)
                {
                    verts[i].Position = corners[i];
                    verts[i].Color = Color.Goldenrod;
                }

                foreach(EffectPass pass in effect.CurrentTechnique.Passes)
                {
                    pass.Apply();
                    Renderer.GraphicsDevice.DrawUserIndexedPrimitives(PrimitiveType.LineList, verts, 0, 8, indices, 0, indices.Length / 2);
                }
            }
        }
#endif
        #endregion
    }
#endif
}

