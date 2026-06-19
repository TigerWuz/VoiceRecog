# VoiceRecog

VoiceRecog is a Windows application that allows you to control **Microsoft Flight Simulator 2020** and **Microsoft Flight Simulator 2024** using voice commands.

The application uses the built-in Windows Speech Recognition engine and sends **SimConnect** or **Mobiflight** events directly to the simulator.

---

## Features

- 🎤 Voice control for MSFS 2020 & 2024
- ⚙️ Simple YAML-based command configuration
- ✈️ Supports standard SimConnect events
- 🔧 Supports Mobiflight events
- 🎙️ Enable or disable voice recognition using voice commands

---

## Requirements

- Microsoft Flight Simulator 2020 or 2024
- .NET 8 Desktop Runtime: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.19-windows-x64-installer
- SimConnect

---

## Windows SmartScreen

VoiceRecog is currently not digitally code-signed. Because of that, Windows may display a SmartScreen warning when you launch the application for the first time. 
If you downloaded the release from this GitHub repository, you can click More info → Run anyway.

---

## Configuration

All voice commands are configured in the `voice_commands.yml` file.

Each command contains a spoken phrase and either an `action` or an `event`.

### Internal actions

These commands control the speech recognition itself.

```yaml
commands:
  - phrase: "copilot aus"
    action: "DisableRecognition"

  - phrase: "copilot an"
    action: "EnableRecognition"
```

---

### Simulator events

Any SimConnect or Mobiflight event can be triggered.

```yaml
commands:
  - phrase: "autopilot on"
    event: "AUTOPILOT_ON"

  - phrase: "autopilot off"
    event: "AUTOPILOT_OFF"

  - phrase: "magneto on"
    event: "MAGNETO_START"

  - phrase: "magneto off"
    event: "MAGNETO_OFF"
```

---


## Complete Example

```yaml
commands:
  - phrase: "copilot off"
    action: "DisableRecognition"

  - phrase: "copilot on"
    action: "EnableRecognition"

  - phrase: "autopilot on"
    event: "AUTOPILOT_ON"

  - phrase: "autopilot off"
    event: "AUTOPILOT_OFF"

  - phrase: "magneto on"
    event: "MAGNETO_START"

  - phrase: "magneto off"
    event: "MAGNETO_OFF"

```

---

## Recent Improvements

- Added YAML-based command configuration
- Added internal actions (`EnableRecognition` / `DisableRecognition`)
- Improved reconnect stability
- Fixed pause/resume exception while speech recognition was active
- Improved window layout scaling

---

## Planned Features

- Better reconnect handling
- Improved logging
- Parameterized voice commands
