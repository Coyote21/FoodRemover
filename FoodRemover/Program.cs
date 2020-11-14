using System;
using System.Collections.Generic;
using System.Linq;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Synthesis;
using Mutagen.Bethesda.Skyrim;
using Mutagen.Bethesda.FormKeys.SkyrimSE;
using Noggog;
//using Alphaleonis.Win32.Filesystem;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.IO;



namespace FoodRemover
{
    public static class MyExtensions
    {
        public static bool ContainsInsensitive(this string str, string rhs)
        {
            return str.Contains(rhs, StringComparison.OrdinalIgnoreCase);
        }
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            return SynthesisPipeline.Instance.Patch<ISkyrimMod, ISkyrimModGetter>(
                args: args,
                patcher: RunPatch,
                userPreferences: new UserPreferences()
                {
                    ActionsForEmptyArgs = new RunDefaultPatcher()
                    {
                        IdentifyingModKey = "FoodRemover.esp",
                        TargetRelease = GameRelease.SkyrimSE
                    }
                });
        }

        public static void RunPatch(SynthesisState<ISkyrimMod, ISkyrimModGetter> state)
        {
            Random rnd = new Random();
            var chanceShop = 25;
            var chanceHab = 35;
            var chanceWealthy = 15;
            var chanceBase = 50;
            var chanceCamp = 75;
            var chanceSpecial = 80;
            var chanceDungeon = 100;

            var objsDisabled = 0;
        
            //Read user configs
            string percentFile = state.ExtraSettingsDataPath + @"\percentages.json";
            string skipPluginFile = state.ExtraSettingsDataPath + @"\skipPlugins.json";
            string skipLocationFile = state.ExtraSettingsDataPath + @"\skipLocations.json";
            string specialFile = state.ExtraSettingsDataPath + @"\specialLocations.json";
            
            //string configFilePath = Path.Combine(state.ExtraSettingsDataPath, "config.json");

            if (!File.Exists(percentFile))
            {
                Console.WriteLine("\"percentages.json\" not located in Users Data folder.");
                Console.WriteLine("Using default for all percentages.");
            }
            else
            {
                var percentJson = JObject.Parse(File.ReadAllText(percentFile));

                chanceShop = (int)(percentJson["LocTypeShop"] ?? 25);
                chanceHab = (int)(percentJson["LocTypeHabitation"] ?? 35);
                chanceWealthy = (int)(percentJson["LocTypeWealthy"] ?? 15);
                chanceCamp = (int)(percentJson["LocTypeCamps"] ?? 75);
                chanceDungeon = (int)(percentJson["LocTypeDungeon"] ?? 100);
                chanceSpecial = (int)(percentJson["LocTypeSpecial"] ?? 80);
                chanceBase = (int)(percentJson["Base"] ?? 50);
            }
            
            var removalChance = chanceBase;

            System.Console.WriteLine();
            System.Console.WriteLine("LocTypeShop: Disabling " + chanceShop + "% of Placed Food Objects");
            System.Console.WriteLine("LocTypeHabitation: Disabling " + chanceHab + "% of Placed Food Objects");
            System.Console.WriteLine("LocTypeWealthy: Disabling " + chanceWealthy + "% of Placed Food Objects");
            System.Console.WriteLine("LocTypeCamps: Disabling " + chanceCamp + "% of Placed Food Objects");
            System.Console.WriteLine("LocTypeDungeon: Disabling " + chanceDungeon + "% of Placed Food Objects");
            System.Console.WriteLine("LocTypeSpecial: Disabling " + chanceSpecial + "% of Placed Food Objects");
            System.Console.WriteLine("Base: Disabling " + chanceBase + "% of Placed Food Objects");
            System.Console.WriteLine();

            //Read list of plugins to ignore
            HashSet<ModKey>? skipFiles = null;

            if (!File.Exists(skipPluginFile))
            {
                Console.WriteLine("\"skipPlugins.json\" not located in Users Data folder.");
                Console.WriteLine("Searching all plugins for Placed Food Objects.");
            }
            else
            {
                TextReader textReader = File.OpenText(skipPluginFile);
                skipFiles = JsonConvert.DeserializeObject<HashSet<ModKey>>(textReader.ReadToEnd());

                if (skipFiles == null || skipFiles.Count == 0)
                {
                    System.Console.WriteLine("'skipPlugins.json' must contain a single array of strings of plugin names");
                    System.Console.WriteLine("For example");
                    System.Console.WriteLine("[\"plugin1.esp\", \"plugin2.esp\", \"plugin3.esm\"]");
                }
                else
                {
                    System.Console.WriteLine("Not searching for Objects in the following plugins:");
                    foreach (var plugin in skipFiles)
                    {
                        System.Console.WriteLine(plugin);
                    }
                }     
            }
            System.Console.WriteLine();

            //Read list of Locations to ignore
            HashSet<String>? skipLocations = null;

            if (!File.Exists(skipLocationFile))
            {
                Console.WriteLine("\"skipLocations.json\" not located in Users Data folder.");
                Console.WriteLine("Searching all Locations for Placed Food Objects.");
            }
            else
            {
                TextReader textReader = File.OpenText(skipLocationFile);
                skipLocations = JsonConvert.DeserializeObject<HashSet<String>>(textReader.ReadToEnd());

                if (skipLocations == null || skipLocations.Count == 0)
                {
                    System.Console.WriteLine("'skipLocations.json' must contain a single array of strings of Location EditorID's");
                    System.Console.WriteLine("These can be found using xEdit or a similar tool");
                    System.Console.WriteLine("For example");
                    System.Console.WriteLine("[\"ScaryCaveLocation\", \"BanditTowerLocation\", \"ProblematicPluginLocation\"]");
                }
                else
                {
                    System.Console.WriteLine("Not searching for Objects in the following Locations:");
                    foreach (var loc in skipLocations)
                    {
                        System.Console.WriteLine(loc);
                    }
                }
            }
            System.Console.WriteLine();

            //Read list of Special Locations
            HashSet<String>? specialLocations = null;

            if (!File.Exists(specialFile))
            {
                Console.WriteLine("\"specialLocations.json\" not located in Users Data folder.");
                Console.WriteLine("No Special Locations");
            }
            else
            {
                TextReader textReader = File.OpenText(specialFile);
                specialLocations = JsonConvert.DeserializeObject<HashSet<String>>(textReader.ReadToEnd());

                if (specialLocations == null || specialLocations.Count == 0)
                {
                    System.Console.WriteLine("'specialLocations.json' must contain a single array of strings of Location EditorID's");
                    System.Console.WriteLine("These can be found using xEdit or a similar tool");
                    System.Console.WriteLine("For example");
                    System.Console.WriteLine("[\"ScaryCaveLocation\", \"BanditTowerLocation\", \"ProblematicPluginLocation\"]");
                    System.Console.WriteLine("These locations will have Placed Food Objects Disabled using the LocTypeSpecial percentage listed above");
                }
                else
                {
                    System.Console.WriteLine("Using the following Special Locations:");
                    foreach (var loc in specialLocations)
                    {
                        System.Console.WriteLine(loc);
                    }
                }
            }
            System.Console.WriteLine();


            //Start Placed Object Iteration
            foreach (var placedObjectGetter in state.LoadOrder.PriorityOrder.PlacedObject().WinningContextOverrides(state.LinkCache))
            {
                //If already disabled, skip
                if (placedObjectGetter.Record.MajorRecordFlagsRaw == 0x0000_0800) continue;
               
                var placedObject = placedObjectGetter.Record;
                if (placedObject.EditorID == null)
                {
                    // Try to find the base object record, skip if null or not found
                    if (!state.LinkCache.TryLookup<IIngestibleGetter>(placedObjectGetter.Record.Base.FormKey ?? FormKey.Null, out var placedObjectBase)) continue;

                    // Check if it's EDID contains "Food", skip if EDID null or does not contain "Food"
                    if (placedObjectBase.EditorID == null || !placedObjectBase.EditorID.ContainsInsensitive("Food")) continue;

                    // Try to find the parent cell, skip if null or not found
                    if (!placedObjectGetter.TryGetParent<ICellGetter>(out var parentCell)) continue;

                    // Find if parent cell is in list of plugins to skip
                    if (skipFiles != null && parentCell.FormKey.ModKey != null && skipFiles.Contains(parentCell.FormKey.ModKey)) continue;

                    // Find the cell's location record, skip if null or not found
                    if (!parentCell.Location.TryResolve(state.LinkCache, out var placedObjectLocation)) continue;

                    // Find if location is in list of locations to skip
                    if (skipLocations != null && placedObjectLocation.EditorID != null && skipLocations.Contains(placedObjectLocation.EditorID)) continue;

                    // Ensure the cell location has keywords, skip if it doesn't
                    if (placedObjectLocation.Keywords == null) continue;

                    var locationKeywords = placedObjectLocation.Keywords;

                    //Set the removal chance based on location type
                    //Check for special locations first
                    if (specialLocations != null && placedObjectLocation.EditorID != null && specialLocations.Contains(placedObjectLocation.EditorID))
                    {
                        removalChance = chanceSpecial;
                    }
                    // Check for LocTypeHab Locations
                    else if ( locationKeywords.Contains(Skyrim.Keyword.LocTypeDwelling) || locationKeywords.Contains(Skyrim.Keyword.LocTypeHabitation) || locationKeywords.Contains(Skyrim.Keyword.LocTypeTemple) || locationKeywords.Contains(Skyrim.Keyword.LocTypeShip) )
                    {
                        removalChance = (placedObjectLocation.EditorID == "KatariahLocation" ? chanceWealthy : chanceHab);
                    }
                    // Check for LocTypeShop Locations
                    else if ( locationKeywords.Contains(Skyrim.Keyword.LocTypeStore) || locationKeywords.Contains(Skyrim.Keyword.LocTypeInn) || locationKeywords.Contains(Skyrim.Keyword.LocTypeFarm) )
                    {
                        removalChance = chanceShop;
                    }
                    // Check for LocTypeCamp Locations
                    else if ( locationKeywords.Contains(Skyrim.Keyword.LocTypeMilitaryCamp) || locationKeywords.Contains(Skyrim.Keyword.LocTypeMilitaryFort) || locationKeywords.Contains(Skyrim.Keyword.LocTypeBarracks) || locationKeywords.Contains(Skyrim.Keyword.LocTypeBanditCamp) || locationKeywords.Contains(Skyrim.Keyword.LocTypeForswornCamp) || locationKeywords.Contains(Skyrim.Keyword.LocTypeWarlockLair) || locationKeywords.Contains(Skyrim.Keyword.LocTypeMine) || locationKeywords.Contains(Skyrim.Keyword.LocTypeJail) || locationKeywords.Contains(Skyrim.Keyword.LocSetMilitaryFort) )
                    {
                        removalChance = chanceCamp;
                    }
                    // Check for LocTypeDungeons Locations
                    else if ( locationKeywords.Contains(Skyrim.Keyword.LocTypeDungeon) || locationKeywords.Contains(Skyrim.Keyword.LocSetCave) || locationKeywords.Contains(Skyrim.Keyword.LocTypeAnimalDen) || locationKeywords.Contains(Skyrim.Keyword.LocSetDwarvenRuin) || locationKeywords.Contains(Skyrim.Keyword.LocTypeDraugrCrypt) || locationKeywords.Contains(Skyrim.Keyword.LocSetNordicRuin) )
                    {
                        removalChance = chanceDungeon;
                    }
                    // Check for LocTypeWealthy Locations
                    else if (locationKeywords.Contains(Skyrim.Keyword.LocTypeHouse))
                    {
                        removalChance = (locationKeywords.Contains(Skyrim.Keyword.TGWealthyHome) ? chanceWealthy : chanceHab);
                    }
                    // else use base
                    else
                    {
                        removalChance = chanceBase;
                        
                        //Skip cetain problematic locations
                        if (placedObjectLocation.EditorID == "BluePalaceWingLocation") continue;
                        if (placedObjectLocation.EditorID == "HelgenLocation") continue;
                        if (placedObjectLocation.EditorID == "TwilightSepulcherLocation") continue;
                        if (placedObjectLocation.EditorID == "HalloftheVigilantLocation") continue;
                        if (placedObjectLocation.EditorID == "NightingaleHallLocation") continue;
                        

                        //More problematic locations that should not be skipped
                        if (placedObjectLocation.EditorID == "GoldenglowEstateLocation") removalChance = chanceHab;
                        if (placedObjectLocation.EditorID == "DrelasCottageLocation") removalChance = chanceHab;
                        if (placedObjectLocation.EditorID == "SkyHavenTempleLocation") removalChance = chanceCamp;
                        if (placedObjectLocation.EditorID == "ShorsWatchtowerLocation") removalChance = chanceCamp;
                        
                        //Suitable location not yet found
                        if (removalChance == chanceBase)
                        {
                            foreach (var locType in locationKeywords)
                            {
                                if (locType.TryResolve(state.LinkCache, out var locTypeRec))
                                {
                                    //Falskaar location types
                                    if (locTypeRec.EditorID == "FSLocTypeDungeon") removalChance = chanceDungeon;
                                    if (locTypeRec.EditorID == "FSLocTypeBanditCamp") removalChance = chanceCamp;
                                    if (locTypeRec.EditorID == "FSLocTypeHabitation") removalChance = chanceHab;
                                    if (locTypeRec.EditorID == "FSLocTypeDwelling") removalChance = chanceHab;
                                    if (locTypeRec.EditorID == "FSLocTypeInn") removalChance = chanceShop;
                                }
                            }
                        }
                        
                    }
                        
                    //If RND < removal chance, copy as override into new plugin and set to initially disabled
                    if (rnd.Next(100) < removalChance)
                    {
                        IPlacedObject modifiedObject = placedObjectGetter.GetOrAddAsOverride(state.PatchMod);
                        modifiedObject.MajorRecordFlagsRaw |= 0x0000_0800;
                        objsDisabled++;
                    }
                }
            }
            System.Console.WriteLine(objsDisabled + " Placed Food Objects Disabled");
        }
    }
}