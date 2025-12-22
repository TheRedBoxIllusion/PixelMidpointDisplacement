using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using Vector2 = Microsoft.Xna.Framework.Vector2;


namespace PixelMidpointDisplacement {

    /*
     * ========================================
     * 
     * Evolution Parent Classes
     * 
     *  The parent classes for the different aspects of an evolution tree
     *  
     *   EvolutionTree class that contains all of the layers, and controls the different evolutions
     *   EvolutionTreeLayer class that contains all of the evolutions within a singular layer of the tree
     *   Evolution class that contains the specific ability of the evolution and pre-requesets to unlock the evolution
     * 
     * ========================================
    */

    public class EvolutionTree
    {
        //A class that contains a structured list of evolutions and their prerequesits.
        public Entity owner;
        //Not sure how to contain a tree
        public List<EvolutionTreeLayer> evolutionTree = new List<EvolutionTreeLayer>();
        List<Evolution> activeEvolutions = new List<Evolution>();
        Dictionary<ExperienceField, double> entityExperience = new Dictionary<ExperienceField, double>();

        public EvolutionTree(Entity treeOwner)
        {
            owner = treeOwner;

            entityExperience.Add(ExperienceField.Knowledge, 0);
            entityExperience.Add(ExperienceField.Durability, 0);
            entityExperience.Add(ExperienceField.Maneuverability, 0);
            entityExperience.Add(ExperienceField.Damage, 0);
        }

        public Evolution getEvolution(Type evolutionTpe)
        {

            for (int x = 0; x < evolutionTree.Count; x++)
            {
                for (int y = 0; y < evolutionTree[x].evolutionLayer.Count; y++)
                {
                    if (evolutionTpe.IsInstanceOfType(evolutionTree[x].evolutionLayer[y].evolution))
                    {
                        return evolutionTree[x].evolutionLayer[y].evolution;
                    }
                }
            }

            return null;
        }
        public void addEvolutionTreeLayer(EvolutionTreeLayer e)
        {
            if (evolutionTree.Count == 0)
            {
                for (int i = 0; i < e.evolutionLayer.Count; i++)
                {
                    e.evolutionLayer[i].evolution.canBeActivated = true;
                }
            }
            evolutionTree.Add(e);

        }

        public void activateEvolution(int treeLayer, int indexWithinLayer)
        {
            if (treeLayer >= 0 && treeLayer < evolutionTree.Count)
            {
                if (canEvolutionBeActivated(treeLayer, indexWithinLayer))
                {
                    evolutionTree[treeLayer].evolutionLayer[indexWithinLayer].evolution.onAppliedToEntity();
                    activeEvolutions.Add(evolutionTree[treeLayer].evolutionLayer[indexWithinLayer].evolution);

                    //Reduce the entities experience based on the costs of the evolution

                    List<(ExperienceField field, double cost)> cost = evolutionTree[treeLayer].evolutionLayer[indexWithinLayer].evolution.evolutionCost;
                    for (int i = 0; i < cost.Count; i++)
                    {
                        entityExperience[cost[i].field] -= cost[i].cost;

                    }

                    owner.worldContext.engineController.evolutionController.updateExperienceCounters();

                    //Check up the tree to see if any evolutions can now be activated and update them corrospondingly:
                    recalculateAvailableEvolutions();

                }
            }
        }

        public void recalculateAvailableEvolutions()
        {
            for (int x = 0; x < evolutionTree.Count; x++)
            {
                for (int y = 0; y < evolutionTree[x].evolutionLayer.Count; y++)
                {
                    int evolutionTreeLayer = evolutionTree[x].evolutionLayer[y].evolution.treeLayer;
                    int evolutionIndexWithinLayer = evolutionTree[x].evolutionLayer[y].evolution.indexWithinLayer;
                    evolutionTree[x].evolutionLayer[y].evolution.canBeActivated = canEvolutionBeActivated(evolutionTreeLayer, evolutionIndexWithinLayer);
                }
            }
        }

