Set objShell = CreateObject("WScript.Shell")
Set objExec = objShell.Exec("""" & objShell.RegRead("HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 438100\InstallLocation") & "\VRChat.exe""" & " --url=create?roomId=1688623944&hidden=true&name=BuildAndRun&url=file:///C%3a%5cUsers%5cfdfpy%5cAppData%5cLocalLow%5cVRChat%5cVRChat%5cWorlds%5cscene-StandaloneWindows64-SampleScene.vrcw" & " --no-vr --enable-debug-gui --enable-sdk-log-levels --enable-udon-debug-logging --profile=2")
