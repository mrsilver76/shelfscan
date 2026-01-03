# ShelfScan

<p><img src="https://img.shields.io/badge/Windows-supported-0078D6?logo=windows&logoColor=white" alt="Windows"> <img src="https://img.shields.io/badge/Linux-supported-FCC624?logo=linux&logoColor=black" alt="Linux"> <img src="https://img.shields.io/badge/macOS-supported-000000?logo=apple&logoColor=white" alt="macOS"> <img src="https://img.shields.io/badge/.NET-C%23-512BD4?logo=dotnet&logoColor=white" alt=".NET/C#"> 
<img src="https://img.shields.io/github/license/mrsilver76/shelfscan?logo=gnu&logoColor=white" alt="GPL License"> <img src="https://img.shields.io/github/downloads/mrsilver76/shelfscan/total" alt="total downloads"></p>

_A cross-platform command-line tool (Windows, Linux, macOS) for scanning a media library and reporting on Plex naming compliance._

## 📚 Overview

ShelfScan scans a folder (either locally or on a network) and generates a detailed report indicating how compliant the files are with Plex's naming conventions. It identifies issues with file names, folder structures, and multi-episode formatting for both movies and TV shows. Support for music is not yet implemented.

**ShelfScan does not modify or rename any files.** It's purpose is solely inspection and reporting of file/folder naming issues.

>[!CAUTION]
>- **This is a very early beta release.** There may be mistakes in the filename validation.
>- **File name checks are very strict**. Issues identified by ShelfScan does not necessarily mean there is a problem with it within Plex.

## 🧰 Features

- 💻 Runs on Windows 10 & 11, Linux (x64, ARM64, ARM32), and macOS (Intel & Apple Silicon).
- 📂 Scans local and network folders for TV shows and movies.
- 🛡️ Read-only scanning – your files are never modified.
- 📝 Generates a detailed compliance report showing valid and invalid files.
- ⚠️ Strict file format checking to ensure consistency with Plex naming conventions.
- 🔍 Detects multi-episode formatting issues and folder structure inconsistencies.
- 🧪 Test folder containing dummy movies and TV shows for validation.
- 🛠️ Early beta with user feedback encouraged via GitHub.
- 📚 Includes links to official format specs and resources for reference.

## 📦 Download

Get the latest version from https://github.com/mrsilver76/shelfscan/releases.

Each release includes the following files (`x.x.x` denotes the version number):

|Platform|Download|
|:--------|:-----------|
|Microsoft Windows 10 & 11|`ShelfScan-x.x.x-win-x64.exe` ✅ **Most users should choose this**|
|Linux (64-bit Intel/AMD)|`ShelfScan-x.x.x-linux-x64`|
|Linux (64-bit ARM), e.g. Pi 4 and newer|`ShelfScan-x.x.x-linux-arm64`|
|Linux (32-bit ARM), e.g. Pi 3 and older|`ShelfScan-x.x.x-linux-arm`|
|Docker, e.g. Synology NAS|`ShelfScan-x.x.x-linux-x64`|
|macOS (Apple Silicon)|`ShelfScan-x.x.x-osx-arm64`|
|macOS (Intel)|`ShelfScan-x.x.x-osx-x64`|
|Other/Developers|Source code (zip / tar.gz)|

> [!TIP]
> There is no installer for native platforms. Just download the appropriate file and run it from the command line. If you're using Docker (e.g. on Synology), setup will differ - see notes below.

### macOS users

