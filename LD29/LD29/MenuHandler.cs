using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using Accelerated_Delivery_Win;
using System.IO;
using Microsoft.Win32;

namespace LD29
{
    public static class MenuHandler
    {
        /// <summary>
        /// This is for if you're navigating with the mouse and ditch it for the keyboard. 
        /// Upon pressing a keyboard button, when appropriate, this will toggle and the
        /// game will ignore mouse input until the mouse state changes.
        /// </summary>
        public static bool MouseTempDisabled { get; private set; }

        private static PauseMenu pauseMenu;
        private static MainMenu mainMenu;
        private static GamePadDCMenu disconnectMenu;
        private static EndingMenu gameOverMenu;
        private static ExitMenu exitMenu;
        private static GamePadQueryMenu queryMenu;
        private static EndingMenu endingMenu;
        private static EndingMenu badEndingMenu;
        private static InstructionsMenu instructionsMenu;

        private static Loader loader;

        public static void Create(Loader l)
        {
            loader = l;
            pauseMenu = new PauseMenu();
            mainMenu = new MainMenu();
            disconnectMenu = new GamePadDCMenu();
            exitMenu = new ExitMenu();
            queryMenu = new GamePadQueryMenu();
            instructionsMenu = new InstructionsMenu();
        }

        public static void Draw(GameTime gameTime)
        {
            switch(GameManager.State)
            {
                case GameState.GameOver: gameOverMenu.Draw(gameTime);
                    break;
                case GameState.MainMenu: mainMenu.Draw(gameTime);
                    break;
                case GameState.Paused: pauseMenu.Draw(gameTime);
                    break;
                case GameState.Ending:
                    mainMenu.Draw(gameTime);
                    endingMenu.Draw(gameTime);
                    break;
                case GameState.Paused_DC:
                    switch(GameManager.PreviousState)
                    {
                        case GameState.GameOver: gameOverMenu.Draw(gameTime);
                            break;
                        case GameState.MainMenu: mainMenu.Draw(gameTime);
                            break;
                        case GameState.Paused: pauseMenu.Draw(gameTime);
                            break;
                        case GameState.Exiting: exitMenu.Draw(gameTime);
                            break;
                    }
                    disconnectMenu.Draw(gameTime);
                    break;
                case GameState.Exiting: exitMenu.Draw(gameTime);
                    break;
                case GameState.Paused_PadQuery:
                    switch(GameManager.PreviousState)
                    {
                        case GameState.GameOver: gameOverMenu.Draw(gameTime);
                            break;
                        case GameState.MainMenu: mainMenu.Draw(gameTime);
                            break;
                        case GameState.Paused: pauseMenu.Draw(gameTime);
                            break;
                        case GameState.Exiting: exitMenu.Draw(gameTime);
                            break;
                    }
                    queryMenu.Draw(gameTime);
                    break;
                case GameState.Menuing_Lev: badEndingMenu.Draw(gameTime);
                    break;
                case GameState.Menuing_HiS: instructionsMenu.Draw(gameTime);
                    break;
                default: return;
            }
        }

        public static void Update(GameTime gameTime)
        {
            CheckForMouseMove();

            switch(GameManager.State)
            {
                case GameState.Exiting:
                    exitMenu.Update(gameTime);
                    break;
                case GameState.GameOver:
                    gameOverMenu.Update(gameTime);
                    break;
                case GameState.MainMenu:
                    mainMenu.Update(gameTime);
                    break;
                case GameState.Paused:
                    pauseMenu.Update(gameTime);
                    break;
                case GameState.Paused_DC:
                    disconnectMenu.Update(gameTime);
                    break;
                case GameState.Paused_PadQuery:
                    queryMenu.Update(gameTime);
                    break;
                case GameState.Ending:
                    endingMenu.Update(gameTime);
                    return;
                case GameState.Menuing_Lev:
                    badEndingMenu.Update(gameTime);
                    break;
                case GameState.Menuing_HiS:
                    instructionsMenu.Update(gameTime);
                    break;
            }
        }

        #region Helper methods
        /// <summary>
        /// If the mouse has moved, it becomes enabled.
        /// </summary>
        public static void CheckForMouseMove()
        {
            if(Input.MouseState != Input.MouseLastFrame)
                MouseTempDisabled = false;
        }

        internal static void SaveLoaded()
        {
        }
        #endregion

        #region Menu base
        private abstract class Menu
        {
            /// <summary>
            /// A dictionary of all the controls. The key is the control, and the value is false if not selected, null if selected but
            /// no buttons are down, and true if there's a button down and it's selected.
            /// </summary>
            protected List<MenuControl> controlArray;

            /// <summary>
            /// The currently selected control.
            /// </summary>
            protected MenuControl selectedControl;

            protected bool enterLetGo = false;
            protected bool holdingSelection = false;

            private bool playedClickSound = false;

            /// <summary>
            /// You NEED to set selectedControl in your constructor. Absolutely need. Just set it to
            /// controlArray.ElementAt(0).Key. That's all you have to remember to do. If you don't,
            /// things WILL crash. Boom. Also, set controlArray.IsSelected to null.
            /// </summary>
            protected Menu()
            {
                controlArray = new List<MenuControl>();
            }

            /// <summary>
            /// Calls DetectKeyboardInput and DetectMouseInput and if either is true invokes the selected control.
            /// </summary>
            /// <param name="gameTime">Snapshot of timing values.</param>
            public virtual void Update(GameTime gameTime)
            {
                if(detectKeyboardInput() || detectMouseInput())
                    selectedControl.OnSelect();
            }

            /// <summary>
            /// Draws each of the controls in controlArray.
            /// </summary>
            public virtual void Draw(GameTime gameTime)
            {
                foreach(MenuControl m in controlArray)
                    m.Draw(selectedControl);
            }

            /// <summary>
            /// True denotes do something. False denotes don't.
            /// </summary>
            /// <returns></returns>
            protected virtual bool detectKeyboardInput()
            {
                if((Input.CheckKeyboardJustPressed(Keys.Left) ||
                    Input.CheckXboxJustPressed(Buttons.LeftThumbstickLeft)) && selectedControl.OnLeft != null)
                {
                    MouseTempDisabled = true;
                    selectedControl.IsSelected = false;

                    MenuControl initial = selectedControl;
                    do
                    {
                        selectedControl = selectedControl.OnLeft;
                        if(selectedControl == null)
                        {
                            selectedControl = initial;
                            break;
                        }
                    } while(selectedControl.IsDisabled);
                    //} while(loader.levelArray[controlArray.IndexOf(selectedControl)] == null);

                    if(initial != selectedControl)
                        MediaSystem.PlaySoundEffect(SFXOptions.Button_Rollover);

                    selectedControl.IsSelected = null;
                    return false;
                }
                else if((Input.CheckKeyboardJustPressed(Keys.Right) ||
                         Input.CheckXboxJustPressed(Buttons.LeftThumbstickRight)) && selectedControl.OnRight != null)
                {
                    MouseTempDisabled = true;
                    selectedControl.IsSelected = false;

                    MenuControl initial = selectedControl;

                    do
                    {
                        selectedControl = selectedControl.OnRight;
                        if(selectedControl == null)
                        {
                            selectedControl = initial;
                            break;
                        }
                    } while(selectedControl.IsDisabled);
                    //} while(loader.levelArray[controlArray.IndexOf(selectedControl)] == null);

                    if(initial != selectedControl)
                        MediaSystem.PlaySoundEffect(SFXOptions.Button_Rollover);

                    selectedControl.IsSelected = null;
                    return false;
                }
                else if((Input.CheckKeyboardJustPressed(Keys.Up) ||
                         Input.CheckXboxJustPressed(Buttons.LeftThumbstickUp)) && selectedControl.OnUp != null)
                {
                    MouseTempDisabled = true;
                    selectedControl.IsSelected = false;

                    MenuControl initial = selectedControl;
                    do
                    {
                        if(selectedControl.OnUp == null)
                        {
                            selectedControl = initial;
                            break;
                        }
                        selectedControl = selectedControl.OnUp;
                        if(selectedControl.IsDisabled)
                        {
                            MenuControl initialLeft = selectedControl;
                            do
                            {
                                if(selectedControl.OnLeft == null && selectedControl.OnUp == null)
                                {
                                    selectedControl = initial;
                                    break;
                                }
                                if(selectedControl.OnLeft != null)
                                    selectedControl = selectedControl.OnLeft;
                                else
                                    break; // this'll get us out of all the loops
                                if(initialLeft == selectedControl) // we've looped, time to move on
                                    break;
                            } while(selectedControl.IsDisabled);
                        }
                    } while(selectedControl.IsDisabled);
                    //} while(loader.levelArray[controlArray.IndexOf(selectedControl)] == null);

                    if(initial != selectedControl)
                        MediaSystem.PlaySoundEffect(SFXOptions.Button_Rollover);

                    selectedControl.IsSelected = null;
                    return false;
                }
                else if((Input.CheckKeyboardJustPressed(Keys.Down) ||
                         Input.CheckXboxJustPressed(Buttons.LeftThumbstickDown)) && selectedControl.OnDown != null)
                {
                    MouseTempDisabled = true;
                    selectedControl.IsSelected = false;
                    MenuControl initial = selectedControl;
                    do
                    {
                        if(selectedControl.OnDown == null)
                        {
                            selectedControl = initial;
                            break;
                        }
                        selectedControl = selectedControl.OnDown;
                        if(selectedControl.IsDisabled)
                        {
                            MenuControl initialLeft = selectedControl;
                            do
                            {
                                if(selectedControl.OnLeft == null && selectedControl.OnDown == null)
                                {
                                    selectedControl = initial;
                                    break;
                                }
                                if(selectedControl.OnLeft != null)
                                    selectedControl = selectedControl.OnLeft;
                                else
                                    break;
                                if(initialLeft == selectedControl) // we've looped, time to move on
                                    break;
                            } while(selectedControl.IsDisabled);
                        }
                    } while(selectedControl.IsDisabled);
                    //} while(loader.levelArray[controlArray.IndexOf(selectedControl)] == null);

                    if(initial != selectedControl)
                        MediaSystem.PlaySoundEffect(SFXOptions.Button_Rollover);

                    selectedControl.IsSelected = null;
                    return false;
                }

                bool? old = selectedControl.IsSelected;

                if(Input.CheckKeyboardJustPressed(Keys.Enter) ||
                    Input.CheckXboxJustPressed(Buttons.A))
                {
                    holdingSelection = true;
                    MouseTempDisabled = true;
                }
                else if(Input.KeyboardState.IsKeyUp(Keys.Enter) &&
                    Input.CurrentPad.IsButtonUp(Buttons.A) && holdingSelection)
                    holdingSelection = false;

                bool buttonDown = holdingSelection && !selectedControl.IsDisabled;
                //loader.levelArray[controlArray.IndexOf(selectedControl)] != null;
                if(buttonDown)
                    MouseTempDisabled = true;

                if(!old.HasValue && buttonDown && MouseTempDisabled)
                {
                    if(!(selectedControl is DropUpMenuControl))
                        MediaSystem.PlaySoundEffect(SFXOptions.Button_Press);
                    selectedControl.IsSelected = true;
                    return false;
                }
                else if(old.HasValue && old.Value && !buttonDown && MouseTempDisabled)
                {
                    MediaSystem.PlaySoundEffect(SFXOptions.Button_Release);
                    selectedControl.IsSelected = null;
                    holdingSelection = false;
                    return true;
                }

                return false;
            }

