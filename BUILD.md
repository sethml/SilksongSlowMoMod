# Building SlowMo

## Prerequisites

1. **.NET SDK** (version 6.0 or later)
   - Download from: https://dotnet.microsoft.com/download
   - Verify installation: `dotnet --version`

2. **Hollow Knight Silksong** with **BepInEx** installed
   - Make sure BepInEx is properly set up in your game directory

## Build Steps

### Option 1: Using the Build Script (Easiest)

1. Build only:
   ```bash
   ./build.sh
   ```

2. Build and install:
   ```bash
   ./build.sh --install
   ```

The `--install` flag will copy the DLL to `/Applications/Hollow Knight Silksong/BepInEx/plugins/`.

### Option 2: Using dotnet CLI (Manual)

1. Open a terminal in the project directory:
   ```bash
   cd /Users/sethml/src/SlowMo
   ```

2. Restore NuGet packages:
   ```bash
   dotnet restore
   ```

3. Build the project:
   ```bash
   dotnet build
   ```

4. The compiled DLL will be in: `bin/Debug/SlowMo.dll`

### Option 2: Using Visual Studio / Rider / VS Code

1. Open the `SlowMo.csproj` file in your IDE
2. Restore packages (usually automatic)
3. Build the project (F6 in Visual Studio, Ctrl+Shift+B in VS Code)
4. The DLL will be in `bin/Debug/SlowMo.dll`

## Installing the Mod

1. Copy `bin/Debug/SlowMo.dll` to your game's BepInEx plugins folder:
   ```
   Hollow Knight Silksong/BepInEx/plugins/SlowMo.dll
   ```

2. Launch the game - the mod should load automatically

3. Check `BepInEx/LogOutput.log` if you encounter any issues

## Configuration

After first run, a config file will be created at:
```
BepInEx/config/com.ofb.slowmo.cfg
```

You can edit this file to customize:
- Slow motion key (default: LeftShift)
- Slow motion speed (default: 0.3)
- Toggle mode (default: false = hold to activate)

## Troubleshooting

- **Build errors**: Make sure you have .NET SDK installed and NuGet packages restored
- **Mod doesn't load**: Check that BepInEx is properly installed and the DLL is in the plugins folder
- **Game crashes**: Check `BepInEx/LogOutput.log` for error messages

