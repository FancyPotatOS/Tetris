using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Media;
using System.Collections.Generic;
using System;


/* 
    Tetris was created by Alexey Pajitnov
    ThemeA (Background Music) was created for Tetris, not by myself

    Sound effect and music files created in Musescore
    
*/

namespace Tetris
{
    public class Tetris : Game
    {
        // Graphics
        readonly GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;

        // Sizes
        const int sizeX = 10;
        const int sizeY = 24;
        const int pixelSize = 24;


        /*      Tiles       */

        // 24 x 24
        Texture2D empty;
        // 24 x 24
        Texture2D tile;
        // 45 x 5
        Texture2D lostMessage;
        // 27 x 9
        Texture2D hold;
        // 24x24
        Texture2D center_circle;

        /*      Sounds       */

        // Piece lands
        SoundEffect land;
        // Piece fast-falls
        SoundEffect fall;
        // Lost game
        SoundEffect lost;
        // Theme A
        Song ThemeA;


        /*      Variables       */

        /*  Clocks  */
        // Block falling count
        int fallCount = 0;
        // Down press hesitation
        int downHes;
        // New piece addition
        int newPieceCooldown;
        // Whether you can hold a piece again
        bool canHold;

            /*  Gamestates  */
        enum GameState
        {
            playing, none, lost
        }
        GameState currState;

        // Objects
        static Random random;
        Board board;

        readonly List<Piece> pieces;

        // Keyboard
        KeyboardState ks;
        List<Keys> accountedKeys;

        public Tetris()
        {
            // Setup
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            // Size of board, and 4 spaces for hold piece, 5th for spacing
            graphics.PreferredBackBufferWidth = sizeX * pixelSize + (6 * pixelSize);
            graphics.PreferredBackBufferHeight = sizeY * pixelSize;
            graphics.ApplyChanges();

            // Initialze objects
            board = new Board(sizeX, sizeY);
            pieces = new List<Piece>();
            accountedKeys = new List<Keys>();
            random = new Random();
            canHold = true;

            // Initialize Gamestate
            currState = GameState.playing;

            // L Pieces
            int[][] segments = new int[][] { new int[]{ -2, 0 }, new int[] { -1, 0 }, new int[] { 0, 0 }, new int[] { 0, 1 } };
            Piece piece = new Piece(segments, Color.Green);
            pieces.Add(piece);
            segments = new int[][] { new int[] { -2, 0 }, new int[] { -1, 0 }, new int[] { 0, 0 }, new int[] { 0, -1 } };
            piece = new Piece(segments, Color.Red);
            pieces.Add(piece);

            // Z Pieces
            segments = new int[][] { new int[] { 1, 1 }, new int[] { 1, 0 }, new int[] { 0, 0 }, new int[] { 0, -1 } };
            piece = new Piece(segments, new Color(0.003f, 0.258f, 0.508f));
            pieces.Add(piece);
            segments = new int[][] { new int[] { -1, 1 }, new int[] { -1, 0 }, new int[] { 0, 0 }, new int[] { 0, -1 } };
            piece = new Piece(segments, Color.Orange);
            pieces.Add(piece);

            // Cube piece
            segments = new int[][] { new int[] {1, 1 }, new int[] { 1, 0 }, new int[] { 0, 1 }, new int[] { 0, 0} };
            piece = new Piece(segments, Color.Yellow);
            pieces.Add(piece);

            // Straight piece 
            segments = new int[][] { new int[] { -2, 0 }, new int[] { -1, 0 }, new int[] { 0, 0 }, new int[] { 1, 0 } };
            piece = new Piece(segments, Color.Aquamarine);
            pieces.Add(piece);

            // T piece 
            segments = new int[][] { new int[] { -1, 0 }, new int[] { 0, 1 }, new int[] { 0, 0 }, new int[] { 1, 0 } };
            piece = new Piece(segments, Color.Purple);
            pieces.Add(piece);

            // Set window size
            graphics.PreferredBackBufferWidth = (sizeX * pixelSize);
            graphics.PreferredBackBufferHeight = (sizeY * pixelSize);
        }
        
