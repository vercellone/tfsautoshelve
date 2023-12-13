#tfsautoshelve

##TFS Auto Shelve Extension for Visual Studio

 Protect your code by guaranteeing your pending changes are always backed up to the TFS server.

---
### v7.1 - [Download](https://github.com/F11ppy/tfsautoshelve/tree/master/VSIXReleases/TfsAutoShelve.7.1.vsix)

Visual Studio 2022 compatible version.

[F11ppy](https://github.com/F11ppy): I only updated the references and install targets to get it running with VS 2022. This worked for me, but I can't guarantee everything is working or it's neatly done!
Maybe [@vercellone](https://github.com/vercellone) will merge these changes or update the solution (in the right way); I'm new to GitHub and not sure how everything is working here. :)

---
### v7.0

Visual Studio 2017+2019 compatible version.

---
### v6.2

Corrected Install targets to include Visual Studio 2017 Professional.

---
### v6.1

Removed IntelliTrace PreRequisite because it was blocking installation for VS Community and Professional edition users.

---
### v6.0

Visual Studio 2017 compatible version.

1. Auto shelving occurs at the configured intervals if you have _any_ solution open (source controlled or not) in one or more VS instance with 2 exceptions, of which both require a manual toggle to resume:
  1. You toggle it off manually.
  2. If a significant error occurs (typically a TFS connection error), then it will stop to avoid incessant errors.
2. Shelvesets are no longer created if nothing has changed since the last Shelveset.
  1. If you choose to "Shelve Now" (Ctrl-T), the delta detection is bypassed.  A Shelveset is created even if nothing has changed.
  2. If the number of pending changes is not equal to the number of items in the latest Shelveset, a Shelveset is created.
  3. If any single pending change exists that is not in the latest Shelveset, a Shelveset is created.
  4. If any single pending change exists with an UploadHashValue not equal to the HashValue of the local item's current content, a Shelveset is created.
  5. If any single pending change exists and the local item does not exist (deleted), the UploadHashValue is compared to the HashValue at Checkout time.
3. A Shelveset is no longer created upon initialization.  Instead it will be deferred until the first configured interval has lapsed.
4. Shelveset Name now supports {3} and {4} as placeholders for the workspace owner name's domain and user values split respectively.  Since slashes are invalid/stripped this allows you to use {3}-{4} or whatever other valid delimiting character you choose.
5. Interval was changed from an integer to a double to allow the number of minutes to be specied as a decimal.  Useful for testing if nothing else.
6. SuppressDialogs/MessageBoxes removed: The only MessageBox remaining is a conditional warning on the Tools->Options page that never considered the SuppressDialogs setting.
7. TfsAutoShelve is exposed as a global service, available from other VSPackages:
  1. IAutoShelveService shelvesvc = Package.GetGlobalService(typeof(SAutoShelveService)) as IAutoShelveService;
---

####What it does

*   Automatic Shelving shelves _all_ your pending changes while you are coding
*   Manual Shelving shelves _all_ your pending changes anytime with a single menu click or Ctrl-T hotkey
*   Uses one Shelveset per workspace to shelve all pending changes to the TFS Server (assuming you include {0} in the Shelveset Name)
*   Shelvesets are re-used to save the latest version of pending changes (unless the Shelveset Name is customized to include a date stamp {2})

####Functionality

*   Automatic Shelving
    *   Begins when _any_ solution is opened in Visual Studio
		*  Team Menu allows you to turn on/off automatic shelving
            *   Team Menu -> TFS Auto Shelve (Running)
			*   Team Menu -> TFS Auto Shelve (Not Running)
*   Manual Shelving
    *   Can be triggered any time as long as you are connected to TFS
        *   Team Menu -> TFS Auto Shelve Now
*   Options
    *   Tools Menu -> Options -> TFS Auto Shelve Options
        *   _PauseWhileDebugging_ - **New in v4.0**: When enabled, shelving will not occur while debugging and will shelve immediately on return to design mode
        *   _Interval_ - The interval (in minutes) which automatic shelving will occur
        *   _Shelveset Name_ - string.Format input expression for deriving the unique Shelveset name.  By default it is "Auto-{0}" where {0}=WorkspaceInfo.Name, {1}=WorkspaceInfo.OwnerName, {2}=DateTime.Now, {3}=Domain of WorkspaceInfo.OwnerName, {4}=UserName of WorkspaceInfo.OwnerName. 
**WARNING: If you are upgrading be warned that if your Shelveset name does not include {0} and you use multiple workspaces then only 1 workspace's changes will be saved.**
        *   _Maximum Shelvesets _- **New in v3.3**: Maximum number of Shelvesets to retain. Older Shelvesets will be deleted.  Note: ShelvesetName must include a {2}(DateTime.Now
 component) unique enough to generate more than the maximum for this to have any impact.  If {0} (WorkspaceInfo.Name) is included, then the max is applied per workspace.
**Note**: The ShelvesetName expression supports Composite Formatting.  For example, {2:hh} if you want to include just a 2 digit hour in the name.  Or, {2:yyyyMMdd} for a sortable date value.

####Helpful Info

*   To view Shelvesets, open Source Control Explorer, click on:
    *   File > Source Control > Unshelve Pending Changes
*   Workspaces can be modified by:
    *   Opening the Source Control Explorer > Clicking on Workspaces drop down > Click Workspaces > Click Add/Edit/Remove
*   Custom Visual Studio Activity Logging is implemented. If you run into any errors, please startup Visual Studio with the /log switch, re-create the error, then close Visual Studio. You can browse to "%AppData%\Microsoft\VisualStudio\14.0\ActivityLog.XML" 
 to view the log.