        public bool canEvolutionBeActivated(int treeLayer, int indexWithinLayer)
        {
            bool allPrerequisitesAreActive = true;
            if (treeLayer > 0)
            {
                foreach (int index in evolutionTree[treeLayer].evolutionLayer[indexWithinLayer].prerequisiteEvolutions)
                {
                    if (!evolutionTree[treeLayer - 1].evolutionLayer[index].evolution.isEvolutionActive)
                    {
                        allPrerequisitesAreActive = false;
                        break;
                    }
                }
            }

            bool canPayCost = true;

            List<(ExperienceField field, double cost)> cost = evolutionTree[treeLayer].evolutionLayer[indexWithinLayer].evolution.evolutionCost;
            for (int i = 0; i < cost.Count; i++)
            {
                if (entityExperience[cost[i].field] < cost[i].cost)
                {
                    canPayCost = false;
                    break;
                }
            }

            bool canEvolutionBeActivated = false;
            if (allPrerequisitesAreActive && canPayCost && !activeEvolutions.Contains(evolutionTree[treeLayer].evolutionLayer[indexWithinLayer].evolution))
            {
                canEvolutionBeActivated = true;
            }
            return canEvolutionBeActivated;
        }
        public void addExperience(ExperienceField field, double experienceGain)
        {
            entityExperience[field] += experienceGain;
            owner.worldContext.engineController.evolutionController.updateExperienceCounters();
        }

        public double getExperience(ExperienceField field)
        {
            return entityExperience[field];
        }
    }
    public class EvolutionTreeLayer
    {
        //A single layer of the evolution tree. The prerequisite evolutions must come from the previous layer, and there can be multiple
        public List<(Evolution evolution, List<int> prerequisiteEvolutions)> evolutionLayer;

        public EvolutionTreeLayer()
        {
            evolutionLayer = new List<(Evolution evolution, List<int>)>();
        }
    }
    public class Evolution
    {
        public Entity owner;
        public EvolutionTree tree;

        public int iconSourceY;

        public int treeLayer;
        public int indexWithinLayer;

        public bool isEvolutionActive = false;

        public bool canBeActivated = false;

        public List<(ExperienceField field, double cost)> evolutionCost = new List<(ExperienceField field, double cost)>();
        //Just defines some functions or variable changes
        public Evolution(EvolutionTree tree, int treeLayer, int indexWithinLayer)
        {
            this.owner = tree.owner;
            this.treeLayer = treeLayer;
            this.indexWithinLayer = indexWithinLayer;
            this.tree = tree;
        }

        public void requestActivation()
        {
            if (tree != null)
            {
                tree.activateEvolution(treeLayer, indexWithinLayer);
            }
        }

        public virtual void onAppliedToEntity()
        {
            isEvolutionActive = true;
            //Add whatever action listeers that the evolution needs
        }

        public virtual void onRemovedFromEntity()
        {
        }
    }


    /*
     * ========================================
     * 
     * Entity Specific Evolution Trees
     * 
     *  Entity specific evolution trees that contain what evolutions the entity can unlock
     * 
     *  Player Evolution Tree
     * 
     * ========================================
    */

    public class PlayerEvolutionTree : EvolutionTree {
        public PlayerEvolutionTree(Player player) : base(player) {
            //Setup the players initial evolution tree
            WorldContext worldContext = player.worldContext;

            List<EvolutionButton> buttons = new List<EvolutionButton>();

            EvolutionTreeLayer e1 = new EvolutionTreeLayer();
            JumpEvolution jumpEvolution = new JumpEvolution(this, 0, 0);


            e1.evolutionLayer.Add((jumpEvolution, new List<int>() { 0 }));

            EvolutionTreeLayer e2 = new EvolutionTreeLayer();
            DoubleJumpEvolution doublejumpEvolution = new DoubleJumpEvolution(this, 1, 0);
            JumpEvolution jumpEvolution2 = new JumpEvolution(this, 1, 1);


            e2.evolutionLayer.Add((doublejumpEvolution, new List<int>() { 0 }));
            e2.evolutionLayer.Add((jumpEvolution2, new List<int>() { 0 }));

            addEvolutionTreeLayer(e1);
            addEvolutionTreeLayer(e2);

            worldContext.engineController.evolutionController.setupPlayerEvolutionUI(this);
            worldContext.engineController.evolutionController.setTree(this);
        } 
    }

