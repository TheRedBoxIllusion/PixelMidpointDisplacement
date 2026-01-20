using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace PixelMidpointDisplacement {
    public class CraftingManager
    {
        //The manager contains the player's base dictionary, and when opening an inventory it searches all nearby blocks for blocks with crafting dictionaries and adds them to a list if that type is not already present

        //The manager also controls the visibility of the crafting items in a set range of options that scroll.
        public WorldContext worldContext;

        const int numberOfVisibleRecipes = 4;

        const int pixelsBetweenElements = 5;

        List<CraftItemButton> craftableRecipes = new List<CraftItemButton>();

        public int scrollValueWhenInventoryWasOpened = 0;

        public int x = 10;
        public int y = 500;

        public bool showCraftingSystem = false;

        List<CraftingDictionary> dictionaries = new List<CraftingDictionary>();
        public CraftingManager(WorldContext worldContext)
        {
            this.worldContext = worldContext;
        }

        public void inventoryWasOpened()
        {
            scrollValueWhenInventoryWasOpened = Mouse.GetState().ScrollWheelValue / 120;
            showCraftingSystem = true;
            //Find all of the nearby crafting dictionaries:

            dictionaries.Add(worldContext.player.craftingDictionary);

        }

        public void inventoryWasClosed()
        {
            showCraftingSystem = false;
            dictionaries.Clear();
        }
        public void managerUpdate()
        {
            if (showCraftingSystem)
            {
                //Reset and update them all
                for (int i = 0; i < dictionaries.Count; i++)
                {
                    dictionaries[i].resetCraftingRecipes();
                    dictionaries[i].updateCraftingRecipes(worldContext.player);
                }

                int indexValue = (Mouse.GetState().ScrollWheelValue / 120) - scrollValueWhenInventoryWasOpened;

                for (int i = 0; i < craftableRecipes.Count; i++)
                {
                    craftableRecipes[i].isUIElementActive = false;
                    craftableRecipes[i].background.isUIElementActive = false;
                }

                for (int i = indexValue; i < craftableRecipes.Count && i < indexValue + numberOfVisibleRecipes; i++)
                {
                    if (i >= 0)
                    {
                        craftableRecipes[i].drawRectangle.Y = (int)(((craftableRecipes[i].background.drawRectangle.Height + pixelsBetweenElements) * (i - indexValue)) + y + craftableRecipes[i].y);
                        craftableRecipes[i].drawRectangle.X = x + craftableRecipes[i].x;

                        craftableRecipes[i].isUIElementActive = true;
                        craftableRecipes[i].background.isUIElementActive = true;
                    }
                }

                craftableRecipes.Clear();
            }
        }

        public void addRecipeButton(CraftItemButton button)
        {
            if (!craftableRecipes.Contains(button))
            {
                craftableRecipes.Add(button);
            }
        }

        public void reduceItemQuantity(Item item, int quantity)
        {
            IInventory inventory = worldContext.player;


            Item itemInInventory = null;
            int indexX;
            int indexY;

            (itemInInventory, indexX, indexY) = inventory.findItemInInventory(item);


            while (quantity > 0 && itemInInventory != null)
            {
                (itemInInventory, indexX, indexY) = inventory.findItemInInventory(item);

                if (itemInInventory != null)
                {
                    if (quantity <= itemInInventory.currentStackSize)
                    {
                        itemInInventory.currentStackSize -= quantity;
                        quantity = 0;
                    }
                    else
                    {
                        quantity -= itemInInventory.currentStackSize;
                        itemInInventory.currentStackSize = 0;
                    }

                    if (itemInInventory.currentStackSize <= 0)
                    {
                        inventory.inventory[indexX, indexY].setItem(null);
                    }
                }
            }
        }
    }
    public class CraftingDictionary
    {
        /*
         A dictionary of craftable options that can be parented in different child classes for crafting stations

         The dictionary is a controller of crafting recipes, passing in an inventory, it enables UI elements th
         at pertain to a crafting recipe
        
         Contains a list of:
         InteractiveUI crafting button,
         List of (Item, recipe quantity, inventory quantity) recipe materials
         */


        public CraftingManager manager;
        public List<CraftingRecipe> craftingRecipes = new List<CraftingRecipe>();
        public CraftingDictionary(CraftingManager manager)
        {
            this.manager = manager;
        }
        public void updateCraftingRecipes(IInventory inventory)
        {
            //For each item within the inventory, loop through the crafting recipe list and add the ingredient quantity to any relevant recipe
            for (int y = 0; y < inventory.inventory.GetLength(1); y++)
            {
                for (int x = 0; x < inventory.inventory.GetLength(0); x++)
                {
                    if (inventory.inventory[x, y].item != null)
                    {
                        for (int r = 0; r < craftingRecipes.Count; r++)
                        {
                            for (int i = 0; i < craftingRecipes[r].ingredientList.Count; i++)
                            {
                                if (craftingRecipes[r].ingredientList[i].ingredient.isItemIdentical(inventory.inventory[x, y].item))
                                {
                                    craftingRecipes[r].ingredientList[i].addInventoryQuantity(inventory.inventory[x, y].item.currentStackSize);
                                }
                            }

                        }
                    }
                }
            }

            //Then, check each crafting recipe and see if it is craftable given the items in the inventory
            for (int i = 0; i < craftingRecipes.Count; i++)
            {
                craftingRecipes[i].canRecipeBeCrafted();
            }
            //This could almost definitely be more optimized, but for now this seems logical
        }

        public void resetCraftingRecipes()
        {
            for (int i = 0; i < craftingRecipes.Count; i++)
            {
                for (int y = 0; y < craftingRecipes[i].ingredientList.Count; y++)
                {
                    craftingRecipes[i].ingredientList[y].setInventoryQuantity(0);
                }
            }
        }

    }
    public class CraftingRecipe
    {
        public CraftingManager manager;
        public Item recipeOutput;
        public CraftItemButton craftButton;
        public List<RecipeIngredient> ingredientList;

        public bool canBeCrafted = false;

        public CraftingRecipe(Item recipeResult, int resultQuantity, List<RecipeIngredient> ingredientList, CraftingManager manager)
        {
            recipeOutput = recipeResult;
            if (resultQuantity <= recipeOutput.maxStackSize)
            {
                recipeOutput.currentStackSize = resultQuantity;
            }
            else
            {
                recipeOutput.currentStackSize = recipeOutput.maxStackSize;
            }
            this.ingredientList = ingredientList;
            this.manager = manager;

            craftButton = new CraftItemButton(this);
        }

        public void canRecipeBeCrafted()
        {
            canBeCrafted = true;

            for (int i = 0; i < ingredientList.Count; i++)
            {
                if (!ingredientList[i].doesRecipeHaveEnough())
                {
                    canBeCrafted = false;
                }
            }

            if (canBeCrafted)
            {
                manager.addRecipeButton(craftButton);
            }
            craftButton.isUIElementActive = false;
        }

        public void itemWasCrafted()
        {
            //Put the logic in here for reducing the ingredients within the users inventory
            for (int i = 0; i < ingredientList.Count; i++)
            {
                manager.reduceItemQuantity(ingredientList[i].ingredient, ingredientList[i].requiredQuantity);
            }
        }
    }
    public class RecipeIngredient
    {
        public Item ingredient;
        public int requiredQuantity;
        public int inventoryQuantity;

        public RecipeIngredient(Item ingredient, int requiredQuantity)
        {
            this.ingredient = ingredient;
            this.requiredQuantity = requiredQuantity;
        }

        public void setInventoryQuantity(int inventoryQuantity)
        {
            this.inventoryQuantity = inventoryQuantity;
        }

        public void addInventoryQuantity(int newQuantity)
        {
            inventoryQuantity += newQuantity;
        }

        public bool doesRecipeHaveEnough()
        {
            return inventoryQuantity >= requiredQuantity;
        }

    }

    public class PlayerCraftingDictionary : CraftingDictionary
    {
        public PlayerCraftingDictionary(CraftingManager manager) : base(manager)
        {
            CraftingRecipe ironIngot = new CraftingRecipe(new IngotItem((int)oreIDs.iron), 1, new List<RecipeIngredient>() { new RecipeIngredient(new OreItem((int)oreIDs.iron), 3) }, manager);

            craftingRecipes.Add(ironIngot);

            CraftingRecipe swordRecipe = new CraftingRecipe(new Weapon(), 1, new List<RecipeIngredient>() { new RecipeIngredient(new IngotItem((int)oreIDs.iron), 10) }, manager);
            craftingRecipes.Add(swordRecipe);

            CraftingRecipe woodenBackgroundRecipe = new CraftingRecipe(recipeResult: new BackgroundBlockItem((int)backgroundBlockIDs.woodenPlanks), 5, new List<RecipeIngredient>() { new RecipeIngredient(new BlockItem((int)blockIDs.treeTrunk), 1) }, manager);
            craftingRecipes.Add(woodenBackgroundRecipe);

            CraftingRecipe woodenDoorRecipe = new CraftingRecipe(recipeResult: new BlockItem((int)blockIDs.woodenDoor), 1, new List<RecipeIngredient>() { new RecipeIngredient(new BlockItem((int)blockIDs.treeTrunk), 3) }, manager);
            craftingRecipes.Add(woodenDoorRecipe);

            CraftingRecipe woodenPlatformRecipe = new CraftingRecipe(recipeResult: new BlockItem((int)blockIDs.woodenPlatform), 2, new List<RecipeIngredient>() { new RecipeIngredient(new BlockItem((int)blockIDs.treeTrunk), 1) }, manager);
            craftingRecipes.Add(woodenPlatformRecipe);
        }
    }
}