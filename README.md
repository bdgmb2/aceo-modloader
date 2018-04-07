# Airport CEO ModLoader

ACEO Modloader is an open-source game patcher for Airport CEO that
allows for the existence of mods that override game source code, giving
modders much greater freedom to modify the game.

Modders can create a mod in C#, compile their mod into a library then
have it loaded into the game during startup with this loader.

**Note for Mac Users**: This project currently only works on Windows, but a Mac version may
be coming soon. Read below for more information.

### For Players

Note: ACEO ModLoader is in an early state of development. While it should work,
you may encounter bugs. If you encounter a bug or other undesirable behavior, 
please submit an issue to this repository at the top of the page.

##### How to Install ModLoader:
1. Download the latest release package for your platform in the "Releases" tab
2. Unzip the release package and place all files in your Airport CEO game directory
3. Find your Airport CEO Path - Locate your Steam installation, then navigate to `steamapps/common/
Airport CEO`. Copy this path.
4. Set ModLoader to launch instead of the game in Steam. Right click on Airport CEO in your library and
click "Properties". Then click on "Set launch options...". Now, paste in your path to Airport CEO inside
quotes and add "ModLoader.exe" on the end. Your final path should look something like:
```C:/Steam_Installation/steamapps/common/Airport CEO/ModLoader.exe```
Click OK.
5. You're done. Start the game by launching "ModLoader.exe". You should get a command prompt
window with some logging information, then the game will start. Do **NOT** close the command prompt
window, just leave it alone.
6. You will know ModLoader is working when you see a ModLoader message underneath the game version number 
in the top-right corner once in-game.

##### How to Uninstall ModLoader:
1. Delete the "ModLoader" and "mods" folders. Then delete "ModLoader.exe" and all the files that sound similar to
"Mono.Cecil"
2. Remove everything from the Launch Options dialog for Airport CEO in Steam
2. You're done. Your game should be back to vanilla.

##### How to Install Mods:
Mods go inside the `mods` folder in the root of your Airport CEO game directory. Mod developers should
distribute their mod as a folder or zip file. Move this folder inside the `mods` folder, or optionally -
if the developer distributed a zip file - unzip the file into the `mods` folder.

##### How to Uninstall Mods:
Delete the mod from the `mods` folder.

##### FAQ:
`Will this break the game if it updates?`
No. Only in extreme circumstances will ModLoader not run (see developer FAQ for more info).
This means ModLoader should work when a game update comes out. Mods, however, may not work properly.

