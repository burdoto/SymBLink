# SymBLink
## A tool for all binge-downloading Sims 4 CC maniacs
This tool assists you when downloading a LOT Sims 4 CustomContent.
You will no longer be required to manually sort mods into the Mods directory of your Sims 4 installtion. 

_Note: Currently, only ZIP archives are supported. Other types like RAR or 7Z cannot be extracted and will be ignored._

#### Usage
- Download a for you suitable release [here](https://github.com/comroid-git/SymBLink/releases/latest)
- Extract the release and launch SymBLink.exe
  - Be careful, all files included in the release archive are required!
- Set up the Application using the "Configure..." option icon in the task bar icon's menu
  - Your Download directory should be the directory where you download your CC archives
  - Your Sims directory should be the "Electronic Arts/The Sims 4" directory in your Documents, NOT the Mods folder 
- When downloading a ZIP file to the mirrored directory, it will be extracted and all `.package` and `.ts4script` files will be moved to a subdirectory in your Mods directory.

_Note: All `.package` and `.ts4script` files being added to the monitored directory are copied directly into the Mods directory._

#### Good Practice
- The subdirectory in your Mods directory will be named after the Name of the ZIP archive without the `.zip` extension.
  - When using a "Save As..."-style downloading in your browser, you can define the subdirectory's name by changing the archive title.

#### Upcoming Features
- More Archive types (`.rar`, `.7z`)
- Rescanning monitored directory for Mod files
- When the archive's structure is unclear, a single-click window will open, allowing you to select what you actually need from the archive.
- Custom Copy configurations + Sims 3 support
