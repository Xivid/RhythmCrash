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

namespace RhythmHit
{
    // / <summary>
    // / This is the main type for your game
    // / </summary>
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        GraphicsDeviceManager graphics;
        private Texture2D mBackground;
        private Texture2D mRoad;
        private Texture2D mHazard;
        private Texture2D mCar;
        private List<Texture2D> mAlbums = new List<Texture2D>();

        private KeyboardState mPreviousKeyboardState;
        SpriteBatch spriteBatch, signBatch;

        private Vector2 mCarPosition = new Vector2(290, 440);
        private int mMoveCarX = 160;
        private int mVelocityY;
        private int mScore;
        private int mHazardsPerfect, mHazardsGood, mHazardsMiss;
        private int mHazardsCombo;
        private int mCurrentRoadIndex;
        private bool mLastHazardAtLeft;

        private int[] mRoadY = new int[2]; // use two road pictures to scroll
        private List<Hazard> mHazards = new List<Hazard>();
        private List<Song> mMusics = new List<Song>();
        private List<Song> mMusicPreviews = new List<Song>(); // preview part of the musics

        private SpriteFont[] mFonts = new SpriteFont[10];
        // 0.8, 1.5, 1, 2, 3 , 5

        private int difficulty = 1;
        private int musicChosen = 1;
        private List<string> musicNames;
        private int noteCounter = 0;
        private double deltaTime; // duration of a frame
        private List<JSONNode> BeatmapJson = new List<JSONNode>();
        private JSONNode beatmap;
        private JSONArray hitObjects;
        private JSONArray nowObject;
        private SoundEffect hitSE;

        // ----------------------- Feng ---------------------
        // enumerate type defined to represent the state of game process
        private enum State
        {
            TitleScreen, // title and music/difficulty selection
            Running, // where the player controls the rocket to hit the UFOs while the rhythm goes
            Paused, // the player can pause the game by pressing 'P' when running
            Success // when the song ends, show the score and evaluated level(X, S, A, B, C, D)
        }

        // --------------------- Tian --------------------------


        private State mCurrentState = State.TitleScreen;

        public Game1()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";

