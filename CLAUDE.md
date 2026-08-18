# Project rules for Claude

## What this is

ThemePackInstaller is a Windows Forms application with exactly one job: it opens the bundled
`Anti-BVB.themepack` from its own program directory through the Windows shell, which makes Windows
apply that theme, and then hides its own window. There is no user interface, no configuration, no
command line argument and no console output. The repository is **not** published as a NuGet
package, it ships as an Inno Setup installer that is attached to the GitHub release of its tag.
The built installer is deliberately **not** in git.

One solution `src/ThemePackInstaller.sln` with exactly one project:

- `src/ThemePackInstaller/ThemePackInstaller.csproj`, `OutputType` `WinExe`,
  `UseWindowsForms`, `ApplicationIcon` `Theme.ico`, `RuntimeIdentifiers` `win-x64`.

Layout inside `src/ThemePackInstaller`:

- `Program.cs`: `[STAThread] Main`, `EnableVisualStyles`,
  `SetCompatibleTextRenderingDefault(false)`, `Application.Run(new Main())`.
- `Main.cs`: the only class with logic. `CreateParams` adds `WS_EX_TOOLWINDOW` (`0x80`) so the
  window stays out of the task switcher, `MainLoad` starts the themepack and closes the form.
- `Main.Designer.cs` and `Main.resx`: designer output. Borderless form, `ShowInTaskbar` `false`,
  icon out of the resx.
- `GlobalUsings.cs`: all usings of the project, currently only `System.Diagnostics`.
- `Anti-BVB.themepack`: the payload, a CAB archive holding `Anti-BVB.theme` and its wallpaper.
  Copied next to the executable with `CopyToOutputDirectory=Always`.
- `License.txt`: a copy of the root license, also copied to the output directory, the installer
  shows it as the license page.
- `Theme.ico`: application and installer icon.

`Setup` holds the whole delivery:

- `ThemePackInstaller-Setup.iss`: Inno Setup 6 script. It packs everything from
  `src/ThemePackInstaller/bin/publish`, so the publish has to run before the compile.
- `build-setup-files.bat`: deletes every `bin` and `obj` below `src`, publishes and removes the
  `*.pdb`. It does **not** call the Inno Setup compiler, that is a separate step.
- `ThemePackInstaller-Setup.exe`: the build output. It is untracked, `.gitignore` excludes
  `*.exe`, and it belongs on the GitHub release page, not in a commit.

Repository root: `README.md` (the only user documentation), `Changelog.md`, `License.txt` (MIT),
`.gitattributes` and `.gitignore`. There is no test project, no `.github` folder, no
`Directory.Build.props`, no `Updating.md` and no `HowToUse.md`.

## Build

```powershell
dotnet build src/ThemePackInstaller.sln -c Release
```

- Single target framework `net10.0-windows`, no multi-targeting. `RuntimeIdentifiers` is `win-x64`.
- All build properties live directly in `ThemePackInstaller.csproj`. There is **no**
  `Directory.Build.props` in this repository.
- `TreatWarningsAsErrors` is enabled, so every warning breaks the build, NuGet warnings (`NU****`)
  from restore included. A clean build reports zero warnings, keep it that way.
- `NU1803` (HTTP source usage during restore) is the one warning suppressed via `NoWarn`. Fix
  warnings instead of extending that list. `NuGetAudit` and `NuGetAuditMode=all` are on, so a
  vulnerable transitive package fails the build too.
- Versions come from GitVersion.MsBuild out of the git tags, for example `1.0.8-1` for the first
  commit after tag `1.0.7`. Never edit a version property or an assembly version by hand.
- There are **no tests**. A behaviour change is verified by building, publishing and starting the
  executable. Be aware that a successful run really does change the Windows theme of the machine
  it runs on, so ask before running it on the user's desktop.
- Restore needs nuget.org. If a private feed is configured globally on the machine and answers 404
  for public packages, restore fails with `NU1301`. Then build with an explicit source:
  `dotnet build src/ThemePackInstaller.sln --source https://api.nuget.org/v3/index.json`.

## Code conventions

Follow the surrounding code, it is consistent in the hand written files:

- File header comment block with `<copyright file="..." company="Hämmer Electronics">` and a
  `<summary>`, then the file-scoped namespace.
- XML doc comments on every type and every member, private members included, no exceptions.
- `Nullable`, `ImplicitUsings` and `LangVersion latest` are enabled.
- New `using` directives go into `GlobalUsings.cs`, inside the existing `#pragma warning disable
  IDE0065` block, never at the top of a file. The editorconfig requires usings inside the
  namespace (`csharp_using_directive_placement=inside_namespace:warning`), which global usings
  cannot satisfy, that is what the pragma is for. Do not add other pragmas. The comment text in
  that block is German because Visual Studio generated it, leave it alone.
- Fields, properties, methods and events are always accessed with `this.` qualification
  (`dotnet_style_qualification_for_*` at severity `warning`).
- `src/.editorconfig` also enforces braces everywhere, no multiple blank lines, four spaces, CRLF,
  UTF-8, file scoped namespaces, `System` usings sorted first and `IDE0005` as warning. Analyzer
  warnings are fixed, not silenced.

## Known quirks

Do not silently "clean up" these, they are existing behaviour:

