# Valheim ServerSideMap

This plugin completely moves the explored map and created pins to the server. 
As clients explore, they will send their explored areas to the server who will then
distribute it to all connected clients. When a client joins, the server will synchronize the 
currently explored areas to the client. Pins are shared as well and but default to false and need to be enabled. 
When pin sharing is used, all newly created pins are send to the server who saves them along with the 
explored area. 

## Installation

0. This mod requires BepinEx
1. Place the ServerSideMap.dll inside your BepInX plugin folder on your **server and client**
2. Restart Server and Client

## How it works 
####(If you didnt read any other text, please at least read this one)
#### **Marker Share:**
1) **MARKERS DEFAULT TO FALSE AND NEED TO BE ENABLED IF YOU WANT TO USE THAT FEATURE**
2) The config file will be created after your first launch with the new version. You can edit the config files inside BepInEx/config/eu.mydayyy.plugins.serversidemap.cfg
3) **YOU NEED TO RESTART AFTER EDITING THE CONFIG**
4) Existing markers from clients are not synced to the server
5) Every newly created marker will  only exist on the server, not on the client
6) When a client connects, he downloads all markers from the server
7) When the client disables the sharing of markers, he will opt out of it, others on the server continue to share their markers
8) When the server turns off marker share, no client will be able to share their markers no matter what they set inside their config file

#### **Map Share:**
1) As usual, existing explored areas are synced to the server and merged
2) The server sends the current map exploration to the client and the client merged it
3) When the client disables the sharing of map data, he will opt of it, others on the server continue to share their map exploration
4) When the server turns off map share, no client will be able to share their map exploration no matter what they set inside their config file 

The pin and exploration data is saved along with the map in a new file.

You can toggle marker and map share separately, refer to Marker Share  2&3 for instruction

## Bug Reports
Please use [Github](https://github.com/Mydayyy/Valheim-ServerSideMap/issues) for bug reports and feedback.

## Development
Development takes place on github: https://github.com/Mydayyy/Valheim-ServerSideMap

You need to copy the following dlls into the Libs folder:

- 0Harmony.dll
- assembly_utils.dll
- assembly_valheim.dll
- BepInEx.dll
- BepInEx.Harmony.dll
- UnityEngine.CoreModule.dll
- UnityEngine.dll
- UnityEngine.ImageConversionModule.dll
- UnityEngine.UI.dll