            graphics.IsFullScreen = false;
            // define the size of game scene
            graphics.PreferredBackBufferHeight = 600;
            graphics.PreferredBackBufferWidth = 800;
        }

        // / <summary>
        // / Allows the game to perform any initialization it needs to before starting to run.
        // / This is where it can query for any required services and load any non-graphic
        // / related content.  Calling base.Initialize will enumerate through any components
        // / and initialize them as well.
        // / </summary>
        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        // / <summary>
        // / LoadContent will be called once per game and is the place to load
        // / all of your content.
        // / </summary>
        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            signBatch = new SpriteBatch(GraphicsDevice);
            try
            {
                mCar = Content.Load<Texture2D>("Images/Car");
                mBackground = Content.Load<Texture2D>("Images/Background");
                mRoad = Content.Load<Texture2D>("Images/Road");
                mHazard = Content.Load<Texture2D>("Images/Hazard");
                // define fonts of different size
                mFonts[0] = Content.Load<SpriteFont>("Fonts/0.8x");
                mFonts[1] = Content.Load<SpriteFont>("Fonts/1x");
                mFonts[2] = Content.Load<SpriteFont>("Fonts/2x");
                mFonts[3] = Content.Load<SpriteFont>("Fonts/3x");
                mFonts[4] = Content.Load<SpriteFont>("Fonts/1.5x");
                mFonts[5] = Content.Load<SpriteFont>("Fonts/5x");

                // dynamicly get all music resources on start of the program
                musicNames = Directory.GetDirectories(Content.RootDirectory + "/Music/").ToList();
                for (int i = 0; i < musicNames.Count; i++)
                {
                    string[] dirs = musicNames[i].Split('/'); // get the directory name
                    musicNames[i] = dirs[dirs.Length - 1]; // not the real name of the music, but the directory name. real name is stored in the beatmap
                    mAlbums.Add(Content.Load<Texture2D>("Music/" + musicNames[i] + "/Album")); // cover pictures
                    mMusics.Add(Content.Load<Song>("Music/" + musicNames[i] + "/" + musicNames[i])); // load all musics
                    mMusicPreviews.Add(Content.Load<Song>("Music/" + musicNames[i] + "/Preview")); // preview music(climax part of the music)
                    BeatmapJson.Add(JSON.Parse(Content.Load<string>("Music/" + musicNames[i] + "/beatmap"))); // load all beatmaps (position of all hazards)
                }
            }
            catch (Exception e) // load fail
            {
                this.Exit();
            }
        }

        // / <summary>
        // / UnloadContent will be called once per game and is the place to unload
        // / all content.
        // / </summary>
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
            mVelocityY = (1 + difficulty) * 300; // velocity increases with difficulty
            mCurrentRoadIndex = 0; // the scroll starts from the first road
            mHazards.Clear();

            // Read beatmap of the chosen music with the chosen difficulty
            String musicname = musicNames[musicChosen];
            beatmap = BeatmapJson[musicChosen];
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

            try
            {
                hitSE = Content.Load<SoundEffect>("Music/" + musicname + "/hit");
            }
            catch (Exception e) // load fail
            {
                this.Exit();
            }

            mCurrentState = State.Running;

            noteCounter = 0;
            nowObject = hitObjects[0].AsArray;

            MediaPlayer.Play(mMusics[musicChosen]);
            MediaPlayer.Volume = 1.0f; // reset the volume to 1.0f, in case the player starts the game when the preview music is fading in or fading out
        }

        // / <summary>
        // / Allows the game to run logic such as updating the world,
        // / checking for collisions, gathering input, and playing audio.
        // / </summary>
        // / <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Update(GameTime gameTime)
        {
            KeyboardState aCurrentKeyboardState = Keyboard.GetState();

            // Allows the game to exit
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || aCurrentKeyboardState.IsKeyDown(Keys.Escape) == true)
            {
                this.Exit();
            }
            if (aCurrentKeyboardState.IsKeyDown(Keys.F) == true && mPreviousKeyboardState.IsKeyDown(Keys.F) == false)
            {
                graphics.IsFullScreen = !graphics.IsFullScreen;
                graphics.ApplyChanges();
            }
            deltaTime = gameTime.ElapsedGameTime.TotalSeconds; // the duration 

            switch (mCurrentState)
            {
                
                case State.TitleScreen:
                    {
                        // fade-in effect for preview musics
                        if (MediaPlayer.PlayPosition.TotalSeconds < 2)
                            MediaPlayer.Volume = (float)MediaPlayer.PlayPosition.TotalSeconds * 0.5f;
                        // fade-out effect for preview musics
                        if (MediaPlayer.PlayPosition.TotalSeconds + 2 > mMusicPreviews[musicChosen].Duration.TotalSeconds)
                            MediaPlayer.Volume = (float)(mMusicPreviews[musicChosen].Duration.TotalSeconds - MediaPlayer.PlayPosition.TotalSeconds) * 0.5f;

                        if (MediaPlayer.State == MediaState.Stopped) // Play the preview part of the music and loop
                            MediaPlayer.Play(mMusicPreviews[musicChosen]);
                        if (aCurrentKeyboardState.IsKeyDown(Keys.Left) == true && mPreviousKeyboardState.IsKeyDown(Keys.Left) == false && musicChosen > 0) 
                        {
                            musicChosen--;
                            MediaPlayer.Play(mMusicPreviews[musicChosen]); // Play part of the chosen music
                        }
                        if (aCurrentKeyboardState.IsKeyDown(Keys.Right) == true && mPreviousKeyboardState.IsKeyDown(Keys.Right) == false && musicChosen < musicNames.Count - 1)
                        {
                            musicChosen++;
                            MediaPlayer.Play(mMusicPreviews[musicChosen]);
                        }
                        if (aCurrentKeyboardState.IsKeyDown(Keys.Up) == true && mPreviousKeyboardState.IsKeyDown(Keys.Up) == false && difficulty < 2)
                        {
                            difficulty++;
                        }
                        if (aCurrentKeyboardState.IsKeyDown(Keys.Down) == true && mPreviousKeyboardState.IsKeyDown(Keys.Down) == false && difficulty > 0)
                        {
                            difficulty--;
                        }
                        if (aCurrentKeyboardState.IsKeyDown(Keys.Space) == true && mPreviousKeyboardState.IsKeyDown(Keys.Space) == false)
                        {
                            StartGame();
                        }
                        break;
                    }
                case State.Success:
                    {
                        if (aCurrentKeyboardState.IsKeyDown(Keys.Space) == true && mPreviousKeyboardState.IsKeyDown(Keys.Space) == false)
                        {
                            mCurrentState = State.TitleScreen;
                        }
                        break;
                    }
                case State.Paused:
                    {
                        // resume
                        if (aCurrentKeyboardState.IsKeyDown(Keys.P) == true && mPreviousKeyboardState.IsKeyDown(Keys.P) == false)
                        {
                            MediaPlayer.Resume();
                            mCurrentState = State.Running;
                        }
                        break;
                    }
                case State.Running:
                    {
                        // pause
                        if (aCurrentKeyboardState.IsKeyDown(Keys.P) == true && mPreviousKeyboardState.IsKeyDown(Keys.P) == false)
                        {
                            MediaPlayer.Pause();
                            mCurrentState = State.Paused;
                            break;
                        }
                        
                        // end
                        if (MediaPlayer.State == MediaState.Stopped || (aCurrentKeyboardState.IsKeyDown(Keys.E) == true && mPreviousKeyboardState.IsKeyDown(Keys.E) == false))
                        {
                            MediaPlayer.Stop();
                            mCurrentState = State.Success;
                            break;
                        }

                        // Use Left and Right arrows to control
                        if ((aCurrentKeyboardState.IsKeyDown(Keys.Left) == true && mPreviousKeyboardState.IsKeyDown(Keys.Left) == false && mMoveCarX < 0) || (aCurrentKeyboardState.IsKeyDown(Keys.Right) == true && mPreviousKeyboardState.IsKeyDown(Keys.Right) == false && mMoveCarX > 0))
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

                        UpdateHazards();
                        break;
                    }
            }
            mPreviousKeyboardState = aCurrentKeyboardState;
            base.Update(gameTime);
        }

        // ----------------------- Feng ---------------------
        // move the road backwards (as if the rocket is moving forward)
        private void ScrollRoad()
        {
            // Move the scrolling Road: time complexity optimized to O(mRoadY.Length + 1)
            if (mRoadY[mCurrentRoadIndex] >= this.Window.ClientBounds.Height)
            {
                mRoadY[mCurrentRoadIndex] = mRoadY[mCurrentRoadIndex - 1 < 0 ? mRoadY.Length - 1 : mCurrentRoadIndex - 1] - mRoad.Height;
                mCurrentRoadIndex = (mCurrentRoadIndex + 1) % mRoadY.Length;
            }

            for (int aIndex = 0; aIndex < mRoadY.Length; aIndex++)
            {
                mRoadY[aIndex] += (int)(mVelocityY * deltaTime); // to make the speed the same on different computers with different FPS
            }
        }
        // ----------------------- Tian ---------------------

        private void MoveHazard(Hazard theHazard)
        {
            theHazard.Position.Y += (int)(mVelocityY * deltaTime);
            if (theHazard.Position.Y > graphics.GraphicsDevice.Viewport.Height && theHazard.Visible == true)
            {
                theHazard.Visible = false;
                mHazardsCombo = 0;
                ++mHazardsMiss;
                theHazard.sign = Hazard.Sign.Miss; // sign it with "miss" so that a "MISS" can be shown on the screen
                theHazard.Position.Y -= mHazard.Height;
                theHazard.SignDisplayTime = (double)mHazard.Height / mVelocityY; // set the duration it will be displayed  
            }
        }

        private void UpdateHazards()
        {
            if (nowObject[1].AsDouble <= MediaPlayer.PlayPosition.TotalSeconds + ((mCarPosition.Y + mHazard.Height * 0.7) / mVelocityY))
            { // time for a new hazard to enter the stage
                if (noteCounter < hitObjects.Count)
                {
                    AddHazard();
                    noteCounter++;
                    if (noteCounter == hitObjects.Count)
                        return;
                    nowObject = hitObjects[noteCounter].AsArray; // nowObject = next note
                }
            }
        }

        private void AddHazard()
        {
            int aPosition = mLastHazardAtLeft ? 440 : 275; // make every hazard in the different column with the previous one

            bool aAddNewHazard = true;
            foreach (Hazard aHazard in mHazards)
            {
                if (aHazard.Visible == false && aHazard.sign == Hazard.Sign.Undecided) //reuse the isolated ones
                {
                    aAddNewHazard = false;
                    aHazard.Visible = true;
                    aHazard.Position = new Vector2(aPosition, -mHazard.Height);
                    break;
                }
            }

            if (aAddNewHazard == true) //no one can be reused
            {
                // Add a hazard to the different side to the previous one
                Hazard aHazard = new Hazard();
                aHazard.Position = new Vector2(aPosition, -mHazard.Height);
                mHazards.Add(aHazard);
            }
        }

        // ----------------------- Feng ------------------------------------------------
        // check if the rocket runs into a hazard
        private void CheckCollision(Hazard theHazard)
        {
            // wrap the hazard and car with BoundingBox
            BoundingBox aHazardBox = new BoundingBox(new Vector3(theHazard.Position.X, theHazard.Position.Y, 0), new Vector3(theHazard.Position.X + (mHazard.Width * .5f), theHazard.Position.Y + ((mHazard.Height) * .5f), 0));
            BoundingBox aCarBox = new BoundingBox(new Vector3(mCarPosition.X, mCarPosition.Y, 0), new Vector3(mCarPosition.X + (mCar.Width * .8f), mCarPosition.Y + (mCar.Height * .8f), 0));
            BoundingBox aCarBoxPerfect = new BoundingBox(new Vector3(mCarPosition.X, mCarPosition.Y, 0), new Vector3(mCarPosition.X + (mCar.Width * .8f), mCarPosition.Y + (mCar.Height * .8f) * 0.05f, 0));
            BoundingBox aHazardBoxPerfect = new BoundingBox(new Vector3(theHazard.Position.X, theHazard.Position.Y + (mHazard.Height * .25f), 0), new Vector3(theHazard.Position.X + (mHazard.Width * .5f), theHazard.Position.Y + ((mHazard.Height) * .5f), 0));
            if (aHazardBox.Intersects(aCarBox) == true) // collided
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
                theHazard.SignDisplayTime = (double)mHazard.Height / mVelocityY; // show "Perfect" or "Good" in the game scene
            }
        }
        // ----------------------- Tian ------------------------------------------------------

        // / <summary>
        // / This is called when the game should draw itself.
        // / </summary>
        // / <param name="gameTime">Provides a snapshot of timing values.</param>
        protected override void Draw(GameTime gameTime)
        {
            graphics.GraphicsDevice.Clear(Color.WhiteSmoke);

            spriteBatch.Begin();

            spriteBatch.Draw(mBackground, new Rectangle(graphics.GraphicsDevice.Viewport.X, graphics.GraphicsDevice.Viewport.Y, graphics.GraphicsDevice.Viewport.Width, graphics.GraphicsDevice.Viewport.Height), Color.White);

            switch (mCurrentState)
            {
                case State.TitleScreen:
                    {
                        // Draw the display text for the Title screen

                        DrawTextCentered("Drive and Hit the Rhythm", 30, 2.0f, new Color(200, 200, 200));
                        DrawTextCentered("Left/Right: Music  Up/Down: Difficulty F: FullScreen Space: Start", 100, 0.8f, new Color(200, 200, 200));
                        if (musicChosen > 0) // prev sign
                            spriteBatch.DrawString(mFonts[5], "<<", new Vector2(40, 240), new Color(200, 200, 200), 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);

                        if (musicChosen < musicNames.Count - 1) // next sign
                            spriteBatch.DrawString(mFonts[5], ">>", new Vector2(770 - mFonts[5].MeasureString(">>").X, 240), new Color(200, 200, 200), 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);

                        spriteBatch.Draw(mAlbums[musicChosen], new Rectangle(155, 140, 500, 350), Color.White);
                        DrawTextCentered(BeatmapJson[musicChosen]["Title"], 500, 1.5f, new Color(200, 200, 200));
                        switch (difficulty)
                        {
                            case 0:
                                DrawTextCentered("Easy", 550, 1.0f, new Color(58, 183, 239));
                                break;
                            case 1:
                                DrawTextCentered("Normal", 550, 1.0f, new Color(191, 255, 160));
                                break;
                            case 2:
                                DrawTextCentered("Hard", 550, 1.0f, new Color(249, 90, 101));
                                break;
                        }
                        break;
                    }

                default:
                    {
                        DrawRoad();
                        spriteBatch.Draw(mCar, mCarPosition, new Rectangle(0, 0, mCar.Width, mCar.Height), Color.White, 0, new Vector2(0, 0), 0.8f, SpriteEffects.None, 0);
                        spriteBatch.DrawString(mFonts[1], "Score\n" + mScore.ToString(), new Vector2(28, 25), new Color(200, 200, 200), 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
                        spriteBatch.DrawString(mFonts[1], "Combo\n" + mHazardsCombo.ToString(), new Vector2(28, 520), new Color(200, 200, 200), 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
                        spriteBatch.DrawString(mFonts[1], "Pause\nwith \"P\"", new Vector2(680, 25), new Color(200, 200, 200), 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
                        spriteBatch.DrawString(mFonts[1], "Exit\nwith \"E\"", new Vector2(680, 520), new Color(200, 200, 200), 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
                        
                        DrawHazards(gameTime);

                        if (mCurrentState == State.Success)
                        {
                            DrawTextDisplayArea();
                            String aGrade;
                            Color aColor;
                            //judge the grade
                            if (mHazardsPerfect == hitObjects.Count)
                            {
                                aGrade = "X";
                                aColor = new Color(200, 200, 200);
                            }
                            else if (mHazardsPerfect >= 0.9 * hitObjects.Count  && mHazardsMiss <= 0.01 * hitObjects.Count)
                            {
                                aGrade = "S";
                                aColor = new Color(200, 200, 200);
                            }
                            else if (mHazardsPerfect >= 0.8 * hitObjects.Count)
                            {
                                aGrade = "A";
                                aColor = new Color(58, 183, 239);
                            }
                            else if (mHazardsPerfect >= 0.7 * hitObjects.Count)
                            {
                                aGrade = "B";
                                aColor = new Color(191, 255, 160);

                            }
                            else if (mHazardsPerfect >= 0.6 * hitObjects.Count)
                            {
                                aGrade = "C";
                                aColor = new Color(251, 208, 114);
                            }
                            else
                            {
                                aGrade = "D";
                                aColor = new Color(249, 90, 101);
                            }
                            DrawTextCentered(aGrade, 80, 5.0f, aColor);
                            DrawTextCentered("Perfect  " + mHazardsPerfect.ToString(), 220, 2.0f, new Color(58, 183, 239));
                            DrawTextCentered("Good     " + mHazardsGood.ToString(), 270, 2.0f, new Color(191, 255, 160));
                            DrawTextCentered("Miss     " + mHazardsMiss.ToString(), 320, 2.0f, new Color(249, 90, 101));
                            DrawTextCentered("Press 'Space' to go back.", 400, 1.0f, new Color(200, 200, 200));
                        }
                        else if (mCurrentState == State.Paused)
                        {
                            DrawTextDisplayArea();
                            DrawTextCentered("Paused", 200, 3.0f, new Color(200, 200, 200));
                            DrawTextCentered("Press 'P' to continue playing.", 300, 1.0f, new Color(200, 200, 200));
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
                if (mRoadY[aIndex] > mRoad.Height * -1 && mRoadY[aIndex] <= this.Window.ClientBounds.Height) // draw the roads with part (or whole) in the game scene
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
                if (aHazard.Visible == true) // only draw visible hazards
                {
                    spriteBatch.Draw(mHazard, aHazard.Position, new Rectangle(0, 0, mHazard.Width, mHazard.Height), Color.White, 0, new Vector2(0, 0), 0.5f, SpriteEffects.None, 0);
                    if (aHazard.Position.Y <= mLastHazardY) // update the Y coordinate of the last hazard
                    {
                        mLastHazardAtLeft = (aHazard.Position.X == 275);
                        mLastHazardY = aHazard.Position.Y;
                    }
                }
                else if (aHazard.sign != Hazard.Sign.Undecided) // for invisible hazards with a sign, draw the corresponding text
                {
                    Vector2 aSize;
                    switch (aHazard.sign)
                    {
                        case Hazard.Sign.Perfect:
                            aSize = mFonts[4].MeasureString("Perfect");
                            spriteBatch.DrawString(mFonts[4], "Perfect", new Vector2((aHazard.Position.X == 275 ? 275 : 440) + aSize.X / 2 - 10, aHazard.Position.Y), new Color(58, 183, 239), 0, new Vector2(aSize.X / 2, aSize.Y / 2), (float)(1.0 - aHazard.SignDisplayTime), SpriteEffects.None, 0);
                            break;
                        case Hazard.Sign.Good:
                            aSize = mFonts[4].MeasureString("Good");
                            spriteBatch.DrawString(mFonts[4], "Good", new Vector2((aHazard.Position.X == 275 ? 275 : 440) + aSize.X / 2 + 20, aHazard.Position.Y), new Color(191, 255, 160), 0, new Vector2(aSize.X / 2, aSize.Y / 2), (float)(1.0 - aHazard.SignDisplayTime), SpriteEffects.None, 0);
                            break;
                        case Hazard.Sign.Miss:
                            aSize = mFonts[4].MeasureString("Miss");
                            spriteBatch.DrawString(mFonts[4], "Miss", new Vector2((aHazard.Position.X == 275 ? 275 : 440) + aSize.X / 2 + 20, aHazard.Position.Y), new Color(249, 90, 101), 0, new Vector2(aSize.X / 2, aSize.Y / 2), (float)(1.0 - aHazard.SignDisplayTime), SpriteEffects.None, 0);
                            break;
                    }
                    aHazard.SignDisplayTime -= gameTime.ElapsedGameTime.TotalSeconds;
                    if (aHazard.SignDisplayTime < 0) // set to 'Undecided' so that it can be reused
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
            int mFontIndex;
            if (scale == 0.8f)
                mFontIndex = 0;
            else if (scale == 1.5f)
                mFontIndex = 4;
            else if (scale == 2.0f)
                mFontIndex = 2;
            else if (scale == 3.0f)
                mFontIndex = 3;
            else if (scale == 5.0f)
                mFontIndex = 5;
            else
                mFontIndex = 1;

            Vector2 aSize = mFonts[mFontIndex].MeasureString(theDisplayText);
            int aPositionX = (int)((graphics.GraphicsDevice.Viewport.Width / 2) - (aSize.X / 2));

            spriteBatch.DrawString(mFonts[mFontIndex], theDisplayText, new Vector2(aPositionX, thePositionY), color, 0, new Vector2(0, 0), 1.0f, SpriteEffects.None, 0);
        }
    }
}
