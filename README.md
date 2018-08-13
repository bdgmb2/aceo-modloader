# Airport CEO ModLoader

ACEO Modloader is an open-source game patcher for Airport CEO that
allows for the C# UnityScript mods to override existing game code.

Modders can create a mod in C#, compile their mod into a library then
have it loaded into the game during startup with this loader.

**ACEO ModLoader Mods are compatible with Steam Workshop** (as of ACEO Alpha 27.4)

**Note for Mac Users**: This project will (supposedly) run on Mac, but you will need
the Mono runtime library. See the installation instructions for more information.

### I am a player who downloaded a mod that requires this, what do?
You probably downloaded a mod on the Steam Workshop that requires you to have ModLoader
installed for it to do anything. Follow these steps to get your mod working:

1. Go to the "Releases" tab at the top and download the `.zip` file for your platform
(Windows or Mac OSX)
2. If on Windows, move _all_ files in the compressed folder to your Airport CEO installation.
If on Mac OSX, drag the "ACEO ModLoader" app into your Applications folder
3. To start the game with mods, you MUST run the ACEO ModLoader application, NOT the normal Airport CEO
executable.

##### To Uninstall:
**Mac OSX**: Delete the "ACEO ModLoader" app from your Applications folder

**Windows**: Delete the `ACEOML.exe` file and the `MLL` folder from your Airport CEO installation directory.

### I want to start modding with ACEO ML, what do?
Check the [wiki](https://github.com/bdgmb2/aceo-modloader/wiki) for this project to learn how to get started.