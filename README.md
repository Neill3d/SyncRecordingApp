# SyncRecordingApp

 This is an application to syncronize recording commands between Optitrack Motive and Rokoko Studio

## Optitrack Motive XML broadcasting
 When triggering via XML messages, the Remote Trigger setting under Advanced Network Settings must be set to true.

 Motive could receive xml message but also send them. The SyncRecordingApp has modes to control which stage you want to have.
 In case receiving xml commands option is enabled, then a received command from the Motive will be redirected to a Rokoko Studio
 When Send XML commands options is enabled, then by triggering start and stop recording commands in the app, the xml messages will be send to Motive

More information about Optitrack Motive xml commands could be found here - https://v22.wiki.optitrack.com/index.php?title=Data_Streaming#Remote_Triggering

## Rokoko Studio
 Command API have to be enabled and a key value have to be 1234

More information about Rokoko Studio remote commands - https://docs.rokoko.com/rokoko-studio/data-export-and-live-streaming/command-api

## Sync Recording App Shortcuts

* x to start recording, you have to enter a new clip name and press enter
* z to stop recording 
* c to calibrate, only applied for Rokoko Studio, default calibration warm up time is 3 seconds
* r to toggle receive xml commands from Optitrack Motive
* s to toggle send xml commands to Optitrack Motive
* v to toggle verbose, to print more details about outgoing processes
* q to exit

## Settings file

 In the folder of an executable you should have ini settings file which could help to customize communication ports and parameters. If the file is not presented, it will be created on a first app run.
