# Building Official Release

> **CONFIDENTIAL** - For maintainer use only.

To generate the "Official" binary that hides the "Community Build" warning and enables the "Pro" branding, you must set the `OfficialBuild` property to `true` during the build process.

## Command Line

```powershell
dotnet publish OpenTweak/OpenTweak.csproj `
  -c Release `
  -r win-x64 `
  -p:PublishSingleFile=true `
  -p:OfficialBuild=true `
  --self-contained false `
  -o ./publish_official
```

## How It Works

1. The `OfficialBuild` property triggers the `OFFICIAL_BUILD` compiler constant in `OpenTweak.csproj`.
2. The `BuildIdentity.cs` service checks for this constant.
3. `MainViewModel` and `MainWindow.xaml` check `BuildIdentity.IsOfficialBuild`.
4. If `true`:
   - Community Build warning is HIDDEN.
   - Branding shows "Official Release".
5. If `false` (default for everyone else):
   - Community Build warning is SHOWN.
   - Users are encouraged to support development.

## Verification

Always verify the build before uploading to BuyMeACoffee:

1. Run the built executable.
2. Ensure the orange "Community Build" warning is **NOT** present in the status bar.
3. Ensure "Official Release" is displayed.
