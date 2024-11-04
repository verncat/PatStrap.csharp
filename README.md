# PatStrapServer
My attempt to rewrote [Python server](https://github.com/danielfvm/Patstrap/tree/master/server) to C#
This project provide Haptic Feedback for VRChat

Python server have some issues with CPU load and memory leaks
(constantly after disconnect PatStrap or server running for a long time)

Currently, this program fully support original protocol, connects to VRChat OSC and auto discover PatStrap Hardware.


## Goals:
- GUI (in-progress)
- Auto discover PatStrap Hardware
- Support legacy protocol (from [danielfvm/Patstrap firmware](https://github.com/danielfvm/Patstrap/tree/master/firmware))
- Modern protocol and firmware which supports multiple haptic points (in-progress)
- Setup haptic points in GUI (in-progress)
- Dynamic change pointing mounts (in-progress)
- OpenXR haptic controller (in-progress)