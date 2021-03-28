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
using System.Threading.Tasks;

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
        private static ModKey falskaarMod = ModKey.FromNameAndExtension("Falskaar.esm");

        public static Task<int> Main(string[] args)
        {
            return SynthesisPipeline.Instance
                .SetTypicalOpen(GameRelease.SkyrimSE, "FoodRemover.esp")
                .AddPatch<ISkyrimMod, ISkyrimModGetter>(RunPatch)
                .Run(args);
        }

        public static void RunPatch(IPatcherState<ISkyrimMod, ISkyrimModGetter> state)
        {
            //Start step 1. Initialize everything and read any user-config files
            var chanceShop = 25;
            var chanceHab = 35;
            var chanceWealthy = 15;
            var chanceBase = 50;
            var chanceCamp = 75;
            var chanceSpecial = 80;
            var chanceDungeon = 100;

            var objsDisabled = 0;

            //List of Falskaar Location Keywords and their approriate percentages
            Dictionary<string, int> fsLocations = new Dictionary<string, int>
            {
                { "FSLocTypeBanditCamp", chanceCamp },
                { "FSLocTypeDungeon", chanceDungeon },
                { "FSLocTypeGiantCamp", chanceHab },
                { "FSLocTypeHabitationHasInn", chanceShop },
                { "FSLocTypeHabitation", chanceHab },
                { "FSLocTypeDwelling", chanceHab },
                { "FSLocTypeInn", chanceShop },
            };

            //List of problematic Locations that need percentages
            Dictionary<string, int> problemLocationsP = new Dictionary<string, int>
            {
                { "GoldenglowEstateLocation", chanceHab },
                { "DrelasCottageLocation", chanceHab },
                { "SkyHavenTempleLocation", chanceCamp },
                { "ShorsWatchtowerLocation", chanceCamp },
            };

            //List of problematic Locations the should be skipped
            HashSet<String> problemLocationsS = new HashSet<string>
            {
                { "BluePalaceWingLocation" },
                { "HelgenLocation" },
                { "TwilightSepulcherLocation" },
                { "HalloftheVigilantLocation" },
                { "NightingaleHallLocation" },
            };

            //Read user configs
            string percentFile = state.ExtraSettingsDataPath + @"\percentages.json";
            string skipPluginFile = state.ExtraSettingsDataPath + @"\skipPlugins.json";
            string skipLocationFile = state.ExtraSettingsDataPath + @"\skipLocations.json";
            string specialFile = state.ExtraSettingsDataPath + @"\specialLocations.json";
            string customFile = state.ExtraSettingsDataPath + @"\customPercentages.json";

            //string configFilePath = Path.Combine(state.ExtraSettingsDataPath, "config.json");

            if (!File.Exists(percentFile))
            {
                Console.WriteLine("\"percentages.json\" not located in Users Data folder.");
                Console.WriteLine("Using default for all percentages.");
            }
            else
            {
                var percentJson = JObject.Parse(File.ReadAllText(percentFile));

                chanceShop = (int)(percentJson["LocTypeShop"] ?? chanceShop);
                chanceHab = (int)(percentJson["LocTypeHabitation"] ?? chanceHab);
                chanceWealthy = (int)(percentJson["LocTypeWealthy"] ?? chanceWealthy);
                chanceCamp = (int)(percentJson["LocTypeCamps"] ?? chanceCamp);
                chanceDungeon = (int)(percentJson["LocTypeDungeon"] ?? chanceDungeon);
                chanceSpecial = (int)(percentJson["LocTypeSpecial"] ?? chanceSpecial);
                chanceBase = (int)(percentJson["Base"] ?? chanceBase);
            }

            var customPercent = new JObject();
            if (!File.Exists(customFile))
            {
                Console.WriteLine("\"customPercentages.json\" not located in Users Data folder.");
            }
            else
            {
                Console.WriteLine("\"customPercentages.json\" located.");
                customPercent = JObject.Parse(File.ReadAllText(customFile));
                foreach (var locID in customPercent)
                {
                    Console.WriteLine("Custom Location: " + locID.Key + " has percentage " + locID.Value + "%");
                }
            }

            Console.WriteLine();
            Console.WriteLine("LocTypeShop: Disabling " + chanceShop + "% of Placed Food Objects");
            Console.WriteLine("LocTypeHabitation: Disabling " + chanceHab + "% of Placed Food Objects");
            Console.WriteLine("LocTypeWealthy: Disabling " + chanceWealthy + "% of Placed Food Objects");
            Console.WriteLine("LocTypeCamps: Disabling " + chanceCamp + "% of Placed Food Objects");
            Console.WriteLine("LocTypeDungeon: Disabling " + chanceDungeon + "% of Placed Food Objects");
            Console.WriteLine("LocTypeSpecial: Disabling " + chanceSpecial + "% of Placed Food Objects");
            Console.WriteLine("Base: Disabling " + chanceBase + "% of Placed Food Objects");
            Console.WriteLine();

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
                    Console.WriteLine("'skipPlugins.json' must contain a single array of strings of plugin names");
                    Console.WriteLine("For example");
                    Console.WriteLine("[\"plugin1.esp\", \"plugin2.esp\", \"plugin3.esm\"]");
                }
                else
                {
                    Console.WriteLine("Not searching for Objects in the following plugins:");
                    foreach (var plugin in skipFiles)
                    {
                        Console.WriteLine(plugin);
                    }
                }     
            }
            Console.WriteLine();

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
                    Console.WriteLine("'skipLocations.json' must contain a single array of strings of Location EditorID's");
                    Console.WriteLine("These can be found using xEdit or a similar tool");
                    Console.WriteLine("For example");
                    Console.WriteLine("[\"ScaryCaveLocation\", \"BanditTowerLocation\", \"ProblematicPluginLocation\"]");
                }
                else
                {
                    Console.WriteLine("Not searching for Objects in the following Locations:");
                    foreach (var loc in skipLocations)
                    {
                        Console.WriteLine(loc);
                    }
                }
            }
            Console.WriteLine();

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
                    Console.WriteLine("'specialLocations.json' must contain a single array of strings of Location EditorID's");
                    Console.WriteLine("These can be found using xEdit or a similar tool");
                    Console.WriteLine("For example");
                    Console.WriteLine("[\"ScaryCaveLocation\", \"BanditTowerLocation\", \"ProblematicPluginLocation\"]");
                    Console.WriteLine("These locations will have Placed Food Objects Disabled using the LocTypeSpecial percentage listed above");
                }
                else
                {
                    Console.WriteLine("Using the following Special Locations:");
                    foreach (var loc in specialLocations)
                    {
                        Console.WriteLine(loc);
                    }
                }
            }
            Console.WriteLine();


            // Start step 2. Start Placed Object Iteration
            foreach (var placedObjectGetter in state.LoadOrder.PriorityOrder.PlacedObject().WinningContextOverrides(state.LinkCache))
            {
                //If already disabled, skip
                if (placedObjectGetter.Record.MajorRecordFlagsRaw == 0x0000_0800) continue;
               
                var placedObject = placedObjectGetter.Record;
                if (placedObject.EditorID == null)
                {
                    // Try to find the base object record, skip if null or not found
                    if (!placedObjectGetter.Record.Base.TryResolve<IIngestibleGetter>(state.LinkCache, out var placedObjectBase)) continue;

                    // Check if it's EDID contains "Food", skip if EDID null or does not contain "Food"
                    if (placedObjectBase.EditorID == null || !placedObjectBase.EditorID.ContainsInsensitive("Food")) continue;

                    // Try to find the parent cell, skip if null or not found
                    if (!placedObjectGetter.TryGetParent<ICellGetter>(out var parentCell)) continue;

                    // Find if parent cell is in users list of plugins to skip
                    if (skipFiles != null && parentCell.FormKey.ModKey != null && skipFiles.Contains(parentCell.FormKey.ModKey)) continue;

                    // Find the cell's location record, skip if null or not found
                    if (!parentCell.Location.TryResolve(state.LinkCache, out var placedObjectLocation)) continue;

                    // Find if location is in list of locations to skip
                    if (placedObjectLocation.EditorID == null || problemLocationsS.Contains(placedObjectLocation.EditorID)) continue;
                        
                    // Check if location is in users list of locations to skip
                    if (skipLocations != null && placedObjectLocation.EditorID != null && skipLocations.Contains(placedObjectLocation.EditorID)) continue;

                    // Ensure the cell location has keywords, skip if it doesn't
                    if (placedObjectLocation.Keywords == null) continue;

                    //Start disabling step
                    //Set the removal chance based on location type keyword
                    var locationKeywords = placedObjectLocation.Keywords;

                    //Start at base chance
                    int removalChance = chanceBase;

                    // Check if location is in users custom list of percentages
                    if (placedObjectLocation.EditorID != null && customPercent != null && customPercent.ContainsKey(placedObjectLocation.EditorID))
                    {
                        removalChance = (int)customPercent[placedObjectLocation.EditorID]!;
                        Console.WriteLine("CustomLoc: " + placedObjectLocation.EditorID + " using " + removalChance + "%");
                    }
                    else
                    // Check for special locations
                    if (specialLocations != null && placedObjectLocation.EditorID != null && specialLocations.Contains(placedObjectLocation.EditorID))
                    {
                        removalChance = chanceSpecial;
                    }
                    // Check for house with LocTypeWealthy Locations
                    else if (locationKeywords.Contains(Skyrim.Keyword.LocTypeHouse))
                    {
                        removalChance = (locationKeywords.Contains(Skyrim.Keyword.TGWealthyHome) ? chanceWealthy : chanceHab);
                    }
                    // Check for Palace Locations
                    else if (locationKeywords.Contains(Skyrim.Keyword.LocTypeCastle))
                    {
                        removalChance = chanceWealthy;
                    }
                    // Check for LocTypeHab Locations
                    else if ( locationKeywords.Contains(Skyrim.Keyword.LocTypeDwelling) || locationKeywords.Contains(Skyrim.Keyword.LocTypeHabitation) || locationKeywords.Contains(Skyrim.Keyword.LocTypeTemple) || locationKeywords.Contains(Skyrim.Keyword.LocTypeShip) || locationKeywords.Contains(Skyrim.Keyword.LocTypeGiantCamp) || locationKeywords.Contains(Skyrim.Keyword.LocTypeHagravenNest))
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
                    else if ( locationKeywords.Contains(Skyrim.Keyword.LocTypeDungeon) || locationKeywords.Contains(Skyrim.Keyword.LocSetCave) || locationKeywords.Contains(Skyrim.Keyword.LocTypeAnimalDen) || locationKeywords.Contains(Skyrim.Keyword.LocSetDwarvenRuin) || locationKeywords.Contains(Skyrim.Keyword.LocTypeDraugrCrypt) || locationKeywords.Contains(Skyrim.Keyword.LocSetNordicRuin) || locationKeywords.Contains(Skyrim.Keyword.LocTypeVampireLair) )
                    {
                        removalChance = chanceDungeon;
                    }
                    // Check for problematic locations
                    else if (placedObjectLocation.EditorID != null && problemLocationsP.TryGetValue(placedObjectLocation.EditorID, out int plChance))
                    {
                        removalChance = plChance;
                    }
                    // Check if Falskaar mod is present and use Falskaar LocTypes if it is
                    else if (state.LoadOrder.ContainsKey(falskaarMod))
                    {
                        // Check for Falskaar locations
                        foreach (var locType in locationKeywords)
                        {
                            
                                //Falskaar location types
                            if (!locType.TryResolve(state.LinkCache, out var locTypeRec)) continue;
                            if (locTypeRec.EditorID == null) continue;
                            if (fsLocations.TryGetValue(locTypeRec.EditorID, out int fsChance))
                            {
                                removalChance = fsChance;
                                break;
                            }
                        }
                    }
                    //Start step 3. Disable/Remove the placed food object       
                    //If RND < removal chance, copy as override into new plugin and set to initially disabled
                    Random rnd = new Random();
                    if (rnd.Next(100) < removalChance)
                    {
                        IPlacedObject modifiedObject = placedObjectGetter.GetOrAddAsOverride(state.PatchMod);
                        modifiedObject.MajorRecordFlagsRaw |= 0x0000_0800;
                        objsDisabled++;
                    }
                }
            }
            Console.WriteLine(objsDisabled + " Placed Food Objects Disabled");
        }
    }
}
