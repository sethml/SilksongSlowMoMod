# SlowMo Mod

A simple, configurable slow-motion mod for *Hollow Knight: Silksong*.

Perfect for grandpas and other older folks whose fast-twitch reflexes have long since departed, allowing you to play like the youths.

## Features

- **Toggle Slow-Mo**: Instantly slow down time to help with difficult platforming or combat.
- **Adjustable Speed**: Cycle through speed presets (e.g., 80%, 65%, 50%) on the fly.
    - *Expert Mode*: You can also set speeds > 100% (e.g., 150%) to challenge yourself!
- **Pause Compatible**: Safely interacts with game pauses and cutscenes.
- **Configurable**: Customize keybindings, default speed, and speed presets via configuration.

## Installation

1.  **Prerequisite**: Ensure you have [BepInEx](https://github.com/BepInEx/BepInEx) installed for *Hollow Knight: Silksong*.
2.  Download the latest `SlowMo.dll` release.
3.  Place `SlowMo.dll` into your game's `BepInEx/plugins` folder.
    - Example: `.../Hollow Knight Silksong/BepInEx/plugins/SlowMo.dll`
4.  Launch the game.

## Usage

| Action | Default Key | Description |
| :--- | :--- | :--- |
| **Toggle Slow-Mo** | `Right Shift` | Enable or disable slow motion. |
| **Increase Speed** | `=` (Equals) | Cycle up through speed presets. |
| **Decrease Speed** | `-` (Minus) | Cycle down through speed presets. |

*Note: You can change these keybindings in the configuration file generated at `BepInEx/config/com.slowmo.cfg` after the first launch.*

## Building from Source

See [BUILD.md](BUILD.md) for instructions on how to build the mod yourself.

## Attribution

This mod was vibe coded by **Seth LaForge**, using **Cursor** and **Antigravity**.

## License

MIT License

Copyright (c) 2025 Seth LaForge

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.