        protected override void Initialize()
        {
            
            //board.AddPiece(pieces[1].Clone(0, 0));

            //board.currPiece.y -= 10;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Load graphics
            spriteBatch = new SpriteBatch(GraphicsDevice);
            GraphicsDevice.Clear(Color.Black);

            // Initialize tiles
            empty = Content.Load<Texture2D>("empty");
            tile = Content.Load<Texture2D>("tile");
            lostMessage = Content.Load<Texture2D>("lost");
            hold = Content.Load<Texture2D>("hold");
            center_circle = Content.Load<Texture2D>("center_hold");

            // Initialize sound effects
            land = Content.Load<SoundEffect>("Tetris_Land");
            fall = Content.Load<SoundEffect>("Tetris_Fall");
            lost = Content.Load<SoundEffect>("Tetris_Lose");

            // Initialize music
            ThemeA = Content.Load<Song>("Tetris_Full");
            MediaPlayer.IsRepeating = true;

            MediaPlayer.Play(ThemeA);
        }

        protected override void UnloadContent() { }

        protected override void Update(GameTime gameTime)
        {
            // Q to quit
            if (Keyboard.GetState().IsKeyDown(Keys.Q))
                Exit();
            
            // Get key presses
            ks = Keyboard.GetState();

            // Newly pressed keys
            List<Keys> newKeys = new List<Keys>();
            Keys[] pressed = ks.GetPressedKeys();

            // Account for new keys
            foreach (Keys k in pressed)
            {
                if (!accountedKeys.Contains(k))
                {
                    newKeys.Add(k);
                    accountedKeys.Add(k);
                }
            }

            // Remove accounted no longer pressed
            for (int i = 0; i < accountedKeys.Count; i++)
            {
                bool remove = true;
                foreach(Keys pK in pressed)
                {
                    if (accountedKeys[i] == pK)
                    {
                        remove = false;
                        break;
                    }
                }
                if (remove)
                {
                    accountedKeys.RemoveAt(i);
                    i--;
                }
            }

            if (currState == GameState.playing)
            {

                /*      Movement        */

                // Down
                if (accountedKeys.Contains(Keys.S))
                {
                    // Stop fall clock
                    fallCount = 0;

                    // Start hesitation
                    downHes++;

                    // Initial down movement when pressed
                    if (downHes == 1)
                    {
                        if (board.Move(new int[] { 0, -1 }, true))
                        {
                            land.Play();
                        }
                    }
                    // Test if past hesitation threshold and at specific values
                    else if (downHes >= 24 && downHes % 4 == 0)
                    {
                        if (board.Move(new int[] { 0, -1 }, false))
                        {
                            land.Play();
                        }
                    }
                }
                // Otherwise move down by clock
                else
                {
                    // Reset downwards hesitation
                    downHes = 0;

                    // Update fall values
                    fallCount++;
                    fallCount %= 60;

                    // Move down once at a specific value
                    if (fallCount == 59)
                    {
                        if (board.Move(new int[] { 0, -1 }, true))
                        {
                            land.Play();
                        }
                    }
                }

                // Left/Right
                if (newKeys.Contains(Keys.A))
                {
                    if (board.Move(new int[] { -1, 0 }, false))
                    {
                        land.Play();
                    }
                }
                if (newKeys.Contains(Keys.D))
                {
                    if (board.Move(new int[] { 1, 0 }, false))
                    {
                        land.Play();
                    }
                }

                // Instant Place
                if (newKeys.Contains(Keys.W))
                {
                    if (board.Fall())
                    {
                        newPieceCooldown = 25;
                        fall.Play();
                    }
                }

                // Hold Piece
                if (newKeys.Contains(Keys.Space))
                {
                    if (canHold && board.Hold() != null)
                    {
                        canHold = false;
                    }
                }

                // Rotate
                if (newKeys.Contains(Keys.N))
                {
                    int[][] counterClockRot = new int[][] { new int[] { 0, 1 }, new int[] { -1, 0 } };
                    board.Rotate(counterClockRot);
                }
                if (newKeys.Contains(Keys.M))
                {
                    int[][] clockRot = new int[][] { new int[] { 0, -1 }, new int[] { 1, 0 } };
                    board.Rotate(clockRot);
                }

                /*      Update Game     */

                // Clean board of finished lines
                board.Clean();

                // Test if all pieces are below position
                if (!board.IsValid())
                {
                    Lose();
                }

                // If there are no more valid places
                if (board.currPiece == null)
                {
                    newPieceCooldown++;
                    newPieceCooldown %= 30;
                    if (newPieceCooldown == 1)
                    {
                        int randPiece = (int)(random.NextDouble() * pieces.Count);

                        if (!board.AddPiece(pieces[randPiece].Clone(-1, -1)))
                        {
                            Lose();
                        }
                        else
                        {
                            canHold = true;
                        }
                    }
                }
            } else if (currState == GameState.lost)
            {
                if (newKeys.Contains(Keys.N))
                {
                    Exit();
                }
                else if (newKeys.Contains(Keys.Y))
                {
                    currState = GameState.playing;
                    board.Reset();
                    MediaPlayer.Play(ThemeA);
                }
            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            spriteBatch.Begin(SpriteSortMode.Deferred, null, SamplerState.PointClamp, null, null, null);
            Point size = new Point(pixelSize, pixelSize);

            if (currState == GameState.playing)
            {
                DrawBoard(size);
                DrawHold();
            }
            else if (currState == GameState.lost)
            {
                DrawBoard(size);
                DrawHold();
                int scale = 4;
                DrawLoseMessage(new Point(49 * scale, 25 * scale));
            }

            spriteBatch.End();

            base.Draw(gameTime);
        }

        public void DrawBoard(Point size)
        {

            // Draw Board
            for (int x = 0; x < sizeX; x++)
            {
                for (int y = 0; y <= sizeY; y++)
                {
                    Piece piece;
                    if ((piece = board.GetPieceAt(x, sizeY - 1 - y)) != null)
                    {
                        Point pos = new Point(pixelSize * x, pixelSize * y);
                        Rectangle bound = new Rectangle(pos, size);

                        spriteBatch.Draw(tile, bound, piece.color);
                    }
                    else
                    {
                        Point pos = new Point(pixelSize * x, pixelSize * y);
                        Rectangle bound = new Rectangle(pos, size);

                        spriteBatch.Draw(empty, bound, Color.White);
                    }
                }
            }
        }

        public void DrawHold()
        {

            // Sizes
            Point size = new Point(pixelSize, pixelSize);
            int scale = 5;
            Point holdSize = new Point(27 * scale, 9 * scale);

            // Start position
            int left = pixelSize * sizeX + ((pixelSize / 2) * 5);
            int top = 5 * pixelSize;

            // Draw hold title
            Point holdPos = new Point((pixelSize * sizeX) + (pixelSize / 4), (pixelSize / 2));
            Rectangle holdBound = new Rectangle(holdPos, holdSize);
            Color color = Color.White;

            spriteBatch.Draw(hold, holdBound, color);

            // Stop if no piece being held
            if (board.hold == null)
            {
                return;
            }

            // Draw empty spots for hold
            for (int x = -2; x <= 2; x++)
            {
                for (int y = -2;  y <= 2; y++)
                {
                    Point pos = new Point(left + (x * pixelSize), top - (y * pixelSize));

                    Rectangle bound = new Rectangle(pos, size);

                    spriteBatch.Draw(empty, bound, Color.White);
                }
            }

            // Draw segments of hold
            foreach (int[] segments in board.hold.segments)
            {
                Point pos = new Point(left + (segments[0] * pixelSize), top - (segments[1] * pixelSize));

                Rectangle bound = new Rectangle(pos, size);

                spriteBatch.Draw(tile, bound, board.hold.color);
            }

            // Draw circle around center

            Point centerPos = new Point(left, top);
            Rectangle centerBound = new Rectangle(centerPos, size);
            spriteBatch.Draw(center_circle, centerBound, Color.White);
        }

        public void DrawLoseMessage(Point size)
        {
            int winX = sizeX * pixelSize;
            int winY = sizeY * pixelSize;
            int[] mid = { winX / 2, winY / 2 };
            Point pos = new Point(mid[0] - size.X / 2, mid[1] - size.Y / 2);
            Rectangle bound = new Rectangle(pos, size);

            spriteBatch.Draw(lostMessage, bound, Color.White);
        }

        public void Lose()
        {
            currState = GameState.lost;

            MediaPlayer.Stop();
            lost.Play();
        }
    }
}
