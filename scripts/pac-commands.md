
# CLI-first workflow with Power Platform CLI (pac)

> Prereqs: Install Power Platform CLI (pac) and .NET SDK; sign in to a Dataverse environment.

```powershell
# 1) Auth & Set the active environment
pac auth create --url "https://YOUR-ENV.crm.dynamics.com" --cloud Public --name Dev
pac auth select --name Dev

# 2) Create a solution (or reuse an existing one)
mkdir solution
cd solution
pac solution init --publisher-name Contoso --publisher-prefix bkg --solution-name BankingOps

# 3) Add the plug-in project as a reference so it gets packaged into the solution
pac solution add-reference --path ..\src\BankingOps.Plugin\BankingOps.Plugin.csproj

# 4) Build the plugin (root of repo)
cd ..\..
 dotnet build .\src\BankingOps.Plugin\BankingOps.Plugin.csproj -c Release

# 5) Pack and import the solution (managed or unmanaged)
cd solution
pac solution pack --process CanvasApps --zipfile .\BankingOps_Managed.zip --process-steps build
pac solution import --path .\BankingOps_Managed.zip --publish-changes true
```

> Registering **steps** (messages, stages, pre/post images) still requires either the **Plug-in Registration Tool** or the **Power Platform Tools** in Visual Studio. After assembly import, open the tool and register your steps with your actual logical names from `Schema.cs`. See `scripts/register-steps.md` for a copy-paste checklist.
