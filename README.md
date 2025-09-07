This repository is a Voice Recognition program to control your aircraft in MSFS (either 2020 or 2024). <br>

The voice commands are defined using a text file voice_commands.yml. A couple of examples are given in there:

You can now turn the voice recognition off with voice command "Co pilot off" and on with "Co pilot on". This ensures that no unwanted voice recognition takes place when this is not required.  

<code>
VOICE COMMANDS
gear up: Mobiflight.GEAR_UP
gear down: Mobiflight.GEAR_DOWN
flaps up: Mobiflight.FLAPS_DECR
flaps down: Mobiflight.FLAPS_INCR
autopilot on: AUTOPILOT_ON
autopilot off: AUTOPILOT_OFF
</code>
<br>
The first part of the line contains the actual voice command, after the : follows the event to be send to the simulator. This can be Mobiflight events or standard simconnect events. FSUIPC events will follow in a later version.
<br/><br/>


The program required .Net 8 desktop runtime, which can be downloaded here: https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-8.0.19-windows-x64-installer