            /// <summary>
            /// True denotes do something. False denotes don't.
            /// </summary>
            /// <returns></returns>
            protected virtual bool detectMouseInput()
            {
#if WINDOWS
                if(!MouseTempDisabled)
                {
                    if(Input.MouseState.LeftButton == ButtonState.Released)
                        playedClickSound = false;

                    foreach(MenuControl m in controlArray)
                    {
                        bool? old = m.IsSelected;
                        bool? current = m.CheckMouseInput(selectedControl);

                        if(old.HasValue && !old.Value && !current.HasValue)
                        {
                            MediaSystem.PlaySoundEffect(SFXOptions.Button_Rollover);
                            selectedControl = m;
                            return false;
                        }
                        else if(!old.HasValue && current.HasValue && current.Value)
                        {
                            if(!playedClickSound)
                                MediaSystem.PlaySoundEffect(SFXOptions.Button_Press);
                            playedClickSound = true;
                            selectedControl = m;
                            return false;
                        }
                        else if(old.HasValue && old.Value && !current.HasValue && Input.MouseState.LeftButton == ButtonState.Released)
                        {
                            MediaSystem.PlaySoundEffect(SFXOptions.Button_Release);
                            selectedControl = m;
                            return true;
                        }
                    }
                }
#endif
                return false;
            }
        }
        #endregion

        #region Game Pad Disconnected
        private class GamePadDCMenu : Menu
        {
            public GamePadDCMenu()
            { }

            public override void Draw(GameTime gameTime)
            {
#if XBOX
                SimpleMessageBox.ShowMessageBox("Game Pad Disconnected!", "The primary game pad has become disconnected. Please reconnect it to continue.",
                    new string[] { "Okay" }, 0, MessageBoxIcon.Error);
#else
                if(GameManager.PreviousState == GameState.Running)
                    Program.Game.DrawScene(gameTime);

                RenderingDevice.SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.LinearClamp, null, null);

                RenderingDevice.SpriteBatch.Draw(loader.halfBlack, new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height), Color.White);
                RenderingDevice.SpriteBatch.DrawString(loader.BiggerFont, "The gamepad has become disconnected. Please\nreconnect it, or press the Spacebar to\ncontinue using the keyboard.", new Vector2(RenderingDevice.Width, RenderingDevice.Height) * 0.5f, Color.White, 0, loader.BiggerFont.MeasureString("The gamepad has become disconnected. Please\nreconnect it, or press the Spacebar to\ncontinue using the keyboard.") * 0.5f, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);

                RenderingDevice.SpriteBatch.End();
#endif
            }

