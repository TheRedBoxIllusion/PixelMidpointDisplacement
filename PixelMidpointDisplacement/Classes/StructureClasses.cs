using System;
using System.IO;

namespace PixelMidpointDisplacement {
    public class Structure
    {
        public string structureName;
        int[,] structureArray;
        int[,] structureBackgroundArray;
        public Structure(string structureName)
        {
            this.structureName = structureName;
            importStructure();
        }
        public void importStructure()
        {
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
                while (lineToRead != null)
                {
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

        public void placeStructure(Biome currentBiome, int xLoc, int yLoc)
        {
            for (int x = 0; x < structureArray.GetLength(0); x++)
            {
                for (int y = 0; y < structureArray.GetLength(1); y++)
                {
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
}