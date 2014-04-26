using Accelerated_Delivery_Win;
using BEPUutilities;
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

        public static bool EpsilonEqual(this Vector3 lhs, Vector3 rhs)
        {
            float epsilon = 0.0001f;
            return Math.Abs(lhs.X - rhs.X) < epsilon && Math.Abs(lhs.Y - rhs.Y) < epsilon && Math.Abs(lhs.Z - rhs.Z) < epsilon;
        }
    }
#endif
}

