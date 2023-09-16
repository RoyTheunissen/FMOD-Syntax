[![Roy Theunissen](Documentation~/Github%20Header.jpg)](http://roytheunissen.com)
[![License: MIT](https://img.shields.io/badge/License-MIT-brightgreen.svg)](LICENSE.md)
![GitHub Follow](https://img.shields.io/github/followers/RoyTheunissen?label=RoyTheunissen&style=social) ![Twitter](https://img.shields.io/twitter/follow/Roy_Theunissen?style=social)

_Generates code to allow invoking FMOD events with a strongly-typed syntax._

## About the Project

Out-of-the-box FMOD requires you to access events and parameters via inspector references or by name. Neither of these setups is ideal.

Dispatching an event this way requires you to define a bunch of fields, assign things in the inspector, and then the syntax for setting parameters is weakly-typed.
You can skip some of this by dispatching events/setting parameters by name but that has the drawback that you need to look up what the name is and it's very hard and error-prone to rename anything as it'll break references.

For ease of use it's preferable if FMOD events and parameters are accessed in a strongly-typed way such that they're known at compile-time.

This requires a little bit of code generation, and that's where `FMOD-Wrapper` comes in. With a simple setup wizard and one line of code in your audio service for culling expired events you can start dispatching FMOD events with parameters in as little as one line of code.

![Example](Documentation~/Generated%20Code%20Files.png)

This setup allows events and parameters to be renamed gracefully as you can do it via your IDE, update your banks accordingly and then re-generate the code. If events are renamed in the banks and you still reference those events by their old names, warnings will be thrown to give you a chance to refactor without immediately getting compile errors.

Overall this system significantly speeds up your audio implementation workflow and makes it more robust, at the expense of a little bit of boilerplate code that you won't even have to maintain yourself.

## Getting Started

- Install the package to your Unity project
- The Setup Wizard will pop up and allow you to specify where and how to create the settings file & save generated code files
- Configure the system as desired and press Initialize
- Use `FMOD > Generate FMOD Code` or `CTRL+ALT+G` to generate the FMOD code
- Cull FMOD expired playback instances by calling `FmodWrapperSystem.CullPlaybacks();` in an `Update` loop somewhere. I recommend putting this in your audio service.
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
// Looping sound
TestContinuousPlayback testContinuousPlayback = AudioEvents.TestContinuous.Play(transform);

float value = Mathf.Sin(Time.time * Mathf.PI * 1.0f).Map(-1, 1);
testContinuousPlayback.Strength.Value = value;

testContinuousPlayback?.Stop();

// Cancelling loops in OnDestroy
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

![image](https://github.com/RoyTheunissen/FMOD-Wrapper/assets/3997055/1effbe29-d228-40b9-852e-96741396f0b4)

For convenience, FMOD Wrapper generates an enum for every event with a labeled parameter, so auto-complete conveniently suggests all valid values when you are invoking the event.

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

Furthermore, the enums in FMOD may actually represent an enum in your game. So it's inconvenient to have to map from that game enum to the FMOD enum. But luckily there's a solution for that: **User-specified labeled parameter enums**.

Simply tag your game enum with `[FmodLabelEnum]` and specify the names of the labeled parameters that it represents, and then when code is generated instead of generating event-specific enums, it uses the game enum you specified for those events.

No more duplication, no more mapping.

```cs
[FmodLabelEnum("Surface")]
public enum SurfaceTypes
{
    Generic,
    Dirt,
    Rock,
}
```

## Compatibility

This system was developed for Unity 2021 and upwards, it's recommended that you use it for this version.

If you use an older version of Unity and are running into trouble, feel free to reach out and I'll see what I can do.

## Known Issues
- No support for snapshots yet, but this will be added soon
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
openupm add com.roytheunissen.fmod-wrapper
```

### Manifest
You can also install via git URL by adding this entry in your **manifest.json**

```
"com.roytheunissen.fmod-wrapper": "https://github.com/RoyTheunissen/FMOD-Wrapper.git"
```

### Unity Package Manager
```
from Window->Package Manager, click on the + sign and Add from git: https://github.com/RoyTheunissen/FMOD-Wrapper.git
```


## Contact
[Roy Theunissen](https://roytheunissen.com)

[roy.theunissen@live.nl](mailto:roy.theunissen@live.nl)
