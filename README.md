[![Roy Theunissen](Documentation~/Github%20Header.jpg)](http://roytheunissen.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE.md)
![GitHub Follow](https://img.shields.io/github/followers/RoyTheunissen?label=RoyTheunissen&style=social) ![Twitter](https://img.shields.io/twitter/follow/Roy_Theunissen?style=social)

_Generates code to allow invoking FMOD events with a strongly-typed syntax._

## About the Project

Out-of-the-box FMOD requires you to access events and parameters via inspector references or by name.
Neither of these setups is ideal.

Dispatching an event this way requires you to define a bunch of fields, assign things in the inspector, and then the syntax for setting parameters is weakly-typed.
You can skip some of this by dispatching events/setting parameters by name but that has the drawback that you need to look up what the name is and it's very hard and error-prone to rename anything as it'll break references.

For ease of use it's preferable if FMOD events and parameters are accessed in a strongly-typed way such that they're known at compile-time. Maybe something like this:

```cs
AudioEvents.PlayerJump.Play(transform);
```

This would require a little bit of code generation, and that's where `FMOD-Syntax` comes in. With a simple setup wizard and one line of code in your audio service for culling expired events you can start dispatching FMOD events with parameters in as little as one line of code.

![Example](Documentation~/Generated%20Code%20Files.png)

This setup allows events and parameters to be renamed gracefully as you can do it via your IDE, update your banks accordingly and then re-generate the code. If events are renamed in the banks and you still reference those events by their old names, warnings will be thrown to give you a chance to refactor without immediately getting compile errors.

Overall this system significantly speeds up your audio implementation workflow and makes it more robust, at the expense of a little bit of boilerplate code that you won't even have to maintain yourself.

## Getting Started

- Install the package to your Unity project
- The Setup Wizard will pop up and allow you to specify where and how to create the settings file & save generated code files
- Configure the system as desired and press Initialize
- Use `FMOD > Generate FMOD Code` or `CTRL+ALT+G` to generate the FMOD code
- Cull expired FMOD playback instances by calling `FmodSyntaxSystem.CullPlaybacks();` in an `Update` loop somewhere. I recommend putting this in your audio service.
- You can now fire your FMOD events in a strongly typed way

## How to use

### One-offs
```cs
// Parameterless one-off sound (global)
AudioEvents.TestOneOff.Play();

// Parameterless one-off sound (spatialized)
AudioEvents.TestOneOff.Play(transform);

// Spatialized one-off with parameters
AudioEvents.Footstep.Play(transform, FootstepPlayback.SurfaceValues.Generic);
```

### Loops
```cs
// Start a looping sound
TestContinuousPlayback testContinuousPlayback = AudioEvents.TestContinuous.Play(transform);

// Set the parameter on a looping sound
float value = Mathf.Sin(Time.time * Mathf.PI * 1.0f).Map(-1, 1);
testContinuousPlayback.Strength.Value = value;

// Stop a looping sound
testContinuousPlayback?.Stop();

// Cancel a looping sound in OnDestroy
testContinuousPlayback?.Cleanup();
```

### Global Parameters
```cs
// Setting global parameters
AudioGlobalParameters.PlayerSpeed.Value = value;
```

### Dispatching a parameterless event that is chosen via the inspector
```cs
// Get a reference to the event that you can assign via the inspector
[SerializeField] private AudioConfigReference parameterlessAudio;

// Dispatch as per usual
parameterlessAudio.Play(transform);
```

### Labeled parameter enums
In FMOD there are three types of parameters: "Continuous" (`int`), "Discrete" (`float`) and "Labeled" (`enum`).

Labeled parameters are sent and received as integers, but they are associated with a name for convenience, exactly like enums in C#.

![image](https://github.com/RoyTheunissen/FMOD-Syntax/assets/3997055/280da27d-abde-4faf-b260-0a2591ea4d29)

For convenience, FMOD Syntax generates an enum for every event with a labeled parameter, so auto-complete conveniently suggests all valid values when you are invoking the event.

```cs
AudioEvents.Footstep.Play(transform, FootstepPlayback.SurfaceValues.Generic);
```

But let's say you have several events that share the same "Surface" parameter, for instance a Jump and a Footstep event.
It could be inconvenient if you just want to determine the current surface type, and then fire jump and footstep events with that type,
because you would have to cast it to the appropriate event-specific enum.

```cs
AudioEvents.Footstep.Play(transform, FootstepPlayback.SurfaceValues.Generic);
AudioEvents.Jump.Play(transform, JumpPlayback.SurfaceValues.Generic);
```

Furthermore, the enums in FMOD may actually represent an enum in your game. So it's inconvenient to have to map from that game enum to the FMOD enum. But there's a solution for that: **User-specified labeled parameter enums**.

Simply tag your game enum with `[FmodLabelType]` and specify the names of the labeled parameters that it represents, and then when code is generated instead of generating event-specific enums, it uses the game enum you specified for those events.

No more duplication, no more mapping.

```cs
[FmodLabelType("Surface")]
public enum SurfaceTypes
{
    Generic,
    Dirt,
    Rock,
}
```
```cs
AudioEvents.Footstep.Play(transform, SurfaceTypes.Generic);
AudioEvents.Jump.Play(transform, SurfaceTypes.Generic);
```
#### Scriptable Object Collection Support

The above 'Labeled parameter enums' feature now also works for the [Scriptable Object Collection](https://github.com/brunomikoski/ScriptableObjectCollection). Simply add the `[FmodLabelType]` attribute to the Scriptable Object Collection Item the same way you would with an enum.

_**NOTE:** If you installed FMOD-Syntax or ScriptableObjectCollection to the `Assets` folder instead of via the Package Manager / in the Packages folder, a `SCRIPTABLE_OBJECT_COLLECTION` scripting define symbol will not be defined automatically and you will have to manually add this to the project settings for this feature to work._



### Moving/renaming Events
FMOD-Syntax has a two-tiered solution for allowing you to move/rename events in FMOD and update your code accordingly:
- **Alias Generation** - When an event is detected as having been moved or renamed, 'aliases' are generated under the old name, tagged with an `[Obsolete]` attribute that informs you what the event is currently called. This causes the game to throw a compile warning everywhere that the old incorrect syntax is used, and you can copy/paste the correct name from the warning. This lets you manually migrate your event code without compile errors and with reminders for what the new syntax is.
- **Auto-Refactoring** - When an event is detected has having been moved or renamed, you are also presented with the option to Auto-Refactor. If chosen, FMOD-Syntax will look through your .cs files, find any references to the old events and automatically refactor them to use the new event syntax.

### Syntax Formats
FMOD-Syntax provides three syntax formats for defining events:
- **Flat**: All events are defined in `AudioEvents`. Simplest / shortest syntax, but event names have to be unique.<br />
  An event called `Player/Footstep` is invoked via `AudioEvents.Footstep.Play();`
- **Flat (With Path Included In Name)**: Slightly longer names, but event names don't have to be unique. Requested by @AldeRoberge <br/>
  An event called `Player/Footstep` is invoked via `AudioEvents.Player_Footstep.Play();`
- **Subclasses Per Folder**: Subclasses are generated inside the `AudioEvents` class for every folder holding their events. Longer syntax, but events are neatly organized and names don't have to be unique.
  An event called `Player/Footstep` is invoked via `AudioEvents.Player.Footstep.Play();`

The default syntax is `Flat`, and you are recommended to use that and give unique names to your events.

For example, you can use the following naming convention: `Object_Event`. That way you can have a footstep event for both a player and a monster without getting a name clash, and it doesn't matter if the event is in a folder called `Core/World1/Gameplay/Characters/Enemies/Monster/Monster_Footstep`, because including all those folders in the name is cumbersome.

However, as you may be integrating FMOD-Syntax into an existing project and may not have the ability to affect the naming convention of events much, we recognize that different projects have different structures and you are free to choose whatever syntax suits your project best.

Switching syntax formats supports all the same migration features as renaming or moving events.

## Compatibility

This system was developed for Unity 2021 and upwards, it's recommended that you use it for these versions.

If you use an older version of Unity and are running into trouble, feel free to reach out and I'll see what I can do.

## Known Issues
- There is a setup for automatically regenerating the code when the FMOD banks update, but this would require you to modify the FMOD Unity plugin so that feature is currently disabled.


## Installation

### Package Manager

Go to `Edit > Project Settings > Package Manager`. Under 'Scoped Registries' make sure there is an OpenUPM entry.

If you don't have one: click the `+` button and enter the following values:

- Name: `OpenUPM` <br />
- URL: `https://package.openupm.com` <br />

Then under 'Scope(s)' press the `+` button and add `com.roytheunissen`.

It should look something like this: <br />
![image](https://user-images.githubusercontent.com/3997055/185363839-37b3bb3d-f70c-4dbd-b30d-cc8a93b592bb.png)

<br />
All of my packages will now be available to you in the Package Manager in the 'My Registries' section and can be installed from there.
<br />


### Git Submodule

You can check out this repository as a submodule into your project's Assets folder. This is recommended if you intend to contribute to the repository yourself.

### OpenUPM
The package is available on the [openupm registry](https://openupm.com). It's recommended to install it via [openupm-cli](https://github.com/openupm/openupm-cli).

```
openupm add com.roytheunissen.fmod-syntax
```

### Manifest
You can also install via git URL by adding this entry in your **manifest.json**

```
"com.roytheunissen.fmod-syntax": "https://github.com/RoyTheunissen/FMOD-Syntax.git"
```

### Unity Package Manager
```
from Window->Package Manager, click on the + sign and Add from git: https://github.com/RoyTheunissen/FMOD-Syntax.git
```


## Contact
[Roy Theunissen](https://roytheunissen.com)

[roy.theunissen@live.nl](mailto:roy.theunissen@live.nl)
