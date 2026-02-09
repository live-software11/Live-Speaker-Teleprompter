# R-Speaker Teleprompter - Bitfocus Companion Module

## Installation

1. Copy the `companion-module` folder to your Companion modules directory
2. Restart Companion
3. Add a new connection and search for "R-Speaker Teleprompter"

## Configuration

- **Target IP**: IP address of the computer running R-Speaker Teleprompter (default: 127.0.0.1)
- **OSC Port**: Port for sending commands (default: 8000)
- **Feedback Port**: Port for receiving status updates (default: 8001)

## Available Actions

### Playback Control
- **Play/Start**: Start teleprompter scrolling
- **Stop/Pause**: Stop teleprompter scrolling  
- **Reset**: Reset to beginning of text

### Speed Control
- **Set Speed**: Set specific speed value (1-10)
- **Speed Increase**: Increase scroll speed
- **Speed Decrease**: Decrease scroll speed

### Text Display
- **Set Font Size**: Set specific font size (20-200)
- **Font Increase**: Increase font size
- **Font Decrease**: Decrease font size
- **Mirror Toggle**: Toggle mirror mode

### Navigation
- **Next Script**: Load next script in list
- **Previous Script**: Load previous script
- **Load Script**: Load specific script by index
- **Jump to Top**: Jump to beginning of text
- **Jump to Bottom**: Jump to end of text
- **Set Position**: Set scroll position (0-100%)

## Available Feedbacks

- **Playing Status**: Shows green when teleprompter is playing
- **Current Speed**: Displays current speed value
- **Mirror Status**: Shows blue when mirror mode is active

## Preset Buttons

Pre-configured buttons are available for common functions:
- Play (Green)
- Stop (Red)
- Reset (Blue)
- Speed Up/Down (Orange)

## OSC Commands Reference

All commands use OSC protocol. You can also control the teleprompter with any OSC-compatible software.

| Command | OSC Address | Arguments |
|---------|------------|-----------|
| Play | `/teleprompter/start` | none |
| Stop | `/teleprompter/stop` | none |
| Reset | `/teleprompter/reset` | none |
| Set Speed | `/teleprompter/speed` | int (1-10) |
| Set Font Size | `/teleprompter/font/size` | int (20-200) |
| Set Position | `/teleprompter/position` | float (0.0-1.0) |
| Mirror Toggle | `/teleprompter/mirror/toggle` | none |

## Troubleshooting

1. **Connection Issues**: Ensure R-Speaker Teleprompter is running and OSC server is enabled
2. **No Response**: Check firewall settings allow UDP traffic on configured ports
3. **Wrong IP**: If running on same computer, use 127.0.0.1
4. **Port Conflicts**: Try different port numbers if defaults are in use

## Support

For issues or feature requests, please visit the R-Speaker Teleprompter GitHub repository.
