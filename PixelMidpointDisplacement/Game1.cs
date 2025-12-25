using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

using System.Collections.Generic;

using System.Linq;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

using System.Threading;


/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * Things to do:

    Optimise the lighting system further:
        Reduce vertex count by extending adjacent faces, should very significantly reduce the vertex count in standard situations
        Some situations, such as diagonal blocks, will still prove to be computationally challenging.

    Decrease entity cluttering:
        - Item entities despawn after a certain period of time
       
    Entity spawning:                                                                                  - Done
        - Implement yMin and Max constraints

    Enemy entity collisions:                                                                          - Done
        - Damange from colliding (aka attacking)
        - Knockback
        - Passive colliders on enemies and active colliders on players and player controlled objects

        - Iframes!!

    Improve physics engine:                                                                           - Done
        - Implement surface friction
            -> direction and magnitude, if the acceleration is opposite to the direction of friction, subtract the two, otherwise do nothing
                Eg. When walking, the frictional force is in the same direction as the player's motion: so don't do anything. But when they stop walking, the friction force swaps direction
            -> Each block has it's own friction values that gets either added or multiplied to the player's own friction value
            -> When walking, the horizontal force cannot exceed the max friction force (because friction is what actually supplies the forwards acceleration). 
                Ice has low friction and is hard to accelerate on
            -> Each physics update, the friction coefficient is reset to some baseline constant
                -> This is the possible acceleration in the air

    *Bug fix*
    Entity death issues:                                                                              - Temporarily Fixed, will need to redo
            -> When an entity dies, it causes index overflow issues in the for loops.
                    -> Physics calulations (due to fall damage)
                    -> Collision detection

            Perhaps causing collision functions to return a value that could cause an index check to occur?

    Evolution:                                                                                        - Done
        - Implement a new scene that pops up after the player dies / hits respawn                     
        - Use InteractiveUI with a link to an evolution.                                              
                -> ask the evolution tree to enable the evolution
        - Order the evolution UI based on the size of each layer and automatically sort               
        - Draw a line between an evolution and its dependencies                                       
                -> Draw a line (somehow) between the icon's location ( plus height and 1/2 width) to the dependencies location (plus 1/2 width)
                -> Because UI is currently only using sprites, do this in a seperate function in Game1
                -> Because SpriteBatch can't natively draw lines, figure out how to draw a rectangle and rotate it to match at two points

        - Add 'experience' and limit if an evolution can be activated 
            -> Enum that indicates different 'fields' of experience
            -> Each evolution has a list of (field, experience cost)
                -> Changes if an evolution can be activeted, so more complex than just pre-requisits.


    Biomes:
        - Implement subterranian biomes                                                                                                               - Done
            -> A secondary iteration after the initial biomes are generated
                -> Each subterranian biome has a list of biomes that it can spawn in. If list is empty, spawns in any biome
        - Have to incorporate height into spawning entities from biomes
            -> Perhaps check the biome of 5 different spots on screen: top right, top left, bottom right, bottom left, centre and add them to a list of biomes to tick
            -> Have to determine a better method of determining if a block is in a certain biome                                                       - Done but needs testing
                -> Would it be easier for each block to contain a biome instance, so when a block is created, the biome that its in is passed in
                    - This would still require identifying the biome 

            -> Perhaps check each subterranian biome first: then check the surface biomes if the location is not contained in any subterranian biome   - Done
                -> Two different lists in the world context?

        -Could improve:
            -> Allow for the biome to generate different cave sizes compared to the ones that currently exist, then figure out a better way to adjust the threshold change/smoothing in all axis'


    Adjust world generation:                                                                                                                            - Done
        - Each biome seeds the world with the ores that its designed to, then, once all the biomes (and subterranean biomes) have seeded the world, you do an ore generation stage,
            then loop back through the biomes, generating structures, backgrounds and the likes. This would hopefully reduce the straight edges that appear on the boundary of different biomes
                -> Must include a list in the world generation class that contains all ores, and somehow figure out how to change the midpoint displacement to function properly

        - Instead of generating ores and combining algorithms:
            -> for each biome generate the seeds, and put the seeds onto the global array
            -> Append the ores to a list in the world generator
            -> Run the brownian motion inside the world generator
            -> Go through each biome and combine algorithms, but instead of using the biomes brownian motion array, use the worlds; with the appropriate offset
        

    Fluids:                                                                                                                                             - Done
            -> A block that updates every _ number of frames
            -> If theres air to either side and below, become a flowing liquid and flow to there (create another flowing liquid block there)
            -> if any block below it is not flowing / air, then spread to either side

    Add fluid generation:
        - start simple: replace a few random exposed blocks with a fluid source block:
                -> generates waterfalls
        - Next: pick a random point inside of a few caves (found using the exposed block system)
            Fill all the blocks that are air to the side or below with a source block: Cave lakes

    Add viscosity:
        - The fluids adjust or set the coefficient of "air" friction

    Fluid Fix:                                                                                                                                          - Done
        - Improve the buoyancy system. Currently each block that the player collides with adds its buoyancy force to the entity. However, each block does this, so the total force isn't very consistent. 
            This is good if it only affects vertical submersion (the more blocks vertically, the greater the force). Sadly, there's variance when the player collides horizontally. It should be identical, 
            but stretching across the boundary of columns makes the force double. This makes the force very inconsistent and unruly.


        - I could make it so the first fluid block that collides applies the force.                                                                     - This was implemented for simplicity
            Issues
            ->  Being between two fluids would cause inconsistency depending on which one was left/right
                    -> Not the worst, especially if it only applies to fluid acceleration, and effects would still apply for all collision blocks


    Crafting System:                                                                                                                                    - Done
        - Implement the scrolling feature of a list:
            -> Set number of elements are visible
            -> Their location is set relative to the location of the list / manager itself, then offset by theie position within the visible elements

        - Crafting UI:
            -> The element is a child of UIItem with custom features
                -> Upon a click, it checks if its parent recipe can be crafted, if so, it acts like a normal UIItem but doesn't diminish when taken from. If the item can be crafted, it removes the ingredients from the users inventory
                -> Only functions if the hovering UIItem has a null item value. THis stops it from crafting a crap ton of the item
                    -> Will have to check if the item type is the same, however (for stacking) so I'll have to add a crafting cooldown, that can reduce if the player holds the mouse down for x period of time

        - Removing ingredients from inventory
            -> Might be a custom function within IInventory, but searchs for an item, reduces the quantity by either the full "required amount" or until the item is depleted. If depleted destroy the item and continue searching
            
        - Bug fix:                                                                                                                                      - Done
            -> All items show up as swords...
    
    Combine items fix:                                                                                                                                  - Done
            - Adding an item to the inventory when there are multiple available stacks adds an item to both. 
            - Now, it doesn't add anything at all. At least for ingot blocks, but I presume a similar thing for all. This is because the player was cast as an IInventory in the dropped item function. I think

        -> The items from the crafting system were not copies. All the same instance



    Hardness and breaking duration:                                                                                                                     - Done
        - Blocks have a "hardness" and "durability" value
            -> The hardness defines the pickaxe strength required to be able to break the block
            -> THe durability is how close a block is to breaking. If it hits zero, then the block is broken

        - Pickaxes currently have a 'use duration,' so just add a "pickaxe strength" value and possibly a second "durability per hit" value
    
    Item UI:                                                                                                                                            - Done
        - An information box that comes up when hovering over an item. Works through a string conversion system. Has to be variable to allow for modification systems.
            -> Header tags
            -> Bolding
            -> Color tags?

        - Have to create a:
            -> string to text converter
            -> Variable size background
            -> Tag system (like html)

    Decorations: 
        - A new generation pass to create 'decorations,' things like trees, grass & flowers based on certain spawn conditions
            -> Decorations will have a .generate(int x, int y) function that can allow for both simple (like a single grass block) and complex generation (Like a tree)

        - Decorations are spawned biome specific when the world is generating. There are two main types:
            Surface:
                - Surface generating decorations have a density that accounts only for the width of the biome
                - For each type of decoration, its spawned in a sequence: For every generation, spawn the decoration then move a set distance horizontally between the max and min distance values
                    -> Continue looping until enough of the decoration has been spawned. This will allow forests (very short maximum distances) as well as sparse coverage
                - The y value will just be the block above the current surface

            Non-Surface:
                - Non-surface decorations will account for both height and width (within their max and min spawn heights) when considering the amount to spawn.
                - Their spawn range will move the set distance in both x and y axis

    Sun system: Things to fix                                                                                                   - In Progress
        - Getting the right blocks to be considered 'exposed to air' such as those below a transparent block or fluid           - Done

        - Fixing edges                                                                                                          - Done
            -> Blocks on the edge of the screen cast large amounts of light because the shadow faces don't extend to the corners of the screen

        - Trees don't cast solar shadows                                                                                        - Done
            -> Fix: Any block including or above the surface height casts a solar shadow! Fixes this issue

        - Cave mouths                                                                                                           - Done
            -> Won't get any shadows cast by the blocks themselves if they're underneath an overhang

        - Maybe making the sun actually far away and doing a normal render pass will be better? Just make the x and y proportional to the angles of the sun, and make the sun outside the screen        - Implemented
            -> Will need to find an a solution regarding going underground & edges of the screen
                - Fix:
                    Find the lowest surface height on screen, and draw a square from that height (or if it's higher than the top of the screen, the top of the screen) to the bottom of the screen and across the entire width
                    Then, cast a shadow from the height at the far right and the far left surface heights, down to the bottom of the screen. This should cover all combinations, issues and heights


            - To make the sunlight not jolt away, lets have its luminosity fall off as the player go further underground        - Need to do

            - This should only apply to the sun!                                                                                - Done
            - For all lights, do a boundary check: Check the edges of the screen, and draw lines between every air block (aka where non-transparent blocks are)

        - Extend the shadow casting of the blocks in the sun to those past the edges of the screen by some amount               - Need to do


    Improved leaves
        - Make the semi leaf blocks detect all nearby leaves and determine their state based on the blocks directly around them:
        
        - Make tree generation create a grouping of 'semi-leaves' in all the air pockets around each leaf block
        
        - Have to figure out shader shadows and how that looks

    Sky / Biome backgrounds:                                                                                                    - Generalised done, now need to make each biome have their own
        - Parallax between layers
        
        - Each biome has their own unique sky (doesn't have to be unique per se                                                 - Will be implemented later, along with the improved biome detection
            -> Gets drawn each frame
        - Need to add a draw lerp between the pixels of the source rectangle change, beause the jumps are too big and not very clean.  - Needs to be implemented

    Add a moon:
        - Just a second mode of the sun, make the sunlight fade as the angle gets lower (and higher i guess) before resetting the location to the opposite side of the world

    Refactoring:
        - Move all of the different class goupings into their own file

        - Make the engine controller own the world context, not the other way around

        - Make a centralised 'rendering' class, or improve the sprite renderer controller to include:                                                               - Very important!! Not important actually
            - A class dictionary that contains the sprite sheet ID (or just the sprite sheet itself) and the source rectangle of that object (y value primarily)
                -> Classes can define their own state, and each object has that state considered in the sprite renderer that adjusts the source rectangle X


    Refactoring subset:                                                                                                                                             - Concept revoked
        JSON overhaul:                                                                                                                  
            - Turn various item constants into a .json data file for that item    

    Update the sky class:                                                                                                                                           - Done
        Modular:
            - The sky constructor takes in a sprite sheet ID, source dimensions, and a layer count, and creates an appropriate number of layers, ordered appropriately and with correct (or automatic at least) movement values

    Refactor the sprite animator:                                                                                                                                   - Done
        Work for all classes that can be drawn, instead of just the entity class
        

    Sprite Animator Fix:
        Figure out why the first animation makes the class disappear? Like wtf is happening

    Sky class Fix:
        Figure out why rendering the sky causes a significant drag/draw on rendering and frames. Is it because it's a very large rendering? Is it because it's moving? Is it because there's values being constantly assigned? Why.
 */

/*
 * Block IDs for reference:
 * Air
 * Stone
 * Dirt
 * Grass
 * Torch
 * Chest
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
        Biome currentBiome;

        float shadowValue = 0.4f;
        int lightCount = 1;

        Matrix worldMatrix, viewMatrix, projectionMatrix;

        VertexPositionColorTexture[] triangleVertices;

        Texture2D collisionSprite;
        Texture2D redTexture;
        Texture2D lineTexture;

        Effect calculateLight;
        Effect combineLightAndColor;
        Effect addLightmaps;

        List<(int x, int y)> currentlyRenderedExposedBlocks = new List<(int x, int y)>();

        short[] ind = { 0, 3, 2, 0, 1, 2 };



        bool useShaders = true;
        double toggleCooldown = 0;

        int exposedBlockCount;


        EngineController engineController;

        SpriteFont ariel;
        SpriteFont itemCountFont;


        //++++++++++++++++++

        bool writeToChat = false;

        string chat;

        string previousAddedCharacters = "";

        double chatCountdown;

        double timeSinceRepeatedLetter;
        // ++++++++++++++++++++++
        string playerAcceleration = "";
        Player player;

        int digSize = 1;
        //++++++++++++++++++
        double biomeTickSpeed;
        double maxBiomeTickSpeed = 0.5f;

        double fluidTickSpeed;
        double maxFluidTickSpeed = 0.3;

        //+++++++++++++++++++++++
        int UIElementCountLastFrame = 0;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            this.TargetElapsedTime = TimeSpan.FromSeconds(1d / 180d);

            engineController = new EngineController();

            worldContext = new WorldContext(engineController);
            engineController.initialiseEngines(worldContext);

            List<int> surfaceX = new List<int>();

            currentScene = Scene.MainMenu;


            player = new Player(worldContext);


        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            
            _graphics.ApplyChanges();

            worldContext.setApplicationDimensions(_graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight);

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

            lineTexture = new Texture2D(GraphicsDevice, 1, 1);
            lineTexture.SetData<Color>(new Color[] { Color.White});

            basicEffect.World = worldMatrix;
            basicEffect.View = viewMatrix;
            basicEffect.Projection = projectionMatrix;


            ariel = Content.Load<SpriteFont>("ariel");
            itemCountFont = Content.Load<SpriteFont>("ItemFont");


            redTexture = new Texture2D(GraphicsDevice, 1, 1);
            redTexture.SetData<Color>(new Color[] { Color.Red });
            List<Texture2D> spriteSheetList = new List<Texture2D>();
            //Need a better way to ensure that they are in the same order as the spriteSheetIDs enum
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\blockSpriteSheet.png")); //0
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\weaponSpriteSheet.png")); //1
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\blockItemSpriteSheet.png")); //2
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\PlayerSpriteSheet.png")); //3
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\ArrowSpriteSheet.png")); //4
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\MainMenuUISpriteSheet.png")); //5
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\blockBackgroundSpriteSheet.png"));//6
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\inventorySpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\healthUISpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\deathScreenSpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\armourSpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\accessorySpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\pixelNumbers.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\evolutionBackground.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\evolutionIconsSpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\evolutionCounterCharacters.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\oreSpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\ingotSpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\stringRenderingSpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\tooltipBackgroundSpriteSheet.png"));
            spriteSheetList.Add(Texture2D.FromFile(_graphics.GraphicsDevice, AppDomain.CurrentDomain.BaseDirectory + "Content\\skySpriteSheet.png"));

            worldContext.engineController.spriteController.setSpriteSheetList(spriteSheetList);

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

        public void changeScene(Scene newScene) {
            currentScene = newScene;

            if (currentScene == Scene.Evolution) {
                worldContext.engineController.evolutionController.updateExperienceCounters();
            }
        }

        protected override void Update(GameTime gameTime)
        {
            updateUI(gameTime);
            updateInteractiveUI(gameTime.ElapsedGameTime.TotalSeconds);
            updateLines(gameTime);
            tickAnimations(gameTime);


            if (currentScene == Scene.Game)
            {
                worldContext.sun.updateTime(gameTime.ElapsedGameTime.TotalSeconds);
                updateChatSystem(gameTime);
                updatePhysicsObjects(gameTime);
                calculateScreenspaceOffset();
                updateInteractiveBlocks(gameTime);
                checkCollisions();
                updateBiome(gameTime);
                updateEntities(gameTime);
                if (worldContext.sun.sky != null)
                {
                    worldContext.sun.sky.updateSky(worldContext);
                }
                worldContext.engineController.craftingManager.managerUpdate();

                if (fluidTickSpeed <= 0)
                {
                    fluidTickSpeed = maxFluidTickSpeed;
                }
                else
                {
                    fluidTickSpeed -= gameTime.ElapsedGameTime.TotalSeconds;
                }
            }

            base.Update(gameTime);
        }
        protected override void Draw(GameTime gameTime)
        {

            if (currentScene == Scene.MainMenu) {
                GraphicsDevice.SetRenderTarget(world);
                GraphicsDevice.Clear(Color.MidnightBlue);

            }
            else if (currentScene == Scene.Game)
            {
                GraphicsDevice.SetRenderTarget(spriteRendering);
                _spriteBatch.Begin(samplerState: SamplerState.PointClamp, blendState: BlendState.NonPremultiplied);
                drawSky();
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
            GraphicsDevice.SetRenderTarget(world);
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            drawLines();
            drawUI();
            //drawInteractiveUIString();
            _spriteBatch.End();
            GraphicsDevice.SetRenderTarget(null);
            _spriteBatch.Begin();

            _spriteBatch.Draw(world, world.Bounds, Color.White);

            _spriteBatch.End();

            base.Draw(gameTime);
        }

        #region Main menu update methods
        public void updateUI(GameTime gameTime) {
            if (worldContext.engineController.UIController.wasElementAdded)
            {
                worldContext.engineController.UIController.UIElements = worldContext.engineController.UIController.UIElements.OrderBy(uiElement => uiElement.drawOrder).ToList();
            }
            worldContext.engineController.UIController.wasElementAdded = false;
            for (int i = 0; i < worldContext.engineController.UIController.UIElements.Count; i++) {
                if (worldContext.engineController.UIController.UIElements[i].uiElement.scene == currentScene)
                {
                    worldContext.engineController.UIController.UIElements[i].uiElement.updateElement(gameTime.ElapsedGameTime.TotalSeconds, this);
                }
            }
        }
        public void updateInteractiveUI(double elapsedTime) {
            for (int i = 0; i < worldContext.engineController.UIController.InteractiveUI.Count; i++) {
                if (worldContext.engineController.UIController.InteractiveUI[i].scene == currentScene)
                {
                    if (worldContext.engineController.UIController.InteractiveUI[i].clickCooldown < 0)
                    {
                        if (checkUICollision(worldContext.engineController.UIController.InteractiveUI[i]))
                        {
                            worldContext.engineController.UIController.InteractiveUI[i].onLeftClick(this);
                        }
                    }
                    else {
                        worldContext.engineController.UIController.InteractiveUI[i].clickCooldown -= (float)elapsedTime;
                    }
                }
            }
        }

        public void updateLines(GameTime gameTime) {
            List<UILine> lines = worldContext.engineController.UIController.UILines;
            for (int i = 0; i < lines.Count; i++) {
                lines[i].updateLine();
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
        public void drawUI() {
            for (int i = 0; i < worldContext.engineController.UIController.UIElements.Count; i++) {
                if (worldContext.engineController.UIController.UIElements[i].uiElement.scene == currentScene)
                {
                    UIElement uiElement = worldContext.engineController.UIController.UIElements[i].uiElement;
                    Rectangle drawRect = new Rectangle();
                    if (uiElement.alignment == UIAlignOffset.TopLeft) { drawRect = uiElement.drawRectangle; }
                    else if (uiElement.alignment == UIAlignOffset.Centre) { drawRect = new Rectangle(uiElement.drawRectangle.X + (_graphics.PreferredBackBufferWidth - uiElement.drawRectangle.Width) / 2, uiElement.drawRectangle.Y, uiElement.drawRectangle.Width, uiElement.drawRectangle.Height); }

                    double widthScale = _graphics.PreferredBackBufferWidth / 1920;
                    double heightScale = _graphics.PreferredBackBufferHeight / 1080;
                    if (uiElement.scaleType == Scale.Relative) { drawRect.Width = (int)(drawRect.Width * widthScale); drawRect.Height = (int)(drawRect.Height * heightScale); }
                    if (uiElement.positionType == Position.Relative) { drawRect.X = (int)(drawRect.X * widthScale); drawRect.Y = (int)(drawRect.Y * heightScale); }
                    if (uiElement.isUIElementActive)
                    {
                        _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[uiElement.spriteSheetID], drawRect, uiElement.sourceRectangle, uiElement.color);

                        if (uiElement is InteractiveUIElement iue) {
                            if (iue.buttonText != null) {
                                _spriteBatch.DrawString(itemCountFont, iue.buttonText, iue.textLocation, Color.White);
                            }
                        }
                    }
                }
            }
        }
        public void drawInteractiveUIString() {
            for (int i = 0; i < worldContext.engineController.UIController.InteractiveUI.Count; i++)
            {
                if (worldContext.engineController.UIController.InteractiveUI[i].scene == currentScene)
                {
                    InteractiveUIElement iue = worldContext.engineController.UIController.InteractiveUI[i];
                    if (iue.isUIElementActive && iue.buttonText != null) {
                        _spriteBatch.DrawString(itemCountFont, iue.buttonText, iue.textLocation, Color.White);
                    }
                }
            }
        }

        public void drawLines() {
            List<UILine> lines = worldContext.engineController.UIController.UILines;
            for (int i = 0; i < lines.Count; i++)
            {
                if (currentScene == lines[i].scene)
                {
                    _spriteBatch.Draw(lineTexture, lines[i].drawRectangle, new Rectangle(0, 0, 1, 1), lines[i].drawColor, lines[i].rotation, new Vector2(0, 0), SpriteEffects.None, 0);
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
                }
                else
                {
                    writeToChat = true;
                    chatCountdown = 3;
                    chat = "";
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
                            if (k.ToString().Equals("OemQuestion")) { stringToAdd = "/"; }

                            if (k.ToString().Equals("D0")) { stringToAdd = "0"; }
                            if (k.ToString().Equals("D1")) { stringToAdd = "1"; }
                            if (k.ToString().Equals("D2")) { stringToAdd = "2"; }
                            if (k.ToString().Equals("D3")) { stringToAdd = "3"; }
                            if (k.ToString().Equals("D4")) { stringToAdd = "4"; }
                            if (k.ToString().Equals("D5")) { stringToAdd = "5"; }
                            if (k.ToString().Equals("D6")) { stringToAdd = "6"; }
                            if (k.ToString().Equals("D7")) { stringToAdd = "7"; }
                            if (k.ToString().Equals("D8")) { stringToAdd = "8"; }
                            if (k.ToString().Equals("D9")) { stringToAdd = "9"; }




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
            else if(chat != null && chat != ""){
                if (chat.StartsWith("/CLEAR")) {
                    while (worldContext.physicsObjects.Count > 1) {
                        worldContext.physicsObjects.RemoveAt(1);
                    }
                    while (worldContext.engineController.entityController.entities.Count > 1)
                    {
                        if (worldContext.engineController.entityController.entities[1] is IActiveCollider c) {
                            worldContext.engineController.collisionController.removeActiveCollider(c);
                        } else if (worldContext.engineController.entityController.entities[1] is IPassiveCollider p) {
                            worldContext.engineController.collisionController.removePassiveCollider(p);
                        }

                            worldContext.engineController.entityController.entities.RemoveAt(1);
                    }
                    chat = "";
                }

                if (chat.StartsWith("/GIVE"))
                {
                    if (chat.Contains("SWORD"))
                    {
                        new DroppedItem(worldContext, new Weapon(), (player.x, player.y), Vector2.Zero);
                        chat = "";
                    }
                    if (chat.Contains("BOW"))
                    {
                        new DroppedItem(worldContext, new Bow(), (player.x, player.y), Vector2.Zero);
                        chat = "";
                    }
                    if (chat.Contains("AMULET OF FALL DAMAGE"))
                    {
                        new DroppedItem(worldContext, new AmuletOfFallDamage(), (player.x, player.y), Vector2.Zero);
                        chat = "";
                    }
                    if (chat.Contains("CLOUD IN A JAR"))
                    {
                        new DroppedItem(worldContext, new CloudInAJar(), (player.x, player.y), Vector2.Zero);
                        chat = "";
                    }
                    if (chat.Contains("HELMET"))
                    {
                        new DroppedItem(worldContext, new Helmet(), (player.x, player.y), Vector2.Zero);
                        chat = "";
                    }
                }

                if (chat.StartsWith("/TP")) {
                    string[] stringArray = chat.Split(" ");
                    player.x = Convert.ToDouble(stringArray[1]) * worldContext.pixelsPerBlock;
                    player.y = Convert.ToDouble(stringArray[2]) * worldContext.pixelsPerBlock;
                    chat = "";

                }
            }
                chatCountdown -= gameTime.ElapsedGameTime.TotalSeconds;

            timeSinceRepeatedLetter -= gameTime.ElapsedGameTime.TotalSeconds;
            player.writeToChat = writeToChat;
            if (!writeToChat && Keyboard.GetState().IsKeyDown(Keys.P) && toggleCooldown <= 0)
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
                    worldContext.physicsObjects[i].isInFluid = false;

                    engineController.physicsEngine.addGravity(worldContext.physicsObjects[i]);
                    
                    engineController.physicsEngine.computeAccelerationWithAirResistance(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);

                    worldContext.physicsObjects[i].kX = worldContext.physicsObjects[i].defaultkX;
                    worldContext.physicsObjects[i].kY = worldContext.physicsObjects[i].defaultkY;

                    engineController.physicsEngine.computeImpulse(worldContext.physicsObjects[i], gameTime.ElapsedGameTime.TotalSeconds);

                    //Reset coefficient of friction:
                    worldContext.physicsObjects[i].cummulativeCoefficientOfFriction = worldContext.physicsObjects[i].objectCoefficientOfFriction;
                    //I don't know if I like this line:
                    worldContext.physicsObjects[i].frictionDirection = -Math.Sign(worldContext.physicsObjects[i].velocityX);

                    engineController.physicsEngine.detectBlockCollisions(worldContext.physicsObjects[i]);

                    //Because block collisions give/cause fall damage, re-check i values:
                    //This is a work around, ideally, dealing with death would be better, or adjust the i value itself to account for the change
                    if (i < worldContext.physicsObjects.Count)
                    {
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

        public void checkCollisions() {
            worldContext.engineController.collisionController.checkCollisions();
        }
        public void updateInteractiveBlocks(GameTime gameTime) {
            if (Mouse.GetState().RightButton == ButtonState.Pressed)
            {
                double mouseXPixelSpace = Mouse.GetState().X - worldContext.screenSpaceOffset.x;
                double mouseYPixelSpace = Mouse.GetState().Y - worldContext.screenSpaceOffset.y;

                int mouseXGridSpace = (int)Math.Floor(mouseXPixelSpace / worldContext.pixelsPerBlock);
                int mouseYGridSpace = (int)Math.Floor(mouseYPixelSpace / worldContext.pixelsPerBlock);
                if (worldContext.worldArray[mouseXGridSpace, mouseYGridSpace] is InteractiveBlock b) {
                    b.onRightClick(worldContext, gameTime);
                }
            }
        }

        public void updateBiome(GameTime gameTime) {
            if (biomeTickSpeed > 0)
            {
                biomeTickSpeed -= gameTime.ElapsedGameTime.TotalSeconds;
            }
            else {
                //Get the biome/s that the player is currently in

                //Search 5 different spots for a biome, and call the update function for it:
                List<Biome> nearbyBiomes = new List<Biome>();
                const int offscreenDistance = 10;

                Biome centre = worldContext.getBiomeFromBlockLocation(((_graphics.PreferredBackBufferWidth / 2 - worldContext.screenSpaceOffset.x) / worldContext.pixelsPerBlock) - offscreenDistance, ((_graphics.PreferredBackBufferHeight / 2 - worldContext.screenSpaceOffset.y) / worldContext.pixelsPerBlock) - offscreenDistance);

                if (centre != null) {
                    nearbyBiomes.Add(centre);
                    worldContext.sun.sky = centre.biomeSky;
                }

                for (int x = 0; x <= 1; x++) {
                    for (int y = 0; y <= 1; y++) {
                        int offsetSignX = Math.Sign((2 * x) - 1);
                        int offsetSignY = Math.Sign((2 * y) - 1);

                        Biome cornerBiome = worldContext.getBiomeFromBlockLocation(((_graphics.PreferredBackBufferWidth * x - worldContext.screenSpaceOffset.x) / worldContext.pixelsPerBlock) + offsetSignX * offscreenDistance, ((_graphics.PreferredBackBufferHeight * y - worldContext.screenSpaceOffset.y) / worldContext.pixelsPerBlock) + offsetSignY * offscreenDistance);

                        if (cornerBiome != null) {
                            if (!nearbyBiomes.Contains(cornerBiome)) { nearbyBiomes.Add(cornerBiome); }
                        }
                    }
                }

                for (int i = 0; i < nearbyBiomes.Count; i++) {
                    nearbyBiomes[i].tickBiome(worldContext);
                }

                biomeTickSpeed = maxBiomeTickSpeed;
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
            worldContext.animationController.tickAnimation(gameTime.ElapsedGameTime.TotalSeconds);
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
                        if (worldContext.worldArray[x, y].ID == (int)blockIDs.air || worldContext.worldArray[x, y].isBlockTransparent)
                        {
                            if (worldContext.backgroundArray[x, y] != (int)backgroundBlockIDs.air)
                            {
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
                        if (worldContext.worldArray[x, y].ID != (int)blockIDs.air)
                        {
                            if (fluidTickSpeed <= 0)
                            {
                                if (worldContext.worldArray[x, y] is FluidBlock f)
                                {
                                    f.tickFluid(worldContext);
                                }
                            }
                            int lightValue = worldContext.lightArray[x, y];
                            if (lightValue > 255)
                            {
                                lightValue = 255;
                            }
                            Color lightLevel = Color.White;
                            if (!useShaders) { lightLevel = new Color(lightValue, lightValue, lightValue); }
                            if (worldContext.exposedBlocks.ContainsKey((x, y)) && !worldContext.worldArray[x,y].isBlockTransparent) { currentlyRenderedExposedBlocks.Add((x, y)); }
                            _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[(int)spriteSheetIDs.blocks], new Rectangle(x * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x, y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y, (int)worldContext.pixelsPerBlock, (int)worldContext.pixelsPerBlock), worldContext.worldArray[x, y].sourceRectangle, lightLevel);
                        }

                    }
                }
            }
        }

        public void drawSky() {
            if (worldContext.sun.sky != null)
            {
                for (int i = 0; i < worldContext.sun.sky.skyLayers.Count; i++)
                {
                    SkyLayer layer = worldContext.sun.sky.skyLayers[i];
                    _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[worldContext.sun.sky.spriteSheet], layer.drawRectangle, layer.sourceRectangle, Color.White);
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
                if (currentBiome != null) {
                    debugInfo += currentBiome;
                    debugInfo += currentBiome.currentBiomeEntityCount;
                    debugInfo += "\n";
                }
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
            _spriteBatch.DrawString(ariel, (int)player.x / worldContext.pixelsPerBlock + ", " + (int)player.y / worldContext.pixelsPerBlock, new Vector2(10, _graphics.PreferredBackBufferHeight - 150 + 10), Color.BlueViolet);
            _spriteBatch.DrawString(ariel, (int)player.velocityX + ", " + (int)player.velocityY, new Vector2(10, _graphics.PreferredBackBufferHeight - 150 + 40), Color.BlueViolet);
            _spriteBatch.DrawString(ariel, playerAcceleration, new Vector2(10, _graphics.PreferredBackBufferHeight - 150 + 70), Color.BlueViolet);
            _spriteBatch.DrawString(ariel, (int)(1 / gameTime.ElapsedGameTime.TotalSeconds) + " fps", new Vector2(200, _graphics.PreferredBackBufferHeight - 150 + 10), Color.BlueViolet);
            _spriteBatch.DrawString(ariel, worldContext.engineController.lightingSystem.lights.Count + " lights", new Vector2(450, _graphics.PreferredBackBufferHeight - 150 + 10), Color.BlueViolet);
            _spriteBatch.DrawString(ariel, Math.Sin(worldContext.sun.angle) + " sun angle", new Vector2(450, _graphics.PreferredBackBufferHeight - 150), Color.BlueViolet);
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
                        _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[entity.spriteSheetID], new Rectangle((int)(entity.x - entity.spriteAnimator.sourceOffset.X + worldContext.screenSpaceOffset.x), (int)(entity.y - entity.spriteAnimator.sourceOffset.Y + worldContext.screenSpaceOffset.y), (int)(entity.drawWidth * worldContext.pixelsPerBlock), (int)(entity.drawHeight * worldContext.pixelsPerBlock)), entity.sourceRectangle, Color.White, entity.rotation, entity.rotationOrigin, entity.directionalEffect, 0f);
                    }
                }
            }
        }
        public void drawPlayer()
        {
            _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[player.spriteSheetID], new Rectangle((int)(player.x - player.spriteAnimator.sourceOffset.X) + worldContext.screenSpaceOffset.x, (int)(player.y - player.spriteAnimator.sourceOffset.Y) + worldContext.screenSpaceOffset.y, (int)(player.drawWidth * worldContext.pixelsPerBlock), (int)(player.drawHeight * worldContext.pixelsPerBlock)), player.sourceRectangle, Color.White, 0f, Vector2.Zero, player.directionalEffect, 0f);
        }

        public void drawMainHandCollisionBounds() {
            if (player.mainHand is INonAxisAlignedActiveCollider itemInHand) {
                _spriteBatch.Draw(redTexture, new Rectangle((int)(itemInHand.rotatedPoints[0].X + itemInHand.x + worldContext.screenSpaceOffset.x - 2), (int)(itemInHand.rotatedPoints[0].Y + itemInHand.y + worldContext.screenSpaceOffset.y - 2), 4, 4), Color.White);
                _spriteBatch.Draw(redTexture, new Rectangle((int)(itemInHand.rotatedPoints[1].X + itemInHand.x + worldContext.screenSpaceOffset.x - 2), (int)(itemInHand.rotatedPoints[1].Y + itemInHand.y + worldContext.screenSpaceOffset.y - 2), 4, 4), Color.White);
                _spriteBatch.Draw(redTexture, new Rectangle((int)(itemInHand.rotatedPoints[2].X + itemInHand.x + worldContext.screenSpaceOffset.x - 2), (int)(itemInHand.rotatedPoints[2].Y + itemInHand.y + worldContext.screenSpaceOffset.y - 2), 4, 4), Color.White);
                _spriteBatch.Draw(redTexture, new Rectangle((int)(itemInHand.rotatedPoints[3].X + itemInHand.x + worldContext.screenSpaceOffset.x - 2), (int)(itemInHand.rotatedPoints[3].Y + itemInHand.y + worldContext.screenSpaceOffset.y - 2), 4, 4), Color.White);

            }
        }
        public void drawAnimatorObjects()
        {
            for (int i = 0; i < worldContext.animationController.animators.Count; i++)
            {
                Animator a = worldContext.animationController.animators[i];
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

                _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[owner.spriteSheetID], new Rectangle((int)(owner.owner.x + worldContext.screenSpaceOffset.x + a.currentPosition.xPos + positionXOffset), (int)(owner.owner.y + worldContext.screenSpaceOffset.y + a.currentPosition.yPos), (int)(owner.drawRectangle.Width), (int)(owner.drawRectangle.Height)), owner.sourceRectangle, Color.White, (float)(owner.owner.playerDirection * (a.currentPosition.rotation)), origin, owner.drawEffect | owner.owner.directionalEffect, 0f);

            }

        }

        public void drawLight()
        {
            if (useShaders)
            {
                GraphicsDevice.SetRenderTarget(lightMap);
                GraphicsDevice.Clear(new Color(0.1f, 0.1f, 0.1f));

                //Sun pass:
                calculateSun();

                for (int i = 0; i < worldContext.engineController.lightingSystem.lights.Count; i++)
                {

                    Vector2 lightPosition = new Vector2((float)((worldContext.engineController.lightingSystem.lights[i].x + worldContext.screenSpaceOffset.x)) / _graphics.PreferredBackBufferWidth, (float)((worldContext.engineController.lightingSystem.lights[i].y + worldContext.screenSpaceOffset.y) / _graphics.PreferredBackBufferHeight));
                    calculateShadowMap(worldContext.engineController.lightingSystem.lights[i], lightPosition); //A noticable performance drop at 10 dynamic lights. At 30 lights, it drops to 9-20fps
                    calculateLightmap(worldContext.engineController.lightingSystem.lights[i], lightPosition); //Minor impact on performance
                    addLightmapToGlobalLights(worldContext.engineController.lightingSystem.lights[i]);
                }

                //I need to adjust this a bit: for each block face, run an algorithm to combine the adjacent vertices, reducing the vertex count that has to be drawn each light.
                // -> Add a vector4 to each block, that defines what faces have already been calculated and added to the face buffer. This resets each frame.
                // Ideally, reducing the number of vertices down to 20-30 from 80-90
                for (int i = 0; i < worldContext.engineController.lightingSystem.emissiveBlocks.Count; i++) {
                    Vector2 lightPosition = new Vector2((float)((worldContext.engineController.lightingSystem.emissiveBlocks[i].x * worldContext.pixelsPerBlock + 0.5 * worldContext.pixelsPerBlock) + worldContext.screenSpaceOffset.x) / _graphics.PreferredBackBufferWidth, (float)(worldContext.engineController.lightingSystem.emissiveBlocks[i].y * worldContext.pixelsPerBlock + 0.5 * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) / _graphics.PreferredBackBufferHeight);
                    //Check to make sure that the light would actually be rendered on screen instead of just some random block far away. 
                    //Will need to adjust this once off screen shadows are considered, and a ton of other things to think about
                    //Just check the blocks that are within the lights 'range'
                    if (lightPosition.X > -0.2 && lightPosition.Y > -0.2 && lightPosition.Y < 1.2 && lightPosition.X < 1.2)
                    {
                        calculateShadowMap(worldContext.engineController.lightingSystem.emissiveBlocks[i], lightPosition); //A noticable performance drop at 10 dynamic lights. At 30 lights, it drops to 9-20fps
                        calculateLightmap(worldContext.engineController.lightingSystem.emissiveBlocks[i], lightPosition); //Minor impact on performance
                        addLightmapToGlobalLights(worldContext.engineController.lightingSystem.emissiveBlocks[i]);
                    }
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

        public void calculateSun() {
            Vector2 lightPosition = new Vector2(0.5f + (float)(worldContext.sun.distance * Math.Cos((float)(worldContext.sun.angle))), 0.5f + (float)(worldContext.sun.distance * Math.Sin((float)(worldContext.sun.angle))));
            calculateSolarShadowMap(worldContext.sun, lightPosition); //A noticable performance drop at 10 dynamic lights. At 30 lights, it drops to 9-20fps
            calculateLightmap(worldContext.sun, lightPosition); //Minor impact on performance
            addLightmapToGlobalLights(worldContext.sun);
        }



        public void calculateLightmap(IEmissiveBlock lightObject, Vector2 lightPosition)
        {
            calculateLight.Parameters["lightIntensity"].SetValue(lightObject.luminosity);
            calculateLight.Parameters["lightColor"].SetValue(lightObject.lightColor);
            calculateLight.Parameters["renderDimensions"].SetValue(new Vector2(lightMap.Width, lightMap.Height));
            calculateLight.Parameters["lightPosition"].SetValue(lightPosition);


            GraphicsDevice.SetRenderTarget(lightObject.lightMap);


            _spriteBatch.Begin(effect: calculateLight);
            _spriteBatch.Draw(lightObject.shadowMap, Vector2.Zero, Color.White);
            _spriteBatch.End();

        }

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

        public void calculateSolarShadowMap(IEmissive lightObject, Vector2 lightPosition) {
            //Cast a global box only for the sun:

            GraphicsDevice.SetRenderTarget(lightObject.shadowMap);
            GraphicsDevice.Clear(Color.White);
            RasterizerState rasterizerState1 = new RasterizerState();
            rasterizerState1.CullMode = CullMode.None;
            GraphicsDevice.RasterizerState = rasterizerState1;

            int leftX = (int)Math.Floor(-worldContext.screenSpaceOffset.x / (double)worldContext.pixelsPerBlock);
            int rightX = leftX + (int)Math.Ceiling(worldContext.applicationWidth / (double)worldContext.pixelsPerBlock);
            int topY = (int)Math.Floor(-worldContext.screenSpaceOffset.y / (double)worldContext.pixelsPerBlock);
            int bottomY = topY + (int)Math.Ceiling(worldContext.applicationHeight / (double)worldContext.pixelsPerBlock);

            int lowestY = worldContext.findLowestSurfaceHeightOnScreen();
            if (lowestY * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y < 0)
            {
                //Then draw a whole box around the screen
                Vector3[] boxArray = new Vector3[5];
                boxArray[0] = new Vector3(0, 0, 0);
                boxArray[1] = new Vector3(1, 0, 0);
                boxArray[2] = new Vector3(1, 1, 0);
                boxArray[3] = new Vector3(0, 1, 0);
                boxArray[4] = new Vector3(0, 0, 0);

                drawShadow(boxArray, lightPosition);

            }
            else{
                //If the owest y is not below the screen, then render the appropriate box:
                if (!(lowestY * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y > worldContext.applicationHeight)) {
                    Vector3[] boxArray = new Vector3[5];
                    boxArray[0] = new Vector3(0, (lowestY * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y)/(float)_graphics.PreferredBackBufferHeight, 0);
                    boxArray[1] = new Vector3(1, (lowestY * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) / (float)_graphics.PreferredBackBufferHeight, 0);
                    boxArray[2] = new Vector3(1, 1, 0);
                    boxArray[3] = new Vector3(0, 1, 0);
                    boxArray[4] = new Vector3(0, (lowestY * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) / (float)_graphics.PreferredBackBufferHeight, 0);

                    drawShadow(boxArray, lightPosition);
                }



                if (leftX >= 0 && leftX < worldContext.surfaceHeight.Count())
                {
                    if (lowestY > worldContext.surfaceHeight[leftX])
                    {
                        Vector3[] boxArray = new Vector3[2];
                        boxArray[0] = new Vector3(0, (worldContext.surfaceHeight[leftX] * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) / (float)_graphics.PreferredBackBufferHeight, 0);
                        boxArray[1] = new Vector3(0, 1, 0);


                        drawShadow(boxArray, lightPosition);
                    }
                }

                if (rightX > 0 && rightX < worldContext.surfaceHeight.Count())
                {
                    if (lowestY > worldContext.surfaceHeight[rightX])
                    {
                        Vector3[] boxArray = new Vector3[2];
                        boxArray[0] = new Vector3(1, (worldContext.surfaceHeight[rightX] * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) / (float)_graphics.PreferredBackBufferHeight, 0);
                        boxArray[1] = new Vector3(1, 1, 0);


                        drawShadow(boxArray, lightPosition);
                    }
                }
                
            }

            //Top of the screen pass:
            edgeShadowPass(leftX, topY - 1, rightX, topY - 1, lightPosition);
            //Right side:
            edgeShadowPass(rightX + 1, topY, rightX + 1, bottomY, lightPosition);
            //Bottom
            edgeShadowPass(leftX, bottomY + 1, rightX, bottomY + 1, lightPosition);
            //Left side:
            edgeShadowPass(leftX - 1, topY, leftX - 1, bottomY, lightPosition);



            foreach ((int, int) coord in currentlyRenderedExposedBlocks)
            {
                int x = coord.Item1;
                int y = coord.Item2;

                if (worldContext.worldArray[x, y].faceVertices != null)
                {
                    Vector3[] vertexArray = new Vector3[worldContext.worldArray[x, y].faceVertices.Count];


                    for (int g = 0; g < vertexArray.Length; g++)
                    {
                        vertexArray[g] = new Vector3((worldContext.worldArray[x, y].faceVertices[g].X * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x) / _graphics.PreferredBackBufferWidth, (worldContext.worldArray[x, y].faceVertices[g].Y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) / _graphics.PreferredBackBufferHeight, 0);
                    }

                    drawShadow(vertexArray, lightPosition);
                }
            }
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

            

            //Calculate a pass across the sides of the screen:

            int leftX = (int)Math.Floor(-worldContext.screenSpaceOffset.x / (double)worldContext.pixelsPerBlock);
            int rightX = leftX + (int)Math.Ceiling(worldContext.applicationWidth / (double)worldContext.pixelsPerBlock);
            int topY = (int)Math.Floor(-worldContext.screenSpaceOffset.y / (double)worldContext.pixelsPerBlock);
            int bottomY = topY + (int)Math.Ceiling(worldContext.applicationHeight / (double)worldContext.pixelsPerBlock);


            //Top of the screen pass:
            edgeShadowPass(leftX, topY - 1, rightX, topY - 1, lightPosition);
            //Right side:
            edgeShadowPass(rightX + 1, topY, rightX + 1, bottomY, lightPosition);
            //Bottom
            edgeShadowPass(leftX, bottomY + 1, rightX, bottomY + 1, lightPosition);
            //Left side:
            edgeShadowPass(leftX - 1, topY, leftX - 1, bottomY, lightPosition);



            foreach ((int, int) coord in currentlyRenderedExposedBlocks)
            {
                int x = coord.Item1;
                int y = coord.Item2;

                if (worldContext.worldArray[x, y].faceVertices != null)
                {
                    Vector3[] vertexArray = new Vector3[worldContext.worldArray[x, y].faceVertices.Count];


                    for (int g = 0; g < vertexArray.Length; g++)
                    {
                        vertexArray[g] = new Vector3((worldContext.worldArray[x, y].faceVertices[g].X * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x) / _graphics.PreferredBackBufferWidth, (worldContext.worldArray[x, y].faceVertices[g].Y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) / _graphics.PreferredBackBufferHeight, 0);
                    }

                    drawShadow(vertexArray, lightPosition);
                }
            }
        }

        public void calculateShadowMap(IEmissiveBlock lightObject, Vector2 lightPosition)
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

            int maxBlockCount = 0;
            if (lightObject.range > 0) {
                maxBlockCount = (int)(4 * (lightObject.range / worldContext.pixelsPerBlock) * (lightObject.range / worldContext.pixelsPerBlock));
            }

            int blockCount = 0;

            foreach ((int, int) coord in currentlyRenderedExposedBlocks)
            {
                int x = coord.Item1;
                int y = coord.Item2;
                double distance = Math.Pow(((x * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x) - lightPosition.X * _graphics.PreferredBackBufferWidth) * ((x * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x) - lightPosition.X * _graphics.PreferredBackBufferWidth) + ((y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) - lightPosition.Y * _graphics.PreferredBackBufferHeight) * ((y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) - lightPosition.Y * _graphics.PreferredBackBufferHeight), 0.5);

                if (maxBlockCount > 0) {
                    if (blockCount >= maxBlockCount) { break; }
                }

                if (lightObject.range == 0 || lightObject.range > distance)
                {
                    blockCount += 1;
                    Vector3[] vertexArray = new Vector3[worldContext.worldArray[x, y].faceVertices.Count];
                    for (int g = 0; g < vertexArray.Length; g++)
                    {
                        vertexArray[g] = new Vector3((worldContext.worldArray[x, y].faceVertices[g].X * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.x) / _graphics.PreferredBackBufferWidth, (worldContext.worldArray[x, y].faceVertices[g].Y * worldContext.pixelsPerBlock + worldContext.screenSpaceOffset.y) / _graphics.PreferredBackBufferHeight, 0);
                    }
                    drawShadow(vertexArray, lightPosition);
                }
            }
        }

        public void edgeShadowPass(int startX, int startY, int finalX, int finalY, Vector2 lightPosition)
        {
            //This will have a singular block that is ignored on the boundaries of caves
            bool startedALine = false;
            Vector3[] edgeArray = new Vector3[2];
            for (int x = startX; x <= finalX; x++)
            {
                for (int y = startY; y <= finalY; y++)
                {
                    if (x >= 0 && y >= 0 && x < worldContext.worldArray.GetLength(0) && y < worldContext.worldArray.GetLength(1))
                    {
                        if ((!worldContext.worldArray[x, y].isBlockTransparent && worldContext.worldArray[x, y].ID != (int)blockIDs.air) || (x == finalX && y == finalY))
                        {
                            if (!startedALine && !(x == finalX && y == finalY))
                            {
                                edgeArray[0] = new Vector3((float)worldContext.locationToShaderSpace(x, new Vector2(1, 0)), (float)worldContext.locationToShaderSpace(y, new Vector2(0, 1)), 0);
                                startedALine = true;

                            }
                            else
                            {
                                edgeArray[1] = new Vector3((float)worldContext.locationToShaderSpace(x, new Vector2(1, 0)), (float)worldContext.locationToShaderSpace(y, new Vector2(0, 1)), 0);
                            }
                        }
                        else
                        {

                            drawShadow(edgeArray, lightPosition);
                            startedALine = false;
                        }
                    }
                }
            }

            if (startedALine)
            {
                drawShadow(edgeArray, lightPosition);

            }
        }
        public void drawShadow(Vector3[] vertexArray, Vector2 lightPosition) {
            for (int i = 0; i < vertexArray.Length - 1; i++)
            {

                if (vertexArray[i].X != vertexArray[i + 1].X && vertexArray[i].Y != vertexArray[i + 1].Y) { continue; }

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
        public void addLightmapToGlobalLights(IEmissiveBlock lightObject)
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

    public class DrawnClass {
        public double x { get; set; }
        public double y { get; set; }

        public Rectangle sourceRectangle;
        public Rectangle drawRectangle;
        public int spriteSheetID { get; set; }

        //Some things just if they're later useful

        public SpriteEffects drawEffect { get; set; }
        public double rotation { get; set; }
        public Vector2 rotationOrigin { get; set; }
    }

    public enum blockIDs {
        //Written in order of their integer IDs
        air,
        stone,
        dirt,
        grass,
        torch,
        chest,
        water,
        ironOre,
        treeTrunk,
        leaves,
        semiLeaves,
        bush,
        bigBush
    }
    public enum backgroundBlockIDs {
        air,
        stone,
        woodenPlanks
    }

    public enum oreIDs {
        iron
    }
    public enum spriteSheetIDs
    {
        blocks,
        weapons,
        blockItems,
        player,
        arrow,
        mainMenuUI,
        blockBackground,
        inventoryUI,
        healthUI,
        deathScreen,
        armour,
        accessories,
        pixelNumbers,
        evolutionBackground,
        evolutionIcons,
        evolutionCounterCharacters,
        oreSpriteSheet,
        ingotSpriteSheet,
        stringRendering,
        tooltipBackground,
        skyLayers
    }
    public enum Scene {
        MainMenu,
        Game,
        Evolution
    }
    public enum ArmorType {
        Head,
        Chest,
        Legs,
        Boots
    }
    public enum DamageType {
        Falldamage,
        Drowning,
        EntityAttack,
        EntityPassive,
        Effect,
    }
    public enum ExperienceField {
        Knowledge,
        Durability,
        Damage,
        Maneuverability,
    }

    public enum TextStyle
    {
        None = 0b_0000_0000,
        h1 = 0b_0000_0001,
        h2 = 0b_0000_0010,
        h3 = 0b_0000_0100,
        h4 = 0b_0000_1000,
        h5 = 0b_0001_0000,

    }
    public enum Tag
    {
        None = 0b_0000_0000,
        Purple = 0b_0000_0001,
        Green = 0b_0000_0010,
        Gold = 0b_0000_0100,
        Red = 0b_0000_1000,
        Grey = 0b_0001_0000,
        H1 = 0b_0010_0000,
        H2 = 0b_0100_0000,
        H3 = 0b_1000_0000,

    }


}
