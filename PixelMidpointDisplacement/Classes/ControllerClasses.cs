using System;
using System.IO;
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

namespace PixelMidpointDisplacement {

    /*
     * ========================================
     * 
     * Engine Controller:
     * 
     *  The engine controller is a class that instantiates and owns all controller instances within the game.
     *  
     * ========================================
    */
    public class EngineController
    {
        public LightingSystem lightingSystem;
        public PhysicsEngine physicsEngine;
        public CollisionController collisionController;
        public EntityController entityController;
        public SpriteController spriteController;
        public UIController UIController;
        public EvolutionUIController evolutionController;
        public CraftingManager craftingManager;

        public WorldContext worldContext;


        public void initialiseEngines(WorldContext wc)
        {
            worldContext = wc;
            lightingSystem = new LightingSystem(wc);
            physicsEngine = new PhysicsEngine(wc);
            collisionController = new CollisionController();
            entityController = new EntityController();
            spriteController = new SpriteController();
            UIController = new UIController();
            evolutionController = new EvolutionUIController(wc);
            craftingManager = new CraftingManager(wc);
        }



    }

    /*
     * ========================================
     * 
     * Physisc Engine
     * 
     *  The physics engine is a self contained module that contains functions modifying and resolving forces and collisions for a physics object
     * ========================================
    */
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

        public double gravity;


        public PhysicsEngine(WorldContext worldContext)
        {
            wc = worldContext;


            //Load txt file and read the values to define important variables
            loadSettings();
        }

        private void loadSettings()
        {
            StreamReader sr = new StreamReader(wc.runtimePath + "Settings\\PhysicsEngineSettings.txt");
            sr.ReadLine();
            blockSizeInMeters = Convert.ToDouble(sr.ReadLine());
            sr.ReadLine();
            gravity = Convert.ToDouble(sr.ReadLine());
            sr.Close();
        }

