# MyroP's PvPKit prefab - For VRChat worlds

![Showcase](https://github.com/MyroG/MyroP-PvPKit/blob/master/_DocImages/Header.png)

PvPKit is a prefab for VRChat worlds that allows you to easily add a PvP system to your world. At its current stage, the system is fairly basic, but I plan to expand it with additional features once I have more time.

The system also includes a join panel and a scoreboard system.

You can test the prefab in this world https://vrchat.com/home/world/wrld_ab6335cf-3891-4aa3-b7c2-21b7215f90d1/info

## How does it work?

Players can join the game by clicking the "Join" button. Once the game starts, they get automatically teleported to the Arena.
There's a 3 second immunity cooldown when the player just respawned, also a player who just respawned cannot shoot other players for 3 seconds.

Aim down to reload the weapon.

## Installation : Quick start

- VRCSDK 3.9.0 or above.
- Drag and drop the `PvP System` into your scene
- Select the `PvPMenu` object, which is located as a child of the `PvP System` prefab, and move it at whatever location you want.
- Place spawn points by creating a bunch of empty GameObjects. <ins>You should create as many spawn points as the number of players who can join the game!</ins>
- Select the `PvP System` prefab, navigate to the `PvP Game manager` component, and drag&drop the spawn points you created into the `Spawn Points` field
- See "Settings" to see how you can customize the prefab.

> [!NOTE]
> If you want to test the Demo scene, you need to install the "Lit" shader package by z3y https://github.com/z3y/shaders, otherwise you'll get the "Pink shader" error. I am just asking you to not reupload the scene as-is without making any major modifications, it is just a demo scene you can use for testing.

> [!TIP]
> By default, VRChat's player synchronisation is slow, you may experience the issue where you get killed by a player that hasn't appeared on the screen yet. The player syncing speed can be improved with additional packages, like Ikeiwa's BetterPlayerSync package https://github.com/Ikeiwa/BetterPlayerSync
> 
> If you also need synced footstep sound effects, you can install Vowgan's "Collision and Footstep Sounds" prefab https://booth.pm/ja/items/1966582

> [!WARNING]
> Do not disable the whole prefab in the world (using an in-world setting or a toggle). If the owner of that prefab disables it, the prefab won't work anymore. This is an issue I'll try to fix later.

## Settings

The prefab can be customized by playing around with some settings, they are scattered around through sereval GameObjects which I'll list below (you can search for them in the hierarchy):

![Tree](https://github.com/MyroG/MyroP-PvPKit/blob/master/_DocImages/Tree.png)

### PvP System

- **Spawn Points**: Your spawn points, which also define how many players can join your game, you should create as many spawn points as the number of players!
- **Respawn Point**: The location players get teleported once the game finished
- **Round Length in Seconds**: How long each round lasts. Once the timer reaches 0, players get teleported to the "Respawn Point" location
- **Show Player Capsule**: If you want to show the player capsule of each player. Note: when that setting is turned off, there's no way to know if players are immune or not
- **Locked by owner**: If checked, the game can only be started by the master of that instance
- **Immunity Time**: How long a player should remain immune if they respawn, and how long they need to wait until they can kill someone after they respawned
- **DesktopAimWithHead**: If checked, desktop players aim using their head, meaning the crosshair will always stay centered on the screen. If disabled, the weapon itself determines the aim direction, which is more realistic but may feel less comfortable for some users. You can test this behavior in Play mode.

### PlayerObject

- **Gun**: The Gun each player needs to be spawned with
- **Healthbar**: A reference to the healthbar object, if you do not want a healthbar you can delete that object
- **Colliders**: List of colliders attached to each player
- **Local Player Got Hit**: Audio clip that needs to be played if the local player got hit

### PvP Gun

- **Max Ammo In Mag**: Number of bullets per magazine.
- **Max Reserve Ammo**: Extra ammo outside the current magazine, so the ammo you're carying. If set to `-1`, the player has infinite reserve ammo. Currently, there's no way to pick up ammo!
- **Reload Time**: Time in seconds required to reload the weapon.

Accuracy:
- **Min Spread Deg**: Minimum bullet spread in degrees (maximum accuracy state).
- **Max Spread Deg**: Maximum bullet spread in degrees (minimum accuracy state).
- **Spread Recovery Speed While Shooting**: Rate at which bullet spread decreases while continuously firing.
- **Spread Recovery Speed While Idle**: Rate at which bullet spread decreases when not firing.
- **Spread Loss Per Shot**: Amount of spread added each time a shot is fired.
- **Gun Mesh**: Transform reference of the weapon model used for recoil animation.
- **GunOrigin**: Initial transform position for recoil animation.
- **GunBlownBack**: Maximum backward transform position reached during recoil.
- **Crosshair**: If you want to render a crosshair in VR, Desktop, both or no crosshair at all.

Audio:
- **Local Audio Manager**: Audio manager responsible for playing sounds for the local player.
- **Remote Audio Manager**: Audio manager responsible for playing sounds from remote players, like background SFX.
- **Shot Close**: Sound played when the weapon is fired nearby.
- **Shot Far**: Sound played when the weapon is fired at a distance.
- **Player Hit**: Sound played when a player is successfully hit.
- **Reload**: Sound played when the weapon reloads.
- **Volume Local Shooting**: Volume multiplier applied to local shooting sounds.
- **Volume Local Hit**: Volume applied to local hit sounds.
- **Volume Remote Shooting**: Volume applied to remote player's shooting sounds.

The Ammo UI will be positioned along the path between these two transforms:
- **Path Start**: Transform defining the starting position of the ammo UI.
- **Path End**: Transform defining the ending position of the ammo UI.  
  
### Barrel

That GameObject has a "Smoothing" script attached to it. If you do not want any smoothing, you can remove that component, but do not delete the whole GameObject

### PvPCrosshair

- **Crosshair Object**: Root GameObject that contains the full crosshair setup.
- **Branches**: Number of crosshair branches (typically the 4 directional elements).
- **Max Render Distance Crosshair**: Maximum distance at which the crosshair is placed in the world. Make sure that value is smaller than the camera's render distance
- **Crosshair Size On Screen**: Base size multiplier of the crosshair in screen space.

### ScoreboardScript

Not much to customize here, but you can have multiple scoreboards in the world (copies of the `ScoreboardWrapper` object), each scoreboard needs to be referenced under the `Scoreboard` array property

### ScoreboardWrapper

Not much to customize here, but you can add or delete rows in the scoreboard if needed. You can have less rows than the max number of players, for instance if you're only interested of showing the top 3 players.

### PlayerScaleEnforcer

Forces each player to have a certain scale in-game, even after switching avatars. If you do not want that behaviour, you can delete the GameObject.
- **Min Height In Meters**: Minimum allowed avatar height in meters. If the avatar is smaller than this value, it will be clamped to this minimum.
- **Max Height In Meters**: Maximum allowed avatar height in meters. If the avatar is taller than this value, it will be clamped to this maximum.

## Customize Gun

- The easiest way would be to duplicate an existing Gun prefab (like the grease gun or AK47)
- Place the new gun mesh as a child of the `GunRoot` mesh, and position it so the grips of both guns match. Then delete the old gun mesh.
- Move the `Barrel` GameObject in front of the barrel.
- There's a `MuzzleFlash` particle system, place it in front of the barrel.
- Customize the recoil by moving and rotating the GameObjects `GunOrigin` and `GunBlownBack` (which is the maximum backward transform position reached during recoil.)
- Place the newly created gun prefab under the `PlayerObject` GameObject, make sure the script attached to that `PlayerObject` GameObject references the new gun.
- Make sure you set all references on the `GunBase` script, otherwise the script may crash. The "Debug" property doesn't need to be set, but you can set it to see huge cubes as bullet impact points, good for debugging. To set each reference, I would recommend clicking on the little circle next to each property.


## Customize player collider

See GameObjects "Head" and "Body". You can attach multiple colliders on each player, and set a different damage multiplier for each of them.
Keep in mind that having multiple colliders impact performance, so if you want to create a huge PvP world that can accept 60 players or more, I would recommend using only one collider.

## Customize crosshair

- Easiest way would be to just replace the texture.
- Make sure the Pivot GameObject is never placed at (0,0,0) in local space, since the system uses its local position to determine the direction the crosshair branch can move. You can think of the local pivot position as the directional vector that defines the branch's movement direction. So, if the position is set to (0,0,0), the branch won't move at all
- A 3-Branch crosshair works, but see the second bullet point to understand how each branch moves.

## Credits and shoutouts
- Prefab created by MyroP
- Textures from ambientCG
- Gun models : https://3dmodelscc0.itch.io/free-cc0-guns-explosives-pack
- Audio : https://happysoulmusic.com/fire-weapons-sound-effects/

If you decide to add me to the credits, please use my VRChat username "MyroP". You can also include a link to this repository, for example: "PvPKit by MyroP – https://github.com/MyroG/MyroP-PvPKit/"

## License 

See the license file in each folder for details.

Unless stated otherwise, the code is licensed under the MIT License.  
Gun assets, textures, and audio files are licensed under CC0.

## Important note
This asset is still in beta, and there are more features I wanted to add later on.
I also started working on an API to ensure world creators can customize the PvP system without needing to modify the base assets, but it is not ready yet.

In the meantime, if you need to modify the prefab, feel free suggesting features by opening a new ticket. You can also submit PRs, but it is not guaranteed that I will merge every PR.

Planned features, or features I am interested to implement :
- Sniper riffle
- Possibility to switch guns
- Two handed weapons
- Possibility to grab ammo from ammo crates.
- Melee weapons