using Accelerated_Delivery_Win;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LD29
{
    class AnimatedTexture
    {
        private List<Texture2D> frames;
        private List<float> timeInSeconds;
        private int currentIndex = 0;
        private Rectangle screenRect;

        private float timer = 0;

        public bool Done { get { return currentIndex == frames.Count; } }

        public AnimatedTexture(List<Texture2D> frames, List<float> timeInSeconds)
        {
            if(frames.Count != timeInSeconds.Count)
                throw new ArgumentException("frame and timeInSeconds must be the same length");

            this.frames = frames;
            this.timeInSeconds = timeInSeconds;

            screenRect = new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height);
        }

        public void Reset()
        {
            timer = currentIndex = 0;
        }

        public void Update(GameTime gameTime)
        {
            if(Done)
                return;
            
            timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if(timer > timeInSeconds[currentIndex])
            {
                timer = 0;
                currentIndex++;
            }
        }

        public void Draw()
        {
            RenderingDevice.SpriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null);
            RenderingDevice.SpriteBatch.Draw(frames[Done ? currentIndex - 1 : currentIndex], screenRect, Color.White);
            RenderingDevice.SpriteBatch.End();
        }
    }
}