            public override void Update(GameTime gameTime)
            {
                if(Input.CurrentPad.IsConnected)
                {
                    GameManager.State = GameManager.PreviousState;
                    MediaSystem.PlayAll();
                }
#if WINDOWS
                if(Input.CheckKeyboardJustPressed(Keys.Space))
                {
                    Input.ContinueWithKeyboard();
                    MediaSystem.PlayAll();
                }
#endif
            }
        }
        #endregion

        #region Game Pad Query
        private class GamePadQueryMenu : Menu
        {
            public GamePadQueryMenu()
            { }

            public override void Draw(GameTime gameTime)
            {
                if(GameManager.PreviousState == GameState.Running)
                    Program.Game.DrawScene(gameTime);

                RenderingDevice.SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.LinearClamp, null, null);

                RenderingDevice.SpriteBatch.Draw(loader.halfBlack, new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height), Color.White);
                string text = "An Xbox controller has been plugged in. Press\nStart to continue the game using the\ncontroller, or disconnect it to close\nthis prompt.";
                RenderingDevice.SpriteBatch.DrawString(loader.BiggerFont, text, new Vector2(RenderingDevice.Width, RenderingDevice.Height) * 0.5f, Color.White, 0, loader.BiggerFont.MeasureString(text) * 0.5f, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);

                RenderingDevice.SpriteBatch.End();
            }

            public override void Update(GameTime gameTime)
            {
                if(!Input.CurrentPad.IsConnected)
                {
                    GameManager.State = GameManager.PreviousState;
                    MediaSystem.PlayAll();
                }
                if(Input.CheckXboxJustPressed(Buttons.Start) || Input.CheckXboxJustPressed(Buttons.A))
                {
                    Input.ContinueWithPad();
                    MediaSystem.PlayAll();
                }
            }
        }
        #endregion

        #region Pause
        private class PauseMenu : Menu
        {
            private readonly MenuButton resume;
            private bool confirming = false;
            private ConfirmationMenu menu;

            public PauseMenu()
            {
                MenuButton mainMenu, quit;

                resume = new MenuButton(loader.resumeButton, delegate { MediaSystem.PlaySoundEffect(SFXOptions.Pause); GameManager.State = GameState.Running; Program.Game.BGM.Volume = 1; });
                mainMenu = new MenuButton(loader.mainMenuButton, delegate { menu.Reset(); confirming = true; });
                quit = new MenuButton(loader.pauseQuitButton, delegate { GameManager.State = GameState.Exiting; });

                resume.SetDirectionals(null, mainMenu, null, null);
                mainMenu.SetDirectionals(resume, quit, null, null);
                quit.SetDirectionals(mainMenu, null, null, null);

                controlArray.AddRange(new MenuControl[] { resume, mainMenu, quit });
                selectedControl = resume;
                selectedControl.IsSelected = null;

                menu = new ConfirmationMenu("Are you sure you want to return to the main menu?\n                                  All progress will be lost.",
                    delegate { Program.Game.BGM.Volume = 0.5f; GameManager.State = GameState.MainMenu; Program.Game.End(); });
            }

            public override void Draw(GameTime gameTime)
            {
                Program.Game.DrawScene(gameTime);

                RenderingDevice.SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.LinearClamp, null, null);

                RenderingDevice.SpriteBatch.Draw(loader.halfBlack, new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height), Color.White);
                RenderingDevice.SpriteBatch.Draw(loader.mainMenuLogo, new Vector2(RenderingDevice.Width * 0.97f - loader.mainMenuLogo.Width / 2, RenderingDevice.Height * 0.03f - loader.mainMenuLogo.Height / 2), null, Color.White, 0.0f, new Vector2(loader.mainMenuLogo.Width / 2, loader.mainMenuLogo.Height / 2), 0.3f * RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                RenderingDevice.SpriteBatch.DrawString(loader.BiggerFont, "Paused", new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.3f), Color.OrangeRed, 0, loader.BiggerFont.MeasureString("Paused") * 0.5f, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);

                Vector2 stringLength = loader.BiggerFont.MeasureString("Press          to resume");
                Vector2 screenSpot = new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.5f);
                RenderingDevice.SpriteBatch.DrawString(loader.BiggerFont, "Press          to resume", screenSpot, Color.White, 0, stringLength * 0.5f, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                if(Input.ControlScheme == ControlScheme.Keyboard)
                    SymbolWriter.WriteKeyboardIcon(Keys.Escape, screenSpot, new Vector2((stringLength.X * 0.5f + SymbolWriter.IconCenter.X * 0.5f * 0.444f - loader.BiggerFont.MeasureString("Pr").X), SymbolWriter.IconCenter.Y + 10), true);
                else
                    SymbolWriter.WriteXboxIcon(Buttons.Start, screenSpot, new Vector2((stringLength.X * 0.5f + SymbolWriter.IconCenter.X * 0.5f * 0.444f - loader.BiggerFont.MeasureString("Pr").X), SymbolWriter.IconCenter.Y + 10), true);

                base.Draw(gameTime);

                if(confirming)
                    menu.Draw(gameTime);

                RenderingDevice.SpriteBatch.End();
            }

            public override void Update(GameTime gameTime)
            {
                Program.Game.BGM.Volume = 0.5f;
                if(confirming)
                {
                    menu.Update(gameTime);
                    if(menu.Finished)
                    {
                        confirming = false;
                        menu.Reset();
                    }
                    return;
                }

                if(Input.CheckKeyboardJustPressed(Keys.Escape) ||
                    Input.CheckXboxJustPressed(Buttons.Start))
                {
                    GameManager.State = GameState.Running;
                    MediaSystem.PlaySoundEffect(SFXOptions.Pause);
                    selectedControl.IsSelected = false;
                    resume.IsSelected = null;
                    Program.Game.BGM.Volume = 1;
                    return;
                }
                base.Update(gameTime);
            }
        }
        #endregion

        #region Main Menu
        private class MainMenu : Menu
        {
            private float timerInMilliseconds = 0;

            private bool startBeenPressed;
            private int timer = 1200;
            private const int smallerTime = 300;
            private const int largerTime = 750;

            private readonly MenuControl start, instructions;

            public MainMenu()
            {
                MenuButton quit;
                start = new MenuButton(loader.startButton, delegate { GameManager.State = GameState.Running; timerInMilliseconds = 0; Program.Game.Start(); });
                instructions = new MenuButton(loader.instructionsButton, delegate { GameManager.State = GameState.Menuing_HiS; });
                quit = new MenuButton(loader.quitButton, delegate { GameManager.State = GameState.Exiting; });

                start.SetDirectionals(null, instructions, null, null);
                instructions.SetDirectionals(start, quit, null, null);
                quit.SetDirectionals(instructions, null, null, null);
                controlArray.AddRange(new MenuControl[] { instructions, start, quit });

                selectedControl = start;
                selectedControl.IsSelected = null;
            }

            public override void Draw(GameTime gameTime)
            {
                RenderingDevice.GraphicsDevice.Clear(Color.White);
                if(Input.MessagePad == null || (startBeenPressed && timer > 0))
                {
                    int time = startBeenPressed ? smallerTime : largerTime;

                    RenderingDevice.SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.LinearClamp, null, null);
                    RenderingDevice.GraphicsDevice.Clear(Color.White);
                    RenderingDevice.SpriteBatch.Draw(loader.mainMenuBackground, new Vector2(0, 0), null, Color.White, 0, Vector2.Zero, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                    RenderingDevice.SpriteBatch.Draw(loader.mainMenuLogo, new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.15f), null, Color.White, 0.0f, new Vector2(loader.mainMenuLogo.Width, loader.mainMenuLogo.Height) * 0.5f * RenderingDevice.TextureScaleFactor, 0.75f * RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                    if(timerInMilliseconds % time <= time * 0.5f)
                    {
                        Vector2 screenSpot = new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.8f);
                        RenderingDevice.SpriteBatch.Draw(Program.Game.Loader.pressStart, screenSpot, null, Color.White, 0, new Vector2(Program.Game.Loader.pressStart.Width, Program.Game.Loader.pressStart.Height) * 0.5f * RenderingDevice.TextureScaleFactor,
                            RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                    }
                    RenderingDevice.SpriteBatch.End();
                }
                else
                {
                    startBeenPressed = false;
                    RenderingDevice.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.NonPremultiplied, SamplerState.LinearClamp, null, null);
                    RenderingDevice.SpriteBatch.Draw(loader.mainMenuBackground, new Vector2(0, 0), null, Color.White, 0, Vector2.Zero, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                    RenderingDevice.SpriteBatch.Draw(loader.mainMenuLogo, new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.15f), null, Color.White, 0.0f, new Vector2(loader.mainMenuLogo.Width / 2, loader.mainMenuLogo.Height / 2), 0.75f * RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                    base.Draw(gameTime);
                    RenderingDevice.SpriteBatch.End();
                }
            }

            public override void Update(GameTime gameTime)
            {
                timerInMilliseconds += gameTime.ElapsedGameTime.Milliseconds;
                if(startBeenPressed)
                    timer -= gameTime.ElapsedGameTime.Milliseconds;

                if(!Program.Game.Loading)
                {
                    if(Input.MessagePad == null)
                    {
                        #region Player Select and Loading
                        Input.PlayerSelect(Program.Game.IsActive);
                        if(Input.MessagePad != null)
                        {
                            MediaSystem.PlaySoundEffect(SFXOptions.Box_Success);
                            MouseTempDisabled = true;
                            startBeenPressed = true;
                            timer = 1200;
                            timerInMilliseconds %= smallerTime;
                        }
                        #endregion
                    }
                    else if(Input.MessagePad != null && timer <= 0)
                    {
                        if(Input.CheckKeyboardJustPressed(Keys.Escape) || Input.CheckXboxJustPressed(Buttons.B))
                        {
                            ReturnToPressStart();
                            return;
                        }
                        base.Update(gameTime);
                    }
                }
            }

            private void ReturnToPressStart()
            {
                Input.NullMessagePad();
                startBeenPressed = false;
                ResetTimer();
                //MediaSystem.PlaySoundEffect(SFXOptions.Box_Death);
            }

            internal void ResetTimer()
            {
                timerInMilliseconds = 0;
            }
        }
        #endregion

        #region Exiting
        private class ExitMenu : Menu
        {
            private readonly string exitString = "Are you sure you want to exit?";
            private Vector2 exitStringCenter;
            private readonly SpriteFont font;

            public ExitMenu()
            {
                MenuButton yes, no;
                yes = new MenuButton(loader.yesButton, delegate { Program.Game.Exit(); });
                no = new MenuButton(loader.noButton, delegate { GameManager.State = GameManager.PreviousState; });
                yes.SetDirectionals(null, no, null, null);
                no.SetDirectionals(yes, null, null, null);
                selectedControl = yes;
                yes.IsSelected = null;

                controlArray.AddRange(new MenuControl[] { yes, no });

                font = loader.BiggerFont;
                exitStringCenter = font.MeasureString(exitString) * 0.5f;
            }

            public override void Draw(GameTime gameTime)
            {
                if(GameManager.PreviousState == GameState.MainMenu)
                    mainMenu.Draw(gameTime);
                else
                    Program.Game.DrawScene(gameTime);

                RenderingDevice.SpriteBatch.Begin(SpriteSortMode.Immediate, null, SamplerState.LinearClamp, null, null);

                RenderingDevice.SpriteBatch.Draw(loader.halfBlack, new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height), Color.White);
                RenderingDevice.SpriteBatch.Draw(loader.mainMenuLogo, new Vector2(RenderingDevice.Width * 0.97f - loader.mainMenuLogo.Width / 2, RenderingDevice.Height * 0.03f - loader.mainMenuLogo.Height / 2), null, Color.White, 0.0f, new Vector2(loader.mainMenuLogo.Width / 2, loader.mainMenuLogo.Height / 2), 0.3f * RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                RenderingDevice.SpriteBatch.DrawString(font, exitString, new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.3f), Color.White, 0.0f, exitStringCenter, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                base.Draw(gameTime);

                RenderingDevice.SpriteBatch.End();
            }
        }
        #endregion

        #region Instructions
        private class InstructionsMenu : Menu
        {
            private Texture2D instructions_PC;
            private Texture2D instructions_Xbox;

            public InstructionsMenu()
            {
                instructions_PC = loader.Instructions_PC;
                instructions_Xbox = loader.Instructions_Xbox;
            }

            public override void Update(GameTime gameTime)
            {
                if(Input.CheckKeyboardPress(Keys.Enter) || Input.CheckKeyboardPress(Keys.Escape) || Input.CheckXboxPress(Buttons.A) || Input.CheckXboxPress(Buttons.Start)
                    || Input.CheckMouseJustClicked(Program.Game.IsActive))
                    GameManager.State = GameState.MainMenu;
            }

            public override void Draw(GameTime gameTime)
            {
                RenderingDevice.SpriteBatch.Begin();
                if(Input.ControlScheme == ControlScheme.XboxController)
                    RenderingDevice.SpriteBatch.Draw(instructions_Xbox, Vector2.Zero, Color.White);
                else if(Input.ControlScheme == ControlScheme.Keyboard)
                    RenderingDevice.SpriteBatch.Draw(instructions_PC, Vector2.Zero, Color.White);
                RenderingDevice.SpriteBatch.End();
            }
        }
        #endregion

        #region Ending
        private class EndingMenu : Menu
        {
            //protected Sprite[] credits;
            //protected float alpha;
            protected float time;
            //protected bool moved;
            //protected bool fadingOut;

            protected float screenAlpha = 255;

            protected const float deltaA = 3;
            protected const float secondsToPause = 3;

            //protected Texture2D gradient;

            //protected Rectangle topR;
            //protected Rectangle bottomR;

            protected Video end;

            protected VideoPlayer player;

            public EndingMenu(Video end)
            {
                this.end = end;
                //credits = loader.Credits;
                onGDMReset(this, EventArgs.Empty);
                RenderingDevice.GDM.DeviceReset += onGDMReset;
            }

            private void onGDMReset(object sender, EventArgs e)
            {
                //gradient = new Texture2D(RenderingDevice.GraphicsDevice, 1, 20);
                //Color[] colors = new Color[20];
                //for(int i = 0; i < 5; i++)
                //    colors[i] = new Color(0, 0, 0, 255);
                //for(int i = 5; i < 20; i++)
                //    colors[i] = new Color(0, 0, 0, 255 - ((255 / 15) * (i - 4)));
                //gradient.SetData(colors);

                player = new VideoPlayer();
                player.Play(end);
                player.Pause();
            }

            public override void Update(GameTime gameTime)
            {
                //if(!credits[0].Moving && !moved)
                //{
                //    moved = true;
                //    credits[0].Move(new Vector2(0, -0.55f), 115);
                //    credits[1].Move(new Vector2(0, -0.55f), 115);
                //    topR = new Rectangle(0, 0, (int)RenderingDevice.Width, 20);
                //    bottomR = new Rectangle(0, (int)RenderingDevice.Height - 20, (int)RenderingDevice.Width, 20);
                //}

                //for(int i = 0; i < credits.Length - 1; i++)
                //    credits[i].ForceMoveUpdate(gameTime);

                //if(!credits[0].Moving)
                //{
                //    if(alpha + deltaA > 255 && !fadingOut)
                //    {
                //        alpha = 255;
                //        time += (float)gameTime.ElapsedGameTime.TotalSeconds;
                //        if(time >= secondsToPause)
                //        {
                //            fadingOut = true;
                //            time = 0;
                //        }
                //    }
                //    else if(alpha - deltaA < 0 && fadingOut)
                //    {
                //        alpha = 0;
                if(Program.Game.BGM.State == SoundState.Playing)
                    Program.Game.BGM.Pause();

                if(player.State == MediaState.Paused)
                {
                    player.Resume();
                    using(RegistryKey k1 = Registry.CurrentUser.OpenSubKey("Software", true))
                    using(RegistryKey k2 = k1.CreateSubKey("LD28_Travesty"))
                        k2.SetValue("ending", (int)GameManager.State);
                }
                else if(player.State == MediaState.Stopped)
                {
                    mainMenu.Update(gameTime);
                    time += (float)gameTime.ElapsedGameTime.TotalSeconds;
                    if(time >= secondsToPause)
                    {
                        if(screenAlpha - deltaA < 0) // black background
                        {
                            Program.Game.BGM.Resume();
                            GameManager.State = GameState.MainMenu;
                            Reset();
                        }
                        else
                            screenAlpha -= deltaA;
                    }
                }
                //    }
                //    else
                //    {
                //        alpha += deltaA * (fadingOut ? -1 : 1);
                //    }
                //}
            }

            public override void Draw(GameTime gameTime)
            {
                RenderingDevice.SpriteBatch.Begin();

                RenderingDevice.SpriteBatch.Draw(loader.EmptyTex, new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height),
                    new Color(0, 0, 0, screenAlpha) * (screenAlpha / 255f));
                //for(int i = 0; i < credits.Length - 1; i++)
                //    credits[i].Draw();
                //credits[credits.Length - 1].Draw(new Color(255, 255, 255, alpha) * (alpha / 255f));

                //Color whiteWithScreenAlpha = new Color(255, 255, 255, screenAlpha) * (screenAlpha / 255f);
                //RenderingDevice.SpriteBatch.Draw(gradient, topR, null, whiteWithScreenAlpha, 0, Vector2.Zero, SpriteEffects.None, 0);
                //RenderingDevice.SpriteBatch.Draw(gradient, bottomR, null, whiteWithScreenAlpha, 0, Vector2.Zero, SpriteEffects.FlipVertically, 0);

                if(player.State == MediaState.Playing)
                    RenderingDevice.SpriteBatch.Draw(player.GetTexture(), new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height), Color.White);

                RenderingDevice.SpriteBatch.End();
            }

            protected void Reset()
            {
                onGDMReset(this, EventArgs.Empty);
            }
        }
        #endregion

        #region Confirmation Menu
        private class ConfirmationMenu : Menu
        {
            /// <summary>
            /// This will be true if the user selects no.
            /// </summary>
            public bool Finished { get; private set; }

            private readonly Vector2 confirmStringCenter;
            private SpriteFont font { get { return loader.BiggerFont; } }
            private readonly string confirmString;

            /// <summary>
            /// Creates a confirmation menu.
            /// </summary>
            /// <param name="confirmationString">The string to prompt the user with.</param>
            /// <param name="onYes">The delegate to perform if the user selects yes.</param>
            public ConfirmationMenu(string confirmationString, Action onYes)
            {
                MenuButton yes, no;
                yes = new MenuButton(loader.yesButton, onYes + delegate { Finished = true; });
                no = new MenuButton(loader.noButton, delegate { Finished = true; });
                yes.SetDirectionals(null, no, null, null);
                no.SetDirectionals(yes, null, null, null);
                selectedControl = no;
                no.IsSelected = null;

                controlArray.AddRange(new MenuControl[] { yes, no });

                confirmString = confirmationString;
                confirmStringCenter = font.MeasureString(confirmString) * 0.5f;
            }

            public override void Draw(GameTime gameTime)
            {
                RenderingDevice.SpriteBatch.Draw(loader.halfBlack, new Rectangle(0, 0, (int)RenderingDevice.Width, (int)RenderingDevice.Height), Color.White);
                RenderingDevice.SpriteBatch.DrawString(font, confirmString, new Vector2(RenderingDevice.Width * 0.5f, RenderingDevice.Height * 0.3f), Color.White, 0.0f, confirmStringCenter, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                base.Draw(gameTime);
            }

            public void Reset()
            {
                Finished = false;
                selectedControl = controlArray[1];
                controlArray[1].IsSelected = null;
                controlArray[0].IsSelected = false;
            }
        }
        #endregion

        #region Controls
        private class DropUpMenuControl : MenuControl
        {
            protected readonly List<MenuButton> controls = new List<MenuButton>();
            protected const float dropTime = 1.1f;
            protected const int deltaA = 4;

            public bool IsActive { get; private set; }
            protected bool isFading;
            protected int alpha;

            protected Vector2 lowerRight { get { return Texture.LowerRight; } }
            protected Vector2 upperLeft { get { if(controls.Count > 0) return controls[controls.Count - 1].Texture.UpperLeft; return Texture.UpperLeft; } }

            protected Pointer<MenuControl> onLeft, onUp, onRight, onDown;
            protected MenuControl selectedLastFrame;
            protected MenuControl selectedLastFrameMouse;
            protected bool playedClickSound = false;

            public bool MouseWithin { get { return Input.CheckMouseWithinCoords(upperLeft, lowerRight); } }

            public override bool? IsSelected
            {
                get { return base.IsSelected; }
                set
                {
                    if((value == null && IsActive && MouseTempDisabled) || // we need to close
                        (!MouseTempDisabled && IsActive && value.HasValue && value == false))
                    {
                        moveTextures(false);
                        IsActive = false;
                    }
                    else if(value == null && !IsActive)
                        this.invoke();
                    else if(value.HasValue && value.Value)
                        value = null;
                    base.IsSelected = value;
                }
            }

            public override MenuControl OnUp
            {
                get
                {
                    MouseTempDisabled = true;
                    if(IsActive && !isFading)
                        return controls[0];
                    else if(!IsActive)
                        this.invoke();
                    return null;
                }
                protected set { onUp.Value = value; }
            }

            public override MenuControl OnDown
            {
                get
                {
                    if(IsActive)
                    {
                        moveTextures(false);
                        IsActive = false;
                    }
                    if(onDown != null)
                        return onDown.Value;
                    return null;
                }
                protected set { onDown.Value = value; }
            }
            public override MenuControl OnLeft
            {
                get
                {
                    if(IsActive)
                    {
                        moveTextures(false);
                        IsActive = false;
                    }
                    if(onLeft != null)
                        return onLeft.Value;
                    return null;
                }
                protected set { onLeft.Value = value; }
            }
            public override MenuControl OnRight
            {
                get
                {
                    if(IsActive)
                    {
                        moveTextures(false);
                        IsActive = false;
                    }
                    if(onRight != null)
                        return onRight.Value;
                    return null;
                }
                protected set { onRight.Value = value; }
            }

            public override Action OnSelect
            {
                get
                {
                    if(!IsActive)
                        return this.invoke;
                    else return delegate
                    {
                        IsActive = false;
                        moveTextures(false);
                    };
                }
                protected set { }
            }

            public DropUpMenuControl(Sprite texture)
            {
                Texture = texture;
                this.HelpfulText = String.Empty;
                IsSelected = false;
            }

            public DropUpMenuControl(Sprite texture, string helpfulText)
                : this(texture)
            {
                HelpfulText = helpfulText;
            }

            /// <summary>
            /// The first will be the closest to the parent, and so on.
            /// </summary>
            /// <param name="controls">They don't need to have their directionals set. They should also have the same
            /// original position and HelpfulText (if applicable) as the parent.</param>
            public void SetInternalMenu(IList<MenuButton> controls)
            {
                this.controls.Clear();
                this.controls.AddRange(controls);
                for(int i = 0; i < controls.Count - 1; i++)
                    this.controls[i].SetDirectionals(null, null, this.controls[i + 1], (i == 0 ? this as MenuControl : this.controls[i - 1]));
                this.controls[this.controls.Count - 1].SetDirectionals(null, null, null, this.controls[this.controls.Count - 2]);
            }

            /// <summary>
            /// Updates the control and its children. Automatically performs necessary mouse input for its children
            /// (the controls' own CheckMouseInput must still be called for it to update itself).
            /// </summary>
            /// <param name="selected">The currently selected control.</param>
            /// <param name="gameTime">The gameTime.</param>
            /// <returns>A reference to one of its children if one is selected and null if it is selected.</returns>
            public MenuControl Update(MenuControl selected, GameTime gameTime)
            {
                if(isFading)
                {
                    if(!IsActive)
                    {
                        alpha -= deltaA;
                        if(alpha < 0)
                        {
                            alpha = 0;
                            isFading = false;
                        }
                    }
                    else
                    {
                        alpha += deltaA;
                        if(alpha > 255)
                        {
                            alpha = 255;
                            isFading = false;
                        }
                    }
                }

                bool selectedWasChanged = false;

                if(IsActive || isFading)
                    foreach(MenuControl c in controls)
                        c.Texture.ForceMoveUpdate(gameTime);
                if(IsActive && alpha >= 215)
                {
                    foreach(MenuButton m in controls)
                    {
                        if(MouseTempDisabled)
                            break;

                        bool? old = m.IsSelected;
                        bool? current = m.CheckMouseInput(selected);
                        if((current == null || (current.HasValue && current.Value)) && m != selectedLastFrameMouse)
                        {
                            selected = m;
                            selectedLastFrameMouse = m;
                            selectedWasChanged = true;
                            current = m.CheckMouseInput(selected);
                        }

                        if(old.HasValue && !old.Value && !current.HasValue)
                            MediaSystem.PlaySoundEffect(SFXOptions.Button_Rollover);
                        else if(!old.HasValue && current.HasValue && current.Value)
                        {
                            if(!playedClickSound)
                                MediaSystem.PlaySoundEffect(SFXOptions.Button_Press);
                            playedClickSound = true;
                        }
                        else if(old.HasValue && old.Value && !current.HasValue && Input.MouseState.LeftButton == ButtonState.Released)
                        {
                            MediaSystem.PlaySoundEffect(SFXOptions.Button_Release);
                            m.OnSelect();
                            playedClickSound = false;
                            return null; // necessary?
                        }
                        else if(Input.MouseState.LeftButton == ButtonState.Released)
                            playedClickSound = false;
                    }
                }
                else if(!IsActive)
                {
                    playedClickSound = false;
                    foreach(MenuButton b in controls)
                        b.IsSelected = false;
                    if(selected == this || controls.Contains(selected))
                    {
                        base.IsSelected = null;
                        return this;
                    }
                }

                if(selectedWasChanged)
                    return selected;

                foreach(MenuControl c in controls)
                    if(!c.IsSelected.HasValue || (c.IsSelected.HasValue && c.IsSelected.Value))
                    {
                        selectedLastFrameMouse = c;
                        return c;
                    }
                return null;
            }

            public override void Draw(MenuControl selected)
            {
                if(IsActive || isFading)
                    foreach(MenuControl c in controls)
                        c.Draw(selected, new Color(255, 255, 255, alpha));
                base.Draw(selected);
            }

            public void SetPointerDirectionals(Pointer<MenuControl> left, Pointer<MenuControl> right,
                Pointer<MenuControl> up, Pointer<MenuControl> down)
            {
                onLeft = left;
                onRight = right;
                onUp = up;
                onDown = down;
            }

            public override void SetDirectionals(MenuControl left, MenuControl right, MenuControl up, MenuControl down)
            {
                if(left != null)
                    onLeft = new Pointer<MenuControl>(() => left, v => { });
                else
                    onLeft = null;

                if(right != null)
                    onRight = new Pointer<MenuControl>(() => right, v => { });
                else
                    onRight = null;

                if(up != null)
                    onUp = new Pointer<MenuControl>(() => up, v => { });
                else
                    onUp = null;

                if(down != null)
                    onDown = new Pointer<MenuControl>(() => down, v => { });
                else
                    onDown = null;
            }

            protected void invoke()
            {
                IsActive = true;
                moveTextures(true);
            }

            protected void moveTextures(bool forward)
            {
                isFading = true;
                if(forward)
                {
                    float offset = Texture.Height * RenderingDevice.TextureScaleFactor.Y * 0.1f; // tenth of texture height
                    Vector2 temp = (controls[0].Texture.Point == Sprite.RenderPoint.Center ? Texture.Center : Texture.UpperLeft) - new Vector2(0, offset + controls[0].Texture.Height);
                    controls[0].Texture.MoveTo(temp, dropTime);
                    for(int i = 1; i < controls.Count; i++)
                    {
                        temp = (controls[i].Texture.Point == Sprite.RenderPoint.Center ? Texture.Center : Texture.UpperLeft) - new Vector2(0, (offset + controls[i - 1].Texture.Height) * (i + 1));
                        controls[i].Texture.MoveTo(temp, dropTime);
                    }
                }
                else
                    foreach(MenuControl c in controls)
                        c.Texture.MoveTo(c.Texture.Point == Sprite.RenderPoint.Center ? Texture.Center : Texture.UpperLeft,
                           dropTime);
            }

            public override bool? CheckMouseInput(MenuControl selected)
            {
                bool eligible = Program.Game.IsActive && !MouseTempDisabled && !IsDisabled;
                if(!eligible)
                    return IsSelected;

                if(!Input.CheckMouseWithinCoords(upperLeft, lowerRight) && IsActive)
                {
                    IsActive = false;
                    moveTextures(false);
                }
                else if(Input.CheckMouseWithinCoords(upperLeft, lowerRight) && !IsActive && alpha > 0)
                {
                    IsActive = true;
                    moveTextures(true);
                }

                bool withinCoords = Input.CheckMouseWithinCoords(Texture);
                if(withinCoords && eligible && !IsActive)// && this != selectedLastFrame)
                {
                    selectedLastFrame.IsSelected = false;
                    base.IsSelected = null;
                    invoke();
                }
                else if((withinCoords && IsActive) || this == selected)
                    base.IsSelected = null;
                else if(!withinCoords && IsActive)
                    base.IsSelected = false;
                else
                    base.IsSelected = false;

                selectedLastFrame = selected;
                return IsSelected;
            }

            public void SetNewPosition(Vector2 pos)
            {
                Texture.TeleportTo(pos);
                foreach(MenuControl c in controls)
                    c.Texture.TeleportTo(pos);
            }
        }

        private class DualValueControl<T> : GreedyControl<T>
        {
            protected T value1, value2;

            protected Action drawV1, drawV2;

            /// <summary>
            /// Creates a control that can only hold two pre-provided values.
            /// </summary>
            /// <param name="t">The texture to use.</param>
            /// <param name="text"></param>
            /// <param name="textV"></param>
            /// <param name="font"></param>
            /// <param name="variable"></param>
            /// <param name="value1"></param>
            /// <param name="value2"></param>
            /// <param name="drawValue1">The function to execute if the value of the variable is value1.</param>
            /// <param name="drawValue2">The function to execute if the value of the variable is value2.</param>
            public DualValueControl(Sprite t, string text, Vector2 textV, FontDelegate font, Pointer<T> variable,
                T value1, T value2, Action drawValue1, Action drawValue2)
                : base(variable, t, text, textV, font)
            {
                this.value1 = value1;
                this.value2 = value2;
                drawV1 = drawValue1;
                drawV2 = drawValue2;
                HelpfulText = "Press %s% or click the box to toggle between values.";
            }

            public override void Draw(MenuControl selectedControl)
            {
                if(variable.Value.Equals(value1))
                    drawV1();
                else
                    drawV2();
                base.Draw(selectedControl);
            }

            protected override void invoke()
            {
                if(IsSelected.HasValue && IsSelected.Value)
                    return; // cause we're still holding the button down.

                if(variable.Value.Equals(value1))
                    variable.Value = value2;
                else
                    variable.Value = value1;
                IsActive = false;
            }
        }

        #region TabControl
        //private class TabControl : MenuControl
        //{
        //    private readonly Color darkenFactor = new Color(158, 158, 158);

        //    public MenuControl[] Controls { get; private set; }
        //    private bool darken = true;

        //    public TabControl(SuperTextor tabTexture, string tooltip)
        //        : base(tabTexture, tooltip, delegate { })
        //    {
        //        HelpfulText = "Use %lr% or rollover the desired tab to toggle between them.";
        //    }

        //    public void SetData(MenuControl[] controls)
        //    {
        //        Controls = controls;
        //    }

        //    public bool? CheckMouseInput(MenuControl selected, TabControl currentTab)
        //    {
        //        darken = !(this == currentTab);
        //        return base.CheckMouseInput(selected);
        //    }

        //    public override bool? CheckMouseInput(MenuControl selected)
        //    {
        //        throw new NotImplementedException("This method is not supported by TabControl.");
        //    }

        //    public override void Draw(MenuControl selected)
        //    {
        //        if(!IsSelected.HasValue || (IsSelected.HasValue && IsSelected.Value))
        //            Texture.Draw(darken ? Color.Lerp(SelectionTint, darkenFactor, 1) : SelectionTint);
        //        else
        //            Texture.Draw(darken ? darkenFactor : Color.White);
        //    }
        //}
        #endregion

        /// <summary>
        /// This technically isn't a GreedyControl because it doesn't command attention, but oh well.
        /// </summary>
        private class ToggleControl : GreedyControl<bool>
        {
            private readonly Sprite checkmark;
            private Action optionalAction;

            public ToggleControl(Sprite checkmark, Sprite border, Vector2 textV, string text, FontDelegate font,
                Pointer<bool> variable, string helpfulText)
                : base(variable, border, text, textV, font)
            {
                this.checkmark = checkmark;
                HelpfulText = helpfulText;
            }

            protected override void invoke()
            {
                if((IsSelected.HasValue && IsSelected.Value) || IsDisabled)
                    return; // cause we're still holding the button down.

                IsActive = false;
                variable.Value = !variable.Value;

                if(optionalAction != null)
                    optionalAction();
            }

            public void SetAction(Action a)
            {
                optionalAction = a;
            }

            public override void Draw(MenuControl selected)
            {
                base.Draw(selected);
                if(variable.Value)
                    checkmark.Draw();
            }

            public override MenuControl OnDown { get { if(onDown != null) return onDown.Value; return null; } protected set { if(onDown != null) onDown.Value = value; onDown = new Pointer<MenuControl>(() => value, v => { value = v; }); OnDown = value; } }
            protected Pointer<MenuControl> onDown;
            public override MenuControl OnUp { get { if(onUp != null) return onUp.Value; return null; } protected set { if(onUp != null) onUp.Value = value; onUp = new Pointer<MenuControl>(() => value, v => { value = v; }); OnUp = value; } }
            protected Pointer<MenuControl> onUp;
            public override MenuControl OnLeft { get { if(onLeft != null) return onLeft.Value; return null; } protected set { if(onLeft != null) onLeft.Value = value; onLeft = new Pointer<MenuControl>(() => value, v => { value = v; }); OnLeft = value; } }
            protected Pointer<MenuControl> onLeft;
            public override MenuControl OnRight { get { if(onRight != null) return onRight.Value; return null; } protected set { if(onRight != null) onRight.Value = value; onRight = new Pointer<MenuControl>(() => value, v => { value = v; }); OnRight = value; } }
            protected Pointer<MenuControl> onRight;
            /// <summary>
            /// Call this instead of the other one.
            /// </summary>
            /// <param name="onL"></param>
            /// <param name="onR"></param>
            /// <param name="onU"></param>
            /// <param name="onD"></param>
            public void SetPointerDirectionals(Pointer<MenuControl> onL, Pointer<MenuControl> onR, Pointer<MenuControl> onU, Pointer<MenuControl> onD)
            {
                onLeft = onL;
                onRight = onR;
                onUp = onU;
                onDown = onD;
            }

            public override void SetDirectionals(MenuControl left, MenuControl right, MenuControl up, MenuControl down)
            {
                throw new NotSupportedException("Cannot be called on this object.");
            }
        }

        #region WindowBox
        //private class WindowBox : GreedyControl<Enum>
        //{
        //    private readonly Color darkenFactor = new Color(158, 158, 158);

        //    private readonly Texture2D internalTexture;
        //    private readonly SuperTextor leftArrow, rightArrow;
        //    private Rectangle frameWindow;
        //    private readonly int numberOfFrames;
        //    private readonly Vector2 framePos;
        //    private readonly int baseWidth;

        //    private const int deltaX = 5;

        //    private bool waiting;
        //    private bool drawRightDown;
        //    private bool drawLeftDown;
        //    private bool drawArrowLighter;

        //    private bool justInvoked = true;

        //    private bool goingLeft, goingRight;

        //    private readonly List<Enum> values;

        //    /// <summary>
        //    /// If this is true, you need to feed this control invocations.
        //    /// </summary>
        //    public override bool IsActive { get { return goingLeft || goingRight || waiting; } protected set { waiting = false; goingRight = false; goingLeft = false; justInvoked = true; } }

        //    public WindowBox(SuperTextor frameTex, SuperTextor leftArrow, SuperTextor rightArrow, Texture2D internalTex, Vector2 windowVector, Rectangle window, Vector2 textVector, string text, SpriteFont font,
        //        Pointer<Enum> variable, int numValues, string tooltip)
        //        : base(variable, frameTex, text, textVector, font, tooltip)
        //    {
        //        internalTexture = internalTex;
        //        frameWindow = window;
        //        framePos = windowVector;
        //        this.leftArrow = leftArrow;
        //        this.rightArrow = rightArrow;
        //        baseWidth = frameWindow.Width;
        //        numberOfFrames = numValues;

        //        if(leftArrow.UpperLeft.X > upperLeft.X)
        //            upperLeft.X = leftArrow.UpperLeft.X;
        //        if(rightArrow.UpperLeft.X > upperLeft.X)
        //            upperLeft.X = leftArrow.UpperLeft.X;
        //        if(leftArrow.UpperLeft.Y > upperLeft.Y)
        //            upperLeft.Y = leftArrow.UpperLeft.Y;
        //        if(rightArrow.UpperLeft.Y > upperLeft.Y)
        //            upperLeft.Y = rightArrow.UpperLeft.Y;

        //        values = new List<Enum>();

        //        foreach(object o in Enum.GetValues(variable.Value.GetType()))
        //            values.Add((Enum)o);

        //        HelpfulText = "Press %s% and use %lr% to adjust, then press %s% again to confirm, or click the arrows with the mouse.";
        //    }

        //    protected override void invoke()
        //    {
        //        if(justInvoked)
        //        {
        //            waiting = true;
        //            justInvoked = false;
        //        }

        //        bool rightDown, leftDown;
        //        rightDown = (Input.KeyboardState.IsKeyDown(Program.Game.Manager.CurrentSaveWindowsOptions.MenuRightKey) || Input.CurrentPad.IsButtonDown(Program.Game.Manager.CurrentSaveXboxOptions.MenuRightKey | Program.Game.Manager.CurrentSaveXboxOptions.CameraRightKey));
        //        leftDown = (Input.KeyboardState.IsKeyDown(Program.Game.Manager.CurrentSaveWindowsOptions.MenuLeftKey) || Input.CurrentPad.IsButtonDown(Program.Game.Manager.CurrentSaveXboxOptions.MenuLeftKey | Program.Game.Manager.CurrentSaveXboxOptions.CameraLeftKey));

        //        if(leftDown && !goingRight)
        //            goingLeft = true;
        //        else if(rightDown && !goingLeft)
        //            goingRight = true;
        //        else if(Input.CheckKeyboardJustPressed(Program.Game.Manager.CurrentSaveWindowsOptions.SelectionKey) || Input.CheckXboxJustPressed(Program.Game.Manager.CurrentSaveXboxOptions.SelectionKey)
        //            && !goingRight && !goingLeft)
        //        {
        //            IsActive = false;
        //            return;
        //        }

        //        if(rightDown && waiting && !(IsSelected.HasValue && !IsSelected.Value))
        //            drawRightDown = drawArrowLighter = true;
        //        else if(!rightDown && waiting && !(IsSelected.HasValue && !IsSelected.Value))
        //        {
        //            drawRightDown = true;
        //            drawArrowLighter = false;
        //        }
        //        else
        //            drawRightDown = drawArrowLighter = false;

        //        if(leftDown && waiting && !(IsSelected.HasValue && !IsSelected.Value))
        //            drawLeftDown = drawArrowLighter = true;
        //        else if(!leftDown && waiting && !(IsSelected.HasValue && !IsSelected.Value))
        //        {
        //            drawLeftDown = true;
        //            drawArrowLighter = false;
        //        }
        //        else
        //            drawLeftDown = drawArrowLighter = false;

        //        if(goingLeft)
        //            frameWindow.X -= deltaX;
        //        else if(goingRight)
        //            frameWindow.X += deltaX;

        //        if(frameWindow.X < 0)
        //        {
        //            frameWindow.X = 0;
        //            IsActive = false;
        //        }
        //        else if(frameWindow.X > baseWidth * (numberOfFrames - 1))
        //        {
        //            frameWindow.X = baseWidth * numberOfFrames;
        //            IsActive = false;
        //        }
        //        if(frameWindow.X % baseWidth == 0 && (goingLeft || goingRight))
        //        {
        //            variable.Value = values[frameWindow.X / baseWidth];
        //            goingLeft = goingRight = false;
        //            loader.RebuildLevelTiming();
        //        }
        //    }

        //    public override void Draw(MenuControl selected)
        //    {
        //        RenderingDevice.SpriteBatch.Draw(internalTexture, framePos, frameWindow, Color.White, 0, Vector2.Zero, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
        //        RenderingDevice.SpriteBatch.DrawString(font, controlText, textVector, Color.White, 0, Vector2.Zero, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
        //        if(IsSelected.HasValue && !IsSelected.Value)
        //            Texture.Draw(darkenFactor);
        //        else if(!IsSelected.HasValue)
        //            Texture.Draw();
        //        else if(IsActive)
        //            Texture.Draw(SelectionTint);
        //        else
        //            Texture.Draw(darkenFactor);

        //        if(drawRightDown && drawArrowLighter)
        //            rightArrow.Draw(SelectionTint);
        //        else if(drawRightDown && !drawArrowLighter)
        //            rightArrow.Draw(Color.Lerp(SelectionTint, darkenFactor, 1));
        //        else if(!drawRightDown && drawArrowLighter)
        //            rightArrow.Draw();
        //        else
        //            rightArrow.Draw(darkenFactor);

        //        if(drawLeftDown && drawArrowLighter)
        //            leftArrow.Draw(SelectionTint);
        //        else if(drawLeftDown && !drawArrowLighter)
        //            leftArrow.Draw(Color.Lerp(SelectionTint, darkenFactor, 1));
        //        else if(!drawLeftDown && drawArrowLighter)
        //            leftArrow.Draw();
        //        else
        //            leftArrow.Draw(darkenFactor);
        //    }

        //    public override MenuControl OnDown { get { if(onDown != null) return onDown.Value; return null; } protected set { if(onDown != null) onDown.Value = value; onDown = new Pointer<MenuControl>(() => value, v => { value = v; }); OnDown = value; } }
        //    protected Pointer<MenuControl> onDown;
        //    public override MenuControl OnUp { get { if(onUp != null) return onUp.Value; return null; } protected set { if(onUp != null) onUp.Value = value; onUp = new Pointer<MenuControl>(() => value, v => { value = v; }); OnUp = value; } }
        //    protected Pointer<MenuControl> onUp;
        //    public override MenuControl OnLeft { get { if(onLeft != null) return onLeft.Value; return null; } protected set { if(onLeft != null) onLeft.Value = value; onLeft = new Pointer<MenuControl>(() => value, v => { value = v; }); OnLeft = value; } }
        //    protected Pointer<MenuControl> onLeft;
        //    public override MenuControl OnRight { get { if(onRight != null) return onRight.Value; return null; } protected set { if(onRight != null) onRight.Value = value; onRight = new Pointer<MenuControl>(() => value, v => { value = v; }); OnRight = value; } }
        //    protected Pointer<MenuControl> onRight;
        //    /// <summary>
        //    /// Call this instead of the other one.
        //    /// </summary>
        //    /// <param name="onL"></param>
        //    /// <param name="onR"></param>
        //    /// <param name="onU"></param>
        //    /// <param name="onD"></param>
        //    public void SetPointerDirectionals(Pointer<MenuControl> onL, Pointer<MenuControl> onR, Pointer<MenuControl> onU, Pointer<MenuControl> onD)
        //    {
        //        onLeft = onL;
        //        onRight = onR;
        //        onUp = onU;
        //        onDown = onD;
        //    }

        //    public override void SetDirectionals(MenuControl left, MenuControl right, MenuControl up, MenuControl down)
        //    {
        //        throw new NotSupportedException("Cannot be called on this object.");
        //    }

        //    public override bool? CheckMouseInput(MenuControl selected)
        //    {
        //        if(Input.CheckMouseWithinCoords(leftArrow))
        //        {
        //            if(Input.MouseState.LeftButton == ButtonState.Pressed && !goingLeft)
        //            {
        //                waiting = false;
        //                goingLeft = true;
        //                drawLeftDown = true;
        //                drawArrowLighter = true;
        //                IsSelected = true; 
        //                justInvoked = true;
        //            }
        //            else
        //            {
        //                drawLeftDown = false;
        //                drawArrowLighter = true;
        //                IsSelected = null; 
        //                justInvoked = true;
        //            }
        //        }
        //        else if(Input.CheckMouseWithinCoords(rightArrow))
        //        {
        //            if(Input.MouseState.LeftButton == ButtonState.Pressed && !goingRight)
        //            {
        //                waiting = false;
        //                justInvoked = true;
        //                goingRight = true;
        //                drawRightDown = true;
        //                drawArrowLighter = true;
        //                IsSelected = true;
        //            }
        //            else
        //            {
        //                drawRightDown = false;
        //                justInvoked = true;
        //                drawArrowLighter = true;
        //                IsSelected = null;
        //            }
        //        }
        //        return IsSelected;
        //    }
        //}
        #endregion

        private class MenuSlider : GreedyControl<float>
        {
            /// <summary>
            /// Background (immobile) part of the slider.
            /// </summary>
            public Sprite BackgroundTexture { get; private set; }

            /// <summary>
            /// If this is true, you should hold everything and call OnSelect(), because the user wants to send input to this slider.
            /// </summary>
            public override bool IsActive { get { return isActive; } protected set { isActive = value; if(!isActive) justInvoked = false; } }

            private readonly float minValue;
            private readonly float maxValue;

            private readonly float distance; // rhsbound - lhsbound
            private readonly float lhsBound; // in screenspace
            private readonly float rhsBound; // in screenspace

            private const int frameLapse = 20; // number of frames to wait until accepting input again
            private const int delta = 5; // amount to advance or devance when using keyboard

            /// <summary>
            /// This is to be set only by the property. PERIOD.
            /// </summary>
            private bool isActive = false;

            private int framesHeld = 0; // number of frames the button has been held down

            // true if using mouse, false if using keyboard/xbox, null if we don't know yet.
            private bool? usingMouse;
            private bool justInvoked;

            private Direction direction = Direction.None;

            private enum Direction
            {
                Left,
                Right,
                None
            }

            /// <summary>
            /// Creates a slider that can be used to modify a floating-point value.
            /// </summary>
            /// <param name="backgroundTexture">The background of the slider (immobile part).</param>
            /// <param name="foreGroundTexture">The foreground of the slider (mobile part). Should be created with the center
            /// as the render point.</param>
            /// <param name="min">The minimum value of the variable.</param>
            /// <param name="max">The maximum value of the variable.</param>
            /// <param name="distance">The distance (in pixels) the slider can travel.</param>
            /// <param name="offset">Offset of distance (in pixels) from backgroundTexture.UpperLeft.X.</param>
            /// <param name="variable">The variable to get and set.</param>
            public MenuSlider(Sprite backgroundTexture, Sprite foreGroundTexture,
                float min, float max, float distance, float offset, string text, Vector2 textVector, FontDelegate font,
                Pointer<float> variable)
                : base(variable, foreGroundTexture, text, textVector, font)
            {
                minValue = min;
                maxValue = max;
                BackgroundTexture = backgroundTexture;
                this.distance = distance;

                lhsBound = Texture.UpperLeft.X + offset;
                rhsBound = lhsBound + distance;

                HelpfulText = "Press %s% and use %lr% to adjust then press %s% again to confirm, or drag with the mouse.";
            }

            protected override void invoke()
            {
                if(IsSelected.HasValue && IsSelected.Value)
                    return; // cause we're still holding the button down.

                IsActive = true;

                if(!usingMouse.HasValue)
                {
                    justInvoked = true;
                    if(Input.CheckKeyboardPress(Keys.Enter) ||
                        Input.CheckXboxPress(Buttons.A))
                        usingMouse = false;
                    else // we got in here with the mouse. Probably.
                        usingMouse = true;
                }
                else
                    justInvoked = false;

                if((Input.MouseState.LeftButton == ButtonState.Released && usingMouse.GetValueOrDefault()) || !Program.Game.IsActive)
                {
                    IsActive = false;
                    return;
                }
                else if((Input.CheckKeyboardPress(Keys.Enter) ||
                        Input.CheckXboxPress(Buttons.A)) && !justInvoked &&
                        !usingMouse.GetValueOrDefault())
                {
                    IsActive = false;
                    return;
                }

                #region variable updates
                if(usingMouse.GetValueOrDefault())
                {
                    float x = Input.MouseState.X;
                    if(x < lhsBound)
                        x = lhsBound;
                    if(x > rhsBound)
                        x = rhsBound;
                    Texture.TeleportTo(new Vector2(x, Texture.Center.Y));
                    variable.Value = ((x - lhsBound) / distance) * (maxValue - minValue);
                }
                else // using keyboard
                {
                    if(direction == Direction.Left)
                    {
                        if(Input.KeyboardState.IsKeyDown(Keys.Left) ||
                           Input.CurrentPad.IsButtonDown(Buttons.LeftThumbstickLeft))
                        {
                            if(framesHeld < frameLapse) // if the button hasn't been held down long enough, increase frames by one and return.
                                framesHeld++;
                            else
                            {
                                framesHeld = 0;
                                variable.Value -= delta;
                                if(variable.Value < minValue)
                                    variable.Value = minValue;
                                Texture.TeleportTo(new Vector2(Texture.Center.Y - delta, Texture.Center.Y));
                            }
                        }
                        else // left is no longer down
                            direction = Direction.None;
                    }
                    else if(direction == Direction.Right)
                    {
                        if(Input.KeyboardState.IsKeyDown(Keys.Right) ||
                              Input.CurrentPad.IsButtonDown(Buttons.LeftThumbstickRight))
                        {
                            if(framesHeld < frameLapse) // if the button hasn't been held down long enough, increase frames by one and return.
                                framesHeld++;
                            else
                            {
                                framesHeld = 0;
                                variable.Value += delta;
                                if(variable.Value > maxValue)
                                    variable.Value = maxValue;
                                Texture.TeleportTo(new Vector2(Texture.Center.Y + delta, Texture.Center.Y));
                            }
                        }
                        else // left is no longer down
                            direction = Direction.None;
                    }
                    else // direction is none
                        framesHeld = 0; // so this can be reset
                }
                #endregion
            }

            public override MenuControl OnDown { get { if(onDown != null) return onDown.Value; return null; } protected set { if(onDown != null) onDown.Value = value; onDown = new Pointer<MenuControl>(() => value, v => { value = v; }); OnDown = value; } }
            protected Pointer<MenuControl> onDown;
            public override MenuControl OnUp { get { if(onUp != null) return onUp.Value; return null; } protected set { if(onUp != null) onUp.Value = value; onUp = new Pointer<MenuControl>(() => value, v => { value = v; }); OnUp = value; } }
            protected Pointer<MenuControl> onUp;
            public override MenuControl OnLeft { get { if(onLeft != null) return onLeft.Value; return null; } protected set { if(onLeft != null) onLeft.Value = value; onLeft = new Pointer<MenuControl>(() => value, v => { value = v; }); OnLeft = value; } }
            protected Pointer<MenuControl> onLeft;
            public override MenuControl OnRight { get { if(onRight != null) return onRight.Value; return null; } protected set { if(onRight != null) onRight.Value = value; onRight = new Pointer<MenuControl>(() => value, v => { value = v; }); OnRight = value; } }
            protected Pointer<MenuControl> onRight;
            /// <summary>
            /// Call this instead of the other one.
            /// </summary>
            /// <param name="onL"></param>
            /// <param name="onR"></param>
            /// <param name="onU"></param>
            /// <param name="onD"></param>
            public void SetPointerDirectionals(Pointer<MenuControl> onL, Pointer<MenuControl> onR, Pointer<MenuControl> onU, Pointer<MenuControl> onD)
            {
                onLeft = onL;
                onRight = onR;
                onUp = onU;
                onDown = onD;
            }

            public override void SetDirectionals(MenuControl left, MenuControl right, MenuControl up, MenuControl down)
            {
                throw new NotSupportedException("Cannot be called on this object.");
            }

            public override void Draw(MenuControl selected)
            {
                base.Draw(selected);
                BackgroundTexture.Draw();
            }
        }

        private class VariableButton : MenuButton
        {
            public override MenuControl OnDown { get { if(onDown != null) return onDown.Value; return null; } protected set { if(onDown != null) onDown.Value = value; onDown = new Pointer<MenuControl>(() => value, v => { value = v; }); OnDown = value; } }
            protected Pointer<MenuControl> onDown;
            public override MenuControl OnUp { get { if(onUp != null) return onUp.Value; return null; } protected set { if(onUp != null) onUp.Value = value; onUp = new Pointer<MenuControl>(() => value, v => { value = v; }); OnUp = value; } }
            protected Pointer<MenuControl> onUp;
            public override MenuControl OnLeft { get { if(onLeft != null) return onLeft.Value; return null; } protected set { if(onLeft != null) onLeft.Value = value; onLeft = new Pointer<MenuControl>(() => value, v => { value = v; }); OnLeft = value; } }
            protected Pointer<MenuControl> onLeft;
            public override MenuControl OnRight { get { if(onRight != null) return onRight.Value; return null; } protected set { if(onRight != null) onRight.Value = value; onRight = new Pointer<MenuControl>(() => value, v => { value = v; }); OnRight = value; } }
            protected Pointer<MenuControl> onRight;

            public VariableButton(Sprite tex, Action a, string tooltip)
                : base(tex, a, tooltip)
            { }

            /// <summary>
            /// Call this instead of the other one.
            /// </summary>
            /// <param name="onL"></param>
            /// <param name="onR"></param>
            /// <param name="onU"></param>
            /// <param name="onD"></param>
            public void SetPointerDirectionals(Pointer<MenuControl> onL, Pointer<MenuControl> onR, Pointer<MenuControl> onU, Pointer<MenuControl> onD)
            {
                onLeft = onL;
                onRight = onR;
                onUp = onU;
                onDown = onD;
            }

            public override void SetDirectionals(MenuControl left, MenuControl right, MenuControl up, MenuControl down)
            {
                throw new InvalidOperationException("Can't call the normal SetDirectionals() on a VariableControl!");
            }
        }

        private class MenuButton : MenuControl
        {
            public MenuButton(Sprite t, Action action)
                : this(t, action, String.Empty)
            { }

            public MenuButton(Sprite t, Action action, string helpfulText)
                : base(t, helpfulText, action)
            { }
        }

        private abstract class GreedyControl<T> : GreedyControl
        {
            protected Pointer<T> variable { get; private set; }
            protected readonly string controlText;
            protected Vector2 textVector;
            protected readonly FontDelegate font;
            protected Vector2 stringLength;
            protected Vector2 relativeScreenSpace;

            public override Action OnSelect { get { return this.invoke; } protected set { } }

            /// <summary>
            /// Creates a generic GreedyControl: a control that requires invocations when it is selected.
            /// </summary>
            /// <param name="variable">The variable to get/set.</param>
            /// <param name="backgroundTex">The background texture of the control.</param>
            /// <param name="text">The control's display text.</param>
            /// <param name="textV">The upper-left corner of the text.</param>
            /// <param name="f">The font to use.</param>
            protected GreedyControl(Pointer<T> variable, Sprite backgroundTex,
                string text, Vector2 textV, FontDelegate f)
            {
                this.variable = variable;
                Texture = backgroundTex;
                font = f;
                controlText = text;
                textVector = textV;
                upperLeft = new Vector2();
                lowerRight = new Vector2();
                upperLeft.X = backgroundTex.UpperLeft.X < textVector.X ? backgroundTex.UpperLeft.X : textVector.X;
                upperLeft.Y = backgroundTex.UpperLeft.Y < textVector.Y ? backgroundTex.UpperLeft.Y : textVector.Y;
                stringLength = font().MeasureString(controlText) * RenderingDevice.TextureScaleFactor;
                lowerRight.X = backgroundTex.LowerRight.X > textVector.X + stringLength.X ? backgroundTex.LowerRight.X : textVector.X + stringLength.X;
                lowerRight.Y = backgroundTex.LowerRight.Y > textVector.Y + stringLength.Y ? backgroundTex.LowerRight.Y : textVector.Y + stringLength.Y;
                IsSelected = false;

                relativeScreenSpace = textVector / new Vector2(RenderingDevice.Width, RenderingDevice.Height);
                Program.Game.Graphics.DeviceReset += GDM_device_reset;
            }

            public override void Draw(MenuControl selected)
            {
                RenderingDevice.SpriteBatch.DrawString(font(), controlText, textVector, IsDisabled ? DisabledTint : textColor, 0, Vector2.Zero, RenderingDevice.TextureScaleFactor, SpriteEffects.None, 0);
                base.Draw(selected);
            }

            protected virtual void GDM_device_reset(object caller, EventArgs e)
            {
                textVector = new Vector2(RenderingDevice.Width, RenderingDevice.Height) * relativeScreenSpace;

                Texture.ForceResize();
                Sprite backgroundTex = Texture;
                stringLength = font().MeasureString(controlText) * RenderingDevice.TextureScaleFactor;

                upperLeft.X = backgroundTex.UpperLeft.X < textVector.X ? backgroundTex.UpperLeft.X : textVector.X;
                upperLeft.Y = backgroundTex.UpperLeft.Y < textVector.Y ? backgroundTex.UpperLeft.Y : textVector.Y;
                lowerRight.X = backgroundTex.LowerRight.X > textVector.X + stringLength.X ? backgroundTex.LowerRight.X : textVector.X + stringLength.X;
                lowerRight.Y = backgroundTex.LowerRight.Y > textVector.Y + stringLength.Y ? backgroundTex.LowerRight.Y : textVector.Y + stringLength.Y;
            }
        }

        /// <summary>
        /// The only purpose for this class is to provide a solution for iterating through GreedyControls
        /// when the generic part doesn't matter.
        /// </summary>
        private abstract class GreedyControl : MenuControl
        {
            protected readonly Color textColor = Color.White;
            protected readonly Color invocationColor = Color.Green;

            /// <summary>
            /// This will be the upper-left of the text's vector and the background texture. Test it against any other applicable textures
            /// in the control.
            /// </summary>
            protected Vector2 upperLeft;
            /// <summary>
            /// This will be the lower-right of the text's vector and the background texture. Test it against any other applicable textures
            /// in the control.
            /// </summary>
            protected Vector2 lowerRight;

            public virtual bool IsActive { get; protected set; }

            protected abstract void invoke();

            /// <summary>
            /// Draws texture differently from MenuControl.Draw(). Does not call MenuControl.Draw(). Draws text.
            /// </summary>
            public override void Draw(MenuControl selected)
            {
                if(IsDisabled)
                {
                    Texture.Draw(DisabledTint);
                    return;
                }
                if(IsSelected.HasValue && IsSelected.Value && !IsActive)
                    Texture.Draw(DownTint);
                else if(!IsSelected.HasValue && !IsActive)
                    Texture.Draw(SelectionTint);
                else if(IsActive && !IsSelected.HasValue)
                    Texture.Draw(invocationColor);
                else
                    Texture.Draw();
            }

            /// <summary>
            /// Checks the mouse within the upper-left and lower-right of the sum of all drawn controls.
            /// </summary>
            /// <param name="selected">The currently selected control.</param>
            /// <returns>True if mouse is held down, null if rolled over or the mouse hasn't rolled over anything else, false if
            /// not selected. Also sets the return value to IsSelected.</returns>
            public override bool? CheckMouseInput(MenuControl selected)
            {
                bool transparent = false;
                if(hasTransparency)
                {
                    Vector2 antiscale = new Vector2(1 / Texture.Scale.X, 1 / Texture.Scale.Y);
                    Color[] pixel = new Color[] { new Color(1, 1, 1, 1) };
                    int relativeX = (int)((Input.MouseState.X - Texture.UpperLeft.X) * antiscale.X) + (Texture.TargetArea == Texture.Texture.Bounds ? 0 : Texture.TargetArea.X);
                    int relativeY = (int)((Input.MouseState.Y - Texture.UpperLeft.Y) * antiscale.Y) + (Texture.TargetArea == Texture.Texture.Bounds ? 0 : Texture.TargetArea.Y);
                    if(relativeX > 0 && relativeY > 0 && relativeX < Texture.Texture.Width && relativeY < Texture.Texture.Height)
                        Texture.Texture.GetData(0, new Rectangle(relativeX, relativeY, 1, 1), pixel, 0, 1);
                    if(pixel[0].A == 0)
                        transparent = true;
                }

                bool withinCoords = Input.CheckMouseWithinCoords(upperLeft, lowerRight) && !transparent;
                bool eligible = Program.Game.IsActive && !MouseTempDisabled && !IsDisabled;
                if(!eligible)
                    return IsSelected;
                if(withinCoords && eligible && Input.MouseState.LeftButton == ButtonState.Pressed)
                    IsSelected = true;
                else if(withinCoords && eligible && Input.MouseState.LeftButton != ButtonState.Pressed)
                    IsSelected = null;
                else if(!withinCoords && this == selected)
                    IsSelected = null;
                else
                    IsSelected = false;
                return IsSelected;
            }
        }

        private abstract class MenuControl
        {
            public static readonly Color SelectionTint = Color.Red;
            public static readonly Color DownTint = new Color(180, 0, 0);
            public static readonly Color DisabledTint = new Color(128, 128, 128, 128);
            /// <summary>
            /// The MenuControl that should be selected when Left is pressed and this is the selected MenuControl.
            /// </summary>
            public virtual MenuControl OnLeft { get; protected set; }
            /// <summary>
            /// The MenuControl that should be selected when Right is pressed and this is the selected MenuControl.
            /// </summary>
            public virtual MenuControl OnRight { get; protected set; }
            /// <summary>
            /// The MenuControl that should be selected when Up is pressed and this is the selected MenuControl.
            /// </summary>
            public virtual MenuControl OnUp { get; protected set; }
            /// <summary>
            /// The MenuControl that should be selected when Down is pressed and this is the selected MenuControl.
            /// </summary>
            public virtual MenuControl OnDown { get; protected set; }
            /// <summary>
            /// An Action defining what should be done when this control is clicked on.
            /// </summary>
            public virtual Action OnSelect { get; protected set; }
            public Sprite Texture { get; protected set; }

            public virtual bool? IsSelected { get; set; }
            public virtual bool IsDisabled { get; set; }

            /// <summary>
            /// This string displays a helpful bit of text about what the control does. %s% means print selection keys, %b% means 
            /// print Escape and Back, %lr% means print left and right icons.
            /// </summary>
            public string HelpfulText { get; protected set; }

            protected bool hasTransparency = false;

            /// <summary>
            /// Creates a new MenuControl.
            /// </summary>
            /// <param name="t">The texture to use.</param>
            /// <param name="onSelect">The action to invoke on selection.</param>
            protected MenuControl(Sprite t, string tooltip, Action onSelect)
            {
                Texture = t;
                OnSelect = onSelect;
                IsSelected = false;
                HelpfulText = tooltip;
            }

            /// <summary>
            /// Only GreedyControl is allowed to call this.
            /// </summary>
            protected MenuControl()
            { }

            /// <summary>
            /// Sets the directionals of the control. I'd recommend calling this.
            /// </summary>
            /// <param name="directionals">
            /// [0] - OnLeft
            /// [1] - OnRight
            /// [2] - OnUp
            /// [3] - OnDown</param>
            public virtual void SetDirectionals(MenuControl left, MenuControl right, MenuControl up, MenuControl down)
            {
                OnLeft = left;
                OnRight = right;
                OnUp = up;
                OnDown = down;
            }

            public void MakeTransparencySensitive()
            {
                hasTransparency = true;
            }

            /// <summary>
            /// Checks for mouse input.
            /// </summary>
            /// <returns>Returns false if nothing, null if rolled over, true if down. The value it returns will
            /// be the same as the one in IsSelected.</returns>
            public virtual bool? CheckMouseInput(MenuControl selected)
            {
                bool transparent = false;
                if(hasTransparency)
                {
                    Vector2 antiscale = new Vector2(1 / Texture.Scale.X, 1 / Texture.Scale.Y);
                    Color[] pixel = new Color[] { new Color(1, 1, 1, 1) };
                    int relativeX = (int)((Input.MouseState.X - Texture.UpperLeft.X) * antiscale.X) + (Texture.TargetArea == Texture.Texture.Bounds ? 0 : Texture.TargetArea.X);
                    int relativeY = (int)((Input.MouseState.Y - Texture.UpperLeft.Y) * antiscale.Y) + (Texture.TargetArea == Texture.Texture.Bounds ? 0 : Texture.TargetArea.Y);
                    if(relativeX > 0 && relativeY > 0 && relativeX < Texture.Texture.Width && relativeY < Texture.Texture.Height)
                        Texture.Texture.GetData(0, new Rectangle(relativeX, relativeY, 1, 1), pixel, 0, 1);
                    if(pixel[0].A == 0)
                        transparent = true;
                }

                bool withinCoords = Input.CheckMouseWithinCoords(Texture) && !transparent;
                bool eligible = Program.Game.IsActive && !MouseTempDisabled && !IsDisabled;
                if(!eligible)
                    return IsSelected;

                if(withinCoords && eligible && Input.MouseState.LeftButton == ButtonState.Pressed && this == selected)
                    IsSelected = true;
                else if(withinCoords && eligible && Input.MouseState.LeftButton != ButtonState.Pressed)
                    IsSelected = null;
                else if(!withinCoords && this == selected)
                    IsSelected = null;
                else
                    IsSelected = false;
                return IsSelected;
            }

            /// <summary>
            /// Draws the control.
            /// </summary>
            public virtual void Draw(MenuControl selected)
            {
                this.Draw(selected, Color.White);
            }

            internal virtual void Draw(MenuControl selected, Color tint)
            {
                if(IsDisabled)
                    Texture.Draw(new Color(new Vector4(DisabledTint.ToVector3(), tint.A / 255f)));
                else if(IsSelected.HasValue && IsSelected.Value)
                    Texture.Draw(new Color(new Vector4(DownTint.ToVector3(), tint.A / 255f)));
                else if(!IsSelected.HasValue)
                    Texture.Draw(new Color(new Vector4(SelectionTint.ToVector3(), tint.A / 255f)));
                else
                    Texture.Draw(tint);
            }
        }
        #endregion
    }
}
