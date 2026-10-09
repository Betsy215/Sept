# FoodTruckCafe (Unity, iOS)

Unity 2022.3.50f1 2D portrait cafe game, shipped on the App Store as 26.03.29. This folder is the only source of truth; the Xcode exports live in `../Builds` and are not committed. The owner's other game (Godot, at Desktop/Godot) is unrelated; never mix the two.

Read `Docs/architecture.md` before touching gameplay code and `Docs/ios-testing-and-release.md` before anything build-related. Known issues and ideas are in `Docs/TODO.md`.

## Working rules

- **This game is live on the App Store and works.** Change code only to fix a real, confirmed bug. No refactors, no re-tuning, no clean-ups for their own sake; write those up in `Docs/TODO.md` instead. Keep every fix surgical and compile it (command below).
- Things the owner has explicitly said to leave alone are listed under "Leave alone" in `Docs/TODO.md`.
- Keep public method names stable. Only three are wired in scene YAML as Inspector `onClick` calls: `ShopManager.LoadNextGameLevel`, `ConfirmPurchase`, `CancelPurchase` (plus the Settings prefab's `Animator.SetTrigger("Show")`). Everything else (`LoadNextLevel`, `RestartLevel`, `GoToMainMenu`, `TogglePause`, `PurchaseItem`, `ToggleAudio`, `ToggleMusic`) is wired from code, but renaming still means updating those call sites.
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

## Unity editor link (MCP for Unity)

Claude Code is registered to the bridge at http://127.0.0.1:8080/mcp (local scope, Desktop/Sept folder). Unity starts the bridge itself on load (Auto-Start is on); if `claude mcp list` shows it unreachable, open Window, MCP for Unity in Unity and click Start Server. Tools: `manage_editor` (play/pause/stop), `read_console` (pass include_stacktrace=false and a filter_text; entries are huge otherwise), `manage_camera` screenshot with include_image=true (Captures/ is gitignored), `execute_code` (runs C# in the editor), `manage_scene`, `manage_gameobject`, `manage_components`, `run_tests`. Always Stop play mode when done. Telemetry is disabled in EditorPrefs.

