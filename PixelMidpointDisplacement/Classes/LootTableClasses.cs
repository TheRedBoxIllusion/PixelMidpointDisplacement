using System;
using System.Collections.Generic;

namespace PixelMidpointDisplacement {

    /* 
     * ========================================
     * 
     * Loot Table Parent Classes
     * 
     *  The different layers of a standard loot table
     *  
     *  LootTable contains a list of different loot tables and the chance for that table to be chosen.
     *  IndividualLootTable contains the list of loot within that table
     *  Loot contains the data of a certain loot item, such as the chance an item is picked, along with the range of stack sizes
     * 
     * ========================================
    */
    public class LootTable
    {
        public List<List<(double percentage, IndividualLootTable)>> lootTable = new List<List<(double percentage, IndividualLootTable)>>();

        public void addLootTable(List<(double percentage, IndividualLootTable individualTable)> lootTable)
        {
            this.lootTable.Add(lootTable);
        }

        public List<Item> generateLoot()
        {
            Random r = new Random();
            List<Item> generatedItems = new List<Item>();
            for (int i = 0; i < lootTable.Count; i++)
            {
                double cummulativePercentage = 0;
                for (int l = 0; l < lootTable[i].Count; l++)
                {
                    cummulativePercentage += lootTable[i][l].percentage;
                    if (r.NextDouble() * 100 < cummulativePercentage)
                    {
                        //This loot table was chosen:
                        foreach (Item item in lootTable[i][l].Item2.generateLootFromTable())
                        {
                            generatedItems.Add(item);
                        }
                        break;
                    }
                }
            }

            return generatedItems;
        }
    }
    public class IndividualLootTable
    {
        public List<Loot> lootTable = new List<Loot>();

        public IndividualLootTable(List<Loot> lootTable)
        {
            this.lootTable = lootTable;
        }

        public List<Item> generateLootFromTable()
        {
            List<Item> generatedLoot = new List<Item>();
            Random r = new Random();
            foreach (Loot l in lootTable)
            {
                //Determine if the item should be added to the chest
                if (r.Next(100) < l.percentageChance)
                {
                    //Pick the amount:
                    int itemCount = r.Next(l.minItemCount, l.maxItemCount + 1);

                    generatedLoot.Add(l.item.itemCopy(itemCount));
                }
            }

            return generatedLoot;
        }
    }
    public class Loot {
        public double percentageChance;
        public int minItemCount;
        public int maxItemCount;
        public Item item;

        public Loot(double percentageChance, int minItemCount, int maxItemCount, Item item) {
            this.percentageChance = percentageChance;
            this.minItemCount = minItemCount;
            this.maxItemCount = maxItemCount;
            this.item = item;
        }
    }

    /*
     * ========================================
     * 
     * Loot Table Classes
     * 
     *  Individual loot tables for different use cases
     *  
     *  WoodenChestLootTable
     *  MountainChestLootTable
     *  ZombieLootTable
     * 
     * ========================================
     */

    public class WoodenChestLootTable : LootTable {
        public WoodenChestLootTable() {
            addLootTable(new List<(double percentage, IndividualLootTable itable)>() {
                (50, new IndividualLootTable(
                    new List<Loot> {
                        new Loot(100, 20, 30, new BlockItem((int)blockIDs.stone)),
                        new Loot(50, 1, 1, new Weapon())
                    }
                    )),
                (50, new IndividualLootTable(
                    new List<Loot>{
                new Loot(100, 45,90, new BlockItem((int)blockIDs.torch)),
                new Loot(30, 1, 1, new Bow())
                }))


             });
        }
    }

    public class MountainChestLootTable : LootTable {
        public MountainChestLootTable() {
            addLootTable(new List<(double percentage, IndividualLootTable)> {
                    (100,
                    new IndividualLootTable(new List<Loot>{
                        new Loot(100, 30, 40, new BlockItem((int)blockIDs.stone)),
                        new Loot(40, 1, 1, new Helmet()),
                        new Loot(70, 1, 1, new AmuletOfFallDamage())
                    }
                    ))
                });
        }
    }
}