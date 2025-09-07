using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;
using System;

namespace PixelMidpointDisplacement
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        public Texture2D blockTexture;
        public Texture2D airTexture;


        WorldContext worldContext;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            //_graphics.IsFullScreen = true;
            _graphics.ApplyChanges();


            worldContext = new WorldContext();
            worldContext.generateWorld(100, 100);
        }

        protected override void Initialize()
        {
            // TODO: Add your initialization logic here

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            blockTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            airTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);

            blockTexture.SetData<Color>(new Color[] { Color.Black});
            airTexture.SetData<Color>(new Color[] { Color.White});

            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // TODO: Add your update logic here

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            // TODO: Add your drawing code here
            _spriteBatch.Begin();

            int[,] tempWorldArray = worldContext.worldArray;

            for (int x = 0; x < tempWorldArray.GetLength(0); x++) {
                for (int y = 0; y < tempWorldArray.GetLength(1); y++) {
                    if (tempWorldArray[x, y] == 0) {
                        _spriteBatch.Draw(airTexture, new Rectangle(x * worldContext.pixelsPerBlock, y * worldContext.pixelsPerBlock, worldContext.pixelsPerBlock, worldContext.pixelsPerBlock), Color.White);
                    } else if (tempWorldArray[x, y] == 1)
                    {
                        _spriteBatch.Draw(blockTexture, new Rectangle(x * worldContext.pixelsPerBlock, y * worldContext.pixelsPerBlock, worldContext.pixelsPerBlock, worldContext.pixelsPerBlock), Color.White);
                    }
                }
            }
            

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }

    public class WorldContext {
        public int[,] worldArray { get; set; }
        public int pixelsPerBlock = 16;
        public void generateWorld(int worldX, int worldY) {
            worldArray = new int[worldX, worldY];

            

            List<(double, double)> initialPoints = new List<(double, double)>() { (0, 300), (1600, 300) }; //Start/end Points must be divisible by the pixelsPerBlock value

            MidpointDisplacementAlgorithm mda = new MidpointDisplacementAlgorithm(initialPoints, 250, 1.3, 8);

            pointsToBlocks(mda.midpointAlgorithm());
        }
        
        public void pointsToBlocks(List<(double x, double y)> pointList) {
            //Convert each point to within the grid-coordinates, then set the worldArray to 1 wherever each lands
            //Improved implementation: check if the distance between the points is greater than the pixels per block, then interpolate

            //Have to flip the y value

            double distanceBetweenPoints = Math.Sqrt(Math.Pow(2, (pointList[0].x - pointList[1].x) + Math.Pow(2, pointList[0].y - pointList[1].y)));

            int numOfInterpolations = 0;

            if (distanceBetweenPoints > Math.Sqrt(2) * pixelsPerBlock) {
                numOfInterpolations = (int)(distanceBetweenPoints / pixelsPerBlock) - 1;
            }

            for (int i = 0; i < pointList.Count; i++) {
                
                int gridX = (int)Math.Floor(pointList[i].x/pixelsPerBlock);
                int gridY = (int)Math.Floor(pointList[i].y / pixelsPerBlock);

                if (gridX < 0)
                {
                    gridX = 0;
                }
                else if (gridX >= worldArray.GetLength(0)) {
                    gridX = worldArray.GetLength(0) - 1;
                }
                if (gridY < 0)
                {
                    gridY = 0;
                }
                else if (gridY >= worldArray.GetLength(1))
                {
                    gridY = worldArray.GetLength(1) - 1;
                }

                for (int y = gridY; y < worldArray.GetLength(1); y++)
                {
                    worldArray[gridX, y] = 1;
                }
            }

        }

    }

    public class MidpointDisplacementAlgorithm
    {
        //An iterative process. Takes in a list of points, returns the same list. Can then be converted to blocks in the worldGeneration function
        List<(double x, double y)> pointList;
        double offset;
        double decayPower;
        int iterations;
        int positiveWeight = 50;
        public MidpointDisplacementAlgorithm(List<(double, double)> initialPoints, double initialOffset, double decayPower, int iterations)
        {
            this.pointList = initialPoints;
            this.offset = initialOffset;
            this.decayPower = decayPower;
            this.iterations = iterations;
        }
        public MidpointDisplacementAlgorithm(List<(double, double)> initialPoints, double initialOffset, double decayPower, int iterations, int positiveWeight)
        {
            this.pointList = initialPoints;
            this.offset = initialOffset;
            this.decayPower = decayPower;
            this.iterations = iterations;
            this.positiveWeight = positiveWeight;
        }

        public List<(double x, double y)> midpointAlgorithm()
        {
            for (int i = 0; i < iterations; i++)
            {
                for (int j = 1; j < pointList.Count; j += 2)
                {
                    (double x, double y) point = calculateMidpoint(j);
                    double thisOffset = generateRandomOffset();
                    pointList.Insert(j, (point.x, point.y + thisOffset));
                }
                offset *= 1 / Math.Pow(2, decayPower);
            }
            return pointList;
        }

        public (double, double) calculateMidpoint(int i)
        {
            
                double midX = (pointList[i-1].x + pointList[i].x) / 2;
                double midY = (pointList[i-1].y + pointList[i].y) / 2;

                return (midX, midY);
        }

        public double generateRandomOffset() {
            Random r = new Random();

            double sign = r.Next(-(100 - positiveWeight), positiveWeight);
            if (sign == 0) { sign = 1; } //To prevent any weird terrain caused by 0 values
            
            return offset * Math.Sign(sign);
        }
    }
}