- Download the appropriate binary for your platform (see table above).
- Install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime). Slightly more technical information can be found [here](https://learn.microsoft.com/en-gb/dotnet/core/install/macos).
- ⚠️ Do not install the SDK, ASP.NET Core Runtime, or Desktop Runtime.
- Make the downloaded file executable: `chmod +x ShelfScan-x.x.x-<your-platform>`
- If you get `zsh: killed` when running the executable then:
  - Apply an ad-hoc code signature: `codesign --force --deep --sign - ShelfScan-x.x.x-<your-platform>`
  - Remove the quarantine attribute: `xattr -d com.apple.quarantine ShelfScan-x.x.x-<your-platform>`

### Linux users

- Download the appropriate binary for your platform (see table above).
- Install the [.NET 8.0 runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0/runtime). Slightly more technical pages can be found [here](https://learn.microsoft.com/en-gb/dotnet/core/install/linux).
- ⚠️ Do not install the SDK, ASP.NET Core Runtime, or Desktop Runtime.
- Make the downloaded file executable: `chmod +x ShelfScan-x.x.x-<your-platform>`

### Docker users

- Install the [.NET 8.0 Linux runtime](https://learn.microsoft.com/en-gb/dotnet/core/install/linux) inside the container or use a [.NET container image](https://learn.microsoft.com/en-gb/dotnet/core/docker/introduction#net-images).
- ⚠️ Do not install the SDK, ASP.NET Core Runtime, or Desktop Runtime.
- Use the `ShelfScan-x.x.x-linux-x64` binary inside the container.
- Mount your media folders into the container with appropriate read access.

### Platform testing notes

* Tested extensively: Windows 11  
* Tested moderately: Linux (64-bit ARM, Raspberry Pi 5 only)  
* Not tested: Windows 10, Linux (x64), Linux (32-bit ARM), Docker, macOS (x64 & Apple Silicon)

>[!NOTE]
>Docker and macOS environments have not been tested, and no platform-specific guidance is available as these setups are outside the developer’s experience. While ShelfScan should work fine on them, support will be limited to questions directly related to the tool itself.

## 💻 Command line options

ShelfScan is a command-line tool. Run it from a terminal or command prompt, supplying all options and arguments directly on the command line.

```
ShelfScan <folder> [options]
```

If you wish to save the output to a file then append `> [filename]` to the command line.

### Mandatory arguments:

- **`<folder>`**   
  Mandatory. Specifies the folder containing media content. Make sure that you provide the same top-level directory as you have configured in Plex.

### Optional arguments:

- **`-t <type>`**, **`--type <type>`**  
  Use to manually set the content type to `movie` or `tv`. ShelfScan will attempt automatic detection unless overridden.

- **`-p`**, **`--pass`**  
  Show files that pass verification. By default these are hidden.

- **`-s`**, **`--selftest`**  
  Performs a self test. When used, `<folder>` must be the path to a directory containing `pass` and `fail` sub-folders, each with `movies` and `tv` sub-folders inside. All content within those folders will be verified and a report generated. Test files are provided in the `test` subfolder of this repository. It is recommended to use 0-byte files for test content.

- **`/?`**, **`-h`**, **`--help`**  
  Show the command line options.

## 🛟 Questions/problems?

Please raise an issue at https://github.com/mrsilver76/shelfscan/issues.

## 💡 Future development

As this is an early beta, the goal is to get movie and TV show validation as accurate as possible.

Afterwards, music libraries may be considered.

## 📝 Attribution

- Bookshelf icons created by smalllikeart - Flaticon (https://www.flaticon.com/free-icons/bookshelf)
- Plex is a registered trademark of Plex, Inc. This tool is not affiliated with or endorsed by Plex, Inc.

## 🌍 Resources

The Plex website provides good documentation to help your organise and name your content.
-  [Naming and organizing your movie media files](https://support.plex.tv/articles/naming-and-organizing-your-movie-media-files/)
-  [Naming and organizing your TV show files](https://support.plex.tv/articles/naming-and-organizing-your-tv-show-files/).

## 🕰️ Version history

### 0.6.0 (26 October 2025)
- Fixed issue where ignored tags at the end of a filename were incorrectly causing parent folder validation to fail.
- Fixed issue where mismatched season numbers between folders and files were not being flagged.
- Added more detailed error messages for invalid movie filenames.
- Added warning when `[...]` is incorrectly used for `imdb`, `tmdb`, `tvdb`, and `edition` tags.
- Changed command line options to be more scalable, now use `-t` (`--type`) to force library type.
- Added `-p` (`--pass`) option to display files that pass verification.
- Added `-s` (`--selftest`) option to run automated tests on dummy content (test files available in repo)
- Updated test data.

### 0.5.0 (02 October 2025)
- Rewrote movie verification to improve brace handling, allow `{}` tags in any order, and parse filenames with optional split parts (`ptX` etc).
- Fixed featurette detection
- Removed "Plex Versions" from validation
- Fixed date formats with periods/spaces
- Fixed season parsing with 4-digit years
- Fixed incorrect `.avi` rejection
- Added percentage score and motivational message
- Added fictional test library (passing & failing) to the source code

### 0.0.1 (30 September 2025)
- Initial release.
