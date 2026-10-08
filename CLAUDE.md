# FoodTruckCafe (Unity, iOS)

Unity 2022.3.50f1 2D portrait cafe game, shipped on the App Store as 26.03.29. This folder is the only source of truth; the Xcode exports live in `../Builds` and are not committed. The owner's other game (Godot, at Desktop/Godot) is unrelated; never mix the two.

Read `Docs/architecture.md` before touching gameplay code and `Docs/ios-testing-and-release.md` before anything build-related. Known issues and ideas are in `Docs/TODO.md`.

## Working rules

- Keep public method names used by scene buttons stable (`LoadNextLevel`, `RestartLevel`, `GoToMainMenu`, `TogglePause`, `OnNextLevelClicked`, `OnMainMenuClicked`, `PurchaseItem`, `ToggleAudio`, `ToggleMusic`). They are wired in scene YAML.
- Serialized field names on MonoBehaviours are also wired in scenes and prefabs; renaming one loses the reference. Add fields rather than rename.
- Input goes through the EventSystem; no `OnMouse*` handlers.
- Money is a float kept at whole cents through `SessionManager.RoundToCents`. Use `CanAfford` for comparisons.
- Food, character, and upgrade identifiers are strings matched across scripts and scene data. Grep before renaming.
- Do not commit `Library/`, `Temp/`, `Builds/`, `IOS_Build/`, `.sln`, `.csproj`. `.gitignore` covers them.

## Compile check without opening Unity

Unity writes a compiler response file with the exact references and defines. Rebuild the runtime assembly from the current sources with Unity's bundled csc:

```
S=/tmp/ftc-check && mkdir -p $S && { grep -v '^"Assets/' Library/Bee/artifacts/900b0aEDbg.dag/Assembly-CSharp.rsp | grep -v '^-out:\|^-refout:'; echo "-out:\"$S/Assembly-CSharp.dll\""; find Assets -name "*.cs" -not -path "*/Editor/*" | sed 's/.*/"&"/'; } > $S/main.rsp && /Applications/Unity/Hub/Editor/2022.3.50f1/Unity.app/Contents/NetCoreRuntime/dotnet /Applications/Unity/Hub/Editor/2022.3.50f1/Unity.app/Contents/DotNetSdkRoslyn/csc.dll @$S/main.rsp 2>&1 | grep "error CS"
```

No output means it compiles. If the `.dag` folder name differs, pick the newest one under `Library/Bee/artifacts`. Running Unity in `-batchmode` fails while the editor has the project open.

## Scene sanity

Always press Play from `MainMenu.unity`; the other scenes rely on managers it creates. The Shop scene is reached only after a level, so `SessionManager.Instance` is non-null there in normal play.
