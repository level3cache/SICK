# SICK - Steam Integrity Checker
This is a sick program with one sick feature: Show you what's wrong with your game.

## Usage
1. **Run the executable** (currently Windows only)
2. **Select your game** by entering the corrosponding index number shown in the terminal
3. **Review the results:** The program compares your game directory against the manifest files provided by steam to identify missing or extra files (including support for DLCs and Language Packs)

## Results
Console Output will show you all missing and/or extra files
- **Missing files:** Use Steams "Verify Integrity" feature to redownload them
- **Extra files:** Simply delete these manually from your folder if no longer need

**Notes:** Some games store logs, replays, streamed assets and possibly more in the install directory. These are usually identifiable by their name and are (mostly) safe to keep while achieving a "clean installation".

## Why this project?
Simple: I live in germany and my internet speed is 5 MiB/s. (Re-)Ddwnloading large games (sometimes 100GB+) takes more than 5 hours... So I was left with two options: Either wait 5 hours just to break my game by modding it again or drive to a friend of mine (20 minutes away) and copy the files over (if he even was home and owned the game).
But now not anymore! (unless it's a new game my friendgroup wants to play 🥲).

## Dependencies
In order to run SICK at runtime, [.NET 10 Runtime](dot.net) or higher is required.

## Credit
- [SteamKit](https://github.com/SteamRE/SteamKit) - To deserialize depot manifest files
- [Gameloop.vdf](https://github.com/shravan2x/Gameloop.Vdf) - To deserialize 'libraryfolders.vdf' and app manifest files 
