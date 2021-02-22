# Valheim ServerSideMap

This plugin moves the map to the server side. Meaning that, when someone else is exploring, your map will be updated too. This applies while being offline too. Once you connect, your explored map will be synced with the server and the server will sync everything to your local client. The server holds the explored map data in a new file which is saved and loaded along with the world.

As of right now, this only applies to the map exploration, not the set markers. Those will be shared too in a update soon to follow.

## Roadmap

1. Clean up code, refactor it into proper classes
2. Address performance concerns about map up and down syncing
3. Update map sync to include map markers

## Installation

1. Place the ServerSideMap.dll inside your BepInX plugin folder on your **server and client**
2. Restart Server and Client
