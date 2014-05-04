using Accelerated_Delivery_Win;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
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
        private List<SoundEffect> sounds;
        private int currentIndex = 0;
        private Rectangle screenRect;

        private float timer = 0;

        public bool Done { get { return currentIndex == frames.Count; } }

        private SoundEffectInstance typingSound;

        public AnimatedTexture(List<Texture2D> frames, List<float> timeInSeconds,
            List<SoundEffect> sounds)
        {
            if(frames.Count != timeInSeconds.Count || frames.Count != sounds.Count || timeInSeconds.Count != sounds.Count)
                throw new ArgumentException("frame, timeInSeconds, and sounds must all be the same length");

            this.frames = frames;
            this.timeInSeconds = timeInSeconds;
            this.sounds = sounds;

            screenRect = new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height);

            Program.Game.Activated += onActivated;
            Program.Game.Deactivated += onDeactivated;
        }

        public void Reset()
        {
            timer = currentIndex = 0;
        }

        protected void onActivated(object sender, EventArgs args)
        {
            if(typingSound != null)
                typingSound.Resume();
        }

        protected void onDeactivated(object sender, EventArgs args)
        {
            if(typingSound != null)
                typingSound.Pause();
        }

        public void Update(GameTime gameTime)
        {
            if(Done)
                return;

            if(currentIndex == 0 && typingSound == null)
            {
                SoundEffectInstance e = sounds[currentIndex].CreateInstance();
                if(sounds[currentIndex] == Program.Game.Loader.AnimationTyping)
                {
                    e.IsLooped = true;
                    typingSound = e;
                }
                e.Play();
            }
            timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            if(timer > timeInSeconds[currentIndex])
            {
                timer = 0;
                currentIndex++;
                if(currentIndex != sounds.Count)
                    if(sounds[currentIndex] != null)
                    {
                        if(typingSound != null)
                        {
                            typingSound.Stop();
                            typingSound = null;
                        }
                        SoundEffectInstance e = sounds[currentIndex].CreateInstance();
                        if(sounds[currentIndex] == Program.Game.Loader.AnimationTyping)
                        {
                            e.IsLooped = true;
                            typingSound = e;
                        }
                        e.Play();
                    }
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
