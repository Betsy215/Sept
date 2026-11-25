#!/bin/bash
# One test build from the open editor: export through the bridge, archive, upload to TestFlight.
#   bash Docs/tools/test_build.sh <version>-<build> [upload]      e.g. 2026.11.21-0        (run in the background; 5 to 20 minutes)
#   bash Docs/tools/test_build.sh <version>-<build> upload        only the upload, for an archive that exists
# The version and build number must already be set in the Player Settings (bundleVersion, iPhone build number).
# After it: reset only `preloadedAssets:` to `[]` in ProjectSettings/ProjectSettings.asset. Never `git checkout`
# that file: it reverts bundleVersion and the build number too (24 Nov 2026 an upload was rejected that way).
# Upload needs the Apple account signed in under Xcode > Settings > Accounts ("Failed to Use Accounts" = signed out;
# only the owner can sign in). Processing: Docs/tools/asc_token.py + GET /v1/builds?filter[app]=6755611355&sort=-uploadedDate.
V="$1"; [ -n "$V" ] || { echo "usage: test_build.sh <version>-<build> [upload]"; exit 1; }
P=/Users/betzthebest/Desktop/Sept/FoodTruckCafe; BUILDS=/Users/betzthebest/Desktop/Sept/Builds; A=$BUILDS/archives/FoodTruckCafe-$V.xcarchive
cd $P || exit 1
if [ "$2" != "upload" ]; then
  T=$(mktemp -d); sed "s#/Users/betzthebest/Desktop/Sept/Builds/iOS-26.10.18-1#$BUILDS/iOS-$V#; s#export-26.10.18-1.result#export-$V.result#" Docs/tools/build_in_editor.cs > $T/build.cs
  rm -f Docs/tools/.mcp_session $BUILDS/export-$V.result
  python3 Docs/tools/unity_mcp.py exec Docs/tools/recipes/ap/stop.cs >/dev/null 2>&1; sleep 4
  echo "export start $(date +%H:%M:%S)"; python3 Docs/tools/unity_mcp_long.py exec $T/build.cs >/dev/null 2>&1    # the call may answer success:false early; the result file is the truth
  for i in $(seq 1 180); do [ -f $BUILDS/export-$V.result ] && break; sleep 10; done
  echo "export: $(cat $BUILDS/export-$V.result 2>/dev/null) $(date +%H:%M:%S)"; grep -q Succeeded $BUILDS/export-$V.result || exit 1
  cd $BUILDS/iOS-$V || exit 1
  echo "version $(/usr/libexec/PlistBuddy -c 'Print :CFBundleShortVersionString' Info.plist) ($(/usr/libexec/PlistBuddy -c 'Print :CFBundleVersion' Info.plist))"
  rm -rf "$A"
  xcodebuild -workspace Unity-iPhone.xcworkspace -scheme Unity-iPhone -configuration Release -destination generic/platform=iOS -archivePath "$A" archive -allowProvisioningUpdates DEVELOPMENT_TEAM=XY39924J9T CODE_SIGN_STYLE=Automatic ALWAYS_EMBED_SWIFT_STANDARD_LIBRARIES=NO > $BUILDS/archives/archive-$V.log 2>&1
  tail -3 $BUILDS/archives/archive-$V.log | grep -E "ARCHIVE (SUCCEEDED|FAILED)" || { echo "archive failed"; tail -20 $BUILDS/archives/archive-$V.log; exit 1; }
fi
[ -d "$A" ] || { echo "no archive $A"; exit 1; }
rm -rf /tmp/ftc-upload-$V
xcodebuild -exportArchive -archivePath "$A" -exportOptionsPlist $BUILDS/archives/exportOptions-upload.plist -exportPath /tmp/ftc-upload-$V -allowProvisioningUpdates > $BUILDS/archives/upload-$V.log 2>&1
grep -E "EXPORT SUCCEEDED|EXPORT FAILED|Upload succeeded|Failed to Use Accounts|session has expired|PLA Update|Pre-Release Train" $BUILDS/archives/upload-$V.log | head -6
echo "done $(date +%H:%M:%S)"
