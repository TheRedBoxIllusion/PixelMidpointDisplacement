using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * Things to do:
    Rewrite the draw system to be as follows:
        - Draw each block to a "chunk" -a 32 x worldDepth sprite. then draw the chunks that would be rednered to the screen
        - Use the "sourceRect" to draw the segments of the chunk that would be on screen
        - When destroying a block or adding it, just spriteBatch.Draw to that chunk and either clear the space that the block would be, or draw the sprite to that spot in the chunk

        = This ended up being slower than rendering each block individually. Also used up more GPU because it had to store so many render targets

    Biomes:     *Done: But need to blend biome's blocks together better
        - Rewrite the world generation system to generate from "biomes" -a class that defines world generation variables and includes lists of entities, structures and stuff like trees that can spawn in that biome
        - Figure out how to seamlessly join between different biomes both on the surface (should be easy, just share a common vertex for the midpoint displacement algorithm) 
            and caves (No clue!!) other than perhaps keeping one perlin noise generation and only modifying the block Threshold values?

        -Process:
            -> Progress from left to right generating a list of biomes.
                    - Each biome generates a random width 
            -> Pass into a biome an initial point for the MPD algorithm that comes from the biome to the previous biome
            -> One perlin noise map, just each biome defines its own threshold weights
            -> Ore generation is per biome
            -> Later on: Underground biomes? I think that would be a different system and moreso just a seperate pass after the initial surface biomes

        
    Structure Editor:      *Next in list for world Gen
        - Make a program that allows you to place blocks within a sandbox style world and then save it as a .txt file to pass into world gen
            - Saves the block IDs that you place and is an array of the specified dimensions. Somehow this gets read to a structure array file?
    
 */

/*
 * Block IDs for reference:
 * Air
 * Stone
 * Dirt
 * Grass
 
 */

