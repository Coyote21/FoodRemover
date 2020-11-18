# FoodRemover
Skyrim Mutagen Patcher that iterates through all placed food objects in a cell and randomly disables them based on location type. Default is to disable all food from dungeons/ruins/crypt/etc. 25% of food objects from Shops/Inns and Farms. 35% of food objects from Dwellings/Temples/Habitations/etc. 75% of food objects from Military Camps/Forts/Bandit Camps/Forsworn/Warlock Caves/etc and 15% of food objects from Wealthy houses and Palaces. 

## Settings
Settings can be configured via several optional files in the Synthesis/Data folder.\

* percentages.json Sets the percentage of food to be removed from each location type.
* skipLocation.json Contains a list of location EditorIDs which should be skipped (I.e no food disabled in cells with this location)
* skipPlugins.json Contains a list of plugins whose cells should be skipped (I.e. no food disabled in these cells)
* specialLocations.json Contains a list of location EditorIDs which will receive the LocTypeSpecial percentage set in percentages.json
* custom.json Contains a list of location EditorIDs and percentages to set specific locations to a specific percentage

Examples of these files can be found in the Data folder located in the Git Repo

TLDR;

If you have a specific location you feel should have only 85% food removed, you can set "LocTypeSpecial": 85 in percentages.json and then add the location EditorID to the specialLocations.json and 85% of food objects in that cell will be disabled.
