# FoodTruckCafe

A portrait 2D cafe game for iPhone and iPad, built in Unity 2022.3.50f1. Customers walk up to a food truck, order one to four items, and the player taps the right food in time. Coins buy new foods, characters, and upgrades in the shop between levels. Monetised with Unity Ads (a rewarded "watch to earn coins" button in the shop).

Released on the App Store as version 26.03.29 (March 2026).

## Where things are

| Path | What |
| --- | --- |
| `Desktop/Sept/FoodTruckCafe` | This Unity project and its git repo |
| `Desktop/Sept/Builds/iOS-<version>` | Xcode exports (not committed) |
| `Assets/Scenes` | MainMenu, GameSceneOne, Shop, KitchenScene (loaded additively over GameSceneOne) |
| `Assets/Scripts` | All game code, grouped by system (see `Docs/architecture.md`) |
| `Assets/Entity` | Level tuning as ScriptableObjects, Level1Data to Level14Data |
| `Assets/Prefabs` | Customers, food items, AudioManager, Settings, shop item |
| `Assets/Editor` | Editor-only tools, including the iOS post-build step |
| `Docs/` | Architecture notes, iOS release guide, to-do list |

## Open and run

1. Open `Desktop/Sept/FoodTruckCafe` from Unity Hub with 2022.3.50f1.
2. Open `Assets/Scenes/MainMenu.unity` and press Play. The other scenes expect the persistent managers that MainMenu creates, so always start from MainMenu.

## Build for iOS

Follow `Docs/ios-testing-and-release.md`. Short version: bump Version and Build in Player Settings, turn off Development Build, build into `Desktop/Sept/Builds/iOS-<version>`, open the `.xcworkspace`, archive, upload.

## Docs

- `Docs/architecture.md`: scene flow, the persistent singletons, how levels, orders, scoring, saving, and ads fit together, and where to change things.
- `Docs/ios-testing-and-release.md`: step-by-step device testing, TestFlight, App Store submission, screenshots, and the fixes for every error hit so far.
- `Docs/TODO.md`: known issues, suggestions, and the plan for banner or interstitial ads.

## Git

Only source is tracked. `Library`, `Temp`, `obj`, `Builds`, and Xcode exports are ignored. Commit after each working change and push to `https://github.com/Betsy215/Sept`.
