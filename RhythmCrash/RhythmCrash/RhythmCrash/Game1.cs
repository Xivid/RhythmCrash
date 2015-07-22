using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.GamerServices;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Media;
using SimpleJSON;

namespace RhythmCrash
{
    /// <summary>
    /// This is the main type for your game
    /// </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        private Texture2D mBackground;
        private Texture2D mRoad;
        private Texture2D mHazard;
        private Texture2D mCar;

        private KeyboardState mPreviousKeyboardState;
        SpriteBatch spriteBatch, signBatch;

        private Vector2 mCarPosition = new Vector2(280, 440);
        private int mMoveCarX = 160;
        private int mVelocityY;
        private int mScore;
        private int mHazardsPerfect, mHazardsGood, mHazardsMiss;
        private int mHazardsCombo;
        private int mCurrentRoadIndex;
        private bool mLastHazardAtLeft;

        private int[] mRoadY = new int[2]; //使用两张路面
        private List<Hazard> mHazards = new List<Hazard>();

        // 定义随机数 - 比方用来表示障碍物的位置
        private Random mRandom = new Random();

        private SpriteFont mFont;


        private bool loadFail = false;
        private int difficulty = 2;
        private int noteCounter = 0;
        private double deltaTime;
        private JSONNode beatmap;
        private JSONArray hitObjects;
        private JSONArray nowObject;
        private Song music;
        private SoundEffect hitSE;

        //----------------------- Feng ---------------------
        // 自定义枚举类型，表明不同的游戏状态
        private enum State
        {
            TitleScreen,      // 初始片头
            Running,
            Paused,
            Success //结束
        }

        //--------------------- Tian --------------------------