    /*
     * ========================================
     * 
     * Evolution Classes
     * 
     *  All of the evolutions within the game
     *  
     *  Evolutions have special, permanent abilities that the entity can obtain
     *  
     *  Jump Evolution
     *  Double Jump Evolution
     * 
     * ========================================
    */
    public class JumpEvolution : Evolution
    {

        double jumpIncrease;
        public JumpEvolution(EvolutionTree tree, int treeLayer, int indexWithinLayer) : base(tree, treeLayer, indexWithinLayer)
        {
            iconSourceY = 16;
            evolutionCost.Add((ExperienceField.Maneuverability, 10));
        }



        public override void onAppliedToEntity()
        {
            jumpIncrease = owner.baseJumpAcceleration;
            owner.baseJumpAcceleration += jumpIncrease;
            base.onAppliedToEntity();
        }

        public override void onRemovedFromEntity()
        {
            owner.baseJumpAcceleration -= jumpIncrease;
            jumpIncrease = 0;
            base.onRemovedFromEntity();
        }
    }
    public class DoubleJumpEvolution : Evolution, IEntityActionListener
    {
        public double jumpWaitTime;
        public double maxJumpWaitTime = 0.4f;
        public bool hasSetWaitTimeOnce = false;
        public bool hasDoubleJumped = false;

        public double jumpAcceleration;
        public DoubleJumpEvolution(EvolutionTree tree, int treeLayer, int indexWithinLayer) : base(tree, treeLayer, indexWithinLayer)
        {
            iconSourceY = 32;
            evolutionCost.Add((ExperienceField.Maneuverability, 15));
        }

        public override void onAppliedToEntity()
        {
            owner.inputListeners.Add(this);
            //Turn off the jump Evolution from before. Eg. Replace it:
            JumpEvolution j = (JumpEvolution)tree.getEvolution(typeof(JumpEvolution));
            if (j != null)
            {
                if (j.isEvolutionActive)
                {
                    j.onRemovedFromEntity();
                }
            }
            base.onAppliedToEntity();
        }

        public override void onRemovedFromEntity()
        {
            owner.inputListeners.Remove(this);
            base.onRemovedFromEntity();
        }

        public void onInput(double elapsedTime)
        {

            if (jumpWaitTime > 0)
            {
                jumpWaitTime -= elapsedTime;
            }

            jumpAcceleration = owner.jumpAcceleration;

            if (!owner.isOnGround)
            {
                if (hasSetWaitTimeOnce)
                {
                    if (jumpWaitTime <= 0)
                    {
                        if ((Keyboard.GetState().IsKeyDown(Keys.W) || Keyboard.GetState().IsKeyDown(Keys.Space)) && !hasDoubleJumped)
                        {
                            hasDoubleJumped = true;
                            if (owner.velocityY < 0)
                            {
                                owner.velocityY = 0;
                            }
                            owner.accelerationY += jumpAcceleration / elapsedTime;
                        }
                    }
                }
                else
                {
                    jumpWaitTime = maxJumpWaitTime;
                    hasSetWaitTimeOnce = true;
                }
            }
            else if (hasSetWaitTimeOnce || hasDoubleJumped)
            {
                hasSetWaitTimeOnce = false;
                hasDoubleJumped = false;
            }
        }
    }
}