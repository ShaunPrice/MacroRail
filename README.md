# MacroRail
Macro photography application for Nikon cameras and [Pololu]( https://www.pololu.com/) TIC stepper controllers.

Copyright (c) 2023 Shaun Price

This file is part of MacroRail (https://github.com/ShaunPrice/MacroRail).
MacroRail is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
MacroRail is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of 
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
You should have received a copy of the GNU General Public License along with MacroRail. If not, see https://www.gnu.org/licenses/.

## Open source licensed software
This application uses copyrighted code from the following open-source projects:

### nikoncswrapper
Dot Net wrapper for the Nikon SDK to remotely control Nikon cameras.
https://sourceforge.net/projects/nikoncswrapper/
Creative Commons Attribution 3.0 Unported License

### libusb-win32
Generic Windows USB Library.
https://github.com/mcuee/libusb-win32
GNU Lesser General Public License as published by the Free Software Foundation; either version 2 of the License, or (at your option) any later version

### LibUSB
Wrapper to enable the application to communicate with the Pololu TIC stepper controller via USB.
https://github.com/LibUsbDotNet/LibUsbDotNet/tree/master
GNU Lesser General Public License v3.0

### TicDotNet
Library to enable the use of Dot Net to communicate with the Pololu TIC stepper controller.
https://github.com/jigarciacortazar/TicDotNet
GNU Lesser General Public License v3.0

### Newtonsoft Json.NET
Popular high-performance JSON framework for .NET.
https://www.newtonsoft.com/json
MIT License

## Closed source licensed software
The Nikon SDK is used throughout the application. The code is NOT open source and is not included in the Git repository!

### Nikon SDK
Nikon SDK that contains the library and documentation for remotely controlling Nikon cameras. The SDK also contains the MD3 files required to communicate with the cameras. Once downloaded you need to add the MD3 files and the dll's into the md3 directory in the project.
https://sdk.nikonimaging.com/apply/
Apply for a license to use the SDK to build this application. After agreeing to the license, you'll be sent an email link to download the SDK.

## NuGet Packages
The following NuGet packages are used in the Visual Studio build:

- libUsbDotNet by Travis Robinson,Stevie-O,Quamotion version 2.2.29
- Newtonsoft.Json by James Newton-King version 13.0.3
 

## Running the Application

### Open the application
Open the application and turn on you camera. If the camera is supported and connected via a USB cable it should automatically be connected and load all the supported setting. You can change the camera settings either on the camera on in the application.

![MacroRail Application with nothing connected](/images/MacroRail_Not_Connected.png)

If one or more of the features is not supported, it will not be enabled. In the example below, the D800 does not support Exposure Delay via USB. The D800 does support exposure delay in camera of 1, 2, and 3 seconds but the Nikon SDK does not support USB control of the setting. If you would like to use it, you must enable it on the camera directly.

![MacroRail Application with camera and TIC connected](/images/MacroRail_Connected.png)

### Configure the Application
It is very important that these parameters are set before starting the stepper. After the Pololu stepper controller has be set up you need to set up these parameters so the applications knows the distances that will be moved per step.

The following settings will configure most of the parameters for you if you're using a lead screw rod:

![MacroRail Application Settings](/images/MacroRail_Settings.png)

Where:

**Pitch (mm):** Distance between the threads.<br/>
**Thread Starts:** Number of starts on the thread.<br/>
**Steps per Revolution:** Number of steps per revolution for the stepper motor.<br/>
**Microsteps:** Microsteps set up in the Pololu TIC stepper motor controller.<br/>
**Gear Ratio:** Gear ration if you're using a gearbox between the stepper motor and the lead screw. If there's no gearbox use 1 for a 1:1 ration.<br/>

Clicking on "Calculate" will give you the "Steps per mm".

Add the Maximum Speed in pulses per second. You'll know if it's set to high because the stepper will make a buzzing noise and won't move. Don't make it so quick that your camera flies off the macro rail.

The Defaul Job Speed (%) sets the jog speed to the desired percentage of the maximum speed on start-up. The jog speed can be changed in the application main screen.

Other setting are set up in the Pololu application for configuring the TIC. Make sure you use acceleration and de-acceleration values low enough that you won't be jerking the camera.

**Note:** These setting are saved with the application.

### Connect the TIC Stepper Motor Controller
Connect the Pololu TIC stepper motor controller to your computer via USB and click on the "TIC Connect" button. The TIC should automatically be detected, and the parameters will show. If your camera is also connected or the "Don't Shoot Camera" checkbox is checked, you will also have the green "Start" button enabled.

### Calculate the settings you require
The Calculator will give you rough settings to get you started. I would recommend shooting more images (Step Count) and a smaller Step Size than indicated in the calculator.

The calculator settings are as follows:

**Camera Sensor Size:** Pitch the type of camera you are using. In the future camera's other than Nikon's may be incorporated as a manual or automatic option.<br/>
**Lens Focal Length:** Size of you lens in mm.<br/>
**Macro Tube Size:** The length of your macro tubes if you're using them (e.g. 10mm, 20mm, 50mm, ...).<br/>
**Aperture:** Aperture your camera is set at.<br/>
**Distance to Subject:** Distance from the front of the lens to the subject being photographed.<br/>
**Subject Depth:** The distance from the front most point you want to shoot to the rear most point you want to photograph. You can find this by "Toggling Liveview" on and jogging the camera forward until the most rearward part you want to photograph is in focus. Click on "Halt and Set Start Position" to zero the current position indicator and then jog the camera backwards until camera focus is at the last point you want to shoot. The distance shown for the current position is your subject distance.<br/>

Click calculate and use the Shots recommended as a guide to the minimum shots for the "Step Count" and the "xxxmm per step" as the "Step Size (mm)" value. The actual distance moved will be the Step Count times the Step Size (e.g. 10 steps with ta step size of 0.5mm will move the camera from the front to back 5mm).

## Projects

You can save your configuration for thte camera and the shots into a project file. Go to the menu File/Save As (Ctrl+Shift+S) and enter the details below. After clicking of the "Save: button another dialog box will ask you where you want to save the *.proj file that will save all the project information. The folder will be used to save all the shoots if you don't change it when shooting. 

![MacroRail Application Save As dialog](/images/MacroRail_Save_As.png)

## Shooting

Once your camera and TIC stepper driver has connected you can click on the green "Start" button, enter the details in the dialog box below and start your shoot. The progress will be display below the stop button with any camera information showing in the status bar at the bottom left of the application and TIC controller messages show next to the "TIC Disconnect" button.

At the end of the shoot the camera will return to where it started, and a dialog will pop-up telling you the shoot has finished. 

![MacroRail Application Start Shooting dialog](/images/MacroRail_Start_Shooting.png)

Where:

**Shoot Name:** The name of your shoot. This is different from the name of your project if you saved a project. A subfolder will be created in the folder you have set for the images directory. If you set up a project this will be the same as the project folder unless you change it in the "Images directory" text box on this screen.<br/>
**Shoot Version:** Version information for your shoot. A subdirectory will be created in the image directory. If you don't change this for each shoot the files will be overwritten.<br/>
**Images directory:** Root directory to save the images into. The image will be saved in the [images directory][Shoot Name]/[Shoot Version]/ directory. If you created or opened a project this will default to the project directory. If you haven't you will need to set this by typing in a directory or clicking on the button to the right of the text box "...".<br/>

**Note:** If you're shooting JPEG images, either by themselves or with RAW images, the shot will be shown in the image area in the middle of the screen.

## About

Provides information about the application, copyright, version, closed source libraries, and open source libraries used.

![MacroRail Application Start Shooting dialog](/images/MacroRail_About.png)

# Setting up you Pololu TIC Stepper Motor Controller

You can purchase you stepper motor controller from Pololu who make the TIC version this application was designed to drive This controller has advanced features that allow you to control the stepper motor in a variety of ways.

The following are website for selecting and purchasing your TIC from Pololu:

	Choose your Pololu TIC: https://www.pololu.com/category/212/tic-stepper-motor-controllers
	TIC user 's guide: https://www.pololu.com/docs/0J71
	TIC configuration software for Windows: https://www.pololu.com/docs/0J71/3.1

**Note:**  The Pololu TIC can be run from many platforms, but at tis stage the application only runs on Windows x64bit due to the Nikon libraries used. This is planned to be changed in the future.

My solution currently runs a [NEMA 23 stepper motor](https://www.makerstore.com.au/product/elec-nema23-635-b/) from the [Maker Store](https://www.makerstore.com.au/) in Australia. This motor is completely oversized for the applcation, but it's intended for other purposes othethan macro-protography.

The rail I used is the [Maker Store](https://www.makerstore.com.au/) [C-Beam Double-Wide 500 Actuator Kit](https://www.makerstore.com.au/product/kit-actuator-cb-d/), but if you're looking to buy one for yourself the 250mm kit should be fine for most applications.

![Macro Rail Hardware](/images/Macrorail_photo.png)

For the front and rear limit switch I used a couple of normally open proximity sensors. These are connected to the TIC controller and are used to stop the camera at the front and rear of the rail. The proximity sensors are lj8a3-2-z/bx inductive proximity sensors. These are 5VDC sensors and are connected to the TIC controller TX & RX inputs.

The settings from the TIC Control Center software from Pololu that I used are shown below - including the limit switches. These are the settings that work for my setup, but you may need to change them for your setup.

![Pololu TIC Control Center - Input & Motor Settings](/images/Pololu-TIC-Control-Center-Input-&-Motor-Settings.png)

![Pololu TIC Control Center - Advanced Settings](/images/Pololu-TIC-Control-Center-Advanced-Settings.png)

## 3D Printed Parts

While my set-up does use 3D printed parts they were very specific to my setu. I also made a lot of custom aluminium parts and Manfrotto quick release camera mount plate and a video head and mount plate, but you don't need all of these parts for a simple stepper solution. You don't require the limits and can turn them off in the Pololu TIC configuration if you don't use them.

## Future releases

At some point I'll make all the required Pololu TIC settings configurable from the application, but for now you'll need to use the Pololu TIC Control Center software to set them up.
