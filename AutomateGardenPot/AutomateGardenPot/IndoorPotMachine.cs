using Microsoft.Xna.Framework;
using Pathoschild.Stardew.Automate;
using StardewValley;
using StardewValley.Objects;
using StardewValley.Tools;
using System;
using System.Reflection;

namespace AutomateGardenPot
{
    public class IndoorPotMachine : IMachine
    {
        /*********
        ** Fields
        *********/
        /// <summary>The underlying entity.</summary>
        private readonly IndoorPot Entity;


        /*********
        ** Accessors
        *********/
        /// <summary>The location which contains the machine.</summary>
        public GameLocation Location { get; }

        /// <summary>The tile area covered by the machine.</summary>
        public Microsoft.Xna.Framework.Rectangle TileArea { get; }

        private Vector2 tile;

        /// <summary>A unique ID for the machine type.</summary>
        /// <remarks>This value should be identical for two machines if they have the exact same behavior and input logic. For example, if one machine in a group can't process input due to missing items, Automate will skip any other empty machines of that type in the same group since it assumes they need the same inputs.</remarks>
        string IMachine.MachineTypeID { get; } = "AutomateGardenPot/IndoorPot";

        /*********
        ** Public methods
        *********/
        /// <summary>Construct an instance.</summary>
        /// <param name="entity">The underlying entity.</param>
        /// <param name="location">The location which contains the machine.</param>
        /// <param name="tile">The tile covered by the machine.</param>
        public IndoorPotMachine(IndoorPot entity, GameLocation location, in Vector2 tile)
        {
            this.tile = tile;
            this.Entity = entity;
            this.Location = location;
            this.TileArea = new Microsoft.Xna.Framework.Rectangle((int)tile.X, (int)tile.Y, 1, 1);
        }

        /// <summary>Get the machine's processing state.</summary>
        public MachineState GetState()
        {
            if (this.Entity.hoeDirt.Value == null || this.Entity.hoeDirt.Value.crop == null)
            {
                return MachineState.Disabled;
            }

            if (this.Entity.hoeDirt.Value.state.Value != 1 && !this.Entity.hoeDirt.Value.readyForHarvest())
                return MachineState.Empty;

            return this.Entity.hoeDirt.Value.readyForHarvest()
                ? MachineState.Done
                : MachineState.Processing;
        }

        private bool CanHarvest()
        {
            Crop crop = GetCrop();
            int currentPhase = crop.currentPhase.Value;
            int lastPhase = crop.phaseDays.Count - 1;
            int dayOfCurrentPhase = crop.dayOfCurrentPhase.Value;
            bool dead = crop.dead.Value;
            bool canHarvest = currentPhase >= lastPhase && !dead && dayOfCurrentPhase == 0;
            return canHarvest;
        }

        private Crop GetCrop()
        {
            return this.Entity.hoeDirt.Value.crop;
        }

        /// <summary>Get the output item.</summary>
        public ITrackedStack GetOutput()
        {
            TrackedItem item = new TrackedItem(GetItem(), onReduced: i =>
            {
                if (GetCrop().regrowAfterHarvest == -1)
                {
                    return;
                }
                GetCrop().fullyGrown.Value = true;
                if (GetCrop().dayOfCurrentPhase.Value == GetCrop().regrowAfterHarvest)
                {
                    GetCrop().updateDrawMath(this.tile);
                }
                GetCrop().dayOfCurrentPhase.Value = GetCrop().regrowAfterHarvest;
            });
            return item;

        }

        private int GetNumToHarvest()
        {
            int numToHarvest = 1;
            if (GetCrop().minHarvest > 1 || GetCrop().maxHarvest > 1)
            {
                int max_harvest_increase = 0;
                if (GetCrop().maxHarvestIncreasePerFarmingLevel.Value > 0)
                {
                    max_harvest_increase = Game1.player.FarmingLevel / GetCrop().maxHarvestIncreasePerFarmingLevel;
                }
                Random rd = new Random();
                numToHarvest = rd.Next(GetCrop().minHarvest, Math.Max(GetCrop().minHarvest + 1, GetCrop().maxHarvest + 1 + max_harvest_increase));
            }
            return numToHarvest;
        }

        private Item GetItem()
        {
            Item item = null;
            int cropQuality = 0;
            if (!GetCrop().programColored)
            {
                item = new StardewValley.Object(GetCrop().indexOfHarvest, GetNumToHarvest(), false, -1, cropQuality);
            }
            else
            {
                //(item = new ColoredObject(GetCrop().indexOfHarvest, 1, GetCrop().tintColor)) = cropQuality;
                item = new ColoredObject(GetCrop().indexOfHarvest, GetNumToHarvest(), GetCrop().tintColor);
            }
            return item;
        }

        /// <summary>Provide input to the machine.</summary>
        /// <param name="input">The available items.</param>
        /// <returns>Returns whether the machine started processing an item.</returns>
        public bool SetInput(IStorage input)
        {
            foreach (ITrackedStack item in input.GetItems())
            {
                if (item.Sample is WateringCan){
                    WateringCan wateringCan = (WateringCan) GetItem(item);
                    wateringCan.WaterLeft--;
                    Entity.hoeDirt.Value.state.Value = 1;
                    return true;
                }
            }
            return false;
        }

        public Item GetItem(ITrackedStack stack)
        {
            FieldInfo field = stack.GetType().GetField("Item", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return (Item)field.GetValue(stack);
            }            
            return null;
        }

        public bool FindWateringCan(ITrackedStack stack)
        {
            if (stack.Sample is WateringCan)
            {
                return ((WateringCan)stack.Sample).WaterLeft > 0;
            }
            return false;
        }
    }
}
