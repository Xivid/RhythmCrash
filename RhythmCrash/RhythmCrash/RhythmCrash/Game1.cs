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

        private KeyboardState mPreviousKeyboardState;
        SpriteBatch spriteBatch;


        private Texture2D mCar;

        private Vector2 mCarPosition = new Vector2(280, 440);
        private int mMoveCarX = 160;
        private int mVelocityY;
        private double mNextHazardAppearsIn;
        private int mCarsRemaining;
        private int mHazardsPassed;
        private int mHazardsCombo;
        private int mIncreaseVelocity;
        private int mCurrentRoadIndex;
        private double mExitCountDown = 10;

        private int[] mRoadY = new int[2]; //ʹ������·��
        private List<Hazard> mHazards = new List<Hazard>();

        // ��������� - �ȷ�������ʾ�ϰ����λ��
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
        // �Զ���ö�����ͣ�������ͬ����Ϸ״̬
        private enum State
        {
            TitleScreen,      // ��ʼƬͷ
            Running,
            /*Crash,           // ��ײ
            GameOver,*/
            Success
        }
        //--------------------- Tian --------------------------


        private State mCurrentState = State.TitleScreen;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            // ������Ϸ���ڴ�С
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

            mCar = Content.Load<Texture2D>("Images/Car");
            mBackground = Content.Load<Texture2D>("Images/Background");
            mRoad = Content.Load<Texture2D>("Images/Road");
            mHazard = Content.Load<Texture2D>("Images/Hazard");

            // ��������
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

            mHazardsPassed = 0;
            mHazardsCombo = 0;
            mCarsRemaining = 3; // ��ʣ����������
            mVelocityY = 600;
            mNextHazardAppearsIn = 1.5;
            mIncreaseVelocity = 1;  // �ٶȵ���
            mCurrentRoadIndex = 0;  //�ӵ�0��·�濪ʼ
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
                    /*case State.GameOver:*/
                    {
                        MediaPlayer.Stop();

                        ExitCountdown(gameTime);

                        if (aCurrentKeyboardState.IsKeyDown(Keys.Space) == true && mPreviousKeyboardState.IsKeyDown(Keys.Space) == false)
                        {
                            StartGame();
                        }
                        break;
                    }

                case State.Running:
                    {
                        //If the user has pressed the Spacebar, then make the car switch lanes
                        if (aCurrentKeyboardState.IsKeyDown(Keys.Left) == true && mPreviousKeyboardState.IsKeyDown(Keys.Left) == false && mMoveCarX < 0)
                        {
                            mCarPosition.X += mMoveCarX;
                            mMoveCarX *= -1;
                        }

                        if (aCurrentKeyboardState.IsKeyDown(Keys.Right) == true && mPreviousKeyboardState.IsKeyDown(Keys.Right) == false && mMoveCarX > 0)
                        {
                            mCarPosition.X += mMoveCarX;
                            mMoveCarX *= -1;
                        }

                        ScrollRoad();

                        if (MediaPlayer.State == MediaState.Stopped)
                            mCurrentState = State.Success;

                        foreach (Hazard aHazard in mHazards)
                        {
                            if (CheckCollision(aHazard) == true)
                            {
                                break;
                            }

                            MoveHazard(aHazard);
                        }

                        UpdateHazards(gameTime);
                        break;
                    }
                /*case State.Crash:
                    {
                        //If the user has pressed the Space key, then resume driving
                        if (aCurrentKeyboardState.IsKeyDown(Keys.Space) == true && mPreviousKeyboardState.IsKeyDown(Keys.Space) == false)
                        {
                            mHazards.Clear();
                            mCurrentState = State.Running;
                        }

                        break;
                    }*/
            }
            mPreviousKeyboardState = aCurrentKeyboardState;
            base.Update(gameTime);
        }

        //----------------------- Feng ---------------------
        // ��·������ƶ���ʹ��������������ǰ�У�
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
                //Miss
                //mHazardsPassed += 1;

                /*if (mHazardsPassed >= hitObjects.Count) // ���ͨ��100���ϰ���ɹ���
                {
                    mCurrentState = State.Success;
                    mExitCountDown = 10;
                }*/

                /*mIncreaseVelocity -= 1;
                if (mIncreaseVelocity < 0)
                {
                    mIncreaseVelocity = 1;
                    //mVelocityY += 1;
                }*/
            }
        }

        private void UpdateHazards(GameTime theGameTime)
        {
            /* if (nowObject[0].AsInt == -2)
            {
                mCurrentState = State.Success;
                return;
            } */

            //mNextHazardAppearsIn -= theGameTime.ElapsedGameTime.TotalSeconds; // ��Ϸ���е�ʱ��
            //if (mNextHazardAppearsIn < 0)
            if (nowObject[1].AsDouble <= MediaPlayer.PlayPosition.TotalSeconds + (600.0 / mVelocityY) * 1.1)
            {

                /*
                int aLowerBound = 24 - ((int)(mVelocityY * deltaTime) * 2);
                int aUpperBound = 30 - ((int)(mVelocityY * deltaTime) * 2);

                if ((int)(mVelocityY * deltaTime) > 10)
                {
                    aLowerBound = 6;
                    aUpperBound = 8;
                }

                // �����ϰ�����ֵ�λ�ã������
                mNextHazardAppearsIn = (double)mRandom.Next(aLowerBound, aUpperBound) / 10;
                */
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
            int aRoadPosition = mRandom.Next(1, 3);
            int aPosition = 275;
            if (aRoadPosition == 2)
            {
                aPosition = 440;
            }

            /*bool aAddNewHazard = true;
            foreach (Hazard aHazard in mHazards)
            {
                if (aHazard.Visible == false)
                {
                    aAddNewHazard = false;
                    aHazard.Visible = true;
                    aHazard.Position = new Vector2(aPosition, -mHazard.Height);
                    break;
                }
            }

            if (aAddNewHazard == true) */
            if (mHazards.Count < 1)
            {
                Hazard aHazard = new Hazard();
                aHazard.Position = new Vector2(aPosition, -mHazard.Height);
                mHazards.Add(aHazard);
            }
            else
            {
                //Add a hazard to the different side to the previous one
                Hazard aHazard = new Hazard();

                Vector2 lastPos = mHazards[mHazards.Count - 1].Position;

                aHazard.Position = new Vector2(lastPos.X == 275 ? 440 : 275, -mHazard.Height);
                mHazards.Add(aHazard);
            }
        }

        //----------------------- Feng ------------------------------------------------
        // ��⳵���Ƿ��������ϰ���
        private bool CheckCollision(Hazard theHazard)
        {
            // �ֱ���㲢ʹ�÷�գ��������и��ϰ���ͳ�
            BoundingBox aHazardBox = new BoundingBox(new Vector3(theHazard.Position.X, theHazard.Position.Y, 0), new Vector3(theHazard.Position.X + (mHazard.Width * .4f), theHazard.Position.Y + ((mHazard.Height - 50) * .4f), 0));
            BoundingBox aCarBox = new BoundingBox(new Vector3(mCarPosition.X, mCarPosition.Y, 0), new Vector3(mCarPosition.X + (mCar.Width * .2f), mCarPosition.Y + (mCar.Height * .2f), 0));

            if (aHazardBox.Intersects(aCarBox) == true) // ��������?
            {
                /*mCurrentState = State.Crash;
                mCarsRemaining -= 1;
                if (mCarsRemaining < 0)
                {
                    mCurrentState = State.GameOver;
                    mExitCountDown = 10;
                }*/
                //return true;
                mHazardsPassed++;
                //Perfect, Good
                mHazardsCombo++;
                hitSE.Play();
                theHazard.Visible = false;
                theHazard.Position = new Vector2(-1000, -1000);
                return false;
            }

            return false;
        }
        //----------------------- Tian ------------------------------------------------------

        private void ExitCountdown(GameTime theGameTime)
        {
            mExitCountDown -= theGameTime.ElapsedGameTime.TotalSeconds;
            if (mExitCountDown < 0)
            {
                this.Exit();
            }
        }

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
                        DrawTextCentered("Drive Fast And Avoid the Oncoming Obstacles", 200);
                        DrawTextCentered("Press 'Space' to begin", 260);
                        DrawTextCentered("Exit in " + ((int)mExitCountDown).ToString(), 475);

                        break;
                    }

                default:
                    {
                        DrawRoad();
                        DrawHazards();

                        spriteBatch.Draw(mCar, mCarPosition, new Rectangle(0, 0, mCar.Width, mCar.Height), Color.White, 0, new Vector2(0, 0), 0.2f, SpriteEffects.None, 0);

                        spriteBatch.DrawString(mFont, "Combo: \n" + mHazardsCombo.ToString(), new Vector2(28, 520), Color.LightGray, 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
                        //spriteBatch.DrawString(mFont, , new Vector2(28, 520), Color.LightGray, 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
                        /*for (int aCounter = 0; aCounter < mCarsRemaining; aCounter++)
                        {
                            spriteBatch.Draw(mCar, new Vector2(25 + (30 * aCounter), 550), new Rectangle(0, 0, mCar.Width, mCar.Height), Color.White, 0, new Vector2(0, 0), 0.05f, SpriteEffects.None, 0);
                        }*/

                        spriteBatch.DrawString(mFont, "Score: \n" + mHazardsPassed.ToString(), new Vector2(28, 25), Color.LightGray, 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);

                        /*if (mCurrentState == State.Crash)
                        {
                            DrawTextDisplayArea();
                            
                            DrawTextCentered("Crash!", 200);
                            DrawTextCentered("Press 'Space' to continue driving.", 260);
                        }
                        else if (mCurrentState == State.GameOver)
                        {
                            DrawTextDisplayArea();

                            DrawTextCentered("Game Over.", 200);
                            DrawTextCentered("Press 'Space' to try again.", 260);
                            DrawTextCentered("Exit in " + ((int)mExitCountDown).ToString(), 400);

                        }
                        else */
                        if (mCurrentState == State.Success)
                        {
                            DrawTextDisplayArea();

                            DrawTextCentered("Congratulations!", 200);
                            DrawTextCentered("Press 'Space' to play again.", 260);
                            DrawTextCentered("Exit in " + ((int)mExitCountDown).ToString(), 400);
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

        private void DrawHazards()
        {
            bool flag = false;
            foreach (Hazard aHazard in mHazards)
            {
                if (aHazard.Visible == true)
                {

                    spriteBatch.Draw(mHazard, aHazard.Position, new Rectangle(0, 0, mHazard.Width, mHazard.Height), Color.White, 0, new Vector2(0, 0), 0.4f, SpriteEffects.None, 0);
                    flag = !flag;
                }
            }
        }

        private void DrawTextDisplayArea()
        {
            int aPositionX = (int)((graphics.GraphicsDevice.Viewport.Width / 2) - (450 / 2));
            spriteBatch.Draw(mBackground, new Rectangle(aPositionX, 75, 450, 400), Color.White);
        }

        private void DrawTextCentered(string theDisplayText, int thePositionY)
        {
            Vector2 aSize = mFont.MeasureString(theDisplayText);
            int aPositionX = (int)((graphics.GraphicsDevice.Viewport.Width / 2) - (aSize.X / 2));

            spriteBatch.DrawString(mFont, theDisplayText, new Vector2(aPositionX, thePositionY), Color.LightGray, 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
            //spriteBatch.DrawString(mFont, theDisplayText, new Vector2(aPositionX + 1, thePositionY + 1), Color.LightGray, 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
        }
    }
}
