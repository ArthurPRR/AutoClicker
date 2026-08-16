# AutoClicker

A Windows desktop application for automating mouse clicks and keyboard actions. Record sequences of clicks and keystrokes, then replay them with customizable repeat counts and global hotkeys.

## Features

- **Auto Clicking**: Automate repetitive mouse clicking tasks
- **Keyboard Support**: Record and replay keyboard actions with modifier keys (Ctrl, Shift, Alt)
- **Sequence Recording**: Record complex sequences of mouse clicks and keyboard presses
- **Global Hotkeys**: Use configurable hotkeys to start/stop automation without switching windows
- **Repeat Control**: Set a specific number of times to repeat the automation or run indefinitely
- **Sequence Playback**: Play back recorded sequences of actions
- **User-Friendly GUI**: Simple Windows Forms interface for easy configuration

## Requirements

- Windows 10 or later
- .NET 10.0 or later (Windows Desktop Runtime)

## Installation

1. Clone this repository:
   ```bash
   git clone https://github.com/yourusername/AutoClicker.git
   cd AutoClicker/AutoClicker
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Run the application:
   ```bash
   dotnet run
   ```

Or run the compiled executable from the build output folder.

## Usage

### Basic Auto-Clicking

1. Launch the AutoClicker application
2. Set your desired click interval (in milliseconds)
3. Set the number of clicks (or leave empty for infinite)
4. Press the configured toggle hotkey to start/stop clicking
5. Press the configured stop hotkey to immediately halt automation

### Recording Sequences

1. Click the "Record" button
2. Perform the mouse clicks and keyboard actions you want to automate
3. Click the "Stop Recording" button when finished
4. Review your recorded sequence in the interface
5. Click "Play" to replay the sequence with your configured repeat count

### Hotkey Configuration

- **Toggle Hotkey**: Starts and pauses the auto-clicker
- **Stop Hotkey**: Immediately stops all automation

Hotkeys work globally, even when the application window is not in focus.

## Project Structure

```
AutoClicker/
├── Form1.cs              # Main UI form with click logic
├── Form1.Designer.cs     # Form designer generated code
├── AutoClickerLoopState.cs  # State management for click loops
├── Program.cs            # Application entry point
└── AutoClicker.csproj    # Project configuration
```

## Technologies Used

- **Language**: C# 12
- **Framework**: .NET 10.0
- **UI**: Windows Forms
- **Interop**: Win32 API (keyboard and mouse hooks)

## License

See the LICENSE file for details.

## Contributing

Contributions are welcome! Feel free to open issues or submit pull requests to improve the project.

## Disclaimer

This tool is provided for legitimate automation purposes only. Users are responsible for ensuring their use complies with applicable laws and terms of service of any software or services they interact with using this tool.
