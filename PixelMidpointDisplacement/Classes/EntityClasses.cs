using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.IO;

using System.Collections.Generic;

using System.Linq;

using Vector2 = Microsoft.Xna.Framework.Vector2;
using Vector3 = Microsoft.Xna.Framework.Vector3;

namespace PixelMidpointDisplacement {
    public class PhysicsObject : DrawnClass
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

        public void recalculateCollider()
        {
            collider = new Rectangle(0, 0, (int)(width * worldContext.pixelsPerBlock), (int)(height * worldContext.pixelsPerBlock));
        }

        public virtual void hasCollided() { }
    }
    public class Entity : PhysicsObject
    {
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

        public int entityDirection = 1;

        public bool collideWithPlatforms = true;
        private bool isCollidingWithPlatforms = false;


        public Entity(WorldContext wc) : base(wc)
        {
            worldContext.physicsObjects.Add(this);
        }

        public virtual void inputUpdate(double elapsedTime)
        {
            if (!isCollidingWithPlatforms)
            {
                collideWithPlatforms = true;
            }
            for (int i = 0; i < inputListeners.Count; i++)
            {
                inputListeners[i].onInput(elapsedTime);
            }
            isCollidingWithPlatforms = false;
        }

        public override void onBlockCollision(Vector2 collisionNormal, WorldContext worldContext, int blockX, int blockY)
        {
            for (int i = 0; i < blockCollisionListeners.Count; i++)
            {
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

            if (worldContext.worldArray[blockX, blockY] is WoodenPlatformBlock wpb) {
                isCollidingWithPlatforms = true;
            }

        }
        public virtual void applyDamage(object attacker, DamageType damageType, double damage)
        {
            for (int i = 0; i < damageListeners.Count; i++)
            {
                damage = damageListeners[i].onDamage(attacker, damageType, damage);
            }
            currentHealth -= damage;
            //Create a damage uielement
            string integerDamageAsAString = ((int)damage).ToString();


            for (int i = 0; i < integerDamageAsAString.Length; i++)
            {
                Damage d = new Damage(worldContext, Convert.ToInt32(integerDamageAsAString[i].ToString()), x + 11 * i, y, 15);
                worldContext.engineController.UIController.addUIElement(15, d);
            }

            if (currentHealth <= 0)
            {
                onDeath(attacker, damageType, damage);
            }
        }


        public void walkRight() {
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

            directionalEffect = SpriteEffects.None;
            entityDirection = 1;
        }

        public void walkLeft() {

            if (velocityX > -maxMovementVelocityX)
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
            directionalEffect = SpriteEffects.FlipHorizontally;
            entityDirection = -1;
        }

        public void jump(double elapsedTime) {
            if (isOnGround)
            {
                accelerationY += baseJumpAcceleration / elapsedTime;
            }
        }

        public void drop() {
            collideWithPlatforms = false;
        }

        //Currently depricated
        public virtual void applyEffect() { }

        public virtual void onDeath(object attacker, DamageType damageType, double damageThatKilled)
        {

        }



        public virtual Entity copyEntity()
        {
            return new Entity(worldContext);
        }
    }
    public class SpawnableEntity : Entity
    {

        public Biome homeBiome { get; set; }
        public int spawnableEntityListIndex { get; set; }

        public double screenLengthsUntilDespawn = 1.5;
        public SpawnableEntity(WorldContext worldContext) : base(worldContext) { }

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
            if (x < -worldContext.screenSpaceOffset.x - screenLengthsUntilDespawn * worldContext.applicationWidth || x > -worldContext.screenSpaceOffset.x + screenLengthsUntilDespawn * worldContext.applicationWidth || y < -worldContext.screenSpaceOffset.y - screenLengthsUntilDespawn * worldContext.applicationHeight || y > -worldContext.screenSpaceOffset.y + screenLengthsUntilDespawn * worldContext.applicationHeight)
            {
                despawn();

            }

        }
    }
    public class EvilClone : SpawnableEntity, IGroundTraversalAlgorithm, IPassiveCollider
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
        public EvilClone(WorldContext wc, Player target) : base(wc)
        {
            player = target;

            notJumpThreshold = -100;
            perceptionDistance = 1000;
            xDifferenceThreshold = 10;
            drawWidth = 1.5f;
            drawHeight = 3;
            entityDirection = 1;

            maxMovementVelocityX = 7;

            maxInvincibilityCooldown = 0.2;
            kX = 0.02;
            kY = 0.02;

            defaultkX = 0.02;
            defaultkY = 0.02;
            minVelocityX = 0.5;
            minVelocityY = 0.01;

            width = 0.8;
            height = 2.7;

            spriteSheetID = (int)spriteSheetIDs.player;

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
            if (attackCooldown > 0)
            {
                attackCooldown -= elapsedTime;
            }
            if (knockbackStunDuration > 0)
            {
                knockbackStunDuration -= elapsedTime;
            }


            if (leftRight == 1)
            {
                walkRight();

                if (!spriteAnimator.isAnimationActive)
                {
                    spriteAnimator.startAnimation(0.5, "walk");
                }
                entityDirection = 1;

            }
            if (leftRight == 2)
            {
                walkLeft();
                if (!spriteAnimator.isAnimationActive)
                {
                    spriteAnimator.startAnimation(0.5, "walk");
                }
                entityDirection = -1;
            }
            if (leftRight == 0)
            {
                spriteAnimator.isAnimationActive = false; //If the entity isn't walking, stop the animation
            }
            if (upDown == 1)
            {
                jump(elapsedTime);
            }
            else if (upDown == -1) {
                drop();
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
            for (int i = 0; i < loot.Count; i++)
            {
                new DroppedItem(worldContext, loot[i], (x, y), new Vector2((float)r.NextDouble() * 4f, (float)r.NextDouble() * 4f));
            }

            despawn();
        }

        public override Entity copyEntity()
        {
            return new EvilClone(worldContext, player);
        }

        public void onCollision(ICollider externalCollider)
        {
            if (externalCollider is Player p)
            {
                p.velocityX = 7 * entityDirection;
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

        public Entity owner { get; set; }
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
        public bool hasSpawnedAnNPC = false;
        public int entityDirection { get; set; }


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

        public string playerName = "John";

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


            spriteSheetID = (int)spriteSheetIDs.player;


            spriteAnimator = new SpriteAnimator(animationController: worldContext.animationController, constantOffset: new Vector2(12f, 8f), frameOffset: new Vector2(32, 65), sourceDimensions: new Vector2(32, 64), animationlessSourceRect: new Rectangle(160, 0, (int)32, (int)64), owner: this);


            spriteAnimator.animationDictionary = new Dictionary<string, (int frameCount, int yOffset)> {

                { "walk", (6, 0) }

            };

            wc.engineController.entityController.addEntity(this);

            craftingDictionary = new PlayerCraftingDictionary(worldContext.engineController.craftingManager);

            initialiseStatGainPerExperience();

            showInventory();
            hideInventory();
        }

        private void loadSettings()
        {
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

        public void initialiseEvolutionTree()
        {
            evolutionTree = new PlayerEvolutionTree(this);
        }

        public void initialiseStatGainPerExperience()
        {
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

            for (int x = 0; x < inventory.GetLength(0); x++)
            {
                for (int y = 0; y < inventory.GetLength(1); y++)
                {
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

        public void showInventory()
        {
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
                worldContext.engineController.housingController.inventoryWasOpened();

            }


        }
        public void hideInventory()
        {
            if (inventory[0, 1].isUIElementActive)
            {
                for (int x = 0; x < inventory.GetLength(0); x++)
                {
                    //Only hide the second row of the inventory, keep the hotbar
                    for (int y = 1; y < inventory.GetLength(1); y++)
                    {
                        inventory[x, y].isUIElementActive = false;
                    }
                }
                inventoryBackground.isUIElementActive = false;

                for (int x = 0; x < equipmentInventory.GetLength(0); x++)
                {
                    for (int y = 0; y < equipmentInventory.GetLength(1); y++)
                    {
                        equipmentInventory[x, y].isUIElementActive = false;
                    }
                }
                equipmentBackground.isUIElementActive = false;
                selectedItem.dropItem();

                worldContext.engineController.craftingManager.inventoryWasClosed();
                worldContext.engineController.housingController.inventoryWasClosed();


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

        public override void inputUpdate(double elapsedTime)
        {
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
                            walkRight();

                            if (!spriteAnimator.isAnimationActive)
                            {
                                spriteAnimator.startAnimation(0.5, "walk");
                            }
                            entityDirection = 1;

                        }
                        if (Keyboard.GetState().IsKeyDown(Keys.A))
                        {
                            walkLeft();


                            if (!spriteAnimator.isAnimationActive)
                            {
                                spriteAnimator.startAnimation(0.5, "walk");
                            }
                            entityDirection = -1;
                        }

                        if (Keyboard.GetState().IsKeyDown(Keys.S)) {
                            drop();
                        }
                    }
                    else
                    {
                        knockbackStunDuration -= elapsedTime;
                    }
                    if (!Keyboard.GetState().IsKeyDown(Keys.A) && !Keyboard.GetState().IsKeyDown(Keys.D))
                    {
                        spriteAnimator.isAnimationActive = false; //If the player isn't walking, stop the animation
                    }
                    if (Keyboard.GetState().IsKeyDown(Keys.W) || Keyboard.GetState().IsKeyDown(Keys.Space))
                    {
                        jump(elapsedTime);
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
                        for (int y = 0; y < equipmentInventory.GetLength(1); y++)
                        {
                            if (equipmentInventory[x, y].item is EquipableItem e)
                            {
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
                    if (Keyboard.GetState().IsKeyDown(Keys.J) && !hasSpawnedAnNPC)
                    {
                        hasSpawnedAnNPC = true;
                        openInventoryCooldown = maxOpenInventoryCooldown;
                        HandyNPC testEntity = new HandyNPC(worldContext);
                        testEntity.x = Mouse.GetState().X - worldContext.screenSpaceOffset.x;
                        testEntity.y = Mouse.GetState().Y - worldContext.screenSpaceOffset.y;
                        worldContext.engineController.npcController.unlockedAnNPC(testEntity);
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
                            if (entityDirection == -1)
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
                    else if (invincibilityCooldown > 0)
                    {
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

        public void increaseVariablesBasedOnExperience()
        {
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
        public void setSpawn(int x, int y)
        {
            initialX = x;
            initialY = y;
        }

        public void respawn()
        {
            x = initialX;
            y = initialY;
            velocityX = 0;
            velocityY = 0;
            currentHealth = maxHealth;
            playerUI.damageTaken(null);
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

                playerUI.damageTaken(attacker);
            }
        }

        public override void onDeath(object attacker, DamageType damageType, double damageThatKilled)
        {
            Entity entityAttacker = null;
            if (attacker != null)
            {
                if (attacker is Entity e)
                {
                    entityAttacker = e;
                }
            }
            if (damageType == DamageType.Falldamage)
            {
                evolutionTree.addExperience(ExperienceField.Durability, 10 * experienceMultiplier);
            }
            if (entityAttacker != null)
            {
                //Later on, check if the attacker isn't a boss
                if (entityAttacker.maxHealth > maxHealth || entityAttacker.currentHealth > 0.3 * entityAttacker.maxHealth)
                {
                    evolutionTree.addExperience(ExperienceField.Durability, 10 * experienceMultiplier);
                    evolutionTree.addExperience(ExperienceField.Damage, 10 * experienceMultiplier);

                }
            }

            evolutionTree.addExperience(ExperienceField.Maneuverability, 8 * experienceMultiplier);
            evolutionTree.addExperience(ExperienceField.Knowledge, 10 * experienceMultiplier);


            evolutionTree.recalculateAvailableEvolutions();
            base.onDeath(attacker, damageType, damageThatKilled);
        }

        public override void updateLocation(double xChange, double yChange)
        {
            int xBlockChange = (int)(Math.Floor((x + xChange) / worldContext.pixelsPerBlock) - Math.Floor(x / worldContext.pixelsPerBlock));
            int yBlockChange = (int)(Math.Floor((y + yChange) / worldContext.pixelsPerBlock) - Math.Floor(y / worldContext.pixelsPerBlock));



            if (xBlockChange >= 1 || xBlockChange <= -1 || yBlockChange >= 1 || yBlockChange <= -1)
            {
                worldContext.engineController.lightingSystem.movedLight((int)Math.Floor(((x) / worldContext.pixelsPerBlock)) + collider.Width / (2 * worldContext.pixelsPerBlock), (int)Math.Floor((y) / worldContext.pixelsPerBlock), xBlockChange, yBlockChange, lightMap, emmissiveMax);
            }

            base.updateLocation(xChange, yChange);
        }

    }

    public class Arrow : Entity, IEmissive, IActiveCollider
    {
        public Vector3 lightColor { get; set; }
        public float luminosity { get; set; }
        public float range { get; set; }
        public Entity owner { get; set; }
        public bool isActive { get; set; }

        public double weaponDamage = 10;

        public double initialVelocity;

        public double invincibilityCooldown { get; set; }
        public double maxInvincibilityCooldown { get; set; }
        public RenderTarget2D shadowMap { get; set; }
        public RenderTarget2D lightMap { get; set; }
        public Arrow(WorldContext wc, (double x, double y) arrowLocation, double initialVelocity, Entity shooter) : base(wc)
        {
            spriteSheetID = (int)spriteSheetIDs.arrow;
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

        public void onCollision(ICollider externalCollider)
        {
            if (externalCollider is Entity e)
            {
                e.velocityX = velocityX / 2;
                e.velocityY += 7;
                //Have to move the entity up, because of the slight overlap with the lower block, it causes a collision to detect and counteract the velocity?
                e.y -= 12;
                e.applyDamage(owner, DamageType.EntityAttack, owner.entityDamageMultiplier * weaponDamage * (Math.Pow(Math.Pow(velocityX, 2) + Math.Pow(velocityY, 2), 0.5) / initialVelocity));
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

    public class DroppedItem : Entity
    {
        public Item item { get; set; }
        float pickupAcceleration = 50f;

        public double pickupDelay;
        double pickupMoveDistance = 96f;
        double pickupDistance = 48f;

        public DroppedItem(WorldContext wc, Item item, (double x, double y) location, Vector2 initialVelocity) : base(wc)
        {
            //Set the texture from the item's spritesheet

            spriteSheetID = item.spriteSheetID;
            spriteAnimator = new SpriteAnimator(wc.animationController, Vector2.Zero, Vector2.Zero, new Vector2(item.sourceRectangle.Width, item.sourceRectangle.Height), item.sourceRectangle, this);
            drawWidth = item.drawRectangle.Width / (double)wc.pixelsPerBlock;
            drawHeight = item.drawRectangle.Height / (double)wc.pixelsPerBlock;
            width = drawWidth;
            height = drawHeight;

            this.item = item;

            collider = new Rectangle(0, 0, (int)(width * wc.pixelsPerBlock), (int)(height * wc.pixelsPerBlock));

            kX = 5;
            kY = 0.01;

            minVelocityX = 0.25;
            minVelocityY = 0.01;

            velocityX = initialVelocity.X;
            velocityY = initialVelocity.Y;

            x = location.x;
            y = location.y;

            wc.engineController.entityController.addEntity(this);
        }

        public override void onBlockCollision(Vector2 collisionNormal, WorldContext worldContext, int blockX, int blockY)
        {
            //Do nothing, don't take fall damage or anything of the sorts
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

                    accelerationX = pickupAcceleration * (((worldContext.player.x + worldContext.player.width * worldContext.pixelsPerBlock / 2.0) - (x + drawWidth * worldContext.pixelsPerBlock / 2.0f)) / distance);
                    accelerationY = -pickupAcceleration * (((worldContext.player.y + worldContext.player.height * worldContext.pixelsPerBlock / 2.0) - (y + drawHeight * worldContext.pixelsPerBlock / 2.0f)) / distance);

                }
                if (distance < pickupDistance)
                {
                    //Pickup action
                    //Now I just need to make an inventory system and sorting

                    if (worldContext.player.addItemToInventory(item))
                    {
                        item.onPickup(worldContext.player);
                        worldContext.engineController.entityController.removeEntity(this);
                    }
                }
            }
        }
    }
}