namespace PixelMidpointDisplacement
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        BasicEffect basicEffect;

        public WorldContext worldContext;
        RenderTarget2D spriteRendering;

        RenderTarget2D workingLightMap;
        RenderTarget2D lightMap;

        public Scene currentScene;

        RenderTarget2D world;

        float shadowValue = 0.4f;
        int lightCount = 1;

        Matrix worldMatrix, viewMatrix, projectionMatrix;

        VertexPositionColorTexture[] triangleVertices;

        Texture2D collisionSprite;
        Texture2D redTexture;

        Effect calculateLight;
        Effect combineLightAndColor;
        Effect addLightmaps;

        List<(int x, int y)> currentlyRenderedExposedBlocks = new List<(int x, int y)>();

        short[] ind = { 0, 3, 2, 0, 1, 2 };

        

        bool useShaders = false;
        double toggleCooldown = 0;

        int exposedBlockCount;


        EngineController engineController;

        AnimationController animationController;

        SpriteFont ariel;
        

        //++++++++++++++++++

        bool writeToChat = false;

        string chat;

        string previousAddedCharacters = "";

        double chatCountdown;

        double timeSinceRepeatedLetter;
        // ++++++++++++++++++++++
        string playerAcceleration="";
        Player player;

        int digSize = 1;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            this.TargetElapsedTime = TimeSpan.FromSeconds(1d / 180d);

            engineController = new EngineController();
            animationController = new AnimationController();

            worldContext = new WorldContext(engineController, animationController);
            engineController.initialiseEngines(worldContext);

            List<int> surfaceX = new List<int>();

            currentScene = Scene.MainMenu;
            

            player = new Player(worldContext);

            worldContext.setPlayer(player);
           
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            _graphics.ApplyChanges();

            worldMatrix = Matrix.Identity;
            viewMatrix = Matrix.CreateLookAt(new Vector3(0, 0, 1), Vector3.Zero, Vector3.Up);

            projectionMatrix = Matrix.CreateOrthographicOffCenter(0, 1, 1, 0, 0, 1);

            base.Initialize();
        }

        protected override void LoadContent()
        {

            _spriteBatch = new SpriteBatch(GraphicsDevice);
            basicEffect = new BasicEffect(_graphics.GraphicsDevice);

            worldContext.engineController.lightingSystem.graphics = _graphics;

            basicEffect.World = worldMatrix;
            basicEffect.View = viewMatrix;
            basicEffect.Projection = projectionMatrix;
            

            ariel = Content.Load<SpriteFont>("ariel");
           

            redTexture = new Texture2D(GraphicsDevice, 1, 1);
            redTexture.SetData<Color>(new Color[]{ Color.Red});
            List<Texture2D> spriteSheetList = new List<Texture2D>();
            //Need a better way to ensure that they are in the same order as the spriteSheetIDs enum
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\blockSpriteSheet.png")); //0
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\weaponSpriteSheet.png")); //1
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\blockItemSpriteSheet.png")); //2
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\PlayerSpriteSheet.png")); //3
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\ArrowSpriteSheet.png")); //4
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\MainMenuUISpriteSheet.png")); //5
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\blockBackgroundSpriteSheet.png"));

            worldContext.engineController.spriteController.setSpriteSheetList(spriteSheetList);

            player.setSpriteTexture(worldContext.engineController.spriteController.spriteSheetList[(int)spriteSheetIDs.player]);

            triangleVertices = new VertexPositionColorTexture[4]; //Might not need the 5th item
            triangleVertices[0].TextureCoordinate = new Vector2(0, 0);
            triangleVertices[1].TextureCoordinate = new Vector2(1, 0);
            triangleVertices[2].TextureCoordinate = new Vector2(1, 1);
            triangleVertices[3].TextureCoordinate = new Vector2(0, 1);
            
            Color shadowColor = new Color(shadowValue, shadowValue, shadowValue);
            triangleVertices[0].Color = shadowColor;
            triangleVertices[1].Color = shadowColor;
            triangleVertices[2].Color = shadowColor;
            triangleVertices[3].Color = shadowColor;
            

            basicEffect.VertexColorEnabled = true;
            
            basicEffect.LightingEnabled = false;

            worldContext.generateIDsFromTextureList(new Rectangle[]{new Rectangle(0, 0, 0, 0), new Rectangle(0, 0, 32, 32), new Rectangle(0, 32, 32, 32), new Rectangle(0, 64, 32, 32)});
            
            

            player.setSpriteTexture(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\PlayerSpriteSheet.png"));
            collisionSprite = new Texture2D(_graphics.GraphicsDevice, 1, 1);

            
            collisionSprite.SetData<Color>(new Color[] { Color.Green });


            spriteRendering = new RenderTarget2D(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);
            world = new RenderTarget2D(GraphicsDevice, _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

            
            workingLightMap = new RenderTarget2D(GraphicsDevice, (int)(_graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(_graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision), false, SurfaceFormat.Alpha8, DepthFormat.None);
            lightMap = new RenderTarget2D(GraphicsDevice, (int)(_graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(_graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));
            

         

            calculateLight = Content.Load<Effect>("LightCalculator");
            addLightmaps = Content.Load<Effect>("CombineMasks");
            combineLightAndColor = Content.Load<Effect>("CombineLightAndColor");
        }


        protected override void Update(GameTime gameTime)
        {
            if (currentScene == Scene.MainMenu) {
                //For each interactive UI element, check if the mouse clicked inside it, then run that UI function
                updateMainMenuUI(gameTime);
                updateMainMenuInteractiveUI();
            }
            else if (currentScene == Scene.Game)
            {
                updateChatSystem(gameTime);
                updatePhysicsObjects(gameTime);
                calculateScreenspaceOffset();
                updateDigSystem();
                tickAnimations(gameTime);
                updateEntities(gameTime);
            }

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {
            
            if (currentScene == Scene.MainMenu) {
                GraphicsDevice.SetRenderTarget(world);
                GraphicsDevice.Clear(Color.MidnightBlue);
                _spriteBatch.Begin(samplerState : SamplerState.PointClamp);
                    drawMainMenuUI();
                _spriteBatch.End();
            }
            else if (currentScene == Scene.Game)
            {
                GraphicsDevice.SetRenderTarget(spriteRendering);
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
                    drawBlocks();
                    drawCoords(gameTime);
                    drawDebugInfo();
                    drawChat();
                    drawEntities();
                    drawPlayer();
                    drawAnimatorObjects();
                _spriteBatch.End();

                drawLight();
            }
            GraphicsDevice.SetRenderTarget(null);
            _spriteBatch.Begin();
            
             _spriteBatch.Draw(world, world.Bounds, Color.White);
            
            _spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Main menu update methods
        public void updateMainMenuUI(GameTime gameTime) {
            for (int i = 0; i < worldContext.engineController.UIController.MainMenuUI.Count; i++) {
                worldContext.engineController.UIController.MainMenuUI[i].updateElement(gameTime.ElapsedGameTime.TotalSeconds, this);
            }
        }
        public void updateMainMenuInteractiveUI() {
            for (int i = 0; i < worldContext.engineController.UIController.MainMenuInteractiveUI.Count; i++) {
                if (checkUICollision(worldContext.engineController.UIController.MainMenuInteractiveUI[i])) {
                    worldContext.engineController.UIController.MainMenuInteractiveUI[i].onLeftClick(this);
                }
            }
        }
        public bool checkUICollision(InteractiveUIElement uiElement) {
            if (uiElement.isUIElementActive)
            {
                Rectangle uiElementCollisionRect = uiElement.drawRectangle;
                if (uiElement.alignment == UIAlignOffset.Centre) { uiElementCollisionRect.X += (_graphics.PreferredBackBufferWidth - uiElementCollisionRect.Width) / 2; }
                if (Mouse.GetState().LeftButton == ButtonState.Pressed && new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, 10, 10).Intersects(uiElementCollisionRect))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
        #region Main menu draw methods
        public void drawMainMenuUI() {
            for (int i = 0; i < worldContext.engineController.UIController.MainMenuUI.Count; i++) {
                UIElement uiElement = worldContext.engineController.UIController.MainMenuUI[i];
                Rectangle drawRect = new Rectangle();
                if (uiElement.alignment == UIAlignOffset.TopLeft) { drawRect = uiElement.drawRectangle; }
                else if (uiElement.alignment == UIAlignOffset.Centre) { drawRect = new Rectangle(uiElement.drawRectangle.X + (_graphics.PreferredBackBufferWidth - uiElement.drawRectangle.Width)/2, uiElement.drawRectangle.Y, uiElement.drawRectangle.Width, uiElement.drawRectangle.Height); }
                if (uiElement.isUIElementActive)
                {
                    _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[uiElement.spriteSheetID], drawRect, uiElement.sourceRectangle, Color.White);
                }
            }
        }
        #endregion


        #region Gameplay update methods
        public void updateChatSystem(GameTime gameTime)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.Enter) && chatCountdown < 2.7)
            {
                if (writeToChat)
                {
                    writeToChat = false;
                    chatCountdown = 3;
                    System.Diagnostics.Debug.WriteLine("Write to chat was disabled");
                }
                else
                {
                    writeToChat = true;
                    chatCountdown = 3;
                    chat = "";
                    System.Diagnostics.Debug.WriteLine("write to chat was enabled");
                }
            }
            if (writeToChat)
            {
                if (Keyboard.GetState().GetPressedKeys().Length != 0)
                {
                    Keys k = Keyboard.GetState().GetPressedKeys()[0];

                    if (!(k.ToString().Equals("Enter")))
                    {
                        chatCountdown = 3;
                        if (!k.ToString().Equals(previousAddedCharacters) || timeSinceRepeatedLetter < 0)
                        {
                            string stringToAdd = k.ToString();
                            if (k.ToString().Equals("Space")) { stringToAdd = " "; }
                            if (k.ToString().Equals("Back")) { stringToAdd = ""; chat = chat.Remove(chat.Length - 1); }
                            if (k.ToString().Equals("OemPeriod")) { stringToAdd = "."; }

                            previousAddedCharacters = k.ToString();
                            chat += stringToAdd;

                            if (chat.Length >= 75 && chat.Length % 75 == 0) { chat = chat.Insert(chat.LastIndexOf(' ') + 1, "\n"); }

                            if (previousAddedCharacters.Equals(k.ToString()))
                            {
                                timeSinceRepeatedLetter = 0.2;
                            }
                            else
                            {
                                timeSinceRepeatedLetter = -0.1;
                            }
                        }
                    }

                }
            }
            chatCountdown -= gameTime.ElapsedGameTime.TotalSeconds;

            timeSinceRepeatedLetter -= gameTime.ElapsedGameTime.TotalSeconds;
            player.writeToChat = writeToChat;
            if (Keyboard.GetState().IsKeyDown(Keys.P) && toggleCooldown <= 0)
            {
                useShaders = !useShaders;
                toggleCooldown = 0.2;
            }
            if (toggleCooldown > 0)
            {
                toggleCooldown -= gameTime.ElapsedGameTime.TotalSeconds;
            }
        }
        public void updatePhysicsObjects(GameTime gameTime)
        {
            for (int i = 0; i < worldContext.physicsObjects.Count; i++)
            {
                //General Physics simulations
                //Order: Acceleration, velocity then location
                if (worldContext.physicsObjects[i].calculatePhysics)
                {
                    worldContext.physicsObjects[i].isOnGround = false;

                    engineController.physicsEngine.addGravity(worldContext.physicsObjects[i]);
                    engineController.physicsEngine.computeAccelerationWithAirResistance(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);

                    engineController.physicsEngine.detectBlockCollisions(worldContext.physicsObjects[i]);
                    engineController.physicsEngine.computeAccelerationToVelocity(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);
                    engineController.physicsEngine.applyVelocityToPosition(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);

                    if (i == 0)
                    {
                        playerAcceleration = Math.Round(worldContext.physicsObjects[i].accelerationX, 4) + ", " + Math.Round(worldContext.physicsObjects[i].accelerationY, 4);
                    }

                    //Reset acceleration to be calculated next frame
                    worldContext.physicsObjects[i].accelerationX = 0;
                    worldContext.physicsObjects[i].accelerationY = 0;
                }
            }

        }
        public void calculateScreenspaceOffset()
        {
            worldContext.screenSpaceOffset = (-(int)player.x + _graphics.GraphicsDevice.Viewport.Width / 2 - (int)(player.width * worldContext.pixelsPerBlock),
                                                  -(int)player.y + _graphics.GraphicsDevice.Viewport.Height / 2 - (int)(player.height * worldContext.pixelsPerBlock));

            if (worldContext.screenSpaceOffset.x > -(int)(player.width * worldContext.pixelsPerBlock) - 5)
            {
                worldContext.screenSpaceOffset = (-(int)(player.width * worldContext.pixelsPerBlock) - 5, worldContext.screenSpaceOffset.y);
            }
            else if (worldContext.screenSpaceOffset.x < (-(int)worldContext.worldArray.GetLength(0) * worldContext.pixelsPerBlock + _graphics.GraphicsDevice.Viewport.Width - (int)(player.width * worldContext.pixelsPerBlock)) + worldContext.pixelsPerBlock / 2)
            {
                worldContext.screenSpaceOffset = ((-(int)worldContext.worldArray.GetLength(0) * worldContext.pixelsPerBlock + _graphics.GraphicsDevice.Viewport.Width - (int)(player.width * worldContext.pixelsPerBlock)) + worldContext.pixelsPerBlock / 2, worldContext.screenSpaceOffset.y);
            }

            if (worldContext.screenSpaceOffset.y > -(int)(player.height * worldContext.pixelsPerBlock) - 5)
            {
                worldContext.screenSpaceOffset = (worldContext.screenSpaceOffset.x, -(int)(player.height * worldContext.pixelsPerBlock) - 5);
            }
            else if (worldContext.screenSpaceOffset.y < (-(int)worldContext.worldArray.GetLength(1) * worldContext.pixelsPerBlock + _graphics.GraphicsDevice.Viewport.Height - (int)(player.height * worldContext.pixelsPerBlock)) + worldContext.pixelsPerBlock / 2)
            {
                worldContext.screenSpaceOffset = (worldContext.screenSpaceOffset.x, (-(int)worldContext.worldArray.GetLength(1) * worldContext.pixelsPerBlock + _graphics.GraphicsDevice.Viewport.Height - (int)(player.height * worldContext.pixelsPerBlock)) + worldContext.pixelsPerBlock / 2);
            }
        }
        public void updateDigSystem()
        {
            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift)) { if (Mouse.GetState().ScrollWheelValue / 120 != lightCount - 1) { lightCount = Mouse.GetState().ScrollWheelValue / 120 + 1; } }
            else if (Mouse.GetState().ScrollWheelValue / 120 != digSize - 1)
            {
                digSize = Mouse.GetState().ScrollWheelValue / 120 + 1;

            }
            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                //Find the mouses position on screen, then use the screenoffset to find it's coordinate in grid space
                //Set the block at that location to equal 0

                double mouseXPixelSpace = Mouse.GetState().X - worldContext.screenSpaceOffset.x;
                double mouseYPixelSpace = Mouse.GetState().Y - worldContext.screenSpaceOffset.y;



                int mouseXGridSpace = (int)Math.Floor(mouseXPixelSpace / worldContext.pixelsPerBlock);
                int mouseYGridSpace = (int)Math.Floor(mouseYPixelSpace / worldContext.pixelsPerBlock);

                //Delete Block at that location
                for (int x = 0; x < digSize; x++)
                {
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

        }
        public void tickAnimations(GameTime gameTime)
        {
            animationController.tickAnimation(gameTime.ElapsedGameTime.TotalSeconds);
        }

        public void updateEntities(GameTime gameTime)
        {
            worldContext.engineController.entityController.entityInputUpdate(gameTime.ElapsedGameTime.TotalSeconds);
        }
        #endregion
        #region Gameplay draw methods
        public void drawBlocks()
        {
            exposedBlockCount = 0;
            currentlyRenderedExposedBlocks.Clear();
            for (int x = ((int)-worldContext.screenSpaceOffset.x) / worldContext.pixelsPerBlock - 1; x < ((int)-worldContext.screenSpaceOffset.x + _graphics.PreferredBackBufferWidth) / worldContext.pixelsPerBlock + 1; x++)
            {
                for (int y = ((int)-worldContext.screenSpaceOffset.y) / worldContext.pixelsPerBlock - 1; y < ((int)-worldContext.screenSpaceOffset.y + _graphics.PreferredBackBufferHeight) / worldContext.pixelsPerBlock + 1; y++)
                {
                    if (x > 0 && y > 0 && x < worldContext.worldArray.GetLength(0) && y < worldContext.worldArray.GetLength(1))
                    {
                        if (worldContext.worldArray[x, y].ID != (int)blockIDs.air)
                        {
                            int lightValue = worldContext.lightArray[x, y];
                            if (lightValue > 255)
                            {
                                lightValue = 255;
                            }
                            Color lightLevel = Color.White;
                            if (!useShaders) { lightLevel = new Color(lightValue, lightValue, lightValue); }
                            if (worldContext.exposedBlocks.ContainsKey((x, y))) { currentlyRenderedExposedBlocks.Add((x, y)); }
                            _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[(int)spriteSheetIDs.blocks], new Rectangle(x * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x, y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y, (int)worldContext.pixelsPerBlock, (int)worldContext.pixelsPerBlock), worldContext.worldArray[x, y].sourceRectangle, lightLevel);
                        }
                        else {
                            int lightValue = worldContext.lightArray[x, y];
                            if (lightValue > 255)
                            {
                                lightValue = 255;
                            }
                            Color lightLevel = Color.White;
                            if (!useShaders) { lightLevel = new Color(lightValue, lightValue, lightValue); }
                            _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[(int)spriteSheetIDs.blockBackground], new Rectangle(x * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x, y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y, (int)worldContext.pixelsPerBlock, (int)worldContext.pixelsPerBlock), new Rectangle(0, worldContext.backgroundArray[x, y] * 32, 32, 32), lightLevel);
                        }
                    }
                }
            }
        }

        public void drawDebugInfo()
        {
            if (Mouse.GetState().MiddleButton == ButtonState.Pressed)
            {
                double mouseXPixelSpace = Mouse.GetState().X - worldContext.screenSpaceOffset.x;
                double mouseYPixelSpace = Mouse.GetState().Y - worldContext.screenSpaceOffset.y;



                int mouseXGridSpace = (int)Math.Floor(mouseXPixelSpace / worldContext.pixelsPerBlock);
                int mouseYGridSpace = (int)Math.Floor(mouseYPixelSpace / worldContext.pixelsPerBlock);
                string debugInfo;
                debugInfo = mouseXGridSpace + ", " + mouseYGridSpace;
                debugInfo += " | " + worldContext.worldArray[mouseXGridSpace, mouseYGridSpace];
                debugInfo += " | " + worldContext.lightArray[mouseXGridSpace, mouseYGridSpace];
                debugInfo += " | " + worldContext.surfaceBlocks.Contains((mouseXGridSpace, mouseYGridSpace));
                debugInfo += " | Exposed: " + worldContext.exposedBlocks.ContainsKey((mouseXGridSpace, mouseYGridSpace));
                debugInfo += " | Count: " + exposedBlockCount;
                debugInfo += "\n";
                if (worldContext.worldArray[mouseXGridSpace, mouseYGridSpace].faceVertices != null)
                {
                    foreach (Vector2 v in worldContext.worldArray[mouseXGridSpace, mouseYGridSpace].faceVertices)
                    {
                        debugInfo += v;
                    }
                    debugInfo += "\n";
                    debugInfo += worldContext.worldArray[mouseXGridSpace, mouseYGridSpace].faceDirection;
                }

                _spriteBatch.DrawString(ariel, debugInfo, new Vector2(_graphics.GraphicsDevice.Viewport.Width / 3, 20), Color.BlueViolet);
                Rectangle rect = new Rectangle(0, 0, 10, 10);

            }

        }

        public void drawCoords(GameTime gameTime)
        {
            _spriteBatch.DrawString(ariel, (int)player.x / worldContext.pixelsPerBlock + ", " + (int)player.y / worldContext.pixelsPerBlock, new Vector2(10, 10), Color.BlueViolet);
            _spriteBatch.DrawString(ariel, (int)player.velocityX + ", " + (int)player.velocityY, new Vector2(10, 40), Color.BlueViolet);
            _spriteBatch.DrawString(ariel, playerAcceleration, new Vector2(10, 70), Color.BlueViolet);
            _spriteBatch.DrawString(ariel, (int)(1 / gameTime.ElapsedGameTime.TotalSeconds) + " fps", new Vector2(200, 10), Color.BlueViolet);
            _spriteBatch.DrawString(ariel, worldContext.engineController.lightingSystem.lights.Count + " lights", new Vector2(450, 10), Color.BlueViolet);
        }

        public void drawChat()
        {
            if (chatCountdown > 0 && chat != "")
            {
                _spriteBatch.DrawString(ariel, chat, new Vector2(1000, 10), Color.BlueViolet);
            }
        }

        public void drawEntities()
        {
            for (int i = 0; i < worldContext.engineController.entityController.entities.Count; i++)
            {
                if (worldContext.engineController.entityController.entities[i] != null && worldContext.engineController.entityController.entities[i].spriteAnimator != null)
                {
                    if (worldContext.engineController.entityController.entities[i] != player)
                    {
                        Entity entity = worldContext.engineController.entityController.entities[i];
                        _spriteBatch.Draw(entity.spriteAnimator.spriteSheet, new Rectangle((int)(entity.x - entity.spriteAnimator.sourceOffset.X + worldContext.screenSpaceOffset.x), (int)(entity.y - entity.spriteAnimator.sourceOffset.Y + worldContext.screenSpaceOffset.y), (int)(entity.drawWidth * worldContext.pixelsPerBlock), (int)(entity.drawHeight * worldContext.pixelsPerBlock)), entity.spriteAnimator.sourceRect, Color.White, entity.rotation, entity.rotationOrigin, entity.directionalEffect, 0f);
                    }
                }
            }
        }
        public void drawPlayer()
        {
            _spriteBatch.Draw(player.spriteAnimator.spriteSheet, new Rectangle((int)(player.x - player.spriteAnimator.sourceOffset.X) + worldContext.screenSpaceOffset.x, (int)(player.y - player.spriteAnimator.sourceOffset.Y) + worldContext.screenSpaceOffset.y, (int)(player.drawWidth * worldContext.pixelsPerBlock), (int)(player.drawHeight * worldContext.pixelsPerBlock)), player.spriteAnimator.sourceRect, Color.White, 0f, Vector2.Zero, player.directionalEffect, 0f);
        }

        public void drawAnimatorObjects()
        {
            for (int i = 0; i < animationController.animators.Count; i++)
            {
                Animator a = animationController.animators[i];
                Item owner = a.owner;

                int rotationXOffset = 0;
                int rotationYOffset = 0;
                int positionXOffset = 0;
                if (owner.owner.playerDirection < 0)
                {
                    //If the item is facing towards the negative x, account for flipping the image
                    rotationXOffset = owner.owner.playerDirection * owner.sourceRectangle.Width;
                    positionXOffset = (int)(owner.owner.width * worldContext.pixelsPerBlock); //Might need to modify the collider to account for this change...
                }
                if (owner.verticalDirection < 0)
                {
                    //If the item is facing towards negative y, account for flipping the image
                    rotationYOffset = owner.verticalDirection * owner.sourceRectangle.Height;
                }
                Vector2 origin = new Vector2(owner.owner.playerDirection * owner.origin.X - rotationXOffset, owner.verticalDirection * owner.origin.Y - rotationYOffset);

                _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[owner.spriteSheetID], new Rectangle((int)(owner.owner.x + worldContext.screenSpaceOffset.x + a.currentPosition.xPos + positionXOffset), (int)(owner.owner.y + worldContext.screenSpaceOffset.y + a.currentPosition.yPos), (int)(owner.drawDimensions.width), (int)(owner.drawDimensions.height)), owner.sourceRectangle, Color.White, (float)(owner.owner.playerDirection * (a.currentPosition.rotation)), origin, owner.spriteEffect | owner.owner.directionalEffect, 0f);

            }

        }

        public void drawLight()
        {
            if (useShaders)
            {
                GraphicsDevice.SetRenderTarget(lightMap);
                GraphicsDevice.Clear(new Color(0.1f, 0.1f, 0.1f));

                for (int i = 0; i < worldContext.engineController.lightingSystem.lights.Count; i++)
                {

                    Vector2 lightPosition = new Vector2((float)((worldContext.engineController.lightingSystem.lights[i].x + worldContext.screenSpaceOffset.x)) / _graphics.PreferredBackBufferWidth, (float)((worldContext.engineController.lightingSystem.lights[i].y + worldContext.screenSpaceOffset.y) / _graphics.PreferredBackBufferHeight));
                    calculateShadowMap(worldContext.engineController.lightingSystem.lights[i], lightPosition); //A noticable performance drop at 10 dynamic lights. At 30 lights, it drops to 9-20fps
                    calculateLightmap(worldContext.engineController.lightingSystem.lights[i], lightPosition); //Minor impact on performance
                    addLightmapToGlobalLights(worldContext.engineController.lightingSystem.lights[i]);
                }
                calculateLightedWorld();
            }
            else
            {
                GraphicsDevice.SetRenderTarget(world);
                _spriteBatch.Begin();
                _spriteBatch.Draw(spriteRendering, Vector2.Zero, Color.White);
                _spriteBatch.End();
            }
        }
        #region shader calculation methods
        public void calculateLightmap(IEmissive lightObject, Vector2 lightPosition)
        {
            calculateLight.Parameters["lightIntensity"].SetValue(lightObject.luminosity);
            calculateLight.Parameters["lightColor"].SetValue(lightObject.lightColor);
            calculateLight.Parameters["renderDimensions"].SetValue(new Vector2(lightMap.Width, lightMap.Height));
            calculateLight.Parameters["lightPosition"].SetValue(lightPosition);
            //calculateLight.Parameters["Mask"].SetValue(finalShadowMap);


            GraphicsDevice.SetRenderTarget(lightObject.lightMap);


            _spriteBatch.Begin(effect: calculateLight);
            _spriteBatch.Draw(lightObject.shadowMap, Vector2.Zero, Color.White);
            _spriteBatch.End();

        }
        public void calculateShadowMap(IEmissive lightObject, Vector2 lightPosition)
        {
            int faceCount = 0;
            //A possible performance increase:
            //Instead of drawing every single shadow. Compile a list of VertexPositionColorTextures of all the polygons and convert it to an array. Make an array of inds duplicated repeatedly and shifted by duplicateNumber * length, then render everything in one graphics call?
            GraphicsDevice.SetRenderTarget(lightObject.shadowMap);
            GraphicsDevice.Clear(Color.White);
            RasterizerState rasterizerState1 = new RasterizerState();
            rasterizerState1.CullMode = CullMode.None;
            GraphicsDevice.RasterizerState = rasterizerState1;
            //List<VertexPositionColorTexture> vertexList = new List<VertexPositionColorTexture>();
            //List<short> indList = new List<short>();

            foreach ((int, int) coord in currentlyRenderedExposedBlocks)
            {
                int x = coord.Item1;
                int y = coord.Item2;

                Vector3[] vertexArray = new Vector3[worldContext.worldArray[x, y].faceVertices.Count];
                for (int g = 0; g < vertexArray.Length; g++)
                {
                    vertexArray[g] = new Vector3((worldContext.worldArray[x, y].faceVertices[g].X * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x) / _graphics.PreferredBackBufferWidth, (worldContext.worldArray[x, y].faceVertices[g].Y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) / _graphics.PreferredBackBufferHeight, 0);
                }
                for (int i = 0; i < vertexArray.Length - 1; i++)
                {

                    if (vertexArray[i].X != vertexArray[i + 1].X && vertexArray[i].Y != vertexArray[i + 1].Y) { continue; }

                    faceCount += 1;
                    float xDif1 = (vertexArray[i].X - lightPosition.X);
                    float xDif2 = (vertexArray[i + 1].X - lightPosition.X);
                    float yDif1 = (vertexArray[i].Y - lightPosition.Y);
                    float yDif2 = (vertexArray[i + 1].Y - lightPosition.Y);

                    float setX1 = 1;
                    float setX2 = 1;
                    if (xDif1 == 0)
                    {
                        setX1 = -1;
                    }
                    if (xDif2 == 0)
                    {
                        setX2 = -1;
                    }
                    float gradient1 = yDif1 / xDif1;
                    float gradient2 = yDif2 / xDif2;

                    if (yDif2 > 0 && setX2 * gradient2 + vertexArray[i + 1].Y < 1)
                    {
                        setX2 = 1 / (gradient2);
                    }
                    else if (yDif2 < 0 && setX2 * gradient2 + vertexArray[i + 1].Y > 0)
                    {
                        setX2 = -1 / (gradient2);
                    }
                    if (yDif1 > 0 && setX1 * gradient1 + vertexArray[i].Y < 1)
                    {
                        setX1 = 1 / (gradient1);
                    }
                    else if (yDif1 < 0 && setX1 * gradient1 + vertexArray[i].Y > 0)
                    {
                        setX1 = -1 / (gradient1);
                    }
                    float setY1 = setX1 * gradient1;
                    float setY2 = setX2 * gradient2;

                    if (xDif1 == 0) { setX1 = 0; setY1 = Math.Sign(yDif1); }
                    if (xDif2 == 0) { setX2 = 0; setY2 = Math.Sign(yDif2); }
                    if (yDif1 == 0) { setX1 = Math.Sign(xDif1); setY1 = 0; }
                    if (yDif2 == 0) { setX2 = Math.Sign(xDif2); setY2 = 0; }

                    triangleVertices[0].Position = vertexArray[i];
                    triangleVertices[1].Position = vertexArray[i + 1];
                    triangleVertices[2].Position = new Vector3(setX2, setY2, 0) + vertexArray[i + 1];
                    triangleVertices[3].Position = new Vector3(setX1, setY1, 0) + vertexArray[i];


                    if (faceCount > 0)
                    {
                        foreach (EffectPass pass in basicEffect.CurrentTechnique.Passes)
                        {
                            pass.Apply();

                            GraphicsDevice.DrawUserIndexedPrimitives(
                                PrimitiveType.TriangleStrip,
                                triangleVertices,
                                0,
                                triangleVertices.Length,
                                ind,
                                0,
                                (ind.Length / 3) + 1
                                );
                        }
                    }
                }
            }
        }
        public void addLightmapToGlobalLights(IEmissive lightObject)
        {
            addLightmaps.Parameters["Lightmap"].SetValue(lightMap);

            GraphicsDevice.SetRenderTarget(workingLightMap);

            _spriteBatch.Begin(effect: addLightmaps);
            _spriteBatch.Draw(lightObject.lightMap, Vector2.Zero, Color.White);
            _spriteBatch.End();

            GraphicsDevice.SetRenderTarget(lightMap);
            _spriteBatch.Begin();
            _spriteBatch.Draw(workingLightMap, Vector2.Zero, Color.White);
            _spriteBatch.End();
        }
        public void calculateLightedWorld()
        {
            combineLightAndColor.Parameters["Lightmap"].SetValue(spriteRendering);
            GraphicsDevice.SetRenderTarget(world);
            GraphicsDevice.Clear(Color.White);
            _spriteBatch.Begin(effect: combineLightAndColor, samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(lightMap, world.Bounds, Color.White);
            _spriteBatch.End();
        }
        #endregion
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
                    if (blockRect.Intersects(entityCollider) && worldContext.worldArray[x, y].ID != 0)
                    {
                        _spriteBatch.Draw(collisionSprite, new Rectangle(x * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, p, p), Color.White);
                    }
                    _spriteBatch.Draw(redTexture, new Rectangle(x * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, p, 2), Color.White);
                    _spriteBatch.Draw(redTexture, new Rectangle(x * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, 2, p), Color.White);
                    _spriteBatch.Draw(redTexture, new Rectangle(x * p + worldContext.screenSpaceOffset.x, (y + 1) * p + worldContext.screenSpaceOffset.y, p, 2), Color.White);
                    _spriteBatch.Draw(redTexture, new Rectangle((x + 1) * p + worldContext.screenSpaceOffset.x, y * p + worldContext.screenSpaceOffset.y, 2, p), Color.White);

                }
            }
        }

        #endregion

    }

    public class WorldContext
    {
        /*
         * A class that is passed to all gametime objects. This class contains the arrays that define the world, scaling and any other contextual information required by objects
         * 
         * ==========================================
         * World Context Settings:
         * 
         * - initial pixels per block
         * - pixels per block after world generation
         */

        /*
         *  The worlrd array is an integer containing block data as follows: 
         *  2 bytes store block ID (More than we really need, but having 2^16 possible IDs will be useful)
         *  2 bytes can store individual data such as texture variation (grass for example can use 3 bits to store 
         */

        public Block[,] worldArray { get; set; }
        public int[,] backgroundArray { get; set; }
        public int[,] intWorldArray { get; set; }
        public int[] surfaceHeight { get; set; } //The index is the x value, the value of the array is the actual height of the surface

        public List<(int x, int y)> surfaceBlocks { get; set; }

        public Dictionary<(int x, int y), Block> exposedBlocks = new Dictionary<(int x, int y), Block>();//This list contains all the blocks that are exposed to air, and hence would cast shadows.

        public int[,] lightArray { get; set; }
        public int pixelsPerBlock { get; set; } = 4; //Overwritten by the settings file

        public int pixelsPerBlockAfterGeneration;


        Dictionary<blockIDs, int> intFromBlockID = Enum.GetValues(typeof(blockIDs)).Cast<blockIDs>().ToDictionary(e => e, e => (int)e);
        Dictionary<int, blockIDs> blockIDFromInt = Enum.GetValues(typeof(blockIDs)).Cast<blockIDs>().ToDictionary(e => (int)e, e => e);
        Dictionary<blockIDs, Block> blockFromID = new Dictionary<blockIDs, Block>();

        public (int x, int y) screenSpaceOffset { get; set; }

        public List<PhysicsObject> physicsObjects = new List<PhysicsObject>();

        public EngineController engineController;

        public AnimationController animationController;

        public Player player;

        public string runtimePath { get; set; }

        public WorldContext(EngineController engineController, AnimationController animationController)
        {
            this.engineController = engineController;
            this.animationController = animationController;

            runtimePath = AppDomain.CurrentDomain.BaseDirectory;

            //Load settings from file
            loadSettings();
        }

        private void loadSettings()
        {
            StreamReader sr = new StreamReader(runtimePath + "Settings\\WorldContextSettings.txt");
            sr.ReadLine();
            pixelsPerBlock = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            pixelsPerBlockAfterGeneration = Convert.ToInt32(sr.ReadLine());
        }
        
        public void generateWorld((int width, int height) worldDimensions)
        {

            worldArray = new Block[worldDimensions.width, worldDimensions.height];
            backgroundArray = new int[worldDimensions.width, worldDimensions.height];

            intWorldArray = new int[worldDimensions.width, worldDimensions.height];

            lightArray = new int[worldDimensions.width, worldDimensions.height];

            surfaceHeight = new int[worldDimensions.width];

            surfaceBlocks = new List<(int x, int y)>();




            WorldGenerator worldGenerator = new WorldGenerator(this);

            intWorldArray = worldGenerator.generateWorld(worldDimensions);
            backgroundArray = worldGenerator.getBackgroundArray();
            surfaceHeight = worldGenerator.getSurfaceHeight();
            surfaceBlocks = worldGenerator.getSurfaceBlocks();



            lightArray = engineController.lightingSystem.initialiseLight(worldDimensions, surfaceHeight);
            engineController.lightingSystem.generateSunlight(intWorldArray, surfaceHeight);
            engineController.lightingSystem.calculateSurfaceLight(intWorldArray, surfaceBlocks);


            for (int x = 0; x < worldArray.GetLength(0); x++)
            {
                for (int y = 0; y < worldArray.GetLength(1); y++)
                {
                    generateInstanceFromID(intWorldArray, blockIDFromInt[intWorldArray[x, y]], x, y);
                    addBlockToDictionaryIfExposedToAir(intWorldArray, x, y);

                }
            }


            updatePixelsPerBlock(pixelsPerBlockAfterGeneration);

        }

        public void generateInstanceFromID(int[,] intArray, blockIDs ID, int x, int y)
        {
            if (ID == blockIDs.air || ID == blockIDs.stone || ID == blockIDs.dirt)
            {
                worldArray[x, y] = new Block(blockFromID[ID]);
            }
            else if (ID == blockIDs.grass)
            {
                worldArray[x, y] = new GrassBlock(blockFromID[ID]);
            }

            worldArray[x, y].setupInitialData(intArray, (x, y));
        }

        public void addBlockToDictionaryIfExposedToAir(int[,] blockArray, int x, int y)
        {
            if (x > 0 && y > 0 && x < worldArray.GetLength(0) - 1 && y < worldArray.GetLength(1) - 1)
            {

                if ((blockArray[x - 1, y] == (int)blockIDs.air || blockArray[x + 1, y] == (int)blockIDs.air || blockArray[x, y - 1] == (int)blockIDs.air || blockArray[x, y + 1] == (int)blockIDs.air) && blockArray[x, y] != (int)blockIDs.air) //Then it is exposed to air
                {
                    exposedBlocks.Add((x, y), worldArray[x, y]);
                    worldArray[x, y].setupFaceVertices(calculateExposedFaces(blockArray, x, y));
                }
            }
        }
        public Vector4 calculateExposedFaces(int[,] blockArray, int x, int y)
        {
            return new Vector4(Convert.ToInt32((blockArray[x, y - 1] == (int)blockIDs.air)), Convert.ToInt32(blockArray[x + 1, y] == (int)blockIDs.air), Convert.ToInt32(blockArray[x, y + 1] == (int)blockIDs.air), Convert.ToInt32(blockArray[x - 1, y] == (int)blockIDs.air));
        }

        public void addBlockToDictionaryIfExposedToAir(Block[,] blockArray, int x, int y)
        {
            if (x > 0 && y > 0 && x < worldArray.GetLength(0) - 1 && y < worldArray.GetLength(1) - 1)
            {

                if ((blockArray[x - 1, y].ID == (int)blockIDs.air || blockArray[x + 1, y].ID == (int)blockIDs.air || blockArray[x, y - 1].ID == (int)blockIDs.air || blockArray[x, y + 1].ID == (int)blockIDs.air) && blockArray[x, y].ID != (int)blockIDs.air) //Then it is exposed to air
                {
                    exposedBlocks.Add((x, y), worldArray[x, y]);
                    worldArray[x, y].setupFaceVertices(calculateExposedFaces(blockArray, x, y));
                }
            }
        }

        public Vector4 calculateExposedFaces(Block[,] blockArray, int x, int y)
        {
            return new Vector4(Convert.ToInt32((blockArray[x, y - 1].ID == (int)blockIDs.air)), Convert.ToInt32(blockArray[x + 1, y].ID == (int)blockIDs.air), Convert.ToInt32(blockArray[x, y + 1].ID == (int)blockIDs.air), Convert.ToInt32(blockArray[x - 1, y].ID == (int)blockIDs.air));
        }
        public void generateIDsFromTextureList(Rectangle[] textureSourceList)
        {
            blockFromID.Add(blockIDs.air, new Block(textureSourceList[intFromBlockID[blockIDs.air]], intFromBlockID[blockIDs.air])); //Air block
            blockFromID.Add(blockIDs.stone, new Block(textureSourceList[intFromBlockID[blockIDs.stone]], intFromBlockID[blockIDs.stone]));
            blockFromID.Add(blockIDs.dirt, new Block(textureSourceList[intFromBlockID[blockIDs.dirt]], intFromBlockID[blockIDs.dirt]));
            blockFromID.Add(blockIDs.grass, new GrassBlock(textureSourceList[intFromBlockID[blockIDs.grass]], intFromBlockID[blockIDs.grass]));
        }

        public Block getBlockFromID(blockIDs ID)
        {
            return blockFromID[ID];
        }

        public void updatePixelsPerBlock(int newPixelsPerBlock)
        {
            pixelsPerBlock = newPixelsPerBlock;
            foreach (PhysicsObject obj in physicsObjects)
            {
                obj.recalculateCollider();
            }
        }

        public void setPlayer(Player player) {
            this.player = player;
        }
        public bool deleteBlock(int x, int y)
        {
            if (worldArray[x, y].ID != 0)
            {
                worldArray[x, y].blockDestroyed(exposedBlocks);
                worldArray[x, y] = new Block(blockFromID[blockIDs.air]);
                worldArray[x, y].setLocation((x, y));

                return true;
            }

            return false;
        }
        public bool addBlock(int x, int y, int ID)
        {
            if (worldArray[x, y].ID == intFromBlockID[blockIDs.air])
            {
                worldArray[x, y] = new Block(blockFromID[blockIDFromInt[ID]]);
                worldArray[x, y].setLocation((x, y));
                addBlockToDictionaryIfExposedToAir(worldArray, x, y);
                return true;
            }

            return false;
        }
    }


    public class WorldGenerator {
        public WorldContext worldContext;

        public int[,] worldArray;
        public int[,] backgroundArray;
        public int[] surfaceHeight;
        List<(int x, int y)> surfaceBlocks = new List<(int x, int y)>(); //This list contains all the blocks facing the surface, not only the ones that are highest. Eg. cliff faces
        

        double[,] perlinNoiseArray;
        BlockGenerationVariables[,] brownianMotionArray;

        List<Biome> biomeList = new List<Biome>();
        List<Biome> biomeStencilList = new List<Biome>() {
            new MeadowBiome(),
            new MountainBiome(),
            new MountainBiome()
        };
        int rightMountainRangeWidth = 0;

        int horizonLine = 900;

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
        0.00325
        };

        //Smaller means the blocks are also smaller...
        double frequency = 0.025;
        int vectorCount = 6;
        double vectorAngleOffset = (Math.PI);



    public WorldGenerator(WorldContext wc) {
            worldContext = wc;

            //Load a select few variables pertaining mostly to the perlin noise caves
            //Not all important variables can be loaded (or aren't) just due to the complexity of the system
            loadSettings();
        }

        private void loadSettings() {
            //Load octave count and octave weights, 
            //Load frequency
            //Load vector count and offset
            
            StreamReader sr = new StreamReader(worldContext.runtimePath + "Settings\\WorldGenerationVariables.txt");
            sr.ReadLine();
            noiseIterations = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            octaveWeights = new double[noiseIterations];
            for (int i = 0; i < noiseIterations; i++) {
                octaveWeights[i] = Convert.ToDouble(sr.ReadLine());
            }
            sr.ReadLine();
            frequency = Convert.ToDouble(sr.ReadLine());
            sr.ReadLine();
            vectorCount = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            vectorAngleOffset = Convert.ToDouble(sr.ReadLine());
        }

        public int[,] generateWorld((int width, int height) worldDimensions) {
            perlinNoiseArray = new double[worldDimensions.width, worldDimensions.height];
            brownianMotionArray = new BlockGenerationVariables[worldDimensions.width, worldDimensions.height];
            worldArray = new int[worldDimensions.width, worldDimensions.height];
            backgroundArray = new int[worldDimensions.width, worldDimensions.height];
            
            surfaceHeight = new int[worldDimensions.width];

            

            for (int x = 0; x < worldDimensions.width; x++) {
                surfaceHeight[x] = worldDimensions.height;
            }

            perlinNoise(worldDimensions, noiseIterations, octaveWeights, frequency, vectorCount, vectorAngleOffset);

            generateBiomes(worldDimensions);
            
            calculateSurfaceBlocks();

            convertDirtToGrass();

            return worldArray;
        }

        public void generateBiomes((int width, int height) worldDimensions) {
            //In blocks. The biome offset is in blocks, the points just get converted into pixel space
            int currentLeadingWidth = 0;
            //To generate something on the left. Put it here
            //Blocks aren't generation. What's happening is that there's only blocks at the point when the biomes are generated
            Biome ocean = biomeStencilList[0].generateBiomeCopy((0, horizonLine), this, (0,0), (0, worldDimensions.height));
            
            biomeList.Add(ocean);
            ocean.generateSurfaceTerrain();
            
            ocean.generateOres();
            
            combineAlgorithms((0,0), 0);
            ocean.generateBackground();
            ocean.generateStructures();

            currentLeadingWidth += ocean.biomeDimensions.width;
            
            while (worldDimensions.width - currentLeadingWidth > rightMountainRangeWidth)
            {
                int biomeNumber = new Random().Next(biomeStencilList.Count);
                //Generate a copy of the biome and pass in the rightmost point of the most recent biome terrain in the list 
                
                Biome biome = biomeStencilList[biomeNumber].generateBiomeCopy(biomeList[biomeList.Count - 1].initialPoints[biomeList[biomeList.Count - 1].initialPoints.Count - 1], this, (currentLeadingWidth, 0), (0, worldDimensions.height));
                biomeList.Add(biome);
                biome.generateSurfaceTerrain();
                biome.generateOres();
                combineAlgorithms((currentLeadingWidth, 0), biomeList.Count - 1);
                biome.generateBackground();
                biome.generateStructures();
                currentLeadingWidth += biome.biomeDimensions.width;
            }
            
            //To generate something on the right. Put it here
        }
        public int[] getSurfaceHeight() {
            return surfaceHeight;
        }

        public List<(int x, int y)> getSurfaceBlocks()
        {
            return surfaceBlocks;
        }

        public int[,] getBackgroundArray() {
            return backgroundArray;
        }

        
        private void calculateSurfaceBlocks() {
            for (int x = 0; x < surfaceHeight.Length; x++)
            {
                    surfaceBlocks.Add((x, surfaceHeight[x]));
                    
                    int y = surfaceHeight[x] + 1;
                    bool isStillSurface = true;
                    while (isStillSurface)
                    {
                        isStillSurface = addSurfaceBlock(x, y);
                        
                        if (!isStillSurface && x >=0 && y >= 0 && x < worldArray.GetLength(0) && y < worldArray.GetLength(1)) {
                          
                            surfaceBlocks.Add((x, y)); //If it has determined that a block is no longer on the surface, add the block right below: corners
                        }
                        y++;
                }
                
            }
            
        }

        private bool addSurfaceBlock(int x, int y) {
            if (y >= 0 && y < worldArray.GetLength(1) && x > 0 && x < worldArray.GetLength(0) - 1)
            { //If either side of the block is exposed to air, then add it to the surfaceBlocks list. However, make sure to account for
              //Letting boundary blocks still be checked
                if (worldArray[x - 1, y] == 0 || worldArray[x + 1, y] == 0)
                {
                    surfaceBlocks.Add((x, y));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (x == 0 && y >= 0 && y < worldArray.GetLength(1))
            {
                if (worldArray[x + 1, y] == 0)
                {
                    surfaceBlocks.Add((x, y));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else if (x == worldArray.GetLength(0) - 1 && y >= 0 && y < worldArray.GetLength(1))
            {
                if (worldArray[x - 1, y] == 0)
                {
                    surfaceBlocks.Add((x, y));
                    return true;
                }
                else
                {
                    return false;
                }
            }
            else
            {
                return false;
            }

        }

        private void convertDirtToGrass() {
            for (int i = 0; i < surfaceBlocks.Count; i++) {
                if (surfaceBlocks[i].x > 0 && surfaceBlocks[i].y > 0 && surfaceBlocks[i].x < worldArray.GetLength(0)-1 && surfaceBlocks[i].y < worldArray.GetLength(1) - 1)
                   
                if (worldArray[surfaceBlocks[i].x - 1, surfaceBlocks[i].y] == 0 || worldArray[surfaceBlocks[i].x + 1, surfaceBlocks[i].y] == 0 || worldArray[surfaceBlocks[i].x, surfaceBlocks[i].y - 1] == 0) {
                    //If the block is dirt and on the surface, convert it to grass
                    if (worldArray[surfaceBlocks[i].x, surfaceBlocks[i].y] == 2) {
                        worldArray[surfaceBlocks[i].x, surfaceBlocks[i].y] = 3; 
                    }
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
        
        private void combineAlgorithms((int x, int y) biomeOffset, int biomeIndex)
        {
            Biome biome = biomeList[biomeIndex];
            int maxX = biomeOffset.x + biome.biomeDimensions.width;
            int maxY = biomeOffset.y + biome.biomeDimensions.height;
            if (maxX > worldArray.GetLength(0)) {
                maxX = worldArray.GetLength(0);
            }
            if (maxY > worldArray.GetLength(1))
            {
                maxY = worldArray.GetLength(1);
            }
            for (int x = biomeOffset.x; x < maxX; x++)
            {
                for (int y = biomeOffset.y; y < maxY; y++)
                {
                    //Compute an averaged value of the noise threshold
                    double threshold = biome.changeThresholdByDepth((x, y));
                    //If the block is very close to the border, blend the threshold
                    int blockBlendRange = 10;
                    //Final - initial + initial. Calculate the threshold of the block not in the biome, but at the edgedw
                    
                    if (x - biomeOffset.x < blockBlendRange && biomeIndex > 0) { threshold = biomeList[biomeIndex - 1].changeThresholdByDepth((biomeOffset.x - 1, y)) +  (x - biomeOffset.x) * (threshold - biomeList[biomeIndex - 1].changeThresholdByDepth((biomeOffset.x - 1,y)))/blockBlendRange; }
                    if (perlinNoiseArray[x, y] > threshold)
                    { //If it's above the block threshold, set the block to be air, 
                        worldArray[x, y] = 0;
                       
                    }
                    else if (biome.brownianMotionArray[x - biomeOffset.x, y - biomeOffset.y] != null && worldArray[x, y] == 1) //If the brownian motion defined it, and it's solid from the midpoint generation
                    {
                        worldArray[x, y] = biome.brownianMotionArray[x - biomeOffset.x , y - biomeOffset.y].block.ID;
                        if (worldArray[x, y] != 0)
                        {
                            if (surfaceHeight[x] == null || surfaceHeight[x] > y)
                            {
                                surfaceHeight[x] = y;
                            }

                        }
                    }
                    else
                    {
                        worldArray[x, y] = 0;
                    }
                }
            }
        }

        

    }

    #region Biome Classes
    public class Biome {

        //+++++++++++++++
        //Brownian motion variables:
        public List<BlockThresholdValues> blockThresholdVariables;
        public BlockGenerationVariables[] ores;

        //This is the array of block variables that is generated by the algorithm
        public BlockGenerationVariables[,] brownianMotionArray;

        int maxAttempts = 15;
        //++++++++++++++



        //++++++++++++++
        //Midpoint Displacement variables:
        public List<(double, double)> initialPoints = new List<(double, double)>();
        public double initialIterationOffset;
        public double decayPower;
        public int iterations;
        public int positiveWeight;
        //++++++++++++++

        public List<Entity> spawnableEntities;
        public List<(Structure structure, double density, int yMax, int yMin)> spawnableStructures = new List<(Structure structure, double density, int yMax, int yMin)>();

        public WorldGenerator worldGenerator;

        public int backgroundBlockID;

        (int x, int y) biomeOffset;
        (int width, int height) worldDimensions;
        public (int width, int height) biomeDimensions;

        public Biome((double x, double y) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int x, int y) biomeDimensions) {
            //Initialise various variables
            initialPoints = new List<(double, double)>();
            initialPoints.Add(rightMostTerrainPoint);
            worldGenerator = wg;
            this.biomeOffset = biomeOffset;
            this.biomeDimensions = biomeDimensions;
            
        }
        //For the stencils
        public Biome() { }

        public void generateSurfaceTerrain() {
            MidpointDisplacementAlgorithm mda = new MidpointDisplacementAlgorithm(initialPoints, initialIterationOffset, decayPower, iterations, positiveWeight);
            //Should by nature be in absolute dimensions (aka. don't have to worry about the location of the biome)
            pointsToBlocks(mda.midpointAlgorithm());
            
        }
        public void generateBackground() {
            for (int x = biomeOffset.x; x < biomeOffset.x + biomeDimensions.width; x++) {
                for (int y = biomeOffset.y; y < biomeOffset.y + biomeDimensions.height; y++) {
                    if (x >= 0 && x < worldGenerator.surfaceHeight.Length)
                    {
                        if (y >= worldGenerator.surfaceHeight[x])
                        {
                            worldGenerator.backgroundArray[x, y] = this.backgroundBlockID;
                        }
                        else
                        {
                            worldGenerator.backgroundArray[x, y] = 0;
                        }
                    }
                }
            }
        }
        public void generateStructures() {
            for (int i = 0; i < spawnableStructures.Count; i++) {
                int structureCount = (int)((spawnableStructures[i].density / 100f) * biomeDimensions.width * biomeDimensions.height);
                
                Random r = new Random();
                //Randomsie the amount of structures, within a certain range;
                structureCount = r.Next(structureCount/2, structureCount);
                for (int s = 0; s < structureCount; s++) {
                    
                    //Generate an x and y, ensuring that they are within the biome and the specified spawn dimensions
                    int x = r.Next(biomeOffset.x, biomeOffset.x + biomeDimensions.width);
                    int y = r.Next(biomeOffset.y + spawnableStructures[i].yMin, biomeOffset.y + spawnableStructures[i].yMax);
                   
                    if (x >= 0 && x < worldGenerator.worldArray.GetLength(0) && y + worldGenerator.surfaceHeight[x] >= 0 && y + worldGenerator.surfaceHeight[x] < worldGenerator.worldArray.GetLength(1))
                    {
                        spawnableStructures[i].structure.placeStructure(this, x, y + worldGenerator.surfaceHeight[x]);
                    }
                }
            }
        }

        public void generateOres() {
            brownianMotionArray = new BlockGenerationVariables[biomeDimensions.width, biomeDimensions.height];
            seededBrownianMotion(ores, maxAttempts);
        }
        private void seededBrownianMotion(BlockGenerationVariables[] oresArray, int attemptCount)
        {
            SeededBrownianMotion sbm = new SeededBrownianMotion();
            brownianMotionArray = sbm.seededBrownianMotion(brownianMotionArray, oresArray);
            brownianMotionArray = sbm.brownianAlgorithm(brownianMotionArray, attemptCount);
        }
        
        
        private void pointsToBlocks(List<(double x, double y)> pointList)
        {
            //Convert each point to within the grid-coordinates, then set the worldArray to 1 wherever each lands

            double distanceBetweenPoints = Math.Sqrt(Math.Pow(2, (pointList[0].x - pointList[1].x) + Math.Pow(2, pointList[0].y - pointList[1].y)));

            int numOfInterpolations = 0;

            if (distanceBetweenPoints > Math.Sqrt(2) * worldGenerator.worldContext.pixelsPerBlock)
            {
                numOfInterpolations = (int)(distanceBetweenPoints / worldGenerator.worldContext.pixelsPerBlock) - 1;
            }

            for (int i = 0; i < pointList.Count; i++)
            {

                int gridX = (int)Math.Floor(pointList[i].x / worldGenerator.worldContext.pixelsPerBlock);
                int gridY = (int)Math.Floor(pointList[i].y / worldGenerator.worldContext.pixelsPerBlock);

                if (gridX < 0)
                {
                    gridX = 0;
                }
                else if (gridX >= worldGenerator.worldArray.GetLength(0))
                {
                    gridX = worldGenerator.worldArray.GetLength(0) - 1;
                }
                if (gridY < 0)
                {
                    gridY = 0;
                }
                else if (gridY >= worldGenerator.worldArray.GetLength(1))
                {
                    gridY = worldGenerator.worldArray.GetLength(1) - 1;
                }

                for (int y = gridY; y < worldGenerator.worldArray.GetLength(1); y++)
                {
                    worldGenerator.worldArray[gridX, y] = 1;
                }
            }

        }

        public double changeThresholdByDepth((double x, double y) position) {
            return changeThresholdByDepth(blockThresholdVariables, position);
        }
        private double changeThresholdByDepth(List<BlockThresholdValues> blockThresholdVariables, (double x, double y) position)
        {
            double blockThreshold = 1;

            for (int i = blockThresholdVariables.Count - 1; i >= 0; i--)
            {
                if (position.y >= blockThresholdVariables[i].maximumY)
                {
                    double calculatedYWeight = position.y * blockThresholdVariables[i].absoluteYHeightWeight + (position.y - worldGenerator.surfaceHeight[(int)position.x]) * blockThresholdVariables[i].relativeYHeightWeight;
                    blockThreshold = blockThresholdVariables[i].blockThreshold - blockThresholdVariables[i].decreasePerY * calculatedYWeight;
                    if (blockThreshold > blockThresholdVariables[i].maximumThreshold)
                    {
                        blockThreshold = blockThresholdVariables[i].maximumThreshold;
                    }
                    else if (blockThreshold < blockThresholdVariables[i].minimumThreshold)
                    {
                        blockThreshold = blockThresholdVariables[i].minimumThreshold;
                    }
                    break;
                }
            }

            return blockThreshold;
        }

        public virtual Biome generateBiomeCopy((double, double) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int width, int height) biomeDimensions) {
            return new Biome(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions);
        }
    }
    public class MeadowBiome : Biome {

        public MeadowBiome((double x, double y) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int x, int y) biomeDimensions) : base(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions) {
            //Generate the randomised variables
            initialIterationOffset = 50;
            decayPower = 1.2;
            iterations = 10;
            positiveWeight = 30;

            backgroundBlockID = 1;

            ores = new BlockGenerationVariables[]{
            new BlockGenerationVariables(seedDensity : 1, block : new Block(ID : 2), maxSingleSpread : 8, oreVeinSpread : 360), //Dirt
            new BlockGenerationVariables(0.1, new Block(1), 1, 4, (0.3, 0.6, 0.1, 0.0, 0.0, 0.0, 0.0, 0.0)),
            new BlockGenerationVariables(0.3, new Block(1), 6, 24)
            };


            blockThresholdVariables = new List<BlockThresholdValues>(){
            new BlockThresholdValues(blockThreshold : 0.9, maximumY : 0, decreasePerY : 0.005, maximumThreshold : 0.9, minimumThreshold : 0.48, absoluteYHeightWeight : 0, relativeYHeightWeight : 1),
            new BlockThresholdValues(0.9, 130, 0.005, 0.9, 0.48, 0.3, 1),

            new BlockThresholdValues(0.9, 150, 0.01, 0.9, 0.48, 1, 0),

            new BlockThresholdValues(0.9, 200, 0.005, 0.9, 0.48, 0.2, 1),
            new BlockThresholdValues(0.9, 210, 0.005, 0.9, 0.48, 0, 1)
            };

            spawnableStructures = new List<(Structure structure, double density, int yMax, int yMin)>()
            {
                (new Structure("StructTest"), 0.05, biomeDimensions.y, 0)
            };

            //Generate a random biome Width:
            Random r = new Random();

            int biomeVariant = r.Next(2);
            switch (biomeVariant) {
                case 0:
                    this.biomeDimensions.width = r.Next(100, 200);
                    
                    break;
                case 1:
                    this.biomeDimensions.width = r.Next(200, 350);
                    break;
            }

            initialPoints.Add((rightMostTerrainPoint.x + this.biomeDimensions.width * wg.worldContext.pixelsPerBlock, rightMostTerrainPoint.y));
        }
        public MeadowBiome() { }
        public override Biome generateBiomeCopy((double, double) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int width, int height) biomeDimensions)
        {
            return new MeadowBiome(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions);
        }
    }
    public class MountainBiome : Biome {
        public MountainBiome((double x, double y) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int x, int y) biomeDimensions) : base(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions)
        {
            //Generate the randomised variables
            iterations = 10;
            decayPower = 0.9;
            positiveWeight = 80;
            initialIterationOffset = 100;

            backgroundBlockID = 1;

            ores = new BlockGenerationVariables[] {
                new BlockGenerationVariables(1, new Block((int)blockIDs.stone), 8, 80),
                new BlockGenerationVariables(0.1, new Block((int)blockIDs.dirt), 3, 10)
            };

            blockThresholdVariables = new List<BlockThresholdValues> {
                new BlockThresholdValues(blockThreshold : 0.9, maximumY : 0, decreasePerY : 0.005, maximumThreshold : 0.9, minimumThreshold : 0.45, absoluteYHeightWeight : 0.3, relativeYHeightWeight : 0.7 ),
                new BlockThresholdValues(blockThreshold : 0.48, maximumY : 400, decreasePerY : 0.001, maximumThreshold : 0.48, minimumThreshold : 0.4, absoluteYHeightWeight : 0, relativeYHeightWeight : 1)
            };

            

            this.biomeDimensions.width = new Random().Next(200, 400);


            initialPoints.Add((rightMostTerrainPoint.x + (this.biomeDimensions.width * wg.worldContext.pixelsPerBlock)/2, rightMostTerrainPoint.y - 500)); //A central peak
            initialPoints.Add((rightMostTerrainPoint.x + this.biomeDimensions.width * wg.worldContext.pixelsPerBlock, rightMostTerrainPoint.y));
        }

        public MountainBiome() { }

        public override Biome generateBiomeCopy((double, double) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int width, int height) biomeDimensions)
        {
            return new MountainBiome(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions);
        }

    }
    #endregion
    #region Structure Classes
    public class Structure {
        public string structureName;
        int[,] structureArray;
        int[,] structureBackgroundArray;
        public Structure(string structureName) {
            this.structureName = structureName;
            importStructure();
        }
        public void importStructure() {
            try
            {

                StreamReader sr = new StreamReader(AppDomain.CurrentDomain.BaseDirectory + "Structures\\" + structureName + ".txt");
                int width = Convert.ToInt32(sr.ReadLine());
                int height = Convert.ToInt32(sr.ReadLine());
                structureArray = new int[width, height];
                structureBackgroundArray = new int[width, height];

                int y = 0;
                int x = 0;
                string lineToRead = sr.ReadLine();
                while (lineToRead != "++")
                {

                    if (lineToRead.Equals("-"))
                    {
                        y += 1;
                        x = 0;
                    }
                    else
                    {
                        structureArray[x, y] = Convert.ToInt32(lineToRead);
                        x++;
                    }
                    lineToRead = sr.ReadLine();
                }
                y = 0;
                x = 0;
                lineToRead = sr.ReadLine();
                while (lineToRead != null) {
                    if (lineToRead.Equals("-"))
                    {
                        y += 1;
                        x = 0;
                    }
                    else
                    {
                        structureBackgroundArray[x, y] = Convert.ToInt32(lineToRead);
                        x++;
                    }
                    lineToRead = sr.ReadLine();
                }

            }
            catch { }
        }

        public void placeStructure(Biome currentBiome, int xLoc, int yLoc) {
            for (int x = 0; x < structureArray.GetLength(0); x++) {
                for (int y = 0; y < structureArray.GetLength(1); y++) {
                    if (structureArray[x, y] != 0)
                    {
                        if (x + xLoc >= 0 && y + yLoc >= 0 && x + xLoc < currentBiome.worldGenerator.worldArray.GetLength(0) && y + yLoc < currentBiome.worldGenerator.worldArray.GetLength(1))
                        {
                            currentBiome.worldGenerator.worldArray[x + xLoc, y + yLoc] = structureArray[x, y] - 1;
                        }
                    }
                }
            }

            for (int x = 0; x < structureBackgroundArray.GetLength(0); x++)
            {
                for (int y = 0; y < structureBackgroundArray.GetLength(1); y++)
                {
                    if (structureBackgroundArray[x, y] != 0)
                    {
                        if (x + xLoc >= 0 && y + yLoc >= 0 && x + xLoc < currentBiome.worldGenerator.backgroundArray.GetLength(0) && y + yLoc < currentBiome.worldGenerator.backgroundArray.GetLength(1))
                        {
                            currentBiome.worldGenerator.backgroundArray[x + xLoc, y + yLoc] = structureBackgroundArray[x, y] - 1;
                        }
                    }
                }
            }

        }
    
    }
    #endregion
    public class LightingSystem
    {
        public int[,] lightArray { get; set; }
        WorldContext wc;
        Vector2 lightDirection = new Vector2(0.9f, 1);
        int sunBrightness = 1024;
        int shadowBrightness = 200;
        int darkestLight = 0;

        public double shaderPrecision = 0.75;
        public GraphicsDeviceManager graphics;

        double scalar = 0.8;
        double emmissiveScalar = 0.5;

        bool accummulateLight = true;

        public List<IEmissive> lights = new List<IEmissive>();

        public LightingSystem(WorldContext worldContext)
        {
            wc = worldContext;

            //Load settings from file
            loadSettings();
        }

        private void loadSettings() {
            StreamReader sr = new StreamReader(wc.runtimePath + "Settings\\LightingSystemSettings.txt");
            sr.ReadLine();
            double sunlightX = Convert.ToDouble(sr.ReadLine());
            double sunlightY = Convert.ToDouble(sr.ReadLine());
            lightDirection = new Vector2((float)sunlightX, (float)sunlightY);
            sr.ReadLine();
            sunBrightness = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            shadowBrightness = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            darkestLight = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            scalar = Convert.ToDouble(sr.ReadLine());
            sr.ReadLine();
            emmissiveScalar = Convert.ToDouble(sr.ReadLine());
            sr.ReadLine();
            accummulateLight = Convert.ToBoolean(sr.ReadLine());
        }

        
        public void generateSunlight(int[,] worldArray, int[] surfaceLevel)
        {
            for (int startingX = 0; startingX < lightArray.GetLength(0); startingX++)
            {
                calculateLightRay(startingX, 0, worldArray, surfaceLevel);
            }
            if (lightDirection.X > 0)
            {
                for (int startingY = 0; startingY < surfaceLevel[0]; startingY++)
                {
                    calculateLightRay(0, startingY, worldArray, surfaceLevel);
                }
            }
            else if (lightDirection.X < 0)
            {
                for (int startingY = 0; startingY < surfaceLevel[worldArray.GetLength(0) - 1]; startingY++)
                {
                    calculateLightRay(worldArray.GetLength(1) - 1, startingY, worldArray,  surfaceLevel);
                }
            }
            
        }
        public void calculateLightRay(int startingX, int startingY, int[,] worldArray, int[] surfaceLevel)
        {
            int stepCount = 0;
            bool hasCollidedWithABlock = false;
            while (!hasCollidedWithABlock)
            {
                int x = startingX + (int)(stepCount * lightDirection.X);
                int y = startingY + (int)(stepCount * lightDirection.Y);
                if (x >= 0 && x < worldArray.GetLength(0) && y >= 0 && y < worldArray.GetLength(1))
                {
                    if (worldArray[x, y] == 0)
                    {
                        int xCheck = (int)Math.Round(x - lightDirection.X);
                        int yCheck = (int)Math.Round(y - lightDirection.Y);
                        if (xCheck >= 0 && xCheck < worldArray.GetLength(0) && yCheck >= 0 && yCheck < worldArray.GetLength(1))
                        {
                            if (worldArray[xCheck, y] == 0 || worldArray[x, yCheck] == 0)
                            {
                                lightArray[x, y] = sunBrightness;
                            }
                            else
                            {
                                hasCollidedWithABlock = true;
                            }
                        }
                        else
                        {
                            lightArray[x, y] = sunBrightness;
                        }


                    }
                    else
                    {
                        hasCollidedWithABlock = true;
                    }
                }
                else { hasCollidedWithABlock = true; }
                stepCount++;
            }

        }

        public int[,] initialiseLight((int width, int height) worldDimensions, int[] surfaceLevel) {
            lightArray = new int[worldDimensions.width, worldDimensions.height];
            for (int x = 0; x < lightArray.GetLength(0); x++) {
                for (int y = 0; y < lightArray.GetLength(1); y++) {
                    lightArray[x, y] = darkestLight;
                }
            }

            for (int x = 0; x < lightArray.GetLength(0); x++) {
                for (int y = 0; y < surfaceLevel[x]; y++) {
                    lightArray[x, y] = shadowBrightness;
                }
            }

            return lightArray;
        }

        public void calculateSurfaceLight(int[,] worldArray, List<(int x, int y)> surfaceLevel) {
            //From i = P/4 * Pi * r^2
            //r = Sqrt(P/0.9 * 4 * PI)
            
            int maxDepthSunlight = (int)Math.Sqrt(sunBrightness/25 * 4 * Math.PI);
            
            for (int i = 0; i < surfaceLevel.Count; i++) {
                int lastX = (int)Math.Round(surfaceLevel[i].x - lightDirection.X);
                int lastY = (int)Math.Round(surfaceLevel[i].y - lightDirection.Y);
                
                if (lastX >= 0 && lastY >= 0 && lastX < lightArray.GetLength(0) && lastY < lightArray.GetLength(1))
                {

                    int surfaceBrightness = lightArray[lastX, lastY];
                        for (int j = 0; j < maxDepthSunlight; j++)
                        {
                            int lightLevel;
                            if (j != 0)
                            {
                                lightLevel = (int)(surfaceBrightness / (4 * Math.PI * Math.Pow(j, 2)));
                            }
                            else {
                                lightLevel = surfaceBrightness;
                            }
                            int changedX = (int)Math.Round(surfaceLevel[i].x + lightDirection.X * j);
                            int changedY = (int)Math.Round(surfaceLevel[i].y + lightDirection.Y * j);
                            if (changedX >= 0 && changedY >= 0 && changedX < lightArray.GetLength(0) && changedY < lightArray.GetLength(1))
                            {
                                if (worldArray[changedX, changedY] != 0 && lightArray[changedX, changedY] < lightLevel/scalar)
                                {
                                    lightArray[changedX, changedY] = (int)(lightLevel/scalar);
                                }
                            }
                            
                        }
                    
                }
            }

            
        }

        public int[,] calculateLightMap(int emmissiveness) {
            int maxImpact = (int)(Math.Sqrt(emmissiveness / 25 * 4 * Math.PI)/emmissiveScalar);
            
            int[,] lightMap = new int[maxImpact, maxImpact]; //I think I can technically shorten this to being a singular array only the width of the max impact and just 'rotate' it around to account for it's sphereical influence. However this sounds horrid so I won't
            for (int x = 0; x < maxImpact; x++) {
                for (int y = 0; y < maxImpact; y++) {
                    lightMap[x, y] = 0;
                }
            }
            for (int x = 0; x <  maxImpact; x++)
            {
                for (int y = 0; y <  maxImpact; y++)
                {
                    int distance = (int)Math.Sqrt(Math.Pow(x - maxImpact / 2, 2) + Math.Pow(y - maxImpact / 2, 2));
                    if (distance <= maxImpact) {
                        int intensity = emmissiveness;
                        if (distance != 0)
                        {
                            intensity = (int)((emmissiveness / (4 * Math.PI * Math.Pow(distance * emmissiveScalar, 2))));
                            if (intensity > emmissiveness) { intensity = emmissiveness; }
                        }
                        
                            lightMap[x, y] = intensity;
                    }
                }
            }
            return lightMap;
        }
    
        public void movedLight(int lightX, int lightY, int xChange, int yChange, int[,] lightMap, int emmissiveMax)
        {
            int[,] newLightMap = new int[lightMap.GetLength(0) + Math.Abs(xChange), lightMap.GetLength(1) + Math.Abs(yChange)];


            for (int x = 0; x < newLightMap.GetLength(0); x++)
            {
                for (int y = 0; y < newLightMap.GetLength(1); y++)
                {
                    newLightMap[x, y] = 0;
                }
            }

            
            int addAtX = 0;
            int addAtY = 0;
            int subtractAtX = 0;
            int subtractAtY = 0;

            if (xChange != 0 && xChange > 0)
            {
                addAtX = 1;
                subtractAtX = 0;
            }
            else if (xChange != 0 && xChange < 0) {
                addAtX = 0;
                subtractAtX = 1;
            }
            if (yChange != 0 && yChange > 0)
            {
                addAtY = 1;
                subtractAtY = 0;
            }
            else if (yChange != 0 && yChange < 0)
            {
                addAtY = 0;
                subtractAtY = 1;
            }

            newLightMap = add2DArray(lightMap, newLightMap, addAtX, addAtY, 1);
            if (!accummulateLight) { newLightMap = add2DArray(lightMap, newLightMap, subtractAtX, subtractAtY, -1); }


            //Add the newLightMap to the lightMap array
            lightArray = add2DArray(newLightMap, lightArray, lightX - (int)Math.Floor(lightMap.GetLength(0)/2.0) - subtractAtX, lightY - (int)Math.Floor(lightMap.GetLength(1) / 2.0) - subtractAtY, 1, emmissiveMax);

        }

        private int[,] add2DArray(int[,] sourceArray, int[,] arrayToBeAddedTo, int xOffset, int yOffset, int valueMultiplier) {
            for (int x = 0; x < sourceArray.GetLength(0); x++) {
                for (int y = 0; y < sourceArray.GetLength(1); y++) {
                    if(x + xOffset >= 0 && x + xOffset < arrayToBeAddedTo.GetLength(0) && y + yOffset >= 0 && y + yOffset < arrayToBeAddedTo.GetLength(1))
                    arrayToBeAddedTo[x + xOffset, y + yOffset] += valueMultiplier * sourceArray[x, y];
                }
            }
            return arrayToBeAddedTo;
        }
        private int[,] add2DArray(int[,] sourceArray, int[,] arrayToBeAddedTo, int xOffset, int yOffset, int valueMultiplier, int maxLightValue)
        {
            for (int x = 0; x < sourceArray.GetLength(0); x++)
            {
                for (int y = 0; y < sourceArray.GetLength(1); y++)
                {
                    if (x + xOffset >= 0 && x + xOffset < arrayToBeAddedTo.GetLength(0) && y + yOffset >= 0 && y + yOffset < arrayToBeAddedTo.GetLength(1))
                    {
                        if (arrayToBeAddedTo[x + xOffset, y + yOffset] + valueMultiplier * sourceArray[x,y] > maxLightValue && accummulateLight)
                        {
                            sourceArray[x,y] = (maxLightValue - arrayToBeAddedTo[x + xOffset, y + yOffset])/valueMultiplier;
                            if (sourceArray[x, y] < 0) {
                                sourceArray[x, y] = 0;
                            }
                        }
                        arrayToBeAddedTo[x + xOffset, y + yOffset] += valueMultiplier * sourceArray[x, y];
                        
                    }
                }
            }
            return arrayToBeAddedTo;
        }
    }

    public class EngineController {
        public LightingSystem lightingSystem;
        public PhysicsEngine physicsEngine;
        public CollisionController collisionController;
        public EntityController entityController;
        public SpriteController spriteController;
        public UIController UIController;

        public WorldContext worldContext;
        

        public void initialiseEngines(WorldContext wc) {
            worldContext = wc;
            lightingSystem = new LightingSystem(wc);
            physicsEngine = new PhysicsEngine(wc);
            collisionController = new CollisionController();
            entityController = new EntityController();
            spriteController = new SpriteController();
            UIController = new UIController();
        }

      

    }
    public class CollisionController
    {
        public List<IActiveCollider> activeColliders;
        public List<IPassiveCollider> passiveColliders;

        public CollisionController() {
            activeColliders = new List<IActiveCollider>();
            passiveColliders = new List<IPassiveCollider>();
        }

        public void checkCollisions() {
            if (activeColliders.Count != 0 && passiveColliders.Count != 0) {
                for (int a = 0; a < activeColliders.Count; a++) {
                    for (int p = 0; p < passiveColliders.Count; p++) {
                        if (activeColliders[a].isActive && passiveColliders[p].isActive) {
                            
                        }
                    }
                } 
            }
        }

        public void addActiveCollider(IActiveCollider collider)
        {
            if (!activeColliders.Contains(collider))
            {
                activeColliders.Add(collider);
            }
        }
    }
    public class UIController {
        public List<UIElement> MainMenuUI = new List<UIElement>();
        public List<InteractiveUIElement> MainMenuInteractiveUI = new List<InteractiveUIElement>();

        public UIController() {
            resetMainMenuUI();
        }
        private void resetMainMenuUI() {
            MainMenuUI.Clear();
            MainMenuInteractiveUI.Clear();
            MainMenuTitle title = new MainMenuTitle();
            MainMenuWorldGenText generationText = new MainMenuWorldGenText();
            MainMenuStartButton start = new MainMenuStartButton(generationText);
            MainMenuUI.Add(title);
            MainMenuUI.Add(start);
            MainMenuUI.Add(generationText);
            MainMenuInteractiveUI.Add(start);
        }
    }
    #region UI classes
    public enum UIAlignOffset {
        TopLeft,
        Centre
    }
    public class UIElement {
        public int spriteSheetID;
        public float rotation = 0;
        public Vector2 rotationOrigin = Vector2.Zero;
        public float scale = 1;
        public SpriteEffects effect;
        public Rectangle sourceRectangle;
        public Rectangle drawRectangle;
        public UIAlignOffset alignment;
        public bool isUIElementActive = true;

        public virtual void updateElement(double elasedTime, Game1 game) { }
    }
    public class InteractiveUIElement : UIElement {
        public virtual void onLeftClick(Game1 game) { }
        public virtual void onRightClick(Game1 game) { }
    }
    public class MainMenuTitle : UIElement {
        public MainMenuTitle() {
            spriteSheetID = (int)spriteSheetIDs.mainMenuUI;
            drawRectangle = new Rectangle(0,50,1160, 152);
            sourceRectangle = new Rectangle(0,0,145,19);
            alignment = UIAlignOffset.Centre;
        }
    }
    public class MainMenuStartButton : InteractiveUIElement {
        UIElement generateWorldText;
        int tickCount = 0;
        public MainMenuStartButton(UIElement generateWorldText) {
            spriteSheetID = (int)spriteSheetIDs.mainMenuUI;
            drawRectangle = new Rectangle(0, 400, 192, 66);
            sourceRectangle = new Rectangle(0,25,33, 12);
            alignment = UIAlignOffset.Centre;
            tickCount = 0;
            this.generateWorldText = generateWorldText;
        }
        public override void onLeftClick(Game1 game)
        {
            generateWorldText.isUIElementActive = true;
            tickCount += 1;
            
        }
        public override void updateElement(double elasedTime, Game1 game)
        {
            //If the button was pressed for 2 ticks, then generate the world. This allows the UI to update
           
            if (tickCount > 10) {
                (int width, int height) worldDimensions = (800, 800);
                game.worldContext.generateWorld(worldDimensions);
                game.currentScene = Scene.Game;
            }
        }
    }
    public class MainMenuWorldGenText : UIElement {
        public MainMenuWorldGenText() {
            isUIElementActive = false;
            spriteSheetID = (int)spriteSheetIDs.mainMenuUI;
            drawRectangle = new Rectangle(0, 350, 576, 30);
            sourceRectangle = new Rectangle(0,38,96, 5);
            alignment = UIAlignOffset.Centre;
        }
    }
    
    #endregion
    public class SpriteController {
        public Texture2D blockSpriteSheet;
        public Texture2D weaponSpriteSheet;
        public Texture2D blockItemSpriteSheet;
        public Texture2D playerSpriteSheet;
        public Texture2D arrowSpriteSheet;

        public List<Texture2D> spriteSheetList = new List<Texture2D>();
        //public List<Texture2D> entitySpriteSheetList = new List<Texture2D>();

        public void setSpriteSheetList(List<Texture2D> spriteSheets) {
            spriteSheetList = spriteSheets;
            
        }
    }
    public enum spriteSheetIDs {
        blocks,
        weapons,
        blockItems,
        player,
        arrow,
        mainMenuUI,
        blockBackground
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
        /*
         * A self contained engine that calculates kinematic physics
         * 
         * 
         * =========================================================
         * Settings file:
         * 
         * - blockSizeInMeters
         * - Gravity
         */


        bool helpDebug = false;
        public double blockSizeInMeters { get; set; } //The pixel size in meters can be found by taking this value and dividing it by pixelsPerBlock
        WorldContext wc;

        int horizontalOverlapMin = 2;
        int verticalOverlapMin = 2;

        double gravity;


        public PhysicsEngine(WorldContext worldContext)
        {
            wc = worldContext;


            //Load txt file and read the values to define important variables
            loadSettings();
        }

        private void loadSettings() {
            StreamReader sr = new StreamReader(wc.runtimePath + "Settings\\PhysicsEngineSettings.txt");
            sr.ReadLine();
            blockSizeInMeters = Convert.ToDouble(sr.ReadLine());
            sr.ReadLine();
            gravity = Convert.ToDouble(sr.ReadLine());
            sr.Close();
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
            Block[,] worldArray = wc.worldArray; //A temporary storage of an array to reduce external function calls

            for (int x = entityLocationInGridX - 1; x < entityLocationInGridX + entityGridWidth + 1; x++)
            { //A range of x values on either side of the outer bounds of the entity
                for (int y = entityLocationInGridY - 1; y < entityLocationInGridY + entityGridHeight + 1; y++)
                {
                    if (x >= 0 && y >= 0 && x < worldArray.GetLength(0) && y < worldArray.GetLength(1))
                    {
                        if (worldArray[x, y].ID != 0) //In game implementation, air can either be null or have a special 'colliderless' block type 
                        {
                            Rectangle blockRect = new Rectangle(x * wc.pixelsPerBlock, y * wc.pixelsPerBlock, wc.pixelsPerBlock, wc.pixelsPerBlock);
                            if (blockRect.Intersects(entityCollider))
                            {
                                (double x, double y) collisionNormal = computeCollisionNormal(entityCollider, blockRect);
                                entity.hasCollided();

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

        public bool calculatePhysics = true;

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

        public double drawWidth { get; set; }
        public double drawHeight { get; set; }
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

        public virtual void updateLocation(double xChange, double yChange)
        {
            x += xChange;
            y += yChange;
            
        }

        public virtual void onBlockCollision(int blockX, int blockY)
        {

        }

        public void recalculateCollider() {
            collider = new Rectangle(0, 0, (int)(width * worldContext.pixelsPerBlock), (int)(height * worldContext.pixelsPerBlock));
        }

        public virtual void hasCollided() { }
    }

    public class Entity : PhysicsObject {
        
        public Texture2D spriteSheet;
        public SpriteAnimator spriteAnimator;
        public float rotation;
        public Vector2 rotationOrigin;
        public SpriteEffects directionalEffect;

        public Entity(WorldContext wc) : base(wc) {
            worldContext = wc;
            worldContext.physicsObjects.Add(this);
        }
        public void setSpriteTexture(Texture2D spriteSheet)
        {
            this.spriteSheet = spriteSheet;
            spriteAnimator.spriteSheet = spriteSheet;
        }
        public virtual void inputUpdate(double elapsedTime)
        { }

    }
    public class EntityController {
        public List<Entity> entities = new List<Entity>();

        public void entityInputUpdate(double elapsedTime) {
            for (int i = 0; i < entities.Count; i++)
            {
                entities[i].inputUpdate(elapsedTime);
            }
        }

        public void addEntity(Entity entity) {
            if (!entities.Contains(entity)) { entities.Add(entity); }
        }
        public void removeEntity(Entity entity) {
            if (entities.Contains(entity)) { entities.Remove(entity); }
        }
       
    }
    public class Player : Entity
    {
        int emmissiveStrength = 500;
        int emmissiveMax = 125;
        int[,] lightMap;

        public bool writeToChat;

        public UIItem[,] inventory = new UIItem[9,5];

        public int playerDirection { get; set; }


        int initialX = 10;
        int initialY = 10;

        double horizontalAcceleration = 4; //The acceleration in m/s^-2
        double jumpAcceleration = 12;

        Item mainHand;
        int mainHandIndex;

        float discardCooldown;
        float maxDiscardCooldown = 0.1f;
    


        public Player(WorldContext wc) : base(wc)
        {
            loadSettings();

            
            //need to dissociate the collider width and the draw width. 
            collider = new Rectangle(0, 0, (int)(width * wc.pixelsPerBlock), (int)(height * wc.pixelsPerBlock));

            drawWidth = 1.5f;
            drawHeight = 3;

            rotation = 0;
            rotationOrigin = Vector2.Zero;

            mainHand = new BlockItem(1, worldContext.animationController, this);
            mainHandIndex = 2;

            lightMap = wc.engineController.lightingSystem.calculateLightMap(emmissiveStrength);

            //Initialise inentory
            initialiseInventory();
            //Setup initial inventory
            inventory[0,0].setItem(new Weapon(worldContext.animationController, this));
            inventory[1, 0].setItem(new Bow(worldContext.animationController, this));
            inventory[2, 0].setItem(new BlockItem(1, worldContext.animationController, this));
            

            spriteAnimator = new SpriteAnimator(animationController : worldContext.animationController, constantOffset : new Vector2(12f, 8f), frameOffset : new Vector2(32, 65), sourceDimensions : new Vector2((float)32, (float)64), animationlessSourceRect : new Rectangle(160, 0, (int)32, (int)64), owner : this);
            
            spriteAnimator.animationDictionary = new Dictionary<string, (int frameCount, int yOffset)> {
                
                { "walk", (6, 0) }
            
            };

            wc.engineController.entityController.addEntity(this);
            
        }

        private void loadSettings() {
            StreamReader sr = new StreamReader(worldContext.runtimePath + "Settings\\PlayerSettings.txt");
            sr.ReadLine();
            initialX = Convert.ToInt32(sr.ReadLine()); 
            initialY = Convert.ToInt32(sr.ReadLine()); 
            x = initialX;
            y = initialY;
            sr.ReadLine();
            kX = Convert.ToDouble(sr.ReadLine()); 
            kY = Convert.ToDouble(sr.ReadLine()); 
            sr.ReadLine();
            width = Convert.ToDouble(sr.ReadLine()); 
            height = Convert.ToDouble(sr.ReadLine()); 
            sr.ReadLine();
            emmissiveStrength = Convert.ToInt32(sr.ReadLine()); 
            sr.ReadLine();
            emmissiveMax = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            horizontalAcceleration = Convert.ToDouble(sr.ReadLine()); 
            sr.ReadLine();
            jumpAcceleration = Convert.ToDouble(sr.ReadLine());
        }

        public void initialiseInventory()
        {
            for (int x = 0; x < inventory.GetLength(0); x++) {
                for (int y = 0; y < inventory.GetLength(1); y++) {
                    inventory[x, y] = new UIItem();
                }
            }
        }

        public bool addItemToInventory(Item item) {
            bool foundASlot = false;
            //Check for any stacks to add the item to
            for (int y = 0; y < inventory.GetLength(1); y++) {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    if (!foundASlot && inventory[x,y].item != null)
                    {
                        
                        if (inventory[x, y].item.GetType() == item.GetType())
                        {
                            if (inventory[x, y].item.currentStackSize < inventory[x, y].item.maxStackSize)
                            {
                                inventory[x, y].item.currentStackSize += 1;
                                foundASlot = true;

                            }
                        }
                    }
                }
            }
            if (!foundASlot)
            {
                for (int y = 0; y < inventory.GetLength(1); y++)
                {
                    for (int x = 0; x < inventory.GetLength(0); x++)
                    {
                        if (!foundASlot)
                        {
                            if (inventory[x, y].item == null)
                            {
                                inventory[x, y].setItem(item);
                                foundASlot = true;
                            }
                        }
                    }
                }
            }
            return foundASlot;
        }

        public override void inputUpdate(double elapsedTime) {
            //Movement
            if (!writeToChat)
            {
                if (Keyboard.GetState().IsKeyDown(Keys.D))
                {

                    accelerationX += horizontalAcceleration;
                    if (!spriteAnimator.isAnimationActive)
                    {
                        spriteAnimator.startAnimation(0.5, "walk");
                    }
                    playerDirection = 1;
                    directionalEffect = SpriteEffects.None;

                }
                if (Keyboard.GetState().IsKeyDown(Keys.A))
                {
                    accelerationX -= horizontalAcceleration;
                    if (!spriteAnimator.isAnimationActive)
                    {
                        spriteAnimator.startAnimation(0.5, "walk");
                    }
                    playerDirection = -1;
                    directionalEffect = SpriteEffects.FlipHorizontally;
                }
                if (!Keyboard.GetState().IsKeyDown(Keys.A) && !Keyboard.GetState().IsKeyDown(Keys.D))
                {
                    spriteAnimator.isAnimationActive = false; //If the player isn't walking, stop the animation
                }
                if (Keyboard.GetState().IsKeyDown(Keys.W) || Keyboard.GetState().IsKeyDown(Keys.Space))
                {
                    if (isOnGround)
                    {
                        accelerationY += jumpAcceleration / elapsedTime;
                    }
                }
                if (Keyboard.GetState().IsKeyDown(Keys.R))
                {
                    x = initialX;
                    y = initialY;
                    velocityX = 0;
                    velocityY = 0;
                }

                //Item Swapping
                if (Keyboard.GetState().IsKeyDown(Keys.D1))
                {

                    mainHand = inventory[0, 0].item;
                    mainHandIndex = 0;
                    if (mainHand != null) { mainHand.onEquip(); }
                    
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.D2))
                {
                    mainHand = inventory[1, 0].item;
                    mainHandIndex = 1;
                    if (mainHand != null) { mainHand.onEquip(); }
                }
                else if (Keyboard.GetState().IsKeyDown(Keys.D3))
                {
                    mainHand = inventory[2, 0].item;
                    mainHandIndex = 2;
                    if (mainHand != null) { mainHand.onEquip(); }
                }

                //Update dropCooldown
                if (discardCooldown > 0) {
                    discardCooldown -= (float)elapsedTime;
                }

                else if (Keyboard.GetState().IsKeyDown(Keys.Q)) {
                    if (mainHand != null)
                    {
                        double initialVelocity = 8f;
                        double pickupDelay = 1f;
                        if (playerDirection == -1)
                        {
                            initialVelocity *= -1;
                        }
                        DroppedItem dropItem = new DroppedItem(worldContext, mainHand, (x, y), initialVelocity);
                        mainHand.currentStackSize -= 1;
                        if (mainHand.currentStackSize <= 0)
                        {
                            inventory[mainHandIndex, 0].item = null;
                            mainHand = null;
                        }
                        dropItem.x = x;
                        dropItem.y = y;
                        dropItem.pickupDelay = pickupDelay;
                        discardCooldown = maxDiscardCooldown;
                    }
                }
            }
                ///Item Action
                if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                {
                    if (mainHand != null)
                    {
                        mainHand.onLeftClick();
                    }
                }
        }
        public override void updateLocation(double xChange, double yChange) {
            int xBlockChange = (int)(Math.Floor((x + xChange) / worldContext.pixelsPerBlock) - Math.Floor(x / worldContext.pixelsPerBlock));
            int yBlockChange = (int)(Math.Floor((y + yChange) / worldContext.pixelsPerBlock) - Math.Floor(y / worldContext.pixelsPerBlock));

            

            if (xBlockChange >= 1 || xBlockChange <= -1 || yBlockChange >= 1 || yBlockChange <= -1)
            {
                worldContext.engineController.lightingSystem.movedLight((int)Math.Floor(((x) / worldContext.pixelsPerBlock)) + collider.Width/(2 * worldContext.pixelsPerBlock), (int)Math.Floor((y) / worldContext.pixelsPerBlock), xBlockChange, yBlockChange, lightMap, emmissiveMax);
            }

            base.updateLocation(xChange, yChange);
        }
        
    }

    public class UIItem : InteractiveUIElement
    {
        public Item item;
        //A class that represents an item. Each ui element contains it's own corrosponding item. 

        //When a droppedItem entity is picked up, it either adjusts the item of the UIItem element, or it creates both a new UiElement and item class
        public void setItem(Item item) {
            this.item = item;
        }
    }
    public class Arrow : Entity, IEmissive
    {
        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }
        public float range { get; set; }
        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }
        public Arrow(WorldContext wc, (double x, double y) arrowLocation, double initialVelocity) : base(wc)
        {
            spriteSheet = wc.engineController.spriteController.spriteSheetList[(int)spriteSheetIDs.arrow];
            spriteAnimator = new SpriteAnimator(wc.animationController, Vector2.Zero, new Vector2(16, 16), new Vector2(16, 16), new Rectangle(0, 0, 16, 16), this);
            spriteAnimator.sourceOffset = new Vector2(0f, 16f);

            rotationOrigin = Vector2.Zero;
            directionalEffect = SpriteEffects.None;

            drawHeight = 1;
            drawWidth = 1;
            width = 1;
            height = 0.5;
            collider = new Rectangle(0, 0, (int)(1 * wc.pixelsPerBlock), (int)(0.5 * wc.pixelsPerBlock));


            x = arrowLocation.x;
            y = arrowLocation.y;


            kX = 0.01;
            kY = 0.01;

            minVelocityX = 0.25;
            minVelocityY = 0;

            velocityX = initialVelocity;

            int lightType = new Random().Next(4);
            if (lightType == 0)
            {
                lightColor = new Vector3(0.98f, 0.44f, 0.16f);
            }
            else if (lightType == 1)
            {
                lightColor = new Vector3(0.17f, 0.98f, 0.98f);
            }
            else if (lightType == 2)
            {
                lightColor = new Vector3(0.8f, 0.18f, 0.06f);
            }
            else if (lightType == 3)
            {
                lightColor = new Vector3(0.8f, 0.06f, 0.7f);
            }


            luminosity = Mouse.GetState().ScrollWheelValue * 4;
            range = 10f;

            shadowMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));
            lightMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));
            worldContext.engineController.lightingSystem.lights.Add(this);

            calculateInitialVelocity(initialVelocity);
            spriteAnimator.animationDictionary = new Dictionary<string, (int frameCount, int yOffset)> {
                { "fly", (1, 0) }
            };

            worldContext.engineController.entityController.addEntity(this);
            //spriteAnimator.startAnimation(1, "fly");

        }
        private void calculateInitialVelocity(double initialVelocity)
        {
            //Compute angle
            double yDif = -(Mouse.GetState().Y - (y + worldContext.screenSpaceOffset.y));
            double xDif = ((Mouse.GetState().X - (x + worldContext.screenSpaceOffset.x)));
            if (xDif < 0)
            {
                yDif *= -1;
            }

            double theta = Math.Atan(-(Mouse.GetState().Y - (y + worldContext.screenSpaceOffset.y)) / ((Mouse.GetState().X - (x + worldContext.screenSpaceOffset.x))));
            velocityX = initialVelocity * Math.Cos(theta);

            velocityY = initialVelocity * Math.Sin(theta);
            if (xDif < 0) { velocityX *= -1; velocityY *= -1; directionalEffect = SpriteEffects.FlipHorizontally; }

        }

        public override void inputUpdate(double elapsedTime)
        {
            if (velocityX != 0 && calculatePhysics)
            {
                rotation = (float)Math.Atan(-velocityY / velocityX);
            }

            if (Keyboard.GetState().IsKeyDown(Keys.LeftShift))
            {
                if (Mouse.GetState().ScrollWheelValue * 4 != luminosity)
                {
                    luminosity = Mouse.GetState().ScrollWheelValue * 4;
                }
            }

        }

        public override void hasCollided()
        {
            calculatePhysics = false;
            worldContext.engineController.lightingSystem.lights.Remove(this);
        }
    }

    #region Item Classes

    public class DroppedItem : Entity {
        public Item item { get; set; }
        float pickupAcceleration = 50f;

        public double pickupDelay;
        double pickupMoveDistance = 96f;
        double pickupDistance = 48f;

        public DroppedItem(WorldContext wc, Item item, (double x, double y) location, double initialVelocity) : base(wc) {
            //Set the texture from the item's spritesheet
            
            
            spriteAnimator = new SpriteAnimator(wc.animationController, Vector2.Zero, Vector2.Zero, new Vector2(item.sourceRectangle.Width, item.sourceRectangle.Height), item.sourceRectangle, this);
            setSpriteTexture(wc.engineController.spriteController.spriteSheetList[item.spriteSheetID]);
            drawWidth = item.drawDimensions.width/(double)wc.pixelsPerBlock;
            drawHeight = item.drawDimensions.height / (double)wc.pixelsPerBlock;
            width = drawWidth;
            height = drawHeight;

            this.item = item;

            collider = new Rectangle(0, 0, (int)(width * wc.pixelsPerBlock), (int)(height * wc.pixelsPerBlock));

            kX = 5;
            kY = 0.01;

            minVelocityX = 0.25;
            minVelocityY = 0.01;

            velocityX = initialVelocity;

            

            wc.engineController.entityController.addEntity(this);
        }

        public override void inputUpdate(double elapsedTime)
        {
            if (pickupDelay > 0)
            {
                pickupDelay -= elapsedTime;
            }
            else
            {
                double distance = Math.Pow(Math.Pow((worldContext.player.y + worldContext.player.height * worldContext.pixelsPerBlock / 2.0) - (y + drawHeight * worldContext.pixelsPerBlock / 2.0f), 2) + Math.Pow((worldContext.player.x + worldContext.player.width * worldContext.pixelsPerBlock / 2.0) - (x + drawWidth * worldContext.pixelsPerBlock / 2.0f), 2), 0.5);
                if (distance < pickupMoveDistance)
                {
                    
                    accelerationX = pickupAcceleration * (((worldContext.player.x + worldContext.player.width * worldContext.pixelsPerBlock / 2.0) - (x + drawWidth * worldContext.pixelsPerBlock/2.0f) ) / distance);
                    accelerationY = -pickupAcceleration * (((worldContext.player.y + worldContext.player.height * worldContext.pixelsPerBlock / 2.0) - (y + drawHeight * worldContext.pixelsPerBlock/2.0f)) / distance);

                }
                if (distance < pickupDistance) {
                    //Pickup action
                    //Now I just need to make an inventory system and sorting
                    
                    if (worldContext.player.addItemToInventory(item)) {
                        worldContext.engineController.entityController.removeEntity(this);
                    }
                }
            }
        }
    }
    public class Item
    {
        //Two ways of going about dropping and picking items up. One: Make each item a physics object, and activate the physics when they are dropped.Seems needlessly bulky
        //Two: Have a "dropped item" class which is itself an entity, and make it point to an Item Class. On the "input" update function of entities, if the player is within
        //A set range, apply a force towards the player. Items float towards the player, and when they get within a smaller range, they add the item to the players inventory
        //and destroy the "dropped item" class
        public Rectangle sourceRectangle { get; set; }
        public int spriteSheetID { get; set; }

        public int maxStackSize { get; set; }

        public int currentStackSize { get; set; }

        
        public (int width, int height) drawDimensions { get; set; }
        public Animator itemAnimator { get; set; }
        public AnimationController animationController { get; set; }
        public Vector2 origin { get; set; }
        public int verticalDirection { get; set; }
        public double constantRotationOffset { get; set; }

        public SpriteEffects spriteEffect;

        public int colliderWidth { get; set; }
        public int colliderHeight { get; set; }

        public Vector2 offsetFromEntity { get; set; }

        public float useCooldown;

        public Player owner { get; set; }

        public Item(Player owner) {
            this.owner = owner;
        }


        public virtual void onLeftClick() { }
        public virtual void onEquip() { }
        public virtual void animationFinished()
        {
            itemAnimator = null;
        }

    }
    public class Weapon : Item, INonAxisAlignedActiveCollider
    {

        bool swungDownwardsLastIteration = false;

        public Vector2[] rotatedPoints { get; set; }

        public Vector2[] originalPoints { get; set; }

        public Vector2 rotationOrigin { get; set; }

        public (double x, double y) location { get; set; }

        public bool isActive { get; set; }

        public bool hasCollided { get; set; }

        public Weapon(AnimationController ac, Player owner) : base(owner)
        {
            spriteSheetID = 1;
            animationController = ac;

            this.owner = owner;

            location = (owner.x, owner.y);

            constantRotationOffset = -Math.PI / 4;

            
            origin = new Vector2(-2f, 18f);
            rotationOrigin = new Vector2(-2, 18f);

            sourceRectangle = new Rectangle(0, 0, 16, 16);
            drawDimensions = (48, 48);



            rotatedPoints = new Vector2[4];
            originalPoints = new Vector2[4];


            colliderWidth = 4;
            colliderHeight = 32;

            useCooldown = 0.2f;

            maxStackSize = 1;
            currentStackSize = 1;

        }
        //Adjusted to define the rectangular vertices only once, this should be a bit more efficient
        public override void onLeftClick()
        {
            if (itemAnimator == null)
            {
                offsetFromEntity = new Vector2(owner.playerDirection * 8, 48);
                isActive = true;
                if (!swungDownwardsLastIteration)
                {
                    spriteEffect = SpriteEffects.None;
                    verticalDirection = 1;
                    origin = new Vector2(-2f, 18f);
                    location = (owner.x - origin.X, owner.y - origin.Y);
                    constantRotationOffset = -Math.PI / 4;

                    float initialRotation = (float)0;
                    itemAnimator = new Animator(animationController, this, 0.2, (0, 0, initialRotation), (0, 0, 2 * Math.PI/3), constantRotationOffset, offsetFromEntity);
                    swungDownwardsLastIteration = true;

                    initialiseColliderVectors(1, initialRotation);


                }
                else
                {

                    spriteEffect = SpriteEffects.FlipVertically;
                    
                    verticalDirection = -1;
                    location = (owner.x - origin.X, owner.y - origin.Y);
                    constantRotationOffset = Math.PI / 4;

                    float initialRotation = (float)-Math.PI / 6;
                    itemAnimator = new Animator(animationController, this, 0.15, (0, 0, initialRotation), (0, 0, -Math.PI / 2), constantRotationOffset, offsetFromEntity);
                    swungDownwardsLastIteration = false;
                    initialiseColliderVectors(-1, initialRotation);

                }
                animationController.addAnimator(itemAnimator);
            }
        }
        public override void onEquip()
        {
            owner.worldContext.engineController.collisionController.addActiveCollider((IActiveCollider)this);

        }
        public override void animationFinished()
        {
            isActive = false;
            itemAnimator = null;
        }

        private void initialiseColliderVectors(int multiplier, float initialRotation)
        {
            originalPoints[0] = new Vector2(-colliderWidth, -colliderHeight) - rotationOrigin; //The rotation origin doesn't adjust with the origin that is used for drawing. This is because the drawing system and the collision system operate under different grid spacesgit
            originalPoints[1] = new Vector2(0, -colliderHeight) - rotationOrigin;
            originalPoints[2] = new Vector2(-colliderWidth, 0) - rotationOrigin;
            originalPoints[3] = new Vector2(0, 0) - rotationOrigin;

            originalPoints[0] *= multiplier;
            originalPoints[1] *= multiplier;
            originalPoints[2] *= multiplier;
            originalPoints[3] *= multiplier;


            //Initialise the rotated points a
            rotatedPoints[0] = new Vector2(originalPoints[0].X, originalPoints[0].Y);
            rotatedPoints[1] = new Vector2(originalPoints[1].X, originalPoints[1].Y);
            rotatedPoints[2] = new Vector2(originalPoints[2].X, originalPoints[2].Y);
            rotatedPoints[3] = new Vector2(originalPoints[3].X, originalPoints[3].Y);

            ((INonAxisAlignedActiveCollider)this).calculateRotation(initialRotation);
        }

    }
    public class Bow : Item {
        public Bow(AnimationController ac, Player owner) : base(owner) {
            spriteSheetID = (int)spriteSheetIDs.weapons;
            animationController = ac;
            origin = new Vector2(-6f,-6f);
            constantRotationOffset = 0;
            spriteEffect =  SpriteEffects.None;

            verticalDirection = 1;


            sourceRectangle = new Rectangle(16, 0, 16, 16);
            drawDimensions = (48, 48);

            maxStackSize = 1;
            currentStackSize = 1;

            useCooldown = 0f;
        }

        public override void onLeftClick()
        {
            if (itemAnimator == null)
            {
                itemAnimator = new Animator(animationController, this, 0.3, (0, 0, 0), (0, 0, 0), 0, new Vector2(0,0));
                if (Mouse.GetState().X < owner.x + owner.worldContext.screenSpaceOffset.x) { owner.playerDirection = -1; owner.directionalEffect = SpriteEffects.FlipHorizontally; } 
                else if 
                    (Mouse.GetState().X > owner.x + owner.worldContext.screenSpaceOffset.x) { owner.playerDirection = 1; owner.directionalEffect = SpriteEffects.None; }
                    animationController.addAnimator(itemAnimator);
                //Generate an arrow entity
                Arrow firedArrow = new Arrow(owner.worldContext, (owner.x, owner.y), 30);
                
            }
        }

    }
    public class BlockItem : Item {
        public int blockID;


        int semiAnimationAdditions = 0;
        int maxSemiAdditions = 3;
        
        
        public BlockItem(int BlockID, AnimationController animationController, Player owner) : base(owner) {
            blockID = BlockID;
            this.animationController = animationController;
            
            this.owner = owner;

            useCooldown = 0f;

            spriteSheetID = 2;
            verticalDirection = 1;

            sourceRectangle = new Rectangle(0, 0, 8, 8);

            

            drawDimensions = (16, 16);

            origin = new Vector2(-2, 18f);
            constantRotationOffset = 0;

            spriteEffect = SpriteEffects.None;



            maxStackSize = 999;
            currentStackSize = 99;

            //When you pick up an item, you check the inventory for an item of the same type. If that item already exists, check the stack size. If the stack size
            //is less than the max, just add to the current stacksize, otherwise add the item to the inventory in the next empty slot
        }

        public override void onLeftClick()
        {
            if (itemAnimator == null) {
                semiAnimationAdditions = 0;
                offsetFromEntity = new Vector2(owner.playerDirection * 8, 16);
                itemAnimator = new Animator(animationController, this, 0.15, (0, 0, 0), (0, 0, 2 * Math.PI / 3), constantRotationOffset, offsetFromEntity);

                animationController.addAnimator(itemAnimator);

                int mouseX = (int)Math.Floor((double)(Mouse.GetState().X - owner.worldContext.screenSpaceOffset.x) / owner.worldContext.pixelsPerBlock);
                int mouseY = (int)Math.Floor((double)(Mouse.GetState().Y - owner.worldContext.screenSpaceOffset.y) / owner.worldContext.pixelsPerBlock);

                owner.worldContext.addBlock(mouseX, mouseY, blockID);
            }
            if(semiAnimationAdditions < maxSemiAdditions ) {
                int mouseX = (int)Math.Floor((double)(Mouse.GetState().X - owner.worldContext.screenSpaceOffset.x) / owner.worldContext.pixelsPerBlock);
                int mouseY = (int)Math.Floor((double)(Mouse.GetState().Y - owner.worldContext.screenSpaceOffset.y) / owner.worldContext.pixelsPerBlock);

                if (owner.worldContext.addBlock(mouseX, mouseY, blockID))
                {
                    semiAnimationAdditions += 1;
                }
            }
        }
    }
    #endregion
    public class Animator
    {
        public double duration;
        public double elapsedTime;
        public double maxDuration;
        public (double xPos, double yPos, double rotation) initialPosition;
        public (double xPos, double yPos, double rotation) currentPosition;
        public (double xPos, double yPos, double rotation) finalPosition;

        public Item owner;

        public (double xPos, double yPos, double rotation) currentChange;
        double constantRotationOffset;



        public AnimationController animationController;

        public Animator(AnimationController ac, Item owner, double duration, (double xPos, double yPos, double rotation) initialPosition, (double xPos, double yPos, double rotation) finalPosition, double constantRotationOffset, Vector2 constantPositionOffset)
        {
            animationController = ac;
            this.owner = owner;

            this.duration = 0;
            maxDuration = duration;
            initialPosition.rotation += constantRotationOffset;
            finalPosition.rotation += constantRotationOffset;

            initialPosition.xPos += constantPositionOffset.X;
            finalPosition.xPos += constantPositionOffset.X;

            initialPosition.yPos += constantPositionOffset.Y;
            finalPosition.yPos += constantPositionOffset.Y;

            this.constantRotationOffset = constantRotationOffset;


            this.initialPosition = initialPosition;
            currentPosition = initialPosition;
            this.finalPosition = finalPosition;

        }

        public void tick(double elapsedTime)
        {

            duration += elapsedTime;
            this.elapsedTime = elapsedTime;
            if (duration >= maxDuration)
            {
                animationController.removeAnimator(this);
                owner.animationFinished();
            }

            currentChange.xPos = linearInterpolation(initialPosition.xPos, finalPosition.xPos);
            currentChange.yPos = linearInterpolation(initialPosition.yPos, finalPosition.yPos);
            currentChange.rotation = linearInterpolation(initialPosition.rotation, finalPosition.rotation);



            currentPosition = (currentPosition.xPos + currentChange.xPos, currentPosition.yPos + currentChange.yPos, currentPosition.rotation + currentChange.rotation);






        }

        public double linearInterpolation(double initialValue, double finalValue)
        {
            double difference = finalValue - initialValue;
            double differencePerSecond = difference / maxDuration;
            double linearlyInterpolatedValue = differencePerSecond * elapsedTime;// + initialValue; //Altered to calculate the change, then do the addition of the initial value when defining the current position. This allows for the change in a frame to be calculated for later efficiency purposes

            return linearlyInterpolatedValue;
        }

    }

    public class AnimationController
    {
        public List<Animator> animators;
        public List<SpriteAnimator> spriteAnimators;

        public AnimationController()
        {
            animators = new List<Animator>();
            spriteAnimators = new List<SpriteAnimator>();
        }

        public void addAnimator(Animator animator)
        {
            animators.Add(animator);
        }
        public void removeAnimator(Animator animator)
        {
            animators.Remove(animator);
        }

        public void addSpriteAnimator(SpriteAnimator animator) {
            spriteAnimators.Add(animator);
        }
        public void removeSpriteAnimator(SpriteAnimator animator)
        {
            spriteAnimators.Remove(animator);
        }


        public void tickAnimation(double elapsedTime)
        {
            for (int i = 0; i < animators.Count; i++)
            {
                animators[i].tick(elapsedTime);
            }
            for (int i = 0; i < spriteAnimators.Count; i++) {
                spriteAnimators[i].tickAnimation(elapsedTime);
            }
        }
        
    }

    public class SpriteAnimator {
        public Texture2D spriteSheet; //The sprite sheet to take pictures from
        public Vector2 sourceOffset; //The initial offset to shift over all the draws
        double maxDuration; //The entire duration of the animation
        double duration; //The current duration
        Vector2 frameOffset; //The offset each frame. The y value will be the offset for different animations (from the yOffset from the dictionary)
        Vector2 sourceDimensions; //The dimensions of the source
        int frame;
        public Rectangle sourceRect; //The rectangle to draw from as the source rect. This takes the sourceDimensions, and the frameOffset * frame to get the location to draw from.
        public Dictionary<String, (int frameCount, int yOffset)> animationDictionary; //The string indicates the animation, and the int value indicates the y offset (an index value) to get to said animation
        int currentAnimationFrameCount;
        int currentAnimationYOffset;
        double currentDurationPerFrame;

        Rectangle animationlessSourceRectangle; //The source rectangle when no animation is being played.
        public bool isAnimationActive;

        public AnimationController animationController;
        public Entity owner; //Substitute with something else later on.

        public SpriteAnimator(AnimationController animationController, Vector2 constantOffset, Vector2 frameOffset, Vector2 sourceDimensions, Rectangle animationlessSourceRect, Entity owner) {
            this.owner = owner;
            this.spriteSheet = owner.spriteSheet;
            this.animationController = animationController;
            this.sourceOffset = constantOffset;
            this.frameOffset = frameOffset;
            
            this.sourceDimensions = sourceDimensions;
            animationlessSourceRectangle = animationlessSourceRect;
            sourceRect = animationlessSourceRectangle;
        }

        public void startAnimation(double duration, string animation) {
            isAnimationActive = true;
            currentAnimationFrameCount = animationDictionary[animation].frameCount;
            currentAnimationYOffset = animationDictionary[animation].yOffset;
            currentDurationPerFrame = maxDuration / currentAnimationFrameCount;
            this.maxDuration = duration;
            this.duration = 0;
            frame = 0;
            animationController.addSpriteAnimator(this);
        }

        public void tickAnimation(double elapsedTime) {
            duration += elapsedTime;
            if (duration >= maxDuration || !isAnimationActive) {
                isAnimationActive = false;
                currentAnimationFrameCount = 0;
                currentAnimationYOffset = 0;
                currentDurationPerFrame = 0;
                frame = 0;
                sourceRect = animationlessSourceRectangle;
                animationController.removeSpriteAnimator(this);
                
                return;
            }

            frame = (int)Math.Floor(duration / currentDurationPerFrame);
            
            sourceRect = new Rectangle((int)(frame * frameOffset.X), (int)(currentAnimationYOffset * frameOffset.Y),(int)sourceDimensions.X, (int)sourceDimensions.Y);
            
        }



        

    }

    #region Block Classes
    public class Block
    {
        public Rectangle sourceRectangle;
        public int emmissiveStrength;
        public int ID;
        public List<Vector2> faceVertices;
        public (int x, int y) location;
        public (int width, int height) dimensions = (1, 1); //Default to 1 by 1 blocks
        public Vector4 faceDirection;
        

        public Block(Rectangle textureSourceRectangle, int ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.ID = ID;
        }
        public Block(Rectangle textureSourceRectangle, int emmissiveStrength, int ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
            this.ID = ID;
        }
        public Block(int ID) {
            this.ID = ID;
        }
        
        public Block(Block b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
            dimensions = b.dimensions;
            location = b.location;
        }

        public void setLocation((int x, int y) location) {
            this.location = location;
        }
        public void blockPlaced(Vector4 exposedFaces) {
            setupFaceVertices(exposedFaces);
        }
        public void blockDestroyed(Dictionary<(int x, int y), Block> exposedBlocks) {
            if (exposedBlocks.ContainsKey(location)) { exposedBlocks.Remove(location); }
        }

        public virtual void setupInitialData(int[,] worldArray, (int x, int y) blockLocation) {
            location = blockLocation;
        }

        public virtual void setupFaceVertices(Vector4 exposedFacesClockwise) {
            this.faceDirection = exposedFacesClockwise;
            //2 Vector2s are needed to allow for all 4 directions to be accounted for. However, this isn't the cleanest code and should be later improved
            faceVertices = new List<Vector2>();
            if(exposedFacesClockwise.X == 1)
            {
                faceVertices.Add(new Vector2(location.x, location.y));
                faceVertices.Add(new Vector2(location.x + dimensions.width, location.y));
            }
            if (exposedFacesClockwise.Y == 1) 
            {
                //Check if the vertex already exists from the previous if statement
                if (!faceVertices.Contains(new Vector2(location.x + dimensions.width, location.y)))
                {

                    faceVertices.Add(new Vector2(location.x + dimensions.width, location.y));
                }


                    faceVertices.Add(new Vector2(location.x + dimensions.width, location.y + dimensions.height));
            }
            if(exposedFacesClockwise.Z == 1)
            {
                if (!faceVertices.Contains(new Vector2(location.x + dimensions.width, location.y + dimensions.height)))
                {
                    faceVertices.Add(new Vector2(location.x + dimensions.width, location.y + dimensions.height));
                }

                faceVertices.Add(new Vector2(location.x, location.y + dimensions.height));
            }
            if (exposedFacesClockwise.W == 1)
            {
                if (!faceVertices.Contains(new Vector2(location.x, location.y + dimensions.height)))
                {
                    //System.Diagnostics.Debug.WriteLine("Decidedly: AHHHH " + location.x + ", " + location.y + dimensions.height);
                    faceVertices.Add(new Vector2(location.x, location.y + dimensions.height));
                }
               
                    faceVertices.Add(new Vector2(location.x, location.y));
            }
        }
    }

    public class GrassBlock : Block {

        
        public GrassBlock(Rectangle textureSourceRectangle, int ID) : base(textureSourceRectangle, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
        }
        public GrassBlock(Rectangle textureSourceRectangle, int emmissiveStrength, int ID) : base (textureSourceRectangle, emmissiveStrength, ID)
        {
            this.sourceRectangle = textureSourceRectangle;
            this.emmissiveStrength = emmissiveStrength;
        }
        public GrassBlock(int ID) : base (ID)
        {
            this.ID = ID;
        }

        public GrassBlock(Block b) : base (b)
        {
            sourceRectangle = b.sourceRectangle;
            emmissiveStrength = b.emmissiveStrength;
            ID = b.ID;
        }

        public override void setupInitialData(int[,] worldArray, (int x, int y) blockLocation)
        {
            bool emptyAbove = false;
            bool emptyRight = false;
            bool emptyLeft = false;

            int xOffset = 2; //Set it to the default upwards block
            //sprite sheet is as follows: |, |-, _, -|, |

            if (blockLocation.x > 0 && blockLocation.y > 0 && blockLocation.x < worldArray.GetLength(0) - 1 && blockLocation.y < worldArray.GetLength(1) - 1)
            {



                if (worldArray[blockLocation.x, blockLocation.y - 1] == 0)
                {
                    emptyAbove = true;
                }
                if (worldArray[blockLocation.x - 1, blockLocation.y] == 0)
                {
                    emptyLeft = true;
                }
                if (worldArray[blockLocation.x + 1, blockLocation.y] == 0)
                {
                    emptyRight = true;
                }
                if (emptyRight && !emptyLeft && !emptyAbove)
                {
                    xOffset = 4;
                }
                else if (emptyLeft && !emptyRight && !emptyAbove)
                {
                    xOffset = 0;
                }

                if (emptyAbove)
                {
                    if (emptyRight)
                    {
                        xOffset = 3;
                    }
                    else if (emptyLeft)
                    {
                        xOffset = 1;
                    }
                }
            }
            
            sourceRectangle = new Rectangle(sourceRectangle.X + xOffset * 32, sourceRectangle.Y, 32, 32);

            base.setupInitialData(worldArray, blockLocation);
        }
        
    }
    #endregion

    public interface ICollider
    {
        bool isActive { get; set; }


        public void onCollision(ICollider otherCollider)
        {

        }
    }

    public interface IActiveCollider : ICollider
    {

        Player owner { get; set; }
        public virtual void calculateCollision(IPassiveCollider externalCollider)
        {

        }
    }

    public interface INonAxisAlignedActiveCollider : IActiveCollider
    {

        public Vector2[] rotatedPoints { get; set; }

        public Vector2[] originalPoints { get; set; }

        public Vector2 rotationOrigin { get; set; }

        public int colliderWidth { get; set; }
        public int colliderHeight { get; set; }

        public (double x, double y) location { get; set; }

        public Animator itemAnimator { get; set; }

        public bool hasCollided { get; set; }



        public void calculateCollision(IPassiveCollider externalCollider)
        {
            if (isActive)
            {
                nonAxisAlignedCollisionDetection(externalCollider);
            }
        }

        public void nonAxisAlignedCollisionDetection(IPassiveCollider externalCollider)
        {
            hasCollided = false;
            double theta = itemAnimator.currentPosition.rotation;


            Rectangle externalColliderInLocalSpace = new Rectangle((int)(externalCollider.collider.Center.X - location.x), (int)(externalCollider.collider.Center.Y - location.y), externalCollider.collider.Width, externalCollider.collider.Height);


            //find the direction of the secondary object and determine which axis to project onto. I can definitely project solely onto an x-y plane given that I'm working with rectangular colliders for now.
            Vector2 seperatingAxis = calculateSeperationAxis(externalColliderInLocalSpace);
            //From the seperating axis, project the collider's shadow onto that axis, then see if there's a gap between the closest points... How shall I do that. Based on the axis, I can tell 
            //The center of the weapon is at 0,0 so that's to note. The seperating axis indicates what corner of the local and external colliders to use. I can take the weapons rectangle, then use a matrix transformation to rotate them, then find the point that has the greatest value along the seperating axis, which is also what it would be like projected, so i can ignore having to do vector dot products and merely take the appropriate component out of the transformed vectors.
            calculateRotation((float)itemAnimator.currentPosition.rotation);

            //Multiply the vectors by the seperating axis to get the proj onto that axis, I can then take the largest (or most negative) one and use that for the shadow. But how do I determine if the two shadows are overlapping? I can get the shadow length,
            //I can calculate the distance (based on the externalColliderInLocalSpace) it's shadow is literally just half the dimension in whatever axis, and the distance is the position of the collider in local space.

            double shadow = 0;
            for (int i = 0; i < rotatedPoints.Length; i++)
            {

                if (seperatingAxis.X != 0)
                {
                    if (shadow < rotatedPoints[i].X) shadow = rotatedPoints[i].X;
                }
                else if (seperatingAxis.Y != 0)
                {
                    if (shadow < rotatedPoints[i].Y) shadow = rotatedPoints[i].Y;
                }
            }




            if (seperatingAxis.X != 0)
            {
                if (externalColliderInLocalSpace.X * seperatingAxis.X < shadow + externalColliderInLocalSpace.Width / 2)
                {
                    //Has collided
                    System.Diagnostics.Debug.WriteLine("Collided in the X axis");
                    hasCollided = true;
                }
            }
            else if (seperatingAxis.Y != 0)
            {
                if (externalColliderInLocalSpace.Y * seperatingAxis.Y < shadow + externalColliderInLocalSpace.Height / 2)
                {
                    //Has collided
                    System.Diagnostics.Debug.WriteLine("Collided in the Y axis");
                    hasCollided = true;
                }
            }


        }

        public Vector2 calculateSeperationAxis(Rectangle externalColliderInLocalSpace)
        {
            Vector2 seperatingAxis;
            if (externalColliderInLocalSpace.X > 0) //Is to the right
            {
                if (externalColliderInLocalSpace.Y > 0) //Is downwards
                {
                    //Determine which axis is overlapping more, then use that to determine the appropriate seperatingAxis. Ensure that it's the one with no/little overlap. EG using the x-axis for things on top of eachother will produce false positives
                    if (externalColliderInLocalSpace.Y - externalColliderInLocalSpace.X > 0)
                    { //Is more below than to the right. So use the y axis to determine collision.
                        seperatingAxis = new Vector2(0, 1);
                    }
                    else
                    {
                        seperatingAxis = new Vector2(1, 0);
                    }
                }
                else //Is upwards
                {
                    if (externalColliderInLocalSpace.X + externalColliderInLocalSpace.Y > 0) //More right than up
                    {
                        seperatingAxis = new Vector2(1, 0);
                    }
                    else
                    {
                        seperatingAxis = new Vector2(0, -1);
                    }

                }
            }
            else  //Is to the left
            {
                if (externalColliderInLocalSpace.Y > 0) //Is downwards
                {
                    //Determine which axis is overlapping more, then use that to determine the appropriate seperatingAxis. Ensure that it's the one with no/little overlap. EG using the x-axis for things on top of eachother will produce false positives
                    if (externalColliderInLocalSpace.Y + externalColliderInLocalSpace.X > 0)
                    { //Is more below than to the left. So use the y axis to determine collision.
                        seperatingAxis = new Vector2(0, 1);
                    }
                    else
                    {
                        seperatingAxis = new Vector2(-1, 0);
                    }
                }
                else //Is upwards
                {
                    if (externalColliderInLocalSpace.X - externalColliderInLocalSpace.Y < 0) //More left than up
                    {
                        seperatingAxis = new Vector2(-1, 0);
                    }
                    else
                    {
                        seperatingAxis = new Vector2(0, -1);
                    }

                }

            }

            return seperatingAxis;
        }

        public void calculateRotation(float rotation)
        {
            rotation *= owner.playerDirection;
            rotatedPoints[0] = Vector2.RotateAround(originalPoints[0], new Vector2(0, 0), rotation);
            rotatedPoints[1] = Vector2.RotateAround(originalPoints[1], new Vector2(0, 0), rotation);
            rotatedPoints[2] = Vector2.RotateAround(originalPoints[2], new Vector2(0, 0), rotation);
            rotatedPoints[3] = Vector2.RotateAround(originalPoints[3], new Vector2(0, 0), rotation);
        }


    }

    public interface IPassiveCollider : ICollider
    {

        Rectangle collider { get; set; }
        //External colliders are colliders that don't compute their own collisions, they only react to collisions. Lets say that monsters have IExternalColliders, when the player collides with the monster, the collision function is run, but the monster doesn't also compute if it collided.
        //I think this will just make it a bit easier to seperate player based colliders from entity colliders. Weapons, including arrows, will have actual colliders that compute collisions with external colliders. This way weapons can 
    }

    public interface IEmissive {
        public double x { get; set; }
        public double y { get; set; }
        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }

        public float range { get; set; }


        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }
    }

    public enum blockIDs {
        //Written in order of their integer IDs
        air,
        stone,
        dirt,
        grass
    }

    public enum Scene {
        MainMenu,
        Game
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
    public class BlockThresholdValues {
        //Higher means more solid
        public double blockThreshold;
        public double maximumY;
        public double decreasePerY;
        public double maximumThreshold;
        public double minimumThreshold;
        //The effect of the absolute y value (from the top of the map) and the relative y value (from the surface)
        public double absoluteYHeightWeight;
        public double relativeYHeightWeight;

        public BlockThresholdValues(double blockThreshold, double maximumY, double decreasePerY, double maximumThreshold, double minimumThreshold, double absoluteYHeightWeight, double relativeYHeightWeight) {
            this.blockThreshold = blockThreshold;
            this.maximumY = maximumY;
            this.decreasePerY = decreasePerY;
            this.maximumThreshold = maximumThreshold;
            this.minimumThreshold = minimumThreshold;
            this.absoluteYHeightWeight = absoluteYHeightWeight;
            this.relativeYHeightWeight = relativeYHeightWeight;
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
                                            blockCount[blocks.IndexOf(blockArray[xLocal, yLocal])] += 1;
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
                                {

                                    if (blockArray[xLocal, yLocal] != null)
                                    {
                                        if (blocks.Contains(blockArray[xLocal, yLocal].block))
                                        {
                                            blockCount[blocks.IndexOf(blockArray[xLocal, yLocal].block)] += 1;
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