        public void computeImpulse(PhysicsObject entity, double timeElapsed)
        {
            for (int i = 0; i < entity.impulse.Count; i++)
            {
                entity.accelerationX += entity.impulse[i].direction.X * entity.impulse[i].magnitude;
                entity.accelerationY += entity.impulse[i].direction.Y * entity.impulse[i].magnitude;

                (Vector2 direction, double magnitude, double duration) impulseValues = entity.impulse[i];
                impulseValues.duration -= timeElapsed;
                entity.impulse[i] = impulseValues;
                if (entity.impulse[i].duration <= 0)
                {
                    entity.impulse.RemoveAt(i);

                    //Account for the loss of a list element
                    i--;
                }
            }
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

            double frictionMax = 0;

            if (Math.Sign(entity.frictionDirection) != Math.Sign(entity.accelerationX))
            {
                frictionMax = entity.cummulativeCoefficientOfFriction * gravity;
            }


            entity.accelerationX += -(directionalityX * (entity.kX * Math.Pow(entity.velocityX, 2)));
            entity.accelerationY += -(directionalityY * (entity.kY * Math.Pow(entity.velocityY, 2)));

            //Friction
            //If the acceleration is lesser than the frictional force if the object is stationary. 
            //If the entities velocity is between +-0.3, to stop jitter
            if (entity.velocityX >= -0.3 && entity.velocityX <= 0.3 && Math.Abs(entity.accelerationX) < frictionMax)
            {
                entity.accelerationX = 0;

                //If the velocity is close enough to zero, and there's a friction force acting upon it, then stop the velocity;
                entity.velocityX = 0;
            }
            else
            {
                entity.accelerationX += frictionMax * entity.frictionDirection;
            }


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

                                entity.onBlockCollision(computeCollisionNormal(entityCollider, blockRect), wc, x, y);
                                worldArray[x, y].onCollisionWithPhysicsObject(entity, this, wc);

                            }
                        }
                    }
                }
            }

        }

        public Vector2 computeCollisionNormal(Rectangle entityCollider, Rectangle blockRect)
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

                        return new Vector2(-1, 0);
                    }
                    else
                    {
                        return new Vector2(0, 1);
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

                        return new Vector2(1, 0);
                    }
                    else
                    {
                        return new Vector2(0, 1);
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
                        return new Vector2(-1, 0);
                    }
                    else
                    {
                        return new Vector2(0, -1);
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

                        return new Vector2(1, 0);
                    }
                    else
                    {
                        return new Vector2(0, -1);
                    }
                }
            }
            return Vector2.Zero;
        }

    }

    /*
     * ========================================
     * 
     * Collision controller
     * 
     *  The Collision controller stores and checks all ICollider interface objects
     *  The system has two colliders: passive and active
     *  The active colliders are ones created / owned by the player, and search all passive colliders for collisions during each update. They don't collide against any other active colliders
     * 
     * ========================================
    */
    public class CollisionController
    {
        public List<IActiveCollider> activeColliders;
        public List<IPassiveCollider> passiveColliders;

        public CollisionController()
        {
            activeColliders = new List<IActiveCollider>();
            passiveColliders = new List<IPassiveCollider>();
        }

        public void checkCollisions()
        {
            if (activeColliders.Count != 0 && passiveColliders.Count != 0)
            {

                for (int a = 0; a < activeColliders.Count; a++)
                {
                    for (int p = 0; p < passiveColliders.Count; p++)
                    {
                        if (a < activeColliders.Count && p < passiveColliders.Count)
                        {
                            if (activeColliders[a].isActive && passiveColliders[p].isActive)
                            {

                                if (activeColliders[a] is INonAxisAlignedActiveCollider n)
                                {
                                    n.calculateCollision(passiveColliders[p]);
                                }
                                else
                                {
                                    activeColliders[a].calculateCollision(passiveColliders[p]);
                                }
                            }
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

        public void removeActiveCollider(IActiveCollider collider)
        {
            if (activeColliders.Contains(collider))
            {
                activeColliders.Remove(collider);
            }
        }

        public void addPassiveCollider(IPassiveCollider collider)
        {
            if (!passiveColliders.Contains(collider))
            {
                passiveColliders.Add(collider);
            }
        }

        public void removePassiveCollider(IPassiveCollider collider)
        {
            if (passiveColliders.Contains(collider))
            {
                passiveColliders.Remove(collider);
            }
        }
    }

    /*
     * ========================================
     * 
     * Sprite Controller
     * 
     * The sprite controller contains a list of all the game engine's registered sprite sheets, which is accessed during render time through an enum "spriteSheetIDs"
     * Content is loaded during the Game class' loadContext function, and the list of Texture2Ds is passed into the sprite controller
     * ========================================
    */
    public class SpriteController
    {
        public Texture2D blockSpriteSheet;
        public Texture2D weaponSpriteSheet;
        public Texture2D blockItemSpriteSheet;
        public Texture2D playerSpriteSheet;
        public Texture2D arrowSpriteSheet;

        public List<Texture2D> spriteSheetList = new List<Texture2D>();
        //public List<Texture2D> entitySpriteSheetList = new List<Texture2D>();

        public void setSpriteSheetList(List<Texture2D> spriteSheets)
        {
            spriteSheetList = spriteSheets;

        }
    }

    /*
     * ========================================
     * Entity Controller
     * 
     *  The entity controller contains a list of all active entities and is responsible for calling their input function and any entity class specific update functions
     * 
     * ========================================
    */
    public class EntityController
    {
        public List<Entity> entities = new List<Entity>();

        public void entityInputUpdate(double elapsedTime)
        {
            for (int i = 0; i < entities.Count; i++)
            {
                entities[i].inputUpdate(elapsedTime);
            }
        }

        public void addEntity(Entity entity)
        {
            if (!entities.Contains(entity)) { entities.Add(entity); }
        }
        public void removeEntity(Entity entity)
        {
            if (entities.Contains(entity)) { entities.Remove(entity); }
        }

    }

    /*
     * ========================================
     * 
     * UI Controller
     * 
     *  The UI controller is responsible for keeping an ordered track of all UIelements, active and inactive.
     *  The class contains two lists: UIElements and InteractiveUI.
     *  UiElements are the base ui elements, and each one is rendered and given an 'update' call during run-time. UIElements is a tuple of render layer (for sorting) and the UIElement
     *  InteractiveUI are also UIElements, but have an additional function call if they are clicked on.
     * ========================================
    */
    public class UIController
    {
        public List<(int drawOrder, UIElement uiElement)> UIElements = new List<(int drawOrder, UIElement uiElement)>();
        public List<InteractiveUIElement> InteractiveUI = new List<InteractiveUIElement>();
        public List<UILine> UILines = new List<UILine>();
        public List<UIElement> inventoryBackgrounds = new List<UIElement>();
        public List<UIItem> inventorySlots = new List<UIItem>();

        public bool wasElementAdded = false;
        public UIController()
        {
            resetMainMenuUI();
        }
        private void resetMainMenuUI()
        {

            UIElements.Clear();
            InteractiveUI.Clear();
            MainMenuTitle title = new MainMenuTitle();
            MainMenuWorldGenText generationText = new MainMenuWorldGenText();
            MainMenuStartButton start = new MainMenuStartButton(generationText);
            UIElements.Add((0, title));
            UIElements.Add((0, start));
            UIElements.Add((0, generationText));
            InteractiveUI.Add(start);
        }

        public void addUIElement(int drawOrder, UIElement element)
        {
            if (!UIElements.Contains((drawOrder, element)))
            {
                UIElements.Add((drawOrder, element));
            }
            if (element is InteractiveUIElement iue)
            {
                if (!InteractiveUI.Contains(iue))
                {
                    InteractiveUI.Add(iue);
                }
            }
            wasElementAdded = true;
        }

        public void removeUIElement(int drawOrder, UIElement element)
        {

            if (UIElements.Contains((drawOrder, element)))
            {
                UIElements.Remove((drawOrder, element));
            }
            if (element is InteractiveUIElement iue)
            {
                if (InteractiveUI.Contains(iue))
                {
                    InteractiveUI.Remove(iue);
                }

            }
        }
    }

    /*
     * ========================================
     * Lighting System
     * 
     *  The lighting system is half depricated, as it contains some pre-shader functionality.
     *  The lighting system is responsible for containing the variables for shader rendering as well as the list of active lightsto be computed.
     * ========================================
    */
    public class LightingSystem
    {
        public int[,] lightArray { get; set; }
        WorldContext wc;
        Vector2 lightDirection = new Vector2(0.9f, 1);
        int sunBrightness = 1024;
        int shadowBrightness = 200;
        int darkestLight = 0;

        public double shaderPrecision = 0.5;
        public GraphicsDeviceManager graphics;

        double scalar = 0.8;
        double emmissiveScalar = 0.5;

        bool accummulateLight = true;

        public List<IEmissive> lights = new List<IEmissive>();
        public List<IEmissiveBlock> emissiveBlocks = new List<IEmissiveBlock>();

        public LightingSystem(WorldContext worldContext)
        {
            wc = worldContext;

            //Load settings from file
            loadSettings();
        }

        private void loadSettings()
        {
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
                    calculateLightRay(worldArray.GetLength(1) - 1, startingY, worldArray, surfaceLevel);
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
                            if (worldArray[xCheck, y] == 0 || worldArray[x, yCheck] == 0 || worldArray[xCheck, y] == (int)blockIDs.bush || worldArray[x, yCheck] == (int)blockIDs.bush || worldArray[xCheck, y] == (int)blockIDs.bigBush || worldArray[x, yCheck] == (int)blockIDs.bigBush)
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

        public int[,] initialiseLight((int width, int height) worldDimensions, int[] surfaceLevel)
        {
            lightArray = new int[worldDimensions.width, worldDimensions.height];
            for (int x = 0; x < lightArray.GetLength(0); x++)
            {
                for (int y = 0; y < lightArray.GetLength(1); y++)
                {
                    lightArray[x, y] = darkestLight;
                }
            }

            for (int x = 0; x < lightArray.GetLength(0); x++)
            {
                for (int y = 0; y < surfaceLevel[x]; y++)
                {
                    lightArray[x, y] = shadowBrightness;
                }
            }

            return lightArray;
        }

        public void calculateSurfaceLight(int[,] worldArray, List<(int x, int y)> surfaceLevel)
        {
            //From i = P/4 * Pi * r^2
            //r = Sqrt(P/0.9 * 4 * PI)

            int maxDepthSunlight = (int)Math.Sqrt(sunBrightness / 25 * 4 * Math.PI);

            for (int i = 0; i < surfaceLevel.Count; i++)
            {
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
                        else
                        {
                            lightLevel = surfaceBrightness;
                        }
                        int changedX = (int)Math.Round(surfaceLevel[i].x + lightDirection.X * j);
                        int changedY = (int)Math.Round(surfaceLevel[i].y + lightDirection.Y * j);
                        if (changedX >= 0 && changedY >= 0 && changedX < lightArray.GetLength(0) && changedY < lightArray.GetLength(1))
                        {
                            if (worldArray[changedX, changedY] != 0 && lightArray[changedX, changedY] < lightLevel / scalar)
                            {
                                lightArray[changedX, changedY] = (int)(lightLevel / scalar);
                            }
                        }

                    }

                }
            }


        }

        public int[,] calculateLightMap(int emmissiveness)
        {
            int maxImpact = (int)(Math.Sqrt(emmissiveness / 25 * 4 * Math.PI) / emmissiveScalar);

            int[,] lightMap = new int[maxImpact, maxImpact]; //I think I can technically shorten this to being a singular array only the width of the max impact and just 'rotate' it around to account for it's sphereical influence. However this sounds horrid so I won't
            for (int x = 0; x < maxImpact; x++)
            {
                for (int y = 0; y < maxImpact; y++)
                {
                    lightMap[x, y] = 0;
                }
            }
            for (int x = 0; x < maxImpact; x++)
            {
                for (int y = 0; y < maxImpact; y++)
                {
                    int distance = (int)Math.Sqrt(Math.Pow(x - maxImpact / 2, 2) + Math.Pow(y - maxImpact / 2, 2));
                    if (distance <= maxImpact)
                    {
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
            else if (xChange != 0 && xChange < 0)
            {
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
            lightArray = add2DArray(newLightMap, lightArray, lightX - (int)Math.Floor(lightMap.GetLength(0) / 2.0) - subtractAtX, lightY - (int)Math.Floor(lightMap.GetLength(1) / 2.0) - subtractAtY, 1, emmissiveMax);

        }

        private int[,] add2DArray(int[,] sourceArray, int[,] arrayToBeAddedTo, int xOffset, int yOffset, int valueMultiplier)
        {
            for (int x = 0; x < sourceArray.GetLength(0); x++)
            {
                for (int y = 0; y < sourceArray.GetLength(1); y++)
                {
                    if (x + xOffset >= 0 && x + xOffset < arrayToBeAddedTo.GetLength(0) && y + yOffset >= 0 && y + yOffset < arrayToBeAddedTo.GetLength(1))
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
                        if (arrayToBeAddedTo[x + xOffset, y + yOffset] + valueMultiplier * sourceArray[x, y] > maxLightValue && accummulateLight)
                        {
                            sourceArray[x, y] = (maxLightValue - arrayToBeAddedTo[x + xOffset, y + yOffset]) / valueMultiplier;
                            if (sourceArray[x, y] < 0)
                            {
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

    /*
     * ========================================
     * Animation Controller
     * 
     *  A class that contains all of the current animator classes (both sprite and item animators)
     *  
     *  The animation controller is responsible for ticking all animation objects
     * ========================================
    */
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

        public void addSpriteAnimator(SpriteAnimator animator)
        {
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
            for (int i = 0; i < spriteAnimators.Count; i++)
            {
                spriteAnimators[i].tickAnimation(elapsedTime);
            }
        }

    }

    /*
    * ========================================
    * 
    * Evolution UI Controller
    * 
    *  A class that controls the displaying of the players evolution tree during the 'evolution' stage of the game
    * 
    * ========================================
    */
    public class EvolutionUIController
    {
        WorldContext worldContext;
        EvolutionTree tree;

        List<(ExperienceField field, ExperienceCounter counter)> experienceCounters = new List<(ExperienceField field, ExperienceCounter counter)>();
        public EvolutionUIController(WorldContext wc)
        {
            worldContext = wc;
            EvolutionStarBackground eb1 = new EvolutionStarBackground();
            eb1.mouseMovementCoefficient = 0.01;
            wc.engineController.UIController.addUIElement(1, eb1);

            EvolutionStarBackground eb2 = new EvolutionStarBackground();
            eb2.mouseMovementCoefficient = 0.005;
            eb2.setSourceLocation(0, 1080);
            wc.engineController.UIController.addUIElement(2, eb2);

            EndEvolutionButton eeb = new EndEvolutionButton();
            wc.engineController.UIController.addUIElement(18, eeb);

            /*EvolutionStarBackground eb3 = new EvolutionStarBackground();
            eb3.mouseMovementCoefficient = 0;
            eb3.setSourceLocation(1920, 0);
            eb3.zeroStartingOffset();
            wc.engineController.UIController.addUIElement((0, eb3));*/

            //Add a counter for each experienceField
            experienceCounters.Add((ExperienceField.Knowledge, new ExperienceCounter(worldContext, 50, 400)));
            experienceCounters.Add((ExperienceField.Durability, new ExperienceCounter(worldContext, 50, 450)));
            experienceCounters.Add((ExperienceField.Maneuverability, new ExperienceCounter(worldContext, 50, 500)));
            experienceCounters.Add((ExperienceField.Damage, new ExperienceCounter(worldContext, 50, 550)));


        }

        public void setTree(EvolutionTree tree)
        {
            this.tree = tree;
        }

        public void setupPlayerEvolutionUI(EvolutionTree e)
        {
            List<EvolutionButton> previousLayerButtons = new List<EvolutionButton>();

            for (int x = 0; x < e.evolutionTree.Count; x++)
            {
                List<EvolutionButton> currentLayerButtons = new List<EvolutionButton>();

                for (int y = 0; y < e.evolutionTree[x].evolutionLayer.Count; y++)
                {
                    Evolution ev = e.evolutionTree[x].evolutionLayer[y].evolution;

                    EvolutionButton evolutionButton = new EvolutionButton(ev);
                    currentLayerButtons.Add(evolutionButton);
                    evolutionButton.setLocationFromTree();
                    worldContext.engineController.UIController.addUIElement(15, evolutionButton);

                    if (x > 0)
                    {
                        List<int> dependencies = e.evolutionTree[x].evolutionLayer[y].prerequisiteEvolutions;
                        for (int i = 0; i < dependencies.Count; i++)
                        {
                            EvolutionDependencyLine el = new EvolutionDependencyLine(evolutionButton, previousLayerButtons[dependencies[i]], Vector2.Zero, Vector2.Zero);
                            worldContext.engineController.UIController.UILines.Add(el);
                        }
                    }
                }
                previousLayerButtons = currentLayerButtons;
            }
        }
        //There was A error here...
        //The evolution tree is null!
        public void updateExperienceCounters()
        {
            for (int i = 0; i < experienceCounters.Count; i++)
            {

                experienceCounters[i].counter.updateString(((int)tree.getExperience(experienceCounters[i].field)).ToString());
            }
        }
    }

    /*
     * ========================================
     * 
     * Player UI Controller
     * 
     *  The only 'controller' not owned by the engine controller.
     *  The Player UI Controller is owned by the player as it is just an extraction of some UI elements.
     *  
     *  The Hotbar is considered UI and not an inventory element as it does not contain any functionality, which is still entirely contained within the Inventory element of the player
     * ========================================
    */
    public class PlayerUIController {
        public Player owner;
        public WorldContext wc;
        public Hotbar hotbar = new Hotbar();
        public HotbarSelected hotbarSelected = new HotbarSelected();

        HealthBar healthBar = new HealthBar();

        RespawnScreen rs;
        RespawnButton rb;

        public PlayerUIController(Player player) {
            this.owner = player;
            wc = owner.worldContext;

            HealthBarOutline hbo = new HealthBarOutline();
            wc.engineController.UIController.addUIElement(5, hbo);
            wc.engineController.UIController.addUIElement(5, healthBar);

            rs = new RespawnScreen();
            wc.engineController.UIController.addUIElement(150, rs);
            rb = new RespawnButton(owner, rs);
            wc.engineController.UIController.addUIElement(150, rb);

            wc.engineController.UIController.addUIElement(3, hotbar);
            wc.engineController.UIController.inventoryBackgrounds.Add(hotbar);
            wc.engineController.UIController.addUIElement(4, hotbarSelected);

        }

        public void damageTaken() {
            healthBar.drawRectangle.Width = (int)((owner.currentHealth / (double)owner.maxHealth) * healthBar.maxHealthDrawWidth);

            if (owner.currentHealth <= 0)
            {
                rb.isUIElementActive = true;
                rs.isUIElementActive = true;
            }
        }

        public void swapHotbar() {
            hotbarSelected.swapItem(owner.mainHandIndex);
        }
    }

    

}