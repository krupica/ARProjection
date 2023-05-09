# ARProjection
This repository contains projected user interface based on Unity that complements the [AREditor](https://github.com/robofit/arcor2_areditor) application when working with the [ARCOR2](https://github.com/robofit/arcor2) system.

## System setup
### Calibration
Calibration needs to be performed first. Proceed according to the instructions in [Calibration](https://github.com/krupica/ARProjection/tree/main/Calibration).

### Building application
The application needs to be [built](https://docs.unity3d.com/Manual/PublishingBuilds.html) using Unity. 

### Connecting
The final step is to connect to the ARServer by filling in the address and port on which it is running and clicking "connect." 

For the projection to work correctly, it is necessary to insert the action object "kinectAzure" into the scene and precisely set its position.