`What does this allow modders to do?`
Previously, modders could only modify the game through the official "MDK" packs Apoapsis Studios released
on their [website](https://www.airportceo.com/modding/)

`Are there any mods to try out yet?`
In the "Releases" tab at the top of this repository, you can find 2 sample mods to install and play with - one
changes the bus companies in the game to more realistic real-world ones, the other doubles the fast-forward to next
day speed.

`What happens when Steam Workshop comes out?`
It depends if Apoapsis Studios decides to create a C# modding API for their game. If they do, this loader
will become redundant and I will start to decomission it. If not, I believe this project will still have
a reason to exist.

`Where's the Mac Version?`
I built this project on Windows with Windows in mind using a framework that's not available (in its current form)
on Mac or Linux. However, work is underway to port the patcher to the same library that Unity uses to run C# code
on your Mac.

### For Modders

Using this project, you can create a game-modifying library and load it into Airport CEO safely. Unlike more mature
modding projects like [SMAPI](https://github.com/Pathoschild/SMAPI) for Stardew Valley and the Cities Skylines API, 
ACEO ModLoader does *not* have it's own API (yet), so you must override game functions yourself using reflection. This is 
quite tedious on it's own, so I highly recommend the use of [Harmony](https://github.com/pardeike/Harmony) or another 
reflection library that can make development easier. Don't forget to keep [ILSpy](https://github.com/icsharpcode/ILSpy) handy!
If you need help or inspiration, I've bundled a couple of sample mods with the project using Harmony.

##### Folder Structure and Distribution
Distributing a mod for ACEO ModLoader is actually pretty easy. ModLoader creates a "mods" folder inside the ACEO root
directory. This folder contains one subfolder for each mod. Inside the subfolder is (at minimum) one library/dll file with the
same name as the mod. This library gets loaded first at startup. **Note:** The folder and library name _must_ match!

###### Structure Diagram:
```
|-- Airport CEO
|-- mods
|   +-- SampleMod
    |   +-- SampleMod.dll
    |   +-- Resources, etc.
```

###### Required Functions
ModLoader only requires your mod to have 2 functions inside one class. There *must* be a class called "Main", and it
*must* have 2 public static void functions called GameInitializing() and GameExiting()

One gets called at the start of the game, one gets called when the game is exiting. If they are not present inside your
mod, ModLoader will throw an error. More API style functions that you can use may follow as ACEO ModLoader grows. 
For now, I recommend using Harmony (see above) to override game functions.

###### Debugging Mods
ModLoaderLibrary includes a class called LogOutput with a function called Log() that you can use to output messages
to ModLoader's log file. Just make sure you reference ModLoaderLibrary when building your mod! You can view ModLoader
log output inside the ModLoader folder in the "output.log" text file.

### For ModLoader Developers

##### Prerequisites:
To build this project locally and contribute, you will need 
- A legally obtained copy of Airport CEO
- [Visual Studio 2017](https://visualstudio.com) (at least Community)
- .NET Framework 4.0 (Unity - to my knowledge - uses the Mono equivalent of .Net 4)
- [Harmony](https://github.com/pardeike/Harmony), another open-source library allowing function modification without
writing to disk (Harmony is distributed with this project, as there is no NuGet package available for it)
- [Mono.Cecil](http://www.mono-project.com/docs/tools+libraries/libraries/Mono.Cecil/) (Included as a NuGet package with this project)

##### Compiling Locally:
1. Clone this repo
2. Open the solution in Visual Studio
3. *Ensure* all project references are accounted for, otherwise the build will fail.
   1. Make sure to `Restore NuGet Packages` before building
   2. Copy over all required references into the `ACEOLibs` folder inside the project root.
   At this time, this includes `UnityEngine.dll`, `UnityEngine.UI.dll`, `Assembly-CSharp.dll`, and
   `UnityEngine.CoreModule.dll`. You can find these libraries in the "Airport CEO_Data/Managed" folder.
4. Build the solution (Build => Build Solution or `Ctrl+Shift+B`)

##### FAQ:
`How does it work?`
There are two projects inside the solution: ModLoaderNetFramework and ModLoaderLibrary.
- **ModLoaderNetFramework** is an executable patcher that runs before Airport CEO starts. It makes a backup of the game's 
UnityScript library (Assembly-CSharp.dll), then patches 2 functions in the library, serving as the "entry point" and "exit point"
of the ModLoader. It then starts the game. When the game shuts down, the patcher then reverts the library back. The main() function
should be pretty easy to follow.

- **ModLoaderLibrary** is where the real magic happens. There are two primary functions, `Entry()` and `Exit()`, both of which
are pretty self-explanatory. `Entry()` searches the mods folder for assemblies to load into the game. `Exit()` doesn't really do
much right now, but can in the future.

`Why write a patcher instead of just distributing the modified library?`
- The patcher itself only modifies 2 functions in the early parts of game startup. If the game updates,
the patcher will most likely still work, and there's no need to redistribute a modified library for a constantly changing
early access title.
- I can't think of a faster way to get a DMCA takedown than distributing modified official game code

`Where's the Mac Version... Again?`
I wrote the patcher in the .NET framework, which only runs on Windows. I tried writing it originally in .NET Core, but 
complications arose in the patching methods because Unity does *NOT* use .NET Core and the patched symbols couldn't be 
recognized. To get a Mac version, the ModLoaderNetFramework patcher will probably need to be rewritten in Mono so a Mac
executable can be built. If you're interested in working on this, let me know.

`Why even make this?`
Good question.

##### Contributing
Wow thanks, I thought you'd never scroll all the way down to this part. First of all, God help you. Second, you probably
will take one look at my code and devise a better or more elegant solution for what I'm doing. *PLEASE* send a pull request
with your feature additions, bugfixes, etc. I have no ego.
