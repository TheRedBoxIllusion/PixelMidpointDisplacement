using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

using System.Collections.Generic;

using System.IO;
using System.Linq;
using System.Numerics;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;
using Vector4 = Microsoft.Xna.Framework.Vector4;

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

        - Make a centralised 'rendering' class, or improve the sprite renderer controller to include:                                                               - Very important!!
            - A class dictionary that contains the sprite sheet ID (or just the sprite sheet itself) and the source rectangle of that object (y value primarily)
                -> Classes can define their own state, and each object has that state considered in the sprite renderer that adjusts the source rectangle X


    Refactoring subset:
        JSON overhaul:
            - Turn various item constants into a .json data file for that item    
        
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

        AnimationController animationController;

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
            animationController = new AnimationController();

            worldContext = new WorldContext(engineController, animationController);
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

            if (currentScene == Scene.Game)
            {
                worldContext.sun.updateTime(gameTime.ElapsedGameTime.TotalSeconds);
                updateChatSystem(gameTime);
                updatePhysicsObjects(gameTime);
                calculateScreenspaceOffset();
                updateInteractiveBlocks(gameTime);
                checkCollisions();
                updateBiome(gameTime);
                tickAnimations(gameTime);
                updateEntities(gameTime);
                worldContext.sun.sky.updateSky(worldContext);
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
                int biomeLeniency = 20 * worldContext.pixelsPerBlock;
                int cummulativebiomeBlockWidth = 0;
                bool foundABiomeThePlayerIsIn = false;
                bool stopAttemptingToFindBiomes = false;

                for (int i = 0; i < worldContext.surfaceWorldBiomeList.Count; i++) {
                    //If the player is within a range of the current biome:
                    //If the player is between the start and the end of the biome
                   
                    if (cummulativebiomeBlockWidth * worldContext.pixelsPerBlock < -worldContext.screenSpaceOffset.x + biomeLeniency && (cummulativebiomeBlockWidth + worldContext.surfaceWorldBiomeList[i].biomeDimensions.width) * worldContext.pixelsPerBlock > -worldContext.screenSpaceOffset.x + worldContext.applicationWidth - biomeLeniency)
                    {
                        currentBiome = worldContext.surfaceWorldBiomeList[i];
                        worldContext.surfaceWorldBiomeList[i].tickBiome(worldContext);
                        foundABiomeThePlayerIsIn = true;
                        stopAttemptingToFindBiomes = true;
                    }
                    else {
                        foundABiomeThePlayerIsIn = false;
                    }

                    //If at least one biome was found for the player, and there is no more, then break out of the loop
                    if (stopAttemptingToFindBiomes && foundABiomeThePlayerIsIn == false){
                        break;
                    }


                        cummulativebiomeBlockWidth += worldContext.surfaceWorldBiomeList[i].biomeDimensions.width;
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
            for (int i = 0; i < worldContext.sun.sky.skyLayers.Count; i++) {
                SkyLayer layer = worldContext.sun.sky.skyLayers[i];
                _spriteBatch.Draw(worldContext.engineController.spriteController.spriteSheetList[worldContext.sun.sky.spriteSheet], layer.drawRectangle, layer.sourceRectangle, Color.White);
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
                        _spriteBatch.Draw(entity.spriteAnimator.spriteSheet, new Rectangle((int)(entity.x - entity.spriteAnimator.sourceOffset.X + worldContext.screenSpaceOffset.x), (int)(entity.y - entity.spriteAnimator.sourceOffset.Y + worldContext.screenSpaceOffset.y), (int)(entity.drawWidth * worldContext.pixelsPerBlock), (int)(entity.drawHeight * worldContext.pixelsPerBlock)), entity.spriteAnimator.sourceRect, Color.White, entity.rotation, entity.rotationOrigin, entity.directionalEffect, 0f);
                    }
                }
            }
        }
        public void drawPlayer()
        {
            _spriteBatch.Draw(player.spriteAnimator.spriteSheet, new Rectangle((int)(player.x - player.spriteAnimator.sourceOffset.X) + worldContext.screenSpaceOffset.x, (int)(player.y - player.spriteAnimator.sourceOffset.Y) + worldContext.screenSpaceOffset.y, (int)(player.drawWidth * worldContext.pixelsPerBlock), (int)(player.drawHeight * worldContext.pixelsPerBlock)), player.spriteAnimator.sourceRect, Color.White, 0f, Vector2.Zero, player.directionalEffect, 0f);
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

        public List<Biome> surfaceWorldBiomeList = new List<Biome>();
        public List<SubterraneanBiome> subterraneanWorldBiomeList = new List<SubterraneanBiome>();
        public int[,] lightArray { get; set; }
        public int pixelsPerBlock { get; set; } = 4; //Overwritten by the settings file

        public int pixelsPerBlockAfterGeneration;

        public int applicationWidth;
        public int applicationHeight;

        public Sun sun;

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
            generateBlockReferences();

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

            surfaceWorldBiomeList = worldGenerator.biomeList;

            lightArray = engineController.lightingSystem.initialiseLight(worldDimensions, surfaceHeight);
            engineController.lightingSystem.generateSunlight(intWorldArray, surfaceHeight);
            engineController.lightingSystem.calculateSurfaceLight(intWorldArray, surfaceBlocks);
            

            for (int x = 0; x < worldArray.GetLength(0); x++)
            {
                for (int y = 0; y < worldArray.GetLength(1); y++)
                {
                    generateInstanceFromID(intWorldArray, blockIDFromInt[intWorldArray[x, y]], x, y);
                }
            }

            for (int x = 0; x < worldArray.GetLength(0); x++)
            {
                for (int y = 0; y < worldArray.GetLength(1); y++)
                {
                    addBlockToDictionaryIfExposedToAir(worldArray, x, y);
                }
            }

                    updatePixelsPerBlock(pixelsPerBlockAfterGeneration);

            sun = new Sun(this);

            updateSurfaceHeight();

            player.setSpawn((int)player.x, pixelsPerBlock * (surfaceHeight[(int)Math.Floor(player.x / pixelsPerBlock)] - 3));
            player.respawn();

        }

        public void updateSurfaceHeight() {
            for (int x = 0; x < worldArray.GetLength(0); x++) {
                int y = surfaceHeight[x];
                while (worldArray[x, y].isBlockTransparent || worldArray[x, y].ID == (int)blockIDs.air) {
                    y += 1;
                }
                surfaceHeight[x] = y;
            }
        }
        public void setApplicationDimensions(int width, int height) {
            applicationHeight = height;
            applicationWidth = width;
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
            else if (ID == blockIDs.torch)
            {
                worldArray[x, y] = new TorchBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.chest)
            {
                worldArray[x, y] = new ChestBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.water)
            {
                worldArray[x, y] = new FluidBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.ironOre)
            {
                worldArray[x, y] = new OreBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.treeTrunk)
            {
                worldArray[x, y] = new TreeBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.leaves)
            {
                worldArray[x, y] = new LeafBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.semiLeaves)
            {
                worldArray[x, y] = new SemiLeafBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.bush) {
                worldArray[x, y] = new BushBlock(blockFromID[ID]);
            }
            else if (ID == blockIDs.bigBush)
            {
                worldArray[x, y] = new BigBushBlock(blockFromID[ID]);
            }

            worldArray[x, y].setupInitialData(this, intArray, (x, y));
        }

        public double locationToShaderSpace(int value, Vector2 axis) {
            if (axis.X > 0)
            {
                return ((double)value * pixelsPerBlock + screenSpaceOffset.x) / (double)applicationWidth;

            }
            else {
                return ((double)value * pixelsPerBlock + screenSpaceOffset.y) / (double)applicationHeight;
            }
        }
        public int findLowestSurfaceHeightOnScreen() {
            int y = 0;
            for (int x = -screenSpaceOffset.x / pixelsPerBlock; x < (-screenSpaceOffset.x + applicationWidth)/pixelsPerBlock; x++) {
                if (surfaceHeight[x] > y) { y = surfaceHeight[x]; }
            }
            return y;
        }

        public int findFirstSurfaceHeightVisibleFromRight() {
            for (int x = -screenSpaceOffset.x / pixelsPerBlock; x < (-screenSpaceOffset.x + applicationWidth) / pixelsPerBlock; x++)
            {
                if (surfaceHeight[x] * pixelsPerBlock + screenSpaceOffset.y >= 0 && surfaceHeight[x] * pixelsPerBlock + screenSpaceOffset.y < applicationHeight) { return x; }
            }
            return -1;
        }

        public int findFirstSurfaceHeightVisibleFromLeft() {
            for (int x = (-screenSpaceOffset.x + applicationWidth) / pixelsPerBlock; x < (-screenSpaceOffset.x) / pixelsPerBlock; x--)
            {
                if (surfaceHeight[x] * pixelsPerBlock + screenSpaceOffset.y >= 0 && surfaceHeight[x] * pixelsPerBlock + screenSpaceOffset.y < applicationHeight) { return x; }
            }
            return -1;
        }

        public void addBlockToDictionaryIfExposedToAir(int[,] blockArray, int x, int y)
        {
            if (x > 0 && y > 0 && x < worldArray.GetLength(0) - 1 && y < worldArray.GetLength(1) - 1)
            {

                if ((blockArray[x - 1, y] == (int)blockIDs.air || blockArray[x + 1, y] == (int)blockIDs.air || blockArray[x, y - 1] == (int)blockIDs.air || blockArray[x, y + 1] == (int)blockIDs.air ||
                    blockFromID[(blockIDs)blockArray[x - 1, y]].isBlockTransparent|| blockFromID[(blockIDs)blockArray[x + 1, y]].isBlockTransparent|| blockFromID[(blockIDs)blockArray[x, y - 1]].isBlockTransparent || blockFromID[(blockIDs)blockArray[x, y + 1]].isBlockTransparent) && blockArray[x,y] != (int)blockIDs.air) //Then it is exposed to air
                {
                    if (blockArray[x, y] != (int)blockIDs.torch && blockArray[x,y] != (int)blockIDs.chest )
                    {
                        exposedBlocks.Add((x, y), worldArray[x, y]);
                        worldArray[x, y].setupFaceVertices(calculateExposedFaces(blockArray, x, y));
                    }
                }
            }
        }

        /*
         * This is why blocks that are transparent are still blocking other blocks from casing shadows that are below them!
         
         */
        public Vector4 calculateExposedFaces(int[,] blockArray, int x, int y)
        {
            return new Vector4(Convert.ToInt32((blockArray[x, y - 1] == (int)blockIDs.air)), Convert.ToInt32(blockArray[x + 1, y] == (int)blockIDs.air), Convert.ToInt32(blockArray[x, y + 1] == (int)blockIDs.air), Convert.ToInt32(blockArray[x - 1, y] == (int)blockIDs.air));
        }

        public void addBlockToDictionaryIfExposedToAir(Block[,] blockArray, int x, int y)
        {
            if (x > 0 && y > 0 && x < worldArray.GetLength(0) - 1 && y < worldArray.GetLength(1) - 1)
            {
                bool isBlockExposedToAir = (blockArray[x - 1, y].ID == (int)blockIDs.air || blockArray[x + 1, y].ID == (int)blockIDs.air || blockArray[x, y - 1].ID == (int)blockIDs.air || blockArray[x, y + 1].ID == (int)blockIDs.air) && blockArray[x, y].ID != (int)blockIDs.air;
                bool isBlockExposedToATransparentBlock = (blockArray[x - 1, y].isBlockTransparent || blockArray[x + 1, y].isBlockTransparent || blockArray[x, y - 1].isBlockTransparent || blockArray[x, y + 1].isBlockTransparent) && blockArray[x, y].ID != (int)blockIDs.air;
                if (isBlockExposedToAir || isBlockExposedToATransparentBlock)
                {
                    if (!blockArray[x, y].isBlockTransparent)
                    {
                        if (!exposedBlocks.ContainsKey((x, y)))
                        {
                            exposedBlocks.Add((x, y), worldArray[x, y]);
                            worldArray[x, y].setupFaceVertices(calculateExposedFaces(blockArray, x, y));
                        }
                        else
                        {
                            worldArray[x, y].setupFaceVertices(calculateExposedFaces(blockArray, x, y));
                        }
                    }
                }
            }
        }

        public Vector4 calculateExposedFaces(Block[,] blockArray, int x, int y)
        {
            return new Vector4(Convert.ToInt32((blockArray[x, y - 1].ID == (int)blockIDs.air) || blockArray[x, y - 1].isBlockTransparent), Convert.ToInt32(blockArray[x + 1, y].ID == (int)blockIDs.air || blockArray[x + 1, y].isBlockTransparent), Convert.ToInt32(blockArray[x, y + 1].ID == (int)blockIDs.air || blockArray[x, y + 1].isBlockTransparent), Convert.ToInt32(blockArray[x - 1, y].ID == (int)blockIDs.air || blockArray[x - 1, y].isBlockTransparent));
        }
        public void generateBlockReferences()
        {
            blockFromID.Add(blockIDs.air, new Block(new Rectangle(0, 0, 0, 0), intFromBlockID[blockIDs.air])); //Air block
            blockFromID.Add(blockIDs.stone, new Block(new Rectangle(0, 0, 32, 32), intFromBlockID[blockIDs.stone]));
            blockFromID.Add(blockIDs.dirt, new Block(new Rectangle(0, 32, 32, 32), intFromBlockID[blockIDs.dirt]));
            blockFromID.Add(blockIDs.grass, new GrassBlock(new Rectangle(0, 64, 32, 32), intFromBlockID[blockIDs.grass]));
            blockFromID.Add(blockIDs.torch, new TorchBlock(new Rectangle(0, 96, 32, 32), intFromBlockID[blockIDs.torch]));
            blockFromID.Add(blockIDs.chest, new ChestBlock(new Rectangle(0, 128, 32, 32), (int)blockIDs.chest));
            blockFromID.Add(blockIDs.water, new FluidBlock(new Rectangle(0, 160, 32, 32), intFromBlockID[blockIDs.water]));
            blockFromID.Add(blockIDs.ironOre, new OreBlock(new Rectangle(0, 224, 32, 32), (int)blockIDs.ironOre, (int)oreIDs.iron));
            blockFromID.Add(blockIDs.treeTrunk, new TreeBlock(new Rectangle(0, 256, 32, 32), (int)blockIDs.treeTrunk));
            blockFromID.Add(blockIDs.leaves, new LeafBlock(new Rectangle(0, 288, 32, 32), (int)blockIDs.leaves));
            blockFromID.Add(blockIDs.semiLeaves, new SemiLeafBlock(new Rectangle(0, 288, 32, 32), (int)blockIDs.semiLeaves));
            blockFromID.Add(blockIDs.bush, new BushBlock(new Rectangle(0, 320, 32, 32), (int)blockIDs.bush));
            blockFromID.Add(blockIDs.bigBush, new BigBushBlock(new Rectangle(96, 320, 32, 32), (int)blockIDs.bigBush));
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

        public Biome getBiomeFromBlockLocation(int x, int y) {
            //Go through each subterrainean biome
            for (int i = 0; i < subterraneanWorldBiomeList.Count; i++) {
                bool withinX = false;
                if (x >= subterraneanWorldBiomeList[i].x && x <= subterraneanWorldBiomeList[i].x + subterraneanWorldBiomeList[i].biomeDimensions.width) {
                    withinX = true;
                }
                bool withinY = false;
                if(y >= subterraneanWorldBiomeList[i].y && y <= subterraneanWorldBiomeList[i].y + subterraneanWorldBiomeList[i].biomeDimensions.height)
                {
                    withinY = true;
                }

                if (withinX && withinY) {
                    return subterraneanWorldBiomeList[i];
                }
            }

            //Presuming no biomes were found, then determine which surface biome:
            int cummulativeLength = 0;
            for (int i = 0; i < surfaceWorldBiomeList.Count; i++) {
                if (x > cummulativeLength && x < cummulativeLength + surfaceWorldBiomeList[i].biomeDimensions.width) {
                    return surfaceWorldBiomeList[i];
                }
                cummulativeLength += surfaceWorldBiomeList[i].biomeDimensions.width;
            }

            return null;
        }
        public void setPlayer(Player player) {
            this.player = player;
        }

        public bool damageBlock(double damageStrength, double durabilityLoss, int x, int y) {
            bool canDamageBlock = false;

            if (worldArray[x, y].ID != 0)
            {
                if (damageStrength >= worldArray[x, y].hardness)
                {
                    worldArray[x, y].durability -= durabilityLoss;
                
                    return true;
                }
                else {
                    return false;
                }
            }
            else {
                return false;
            }
        }
        public bool deleteBlock(int x, int y)
        {
            if (worldArray[x, y].ID != 0)
            {
                worldArray[x, y].onBlockDestroyed(exposedBlocks, this);
                worldArray[x, y] = new Block(blockFromID[blockIDs.air]);
                worldArray[x, y].setLocation((x, y));
                for (int checkX = x - 1; checkX <= x + 1; checkX++) {
                    for (int checkY = y - 1; checkY <= y + 1; checkY++) {
                        if (worldArray[checkX, checkY].ID != (int)blockIDs.air) {
                            addBlockToDictionaryIfExposedToAir(worldArray, checkX, checkY);
                            worldArray[checkX, checkY].updateBlock(this);
                        }
                    }
                }

                return true;
            }

            return false;
        }
        public bool addBlock(int x, int y, int ID)
        {
            if (worldArray[x, y].ID == (int)blockIDs.air && blockFromID[blockIDFromInt[ID]].canBlockBePlaced(this, (x, y)))
            {
                worldArray[x, y] = blockFromID[blockIDFromInt[ID]].copyBlock();
                worldArray[x, y].onBlockPlaced(this, (x, y));
                addBlockToDictionaryIfExposedToAir(worldArray, x, y);
                return true;
            }

            return false;
        }

        public bool setBackground(int x, int y, int ID) {
            if (x >= 0 && y >= 0 && x < backgroundArray.GetLength(0) && y < backgroundArray.GetLength(1)) {
                if (backgroundArray[x, y] != ID) {
                    backgroundArray[x, y] = ID;
                    return true;
                }
            }
            return false;
        }
    }

    public class Sun : IEmissive {
        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }
        public float baseLuminosity;
        public float range { get; set; }
        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }
        public double x { get; set; }
        public double y { get; set; }

        public double time = 0;

        public double dayDuration = 1 * 60;

        public const double horizonAngle = 3 * Math.PI / 4;

        public double angle { get; set; }
        public double distance { get; set; }

        public double coefficientOfDepthDecay = 0.1;

        public Sky sky = new Sky();

        public WorldContext worldContext;

        public Sun(WorldContext worldContext) {
            lightColor = new Vector3(155, 155, 145);
            luminosity = 14000;
            baseLuminosity = luminosity;
            range = 150;
            x = 50;
            y = 50;
            shadowMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));
            lightMap = new RenderTarget2D(worldContext.engineController.lightingSystem.graphics.GraphicsDevice, (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferWidth * worldContext.engineController.lightingSystem.shaderPrecision), (int)(worldContext.engineController.lightingSystem.graphics.PreferredBackBufferHeight * worldContext.engineController.lightingSystem.shaderPrecision));

            angle = -3 * Math.PI/4;
            distance = 3;
            this.worldContext = worldContext;
        }

        public void updateTime(double elapsedTime) {
            time += elapsedTime;

            double dayTime = time % dayDuration;
            
            angle = ((dayTime / dayDuration) * 0.75 * horizonAngle) - horizonAngle;

            lightColor = new Vector3(lightColor.X, 55 + 100f * (float)Math.Sin(Math.PI * dayTime / dayDuration), 145f * (float)Math.Sin(Math.PI * dayTime/dayDuration));

            //Adjust the luminosity as the player goes deeper:
            luminosity = baseLuminosity;
            double playerDistanceDown = worldContext.player.y / (double)worldContext.pixelsPerBlock - worldContext.surfaceHeight[(int)(worldContext.player.x/worldContext.pixelsPerBlock)];

            if (playerDistanceDown > 0) {
                luminosity = (float)(baseLuminosity / (coefficientOfDepthDecay * (playerDistanceDown + 1)));
                if (luminosity > baseLuminosity) {
                    luminosity = baseLuminosity;
                }
            }
        }
    }

    public class Sky {
        public int spriteSheet = (int)spriteSheetIDs.skyLayers;
        public List<SkyLayer> skyLayers = new List<SkyLayer>();

        public Sky() {
            skyLayers.Add(new SkyLayer(0, new Rectangle(480, 1080, 480, 270), new Rectangle(0, 0, 1920, 1080)));
            skyLayers.Add(new SkyLayer(.004, new Rectangle(480, 810, 480, 270), new Rectangle(0, 0, 1920, 1080)));
            skyLayers.Add(new SkyLayer(.06, new Rectangle(480, 540, 480, 270), new Rectangle(0, 0, 1920, 1080)));
            skyLayers.Add(new SkyLayer(.08, new Rectangle(480, 270, 480, 270), new Rectangle(0, 0, 1920, 1080)));

            skyLayers.Add(new SkyLayer(0.1, new Rectangle(0, 0, 480, 270), new Rectangle(0,0,1920,1080)));
        }
        public void updateSky(WorldContext wc) {
            for (int i = 0; i < skyLayers.Count; i++) {
                skyLayers[i].updateLocation(wc.screenSpaceOffset.x/(double)wc.pixelsPerBlock, wc.screenSpaceOffset.y/(double)wc.pixelsPerBlock);
            }
        }
    }

    public class SkyLayer {
        public double movement;
        public Rectangle sourceRectangle;
        public Rectangle drawRectangle;
        public int baseX;
        public SkyLayer(double motion, Rectangle sourceRect, Rectangle drawRect) {
            movement = motion;
            sourceRectangle = sourceRect;
            drawRectangle = drawRect;
            baseX = sourceRectangle.X;
        }

        //I'll have to update this to be the source rectangle, so that it can include the parts extended off screen. Maybe the y is draw, and the x is source. Or not have any y variation
        public void updateLocation(double x, double y) {
            sourceRectangle.X = baseX + (int)(x * movement);
            //drawRectangle.Y = (int)(y * movement);
        }
    }
    public class WorldGenerator {
        public WorldContext worldContext;

        public int[,] worldArray;
        public int[,] backgroundArray;
        public int[] surfaceHeight;
        List<(int x, int y)> surfaceBlocks = new List<(int x, int y)>(); //This list contains all the blocks facing the surface, not only the ones that are highest. Eg. cliff faces


        double[,] perlinNoiseArray;
        public BlockGenerationVariables[,] brownianMotionArray;
        public List<BlockGenerationVariables> ores = new List<BlockGenerationVariables>();
        int maxAttempts = 16;

        public List<Biome> biomeList = new List<Biome>();
        public List<SubterraneanBiome> subterraneanBiomeList = new List<SubterraneanBiome>();
        List<Biome> biomeStencilList = new List<Biome>() {
            new MeadowBiome(),
            new MountainBiome()
        };
        List<SubterraneanBiome> subterraneanBiomeStencilList = new List<SubterraneanBiome>() {
            new CaveBiome()
        };

        const int subterraneanBiomeSpawnAttempt = 15;

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
        int vectorCount = 5;
        double vectorAngleOffset = 0;



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
            generateSurfaceBiomes(worldDimensions);
            generateSubterraneanBiomes(worldDimensions);
            
            //Generate the worlds ores now:
            SeededBrownianMotion sbm = new SeededBrownianMotion();
            brownianMotionArray = sbm.brownianAlgorithm(brownianMotionArray, maxAttempts, fillOutput : true);

            //Go back through the biomes and generate caves, structures and the likes:
            for (int i = 0; i < biomeList.Count; i++) {
                combineAlgorithms(biomeList[i].biomeOffset, i);
                biomeList[i].generateBackground();
                biomeList[i].generateStructures();
            }

            for (int i = 0; i < subterraneanBiomeList.Count; i++)
            {
                combineAlgorithmsSubterranean((subterraneanBiomeList[i].x, subterraneanBiomeList[i].y), i, subterraneanBiomeList[i].externalBiomeIndex);
                subterraneanBiomeList[i].generateBackground();
                subterraneanBiomeList[i].generateStructures();
            }

            for (int i = 0; i < biomeList.Count; i++) {
                biomeList[i].generateFluids();

            }

            for (int i = 0; i < subterraneanBiomeList.Count; i++) {
                subterraneanBiomeList[i].generateFluids();
            }

            for (int i = 0; i < biomeList.Count; i++)
            {
                biomeList[i].generateDecorations();

            }

            for (int i = 0; i < subterraneanBiomeList.Count; i++)
            {
                subterraneanBiomeList[i].generateDecorations();
            }

            //To generate something on the right. Put it here
        }

        public void generateSurfaceBiomes((int width, int height) worldDimensions) {
            //In blocks. The biome offset is in blocks, the points just get converted into pixel space
            int currentLeadingWidth = 0;
            //To generate something on the left. Put it here
            //Blocks aren't generation. What's happening is that there's only blocks at the point when the biomes are generated
            Biome ocean = biomeStencilList[0].generateBiomeCopy((0, horizonLine), this, (0, 0), (0, worldDimensions.height));

            biomeList.Add(ocean);
            ocean.generateSurfaceTerrain();

            ocean.generateOres();


            currentLeadingWidth += ocean.biomeDimensions.width;

            while (worldDimensions.width - currentLeadingWidth > rightMountainRangeWidth)
            {
                int biomeNumber = new Random().Next(biomeStencilList.Count);
                //Generate a copy of the biome and pass in the rightmost point of the most recent biome terrain in the list 

                Biome biome = biomeStencilList[biomeNumber].generateBiomeCopy(biomeList[biomeList.Count - 1].initialPoints[biomeList[biomeList.Count - 1].initialPoints.Count - 1], this, (currentLeadingWidth, 0), (0, worldDimensions.height));
                biomeList.Add(biome);
                biome.generateSurfaceTerrain();
                biome.generateOres();

                currentLeadingWidth += biome.biomeDimensions.width;
            }
        }

        public void generateSubterraneanBiomes((int width, int height) worldDimensions) {
            for (int i = 0; i < subterraneanBiomeStencilList.Count; i++)
            {
                for (int c = 0; c < subterraneanBiomeStencilList[i].biomesPerWorld; c++)
                {
                    SubterraneanBiome biome = (SubterraneanBiome)subterraneanBiomeStencilList[i].generateBiomeCopy((0, 900), this, (0, 0), (0, 0));

                    bool foundASpot = false;
                    
                    int attempts = 0;
                    Random r = new Random();
                    int biomeX = r.Next(0, worldDimensions.width);
                    int biomeY = r.Next(biome.minY, biome.maxY);
                    
                    while (!foundASpot && attempts < subterraneanBiomeSpawnAttempt) {
                        biomeX = r.Next(0, worldDimensions.width);
                        if (surfaceHeight[biomeX] + biome.maxY < worldArray.GetLength(1))
                        {
                            biomeY = r.Next(surfaceHeight[biomeX] + biome.minY, surfaceHeight[biomeX] + biome.maxY);
                        }
                        else {
                            //biomeY = r.Next(surfaceHeight[biomeX] + biome.minY, worldArray.GetLength(1));
                        }
                        //Get the biome the x and y is in                        
                        int cummulativeWidth = 0;
                        for (int b = 0; b < biomeList.Count; b++) {
                            if (biomeX > cummulativeWidth && biomeX < cummulativeWidth + biomeList[b].biomeDimensions.width) {
                                if (biome.biomesThisCanSpawnIn.Contains(biomeList[b].GetType())) {
                                    foundASpot = true;
                                    biome.externalBiomeIndex = b;
                                    biome.setBiomeLocation(biomeX, biomeY);
                                }
                                break;
                            }
                        }
                    }

                    if (foundASpot)
                    {
                        subterraneanBiomeList.Add(biome);
                        biome.generateOres();
                    }
                }
            }
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

                    if (!isStillSurface && x >= 0 && y >= 0 && x < worldArray.GetLength(0) && y < worldArray.GetLength(1)) {

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
                if (surfaceBlocks[i].x > 0 && surfaceBlocks[i].y > 0 && surfaceBlocks[i].x < worldArray.GetLength(0) - 1 && surfaceBlocks[i].y < worldArray.GetLength(1) - 1)

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

                    if (x - biomeOffset.x < blockBlendRange && biomeIndex > 0) { threshold = biomeList[biomeIndex - 1].changeThresholdByDepth((biomeOffset.x - 1, y)) + (x - biomeOffset.x) * (threshold - biomeList[biomeIndex - 1].changeThresholdByDepth((biomeOffset.x - 1, y))) / blockBlendRange; }
                    if (perlinNoiseArray[x, y] > threshold)
                    { //If it's above the block threshold, set the block to be air, 
                        worldArray[x, y] = 0;

                    }
                    else if (brownianMotionArray[x, y] != null && worldArray[x, y] != 0) //If the brownian motion defined it, and it's solid from the midpoint generation
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


        private void combineAlgorithmsSubterranean((int x, int y) biomeOffset, int biomeIndex, int externalBiomeIndex)
        {
            Biome biome = subterraneanBiomeList[biomeIndex];
            int maxX = biomeOffset.x + biome.biomeDimensions.width;
            int maxY = biomeOffset.y + biome.biomeDimensions.height;
            if (maxX > worldArray.GetLength(0))
            {
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

                    //Use old/current cave systems: Don't re-do the caves

                    //if (x - biomeOffset.x < blockBlendRange || (biome.biomeDimensions.width - (x - biomeOffset.x) < blockBlendRange) || y - biomeOffset.y < blockBlendRange || (biome.biomeDimensions.height - (y - biomeOffset.y) < blockBlendRange)) { threshold = biomeList[externalBiomeIndex].changeThresholdByDepth((biomeOffset.x - 1, y)) + (x - biomeOffset.x) * (threshold - biomeList[externalBiomeIndex].changeThresholdByDepth((biomeOffset.x - 1, y))) / blockBlendRange; }
                    /*if (perlinNoiseArray[x, y] > threshold)
                    { //If it's above the block threshold, set the block to be air, 
                        worldArray[x, y] = 0;

                    }*/
                    if (brownianMotionArray[x, y] != null && worldArray[x, y] != 0) //If the brownian motion defined it, and it's solid from the midpoint generation
                    {
                        worldArray[x, y] = brownianMotionArray[x, y].block.ID;
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


        public (int x, int y) findLocalMin(int x, int y)
        {
            const int maxSearchAttempts = 300;
            const int maxSidewaysSearchAttempts = 30;
            int minX = x;
            int minY = y;
            Random r = new Random();
            int resetX = 0;
            int resetY = 0;
            int oppositeDirection = 0;
            if (worldArray[minX, minY] == 0)
            {
                //Search downwards first
                bool foundMin = false;
                int searchAttempts = 0;
                int sidewaysSearchAttempts = 0;
                int sidewaysSearchDirection = 0;

                bool hitAWallLeft = false;
                bool hitAWallRight = false;
                while (!foundMin && searchAttempts < maxSearchAttempts && sidewaysSearchAttempts < maxSidewaysSearchAttempts)
                {
                    if (minX >= 0 && minX < worldArray.GetLength(0) && minY >= 0 && minY < worldArray.GetLength(1) - 1)
                    {
                        if (worldArray[minX, minY + 1] == 0)
                        {
                            minY = minY + 1;
                            sidewaysSearchDirection = 0;
                            searchAttempts += 1;

                            resetX = 0;
                            resetY = 0;
                            oppositeDirection = 0;
                        }
                        else
                        {
                            searchAttempts = 0;
                            //Not air below, so pick a direction to search in:

                            if (sidewaysSearchDirection == 0)
                            {
                                sidewaysSearchDirection = (2 * r.Next(2)) - 1; //Randomly pick -1 or 1
                                resetX = minX;
                                resetY = minY;
                                oppositeDirection = sidewaysSearchDirection * -1;

                            }
                            if (minX + sidewaysSearchDirection >= 0 && minX + sidewaysSearchDirection < worldArray.GetLength(0) && minY >= 0 && minY < worldArray.GetLength(1))
                            {
                                if (worldArray[minX + sidewaysSearchDirection, minY] == 0)
                                {
                                    minX += sidewaysSearchDirection;
                                }
                                else
                                {
                                    //Hit a wall!
                                    if (sidewaysSearchDirection == -1)
                                    {
                                        hitAWallLeft = true;
                                    }
                                    else
                                    {
                                        hitAWallRight = true;
                                    }


                                    if (hitAWallLeft && hitAWallRight)
                                    {
                                        foundMin = true;
                                    }
                                    else
                                    {
                                        sidewaysSearchDirection = oppositeDirection;
                                        minX = resetX;
                                        minY = resetY;
                                    }
                                }
                            }
                            else
                            {
                                return (0, 0);
                            }

                        }
                    }
                    else
                    {
                        return (0, 0);
                    }
                }
            }

            return (minX, minY);

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
        int spawnableOffscreenDistance = 30;


        //++++++++++++++
        //Midpoint Displacement variables:
        public List<(double, double)> initialPoints = new List<(double, double)>();
        public double initialIterationOffset;
        public double decayPower;
        public int iterations;
        public int positiveWeight;
        //++++++++++++++

        public int maxBiomeEntityCount;
        public int currentBiomeEntityCount;

        public int maxSpawnAttempts = 30;

        //Should probably convert all these tupples to "____SpawnVariables" classes
        public List<BiomeSpawnableEntityVariables> spawnableEntities;
        public List<BiomeSpawnableStructureVariables> spawnableStructures = new List<BiomeSpawnableStructureVariables>();
        public List<BiomeSpawnableFluidVariables> spawnableFluids = new List<BiomeSpawnableFluidVariables>();
        public List<BiomeSpawnableDecorationVariables> spawnableDecorations = new List<BiomeSpawnableDecorationVariables>();

        public WorldGenerator worldGenerator;

        public int backgroundBlockID;

        public (int x, int y) biomeOffset;
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
        public void generateFluids()
        {
            //Go through each item in the spawnable fluids list and attempt to generate a fluid lake from it
            //If the maxLakeSize is 1, then just spawn it anywhere on an edge of a cave (eg. where an air and a solid block meets)
            //If the maxLakeSize is greater than that, find a random air pocket (cave) and then search downwards until you reach a bottom
            //  -> searching sideways finds a book end on both sides. Then fill that volume. Go up, and re-check the sides to see if you find book ends. If the volume that you find is greater than the maxLakeSide, don't fill it, or if there isn't a book end on both sides within the maxLakeVolume
            Random r = new Random();

            for (int i = 0; i < spawnableFluids.Count; i++)
            {
                int numberOfLakes = (int)(biomeDimensions.width * biomeDimensions.height * (spawnableFluids[i].density / 100.0));
                for (int n = 0; n < numberOfLakes; n++)
                {
                    //Add some more logic for fluids of only 1 size: creating water falls

                    if (spawnableFluids[i].maxLakeSize > 1)
                    {
                        //Attempt a set number of times to find an air pocket within the biome:
                        bool foundASpot = false;
                        int attemptCount = 0;

                        int x = 0;
                        int y = 0;
                        while (!foundASpot && attemptCount < maxSpawnAttempts)
                        {
                            attemptCount += 1;
                            //Randomly generate a spot:
                            x = r.Next(0, biomeDimensions.width);
                            y = 0;
                            //If it's a surface biome:
                            if (biomeOffset.y == 0)
                            {
                                y = r.Next(worldGenerator.surfaceHeight[x] + spawnableFluids[i].yMin, worldGenerator.surfaceHeight[x] + spawnableFluids[i].yMax);
                            }
                            else
                            {
                                //Else if it's not on the surface, base it on the biome dimensions:
                                if (spawnableFluids[i].yMax < biomeDimensions.height)
                                {
                                    y = r.Next(biomeOffset.y + spawnableFluids[i].yMin, biomeOffset.y + spawnableFluids[i].yMax);
                                }
                                else
                                {
                                    y = r.Next(biomeOffset.y + spawnableFluids[i].yMin, biomeOffset.y + biomeDimensions.height);

                                }
                            }
                            if (x >= 0 && x < worldGenerator.worldArray.GetLength(0) && y >= 0 && y < worldGenerator.worldArray.GetLength(1))
                            {
                                if (worldGenerator.worldArray[x, y] == 0)
                                {
                                    foundASpot = true;
                                }
                            }
                        }
                        //The spot is air, and thus yay!
                        //Use the world generator to find a local min
                        (int minX, int minY) = worldGenerator.findLocalMin(x, y);
                        int lowestX = minX;
                        //If the returned point isn't the error value
                        if (!(minX == 0 && minY == 0))
                        {
                            //Fill upwards:
                            //Search sideways until you hit a wall, then go the other way. Count the number of blocks here, and reduce the lakeSize by that value

                            int searchDirection = 0;
                            int oppositeDirection = 0;
                            int resetX = minX;
                            int resetY = minY;

                            int layerSize = 0;

                            bool hasFaulted = false;

                            int remainingLakeVolume = spawnableFluids[i].maxLakeSize;

                            while (remainingLakeVolume >= 0 && remainingLakeVolume > layerSize && !hasFaulted)
                            {
                                bool foundFullLayer = false;
                                bool hitAWallLeft = false;
                                bool hitAWallRight = false;
                                List<(int x, int y)> blocksToSet = new List<(int, int)>();
                                while (!foundFullLayer && remainingLakeVolume > layerSize && !hasFaulted)
                                {
                                    layerSize += 1;

                                    if (searchDirection == 0)
                                    {
                                        searchDirection = (2 * r.Next(2)) - 1; //Randomly pick -1 or 1
                                        resetX = minX;
                                        resetY = minY;
                                        oppositeDirection = -1 * searchDirection;
                                    }
                                    //Have to include the "search direction" aspect because the reset values are obviously set to the fluid
                                    if (minX + searchDirection >= 0 && minX + searchDirection < worldGenerator.worldArray.GetLength(0) && minY >= 0 && minY < worldGenerator.worldArray.GetLength(1))
                                    {
                                        if (minY < worldGenerator.worldArray.GetLength(1) - 1 ? worldGenerator.worldArray[minX, minY + 1] == 0 : false)
                                        {
                                            hasFaulted = true;
                                        }
                                        else
                                        {
                                            if (worldGenerator.worldArray[minX, minY] == 0)
                                            {
                                                blocksToSet.Add((minX, minY));
                                                minX += searchDirection;
                                            }
                                            else
                                            {
                                                //Hit a wall!
                                                if (searchDirection == -1)
                                                {
                                                    hitAWallLeft = true;
                                                }
                                                else
                                                {
                                                    hitAWallRight = true;
                                                }


                                                if (hitAWallLeft && hitAWallRight)
                                                {
                                                    foundFullLayer = true;
                                                    searchDirection = 0;
                                                }
                                                else
                                                {
                                                    searchDirection = oppositeDirection;
                                                    minX = resetX + searchDirection;
                                                    minY = resetY;
                                                }
                                            }
                                        }


                                    }
                                }
                                if (foundFullLayer)
                                {
                                    for (int b = 0; b < blocksToSet.Count; b++)
                                    {
                                        worldGenerator.worldArray[blocksToSet[b].x, blocksToSet[b].y] = spawnableFluids[i].fluid.ID;
                                    }
                                }

                                minX = lowestX;
                                minY -= 1;
                                blocksToSet.Clear();
                                searchDirection = 0;
                                remainingLakeVolume -= layerSize;


                                layerSize = 0;
                            }

                        }
                    }
                    else {
                        bool foundASpot = false;
                        int attemptCount = 0;

                        int x = 0;
                        int y = 0;
                        while (!foundASpot && attemptCount < maxSpawnAttempts)
                        {
                            attemptCount += 1;
                            //Randomly generate a spot:
                            x = r.Next(0, biomeDimensions.width);
                            y = 0;
                            //If it's a surface biome:
                            if (biomeOffset.y == 0)
                            {
                                y = r.Next(worldGenerator.surfaceHeight[x] + spawnableFluids[i].yMin, worldGenerator.surfaceHeight[x] + spawnableFluids[i].yMax);
                            }
                            else
                            {
                                //Else if it's not on the surface, base it on the biome dimensions:
                                if (spawnableFluids[i].yMax < biomeDimensions.height)
                                {
                                    y = r.Next(biomeOffset.y + spawnableFluids[i].yMin, biomeOffset.y + spawnableFluids[i].yMax);
                                }
                                else
                                {
                                    y = r.Next(biomeOffset.y + spawnableFluids[i].yMin, biomeOffset.y + biomeDimensions.height);

                                }
                            }
                            if (x >= 0 && x < worldGenerator.worldArray.GetLength(0) && y >= 0 && y < worldGenerator.worldArray.GetLength(1))
                            {
                                if (worldGenerator.worldArray[x, y] == 0)
                                {
                                    foundASpot = true;
                                }
                            }
                        }
                        //Found a spot (presumably):
                        if (foundASpot) {

                            //Search sideways until hitting a solid block: then, convert that block to the fluid
                            int searchDirection = (2 * r.Next(2)) - 1; //Randomly pick -1 or 1

                            bool foundAWall = false;
                            bool faulted = false;

                            while (!foundAWall && !faulted) {
                                if (x + searchDirection >= 0 && x + searchDirection < worldGenerator.worldArray.GetLength(0) && y >= 0 && y < worldGenerator.worldArray.GetLength(1))
                                {
                                    x += searchDirection;
                                    if (worldGenerator.worldArray[x, y] != 0) {
                                        //Found a wall:
                                        worldGenerator.worldArray[x, y] = spawnableFluids[i].fluid.ID;
                                        foundAWall = true;
                                    }
                                }
                                else {
                                    faulted = true;
                                }
                            }
                        }

                    }
                
                
                }
            }
        }
        public void generateBackground() {
            for (int x = biomeOffset.x; x < biomeOffset.x + biomeDimensions.width; x++) {
                for (int y = biomeOffset.y; y < biomeOffset.y + biomeDimensions.height; y++) {
                    if (x >= 0 && x < worldGenerator.surfaceHeight.Length && y >= 0 && y < worldGenerator.backgroundArray.GetLength(1))
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
                structureCount = r.Next(structureCount / 2, structureCount);
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
            //Only seed the algorithm, implement it into the worlds array and add the ores to a list
            seededBrownianMotion(ores, maxAttempts);
            
            for (int x = 0; x < brownianMotionArray.GetLength(0); x++) {
                for (int y = 0; y < brownianMotionArray.GetLength(1); y++) {
                    int globalX = x + biomeOffset.x;
                    int globalY = y + biomeOffset.y;

                    if (globalX > 0 && globalX < worldGenerator.brownianMotionArray.GetLength(0) && globalY > 0 && globalY < worldGenerator.brownianMotionArray.GetLength(1)) {
                        worldGenerator.brownianMotionArray[globalX, globalY] = brownianMotionArray[x, y];
                    }
                }
            }

        }

        public void generateDecorations() {
            //Two ways that it can be random directions:
            //Each movement is in a random direction
            //  -> Pros: Can go in many directions and is more interesting. Cons: Can make them be closer than the 'minimum' distance

            //When generating, each decoration moves in a set direction - Current technique
            if (spawnableDecorations.Count > 0)
            {
                foreach (BiomeSpawnableDecorationVariables decorationVar in spawnableDecorations)
                {
                    Random r = new Random();
                    //Calculate the number of decorations:
                    int numberOfDecorations = 0;
                    if (decorationVar.spawnOnSurface)
                    {
                        double averageCount = (decorationVar.density / 100.0) * biomeDimensions.width;
                        numberOfDecorations = r.Next((int)(averageCount * 0.9), (int)(averageCount * 1.1));
                    }
                    else
                    {
                        double averageCount = (decorationVar.density / 100.0) * biomeDimensions.width * biomeDimensions.height;
                        numberOfDecorations = r.Next((int)(averageCount * 0.9), (int)(averageCount * 1.1));
                    }

                    int sign = r.Next(2) * 2 - 1;


                    if (decorationVar.spawnOnSurface)
                    {
                        int x = r.Next(0, biomeDimensions.width) + biomeOffset.x;
                        int y = 0;
                        if (x >= 0 && x < worldGenerator.worldArray.GetLength(0))
                        {
                            y = worldGenerator.surfaceHeight[x];
                        }
                        int lastX = x;
                        int lastY = y;
                        for (int i = 0; i < numberOfDecorations; i++)
                        {

                            int maxAttemptCount = 15;
                            int attemptCount = 0;
                            bool generatedDecoration = false;
                            while (!generatedDecoration && attemptCount < maxAttemptCount)
                            {
                                attemptCount += 1;

                                //Currently only moves right, so make it either positive or negative randomly
                                x =  sign * r.Next(decorationVar.minToNextDecoration, decorationVar.maxToNextDecoration) + lastX;
                                if (x >= 0 && x < worldGenerator.worldArray.GetLength(0))
                                {
                                    y = worldGenerator.surfaceHeight[x];
                                }

                                if (decorationVar.yMin < y && y < decorationVar.yMax)
                                {
                                    if (x > 0 && x < worldGenerator.worldArray.GetLength(0) -1&& y > 0 && y < worldGenerator.worldArray.GetLength(1) - 1)
                                    {
                                        generatedDecoration = decorationVar.decoration.generate(x, y, worldGenerator);
                                    }
                                }
                            }

                            lastX = x;
                            lastY = y;
                        }
                    }
                    else
                    {
                        int x = r.Next(0, biomeDimensions.width) + biomeOffset.x;
                        int y = r.Next(0, biomeDimensions.height) + biomeOffset.y;

                        int lastX = x;
                        int lastY = y;
                        for (int i = 0; i < numberOfDecorations; i++)
                        {
                            int maxAttemptCount = 15;
                            int attemptCount = 0;
                            bool generatedDecoration = false;
                            while (!generatedDecoration && attemptCount < maxAttemptCount)
                            {
                                attemptCount += 1;
                                x = (r.Next() * 2 - 1) * r.Next(decorationVar.minToNextDecoration, decorationVar.maxToNextDecoration) + lastX;
                                y = (r.Next() * 2 - 1) * r.Next(decorationVar.minToNextDecoration, decorationVar.maxToNextDecoration) + lastY;

                                if (decorationVar.yMin < y && y < decorationVar.yMax)
                                {
                                    if (x >= 0 && x < worldGenerator.worldArray.GetLength(0) && y >= 0 && y < worldGenerator.worldArray.GetLength(1))
                                    {
                                        generatedDecoration = decorationVar.decoration.generate(x, y, worldGenerator);
                                    }
                                }
                            }

                            lastX = x;
                            lastY = y;
                        }
                    }
                }
            }
        }
        private void seededBrownianMotion(BlockGenerationVariables[] oresArray, int attemptCount)
        {
            SeededBrownianMotion sbm = new SeededBrownianMotion();
            brownianMotionArray = sbm.seededBrownianMotion(brownianMotionArray, oresArray);
            //brownianMotionArray = sbm.brownianAlgorithm(brownianMotionArray, attemptCount);
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

                    if (worldGenerator.worldArray[gridX, y] != 0)
                    {
                        if (worldGenerator.surfaceHeight[gridX] > y)
                        {
                            worldGenerator.surfaceHeight[gridX] = y;
                        }

                    }
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

        public virtual void tickBiome(WorldContext w) {
            Random r = new Random();

            //Go through the list of entities and check if it should be spawned
            if (spawnableEntities != null)
            {
                for (int i = 0; i < spawnableEntities.Count; i++)
                {
                    if (spawnableEntities[i].yMax > (int)Math.Floor((- w.screenSpaceOffset.y) / (double)w.pixelsPerBlock) - w.surfaceHeight[(int)Math.Floor(-w.screenSpaceOffset.x / (double)w.pixelsPerBlock)] && spawnableEntities[i].yMin < (int)Math.Floor(-w.screenSpaceOffset.y / (double)w.pixelsPerBlock) - w.surfaceHeight[(int)Math.Floor(-w.screenSpaceOffset.x / (double)w.pixelsPerBlock)])
                    {
                        if (currentBiomeEntityCount < maxBiomeEntityCount && spawnableEntities[i].currentSpecificEntityCount < spawnableEntities[i].maxSpecificEntityCount)
                        {
                            //Check to spawn the entity
                            double percentage = r.NextDouble() * 100;
                            if (percentage <= spawnableEntities[i].spawnProbability)
                            {
                                //Adjust the current specific entity count

                                currentBiomeEntityCount += 1;
                                spawnableEntities[i].currentSpecificEntityCount += 1;
                                

                                //Adjust entity location:


                                int xLoc = r.Next((int)Math.Floor(-w.screenSpaceOffset.x / (double)w.pixelsPerBlock) - spawnableOffscreenDistance, (int)Math.Floor((w.applicationWidth - w.screenSpaceOffset.x) / (double)w.pixelsPerBlock) + spawnableOffscreenDistance);
                                int yLoc = r.Next((int)Math.Floor(-w.screenSpaceOffset.y / (double)w.pixelsPerBlock) - spawnableOffscreenDistance, (int)Math.Floor((w.applicationHeight - w.screenSpaceOffset.y) / (double)w.pixelsPerBlock) + spawnableOffscreenDistance);

                                //Find a spawnable location:
                                bool foundALocation = false;
                                int currentSpawnAttemptCount = 0;
                                while (!foundALocation && currentSpawnAttemptCount < maxSpawnAttempts)
                                {
                                    currentSpawnAttemptCount += 1;
                                    xLoc = r.Next((int)Math.Floor(-w.screenSpaceOffset.x / (double)w.pixelsPerBlock) - spawnableOffscreenDistance, (int)Math.Floor((w.applicationWidth - w.screenSpaceOffset.x) / (double)w.pixelsPerBlock) + spawnableOffscreenDistance);
                                    yLoc = r.Next((int)Math.Floor(-w.screenSpaceOffset.y / (double)w.pixelsPerBlock) - spawnableOffscreenDistance, (int)Math.Floor((w.applicationHeight - w.screenSpaceOffset.y) / (double)w.pixelsPerBlock) + spawnableOffscreenDistance);
                                    if (spawnableEntities[i].spawnOnSurface)
                                    {
                                        yLoc = w.surfaceHeight[xLoc] - (int)Math.Ceiling(spawnableEntities[i].entity.height - 1);
                                    }

                                    //Check if that location is somewhere that the entity can spawn:
                                    //Off-screen
                                    if ((xLoc < Math.Floor(-w.screenSpaceOffset.x / (double)w.pixelsPerBlock) || xLoc > Math.Floor((w.applicationWidth - w.screenSpaceOffset.x) / (double)w.pixelsPerBlock)) && (yLoc < Math.Floor(-w.screenSpaceOffset.y / (double)w.pixelsPerBlock) || yLoc > Math.Floor((w.applicationHeight - w.screenSpaceOffset.y) / (double)w.pixelsPerBlock)))
                                    {
                                        //Sufficient air for the entity to exist in
                                        bool isASolidBlockInSpawnLocation = false;
                                        for (int x = -1; x < (int)Math.Ceiling(spawnableEntities[i].entity.width); x++)
                                        {
                                            for (int y = -1; y < (int)Math.Ceiling(spawnableEntities[i].entity.height); y++)
                                            {
                                                if (xLoc + x >= 0 && xLoc + x < w.worldArray.GetLength(0) && yLoc + y >= 0 && yLoc + y < w.worldArray.GetLength(1))
                                                    if (w.worldArray[xLoc + x, yLoc + y].ID != (int)blockIDs.air)
                                                    {
                                                        isASolidBlockInSpawnLocation = true;
                                                    }
                                            }
                                        }
                                        bool isOnSolidGround = false;

                                        int integerHeight = (int)Math.Ceiling(spawnableEntities[i].entity.height);
                                        if (xLoc >= 0 && xLoc < w.worldArray.GetLength(0) && yLoc + integerHeight >= 0 && yLoc + integerHeight < w.worldArray.GetLength(1))
                                        {
                                            isOnSolidGround = !w.worldArray[xLoc, yLoc + integerHeight].isBlockTransparent;
                                        }

                                        if (!isASolidBlockInSpawnLocation && isOnSolidGround)
                                        {
                                            foundALocation = true;
                                            SpawnableEntity entity = spawnableEntities[i].entity.copyEntity() as SpawnableEntity;
                                            entity.setBiome(this, i);
                                            entity.x = xLoc * w.pixelsPerBlock;
                                            entity.y = yLoc * w.pixelsPerBlock;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        public virtual Biome generateBiomeCopy((double, double) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int width, int height) biomeDimensions) {
            return new Biome(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions);
        }
    }
    public class SubterraneanBiome : Biome {
        //A biome that exists only under the surface of the world


        public int x;
        public int y;

        public int biomesPerWorld;

        public int externalBiomeIndex;

        public List<Type> biomesThisCanSpawnIn = new List<Type>();

        public int minY;
        public int maxY;

        public SubterraneanBiome((double x, double y) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int x, int y) biomeDimensions) : base(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions)
        {
            x = biomeOffset.x;
            y = biomeOffset.y;
        }

        public SubterraneanBiome() { }

        public void setBiomeLocation(int x, int y) {
            this.x = x;
            this.y = y;
            biomeOffset.x = x;
            biomeOffset.y = y;
        }

        public override Biome generateBiomeCopy((double, double) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int width, int height) biomeDimensions){
            return new SubterraneanBiome(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions);
        }
    }
    public class MeadowBiome : Biome {

        public MeadowBiome((double x, double y) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int x, int y) biomeDimensions) : base(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions) {
            //Generate the randomised variables
            initialIterationOffset = 90;
            decayPower = 1.1;
            iterations = 10;
            positiveWeight = 30;

            backgroundBlockID = 1;

            maxBiomeEntityCount = 30;

            ores = new BlockGenerationVariables[]{
            new BlockGenerationVariables(seedDensity : 1, block : new Block(ID : 2), maxSingleSpread : 8, oreVeinSpread : 360), //Dirt
            new BlockGenerationVariables(0.1, new OreBlock((int)blockIDs.ironOre), 1, 4, (0.3, 0.6, 0.1, 0.0, 0.0, 0.0, 0.0, 0.0)),
            new BlockGenerationVariables(0.3, new Block(1), 6, 24)
            };


            blockThresholdVariables = new List<BlockThresholdValues>(){
            new BlockThresholdValues(blockThreshold : 0.9, maximumY : 0, decreasePerY : 0.005, maximumThreshold : 0.9, minimumThreshold : 0.48, absoluteYHeightWeight : 0, relativeYHeightWeight : 1),
            new BlockThresholdValues(0.9, 130, 0.005, 0.9, 0.48, 0.3, 1),

            new BlockThresholdValues(0.9, 150, 0.01, 0.9, 0.48, 1, 0),

            new BlockThresholdValues(0.9, 200, 0.005, 0.9, 0.48, 0.2, 1),
            new BlockThresholdValues(0.9, 210, 0.005, 0.9, 0.48, 0, 1)
            };

            spawnableStructures = new List<BiomeSpawnableStructureVariables>()
            {
                new BiomeSpawnableStructureVariables(new Structure("House"), 0.05, biomeDimensions.y, 0)
            };

            spawnableEntities = new List<BiomeSpawnableEntityVariables>() {
                new BiomeSpawnableEntityVariables(new ControlledEntity(wg.worldContext, wg.worldContext.player), 50, 0, 20, 500, 10, false)
            };

            spawnableFluids = new List<BiomeSpawnableFluidVariables>()
            {
                new BiomeSpawnableFluidVariables(new FluidBlock((int)blockIDs.water), 0.01, 600, 1, 1),
                new BiomeSpawnableFluidVariables(new FluidBlock((int)blockIDs.water), 0.01, 600, 1, 20),
                new BiomeSpawnableFluidVariables(new FluidBlock((int)blockIDs.water), 0.05, 0, -30, 60)
            };

            spawnableDecorations = new List<BiomeSpawnableDecorationVariables>() {
                new BiomeSpawnableDecorationVariables(new TreeGeneration(), 3, 4, 15, 400, 0, true),
                new BiomeSpawnableDecorationVariables(new TreeGeneration(), 3, 4, 15, 400, 0, true),
                new BiomeSpawnableDecorationVariables(new BushGeneration(), 10, 3, 25, 400, 0, true),
                new BiomeSpawnableDecorationVariables(new BigBushGeneration(), 10, 3, 25, 400, 0, true)

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

            spawnableStructures = new List<BiomeSpawnableStructureVariables>() {
                new BiomeSpawnableStructureVariables(new Structure("Shrine"), 0.005, biomeDimensions.y, 200)
            };

            spawnableFluids = new List<BiomeSpawnableFluidVariables>() {
                new BiomeSpawnableFluidVariables(new FluidBlock((int)blockIDs.water), 0.01, 400, 10, 1)
            };


            this.biomeDimensions.width = new Random().Next(200, 400);


            initialPoints.Add((rightMostTerrainPoint.x + (this.biomeDimensions.width * wg.worldContext.pixelsPerBlock) / 2, rightMostTerrainPoint.y - 500)); //A central peak
            initialPoints.Add((rightMostTerrainPoint.x + this.biomeDimensions.width * wg.worldContext.pixelsPerBlock, rightMostTerrainPoint.y));
        }

        public MountainBiome() { }

        public override Biome generateBiomeCopy((double, double) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int width, int height) biomeDimensions)
        {
            return new MountainBiome(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions);
        }

    }
    public class CaveBiome : SubterraneanBiome {
        
        public CaveBiome((double x, double y) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int x, int y) biomeDimensions) : base(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions) {
            ores = new BlockGenerationVariables[] {
                new BlockGenerationVariables(1, new Block((int)blockIDs.stone), 8, 80),
                new BlockGenerationVariables(0.1, new Block((int)blockIDs.dirt), 3, 10)
            };

            blockThresholdVariables = new List<BlockThresholdValues> {
                new BlockThresholdValues(blockThreshold : 0.9, maximumY : 0, decreasePerY : 0.005, maximumThreshold : 0.9, minimumThreshold : 0.45, absoluteYHeightWeight : 0.3, relativeYHeightWeight : 0.7 ),
                new BlockThresholdValues(blockThreshold : 0.48, maximumY : 400, decreasePerY : 0.001, maximumThreshold : 0.48, minimumThreshold : 0.4, absoluteYHeightWeight : 0, relativeYHeightWeight : 1)
            };

            biomesThisCanSpawnIn.Add(typeof(MeadowBiome));

            biomesPerWorld = 5;
            minY = 50;
            maxY = 250;

            backgroundBlockID = 1;

            Random r = new Random();
            this.biomeDimensions.width = r.Next(50, 100);
            this.biomeDimensions.height = r.Next(50, 100);

        }

        public CaveBiome() {
            biomesPerWorld = 5;
            minY = 50;
            maxY = 250;
            biomesThisCanSpawnIn.Add(typeof(MeadowBiome));

        }

        public override Biome generateBiomeCopy((double, double) rightMostTerrainPoint, WorldGenerator wg, (int x, int y) biomeOffset, (int width, int height) biomeDimensions)
        {
            return new CaveBiome(rightMostTerrainPoint, wg, biomeOffset, biomeDimensions);
        }
    }
    #endregion
    #region Biome Generation Variables
    public class BiomeSpawnableEntityVariables {
        public SpawnableEntity entity;
        public int maxSpecificEntityCount;
        public int currentSpecificEntityCount;
        public double spawnProbability;
        public int yMax;
        public int yMin;
        public bool spawnOnSurface;

        public BiomeSpawnableEntityVariables(SpawnableEntity entity, int maxSpecificEntityCount, int currentSpecificEntityCount, double spawnProbability, int yMax, int yMin, bool spawnOnSurface) {
            this.entity = entity;
            this.maxSpecificEntityCount = maxSpecificEntityCount;
            this.currentSpecificEntityCount = currentSpecificEntityCount;
            this.spawnProbability = spawnProbability;
            this.yMax = yMax;
            this.yMin = yMin;
            this.spawnOnSurface = spawnOnSurface;
        }
    }

    public class BiomeSpawnableStructureVariables {
        public Structure structure;
        public double density;
        public int yMax;
        public int yMin;

        public BiomeSpawnableStructureVariables(Structure structure, double density, int yMax, int yMin) {
            this.structure = structure;
            this.density = density;
            this.yMax = yMax;
            this.yMin = yMin;
        }
    }

    public class BiomeSpawnableFluidVariables {
        public FluidBlock fluid;
        public double density;
        public int yMax;
        public int yMin;
        public int maxLakeSize;

        public BiomeSpawnableFluidVariables(FluidBlock fluid, double density, int yMax, int yMin, int maxLakeSize) {
            this.fluid = fluid;
            this.density = density;
            this.yMax = yMax;
            this.yMin = yMin;
            this.maxLakeSize = maxLakeSize;
        }
    }

    public class BiomeSpawnableDecorationVariables {
        public Decoration decoration;
        public double density;
        public int minToNextDecoration;
        public int maxToNextDecoration;
        public int yMax;
        public int yMin;
        public bool spawnOnSurface;

        public BiomeSpawnableDecorationVariables(Decoration decoration, double density, int minToNextDecoration, int maxToNextDecoration, int yMax, int yMin, bool spawnOnSurface) {
            this.decoration = decoration;
            this.density = density;
            this.minToNextDecoration = minToNextDecoration;
            this.maxToNextDecoration = maxToNextDecoration;
            this.yMax = yMax;
            this.yMin = yMin;
            this.spawnOnSurface = spawnOnSurface;
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

                sr.Close();

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

    #region Decoration Classes

    public class Decoration {
        public virtual bool generate(int x, int y, WorldContext wc) {
            return true;
        }

        public virtual bool generate(int x, int y, WorldGenerator wg) {
            return true;
        }
    }
    public class TreeGeneration : Decoration
    {

        List<BranchVariables> branches = new List<BranchVariables>();


        List<BranchEndVariables> branchEnds = new List<BranchEndVariables>();
        public int branchLength = 8;

        public double branchAngle = Math.PI / 5;
        public double branchProbability = 0.25;

        int currentSign = 1;

        public double angleIncreasePerBlockAdded = 0;

        public int blocksTilBranch = 3;
        int currentBlocksFromBranch = 3;

        public bool splitBranch = false;

        public double branchLengthDecay = 0.9;

        double currentX;
        double currentY;

        public int baseX;
        public int baseY;

        public double probabilityToGenerateLeavesOverWood = 0.1;


        WorldContext wc;
        WorldGenerator wg;

        public blockIDs treeTrunkBlock = blockIDs.treeTrunk;
        public blockIDs leafBlock = blockIDs.leaves;
        public override bool generate(int x, int y, WorldContext wc)
        {
            baseX = x;
            baseY = y;
            currentX = x;
            currentY = y;
            this.wc = wc;

            branchEnds.Clear();

            branches.Add(new BranchVariables(3 * Math.PI / 2, x, y, 0, branchLength, 0));

            while (generateBranch()) ;

            //Generate leaves from branch ends:
            generateLeaves();

            return true;
        }

        public override bool generate(int x, int y, WorldGenerator wg)
        {
            if (wg.worldArray[x, y - 1] == (int)blockIDs.air && (wg.worldArray[x, y + 1] == (int)blockIDs.dirt || wg.worldArray[x, y + 1] == (int)blockIDs.grass))
            {
                baseX = x;
                baseY = y;
                currentX = x;
                currentY = y;
                this.wg = wg;

                branchEnds.Clear();

                branches.Add(new BranchVariables(3 * Math.PI / 2, x, y, 0, branchLength, 0));

                while (generateBranchIntegerOutput()) ;

                //Generate leaves from branch ends:
                generateLeavesIntegerOutput();

                return true;
            }
            else {
                return false;
            }
        }

        public bool generateBranch()
        {
            bool addedABlock = false;
            int i = branches.Count - 1;
            if (i >= 0)
            {
                //For each branch, set the current x and current y to the branches x and y
                //while (branches[i].currentLength < branchLength && currentX >= 0 && currentY >= 0 && currentX < wc.worldArray.GetLength(0) && currentY < wc.worldArray.GetLength(1))
                //{
                currentX = (currentX + Math.Cos(branches[i].angle));
                currentY = (currentY + Math.Sin(branches[i].angle));

                if (currentX >= 0 && currentY >= 0 && currentX < wc.worldArray.GetLength(0) && currentY < wc.worldArray.GetLength(1))
                {

                    branches[i].currentLength += 1;

                    if (branches[i].angle % MathHelper.TwoPi > 3 * Math.PI / 2 || branches[i].angle % MathHelper.TwoPi < MathHelper.PiOver2)
                    {
                        branches[i].angle += angleIncreasePerBlockAdded;
                    }
                    else if (branches[i].angle % MathHelper.TwoPi < 3 * Math.PI / 2 && branches[i].angle % MathHelper.TwoPi > MathHelper.PiOver2)
                    {
                        branches[i].angle -= angleIncreasePerBlockAdded;
                    }


                    wc.addBlock((int)currentX, (int)currentY, (int)treeTrunkBlock);
                    currentBlocksFromBranch += 1;
                    addedABlock = true;
                }
                else
                {
                    branchEnds.Add(new BranchEndVariables((int)currentX, (int)currentY, branches[i].branchLayerDepth));


                    currentX = branches[i].x;
                    currentY = branches[i].y;
                    currentBlocksFromBranch = 0;
                    branches.RemoveAt(i);
                    i = branches.Count - 1;
                    if (i >= 0) { addedABlock = true; }


                    return addedABlock;
                }

                Random r = new Random();

                if (i >= 0)
                {
                    if (branches[i].currentLength >= branches[i].maxBranchLength)
                    {
                        branchEnds.Add(new BranchEndVariables((int)currentX, (int)currentY, branches[i].branchLayerDepth));


                        currentX = branches[i].x;
                        currentY = branches[i].y;
                        currentBlocksFromBranch = 0;

                        //Add to the list of branch ends


                        branches.RemoveAt(i);
                        i = branches.Count - 1;
                        if (i >= 0) { addedABlock = true; }

                        return addedABlock;
                    }
                    else if (r.NextDouble() < branchProbability && blocksTilBranch <= currentBlocksFromBranch)
                    {
                        System.Diagnostics.Debug.WriteLine("Tried to branch");
                        branches[i].x = currentX;
                        branches[i].y = currentY;
                        //int angleDifferentSign = (r.Next(0, 2) * 2) - 1;
                        currentSign *= -1;
                        //A 10% angle variance
                        branches.Add(new BranchVariables(branches[i].angle + currentSign * branchAngle * (r.NextDouble() * 0.2 + 0.9), currentX, currentY, branches[i].currentLength, branches[i].maxBranchLength * branchLengthDecay, branches[i].branchLayerDepth + 1));
                        currentBlocksFromBranch = 0;
                        branches[i].angle -= Convert.ToInt32(splitBranch) * (currentSign * branchAngle);
                        //break;
                        addedABlock = true;
                    }
                }

                i = branches.Count - 1;
            }

            return addedABlock;


        }

        public void generateLeaves()
        {
            //Seed a brownian array:
            BlockGenerationVariables[,] leafArray = new BlockGenerationVariables[branchLength * 2, branchLength * 2];

            SeededBrownianMotion sbm = new SeededBrownianMotion();

            //Seed the array

            //The baseX and baseY are at the bottom centre of the array:
            for (int i = 0; i < branchEnds.Count; i++)
            {
                int relativeX = (branchEnds[i].x - baseX) + leafArray.GetLength(0) / 2;
                int relativeY = (branchEnds[i].y - baseY) + leafArray.GetLength(1);
                if (relativeX >= 0 && relativeY >= 0 && relativeX < leafArray.GetLength(0) && relativeY < leafArray.GetLength(1))
                {
                    leafArray[relativeX, relativeY] = new BlockGenerationVariables(0, new Block((int)leafBlock), 8, 10 / (branchEnds[i].branchLayerDepth + 1), (0.3, 0.05, 0.3, 0.05, 0.05, 0.05, 0.3, 0.05));
                }
            }


            //Spread the seeds
            leafArray = sbm.brownianAlgorithm(leafArray, 15, fillOutput : false);

            //Convert it to blocks in the world
            Random r = new Random();
            for (int x = 0; x < leafArray.GetLength(0); x++)
            {
                for (int y = 0; y < leafArray.GetLength(1); y++)
                {
                    if (leafArray[x, y] != null)
                    {
                        int localToGlobalX = (x - leafArray.GetLength(0) / 2) + baseX;
                        int localToGlobalY = (y - leafArray.GetLength(1)) + baseY;
                        if (localToGlobalX >= 0 && localToGlobalY >= 0 && localToGlobalX < wc.worldArray.GetLength(0) && localToGlobalY < wc.worldArray.GetLength(1))
                        {
                            if (wc.worldArray[localToGlobalX, localToGlobalY].ID == (int)blockIDs.air || r.NextDouble() < probabilityToGenerateLeavesOverWood)
                            {
                                wc.addBlock(x, y, leafArray[x, y].block.ID);
                            }
                        }
                    }
                }
            }
        }

        public bool generateBranchIntegerOutput()
        {
            bool addedABlock = false;
            int i = branches.Count - 1;
            if (i >= 0)
            {
                //For each branch, set the current x and current y to the branches x and y
                //while (branches[i].currentLength < branchLength && currentX >= 0 && currentY >= 0 && currentX < wc.worldArray.GetLength(0) && currentY < wc.worldArray.GetLength(1))
                //{
                currentX = (currentX + Math.Cos(branches[i].angle));
                currentY = (currentY + Math.Sin(branches[i].angle));

                if (currentX >= 0 && currentY >= 0 && currentX < wg.worldArray.GetLength(0) && currentY < wg.worldArray.GetLength(1))
                {

                    branches[i].currentLength += 1;

                    if (branches[i].angle % MathHelper.TwoPi > 3 * Math.PI / 2 || branches[i].angle % MathHelper.TwoPi < MathHelper.PiOver2)
                    {
                        branches[i].angle += angleIncreasePerBlockAdded;
                    }
                    else if (branches[i].angle % MathHelper.TwoPi < 3 * Math.PI / 2 && branches[i].angle % MathHelper.TwoPi > MathHelper.PiOver2)
                    {
                        branches[i].angle -= angleIncreasePerBlockAdded;
                    }


                    wg.worldArray[(int)currentX, (int)currentY] =  (int)treeTrunkBlock;
                    currentBlocksFromBranch += 1;
                    addedABlock = true;
                }
                else
                {
                    branchEnds.Add(new BranchEndVariables((int)currentX, (int)currentY, branches[i].branchLayerDepth));


                    currentX = branches[i].x;
                    currentY = branches[i].y;
                    currentBlocksFromBranch = 0;
                    branches.RemoveAt(i);
                    i = branches.Count - 1;
                    if (i >= 0) { addedABlock = true; }


                    return addedABlock;
                }

                Random r = new Random();

                if (i >= 0)
                {
                    if (branches[i].currentLength >= branches[i].maxBranchLength)
                    {
                        branchEnds.Add(new BranchEndVariables((int)currentX, (int)currentY, branches[i].branchLayerDepth));


                        currentX = branches[i].x;
                        currentY = branches[i].y;
                        currentBlocksFromBranch = 0;

                        //Add to the list of branch ends


                        branches.RemoveAt(i);
                        i = branches.Count - 1;
                        if (i >= 0) { addedABlock = true; }

                        return addedABlock;
                    }
                    else if (r.NextDouble() < branchProbability && blocksTilBranch <= currentBlocksFromBranch)
                    {
                        System.Diagnostics.Debug.WriteLine("Tried to branch");
                        branches[i].x = currentX;
                        branches[i].y = currentY;
                        //int angleDifferentSign = (r.Next(0, 2) * 2) - 1;
                        currentSign *= -1;
                        //A 10% angle variance
                        branches.Add(new BranchVariables(branches[i].angle + currentSign * branchAngle * (r.NextDouble() * 0.2 + 0.9), currentX, currentY, branches[i].currentLength, branches[i].maxBranchLength * branchLengthDecay, branches[i].branchLayerDepth + 1));
                        currentBlocksFromBranch = 0;
                        branches[i].angle -= Convert.ToInt32(splitBranch) * (currentSign * branchAngle);
                        //break;
                        addedABlock = true;
                    }
                }

                i = branches.Count - 1;
            }

            return addedABlock;


        }

        public void generateLeavesIntegerOutput()
        {
            //Seed a brownian array:
            BlockGenerationVariables[,] leafArray = new BlockGenerationVariables[branchLength * 2, branchLength * 2];

            SeededBrownianMotion sbm = new SeededBrownianMotion();

            //Seed the array

            //The baseX and baseY are at the bottom centre of the array:
            for (int i = 0; i < branchEnds.Count; i++)
            {
                int relativeX = (branchEnds[i].x - baseX) + leafArray.GetLength(0) / 2;
                int relativeY = (branchEnds[i].y - baseY) + leafArray.GetLength(1);
                if (relativeX >= 0 && relativeY >= 0 && relativeX < leafArray.GetLength(0) && relativeY < leafArray.GetLength(1))
                {
                    leafArray[relativeX, relativeY] = new BlockGenerationVariables(0, new Block((int)leafBlock), 8, 10 / (branchEnds[i].branchLayerDepth + 1), (0.3, 0.05, 0.3, 0.05, 0.05, 0.05, 0.3, 0.05));
                }
            }


            //Spread the seeds
            leafArray = sbm.brownianAlgorithm(leafArray, 15, fillOutput: false);

            for (int x = 0; x < leafArray.GetLength(0); x++) {
                for (int y = 0; y < leafArray.GetLength(1); y++) {
                    if (leafArray[x, y] != null)
                    {
                        if (leafArray[x, y].block.ID == (int)blockIDs.leaves)
                        {
                            for (int checkX = x - 1; checkX <= x + 1; checkX++)
                            {
                                for (int checkY = y - 1; checkY <= y + 1; checkY++)
                                {
                                    if (checkX >= 0 && checkX < leafArray.GetLength(0) && checkY >= 0 && checkY < leafArray.GetLength(1))
                                    {
                                        if (leafArray[checkX, checkY] == null)
                                        {
                                            leafArray[checkX, checkY] = new BlockGenerationVariables(0, new SemiLeafBlock((int)blockIDs.semiLeaves), 0, 0);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            //Convert it to blocks in the world
            Random r = new Random();
            for (int x = 0; x < leafArray.GetLength(0); x++)
            {
                for (int y = 0; y < leafArray.GetLength(1); y++)
                {
                    if (leafArray[x, y] != null)
                    {
                        int localToGlobalX = (x - leafArray.GetLength(0) / 2) + baseX;
                        int localToGlobalY = (y - leafArray.GetLength(1)) + baseY;
                        if (localToGlobalX >= 0 && localToGlobalY >= 0 && localToGlobalX < wg.worldArray.GetLength(0) && localToGlobalY < wg.worldArray.GetLength(1))
                        {
                            if (wg.worldArray[localToGlobalX, localToGlobalY] == (int)blockIDs.air || r.NextDouble() < probabilityToGenerateLeavesOverWood)
                            {
                                wg.worldArray[localToGlobalX, localToGlobalY] = leafArray[x, y].block.ID;
                            }
                        }
                    }
                }
            }
        }

    }

    public class BranchVariables
    {
        public double angle;
        public double x;
        public double y;
        public int currentLength;
        public double maxBranchLength;
        public int branchLayerDepth;

        public BranchVariables(double angle, double x, double y, int currentLength, double maxBranchLength, int branchLayerDepth)
        {
            this.angle = angle;
            this.x = x;
            this.y = y;
            this.currentLength = currentLength;
            this.maxBranchLength = maxBranchLength;
            this.branchLayerDepth = branchLayerDepth;
        }
    }

    public class BranchEndVariables
    {
        public int x;
        public int y;
        public int branchLayerDepth;

        public BranchEndVariables(int x, int y, int branchLayerDepth)
        {
            this.x = x;
            this.y = y;
            this.branchLayerDepth = branchLayerDepth;
        }
    }

    public class BushGeneration : Decoration {

        public blockIDs bushBlock = blockIDs.bush;
        public override bool generate(int x, int y, WorldContext wc)
        {
            if (wc.worldArray[x, y].ID == (int)blockIDs.air && wc.worldArray[x,y + 1].ID == (int)blockIDs.dirt || wc.worldArray[x, y + 1].ID == (int)blockIDs.grass)
            {
                wc.addBlock(x, y, (int)bushBlock);
                return true;

            }
            else
            {
                return false;
            }
        }

        public override bool generate(int x, int y, WorldGenerator wg)
        {
            if (wg.worldArray[x, y - 1] == (int)blockIDs.air && wg.worldArray[x, y + 1] == (int)blockIDs.dirt || wg.worldArray[x, y + 1] == (int)blockIDs.grass)
            {
                wg.worldArray[x, y - 1] = (int)bushBlock;
                return true;

            }
            else {
                return false;
            }

        }
    }

    public class BigBushGeneration : Decoration
    {

        public blockIDs bushBlock = blockIDs.bigBush;
        public override bool generate(int x, int y, WorldContext wc)
        {
            if (wc.worldArray[x, y].ID == (int)blockIDs.air && (wc.worldArray[x, y + 1].ID == (int)blockIDs.dirt || wc.worldArray[x, y + 1].ID == (int)blockIDs.grass))
            {
                wc.addBlock(x, y, (int)bushBlock);
                return true;

            }
            else
            {
                return false;
            }
        }

        public override bool generate(int x, int y, WorldGenerator wg)
        {
            if (wg.worldArray[x, y - 1] == (int)blockIDs.air && (wg.worldArray[x, y] == (int)blockIDs.dirt || wg.worldArray[x, y] == (int)blockIDs.grass) && wg.worldArray[x + 1, y - 1] == (int)blockIDs.air && (wg.worldArray[x + 1, y] == (int)blockIDs.dirt || wg.worldArray[x + 1, y] == (int)blockIDs.grass))
            {
                wg.worldArray[x, y - 1] = (int)bushBlock;
                return true;

            }
            else
            {
                return false;
            }
        }
    }
    #endregion    #region UI classes
    public enum UIAlignOffset {
        TopLeft,
        Centre
    }
    public enum Position {
        Absolute,
        Relative
    }
    public enum Scale {
        Absolute,
        Relative
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
        public Position positionType;
        public Scale scaleType;
        public Color color = Color.White;

        public const int defaultScreenWidth = 1920;
        public const int defaultScreenHeight = 1080;

        public bool isUIElementActive = true;
        public Scene scene;

        public virtual void updateElement(double elapsedTime, Game1 game) { }
    }
    public class InteractiveUIElement : UIElement {

        public float clickCooldown;
        public float maxClickCooldown;

        public string buttonText;
        public Vector2 textLocation;
        public virtual void onLeftClick(Game1 game) { }
        public virtual void onRightClick(Game1 game) { }
    }

    public class UILine {
        public Vector2 point1;
        public Vector2 point2;

        public float rotation;
        public Rectangle drawRectangle;

        public Color drawColor;

        public int lineWidth = 3;

        public Scene scene;

        public UILine(Vector2 point1, Vector2 point2)
        {
            this.point1 = point1;
            this.point2 = point2;

            drawRectangle = new Rectangle((int)point1.X, (int)point1.Y, lineWidth, 10);
        }

        public void updateSecondPoint(Vector2 secondPoint)
        {
            point2 = secondPoint;   
        }

        public void updateFirstPoint(Vector2 firstPoint) {
            point1 = firstPoint;
            drawRectangle.X = (int)point1.X - (int)(lineWidth / 2.0);
            drawRectangle.Y = (int)point1.Y;
        }

        public virtual void updateLine() {


            int distance = (int)Math.Pow(Math.Pow(point1.X - point2.X, 2) + Math.Pow(point1.Y - point2.Y, 2), 0.5);
            drawRectangle.Height = distance;

            //Rotation:
            rotation = (float)Math.Atan((point1.Y - point2.Y) / (point1.X - point2.X));

            if (point2.X > point1.X)
            {
                rotation -= MathHelper.PiOver2;
            }
            else
            {
                rotation += MathHelper.PiOver2;
            }
        }
    }
    public class MainMenuTitle : UIElement {
        public MainMenuTitle() {
            spriteSheetID = (int)spriteSheetIDs.mainMenuUI;
            drawRectangle = new Rectangle(0, 50, 1160, 152);
            sourceRectangle = new Rectangle(0, 0, 145, 19);
            alignment = UIAlignOffset.Centre;
            scaleType = Scale.Relative;
            positionType = Position.Relative;

            scene = Scene.MainMenu;
        }
    }
    public class MainMenuStartButton : InteractiveUIElement {
        UIElement generateWorldText;
        int tickCount = 0;
        public MainMenuStartButton(UIElement generateWorldText) {
            spriteSheetID = (int)spriteSheetIDs.mainMenuUI;
            drawRectangle = new Rectangle(0, 400, 192, 66);
            sourceRectangle = new Rectangle(0, 25, 33, 12);
            alignment = UIAlignOffset.Centre;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            tickCount = 0;
            scene = Scene.MainMenu;
            //this.generateWorldText = generateWorldText;
        }
        public override void onLeftClick(Game1 game)
        {
            //generateWorldText.isUIElementActive = true;
            if (tickCount == 0)
            {
                StringRenderer sr = new StringRenderer(Scene.MainMenu, UIAlignOffset.Centre, 42 ,false);
                sr.setWorldContext(game.worldContext);
                sr.setString("Generating the world");
                sr.updateLocation(0, 350);

            }
            tickCount += 1;

        }
        public override void updateElement(double elapsedTime, Game1 game)
        {
            //If the button was pressed for 2 ticks, then generate the world. This allows the UI to update

            if (tickCount > 10) {
                (int width, int height) worldDimensions = (800, 800);
                game.worldContext.generateWorld(worldDimensions);
                game.changeScene(Scene.Game);
            }
        }
    }
    public class MainMenuWorldGenText : UIElement {
        public MainMenuWorldGenText() {
            isUIElementActive = false;
            spriteSheetID = (int)spriteSheetIDs.mainMenuUI;
            drawRectangle = new Rectangle(0, 350, 576, 30);
            sourceRectangle = new Rectangle(0, 38, 96, 5);
            alignment = UIAlignOffset.Centre;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.MainMenu;
        }
    }

    public class InventoryBackground : UIElement {
        public InventoryBackground() {
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            drawRectangle = new Rectangle(0, 66, 594, 266);
            sourceRectangle = new Rectangle(0, 32, 297, 132);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
            isUIElementActive = false;
        }
    }

    public class EquipmentBackground : UIElement{
        public EquipmentBackground() {
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            isUIElementActive = false;
            sourceRectangle = new Rectangle(297,0, 65, 132);
            drawRectangle = new Rectangle(700, 66, 130, 264);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
        }
    }
    public class Hotbar : UIElement {
        public Hotbar()
        {
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            drawRectangle = new Rectangle(0, 0, 594, 64);
            sourceRectangle = new Rectangle(0, 0, 297, 32);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
        }
    }
    public class HotbarSelected : UIElement {
        public HotbarSelected()
        {
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            drawRectangle = new Rectangle(0, 0, 64, 64);
            sourceRectangle = new Rectangle(1, 165, 32, 32);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
        }

        public void swapItem(int x) {
            int pixelsPerHotbarSlot = 66;
            drawRectangle = new Rectangle(pixelsPerHotbarSlot * x, 0, 64, 64);
        }
    }

    public class HealthBarOutline : UIElement {
        public HealthBarOutline() {
            spriteSheetID = (int)spriteSheetIDs.healthUI;
            sourceRectangle = new Rectangle(0, 0, 145, 23);
            drawRectangle = new Rectangle(0, 900, 290, 46);
            alignment = UIAlignOffset.Centre;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Game;
        }
    }
    public class HealthBar : UIElement {
        public int maxHealthDrawWidth = 290;
        public HealthBar() {
            spriteSheetID = (int)spriteSheetIDs.healthUI;
            sourceRectangle = new Rectangle(0, 25, 145, 21);
            drawRectangle = new Rectangle(0, 902, 290, 46);
            alignment = UIAlignOffset.Centre;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Game;
        }
    }

    public class RespawnScreen : UIElement {
        public RespawnScreen()
        {
            spriteSheetID = (int)spriteSheetIDs.deathScreen;
            sourceRectangle = new Rectangle(0, 0, 384, 216);
            drawRectangle = new Rectangle(0, 0, 1920, 1080);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Relative;
            scene = Scene.Game;

            isUIElementActive = false;
        }
    }

    public class RespawnButton : InteractiveUIElement {
        Player player;
        RespawnScreen rs;
        public RespawnButton(Player owner, RespawnScreen rs) {
            spriteSheetID = (int)spriteSheetIDs.deathScreen;
            sourceRectangle = new Rectangle(177, 74, 40, 8);
            drawRectangle = new Rectangle(885, 370, 200, 40);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Game;
            isUIElementActive = false;
            player = owner;
            this.rs = rs;
        }

        public override void onLeftClick(Game1 game)
        {
            player.respawn();
            game.changeScene(Scene.Evolution);
            rs.isUIElementActive = false;
            isUIElementActive = false;
        }
    }

    public class EndEvolutionButton : InteractiveUIElement {

        double mouseMovementCoefficient = 0.027;
        public EndEvolutionButton()
        {
            spriteSheetID = (int)spriteSheetIDs.deathScreen;
            sourceRectangle = new Rectangle(177, 74, 40, 12);
            drawRectangle = new Rectangle(885, 900, 200, 60);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Evolution;
            isUIElementActive = true;
        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            if (Mouse.GetState().X >= 0 && Mouse.GetState().X < game.GraphicsDevice.Viewport.Width)
            {
                drawRectangle.X = 885 + (int)(mouseMovementCoefficient * Mouse.GetState().X);
            }

            if (Mouse.GetState().Y >= 0 && Mouse.GetState().Y < game.GraphicsDevice.Viewport.Height)
            {
                drawRectangle.Y = 900 + (int)(mouseMovementCoefficient * Mouse.GetState().Y);
            }
        }

        public override void onLeftClick(Game1 game)
        {
            game.changeScene(Scene.Game);
        }
    }

    public class Damage : UIElement {
        WorldContext worldContext;

        double x;
        double y;

        int drawOrder;

        double yIncrease = 30;
        double maxExistingDuration = 0.5;
        double existingDuration;

        public Damage(WorldContext wc, int damageAmount, double x, double y, int drawOrder) {

            this.drawOrder = drawOrder;
            drawRectangle = new Rectangle((int)x + wc.screenSpaceOffset.x, (int)y + wc.screenSpaceOffset.y, 10, 14);
            this.x = x;
            this.y = y;
            existingDuration = maxExistingDuration;

            sourceRectangle = new Rectangle(damageAmount * 6, 0, 5, 7);
            worldContext = wc;
            spriteSheetID = (int)spriteSheetIDs.pixelNumbers;
            
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            scene = Scene.Game;
            isUIElementActive = true;
        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            existingDuration -= elapsedTime;
            y -= ((yIncrease / maxExistingDuration) * elapsedTime);

            drawRectangle = new Rectangle((int)x + worldContext.screenSpaceOffset.x, (int)(y + worldContext.screenSpaceOffset.y), 10, 14);

            if (existingDuration <= 0) {
                worldContext.engineController.UIController.removeUIElement(drawOrder, this);
            }
        }
    }

    public class EvolutionStarBackground : UIElement {

        public double mouseMovementCoefficient;


        int startXOffset = 1920;
        int startYOffset = 1080;

        int sourceY = 0;
        int sourceX = 0;
        
        public EvolutionStarBackground()
        {
            spriteSheetID = (int)spriteSheetIDs.evolutionBackground;
            sourceRectangle = new Rectangle(0, 0, 960, 540);
            drawRectangle = new Rectangle(0, 0, 1920, 1080);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Absolute;
            scaleType = Scale.Relative;
            scene = Scene.Evolution;
            isUIElementActive = true;


            mouseMovementCoefficient = 1;
        }

        public void setSourceLocation(int x, int y) {
            
            sourceY = y;
            sourceX = x;
        }

        public void zeroStartingOffset()
        {
            startXOffset = 0;
            startYOffset = 0;
        }

        public override void updateElement(double elapsedTime, Game1 game) {
            if (Mouse.GetState().X >= 0 && Mouse.GetState().X < game.GraphicsDevice.Viewport.Width) {
                sourceRectangle.X = sourceX + (int)(startXOffset * mouseMovementCoefficient) - (int)(mouseMovementCoefficient * Mouse.GetState().X);
            }

            if (Mouse.GetState().Y >= 0 && Mouse.GetState().Y < game.GraphicsDevice.Viewport.Height)
            {
                sourceRectangle.Y = sourceY + (int)(startYOffset * mouseMovementCoefficient)- (int)(mouseMovementCoefficient * Mouse.GetState().Y);
            }
        }
    }

    public class EvolutionButton : InteractiveUIElement {
        double mouseMovementCoefficient = 0.027;

        const int iconWidth = 15;

        const int heightBetweenLayers = 75;

        int defaultX;
        int defaultY;

        const int heightOffset = 200;

        public Evolution ownerEvolution;

        bool isClicked = false;
        public EvolutionButton(Evolution owner)
        {
            spriteSheetID = (int)spriteSheetIDs.evolutionIcons;
            sourceRectangle = new Rectangle(0, owner.iconSourceY, 15, 15);
            //Adjust the draw location based on the location within the tree
            drawRectangle = new Rectangle(50, 50, 45, 45);
            alignment = UIAlignOffset.TopLeft;
            positionType = Position.Relative;
            scaleType = Scale.Relative;
            scene = Scene.Evolution;
            isUIElementActive = true;

            ownerEvolution = owner;
        }

        public void setSourceLocation(int x, int y) {
            sourceRectangle.X = x;
            sourceRectangle.Y = y;
        }

        public void setLocationFromTree() {
            //Set the x and y location on screen based on the tree layer
            int evolutionsInLayer = ownerEvolution.tree.evolutionTree[ownerEvolution.treeLayer].evolutionLayer.Count;
            defaultX = (ownerEvolution.indexWithinLayer + 1) * (defaultScreenWidth / (evolutionsInLayer + 1));
            defaultY = defaultScreenHeight - ((ownerEvolution.treeLayer + 1) * heightBetweenLayers + heightOffset);
        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            if (Mouse.GetState().X >= 0 && Mouse.GetState().X < game.GraphicsDevice.Viewport.Width)
            {
                drawRectangle.X = defaultX + (int)(mouseMovementCoefficient * Mouse.GetState().X);
            }

            if (Mouse.GetState().Y >= 0 && Mouse.GetState().Y < game.GraphicsDevice.Viewport.Height)
            {
                drawRectangle.Y = defaultY + (int)(mouseMovementCoefficient * Mouse.GetState().Y);
            }

            if (ownerEvolution.isEvolutionActive)
            {
                sourceRectangle.X = 3 * iconWidth;
            }
            else if (!ownerEvolution.canBeActivated)
            {
                if (isClicked)
                {
                    sourceRectangle.X = iconWidth;
                }
                else
                {
                    sourceRectangle.X = 0;
                }
            }
            else {
                sourceRectangle.X = 2 * iconWidth;
            }

                isClicked = false;
        }

        public override void onLeftClick(Game1 game)
        {
            isClicked = true;
            ownerEvolution.requestActivation();
        }
    }

    public class EvolutionDependencyLine : UILine {
        EvolutionButton evolution;
        EvolutionButton dependencyEvolution;
        public EvolutionDependencyLine(EvolutionButton evolution, EvolutionButton dependency, Vector2 point1, Vector2 point2) : base(point1, point2) {
            this.evolution = evolution;
            dependencyEvolution = dependency;

            scene = Scene.Evolution;

            point1 = new Vector2(evolution.drawRectangle.X + evolution.drawRectangle.Width/2, evolution.drawRectangle.Y + evolution.drawRectangle.Height);
            point2 = new Vector2(dependencyEvolution.drawRectangle.X + dependencyEvolution.drawRectangle.Width / 2, dependencyEvolution.drawRectangle.Y + dependencyEvolution.drawRectangle.Height);

        }

        public override void updateLine()
        {
            
            point1 = new Vector2(evolution.drawRectangle.X + evolution.drawRectangle.Width / 2, evolution.drawRectangle.Y + evolution.drawRectangle.Height);
            
            point2 = new Vector2(dependencyEvolution.drawRectangle.X + dependencyEvolution.drawRectangle.Width / 2, dependencyEvolution.drawRectangle.Y);

            drawRectangle.X = (int)point1.X;
            drawRectangle.Y = (int)point1.Y;


            if (dependencyEvolution.ownerEvolution.isEvolutionActive)
            {
                drawColor = Color.White;
            }
            else {
                drawColor = new Color(50, 50, 50);
            }
                base.updateLine();
        }
    }

    public class ExperienceStringCharacter : UIElement {
        const int characterSizeInPixels = 6;

        double mouseMovementCoefficient = 0.027;

        int defaultX;
        int defaultY;
        public ExperienceStringCharacter(int character, int x, int y) {
            spriteSheetID = (int)spriteSheetIDs.evolutionCounterCharacters;
            sourceRectangle = new Rectangle(character * characterSizeInPixels, 0, 5, 7);
            drawRectangle = new Rectangle(x, y, 10, 14);
            defaultX = x;
            defaultY = y;
            scene = Scene.Evolution;
            alignment = UIAlignOffset.TopLeft;
            scaleType = Scale.Relative;
            positionType = Position.Relative;

            isUIElementActive = true;


        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            if (Mouse.GetState().X >= 0 && Mouse.GetState().X < game.GraphicsDevice.Viewport.Width)
            {
                drawRectangle.X = defaultX + (int)(mouseMovementCoefficient * Mouse.GetState().X);
            }

            if (Mouse.GetState().Y >= 0 && Mouse.GetState().Y < game.GraphicsDevice.Viewport.Height)
            {
                drawRectangle.Y = defaultY + (int)(mouseMovementCoefficient * Mouse.GetState().Y);
            }
        }
        }

    public class ExperienceCounter{
        public List<ExperienceStringCharacter> stringCharacters = new List<ExperienceStringCharacter>();
        const int stringDrawLayer = 14;
        public WorldContext worldContext;

        public string stringNumber;

        int x;
        int y;

        const int characterSizeInPixels = 10;
        public ExperienceCounter(WorldContext wc, int x, int y) {
            worldContext = wc;
            this.x = x;
            this.y = y;
        }
        public void updateString(string newString) {
            while(stringCharacters.Count > 0) {
                worldContext.engineController.UIController.removeUIElement(stringDrawLayer, stringCharacters[0]);
                stringCharacters.RemoveAt(0);
            }


            stringNumber = newString;

            //for each character of the string:
            for (int i = 0; i < newString.Length; i++) {
                string character = newString[i].ToString();
                ExperienceStringCharacter esc = new ExperienceStringCharacter(Convert.ToInt32(character), x + i * characterSizeInPixels, y);
                stringCharacters.Add(esc);
                worldContext.engineController.UIController.addUIElement(stringDrawLayer, esc);
            }
        }
    }

    public class CraftItemButton : InteractiveUIElement{
        Player owner;
        Item craftedItem;
        CraftingRecipe recipe;

        public CraftItemBackground background;

        const int drawLayer = 15;

        public int x;
        public int y;

        public CraftItemButton(CraftingRecipe recipe) {
            this.recipe = recipe;
            setItem(recipe.recipeOutput);
            owner = recipe.manager.worldContext.player;

            recipe.manager.worldContext.engineController.UIController.addUIElement(drawLayer, this);

            scene = Scene.Game;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;

            maxClickCooldown = 0.25f;
            

            background = new CraftItemBackground();
            recipe.manager.worldContext.engineController.UIController.addUIElement(drawLayer - 1, background);

        }
        public void setItem(Item item) {
            if (item != null)
            {
                this.craftedItem = item;
                if (item.currentStackSize <= 1) { buttonText = null; }
                spriteSheetID = item.spriteSheetID;
                sourceRectangle = item.sourceRectangle;
                int offsetWidth = item.drawDimensions.width;
                int offsetHeight = item.drawDimensions.height;

                //If the sprite is the exact same size, don't offset it by anything
                //If the sprite is smaller, offset it by half - half the width
                drawRectangle.Width = item.drawDimensions.width;
                drawRectangle.Height = item.drawDimensions.height;
                x = ((64 - offsetWidth)/2 );
                y = ((64 - offsetHeight)/2);
                textLocation = new Vector2( offsetWidth + ((64 - offsetWidth) / 2),   offsetWidth + ((64 - offsetHeight) / 2));
            }
            else
            {
                this.craftedItem = null;
                sourceRectangle = new Rectangle(0, 0, 0, 0);
                drawRectangle = new Rectangle(0, 0, 64, 64);
            }
        }

        public override void updateElement(double elapsedTime, Game1 game)
        {
            isUIElementActive = recipe.canBeCrafted && recipe.manager.showCraftingSystem;
            background.drawRectangle.X = drawRectangle.X - x;
            background.drawRectangle.Y = drawRectangle.Y - y;
            background.isUIElementActive = isUIElementActive;

            base.updateElement(elapsedTime, game);
        }
        public override void onLeftClick(Game1 game)
        {
            clickCooldown = maxClickCooldown;
            
            if (recipe.canBeCrafted)
            {
                Item floatingItem = owner.selectedItem.item;
                bool couldCombineItems = false;
                owner.selectedItem.clickedOnAUIElement = true;
                if (floatingItem != null && craftedItem != null)
                {
                    //Add some logic in here for combining items onto a stack when crafting. Shouldn't be super hard to do
                }
                else
                {
                    owner.selectedItem.setItem(craftedItem.itemCopy(craftedItem.currentStackSize));
                    recipe.itemWasCrafted();
                }
            }
        }
    }

    public class CraftItemBackground : UIElement {
        public CraftItemBackground() {
            scene = Scene.Game;
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            spriteSheetID = (int)spriteSheetIDs.inventoryUI;
            sourceRectangle = new Rectangle(0, 32, 32, 33);
            drawRectangle = new Rectangle(0,0,64,66);
        }
    }

    public class StringRenderer
    {
        public List<StringCharacter> visualisedString = new List<StringCharacter>();


        public string stringToRender;

        const int UILayer = 18;

        int textDrawHeight;

        public bool isVisible = true;

        public StringRendererBackground background;

        WorldContext wc;

        Scene scene;
        UIAlignOffset stringOffset;

        bool haveBackground = false;

        int maxLineLength = 0;

        public int x;
        public int y;

        int borderOffsetX;
        int borderOffsetY;
        public StringRenderer(Scene scene, UIAlignOffset offset, int textHeight, bool haveBackground)
        {
            this.scene = scene;
            this.haveBackground = haveBackground;
            this.textDrawHeight = textHeight;

            stringOffset = offset;
        }

        public void setWorldContext(WorldContext wc) {
            this.wc = wc;
        }

        public void updateLocation(int x, int y)
        {
            if (stringOffset == UIAlignOffset.Centre) {
                x -= maxLineLength / 2;
            }

            this.x = x;
            this.y = y;


            for (int i = 0; i < visualisedString.Count; i++)
            {
                visualisedString[i].drawRectangle.X = visualisedString[i].x + x;
                visualisedString[i].drawRectangle.Y = visualisedString[i].y + y;
            }
            if (background != null)
            {
                background.setLocation(x, y);
            }
        }

        public void showString() {
            //Re-generate the string, reduces the size of the UIElements list, and hence performance: a lot.
            setString(stringToRender);

            
            isVisible = true;
        }

        public void hideString() {
            for (int i = 0; i < visualisedString.Count; i++) {
                wc.engineController.UIController.removeUIElement(UILayer, visualisedString[i]);
            }

            visualisedString.Clear();
            if (background != null)
            {
                background.clear();
            }
            isVisible = false;
        }

        public void setString(string stringToSet)
        {
            stringToRender = stringToSet;

            for (int i = 0; i < visualisedString.Count; i++)
            {
                //Clear it from the other lists
                wc.engineController.UIController.removeUIElement(UILayer, visualisedString[i]);
            }
            visualisedString.Clear();

            int trailingX = 0;
            int baseTrailingX = 0;
            int maxLineHeight = 35;
            int currentLineY = 0;

            maxLineLength = 0;
            int textHeight = 0;

            Tag currentTag = Tag.None;

            for (int i = 0; i < stringToRender.Length; i++)
            {
                if (stringToRender[i] == '\n')
                {
                    currentLineY += maxLineHeight;
                    trailingX = baseTrailingX;
                    maxLineHeight = 0;
                }
                else if (stringToRender[i] == '<')
                {
                    //Find in the string the next '>' tag and take the string between
                    if (stringToRender[i + 1] != '/')
                    {
                        int endTagIndex = stringToRender.IndexOf('>', i);
                        string tag = stringToRender.Substring(i + 1, endTagIndex - (i + 1)).ToLower();

                        switch (tag)
                        {
                            case "purple":
                                currentTag |= Tag.Purple;
                                break;
                            case "green":
                                currentTag |= Tag.Green;
                                break;
                            case "grey":
                                currentTag |= Tag.Grey;
                                break;
                            case "gold":
                                currentTag |= Tag.Gold;
                                break;
                            case "red":
                                currentTag |= Tag.Red;
                                break;
                            case "h1":
                                currentTag |= Tag.H1;
                                break;
                            case "h2":
                                currentTag |= Tag.H2;
                                break;
                            case "h3":
                                currentTag |= Tag.H3;
                                break;

                        }

                        i = endTagIndex;
                    }
                    else
                    {
                        //Is an end tag
                        int endTagIndex = stringToRender.IndexOf('>', i);
                        string tag = stringToRender.Substring(i + 2, endTagIndex - (i + 2)).ToLower();

                        switch (tag)
                        {
                            case "purple":
                                currentTag &= ~Tag.Purple;
                                break;
                            case "green":
                                currentTag &= ~Tag.Green;
                                break;
                            case "grey":
                                currentTag &= ~Tag.Grey;
                                break;
                            case "gold":
                                currentTag &= ~Tag.Gold;
                                break;
                            case "red":
                                currentTag &= ~Tag.Red;
                                break;
                            case "h1":
                                currentTag &= ~Tag.H1;
                                break;
                            case "h2":
                                currentTag &= ~Tag.H2;
                                break;
                            case "h3":
                                currentTag &= ~Tag.H3;
                                break;

                        }
                        i = endTagIndex;
                    }
                }
                else
                {
                    TextStyle currentStyle = TextStyle.None;
                    if ((currentTag & Tag.H1) == Tag.H1)
                    {
                        currentStyle |= TextStyle.h1;
                    }
                    else if ((currentTag & Tag.H2) == Tag.H2)
                    {
                        currentStyle |= TextStyle.h2;
                    }
                    else if ((currentTag & Tag.H3) == Tag.H3)
                    {
                        currentStyle |= TextStyle.h3;
                    }

                    StringCharacter c = new StringCharacter(stringToRender[i], textDrawHeight, trailingX, currentLineY, currentStyle);
                    
                    c.scene = scene;
                    c.alignment = stringOffset;
                    //I could improve this by allowing for nested color tags
                    if ((currentTag & Tag.Purple) == Tag.Purple)
                    {
                        c.color = Color.Purple;
                    }
                    else if ((currentTag & Tag.Green) == Tag.Green)
                    {
                        c.color = Color.Green;
                    }
                    else if ((currentTag & Tag.Gold) == Tag.Gold)
                    {
                        c.color = Color.Gold;
                    }
                    else if ((currentTag & Tag.Grey) == Tag.Grey)
                    {
                        c.color = Color.Gray;
                    }
                    else if ((currentTag & Tag.Red) == Tag.Red)
                    {
                        c.color = Color.Red;
                    }
                    trailingX += c.drawRectangle.Width;
                    if (maxLineLength < trailingX)
                    {
                        maxLineLength = trailingX;
                    }
                    if (c.drawRectangle.Height > maxLineHeight)
                    {
                        maxLineHeight = c.drawRectangle.Height;
                    }

                    visualisedString.Add(c);
                    wc.engineController.UIController.addUIElement(UILayer, c);
                }
            }
            if (background != null) {
                background.clear();
            }
            if (haveBackground)
            {
                background = new StringRendererBackground(wc, scene, stringOffset, maxLineLength, currentLineY + maxLineHeight, x, y);
            }
        }

        public void replaceAllTags(Tag find, Tag replaceWith)
        {
            string tagToFind = tagToString(find);



            string tagToReplace = tagToString(replaceWith);
            //Replace the start and end tags in two different stages:
            //Add a "<" at the start, then insert a "><" into all of the spaces, then add a ">"
            if (tagToFind != "" && tagToReplace != "")
            {
                string startTagToFind = tagToFind;
                startTagToFind = "<" + startTagToFind;
                while (startTagToFind != startTagToFind.Replace(" ", "><"))
                {
                    startTagToFind = startTagToFind.Replace(" ", "><");
                }
                string startTagToReplace = tagToReplace;
                startTagToReplace = "<" + startTagToReplace;
                while (startTagToReplace != startTagToReplace.Replace(" ", "><"))
                {
                    startTagToReplace = startTagToReplace.Replace(" ", "><");
                }

                while (stringToRender.Replace(startTagToFind, startTagToReplace) != stringToRender)
                {
                    stringToRender = stringToRender = stringToRender.Replace(startTagToFind, startTagToReplace);
                }


                //End tags:

                string endTagToFind = tagToFind;
                endTagToFind = "</" + endTagToFind;
                while (endTagToFind != endTagToFind.Replace(" ", "></"))
                {
                    endTagToFind = endTagToFind.Replace(" ", "></");
                }

                string endTagToReplace = tagToReplace;
                endTagToReplace = "</" + endTagToReplace;
                while (endTagToReplace != endTagToReplace.Replace(" ", "></"))
                {
                    endTagToReplace = endTagToReplace.Replace(" ", "></");
                }

                while (stringToRender.Replace(endTagToFind, endTagToReplace) != stringToRender)
                {
                    stringToRender = stringToRender.Replace(endTagToFind, endTagToReplace);
                }


            }
            setString(stringToRender);
        }

        public string tagToString(Tag tag)
        {
            string stringVersion = "";
            if ((tag & Tag.Purple) == Tag.Purple)
            {
                stringVersion += "purple ";
            }
            if ((tag & Tag.Green) == Tag.Green)
            {
                stringVersion += "green ";
            }
            if ((tag & Tag.Grey) == Tag.Grey)
            {
                stringVersion += "grey ";
            }
            if ((tag & Tag.Gold) == Tag.Gold)
            {
                stringVersion += "gold ";
            }
            if ((tag & Tag.Red) == Tag.Red)
            {
                stringVersion += "red ";
            }
            if ((tag & Tag.H1) == Tag.H1)
            {
                stringVersion += "h1 ";
            }
            if ((tag & Tag.H2) == Tag.H2)
            {
                stringVersion += "h2 ";
            }
            if ((tag & Tag.H3) == Tag.H3)
            {
                stringVersion += "h3 ";
            }

            stringVersion = stringVersion.TrimEnd();

            return stringVersion;

        }
    }
    public class StringCharacter : UIElement
    {
        double drawScale = 3;
        

        const int characterWidth = 5;
        const int characterHeight = 7;

        public int x;
        public int y;

        bool isAlphabet = false;

        public StringCharacter(char character, int drawHeight, int x, int y, TextStyle style)
        {
            isUIElementActive = true;
            spriteSheetID = (int)spriteSheetIDs.stringRendering;

            alignment = UIAlignOffset.TopLeft;
            scaleType = Scale.Absolute;
            positionType = Position.Absolute;

            drawScale = drawHeight / (double)characterHeight;

            this.x = x;
            this.y = y;
            int relativeValue = -4;
            if (65 <= character && character <= 90)
            {
                //Is a capital letter
                relativeValue = character - 'A';
                isAlphabet = true;
            }
            else if (97 <= character && character <= 122)
            {
                relativeValue = character - 'a';
                isAlphabet = true;
            }

            if (style == TextStyle.h1)
            {
                drawScale *= 2;
            }
            else if (style == TextStyle.h2)
            {
                drawScale = drawScale * 1.8;
            }
            else if (style == TextStyle.h3)
            {
                drawScale = drawScale * 1.6;
            }
            else if (style == TextStyle.h4)
            {
                drawScale = drawScale * 1.4;
            }
            else if (style == TextStyle.h5)
            {
                drawScale = drawScale * 1.2;
            }

            drawRectangle = new Rectangle(x, y, (int)(characterWidth * drawScale), (int)(characterHeight * drawScale));

            if (isAlphabet)
            {
                sourceRectangle = new Rectangle(4 * relativeValue, 0, 5, 7);

                if (relativeValue > 12)
                {
                    sourceRectangle.X += 2;
                }
                else if (relativeValue == 12)
                {
                    sourceRectangle.Width += 2;
                    drawRectangle.Width += (int)(drawScale * 2);
                }
                if (relativeValue > 13)
                {
                    sourceRectangle.X += 1;
                }
                else if (relativeValue == 13)
                {
                    sourceRectangle.Width += 1;

                    drawRectangle.Width += (int)drawScale;
                }

                if (relativeValue > 22)
                {
                    sourceRectangle.X += 2;
                }
                else if (relativeValue == 22)
                {
                    sourceRectangle.Width += 2;

                    drawRectangle.Width += (int)(drawScale * 2);
                }
            }

            //If it's not an alphabetical character, then check if it's a number:
            else if (48 <= character && character <= 57)
            {
                relativeValue = character - 48;
                sourceRectangle = new Rectangle(relativeValue * (characterWidth - 1), 6, characterWidth, characterHeight);
            }
            //Special cases
            //Does use magic numbers, so sorry, however they can be found from the alphabet.png file
            if (character == ' ')
            {
                drawRectangle.Width = (int)(characterWidth * drawScale) / 2;
            }
            else if (character == ':')
            {
                sourceRectangle = new Rectangle(0, 14, characterWidth, characterHeight);
            }
            else if (character == '!')
            {
                sourceRectangle = new Rectangle(5, 14, characterWidth, characterHeight);
            }
            else if (character == '%')
            {
                sourceRectangle = new Rectangle(9, 14, characterWidth, characterHeight);
            }
        }

    }
    public class StringRendererBackground
    {
        public int x;
        public int y;
        StringRendererBackgroundSegment top = new StringRendererBackgroundSegment();
        StringRendererBackgroundSegment body = new StringRendererBackgroundSegment();
        StringRendererBackgroundSegment bottom = new StringRendererBackgroundSegment();

        const int UILayer = 17;

        WorldContext wc;


        const int imageWidth = 64;
        const int pixelsHighToBorder = 12;

        double drawScale = 1;
        //3 parts:

        int height;
        int width;

        const int pixelOffset = 8;
    
        public StringRendererBackground(WorldContext wc, Scene scene, UIAlignOffset offset, int width, int height, int x, int y)
        {
            //Scale the image according to the width, then variably increase the height by cutting it into 3 sections
            this.x = x;
            this.y = y;
            this.wc = wc;


            drawScale = (double)width / (imageWidth - 2 * pixelOffset);

            height += (int)(drawScale);
            width += (int)(2 * pixelOffset * drawScale);
            this.height = height;
            this.width = width;

            top.scene = scene;
            top.alignment = offset;
            body.scene = scene;
            body.alignment = offset;
            bottom.scene = scene;
            bottom.alignment = offset;
            top.drawRectangle = new Rectangle(x, y, width, (int)(pixelsHighToBorder * drawScale));
            body.drawRectangle = new Rectangle(x, y + (int)(pixelsHighToBorder * drawScale), width, height - (int)(pixelsHighToBorder * drawScale));
            bottom.drawRectangle = new Rectangle(x, y + height, width, (int)(pixelsHighToBorder * drawScale));

            top.sourceRectangle = new Rectangle(0, 0, imageWidth, pixelsHighToBorder);
            body.sourceRectangle = new Rectangle(0, pixelsHighToBorder, imageWidth, imageWidth - 2 * pixelsHighToBorder);
            bottom.sourceRectangle = new Rectangle(0, imageWidth - pixelsHighToBorder, imageWidth, pixelsHighToBorder);

            wc.engineController.UIController.addUIElement(UILayer, top);
            wc.engineController.UIController.addUIElement(UILayer, body);
            wc.engineController.UIController.addUIElement(UILayer, bottom);

        }

        public void setLocation(int x, int y)
        {
            top.drawRectangle.X = x - (int)(pixelOffset * drawScale);
            top.drawRectangle.Y = y - (int)(pixelOffset * drawScale);

            body.drawRectangle.X = x - (int)(pixelOffset * drawScale);
            body.drawRectangle.Y = y + (int)(pixelsHighToBorder * drawScale) - (int)(pixelOffset * drawScale);


            bottom.drawRectangle.X = x - (int)(pixelOffset * drawScale);
            bottom.drawRectangle.Y = y + height - (int)(pixelOffset * drawScale);


        }

        public void hideBackground() {
            top.isUIElementActive = false;
            body.isUIElementActive = false;
            bottom.isUIElementActive = false;
        }

        public void showBackground() {
            top.isUIElementActive = true;
            body.isUIElementActive = true;
            bottom.isUIElementActive = true;
        }

        public void clear() {
            wc.engineController.UIController.removeUIElement(UILayer, top);
            wc.engineController.UIController.removeUIElement(UILayer, body);
            wc.engineController.UIController.removeUIElement(UILayer, bottom);
        }
    }
    public class StringRendererBackgroundSegment : UIElement {
        public StringRendererBackgroundSegment() {
            positionType = Position.Absolute;
            scaleType = Scale.Absolute;
            alignment = UIAlignOffset.TopLeft;
            spriteSheetID = (int)spriteSheetIDs.tooltipBackground;
            isUIElementActive = true;
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

            double midX = (pointList[i - 1].x + pointList[i].x) / 2;
            double midY = (pointList[i - 1].y + pointList[i].y) / 2;

            return (midX, midY);
        }

        public double generateRandomOffset() {
            Random r = new Random();

            double sign = r.Next(-(100 - positiveWeight), positiveWeight);
            if (sign == 0) { sign = 1; } //To prevent any weird terrain caused by 0 values

            return offset * Math.Sign(sign);
        }
    }

    public class PhysicsObject
    {
        public double accelerationX { get; set; }
        public double accelerationY { get; set; }

        public bool calculatePhysics = true;

        public double velocityX { get; set; }
        public double velocityY { get; set; }

        public List<(Vector2 direction, double magnitude, double duration)> impulse { get; set; }

        public double x { get; set; }
        public double y { get; set; }

        public double kX { get; set; }
        public double defaultkX { get; set; }
        public double kY { get; set; }
        public double defaultkY { get; set; }

        public double cummulativeCoefficientOfFriction { get; set; }
        public double objectCoefficientOfFriction { get; set; }

        public int frictionDirection { get; set; }
        public double bounceCoefficient { get; set; }

        public double buoyancyCoefficient { get; set; }

        public double minVelocityX { get; set; }
        public double minVelocityY { get; set; }

        public double maxMovementVelocityX { get; set; }
        public double baseMaxMovementVelocityX { get; set; }

        public Rectangle collider { get; set; }

        public double drawWidth { get; set; }
        public double drawHeight { get; set; }
        public double width { get; set; }
        public double height { get; set; }



        public WorldContext worldContext { get; set; }

        public bool isOnGround { get; set; }

        public bool isInFluid { get; set; }

        public PhysicsObject(WorldContext wc)
        {
            impulse = new List<(Vector2 direction, double magnitude, double duration)>();

            accelerationX = 0.0;
            accelerationY = 0.0;
            velocityX = 0;    
            velocityY = 0;
            x = 0.0;
            y = 0.0;
            kX = 0.0;
            kY = 0.0;
            bounceCoefficient = 0.0;
            minVelocityX = 0.001;
            minVelocityY = 0.01;
            isOnGround = false;
            buoyancyCoefficient = 1;

            collider = new Rectangle(0, 0, wc.pixelsPerBlock, wc.pixelsPerBlock);

            worldContext = wc;
        }

        public virtual void updateLocation(double xChange, double yChange)
        {
            x += xChange;
            y += yChange;

        }

        public virtual void onBlockCollision(Vector2 collisionNormal, WorldContext worldContext, int blockX, int blockY)
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

        //Have to add Lists of listeners to certain events
        public List<IEntityActionListener> damageListeners = new List<IEntityActionListener>();
        public List<IEntityActionListener> inputListeners = new List<IEntityActionListener>();
        public List<IEntityActionListener> entityCollisionListeners = new List<IEntityActionListener>();
        public List<IEntityActionListener> blockCollisionListeners = new List<IEntityActionListener>();


        public double horizontalAccelerationIncreasePerExperience;
        public double maxSpeedIncreasePerExperience;
        public double verticalAccelerationIncreasePerExperience;

        public double healthIncreasePerExperience;
        public double toughnessIncreasePerExperience;

        public double experienceGainIncreasePerExperience;

        public double damageIncreasePerExperience;

        public double baseEntityDamageMultiplier = 1;
        public double entityDamageMultiplier;

        public double baseExperienceMultiplier = 1;
        public double experienceMultiplier;

        public double knockbackStunDuration;

        //The entities current health at a point in time
        public double currentHealth;
        //The entities base helath: It's a default per entity
        public double baseHealth;
        //The entities max health after equipment is applied
        public double maxHealth;

        //Variables for motion
        public double baseHorizontalAcceleration;
        public double horizontalAcceleration;

        public double baseJumpAcceleration;
        public double jumpAcceleration;



        public Entity(WorldContext wc) : base(wc) {
            worldContext.physicsObjects.Add(this);
        }
        public void setSpriteTexture(Texture2D spriteSheet)
        {
            this.spriteSheet = spriteSheet;
            spriteAnimator.spriteSheet = spriteSheet;
        }
        public virtual void inputUpdate(double elapsedTime)
        {
            for (int i = 0; i < inputListeners.Count; i++) {
                inputListeners[i].onInput(elapsedTime);
            }

            
        }

        public override void onBlockCollision(Vector2 collisionNormal, WorldContext worldContext, int blockX, int blockY)
        {
            for (int i = 0; i < blockCollisionListeners.Count; i++) {
                blockCollisionListeners[i].onBlockCollision();
            }
            if (!worldContext.worldArray[blockX, blockY].isBlockTransparent)
            {
                //If the collision is upwards acting
                if (collisionNormal.Y == 1)
                {
                    float someThreshold = -15f;
                    if (velocityY <= someThreshold)
                    {
                        applyDamage(null, DamageType.Falldamage, -velocityY);
                    }
                }
            }

        }
        public virtual void applyDamage(object attacker, DamageType damageType, double damage) {
            for (int i = 0; i < damageListeners.Count; i++) {
                damage = damageListeners[i].onDamage(attacker, damageType, damage);
            }
            currentHealth -= damage;
            //Create a damage uielement
            string integerDamageAsAString = ((int)damage).ToString();


            for (int i = 0; i < integerDamageAsAString.Length; i++) {
                Damage d = new Damage(worldContext, Convert.ToInt32(integerDamageAsAString[i].ToString()), x + 11 * i, y, 15);
                worldContext.engineController.UIController.addUIElement(15, d);
            }

            if (currentHealth <= 0) {
                onDeath(attacker, damageType, damage);
            }
        }


        //Currently depricated
        public virtual void applyEffect() { }

        public virtual void onDeath(object attacker, DamageType damageType, double damageThatKilled) {
            
        }

        

        public virtual Entity copyEntity() {
            return new Entity(worldContext);
        }
    }
    public class SpawnableEntity : Entity {

        public Biome homeBiome { get; set; }
        public int spawnableEntityListIndex { get; set; }

        public double screenLengthsUntilDespawn = 1.5;
        public SpawnableEntity(WorldContext worldContext) : base(worldContext) {}

        public void setBiome(Biome biome, int spawnableEntityIndex)
        {
            homeBiome = biome;
            spawnableEntityListIndex = spawnableEntityIndex;
        }
        public virtual void despawn()
        {
            if (homeBiome != null)
            {
                homeBiome.currentBiomeEntityCount -= 1;
                homeBiome.spawnableEntities[spawnableEntityListIndex].currentSpecificEntityCount -= 1;
                
            }

            worldContext.physicsObjects.Remove(this);
            worldContext.engineController.entityController.removeEntity(this);
        }

        public override void inputUpdate(double elapsedTime)
        {
            base.inputUpdate(elapsedTime);
            //Despawn the entity if it's sufficiently offscreen:

            //If it's too far up, down left or right
            if (x < -worldContext.screenSpaceOffset.x - screenLengthsUntilDespawn * worldContext.applicationWidth || x > -worldContext.screenSpaceOffset.x + screenLengthsUntilDespawn * worldContext.applicationWidth || y < -worldContext.screenSpaceOffset.y - screenLengthsUntilDespawn * worldContext.applicationHeight || y > -worldContext.screenSpaceOffset.y + screenLengthsUntilDespawn * worldContext.applicationHeight) {
                despawn();
                
            }

        }
    }
    public class ControlledEntity : SpawnableEntity, IGroundTraversalAlgorithm, IPassiveCollider
    {
        
        public double notJumpThreshold { get; set; }
        public double jumpWhenWithinXRange { get; set; }

        public bool isActive { get; set; }

        public double attackCooldown;
        public double maxAttackCooldown = 0.4f;

        public double invincibilityCooldown { get; set; }
        public double maxInvincibilityCooldown { get; set; }

        public double targetX { get; set; }
        public double targetY { get; set; }

        public double percievedX { get; set; }
        public double percievedY { get; set; }
        public double perceptionDistance { get; set; }

        public double damage = 15;
        public double xDifferenceThreshold { get; set; }

        //Each list of loot tables inside the list is exclusive. Only one of each loot table would be generated
        LootTable entityDropLoot = new LootTable();
        

        Player player;

        int playerDirection;
        public ControlledEntity(WorldContext wc, Player target) : base(wc)
        {
            player = target;

            notJumpThreshold = -100;
            perceptionDistance = 1000;
            xDifferenceThreshold = 10;
            drawWidth = 1.5f;
            drawHeight = 3;
            playerDirection = 1;

            maxMovementVelocityX = 7;

            maxInvincibilityCooldown = 0.2;

            accelerationX = 0.0;
            accelerationY = 0.0;
            velocityX = 0;
            velocityY = 0;
            x = 40.0;
            y = 00.0;
            kX = 0.02;
            kY = 0.02;

            defaultkX = 0.02;
            defaultkY = 0.02;
            bounceCoefficient = 0.0;
            minVelocityX = 0.5;
            minVelocityY = 0.01;

            width = 0.8;
            height = 2.7;

            baseHorizontalAcceleration = 200;
            baseJumpAcceleration = 12;

            collider = new Rectangle(0, 0, (int)(width * wc.pixelsPerBlock), (int)(height * wc.pixelsPerBlock));

            drawWidth = 1.5f;
            drawHeight = 3;

            rotation = 0;
            rotationOrigin = Vector2.Zero;

            maxHealth = 100;
            baseHealth = 100;
            currentHealth = maxHealth;

            worldContext.engineController.collisionController.addPassiveCollider(this);
            isActive = true;

            generateLootTable();


            spriteAnimator = new SpriteAnimator(animationController: worldContext.animationController, constantOffset: new Vector2(12f, 8f), frameOffset: new Vector2(32, 65), sourceDimensions: new Vector2((float)32, (float)64), animationlessSourceRect: new Rectangle(160, 0, (int)32, (int)64), owner: this);

            spriteAnimator.animationDictionary = new Dictionary<string, (int frameCount, int yOffset)> {

                { "walk", (6, 0) }

            };

            setSpriteTexture(worldContext.engineController.spriteController.spriteSheetList[(int)spriteSheetIDs.player]);

            spriteAnimator.startAnimation(0.1, "walk");

            wc.engineController.entityController.addEntity(this);
        }

        public virtual void generateLootTable()
        {
            //Primary loot tables

            entityDropLoot.addLootTable(
                new List<(double percentage, IndividualLootTable)>() {
                    (50, new IndividualLootTable(new List<Loot>() { new Loot(100, 1, 1, new Bow()) })),
                    (50, new IndividualLootTable(new List<Loot>() { new Loot(100, 1, 1, new CloudInAJar()) }))
                }
            );

                //Secondary loot tables
            entityDropLoot.addLootTable(
                new List<(double percentage, IndividualLootTable)>(){
                    (100, new IndividualLootTable(new List<Loot>(){
                        new Loot(40, 10, 30, new BlockItem((int)blockIDs.torch)),
                        new Loot(25, 5, 20, new BlockItem((int)blockIDs.grass))
                    }))
                }
            );
        }

        public override void inputUpdate(double elapsedTime)
        {
            base.inputUpdate(elapsedTime);
            int leftRight = 0;
            int upDown = 0;
            targetX = player.x;
            targetY = player.y;

            if (attackCooldown <= 0 && knockbackStunDuration <= 0)
            {
                (int horizontal, int vertical) algorithmOutput = ((IGroundTraversalAlgorithm)this).traverseTerrain();
                leftRight = algorithmOutput.horizontal;
                upDown = algorithmOutput.vertical;
            }
            if(attackCooldown > 0) {
                attackCooldown -= elapsedTime;
            }
            if (knockbackStunDuration > 0) {
                knockbackStunDuration -= elapsedTime;
            }


            if (leftRight == 1)
            {
                if (velocityX < maxMovementVelocityX)
                {
                    //The or is not on ground is there to allow air control for QOL
                    if (baseHorizontalAcceleration < cummulativeCoefficientOfFriction * worldContext.engineController.physicsEngine.gravity || !isOnGround)
                    {
                        accelerationX += baseHorizontalAcceleration;
                        frictionDirection += 1;
                    }
                    else
                    {
                        accelerationX += cummulativeCoefficientOfFriction * worldContext.engineController.physicsEngine.gravity;
                        frictionDirection += 1;
                    }
                }
                else
                {
                    frictionDirection += 1;
                }

                if (!spriteAnimator.isAnimationActive)
                {
                    spriteAnimator.startAnimation(0.5, "walk");
                }
                playerDirection = 1;
                directionalEffect = SpriteEffects.None;

            }
            if (leftRight == 2)
            {
                if (velocityX >  -maxMovementVelocityX)
                {
                    //The or is not on ground is there to allow air control for QOL
                    if (baseHorizontalAcceleration < cummulativeCoefficientOfFriction * worldContext.engineController.physicsEngine.gravity || !isOnGround)
                    {
                        accelerationX -= baseHorizontalAcceleration;
                        frictionDirection -= 1;
                    }
                    else
                    {
                        accelerationX -= cummulativeCoefficientOfFriction * worldContext.engineController.physicsEngine.gravity;
                        frictionDirection -= 1;
                    }
                }
                else
                {
                    frictionDirection -= 1;
                }
                if (!spriteAnimator.isAnimationActive)
                {
                    spriteAnimator.startAnimation(0.5, "walk");
                }
                playerDirection = -1;
                directionalEffect = SpriteEffects.FlipHorizontally;
            }
            if (leftRight == 0)
            {
                spriteAnimator.isAnimationActive = false; //If the entity isn't walking, stop the animation
            }
            if(upDown == 1)
            {
                if (isOnGround)
                {
                    accelerationY += baseJumpAcceleration / elapsedTime;
                }
            }

            //Update invincibilityCooldown:
            if (invincibilityCooldown <= 0 && !isActive)
            {
                isActive = true;
            }
            else if (invincibilityCooldown > 0)
            {
                invincibilityCooldown -= elapsedTime;
            }
        }

        public override void despawn()
        {
            base.despawn();
            worldContext.engineController.collisionController.removePassiveCollider(this);
        }

        public override void onDeath(object attacker, DamageType damageType, double damageThatKilled)
        {
            //Drop loot:
            List<Item> loot = entityDropLoot.generateLoot();
            Random r = new Random();
            for (int i = 0; i < loot.Count; i++) {
                new DroppedItem(worldContext, loot[i], (x,y), new Vector2((float)r.NextDouble() * 4f, (float)r.NextDouble() * 4f));
            }

            despawn();
        }

        public override Entity copyEntity(){
            return new ControlledEntity(worldContext, player);
        }

        public void onCollision(ICollider externalCollider) {
            if (externalCollider is Player p) {
                p.velocityX = 7 * playerDirection;
                p.velocityY += 7;

                //Have to move the player up, because of the slight overlap with the lower block, it causes a collision to detect and counteract the velocity?
                p.y -= 12;
                p.applyDamage(this, DamageType.EntityAttack, damage);
                attackCooldown = maxAttackCooldown;
                p.knockbackStunDuration = 0.2f;
                ((ICollider)p).startInvincibilityFrames();
            }
        }
    }

    public class Player : Entity, IInventory, IActiveCollider
    {
        int emmissiveStrength = 500;
        int emmissiveMax = 125;
        int[,] lightMap;

        public bool writeToChat;

        public Player owner { get; set; }
        public bool isActive { get; set; }
        public double invincibilityCooldown { get; set; }
        public double maxInvincibilityCooldown { get; set; }
        public UIItem[,] inventory { get; set; }
        public UIItem[,] equipmentInventory;
        public FloatingUIItem selectedItem;
       

        public List<IInventory> activeInventories = new List<IInventory>();

        public PlayerCraftingDictionary craftingDictionary;

        public UIElement inventoryBackground { get; set; }
        public UIElement equipmentBackground;

        public int collisionCount = 0;

        public int playerDirection { get; set; }


        int initialX = 10;
        int initialY = 10;

        public Item mainHand;
        public int mainHandIndex;

        float discardCooldown;
        float maxDiscardCooldown = 0.1f;

        double openInventoryCooldown;
        double maxOpenInventoryCooldown = 0.2f;

        

        public EvolutionTree evolutionTree;

        public PlayerUIController playerUI;

        public Player(WorldContext wc) : base(wc)
        {
            wc.setPlayer(this);

            loadSettings();

            playerUI = new PlayerUIController(this);

            //need to dissociate the collider width and the draw width. 
            collider = new Rectangle(0, 0, (int)(width * wc.pixelsPerBlock), (int)(height * wc.pixelsPerBlock));

            worldContext.engineController.collisionController.addActiveCollider(this);
            isActive = true;
            maxInvincibilityCooldown = 0.5;

            maxMovementVelocityX = 8;
            baseMaxMovementVelocityX = 8;

            objectCoefficientOfFriction = 1;

            owner = this;

            drawWidth = 1.5f;
            drawHeight = 3;

            baseEntityDamageMultiplier = 1;
            entityDamageMultiplier = 1;

            rotation = 0;
            rotationOrigin = Vector2.Zero;

            maxHealth = 100;
            baseHealth = 100;
            currentHealth = maxHealth;

            lightMap = wc.engineController.lightingSystem.calculateLightMap(emmissiveStrength);

            initialiseEvolutionTree();

            //Add a second system 
            //Initialise inventory
            int inventoryWidth = 9;
            int inventoryHeight = 5;
            initialiseInventory(worldContext, inventoryWidth, inventoryHeight);
            //Setup initial inventory

            inventory[0, 0].setItem(new Pickaxe());
            

            inventory[1, 0].setItem(new BlockItem((int)blockIDs.water));
            if (inventory[1, 0].item is BlockItem b3)
            {
                b3.currentStackSize = 99;
            }

            inventory[2, 0].setItem(new Helmet());
            inventory[3, 0].setItem(new CloudInAJar());


            spriteAnimator = new SpriteAnimator(animationController: worldContext.animationController, constantOffset: new Vector2(12f, 8f), frameOffset: new Vector2(32, 65), sourceDimensions: new Vector2((float)32, (float)64), animationlessSourceRect: new Rectangle(160, 0, (int)32, (int)64), owner: this);

            spriteAnimator.animationDictionary = new Dictionary<string, (int frameCount, int yOffset)> {

                { "walk", (6, 0) }

            };

            wc.engineController.entityController.addEntity(this);

            craftingDictionary = new PlayerCraftingDictionary(worldContext.engineController.craftingManager);

            initialiseStatGainPerExperience();

            showInventory();
            hideInventory();
        }

        private void loadSettings() {
            StreamReader sr = new StreamReader(worldContext.runtimePath + "Settings\\PlayerSettings.txt");
            sr.ReadLine();
            initialX = Convert.ToInt32(sr.ReadLine());
            initialY = Convert.ToInt32(sr.ReadLine());
            x = initialX;
            y = initialY;
            sr.ReadLine();
            defaultkX = Convert.ToDouble(sr.ReadLine());
            defaultkY = Convert.ToDouble(sr.ReadLine());
            sr.ReadLine();
            width = Convert.ToDouble(sr.ReadLine());
            height = Convert.ToDouble(sr.ReadLine());
            sr.ReadLine();
            emmissiveStrength = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            emmissiveMax = Convert.ToInt32(sr.ReadLine());
            sr.ReadLine();
            baseHorizontalAcceleration = Convert.ToDouble(sr.ReadLine());
            sr.ReadLine();
            baseJumpAcceleration = Convert.ToDouble(sr.ReadLine());
        }

        public void initialiseEvolutionTree() {
            evolutionTree = new PlayerEvolutionTree(this);
        }

        public void initialiseStatGainPerExperience() {
            //For every experience, 1 percent increase
            experienceGainIncreasePerExperience = 0.01;

            //For every experience, gain 1 health
            healthIncreasePerExperience = 1;

            toughnessIncreasePerExperience = 0.01;

            damageIncreasePerExperience = 0.1;
            

            horizontalAccelerationIncreasePerExperience = 3;
            verticalAccelerationIncreasePerExperience = 0.1;

            maxSpeedIncreasePerExperience = 0.05;

        }
        public void initialiseInventory(WorldContext worldContext, int inventoryWidth, int inventoryHeight)
        {
            inventory = new UIItem[inventoryWidth, inventoryHeight];
            equipmentInventory = new UIItem[2, 4];
            inventoryBackground = new InventoryBackground();
            equipmentBackground = new EquipmentBackground();
            worldContext.engineController.UIController.addUIElement(3, inventoryBackground);
            worldContext.engineController.UIController.inventoryBackgrounds.Add(inventoryBackground);
            worldContext.engineController.UIController.inventoryBackgrounds.Add(equipmentBackground);
            worldContext.engineController.UIController.addUIElement(3, equipmentBackground);

            for (int x = 0; x < inventory.GetLength(0); x++) {
                for (int y = 0; y < inventory.GetLength(1); y++) {
                    inventory[x, y] = new UIItem(x, y, playerUI.hotbar.drawRectangle.X, playerUI.hotbar.drawRectangle.Y, this);
                    worldContext.engineController.UIController.addUIElement(5, inventory[x, y]);
                }
            }
            equipmentInventory[0, 0] = new AccessoryUIItem(0, 0, equipmentBackground.drawRectangle.X, equipmentBackground.drawRectangle.Y, this);
            equipmentInventory[0, 1] = new AccessoryUIItem(0, 1, equipmentBackground.drawRectangle.X, equipmentBackground.drawRectangle.Y, this);
            equipmentInventory[0, 2] = new AccessoryUIItem(0, 2, equipmentBackground.drawRectangle.X, equipmentBackground.drawRectangle.Y, this);
            equipmentInventory[0, 3] = new AccessoryUIItem(0, 3, equipmentBackground.drawRectangle.X, equipmentBackground.drawRectangle.Y, this);


            equipmentInventory[1, 0] = new EquipmentUIItem(ArmorType.Head, 1, 0, equipmentBackground.drawRectangle.X, equipmentBackground.drawRectangle.Y, this);
            equipmentInventory[1, 1] = new EquipmentUIItem(ArmorType.Chest, 1, 1, equipmentBackground.drawRectangle.X, equipmentBackground.drawRectangle.Y, this);
            equipmentInventory[1, 2] = new EquipmentUIItem(ArmorType.Legs, 1, 2, equipmentBackground.drawRectangle.X, equipmentBackground.drawRectangle.Y, this);
            equipmentInventory[1, 3] = new EquipmentUIItem(ArmorType.Boots, 1, 3, equipmentBackground.drawRectangle.X, equipmentBackground.drawRectangle.Y, this);
            for (int x = 0; x < equipmentInventory.GetLength(0); x++)
            {
                for (int y = 0; y < equipmentInventory.GetLength(1); y++)
                {
                    worldContext.engineController.UIController.addUIElement(5, equipmentInventory[x, y]);
                }
            }

            selectedItem = new FloatingUIItem(this);
            worldContext.engineController.UIController.addUIElement(100, selectedItem);
        }

        public void showInventory() {
            if (!inventory[0, 1].isUIElementActive)
            {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    //Only hide the second row of the inventory, keep the hotbar
                    for (int y = 0; y < inventory.GetLength(1); y++)
                    {
                        inventory[x, y].isUIElementActive = true;
                    }
                }
                inventoryBackground.isUIElementActive = true;
                

                for (int x = 0; x < equipmentInventory.GetLength(0); x++)
                {
                    for (int y = 0; y < equipmentInventory.GetLength(1); y++)
                    {
                        equipmentInventory[x, y].isUIElementActive = true;
                    }
                }
                equipmentBackground.isUIElementActive = true;
                worldContext.engineController.craftingManager.inventoryWasOpened();   
            }

            
        }
        public void hideInventory() {
            if (inventory[0, 1].isUIElementActive) {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    //Only hide the second row of the inventory, keep the hotbar
                    for (int y = 1; y < inventory.GetLength(1); y++)
                    {
                        inventory[x, y].isUIElementActive = false;
                    }
                }
                inventoryBackground.isUIElementActive = false;

                for (int x = 0; x < equipmentInventory.GetLength(0); x++) {
                    for (int y = 0; y < equipmentInventory.GetLength(1); y++) {
                        equipmentInventory[x, y].isUIElementActive = false;
                    }
                }
                equipmentBackground.isUIElementActive = false;
                selectedItem.dropItem();

                worldContext.engineController.craftingManager.inventoryWasClosed();

            }

            for (int i = 0; i < activeInventories.Count; i++)
            {
                activeInventories[i].hideInventory();
            }
            activeInventories.Clear();

        }

        public bool addItemToInventory(Item item)
        {
            bool foundASlot = false;
            //Check for any stacks to add the item to
            for (int y = 0; y < inventory.GetLength(1); y++)
            {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    if (!foundASlot && inventory[x, y].item != null && item.currentStackSize > 0)
                    {

                        if (inventory[x, y].item.isItemIdentical(item))
                        {
                            //Class specific checks:
                            foundASlot = ((IInventory)this).combineItemStacks(item, x, y);
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
            base.inputUpdate(elapsedTime);

            increaseVariablesBasedOnExperience();
            if (!writeToChat)
            {
                if (currentHealth > 0)
                {
                    if (knockbackStunDuration <= 0)
                    {
                        //Movement
                        if (Keyboard.GetState().IsKeyDown(Keys.D))
                        {
                            if (velocityX < maxMovementVelocityX)
                            {
                                //The or is not on ground is there to allow air control for QOL
                                if (horizontalAcceleration < cummulativeCoefficientOfFriction * worldContext.engineController.physicsEngine.gravity || !isOnGround)
                                {
                                    accelerationX += horizontalAcceleration;
                                    frictionDirection += 1;
                                }
                                else
                                {
                                    accelerationX += cummulativeCoefficientOfFriction * worldContext.engineController.physicsEngine.gravity;
                                    frictionDirection += 1;
                                }
                            }
                            else
                            {
                                frictionDirection += 1;
                            }

                            if (!spriteAnimator.isAnimationActive)
                            {
                                spriteAnimator.startAnimation(0.5, "walk");
                            }
                            playerDirection = 1;
                            directionalEffect = SpriteEffects.None;

                        }
                        if (Keyboard.GetState().IsKeyDown(Keys.A))
                        {
                            if (velocityX > -maxMovementVelocityX)
                            {
                                if (horizontalAcceleration < cummulativeCoefficientOfFriction * worldContext.engineController.physicsEngine.gravity || !isOnGround)
                                {
                                    accelerationX -= horizontalAcceleration;
                                    frictionDirection -= 1;
                                }
                                else
                                {
                                    accelerationX -= cummulativeCoefficientOfFriction * worldContext.engineController.physicsEngine.gravity;
                                    frictionDirection -= 1;
                                }
                            }
                            else
                            {
                                frictionDirection -= 1;
                            }


                            if (!spriteAnimator.isAnimationActive)
                            {
                                spriteAnimator.startAnimation(0.5, "walk");
                            }
                            playerDirection = -1;
                            directionalEffect = SpriteEffects.FlipHorizontally;
                        }
                    }
                    else {
                        knockbackStunDuration -= elapsedTime;
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
                        respawn();
                    }

                    //Item Swapping
                    if (Keyboard.GetState().IsKeyDown(Keys.D1))
                    {
                        if (mainHand != null) { mainHand.onUnequip(); }
                        mainHand = inventory[0, 0].item;
                        inventory[0, 0].item.onEquip();
                        mainHandIndex = 0;
                        playerUI.swapHotbar();
                        if (mainHand != null) { mainHand.onEquip(); }

                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.D2))
                    {
                        if (mainHand != null) { mainHand.onUnequip(); }
                        mainHand = inventory[1, 0].item;

                        mainHandIndex = 1;
                        playerUI.swapHotbar();
                        if (mainHand != null) { mainHand.onEquip(); }
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.D3))
                    {
                        if (mainHand != null) { mainHand.onUnequip(); }
                        mainHand = inventory[2, 0].item;

                        mainHandIndex = 2;
                        playerUI.swapHotbar();
                        if (mainHand != null) { mainHand.onEquip(); }
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.D4))
                    {
                        if (mainHand != null) { mainHand.onUnequip(); }
                        mainHand = inventory[3, 0].item;

                        mainHandIndex = 3;
                        playerUI.swapHotbar();
                        if (mainHand != null) { mainHand.onEquip(); }
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.D5))
                    {
                        if (mainHand != null) { mainHand.onUnequip(); }
                        mainHand = inventory[4, 0].item;

                        mainHandIndex = 4;
                        playerUI.swapHotbar();
                        if (mainHand != null) { mainHand.onEquip(); }
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.D6))
                    {
                        if (mainHand != null) { mainHand.onUnequip(); }
                        mainHand = inventory[5, 0].item;

                        mainHandIndex = 5;
                        playerUI.swapHotbar();
                        if (mainHand != null) { mainHand.onEquip(); }
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.D7))
                    {
                        if (mainHand != null) { mainHand.onUnequip(); }
                        mainHand = inventory[6, 0].item;

                        mainHandIndex = 6;
                        playerUI.swapHotbar();
                        if (mainHand != null) { mainHand.onEquip(); }
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.D8))
                    {
                        if (mainHand != null) { mainHand.onUnequip(); }
                        mainHand = inventory[7, 0].item;

                        mainHandIndex = 7;
                        playerUI.swapHotbar();
                        if (mainHand != null) { mainHand.onEquip(); }
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.D9))
                    {
                        if (mainHand != null) { mainHand.onUnequip(); }
                        mainHand = inventory[8, 0].item;

                        mainHandIndex = 8;
                        playerUI.swapHotbar();
                        if (mainHand != null) { mainHand.onEquip(); }
                    }

                    if (mainHand is INonAxisAlignedActiveCollider i)
                    {
                        i.x = x - mainHand.origin.X;
                        i.y = y - mainHand.origin.Y;
                    }

                    
                    for (int x = 0; x < equipmentInventory.GetLength(0); x++)
                    {
                        for (int y = 0; y < equipmentInventory.GetLength(1); y++) {
                            if (equipmentInventory[x, y].item is EquipableItem e) {
                                e.onInput(elapsedTime);
                            }                        
                        }
                    }


                    if (openInventoryCooldown > 0)
                    {
                        openInventoryCooldown -= elapsedTime;
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.Tab))
                    {

                        if (!inventory[0, 1].isUIElementActive)
                        {

                            this.showInventory();
                        }
                        else
                        {
                            hideInventory();
                        }
                        openInventoryCooldown = maxOpenInventoryCooldown;
                    }

                    //Spawn an entity for testing:
                    if (Keyboard.GetState().IsKeyDown(Keys.J) && openInventoryCooldown <= 0) {
                        openInventoryCooldown = maxOpenInventoryCooldown;
                        ControlledEntity testEntity = new ControlledEntity(worldContext, this);
                        testEntity.x = Mouse.GetState().X - worldContext.screenSpaceOffset.x;
                        testEntity.y = Mouse.GetState().Y - worldContext.screenSpaceOffset.y;

                        //testEntity.setSpriteTexture(spriteSheet);
                    }
                        
                    //Check if the mainHand item no longer exists
                    if (mainHand != null)
                    {
                        if (mainHand.currentStackSize <= 0)
                        {
                            inventory[mainHandIndex, 0].setItem(null);
                            mainHand = null;
                        }
                    }

                    //Update dropCooldown
                    if (discardCooldown > 0)
                    {
                        discardCooldown -= (float)elapsedTime;
                    }
                    else if (Keyboard.GetState().IsKeyDown(Keys.Q))
                    {
                        if (mainHand != null)
                        {

                            float initialVelocity = 8f;
                            double pickupDelay = 1f;
                            if (playerDirection == -1)
                            {
                                initialVelocity *= -1;
                            }
                            DroppedItem dropItem = new DroppedItem(worldContext, mainHand.itemCopy(1), (x, y), new Vector2(initialVelocity, 0));
                            mainHand.currentStackSize -= 1;
                            if (mainHand.currentStackSize <= 0)
                            {
                                inventory[mainHandIndex, 0].setItem(null);
                                mainHand = null;
                            }
                            dropItem.x = x;
                            dropItem.y = y;
                            dropItem.pickupDelay = pickupDelay;
                            discardCooldown = maxDiscardCooldown;
                        }
                    }

                    //Update invincibilityCooldown:
                    if (invincibilityCooldown <= 0 && !isActive)
                    {
                        isActive = true;
                    }
                    else if (invincibilityCooldown > 0) {
                        invincibilityCooldown -= elapsedTime;
                    }

                    if (!inventory[0, 1].isUIElementActive)
                    {
                        ///Item Action
                        if (Mouse.GetState().LeftButton == ButtonState.Pressed)
                        {
                            if (mainHand != null)
                            {
                                mainHand.onLeftClick();
                            }
                        }
                    }
                }
            }
        }

        public void increaseVariablesBasedOnExperience() {
            //Incorporate adjusting all the base values depending on the experience:
            
            //Durability:
            maxHealth = baseHealth + healthIncreasePerExperience * evolutionTree.getExperience(ExperienceField.Durability);

            //Damage
            entityDamageMultiplier = baseEntityDamageMultiplier + damageIncreasePerExperience * evolutionTree.getExperience(ExperienceField.Damage);

            //Maneuverability
            horizontalAcceleration = baseHorizontalAcceleration + horizontalAccelerationIncreasePerExperience * evolutionTree.getExperience(ExperienceField.Maneuverability);
            jumpAcceleration = baseJumpAcceleration + verticalAccelerationIncreasePerExperience * evolutionTree.getExperience(ExperienceField.Maneuverability);
            maxMovementVelocityX = baseMaxMovementVelocityX + maxSpeedIncreasePerExperience * evolutionTree.getExperience(ExperienceField.Maneuverability);

            //Knowledge
            experienceMultiplier = baseExperienceMultiplier + experienceGainIncreasePerExperience * evolutionTree.getExperience(ExperienceField.Knowledge);


        }
        public void setSpawn(int x, int y) {
            initialX = x;
            initialY = y;
        }

        public void respawn() {
            x = initialX;
            y = initialY;
            velocityX = 0;
            velocityY = 0;
            currentHealth = maxHealth;
            playerUI.damageTaken();
        }

        public override void applyDamage(object attacker, DamageType damageType, double damage)
        {
            if (currentHealth > 0) //If the player isn't already dead
            {
                for (int x = 0; x < equipmentInventory.GetLength(0); x++)
                {
                    for (int y = 0; y < equipmentInventory.GetLength(1); y++)
                    {
                        if (equipmentInventory[x, y].item is EquipableItem e)
                        {
                            damage = e.onDamageTaken(damageType, damage, attacker);
                        }
                    }
                }

                base.applyDamage(attacker, damageType, damage);

                playerUI.damageTaken();
            }
        }

        public override void onDeath(object attacker, DamageType damageType, double damageThatKilled)
        {
            Entity entityAttacker = null ;
            if (attacker != null) {
                if (attacker is Entity e) {
                    entityAttacker = e;
                }
            }
            if (damageType == DamageType.Falldamage)
            {
                evolutionTree.addExperience(ExperienceField.Durability, 10 * experienceMultiplier);
            }
            if (entityAttacker != null) {
                //Later on, check if the attacker isn't a boss
                if (entityAttacker.maxHealth > maxHealth || entityAttacker.currentHealth > 0.3 * entityAttacker.maxHealth) {
                    evolutionTree.addExperience(ExperienceField.Durability, 10 * experienceMultiplier);
                    evolutionTree.addExperience(ExperienceField.Damage, 10 * experienceMultiplier);

                }
            }

            evolutionTree.addExperience(ExperienceField.Maneuverability, 8 * experienceMultiplier);
            evolutionTree.addExperience(ExperienceField.Knowledge, 10 * experienceMultiplier);


            evolutionTree.recalculateAvailableEvolutions();
            base.onDeath(attacker, damageType, damageThatKilled);
        }

        public override void updateLocation(double xChange, double yChange) {
            int xBlockChange = (int)(Math.Floor((x + xChange) / worldContext.pixelsPerBlock) - Math.Floor(x / worldContext.pixelsPerBlock));
            int yBlockChange = (int)(Math.Floor((y + yChange) / worldContext.pixelsPerBlock) - Math.Floor(y / worldContext.pixelsPerBlock));



            if (xBlockChange >= 1 || xBlockChange <= -1 || yBlockChange >= 1 || yBlockChange <= -1)
            {
                worldContext.engineController.lightingSystem.movedLight((int)Math.Floor(((x) / worldContext.pixelsPerBlock)) + collider.Width / (2 * worldContext.pixelsPerBlock), (int)Math.Floor((y) / worldContext.pixelsPerBlock), xBlockChange, yBlockChange, lightMap, emmissiveMax);
            }

            base.updateLocation(xChange, yChange);
        }

    }
    public class FloatingUIItem : InteractiveUIElement {

        public Item item;
        public Player owner;

        public bool clickedOnAUIElement = false;

        public FloatingUIItem(Player owner) {
            isUIElementActive = false;
            scene = Scene.Game;
            setItem(null);
            this.owner = owner;
            maxClickCooldown = 0.1f;
        }
        public void setItem(Item item)
        {
            if (item != null)
            {
                this.item = item;
                item.onPickup(owner);
                if (item.currentStackSize <= 1) { buttonText = null; }
                isUIElementActive = true;
                spriteSheetID = item.spriteSheetID;
                sourceRectangle = item.sourceRectangle;
                int offsetWidth = item.drawDimensions.width;
                int offsetHeight = item.drawDimensions.height;

                //If the sprite is the exact same size, don't offset it by anything
                //If the sprite is smaller, offset it by half - half the width
                drawRectangle = new Rectangle(Mouse.GetState().X + ((64 - offsetWidth) / 2), Mouse.GetState().Y + ((64 - offsetHeight) / 2), item.drawDimensions.width, item.drawDimensions.height);
            }
            else
            {
                this.item = null;
                isUIElementActive = false;
                sourceRectangle = new Rectangle(0, 0, 0, 0);
                drawRectangle = new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, 64, 64);
            }
        }

        public override void updateElement(double elasedTime, Game1 game)
        {
            if (isUIElementActive)
            {
                drawRectangle.X = Mouse.GetState().X;
                drawRectangle.Y = Mouse.GetState().Y;
                if (item != null)
                {
                    if (item.currentStackSize > 1)
                    {
                        buttonText = item.currentStackSize.ToString();
                    }
                    textLocation = new Vector2(drawRectangle.X + item.drawDimensions.width, drawRectangle.Y + item.drawDimensions.height);
                }
            }
        }

        public void dropItem() {
            if (item != null)
            {
                float pickupDelay = 1f;
                DroppedItem dropItem = new DroppedItem(owner.worldContext, item.itemCopy(item.currentStackSize), (owner.x, owner.y), Vector2.Zero);
                dropItem.x = owner.x;
                dropItem.y = owner.y;
                dropItem.pickupDelay = pickupDelay;
                owner.worldContext.engineController.entityController.addEntity(dropItem);
                setItem(null);
            }
        }

        public override void onLeftClick(Game1 game)
        {
            if (item != null && isUIElementActive) {
                clickCooldown = maxClickCooldown;
                if (!clickedOnAUIElement)
                {
                    bool isOutsideAllActiveInventoryBackgrounds = true;
                    for (int i = 0; i < game.worldContext.engineController.UIController.inventoryBackgrounds.Count; i++)
                    {
                        UIElement inventoryBackground = game.worldContext.engineController.UIController.inventoryBackgrounds[i];
                        if (inventoryBackground.isUIElementActive)
                        {
                            if (drawRectangle.X > inventoryBackground.drawRectangle.X && drawRectangle.Y > inventoryBackground.drawRectangle.Y && drawRectangle.X < inventoryBackground.drawRectangle.X + inventoryBackground.drawRectangle.Width && drawRectangle.Y < inventoryBackground.drawRectangle.Y + inventoryBackground.drawRectangle.Height)
                            {
                                isOutsideAllActiveInventoryBackgrounds = false;
                                //No need to continue checking, just break

                                break;
                            }
                        }
                    }
                    if (isOutsideAllActiveInventoryBackgrounds)
                    {
                        dropItem();
                    }
                }


                clickedOnAUIElement = false;
            }


        }
    }
    public class UIItem : InteractiveUIElement
    {
        public Item item;
        public Player owner;
        public IInventory inventory;
        //A class that represents an item. Each ui element contains it's own corrosponding item. 
        public int drawX;
        public int drawY;


        public const int inventorySlotSize = 66;

        public (int x, int y) inventoryIndex;

        bool lockedToInventory = true;
        //When a droppedItem entity is picked up, it either adjusts the item of the UIItem element, or it creates both a new UiElement and item class
        public UIItem(int x, int y, int inventoryDrawOffsetX, int inventoryDrawOffsetY, Player owner) {
            isUIElementActive = false;
            scene = Scene.Game;
            this.owner = owner;
            inventory = owner;

            inventoryIndex = (x, y);
            setDrawLocation(inventoryDrawOffsetX, inventoryDrawOffsetY);
            maxClickCooldown = 0.1f;
            //Just set the ID to be a random sprite sheet that has opacity
            spriteSheetID = (int)spriteSheetIDs.weapons;
            textLocation = Vector2.Zero;
            setItem(null);
        }
        public UIItem(int x, int y, int inventoryDrawOffsetX, int inventoryDrawOffsetY, WorldContext worldContext, IInventory inventoryClass)
        {
            isUIElementActive = false;
            scene = Scene.Game;
            this.owner = worldContext.player;
            inventory = inventoryClass;
            inventoryIndex = (x, y);
            setDrawLocation(inventoryDrawOffsetX, inventoryDrawOffsetY);
            maxClickCooldown = 0.1f;
            //Just set the ID to be a random sprite sheet that has opacity
            spriteSheetID = (int)spriteSheetIDs.weapons;
            textLocation = Vector2.Zero;
            setItem(null);
        }

        public void setDrawLocation(int inventoryDrawOffsetX, int inventoryDrawOffsetY) {
            drawX = inventorySlotSize * inventoryIndex.x + inventoryDrawOffsetX;
            drawY = inventorySlotSize * inventoryIndex.y + inventoryDrawOffsetY;
        }

        public void setItem(Item item) {
            if (item != null)
            {
                this.item = item;
                item.onPickup(owner);
                if (item.currentStackSize <= 1) { buttonText = null; }
                spriteSheetID = item.spriteSheetID;
                sourceRectangle = item.sourceRectangle;
                int offsetWidth = item.drawDimensions.width;
                int offsetHeight = item.drawDimensions.height;

                //If the sprite is the exact same size, don't offset it by anything
                //If the sprite is smaller, offset it by half - half the width
                drawRectangle = new Rectangle(drawX + ((64 - offsetWidth) / 2), drawY + ((64 - offsetHeight) / 2), item.drawDimensions.width, item.drawDimensions.height);
                textLocation = new Vector2(drawX + offsetWidth + ((64 - offsetWidth) / 2), drawY + offsetWidth + ((64 - offsetHeight) / 2));
            }
            else {
                this.item = null;
                sourceRectangle = new Rectangle(0, 0, 0, 0);
                drawRectangle = new Rectangle(drawX, drawY, 64, 64);
            }
        }

        public override void updateElement(double elasedTime, Game1 game)
        {
            if (item != null)
            {
                if (item.currentStackSize > 1)
                {
                    buttonText = item.currentStackSize.ToString();
                }
                if (isUIElementActive)
                {
                    if (item.tooltipRenderer != null)
                    {
                        
                        if (new Rectangle(Mouse.GetState().X, Mouse.GetState().Y, 5, 5).Intersects(drawRectangle))
                        {
                            if (item.tooltipRenderer.isVisible == false)
                            {
                                item.tooltipRenderer.showString();
                            }
                            else {
                                item.tooltipRenderer.updateLocation(Mouse.GetState().X + 25, Mouse.GetState().Y + 25);
                            }
                        }
                        else if(item.tooltipRenderer.isVisible == true)
                        {
                            item.tooltipRenderer.hideString();
                        }
                    }
                }
            }
            else {
                buttonText = "";
            }
        }
        public override void onLeftClick(Game1 game)
        {
            clickCooldown = maxClickCooldown;
            Item floatingItem = owner.selectedItem.item;
            bool couldCombineItems = false;
            if (floatingItem != null && item != null)
            {
                if (floatingItem.isItemIdentical(item))
                {

                    couldCombineItems = inventory.combineItemStacks(floatingItem, inventoryIndex.x, inventoryIndex.y);
                    if (couldCombineItems) {
                        if (item != null)
                        {
                            if (item.tooltipRenderer != null)
                            {
                                item.tooltipRenderer.hideString();
                            }
                        }
                        owner.selectedItem.setItem(null);
                        inventory.inventory[inventoryIndex.x, inventoryIndex.y].setItem(inventory.inventory[inventoryIndex.x, inventoryIndex.y].item);
                    }
                }
            }
            if (!couldCombineItems)
            {
                if (item != null)
                {
                    if (item.tooltipRenderer != null)
                    {
                        item.tooltipRenderer.hideString();
                    }
                }

                owner.selectedItem.setItem(item);
                setItem(floatingItem);
                if (inventoryIndex.x == owner.mainHandIndex && owner == inventory)
                {
                    owner.mainHand = null;
                }
            }
        }
    }
    public class EquipmentUIItem : UIItem {
        public ArmorType slotEquipmentType;
        
        public EquipmentUIItem(ArmorType equipmentSlotType, int x, int y, int inventoryDrawOffsetX, int inventoryDrawOffsetY, Player owner) : base(x, y, inventoryDrawOffsetX, inventoryDrawOffsetY, owner) {
            slotEquipmentType = equipmentSlotType;
            this.setDrawLocation(inventoryDrawOffsetX, inventoryDrawOffsetY);
            
        }

        public override void onLeftClick(Game1 game)
        {
            clickCooldown = maxClickCooldown;
            
            if (owner.selectedItem.item is Equipment floatingEquipmentItem)
            {
                
                if (floatingEquipmentItem.equipmentType == slotEquipmentType)
                {
                    owner.selectedItem.setItem(item);
                    if (item is Equipment previouslyEquipment)
                    {
                        previouslyEquipment.onUnequipFromSlot();
                    }
                    setItem(floatingEquipmentItem);
                    if (floatingEquipmentItem != null)
                    {
                        //Because the armor and all that never reach the mainhand, equip them 
                        floatingEquipmentItem.onEquipToSlot();
                    }
               
                }
            }
            else if (owner.selectedItem.item == null) {
                owner.selectedItem.setItem(item);
                setItem(null);
            }
        }
    }
    public class AccessoryUIItem : UIItem {

        public AccessoryUIItem(int x, int y, int inventoryDrawOffsetX, int inventoryDrawOffsetY, Player owner) : base(x, y, inventoryDrawOffsetX, inventoryDrawOffsetY, owner){
        
        }
        public override void onLeftClick(Game1 game)
        {
            clickCooldown = maxClickCooldown;
            if (owner.selectedItem.item is Accessory floatingAccessoryItem)
            {
                
                    owner.selectedItem.setItem(item);
                    if (item is Accessory previouslyEquipment)
                    {
                        previouslyEquipment.onUnequipFromSlot();
                    }
                    setItem(floatingAccessoryItem);
                    if (floatingAccessoryItem != null)
                    {
                        floatingAccessoryItem.onEquipToSlot();
                    }

            
            }
            else if (owner.selectedItem.item == null)
            {
                owner.selectedItem.setItem(item);
                setItem(null);
            }
        }
    }
    public class Arrow : Entity, IEmissive, IActiveCollider
    {
        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }
        public float range { get; set; }
        public Player owner { get; set; }
        public bool isActive { get; set; }

        public double weaponDamage = 10;

        public double initialVelocity;

        public double invincibilityCooldown { get; set; }
        public double maxInvincibilityCooldown { get; set; }
        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }
        public Arrow(WorldContext wc, (double x, double y) arrowLocation, double initialVelocity, Player shooter) : base(wc)
        {
            spriteSheet = wc.engineController.spriteController.spriteSheetList[(int)spriteSheetIDs.arrow];
            spriteAnimator = new SpriteAnimator(wc.animationController, Vector2.Zero, new Vector2(16, 16), new Vector2(16, 16), new Rectangle(0, 0, 16, 16), this);
            spriteAnimator.sourceOffset = new Vector2(0f, 16f);

            this.initialVelocity = initialVelocity;

            owner = shooter;

            rotationOrigin = Vector2.Zero;
            directionalEffect = SpriteEffects.None;
            isActive = true;
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
            worldContext.engineController.collisionController.addActiveCollider(this);
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

        public override void onBlockCollision(Vector2 collisionNormal, WorldContext worldContext, int blockX, int blockY)
        {
            //Don't take fall damage
        }

        public void onCollision(ICollider externalCollider) {
            if (externalCollider is Entity e)
            {
                e.velocityX = velocityX / 2 ;
                e.velocityY += 7;
                //Have to move the entity up, because of the slight overlap with the lower block, it causes a collision to detect and counteract the velocity?
                e.y -= 12;
                e.applyDamage(owner, DamageType.EntityAttack, owner.entityDamageMultiplier * weaponDamage * (Math.Pow(Math.Pow(velocityX, 2) + Math.Pow(velocityY, 2), 0.5)/initialVelocity));
                e.knockbackStunDuration = 0.5f;
                ((ICollider)e).startInvincibilityFrames();

                worldContext.engineController.entityController.removeEntity(this);
                worldContext.physicsObjects.Remove(this);
                worldContext.engineController.lightingSystem.lights.Remove(this);
                worldContext.engineController.collisionController.removeActiveCollider(this);

            }
        }

        public override void hasCollided()
        {
            calculatePhysics = false;
            velocityX = 0;
            velocityY = 0;
            worldContext.engineController.collisionController.removeActiveCollider(this);
            worldContext.engineController.lightingSystem.lights.Remove(this);
        }
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


    #region Interfaces
    public interface ICollider
    {
        bool isActive { get; set; }

        double x { get; set; }
        double y { get; set; }

        double invincibilityCooldown { get; set; }
        double maxInvincibilityCooldown { get; set; }

        public void onCollision(ICollider otherCollider)
        {
        }

        public void startInvincibilityFrames() {
            isActive = false;
            invincibilityCooldown = maxInvincibilityCooldown;
        }

        //It might be good to add a "updateInvincibilityFramesCount" function, but I think that would get annoying to call every inputUpdate within entities
    }

    public interface IActiveCollider : ICollider
    {

        Player owner { get; set; }

        Rectangle collider { get; set; }
        public virtual void calculateCollision(IPassiveCollider externalCollider)
        {
            if (new Rectangle((int)externalCollider.x + externalCollider.collider.X, (int)externalCollider.y + externalCollider.collider.Y, externalCollider.collider.Width, externalCollider.collider.Height).Intersects(new Rectangle(collider.X + (int)x, collider.Y + (int)y, collider.Width, collider.Height))) {
                //Collision happened!
                onCollision(externalCollider);
                externalCollider.onCollision(this);
            }
        }
    }

    public interface INonAxisAlignedActiveCollider : IActiveCollider
    {

        public Vector2[] rotatedPoints { get; set; }

        public Vector2[] originalPoints { get; set; }

        public Vector2 rotationOrigin { get; set; }

        public int colliderWidth { get; set; }
        public int colliderHeight { get; set; }

        

        public Animator itemAnimator { get; set; }

        public bool hasCollided { get; set; }



        public virtual void calculateCollision(IPassiveCollider externalCollider)
        {
                nonAxisAlignedCollisionDetection(externalCollider);
        }

        public void nonAxisAlignedCollisionDetection(IPassiveCollider externalCollider)
        {
            hasCollided = false;
            double theta = itemAnimator.currentPosition.rotation;


            Rectangle externalColliderInLocalSpace = new Rectangle((int)(externalCollider.x - x), (int)(externalCollider.y -y), externalCollider.collider.Width, externalCollider.collider.Height);

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
                    if (seperatingAxis.X > 0 ? shadow < rotatedPoints[i].X : shadow > rotatedPoints[i].X) { shadow = rotatedPoints[i].X; }

                }
                else if (seperatingAxis.Y != 0)
                {
                    if (seperatingAxis.Y > 0 ? shadow < seperatingAxis.Y : shadow > seperatingAxis.Y) { shadow = rotatedPoints[i].Y; }
                }
            }




            if (seperatingAxis.X != 0)
            {
                if (externalColliderInLocalSpace.X * seperatingAxis.X < Math.Abs(shadow) + externalColliderInLocalSpace.Width / 2)
                {
                    //Has collided
                    hasCollided = true;
                    onCollision(externalCollider);
                    externalCollider.onCollision(this);
                }
            }
            else if (seperatingAxis.Y != 0)
            {
                if (externalColliderInLocalSpace.Y * seperatingAxis.Y < Math.Abs(shadow) + externalColliderInLocalSpace.Height / 2)
                {
                    //Has collided
                    hasCollided = true;
                    onCollision(externalCollider);
                    externalCollider.onCollision(this);
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

    public interface IEmissiveBlock {
        public double x { get; set; }
        public double y { get; set; }

        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }

        public float range { get; set; }


        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }

    }

    public interface IInventory {
        public UIItem[,] inventory { get; set; }
        public UIElement inventoryBackground { get; set; }

        public void initialiseInventory(WorldContext worldContext, int inventoryWidth, int inventoryHeight)
        {
            inventory = new UIItem[inventoryWidth, inventoryHeight];
            worldContext.engineController.UIController.addUIElement(4, inventoryBackground);
            worldContext.engineController.UIController.inventoryBackgrounds.Add(inventoryBackground);
            for (int x = 0; x < inventory.GetLength(0); x++)
            {
                for (int y = 0; y < inventory.GetLength(1); y++)
                {
                    inventory[x, y] = new UIItem(x, y, inventoryBackground.drawRectangle.X, inventoryBackground.drawRectangle.Y, worldContext, this);
                    worldContext.engineController.UIController.addUIElement(5, inventory[x, y]);
                }
            }
        }

        public void destroyInventory(WorldContext worldContext, int xLoc, int yLoc) {
            for (int x = 0; x < inventory.GetLength(0); x++) {
                for (int y = 0; y < inventory.GetLength(1); y++) {
                    (int, UIElement) UIListElement = worldContext.engineController.UIController.UIElements.Find(i => i.uiElement == inventory[x,y]);
                    worldContext.engineController.UIController.removeUIElement(UIListElement.Item1, UIListElement.Item2);
                    Random r = new Random();
                    if (inventory[x, y].item != null)
                    {
                        
                        DroppedItem dropItem = new DroppedItem(worldContext, inventory[x, y].item, (xLoc, yLoc), new Vector2((float)r.NextDouble(), (float)r.NextDouble()));
                        dropItem.pickupDelay = 0f;
                        worldContext.engineController.entityController.addEntity(dropItem);
                    }
                }
            }
            (int, UIElement) InventoryBackgroundElement = worldContext.engineController.UIController.UIElements.Find(i => i.uiElement == inventoryBackground);
            worldContext.engineController.UIController.removeUIElement(InventoryBackgroundElement.Item1, InventoryBackgroundElement.Item2);
        }

        public bool addItemToInventory(Item item)
        {
            bool foundASlot = false;
            //Check for any stacks to add the item to
            for (int y = 0; y < inventory.GetLength(1); y++)
            {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    if (!foundASlot && inventory[x, y].item != null && item.currentStackSize > 0)
                    {

                        if (inventory[x, y].item.isItemIdentical(item))
                        {
                            //Class specific checks:
                            foundASlot = combineItemStacks(item, x, y);
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
        public bool combineItemStacks(Item item, int x, int y)
        {
            bool foundASlot = false;
            bool isTheRightItem = item.isItemIdentical(inventory[x,y].item);
            
            if (isTheRightItem)
            {
                int amountUntilMaxStack = inventory[x, y].item.maxStackSize - inventory[x, y].item.currentStackSize;
                if (amountUntilMaxStack > 0 && item.currentStackSize > 0)
                {
                    int stackSizeToAdd = item.currentStackSize;
                    if (stackSizeToAdd > amountUntilMaxStack) { stackSizeToAdd = amountUntilMaxStack; }
                    inventory[x, y].item.currentStackSize += stackSizeToAdd;

                    item.currentStackSize -= stackSizeToAdd;
                    if (item.currentStackSize <= 0)
                    {
                        foundASlot = true;
                    }
                }
                
            }
            return foundASlot;
        }
        public void showInventory()
        {
            if (!inventory[0, 1].isUIElementActive)
            {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    for (int y = 0; y < inventory.GetLength(1); y++)
                    {
                        inventory[x, y].isUIElementActive = true;
                    }
                }

                inventoryBackground.isUIElementActive = true;
            }

        }
        public void hideInventory()
        {
            if (inventory[0, 1].isUIElementActive)
            {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    //Only hide the second row of the inventory, keep the hotbar
                    for (int y = 0; y < inventory.GetLength(1); y++)
                    {
                        inventory[x, y].isUIElementActive = false;
                    }
                }
                inventoryBackground.isUIElementActive = false;
            }
        }

        public (Item, int, int) findItemInInventory(Item item) {
            Item foundItem = null;
            int indexX  = 0;
            int indexY = 0;

            if (item != null)
            {
                for (int y = 0; y < inventory.GetLength(1); y++)
                {
                    for (int x = 0; x < inventory.GetLength(0); x++)
                    {
                        if (inventory[x, y].item != null)
                        {
                            if (inventory[x, y].item.isItemIdentical(item))
                            {
                                foundItem = inventory[x, y].item;
                                indexX = x;
                                indexY = y;
                            }
                        }
                    }
                }
            }
            

            return (foundItem, indexX, indexY);
        }
    }

    public interface IGroundTraversalAlgorithm {
        public double x { get; set; }
        public double y { get; set; }

        public double targetX { get; set; }
        public double targetY { get; set; }

        public double percievedX { get; set; }
        public double percievedY { get; set; }

        public double perceptionDistance { get; set; }

        public double xDifferenceThreshold { get; set; }
        public WorldContext worldContext { get; set; }

        public double height { get; set; }

        public double notJumpThreshold { get; set; }
        public double jumpWhenWithinXRange { get; set; }

        public (int horizontal, int vertical) traverseTerrain() {

            //Update perception: 
            if (percievedX == 0 && percievedY == 0) {
                //Setup initial perception if the player isn't within the radius
                percievedX = x;
                percievedY = y;
            }
            if (Math.Pow(Math.Pow(targetX - x, 2) + Math.Pow(targetY - y, 2), 0.5) < perceptionDistance) {
                percievedX = targetX;
                percievedY = targetY;
            }


            int leftRight = 0;
            int upDown = 0;
            if (Math.Abs(x - percievedX) > xDifferenceThreshold)
            {
                if (x > percievedX)
                {
                    leftRight = 2;
                }
                else if (x < percievedX)
                {
                    leftRight = 1;
                }

                //Jump on thre conditions: 
                //Theres a hole in front of them, try to jump over it given that the player is above or approximately on the same level as them 
                //The entity is close enough to the player but isn't colliding, so jump instead
                //There's a wall in front of the entity

                int playerBlockX = (int)Math.Floor(x / worldContext.pixelsPerBlock);
                int playerBlockY = (int)Math.Floor(y / worldContext.pixelsPerBlock);

                //If there's a hole in front of them:
                if (y - percievedY > notJumpThreshold)
                {
                    if (leftRight == 2)
                    {
                        //Moving left

                        if (playerBlockX > 0 && playerBlockX < worldContext.worldArray.GetLength(0) && playerBlockY > 0 && playerBlockY + (int)Math.Round(height) < worldContext.worldArray.GetLength(1))
                        {
                            //If the block to the left and at the floor level is either transparent or air, jump

                            if (worldContext.worldArray[playerBlockX - 1, playerBlockY + (int)Math.Round(height)].isBlockTransparent || worldContext.worldArray[playerBlockX - 1, playerBlockY + (int)Math.Round(height)].ID == 0)
                            {
                                upDown = 1;
                            }
                        }
                    }
                    else if (leftRight == 1)
                    {
                        //Moving right

                        if (playerBlockX >= 0 && playerBlockX + 1 < worldContext.worldArray.GetLength(0) && playerBlockY > 0 && playerBlockY + (int)height < worldContext.worldArray.GetLength(1))
                        {
                            if (worldContext.worldArray[playerBlockX + 1, playerBlockY + (int)Math.Round(height)].isBlockTransparent || worldContext.worldArray[playerBlockX + 1, playerBlockY + (int)Math.Round(height)].ID == 0)
                            {
                                upDown = 1;
                            }

                        }
                    }
                }

                //There's a wall in front of them and they should jump over: 
                if (leftRight == 2)
                {
                    //Moving left
                    for (int y = 0; y < Math.Ceiling(height); y++)
                    {
                        //Check every block that could collide with the entity
                        if (playerBlockX > 0 && playerBlockX < worldContext.worldArray.GetLength(0) && playerBlockY > 0 && playerBlockY + (int)y < worldContext.worldArray.GetLength(1))
                        {
                            //If the block is solid, then jump
                            if (!worldContext.worldArray[playerBlockX - 1, playerBlockY + (int)y].isBlockTransparent && worldContext.worldArray[playerBlockX - 1, playerBlockY + (int)y].ID != 0)
                            {
                                upDown = 1;
                            }
                        }
                    }

                }
                else if (leftRight == 1)
                {
                    //Moving right
                    for (int y = 0; y < Math.Ceiling(height); y++)
                    {
                        if (playerBlockX >= 0 && playerBlockX + 1 < worldContext.worldArray.GetLength(0) && playerBlockY > 0 && playerBlockY + (int)y < worldContext.worldArray.GetLength(1))
                        {
                            if (!worldContext.worldArray[playerBlockX + 1, playerBlockY + (int)y].isBlockTransparent && worldContext.worldArray[playerBlockX + 1, playerBlockY + (int)y].ID != 0)
                            {
                                upDown = 1;
                            }

                        }
                    }
                }
            }
        
            return (leftRight, upDown);
        }
    }


    public interface IEntityActionListener {
        //An interface that allows a class to listen and respond to events within an entity
        public double onDamage(Object source, DamageType damageType, double damage) {
            return damage;
        }

        public void onInput(double elapsedTime) { }
        public void onEntityCollision() { }
        public void onBlockCollision() { }
    }
    #endregion

    #region Enums
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

    #endregion
    #region World Generation Algorithms
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

        public BlockGenerationVariables[,] brownianAlgorithm(BlockGenerationVariables[,] worldArray, int attemptCount, bool fillOutput)
        {
            //It would probably be more efficient to have a seperate array containing only the non-null blocks but I don't know a
            //readable way of doing it (ironic with the line break)
            int attempts = 0;

            while (attempts < attemptCount) //Runs until no changes have been made in that iteration.
            {
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

            if (fillOutput)
            {
                fill(worldArray);
            }

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
    #endregion
}
