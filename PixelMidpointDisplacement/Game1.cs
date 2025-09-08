using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Vector2 = Microsoft.Xna.Framework.Vector2;



namespace PixelMidpointDisplacement
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        WorldContext worldContext;
        
        Texture2D playerSprite;
        Texture2D collisionSprite;


        PhysicsEngine physicsEngine;
        

        Player player;

        int digSize = 1;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            this.TargetElapsedTime = TimeSpan.FromSeconds(1d / 180d);

            worldContext = new WorldContext();
            

            physicsEngine = new PhysicsEngine(worldContext);

            player = new Player(worldContext);
            worldContext.physicsObjects.Add(player);


        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            //Block Textures
            Texture2D airTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1); ;
            Texture2D stoneTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            Texture2D blueBlockTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            Texture2D crimsonBlockTexture = new Texture2D(_graphics.GraphicsDevice, 1, 1);

            stoneTexture.SetData<Color>(new Color[] { Color.Black });
            airTexture.SetData<Color>(new Color[] { Color.White });
            blueBlockTexture.SetData<Color>(new Color[] { Color.Blue });
            crimsonBlockTexture.SetData<Color>(new Color[] { Color.Crimson });

            worldContext.generateIDsFromTextureList(new Texture2D[]{airTexture, stoneTexture, blueBlockTexture, crimsonBlockTexture});


            worldContext.generateWorld((300, 300));
            worldContext.updatePixelsPerBlock(16);

            playerSprite = new Texture2D(_graphics.GraphicsDevice, 1, 1);
            collisionSprite = new Texture2D(_graphics.GraphicsDevice, 1, 1);

            playerSprite.SetData<Color>(new Color[] { Color.Red });
            collisionSprite.SetData<Color>(new Color[] { Color.Green });
            // TODO: use this.Content to load your game content here
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            //As the game's framerate changes, the small impulses caused by the added acceleration changes.
            //So the added values must change with the elapsed game time. All other acceleration is properly controlled inside 
            //of the physics engine


            double horizontalAcceleration = 4; //The acceleration in m/s^-2
            double jumpAcceleration = 12;

            player.inputUpdate(horizontalAcceleration, jumpAcceleration, gameTime.ElapsedGameTime.TotalSeconds);


            for (int i = 0; i < worldContext.physicsObjects.Count; i++)
            {
                //General Physics simulations
                //Order: Acceleration, velocity then location
                worldContext.physicsObjects[i].isOnGround = false;
                physicsEngine.addGravity(worldContext.physicsObjects[i]);
                physicsEngine.computeAccelerationWithAirResistance(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);
                physicsEngine.detectBlockCollisions(worldContext.physicsObjects[i]);
                physicsEngine.computeAccelerationToVelocity(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);
                physicsEngine.applyVelocityToPosition(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);

                //Reset acceleration to be calculated next frame
                worldContext.physicsObjects[i].accelerationX = 0;
                worldContext.physicsObjects[i].accelerationY = 0;
            }

            worldContext.screenSpaceOffset = (-(int)player.x + _graphics.GraphicsDevice.Viewport.Width / 2 - (int)(player.width * worldContext.pixelsPerBlock),
                                              -(int)player.y + _graphics.GraphicsDevice.Viewport.Height / 2 - (int)(player.height * worldContext.pixelsPerBlock));

            //Digging system
            if (Mouse.GetState().ScrollWheelValue/120 != digSize-1) {
                digSize = Mouse.GetState().ScrollWheelValue/120 + 1;
                
            }
            if (Mouse.GetState().LeftButton == ButtonState.Pressed)
            {
                //Find the mouses position on screen, then use the screenoffset to find it's coordinate in grid space
                //Set the block at that location to equal 0

                double mouseXPixelSpace = Mouse.GetState().X - worldContext.screenSpaceOffset.x;
                double mouseYPixelSpace = Mouse.GetState().Y - worldContext.screenSpaceOffset.y;



                int mouseXGridSpace = (int)Math.Floor(mouseXPixelSpace / worldContext.pixelsPerBlock);
                int mouseYGridSpace = (int)Math.Floor(mouseYPixelSpace / worldContext.pixelsPerBlock);

                //Delete Block at that location
                for (int x = 0; x < digSize; x++) {
                    for (int y = 0; y < digSize; y++)
                    {
                        int usedX = x - (int)Math.Floor(digSize / 2.0);
                        int usedY = y - (int)Math.Floor(digSize / 2.0);
                        if (mouseXGridSpace + usedX > 0 && mouseXGridSpace + usedX < worldContext.worldArray.GetLength(0) && mouseYGridSpace + usedY > 0 && mouseYGridSpace + usedY < worldContext.worldArray.GetLength(1))
                        {
                            worldContext.deleteBlock(mouseXGridSpace + usedX, mouseYGridSpace + usedY);
                        }
                    }
                }

            }
            else if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                //Find the mouses position on screen, then use the screenoffset to find it's coordinate in grid space
                //Set the block at that location to equal 0

                double mouseXPixelSpace = Mouse.GetState().X - worldContext.screenSpaceOffset.x;
                double mouseYPixelSpace = Mouse.GetState().Y - worldContext.screenSpaceOffset.y;



                int mouseXGridSpace = (int)Math.Floor(mouseXPixelSpace / worldContext.pixelsPerBlock);
                int mouseYGridSpace = (int)Math.Floor(mouseYPixelSpace / worldContext.pixelsPerBlock);

                //Delete Block at that location
                worldContext.addBlock(mouseXGridSpace, mouseYGridSpace, 1);

            }

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);
            int[,] tempWorldArray = worldContext.worldArray;

            _spriteBatch.Begin();
            for (int x = 0; x < tempWorldArray.GetLength(0); x++)
            {
                for (int y = 0; y < tempWorldArray.GetLength(1); y++)
                {
                    _spriteBatch.Draw(worldContext.getBlockFromID(tempWorldArray[x,y]).texture, new Rectangle(x * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x, y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y, (int)worldContext.pixelsPerBlock, (int)worldContext.pixelsPerBlock), Color.White);
                }
            }


            _spriteBatch.Draw(playerSprite, new Rectangle((int)player.x + worldContext.screenSpaceOffset.x, (int)player.y + worldContext.screenSpaceOffset.y, (int)(player.width * worldContext.pixelsPerBlock), (int)(player.height * worldContext.pixelsPerBlock)), Color.White);


            //drawCollisionBox();

            _spriteBatch.End();
            base.Draw(gameTime);
        }

        public void drawCollisionBox()
        {
            //A version of the collision code. It runs the same basic collision detection system, 
            //but paints a red outline around the blocks that were tested, and colors the blocks that the player is colliding
            //with in green.
            int entityLocationInGridX = (int)Math.Floor(player.x / worldContext.pixelsPerBlock);
            int entityLocationInGridY = (int)Math.Floor(player.y / worldContext.pixelsPerBlock);
            int entityGridWidth = (int)Math.Ceiling((double)player.collider.Width / worldContext.pixelsPerBlock);
            int entityGridHeight = (int)Math.Ceiling((double)player.collider.Height / worldContext.pixelsPerBlock);
            int p = worldContext.pixelsPerBlock;
            for (int x = entityLocationInGridX - 1; x < entityLocationInGridX + entityGridWidth + 1; x++)
            { //A range of x values on either side of the outer bounds of the entity
                for (int y = entityLocationInGridY - 1; y < entityLocationInGridY + entityGridHeight + 1; y++)
                {
                    Rectangle entityCollider = new Rectangle((int)player.x, (int)player.y, player.collider.Width, player.collider.Height);

                    Rectangle blockRect = new Rectangle(x * p, y * p, p, p);
                    if (blockRect.Intersects(entityCollider) && worldContext.worldArray[x, y] != 0)
                    {
                        _spriteBatch.Draw(collisionSprite, new Rectangle(x * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, p, p), Color.White);
                    }
                    _spriteBatch.Draw(playerSprite, new Rectangle(x * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, p, 2), Color.White);
                    _spriteBatch.Draw(playerSprite, new Rectangle(x * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, 2, p), Color.White);
                    _spriteBatch.Draw(playerSprite, new Rectangle(x * p + worldContext.screenSpaceOffset.x, (y + 1) * p + worldContext.screenSpaceOffset.y, p, 2), Color.White);
                    _spriteBatch.Draw(playerSprite, new Rectangle((x + 1) * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, 2, p), Color.White);
                }
            }
        }
    }

    public class WorldGenerator {
        WorldContext worldContext;

        int[,] worldArray;
        int[] surfaceHeight;

        double[,] perlinNoiseArray;
        BlockGenerationVariables[,] brownianMotionArray;


        Block[,] oreArray;
        

        //Perlin Noise Variables:
        int noiseIterations = 8;

        double[] octaveWeights = {
        5,
        0.9,
        0.055,
        0.05,
        0.02,
        0.015,
        0.0075,
        0.00325};

        //Smaller means the blocks are also smaller...
        double frequency = 0.0045;

        //Higher means more solid
        double blockThreshold = 0.9;
        double decreasePerY = 0.005;
        double maximumThreshold = 0.9;//Only decreasing so...
        double minimumThreshold = 0.42;
        //The effect of the absolute y value (from the top of the map) and the relative y value (from the surface)
        double absoluteYHeightWeight = 0.1;
        double relativeYHeightWeight = 1;

        int vectorCount = 6;
        double vectorAngleOffset = (Math.PI);

        //SeededBrownianMotion Variables:
        BlockGenerationVariables[] ores = {
        new BlockGenerationVariables(1, new Block(1), 8, 360),
        new BlockGenerationVariables(0.1, new Block(2), 1, 4, (0.3, 0.6, 0.1, 0.0, 0.0, 0.0, 0.0, 0.0)),
        new BlockGenerationVariables(0.4, new Block(3), 2, 40)
        }; //n-1 where n is the number of blockIds
        int maxAttempts = 15;

    public WorldGenerator(WorldContext wc) {
            worldContext = wc;
        }

        public int[,] generateWorld((int width, int height) worldDimensions) {
            perlinNoiseArray = new double[worldDimensions.width, worldDimensions.height];
            brownianMotionArray = new BlockGenerationVariables[worldDimensions.width, worldDimensions.height];
            worldArray = new int[worldDimensions.width, worldDimensions.height];
            oreArray = new Block[worldDimensions.width, worldDimensions.height];
            surfaceHeight = new int[worldDimensions.width];


            List<(double, double)> initialPoints = new List<(double, double)>() { (0, 900), ((worldContext.pixelsPerBlock * worldArray.GetLength(0) / 2), 100), (worldContext.pixelsPerBlock * worldArray.GetLength(0), 900) }; //Start/end Points must be divisible by the pixelsPerBlock value

            MidpointDisplacementAlgorithm mda = new MidpointDisplacementAlgorithm(initialPoints, 400, 1, 10, 70);

            pointsToBlocks(mda.midpointAlgorithm());

            perlinNoise(worldDimensions, noiseIterations, octaveWeights, frequency, vectorCount, vectorAngleOffset);
            seededBrownianMotion(ores, maxAttempts);
            combineAlgorithms(blockThreshold, decreasePerY, maximumThreshold, minimumThreshold);

            return worldArray;
        }

        public int[] getSurfaceHeight() {
            return surfaceHeight;
        }

        public Block[,] getOreArray()
        {
            return oreArray;
        }

        private void pointsToBlocks(List<(double x, double y)> pointList)
        {
            //Convert each point to within the grid-coordinates, then set the worldArray to 1 wherever each lands
            //Improved implementation: check if the distance between the points is greater than the pixels per block, then interpolate

            //Have to flip the y value

            double distanceBetweenPoints = Math.Sqrt(Math.Pow(2, (pointList[0].x - pointList[1].x) + Math.Pow(2, pointList[0].y - pointList[1].y)));

            int numOfInterpolations = 0;

            if (distanceBetweenPoints > Math.Sqrt(2) * worldContext.pixelsPerBlock)
            {
                numOfInterpolations = (int)(distanceBetweenPoints / worldContext.pixelsPerBlock) - 1;
            }

            for (int i = 0; i < pointList.Count; i++)
            {

                int gridX = (int)Math.Floor(pointList[i].x / worldContext.pixelsPerBlock);
                int gridY = (int)Math.Floor(pointList[i].y / worldContext.pixelsPerBlock);

                if (gridX < 0)
                {
                    gridX = 0;
                }
                else if (gridX >= worldArray.GetLength(0))
                {
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

                if (surfaceHeight[gridX] == null || surfaceHeight[gridX] < gridY)
                {
                    surfaceHeight[gridX] = gridY;
                }

                for (int y = gridY; y < worldArray.GetLength(1); y++)
                {
                    worldArray[gridX, y] = 1;
                }
            }

        }

        //Code from the perlin noise caves:
        private void perlinNoise((int width, int height) worldDimensions, int perlinNoiseIterations, double[] octaveWeights, double frequency, int vectorCount, double vectorAngleOffset)
        {
            PerlinNoise pn = new PerlinNoise(worldDimensions, perlinNoiseIterations, vectorCount, vectorAngleOffset);
            int[] g = generateRandomIntArray();
            perlinNoiseArray = pn.generatePerlinNoise(g, worldDimensions, octaveWeights, frequency);
        }

        private int[] generateRandomIntArray()
        {
            int[] initialArray = new int[256];
            List<int> sortedArray = new List<int>();

            for (int i = 0; i < initialArray.Count(); i++)
            {
                sortedArray.Add(i);
            }
            for (int i = 0; i < initialArray.Count(); i++)
            {
                Random r = new Random();
                int rIndex = r.Next(0, sortedArray.Count());
                initialArray[i] = sortedArray[rIndex];
                sortedArray.RemoveAt(rIndex);
            }

            int[] outputArray = new int[initialArray.Count() * 2];
            for (int i = 0; i < outputArray.Count(); i++)
            {
                outputArray[i] = initialArray[i % 255];
            }

            return outputArray;
        }
        private void seededBrownianMotion(BlockGenerationVariables[] oresArray, int attemptCount)
        {
            SeededBrownianMotion sbm = new SeededBrownianMotion();
            brownianMotionArray = sbm.seededBrownianMotion(brownianMotionArray, oresArray);
            brownianMotionArray = sbm.brownianAlgorithm(brownianMotionArray, attemptCount);
        }
        private void combineAlgorithms(double blockThreshold, double changePerY, double maximumThreshold, double minimumThreshold)
        {
            for (int x = 0; x < worldArray.GetLength(0); x++)
            {
                for (int y = 0; y < worldArray.GetLength(1); y++)
                {
                    if (perlinNoiseArray[x, y] > changeThresholdByDepth(blockThreshold, changePerY, (x, y), maximumThreshold, minimumThreshold))
                    { //If it's above the block threshold, set the block to be air, 
                        worldArray[x, y] = 0;
                    }
                    else if (brownianMotionArray[x, y] != null && worldArray[x, y] == 1) //If the brownian motion defined it, and it's solid from the midpoint generation
                    {
                        worldArray[x, y] = brownianMotionArray[x, y].block.ID;
                    }
                    else
                    {
                        worldArray[x, y] = 0;
                    }
                }
            }
        }

        private double changeThresholdByDepth(double blockThreshold, double changePerY, (double x, double y) position, double maximumThreshold, double minimumThreshold)
        {
            double calculatedYWeight = position.y * absoluteYHeightWeight + (position.y - surfaceHeight[(int)position.x]) * relativeYHeightWeight;
            blockThreshold = blockThreshold - changePerY * calculatedYWeight;
            if (blockThreshold > maximumThreshold)
            {
                blockThreshold = maximumThreshold;
            }
            else if (blockThreshold < minimumThreshold)
            {
                blockThreshold = minimumThreshold;
            }

            return blockThreshold;
        }

    }

    public class WorldContext {
        public int[,] worldArray { get; set; }
        public int[] surfaceHeight { get; set; } //The index is the x value, the value of the array is the actual height of the surface
        public int pixelsPerBlock { get; set; } = 4;

        Block[] blockIds = new Block[4];

        public (int x, int y) screenSpaceOffset { get; set; }

        public List<PhysicsObject> physicsObjects = new List<PhysicsObject>();

        public void generateWorld((int width, int height) worldDimensions) {
            
            worldArray = new int[worldDimensions.width, worldDimensions.height];
            
            surfaceHeight = new int[worldDimensions.width];

            WorldGenerator worldGenerator = new WorldGenerator(this);
            
            worldArray = worldGenerator.generateWorld(worldDimensions);
            surfaceHeight = worldGenerator.getSurfaceHeight();
        }

        public void generateIDsFromTextureList(Texture2D[] textureList) {
            blockIds[0] = new Block(textureList[0]);
            blockIds[1] = new Block(textureList[1]);
            blockIds[2] = new Block(textureList[2]);
            blockIds[3] = new Block(textureList[3]);
        }

        public Block getBlockFromID(int ID) {
            return blockIds[ID];
        }

        public void updatePixelsPerBlock(int newPixelsPerBlock) {
            pixelsPerBlock = newPixelsPerBlock;
            foreach (PhysicsObject obj in physicsObjects) {
                obj.recalculateCollider();
            }
        }
        
        public void deleteBlock(int x, int y) {
            if (worldArray[x, y] != 0)
            {
                worldArray[x, y] = 0;
            }
        }
        public void addBlock(int x, int y, int ID) {
            if (worldArray[x, y] == 0)
            {
                worldArray[x, y] = ID;
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

    public class PhysicsEngine
    {
        //Current bug: When the player collides with a block horizontally, there's a singular frame where they aren't colliding
        //anymore, allowing this annoying rubberbanding visual. Using a basic visual of the collision, it is considered to be
        //colliding with the horizontal block. So it's something else weird.
        bool helpDebug = false;
        public double blockSizeInMeters { get; } = 0.6; //The pixel size in meters can be found by taking this value and dividing it by pixelsPerBlock
        WorldContext wc;

        int horizontalOverlapMin = 2;
        int verticalOverlapMin = 2;

        double gravity = 25;


        public PhysicsEngine(WorldContext worldContext)
        {
            wc = worldContext;
        }
        public void computeAccelerationWithAirResistance(PhysicsObject entity, double timeElapsed)
        {
            int directionalityX;
            int directionalityY;
            //If cases to determine the direction of the current velocity. It can be done purely mathematically but it yeilded /0 errors. The directionality is unimportant when velocity = 0
            if (entity.velocityX > 0)
            {
                directionalityX = 1;
            }
            else
            {
                directionalityX = -1;
            }
            if (entity.velocityY > 0)
            {
                directionalityY = 1;
            }
            else
            {
                directionalityY = -1;
            }
            entity.accelerationX += -(directionalityX * (entity.kX * Math.Pow(entity.velocityX, 2)));
            entity.accelerationY += -(directionalityY * (entity.kY * Math.Pow(entity.velocityY, 2)));
        }
        public void computeAccelerationToVelocity(PhysicsObject entity, double timeElapsed)
        {
            entity.velocityX += (entity.accelerationX) * timeElapsed;
            entity.velocityY += (entity.accelerationY) * timeElapsed;

            

           
            //Sets the velocity to 0 if it is below a threshold. Reduces excessive sliding and causes the drag function to actually reach a halt
            if ((entity.velocityX > 0 && entity.velocityX < entity.minVelocityX) || (entity.velocityX < 0 && entity.velocityX > -entity.minVelocityX))
            {
                entity.velocityX = 0;
            }
            if ((entity.velocityY > 0 && entity.velocityY < entity.minVelocityY) || (entity.velocityY < 0 && entity.velocityY > -entity.minVelocityY))
            {
                entity.velocityY = 0;
            }

        }

        public void addGravity(PhysicsObject entity)
        {
            entity.accelerationY -= gravity;
        }

        public void applyVelocityToPosition(PhysicsObject entity, double timeElapsed)
        {
            //Adds the velocity * time passed to the x and y variables of the entity. Y is -velocity as the y-axis is flipped from in real life (Up is negative in screen space)
            //Converts the velocity into pixel space. This allows for realistic m/s calculations in the actual physics function and then converted to pixel space for the location
            
            entity.updateLocation(entity.velocityX * timeElapsed * (wc.pixelsPerBlock / blockSizeInMeters), -entity.velocityY * timeElapsed * (wc.pixelsPerBlock / blockSizeInMeters));
        }


        public void detectBlockCollisions(PhysicsObject entity)
        {
            helpDebug = false;
            //Gets the blocks within a single block radius around the entity. Detects if they are colliding, then if they are, calls another method
            int entityLocationInGridX = (int)Math.Floor(entity.x / wc.pixelsPerBlock);
            int entityLocationInGridY = (int)Math.Floor(entity.y / wc.pixelsPerBlock);
            int entityGridWidth = (int)Math.Ceiling((double)entity.collider.Width / wc.pixelsPerBlock);
            int entityGridHeight = (int)Math.Ceiling((double)entity.collider.Height / wc.pixelsPerBlock);

            Rectangle entityCollider = new Rectangle((int)entity.x, (int)entity.y, entity.collider.Width, entity.collider.Height);
            int[,] worldArray = wc.worldArray; //A temporary storage of an array to reduce external function calls

            for (int x = entityLocationInGridX - 1; x < entityLocationInGridX + entityGridWidth + 1; x++)
            { //A range of x values on either side of the outer bounds of the entity
                for (int y = entityLocationInGridY - 1; y < entityLocationInGridY + entityGridHeight + 1; y++)
                {
                    if (x >= 0 && y >= 0 && x < worldArray.GetLength(0) && y < worldArray.GetLength(1))
                    {
                        if (worldArray[x, y] != 0) //In game implementation, air can either be null or have a special 'colliderless' block type 
                        {
                            Rectangle blockRect = new Rectangle(x * wc.pixelsPerBlock, y * wc.pixelsPerBlock, wc.pixelsPerBlock, wc.pixelsPerBlock);
                            if (blockRect.Intersects(entityCollider))
                            {
                                (double x, double y) collisionNormal = computeCollisionNormal(entityCollider, blockRect);


                                //If the signs are unequal on either the velocity or the acceleration then the forces should cancel as the resulting motion would be counteracted by the block
                                if (((Math.Sign(collisionNormal.y) != Math.Sign(entity.velocityY) && entity.velocityY != 0) || (Math.Sign(collisionNormal.y) != Math.Sign(entity.accelerationY) && entity.accelerationY != 0)) && collisionNormal.y != 0)
                                {
                                    entity.velocityY -= (1 + entity.bounceCoefficient) * entity.velocityY;
                                    entity.accelerationY -= entity.accelerationY;

                                    if (Math.Sign(collisionNormal.y) > 0)
                                    {
                                        entity.isOnGround = true;
                                    }

                                    if (Math.Sign(collisionNormal.y) > 0)
                                    {
                                        entity.y = blockRect.Y - entityCollider.Height + 1;
                                    }
                                    else
                                    {
                                        entity.y = blockRect.Bottom - 1;
                                    }
                                }

                                if (((Math.Sign(collisionNormal.x) != Math.Sign(entity.velocityX) && entity.velocityX != 0) || (Math.Sign(collisionNormal.x) != Math.Sign(entity.accelerationX) && entity.accelerationX != 0)) && collisionNormal.x != 0)
                                {
                                    helpDebug = true;
                                    

                                    entity.velocityX -= (1 + entity.bounceCoefficient) * entity.velocityX;
                                    entity.accelerationX -= entity.accelerationX;
                                    
                                    if (Math.Sign(collisionNormal.x) > 0)
                                    {
                                        entity.x = blockRect.Right - 1;
                                    }
                                    else
                                    {
                                        entity.x = blockRect.Left - entityCollider.Width + 1;
                                    }
                                    
                                }

                            }
                        }
                    }
                }
            }

        }

        public (double, double) computeCollisionNormal(Rectangle entityCollider, Rectangle blockRect)
        {
            (double x, double y) collisionNormal = (0, 0);
            (int x, int y) approximateCollisionDirection = (entityCollider.Center.X - blockRect.Center.X, entityCollider.Center.Y - blockRect.Center.Y);

            if (approximateCollisionDirection.x <= 0 && approximateCollisionDirection.y <= 0)
            { //Bottom Right from the player
                int verticalOverlap = entityCollider.Bottom - blockRect.Top;
                int horizontalOverlap = entityCollider.Right - blockRect.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {

                    if (verticalOverlap > horizontalOverlap)
                    {

                        return (-1, 0);
                    }
                    else
                    {
                        return (0, 1);
                    }
                }
            }
            else if (approximateCollisionDirection.x >= 0 && approximateCollisionDirection.y <= 0)
            { //Bottom Left from the player
                int verticalOverlap = entityCollider.Bottom - blockRect.Top;
                int horizontalOverlap = blockRect.Right - entityCollider.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {

                    if (verticalOverlap > horizontalOverlap)
                    {

                        return (1, 0);
                    }
                    else
                    {
                        return (0, 1);
                    }
                }
            }
            else if (approximateCollisionDirection.x <= 0 && approximateCollisionDirection.y >= 0)
            { //Top Right from the player
                int verticalOverlap = blockRect.Bottom - entityCollider.Top;
                int horizontalOverlap = entityCollider.Right - blockRect.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {


                    if (verticalOverlap > horizontalOverlap)
                    {
                        return (-1, 0);
                    }
                    else
                    {
                        return (0, -1);
                    }
                }
            }
            else if (approximateCollisionDirection.x >= 0 && approximateCollisionDirection.y >= 0)
            { //Top Left from the player
                int verticalOverlap = blockRect.Bottom - entityCollider.Top;
                int horizontalOverlap = blockRect.Right - entityCollider.Left;
                if (horizontalOverlap < horizontalOverlapMin)
                {
                    horizontalOverlap = 0;
                }
                if (verticalOverlap < verticalOverlapMin)
                {
                    verticalOverlap = 0;
                }
                if (verticalOverlap != 0 || horizontalOverlap != 0)
                {

                    if (verticalOverlap > horizontalOverlap)
                    {

                        return (1, 0);
                    }
                    else
                    {
                        return (0, -1);
                    }
                }
            }
            return collisionNormal;
        }

    }

    public class PhysicsObject
    {
        public double accelerationX { get; set; }
        public double accelerationY { get; set; }

        public double velocityX { get; set; }
        public double velocityY { get; set; }

        public double x { get; set; }
        public double y { get; set; }

        public double kX { get; set; }
        public double kY { get; set; }

        public double bounceCoefficient { get; set; }

        public double minVelocityX { get; set; }
        public double minVelocityY { get; set; }

        public Rectangle collider { get; set; }

        public double width { get; set; }
        public double height { get; set; }

        public WorldContext worldContext;

        public bool isOnGround { get; set; }

        public PhysicsObject(WorldContext wc)
        {
            accelerationX = 0.0;
            accelerationY = 0.0;
            velocityX = 1.0;
            velocityY = 1.0;
            x = 0.0;
            y = 0.0;
            kX = 0.0;
            kY = 0.0;
            bounceCoefficient = 0.0;
            minVelocityX = 0.5;
            minVelocityY = 0.01;
            isOnGround = false;

            collider = new Rectangle(0, 0, wc.pixelsPerBlock, wc.pixelsPerBlock);

            worldContext = wc;
        }

        public void updateLocation(double xChange, double yChange)
        {
            x += xChange;
            y += yChange;
        }

        public void onBlockCollision(int blockX, int blockY)
        {

        }

        public void recalculateCollider() {
            collider = new Rectangle(0, 0, (int)(width * worldContext.pixelsPerBlock), (int)(height * worldContext.pixelsPerBlock));
        }
    }

    public class Player : PhysicsObject
    {
        
        public Player(WorldContext wc) : base(wc)
        {

            x = 10.0;
            y = 10.0;
            //It's weird because k is unitless, however the fact that I'm in the world of pixels/second means that realistic drag coefficients don't work very well. 
            //I think it's because v^2 has a massive change with the pixel to block ratio. My math's is probably just wrong, so I'll merely account for it by using unrealistic numbers
            kX = 8;
            kY = 0.05;
            

            width = 0.9;
            height = 2;
            collider = new Rectangle(0, 0, (int)(width * wc.pixelsPerBlock), (int)(height * wc.pixelsPerBlock));

        }

        public void inputUpdate(double horizontalAcceleration, double jumpAcceleration, double elapsedTime) {
            if (Keyboard.GetState().IsKeyDown(Keys.D))
            {

                accelerationX += horizontalAcceleration / elapsedTime;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.A))
            {
                accelerationX -= horizontalAcceleration / elapsedTime;
            }
            if (Keyboard.GetState().IsKeyDown(Keys.W))
            {
                if (isOnGround)
                {
                    accelerationY += jumpAcceleration / elapsedTime;
                }
            }
            if (Keyboard.GetState().IsKeyDown(Keys.R))
            {
                x = 10;
                y = 10;
                velocityX = 0;
                velocityY = 0;
            }

        }
    }

    public class Block
    {
        public Texture2D texture;
        int emmissiveStrength;
        public int ID;

        public Block(Texture2D texture)
        {
            this.texture = texture;
        }
        public Block(Texture2D texture, int emmissiveStrength)
        {
            this.texture = texture;
            this.emmissiveStrength = emmissiveStrength;
        }
        public Block(int ID) {
            this.ID = ID;
        }
        
        public Block(Block b)
        {
            texture = b.texture;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
        }
    }

    public class BlockGenerationVariables
    {
        public double seedDensity;
        public Block block;
        public int maxSingleSpread;
        public int currentSingleSpread;
        public int oreVeinSpread;

        public int identifier;

        public List<BlockGenerationVariables> veinList = new List<BlockGenerationVariables>();

        public (double north, double northEast, double east, double southEast, double south, double southWest, double west, double northWest) directionWeights = (0.125, 0.125, 0.125, 0.125, 0.125, 0.125, 0.125, 0.125); //Perfectly weighted as default
        public BlockGenerationVariables(double seedDensity, Block block, int maxSingleSpread, int oreVeinSpread, (double north, double northEast, double east, double southEast, double south, double southWest, double west, double northWest) directionWeights)
        {
            this.seedDensity = seedDensity;
            this.block = block;
            this.maxSingleSpread = maxSingleSpread;
            this.currentSingleSpread = maxSingleSpread;
            this.oreVeinSpread = oreVeinSpread;
            this.directionWeights = directionWeights;

        }

        public BlockGenerationVariables(double seedDensity, Block block, int maxSingleSpread, int oreVeinSpread)
        {
            this.seedDensity = seedDensity;
            this.block = block;
            this.maxSingleSpread = maxSingleSpread;
            this.currentSingleSpread = maxSingleSpread;
            this.oreVeinSpread = oreVeinSpread;
        }

        public BlockGenerationVariables(BlockGenerationVariables blockVariables)
        {
            seedDensity = blockVariables.seedDensity;
            block = blockVariables.block;
            maxSingleSpread = blockVariables.maxSingleSpread;
            currentSingleSpread = blockVariables.maxSingleSpread;
            oreVeinSpread = blockVariables.oreVeinSpread;
            directionWeights = blockVariables.directionWeights;
            veinList = blockVariables.veinList;
            identifier = blockVariables.identifier + 2;
        }

        public void hasSpread()
        {
            currentSingleSpread -= 1;
            oreVeinSpread -= 1;
        }

        public void hasSpreadVein(int oreVeinSpread)
        {
            this.oreVeinSpread = oreVeinSpread;
        }

        public void initialiseVeinList(BlockGenerationVariables thisBlock)
        {
            veinList = new List<BlockGenerationVariables>();
            veinList.Add(thisBlock);
        }

        public void updateVeinList(BlockGenerationVariables newBlock)
        {
            if (!veinList.Contains(newBlock))
            {
                veinList.Add(newBlock);
            }
        }

    }

    public class PerlinNoise
    {
        List<double[,]> pixelOctaves = new List<double[,]>();

        Vector2[] randomisedUnitVectors;

        double vectorAngleOffset;
        public PerlinNoise((int outputSizeX, int outputSizeY) outputDimensions, int octaveCount, int vectorCount, double vectorAngleOffset)
        {
            randomisedUnitVectors = new Vector2[vectorCount];
            this.vectorAngleOffset = vectorAngleOffset;
            for (int octaves = 0; octaves < octaveCount; octaves++)
            {
                pixelOctaves.Add(new double[outputDimensions.outputSizeX, outputDimensions.outputSizeY]);
            }
        }

        public void randomiseVectorArray(int[] g)
        {
            double radiansPerIndex = 2 * Math.PI / randomisedUnitVectors.Count();

            for (int i = 0; i < randomisedUnitVectors.Count(); i++)
            {
                randomisedUnitVectors[i] = new Vector2((float)Math.Cos(radiansPerIndex * g[i] + vectorAngleOffset), (float)Math.Sin(radiansPerIndex * g[i] + vectorAngleOffset));
            }
        }


        public double[,] generatePerlinNoise(int[] g, (int noiseOutputSizeX, int noiseOutputSizeY) outputDimensions, double[] octaveWeights, double frequency)
        {
            double[,] noiseOutput = new double[outputDimensions.noiseOutputSizeX, outputDimensions.noiseOutputSizeY];

            randomiseVectorArray(g);

            for (int i = 0; i < pixelOctaves.Count(); i++)
            {
                //pixelOctaves[i] = randomlyInitialisePixelArray(pixelOctaves[i]);
                pixelOctaves[i] = perlinAlgorithm(pixelOctaves[i], frequency * Math.Pow(2, i), g);
                noiseOutput = addNoiseToOutput(noiseOutput, pixelOctaves[i], octaveWeights[i]);
            }


            return noiseOutput;
        }

        public double[,] addNoiseToOutput(double[,] currentNoise, double[,] newNoise, double octaveWeight)
        {
            double[,] cumulatedNoise = new double[currentNoise.GetLength(0), currentNoise.GetLength(1)];

            for (int x = 0; x < currentNoise.GetLength(0); x++)
            {
                for (int y = 0; y < currentNoise.GetLength(1); y++)
                {
                    double cumulateNoise = currentNoise[x, y] + octaveWeight * newNoise[x, y];
                    double boundedNoise = cumulateNoise / (1 + octaveWeight);
                    cumulatedNoise[x, y] = boundedNoise;

                }
            }

            return cumulatedNoise;
        }
        public double[,] perlinAlgorithm(double[,] pixels, double frequency, int[] g)
        {
            for (int x = 0; x < pixels.GetLength(0); x++)
            {
                for (int y = 0; y < pixels.GetLength(1); y++)
                {

                    //Multiply the location by a small 'frequency' value
                    Vector2 location = new Vector2((float)frequency * x, (float)frequency * y);

                    int X = (int)Math.Floor(location.X) % 255;
                    int Y = (int)Math.Floor(location.Y) % 255;

                    float xlocal = (float)(location.X - Math.Floor(location.X));
                    float ylocal = (float)(location.Y - Math.Floor(location.Y));

                    Vector2 topLeft = new Vector2(xlocal, ylocal);
                    Vector2 topRight = new Vector2(xlocal - 1, ylocal);
                    Vector2 bottomLeft = new Vector2(xlocal, ylocal - 1);
                    Vector2 bottomRight = new Vector2(xlocal - 1, ylocal - 1);


                    int topLeftValue = g[g[X] + Y];
                    int topRightValue = g[g[X + 1] + Y];
                    int bottomLeftValue = g[g[X] + Y + 1];
                    int bottomRightValue = g[g[X + 1] + Y + 1];

                    double dotTopLeft = Vector2.Dot(topLeft, getConstantVector(topLeftValue));
                    double dotTopRight = Vector2.Dot(topRight, getConstantVector(topRightValue));
                    double dotBottomLeft = Vector2.Dot(bottomLeft, getConstantVector(bottomLeftValue));
                    double dotBottomRight = Vector2.Dot(bottomRight, getConstantVector(bottomRightValue));



                    double xf = fadeFunction(xlocal);
                    double yf = fadeFunction(ylocal);

                    double noise = Lerp(xf,
                    Lerp(yf, dotTopLeft, dotBottomLeft),
                    Lerp(yf, dotTopRight, dotBottomRight));

                    if (noise > 1)
                    {
                        Console.WriteLine("At this pixel-");
                        Console.WriteLine("Noise: " + noise);
                        Console.WriteLine("xf & yf: " + xf + ", " + yf);
                    }

                    pixels[x, y] = (noise + 1) / 2;
                }
            }

            return pixels;
        }

        public Vector2 getConstantVector(int value)
        {
            Vector2 constantVector;

            constantVector = randomisedUnitVectors[value % randomisedUnitVectors.Count()];

            return constantVector;
        }
        public double[] dotProduct(System.Numerics.Vector2[,] unitSquareVector, System.Numerics.Vector2[,] differenceVector)
        {
            //A method returning a list of dot products in English reading order
            double[] dotProducts = new double[4];

            //Both vector arrays are 2x2 but it is easier to read and implement using for loops
            for (int y = 0; y < unitSquareVector.GetLength(0); y++)
            {
                for (int x = 0; x < unitSquareVector.GetLength(1); x++)
                {
                    dotProducts[x + 2 * y] = Vector2.Dot(unitSquareVector[x, y], differenceVector[x, y]);
                }
            }

            return dotProducts;
        }

        public double fadeFunction(double t)
        {
            //Perlins improved fade function: 6^t5 -15t^4 +10t^3
            double fadeFunctionValue = 6.0 * Math.Pow(t, 5) - 15.0 * Math.Pow(t, 4) + 10.0 * Math.Pow(t, 3);
            return fadeFunctionValue;
        }

        public double Lerp(double t, double v1, double v2)
        {
            double lerp = v1 + t * (v2 - v1);

            return lerp;
        }

    }

    public class SeededBrownianMotion
    {
        public BlockGenerationVariables[,] seededBrownianMotion(BlockGenerationVariables[,] worldArray, BlockGenerationVariables[] ores)
        {
            BlockGenerationVariables[,] seededArray = new BlockGenerationVariables[worldArray.GetLength(0), worldArray.GetLength(1)];
            seededArray = seedArray(worldArray, ores);
            //seededArray = BrownianAlgorithm(seededArray);
            return seededArray;
        }

        public BlockGenerationVariables[,] seedArray(BlockGenerationVariables[,] worldArray, BlockGenerationVariables[] ores)
        {
            //Generate a random number of seeds for each ore, depending on it's seedDensity, 
            //then randomly distribute them inside the world Array
            foreach (BlockGenerationVariables ore in ores)
            {
                int numberOfSeeds = (int)((ore.seedDensity / 100) * worldArray.Length);
                for (int i = 0; i < numberOfSeeds; i++)
                {
                    Random r = new Random();
                    int seedX = r.Next(0, worldArray.GetLength(0) - 1);
                    int seedY = r.Next(0, worldArray.GetLength(1) - 1);
                    //Creates a new class with the same parameters. If it directly equals it just passes a pointer
                    BlockGenerationVariables newBlock = new BlockGenerationVariables(ore);
                    newBlock.initialiseVeinList(newBlock); //Add itself to the Veinlist Array
                    newBlock.identifier = i;
                    worldArray[seedX, seedY] = newBlock;
                }
            }

            return worldArray;
        }

        public BlockGenerationVariables[,] brownianAlgorithm(BlockGenerationVariables[,] worldArray, int attemptCount)
        {
            //It would probably be more efficient to have a seperate array containing only the non-null blocks but I don't know a
            //readable way of doing it (ironic with the line break)
            int attempts = 0;

            while (attempts < attemptCount) //Runs until no changes have been made in that iteration.
            {
                Console.WriteLine("Iterated!");
                bool hasChangedTheArray = false;
                //Read everything from the worldArray but write to the tempArray then equalise at the end
                BlockGenerationVariables[,] tempArray = worldArray.Clone() as BlockGenerationVariables[,];

                for (int x = 0; x < worldArray.GetLength(0); x++)
                {
                    for (int y = 0; y < worldArray.GetLength(1); y++)
                    {
                        (BlockGenerationVariables[,] outputArray, bool hasChanged) output = brownianMotion(worldArray, tempArray, x, y);
                        tempArray = output.outputArray;
                        if (output.hasChanged && !hasChangedTheArray)
                        {
                            hasChangedTheArray = true;
                        }
                    }

                }

                worldArray = tempArray.Clone() as BlockGenerationVariables[,];
                if (!hasChangedTheArray)
                {
                    attempts += 1;
                }
            }

            fill(worldArray);

            return worldArray;
        }

        public (BlockGenerationVariables[,], bool) brownianMotion(BlockGenerationVariables[,] worldArray, BlockGenerationVariables[,] tempArray, int x, int y)
        {
            bool hasChanged = false;
            if (worldArray[x, y] != null)
            {
                BlockGenerationVariables block = worldArray[x, y];
                if (block.currentSingleSpread > 0 && block.oreVeinSpread > 0) //If the block/vein is allowed to spread
                {
                    Random r = new Random();
                    double rValue = r.NextDouble();
                    if (rValue <= block.directionWeights.north)
                    {
                        if (y - 1 >= 0)
                        {
                            if (tempArray[x, y - 1] == null)
                            {
                                tempArray[x, y - 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }

                        }
                    }
                    else if (rValue < block.directionWeights.north + block.directionWeights.northEast)
                    {
                        if (x + 1 < worldArray.GetLength(0) && y - 1 >= 0)
                        {
                            if (tempArray[x + 1, y - 1] == null)
                            {
                                tempArray[x + 1, y - 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east))
                    {
                        if (x + 1 < worldArray.GetLength(0))
                        {
                            if (tempArray[x + 1, y] == null)
                            {
                                tempArray[x + 1, y] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east + block.directionWeights.southEast))
                    {
                        if (y + 1 < worldArray.GetLength(1) && x + 1 < worldArray.GetLength(0)) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x + 1, y + 1] == null)
                            {
                                tempArray[x + 1, y + 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }

                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east + block.directionWeights.southEast + block.directionWeights.south))
                    {
                        if (y + 1 < worldArray.GetLength(1)) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x, y + 1] == null)
                            {
                                tempArray[x, y + 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east + block.directionWeights.southEast + block.directionWeights.south + block.directionWeights.southWest))
                    {
                        if (y + 1 < worldArray.GetLength(1) && x - 1 >= 0) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x - 1, y + 1] == null)
                            {
                                tempArray[x - 1, y + 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else if (rValue < (block.directionWeights.north + block.directionWeights.northEast + block.directionWeights.east + block.directionWeights.southEast + block.directionWeights.south + block.directionWeights.southWest + block.directionWeights.west))
                    {
                        if (x - 1 >= 0) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x - 1, y] == null)
                            {
                                tempArray[x - 1, y] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                    else
                    {
                        if (x - 1 >= 0 && y - 1 >= 0) //Make sure the block is inside the array bounds
                        {
                            if (tempArray[x - 1, y - 1] == null)
                            {
                                tempArray[x - 1, y - 1] = spreadBlock(worldArray, tempArray, (x, y));
                                hasChanged = true;
                            }
                        }

                    }
                }
            }
            return (tempArray, hasChanged);
        }

        public BlockGenerationVariables spreadBlock(BlockGenerationVariables[,] worldArray, BlockGenerationVariables[,] tempArray, (int x, int y) location)
        {
            int x = location.x;
            int y = location.y;
            foreach (BlockGenerationVariables b in worldArray[x, y].veinList)
            {
                b.oreVeinSpread -= 1;    //Sychronise the updated ore size across all the blocks in the vein
            }
            tempArray[x, y].currentSingleSpread -= 1;
            BlockGenerationVariables newBlock = new BlockGenerationVariables(worldArray[x, y]);
            newBlock.updateVeinList(newBlock);
            return newBlock;
        }

        public Block[,] fill(Block[,] blockArray)
        {
            for (int x = 0; x < blockArray.GetLength(0); x++)
            {
                for (int y = 0; y < blockArray.GetLength(1); y++)
                {
                    if (blockArray[x, y] == null)
                    {
                        List<Block> blocks = new List<Block>();
                        List<int> blockCount = new List<int>();
                        for (int xLocal = x - 1; xLocal <= x + 1; xLocal++)
                        {
                            for (int yLocal = y - 1; yLocal <= y + 1; yLocal++)
                            {
                                if (xLocal >= 0 && yLocal >= 0 && xLocal < blockArray.GetLength(0) && yLocal < blockArray.GetLength(1))
                                    if (blockArray[xLocal, yLocal] != null)
                                    {
                                        if (blocks.Contains(blockArray[xLocal, yLocal]))
                                        {
                                            blockCount[blocks.FindIndex(u => u.texture == blockArray[xLocal, yLocal].texture)] += 1;
                                        }
                                        else
                                        {
                                            blocks.Add(blockArray[xLocal, yLocal]);
                                            blockCount.Add(1);
                                        }
                                    }
                            }
                        }
                        if (blocks.Count != 0)
                        {
                            blockArray[x, y] = blocks[blockCount.IndexOf(blockCount.Max())];
                        }

                    }
                }
            }

            return blockArray;
        }

        public BlockGenerationVariables[,] fill(BlockGenerationVariables[,] blockArray)
        {
            for (int x = 0; x < blockArray.GetLength(0); x++)
            {
                for (int y = 0; y < blockArray.GetLength(1); y++)
                {
                    if (blockArray[x, y] == null)
                    {
                        List<Block> blocks = new List<Block>();
                        List<BlockGenerationVariables> blockVariables = new List<BlockGenerationVariables>();
                        List<int> blockCount = new List<int>();
                        for (int xLocal = x - 1; xLocal <= x + 1; xLocal++)
                        {
                            for (int yLocal = y - 1; yLocal <= y + 1; yLocal++)
                            {
                                if (xLocal >= 0 && yLocal >= 0 && xLocal < blockArray.GetLength(0) && yLocal < blockArray.GetLength(1))
                                    if (blockArray[xLocal, yLocal] != null)
                                    {
                                        if (blocks.Contains(blockArray[xLocal, yLocal].block))
                                        {
                                            blockCount[blocks.FindIndex(u => u.texture == blockArray[xLocal, yLocal].block.texture)] += 1;
                                        }
                                        else
                                        {
                                            blocks.Add(blockArray[xLocal, yLocal].block);
                                            blockVariables.Add(blockArray[xLocal, yLocal]);
                                            blockCount.Add(1);
                                        }
                                    }
                            }
                        }
                        if (blocks.Count != 0)
                        {
                            blockArray[x, y] = blockVariables[blockCount.IndexOf(blockCount.Max())];
                        }

                    }
                }
            }

            return blockArray;
        }

    }
}
