# LoadNormalizer
This is a mod for standardizing load times, for use in races.
## Settings
LoadNormalizer has two settings: "DefaultLoadTime" and "DefaultBossLoadTime"
- For a standard room, if the load finishes faster than "DefaultLoadTime", LoadNormalizer will wait the remaining duration before allowing the game to continue.
- For some rooms with bosses, there is a subscene containing the boss which loads separately. That load is timed separately according to "DefaultBossLoadTime", in the same manner described above.

Settings can be edited in the "LoadNormalizer.GlobalSettings.json" file in the save folder.

Settings can be viewed as the LoadNormalizer version: the first number is the "DefaultLoadTime" and the second is the "DefaultBossLoadTime".
## Finding the best settings
LoadNormalizer creates a file named "loadTimes.yaml" in the save folder, which accumulates data as you go through loads with LoadNormalizer enabled. This file can be opened with any text editor. Each block has the following fields:
- sceneName: the room which was loaded
- FetchBlocked: the amount of the time the game waited before starting to load. For normal scenes, this is roughly 0.48 seconds, and is tied to the camera fade.
- Fetch: this is the main part of the load, and the part which corresponds to "DefaultLoadTime". You should choose "DefaultLoadTime" to correspond to the largest "Fetch"
- ActivationBlocked: this is the time after Fetch where LoadNormalizer waits for the "DefaultLoadTime" threshold to be reached. Rarely, the base game may also contribute time here in places such as stags.
- Activation, UnloadUnusedAssets, GarbageCollect, StartCall: these are other parts of the load with very small contributions (~0.01-0.05 seconds). Occasionally they may fluctuate to larger values, but is rare. These cannot be normalized globally due to this inconsistency, and the added problem that after Activation, any wait will affect the room cycles.
- LoadBoss: This field occurs only for scenes which have a boss, and represents the time controlled by "DefaultBossLoadTime". Note that this includes the time where LoadNormalizer waits, so it can only be properly measured when the time is greater than "DefaultBossLoadTime".

TLDR: look at "Fetch" and "LoadBoss" to select the settings. Be aware that "LoadBoss" will not display values smaller than "DefaultBossLoadTime".

Alternatively, you can look in the ModLog. LoadNormalizer will print a warning message whenever the load thresholds are exceed by 0.05 seconds. If there are no messages, your thresholds are high enough (and can possibly be lowered).