        private State mCurrentState = State.TitleScreen;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            //graphics.IsFullScreen = true;
            // 定义游戏窗口大小
            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;
        }

        /// <summary>
        /// Allows the game to perform any initialization it needs to before starting to run.
        /// This is where it can query for any required services and load any non-graphic
        /// related content.  Calling base.Initialize will enumerate through any components
        /// and initialize them as well.
        /// </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        /// <summary>
        /// LoadContent will be called once per game and is the place to load
        /// all of your content.
        /// </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            signBatch = new SpriteBatch(GraphicsDevice);

            mCar = Content.Load<Texture2D>("Images/Car");
            mBackground = Content.Load<Texture2D>("Images/Background");
            mRoad = Content.Load<Texture2D>("Images/Road");
            mHazard = Content.Load<Texture2D>("Images/Hazard");

            // 定义字体
            mFont = Content.Load<SpriteFont>("MyFont");

            // Read beatmap
            StreamReader sr = new StreamReader("../../../../RhythmCrashContent/Music/CroatianRhapsody/beatmap");
            beatmap = JSON.Parse(sr.ReadToEnd());
            switch (difficulty)
            {
                case 0:
                    hitObjects = beatmap["GameObject"]["Easy"].AsArray;
                    break;
                case 1:
                    hitObjects = beatmap["GameObject"]["Normal"].AsArray;
                    break;
                case 2:
                    hitObjects = beatmap["GameObject"]["Hard"].AsArray;
                    break;
                default:
                    return;
            }

            if (beatmap == null || hitObjects == null)
                loadFail = true;

            hitSE = Content.Load<SoundEffect>("Music/CroatianRhapsody/hit");
            if (hitSE == null)
                loadFail = true;

            music = Content.Load<Song>("Music/CroatianRhapsody/CroatianRhapsody");
            if (music == null)
                loadFail = true;

            if (loadFail)
                this.Exit();

            sr.Close();
        }

        /// <summary>
        /// UnloadContent will be called once per game and is the place to unload
        /// all content.
        /// </summary>
        protected override void UnloadContent()
        {
            // TODO: Unload any non ContentManager content here
        }

        protected void StartGame()
        {
            mRoadY[0] = 0;
            mRoadY[1] = -1 * mRoad.Height;

            mScore = 0;
            mHazardsPerfect = mHazardsGood = mHazardsMiss = 0;
            mHazardsCombo = 0;
            mVelocityY = 600; //根据用户所选速度
            mCurrentRoadIndex = 0;  //从第0张路面开始
            mHazards.Clear();

            mCurrentState = State.Running;

            noteCounter = 0;
            nowObject = hitObjects[0].AsArray;

            MediaPlayer.Play(music);
        }

        /// <summary>
        /// Allows the game to run logic such as updating the world,
        /// checking for collisions, gathering input, and playing audio.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState aCurrentKeyboardState = Keyboard.GetState();

            //Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed ||
                aCurrentKeyboardState.IsKeyDown(Keys.Escape) == true)
            {
                this.Exit();
            }

            deltaTime = gameTime.ElapsedGameTime.TotalSeconds;

            switch (mCurrentState)
            {
                case State.TitleScreen:
                case State.Success:
                    {
                        //MediaPlayer.Stop();

                        if (aCurrentKeyboardState.IsKeyDown(Keys.Space) == true && mPreviousKeyboardState.IsKeyDown(Keys.Space) == false)
                        {
                            StartGame();
                        }
                        break;
                    }
                case State.Paused:
                    {
                        if (aCurrentKeyboardState.IsKeyDown(Keys.P) == true && mPreviousKeyboardState.IsKeyDown(Keys.P) == false)
                        {
                            MediaPlayer.Resume();
                            mCurrentState = State.Running;
                        }
                        break;
                    }
                case State.Running:
                    {
                        //pause
                        if (aCurrentKeyboardState.IsKeyDown(Keys.P) == true && mPreviousKeyboardState.IsKeyDown(Keys.P) == false)
                        {
                            MediaPlayer.Pause();
                            mCurrentState = State.Paused;
                            break;
                        }
                        //end

                        if (MediaPlayer.State == MediaState.Stopped || (aCurrentKeyboardState.IsKeyDown(Keys.E) == true && mPreviousKeyboardState.IsKeyDown(Keys.E) == false))
                        {
                            MediaPlayer.Stop();
                            mCurrentState = State.Success;
                            break;
                        }

                        //Change: Use Left and Right arrows to control
                        if ((aCurrentKeyboardState.IsKeyDown(Keys.Left) == true && mPreviousKeyboardState.IsKeyDown(Keys.Left) == false && mMoveCarX < 0)
                            || (aCurrentKeyboardState.IsKeyDown(Keys.Right) == true && mPreviousKeyboardState.IsKeyDown(Keys.Right) == false && mMoveCarX > 0))
                        {
                            mCarPosition.X += mMoveCarX;
                            mMoveCarX *= -1;
                        }
                        
                        ScrollRoad();

                        foreach (Hazard aHazard in mHazards)
                        {
                            if (aHazard.Visible == true)
                            {
                                CheckCollision(aHazard);
                                MoveHazard(aHazard);
                            }                          
                        }

                        UpdateHazards(gameTime);
                        break;
                    }
            }
            mPreviousKeyboardState = aCurrentKeyboardState;
            base.Update(gameTime);
        }

        //----------------------- Feng ---------------------
        // 让路面向后移动（使车辆看起来在往前行）
        private void ScrollRoad()
        {
            //Move the scrolling Road: time complexity optimized to O(mRoadY.Length + 1)
            if (mRoadY[mCurrentRoadIndex] >= this.Window.ClientBounds.Height)
            {
                mRoadY[mCurrentRoadIndex] = mRoadY[mCurrentRoadIndex - 1 < 0 ? mRoadY.Length - 1 : mCurrentRoadIndex - 1] - mRoad.Height;
                mCurrentRoadIndex = (mCurrentRoadIndex + 1) % mRoadY.Length;
            }

            for (int aIndex = 0; aIndex < mRoadY.Length; aIndex++)
            {
                mRoadY[aIndex] += (int)(mVelocityY * deltaTime);
            }
        }
        //----------------------- Tian ---------------------

        private void MoveHazard(Hazard theHazard)
        {
            theHazard.Position.Y += (int)(mVelocityY * deltaTime);
            if (theHazard.Position.Y > graphics.GraphicsDevice.Viewport.Height && theHazard.Visible == true)
            {
                theHazard.Visible = false;
                mHazardsCombo = 0;
                ++mHazardsMiss;
                theHazard.sign = Hazard.Sign.Miss;
                theHazard.Position.Y -= mHazard.Height;
                theHazard.SignDisplayTime = (double)mHazard.Height / mVelocityY;
            }
        }

        private void UpdateHazards(GameTime theGameTime)
        {
            if (nowObject[1].AsDouble <= MediaPlayer.PlayPosition.TotalSeconds + (600.0 / mVelocityY) * 0.9)
            {
                if (nowObject[0].AsInt >= 0)
                    AddHazard();

                noteCounter++;
                if (noteCounter < hitObjects.Count)
                    nowObject = hitObjects[noteCounter].AsArray;
                else
                    return;
            }
        }

        private void AddHazard()
        {
            int aPosition = mLastHazardAtLeft ? 440 : 275;

            bool aAddNewHazard = true;
            foreach (Hazard aHazard in mHazards)
            {
                if (aHazard.Visible == false && aHazard.sign == Hazard.Sign.Undecided)
                {
                    aAddNewHazard = false;
                    aHazard.Visible = true;
                    aHazard.Position = new Vector2(aPosition, -mHazard.Height);
                    break;
                }
            }

            if (aAddNewHazard == true)
            {
                //Add a hazard to the different side to the previous one
                Hazard aHazard = new Hazard();
                aHazard.Position = new Vector2(aPosition, -mHazard.Height);
                mHazards.Add(aHazard);
            }
        }

        //----------------------- Feng ------------------------------------------------
        // 检测车辆是否碰到了障碍物
        private void CheckCollision(Hazard theHazard)
        {
            // 分别计算并使用封闭（包裹）盒给障碍物和车
            BoundingBox aHazardBox = new BoundingBox(new Vector3(theHazard.Position.X, theHazard.Position.Y, 0), new Vector3(theHazard.Position.X + (mHazard.Width * .5f), theHazard.Position.Y + ((mHazard.Height) * .5f), 0));
            BoundingBox aCarBox = new BoundingBox(new Vector3(mCarPosition.X, mCarPosition.Y, 0), new Vector3(mCarPosition.X + (mCar.Width * .2f), mCarPosition.Y + (mCar.Height * .2f), 0));
            BoundingBox aCarBoxPerfect = new BoundingBox(new Vector3(mCarPosition.X, mCarPosition.Y, 0), new Vector3(mCarPosition.X + (mCar.Width * .2f), mCarPosition.Y + (mCar.Height * .2f)*0.05f, 0));
            BoundingBox aHazardBoxPerfect = new BoundingBox(new Vector3(theHazard.Position.X, theHazard.Position.Y + (mHazard.Height * .25f), 0), new Vector3(theHazard.Position.X + (mHazard.Width * .5f), theHazard.Position.Y + ((mHazard.Height) * .5f), 0));
            if (aHazardBox.Intersects(aCarBox) == true) // 碰上了
            {
                if (aHazardBoxPerfect.Intersects(aCarBoxPerfect) == true)
                {
                    mScore += 2;
                    mHazardsPerfect++;
                    theHazard.sign = Hazard.Sign.Perfect;
                }
                else 
                {
                    mScore++;
                    mHazardsGood++;
                    theHazard.sign = Hazard.Sign.Good;
                }
                mHazardsCombo++;
                hitSE.Play();
                theHazard.Visible = false;
                theHazard.SignDisplayTime = (double)mHazard.Height / mVelocityY;
            }
        }
        //----------------------- Tian ------------------------------------------------------

        /// <summary>
        /// This is called when the game should draw itself.
        /// </summary>
        /// <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.WhiteSmoke);

            spriteBatch.Begin();

            spriteBatch.Draw(mBackground, new Rectangle(graphics.GraphicsDevice.Viewport.X, graphics.GraphicsDevice.Viewport.Y, graphics.GraphicsDevice.Viewport.Width, graphics.GraphicsDevice.Viewport.Height), Color.White);

            switch (mCurrentState)
            {
                case State.TitleScreen:
                    {
                        //Draw the display text for the Title screen
                        DrawTextCentered("Drive and Hit the Rhythm", 200, 2.0f, Color.Red);
                        DrawTextCentered("Press 'Space' to begin", 500, 1.0f, Color.LightGray);

                        break;
                    }

                default:
                    {
                        DrawRoad();
                        DrawHazards(gameTime);
                        spriteBatch.Draw(mCar, mCarPosition, new Rectangle(0, 0, mCar.Width, mCar.Height), Color.White, 0, new Vector2(0, 0), 0.2f, SpriteEffects.None, 0);
                        
                        spriteBatch.DrawString(mFont, "Combo: \n" + mHazardsCombo.ToString(), new Vector2(28, 520), Color.LightGray, 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
                        spriteBatch.DrawString(mFont, "Score: \n" + mScore.ToString(), new Vector2(28, 25), Color.LightGray, 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
                        if (mCurrentState == State.Success)
                        {
                            DrawTextDisplayArea();
                            String aGrade;
                            if (mHazardsPerfect == hitObjects.Count)
                                aGrade = "S";
                            else if (mHazardsPerfect >= 0.9 * hitObjects.Count && mHazardsMiss <= 0.05 * hitObjects.Count)
                                aGrade = "A";
                            else if (mHazardsPerfect >= 0.7 * hitObjects.Count)
                                aGrade = "B";
                            else if (mHazardsPerfect >= 0.5 * hitObjects.Count)
                                aGrade = "C";
                            else
                                aGrade = "D";
                            DrawTextCentered(aGrade, 80, 5.0f, Color.Red);
                            DrawTextCentered("Perfect: " + mHazardsPerfect.ToString(), 190, 2.0f, Color.Purple);
                            DrawTextCentered("Good: " + mHazardsGood.ToString(), 230, 2.0f, Color.Purple);
                            DrawTextCentered("Miss: " + mHazardsMiss.ToString(), 270, 2.0f, Color.Purple);
                            DrawTextCentered("Press 'Space' to play again.", 400, 1.0f, Color.LightGray);
                        }
                        else if (mCurrentState == State.Paused)
                        {
                            DrawTextDisplayArea();

                            DrawTextCentered("Paused", 200, 3.0f, Color.Red);
                            DrawTextCentered("Press 'P' to continue playing.", 260, 1.0f, Color.LightGray);
                        }
                        break;
                    }
            }
            spriteBatch.End();

            base.Draw(gameTime);
        }

        private void DrawRoad()
        {
            for (int aIndex = 0; aIndex < mRoadY.Length; aIndex++)
            {
                if (mRoadY[aIndex] > mRoad.Height * -1 && mRoadY[aIndex] <= this.Window.ClientBounds.Height)
                {
                    spriteBatch.Draw(mRoad, new Rectangle((int)((this.Window.ClientBounds.Width - mRoad.Width) / 2 - 18), mRoadY[aIndex], mRoad.Width, mRoad.Height + 5), Color.White);
                }
            }
        }

        private void DrawHazards(GameTime gameTime)
        {
            float mLastHazardY = 800;
            foreach (Hazard aHazard in mHazards)
            {
                if (aHazard.Visible == true)
                {
                    spriteBatch.Draw(mHazard, aHazard.Position, new Rectangle(0, 0, mHazard.Width, mHazard.Height), Color.White, 0, new Vector2(0, 0), 0.5f, SpriteEffects.None, 0);
                    if (aHazard.Position.Y <= mLastHazardY) //update the Y coordinate of the last hazard
                    {
                        mLastHazardAtLeft = (aHazard.Position.X == 275);
                        mLastHazardY = aHazard.Position.Y;
                    }
                }
                else if (aHazard.sign != Hazard.Sign.Undecided)
                {
                    Vector2 aSize;
                    switch (aHazard.sign)
                    {
                        case Hazard.Sign.Perfect:
                            aSize = mFont.MeasureString("Perfect   ");
                            spriteBatch.DrawString(mFont, "Perfect\n", new Vector2(aHazard.Position.X == 275 ? 275 - aSize.X : 480 + aSize.X, aHazard.Position.Y), Color.Red, 0, new Vector2(0, 0), (float)(2.0 - aHazard.SignDisplayTime*2), SpriteEffects.None, 0);
                            break;
                        case Hazard.Sign.Good:
                            aSize = mFont.MeasureString("Good   ");
                            spriteBatch.DrawString(mFont, "Good", new Vector2(aHazard.Position.X == 275 ? 275 - aSize.X : 480 + aSize.X, aHazard.Position.Y), Color.Green, 0, new Vector2(0, 0), (float)(2.0 - aHazard.SignDisplayTime*2), SpriteEffects.None, 0);
                            break;
                        case Hazard.Sign.Miss:
                            aSize = mFont.MeasureString("Miss   ");
                            spriteBatch.DrawString(mFont, "Miss", new Vector2(aHazard.Position.X == 275 ? 275 - aSize.X : 480 + aSize.X, aHazard.Position.Y), Color.Gray, 0, new Vector2(0, 0), (float)(2.0 - aHazard.SignDisplayTime*2), SpriteEffects.None, 0);
                            break;
                    }
                    aHazard.SignDisplayTime -= gameTime.ElapsedGameTime.TotalSeconds;
                    if (aHazard.SignDisplayTime < 0)
                        aHazard.sign = Hazard.Sign.Undecided;
                }
            }
        }

        private void DrawTextDisplayArea()
        {
            int aPositionX = (int)((graphics.GraphicsDevice.Viewport.Width / 2) - (450 / 2));
            spriteBatch.Draw(mBackground, new Rectangle(aPositionX, 75, 450, 400), Color.White);
        }

        private void DrawTextCentered(string theDisplayText, int thePositionY, float scale, Color color)
        {
            Vector2 aSize = mFont.MeasureString(theDisplayText) * scale;
            int aPositionX = (int)((graphics.GraphicsDevice.Viewport.Width / 2) - (aSize.X / 2));

            spriteBatch.DrawString(mFont, theDisplayText, new Vector2(aPositionX, thePositionY), color, 0, new Vector2(0, 0), scale, SpriteEffects.None, 0);
        }
    }
}