- **`Process.Start` needs `UseShellExecute = true`.** The themepack is a data file, not an
  executable. On .NET Framework the shell was used by default, on .NET it is not, so the plain
  `Process.Start(path)` that this repository shipped from version 1.0.2.0 to 1.0.7.0 threw a
  `Win32Exception` ("The specified executable is not a valid application for this OS platform")
  and the application did nothing but show a crash dialog. `MainLoad` therefore sets
  `UseShellExecute` explicitly. Never remove that flag, and never "simplify" the
  `ProcessStartInfo` back into the one argument overload.
- **The window is meant to be invisible.** `FormBorderStyle` is `None`, `ShowInTaskbar` is
  `false`, `CreateParams` adds `WS_EX_TOOLWINDOW` and `MainLoad` closes the form right after
  starting the themepack. The form size of 284x262 in the designer is therefore irrelevant, the
  window never gets painted.
- **The installer does not belong in git.** Up to version 1.0.8.0 every release committed
  `Setup/ThemePackInstaller-Setup.exe`, which is why the history carries roughly 40 MB of dead
  installers that no `git rm` can take back. Since version 1.0.9.0 the file is untracked and the
  installer is uploaded as an asset of the GitHub release instead. Never bring it back with
  `git add -f`.
- **The designer file breaks the editorconfig on purpose.** `Main.Designer.cs` uses a block scoped
  namespace, has no `this.` in `Dispose` and declares fully qualified types. That does not break
  the build because the IDE style rules do not run during a command line build
  (`EnforceCodeStyleInBuild` is not set). Regenerating the file with Visual Studio would produce
  the same shape again, so leave it as it is.
- **`Main.resx` carries four unused entries.** `Name1`, `Color1`, `Bitmap1` and `Icon1` are
  leftovers of the Visual Studio resx template, nothing in the code reads them. `$this.Icon` is
  the only entry that matters.
- **The themepack is a binary CAB.** `Anti-BVB.themepack` holds `Anti-BVB.theme` plus the
  wallpaper. Do not repack or "optimize" it, and keep the `*.themepack binary` rule in
  `.gitattributes`, otherwise `* text=auto` would leave the decision to a heuristic.
- **AppVeyor badge without CI in the repository.** `README.md` links an AppVeyor build that is
  configured outside of this repository. There is no `.github` folder and no pipeline file here.
- **The readme is `README.md`, not `Readme.md`.** Sibling repositories of the same author spell it
  differently, do not rename it here.
- **`src/ThemePackInstaller.sln.DotSettings`** is tracked and holds nothing but a ReSharper user
  dictionary (`H_00E4mmer`, `themepack`). Leave it alone.
- **`.gitattributes` sets `* text=auto`**, every rule of the Visual Studio template below it is
  commented out. `*.themepack binary` and `*.exe binary` were added on top so that the payload is
  never line ending normalized and the rule still holds for the installers already in the history.
  Any further binary file needs its own rule.

## Releasing

1. Make the change.
2. Add an entry at the top of `Changelog.md` in the existing format:
   `* **Version 1.0.8.0 (2026-08-18)** : Short description.`
3. Set `MyAppVersion` in `Setup/ThemePackInstaller-Setup.iss` to the same four part version. The
   file is UTF-8 **with** BOM and CRLF, keep both.
4. Commit that.
5. Tag the commit with the plain version number, no `v` prefix (`1.0.8`, `1.0.7`, ...). The
   existing tags are lightweight tags, create new ones the same way.
6. Only now build the installer, in this order:
   - `Setup/build-setup-files.bat` (cleans, publishes self contained, deletes the `*.pdb`).
   - `ISCC.exe Setup/ThemePackInstaller-Setup.iss`, which writes `Setup/ThemePackInstaller-Setup.exe`.
7. Push the commits and the tag.
8. Create the GitHub release for that tag and attach `Setup/ThemePackInstaller-Setup.exe` as an
   asset. Do **not** commit the installer.

There is no `gh` CLI on this machine. The token that `git push` uses works for the REST API and
carries the `repo` scope:

```powershell
$c = "protocol=https`nhost=github.com`n`n" | git credential fill
$tok = ($c | Select-String '^password=').ToString().Split('=',2)[1]
```

`POST https://api.github.com/repos/SeppPenner/ThemePackInstaller/releases` with `tag_name` creates
the release, the `upload_url` it returns takes the asset as
`Content-Type: application/octet-stream`.

The tag has to exist **before** the installer build. GitVersion takes the version out of the tags,
so an untagged commit burns a prerelease version such as `1.0.8-1+Branch.master.Sha...` into the
shipped executable. The version in `Changelog.md` has four parts (`1.0.8.0`), the tag has three
(`1.0.8`).

## Git

- **Never amend a commit.** No `git commit --amend`, not for a typo in the message, not to add a
  forgotten file, not even when the commit is still local. Write a follow-up commit instead. The
  release versions come from tags on exact commits, an amended commit leaves its tag pointing at a
  commit that no longer exists in the branch.

## Writing style

- Commit messages are written **in English only**: short, precise subject line, explanatory body
  when needed.
- Code comments and comments in project files such as `.csproj` are **always English**, regardless
  of the language used in the conversation.
- **No em dashes or en dashes** (`—`, `–`), neither in prose, commit messages, code comments nor
  documentation. Use a regular hyphen, comma, colon, parentheses or a separate sentence.
- German texts (documentation, chat replies) always use real umlauts and ß, never ASCII
  transliterations such as `ae`, `oe`, `ue` or `ss`. Identifiers, file names and configuration keys
  stay unchanged where umlauts are technically undesirable.
