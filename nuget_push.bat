@ECHO OFF
CD %~dp0
SET PACKAGE_VERSION=1.0.30
dotnet nuget push .\src\Baker76.Atlas\bin\Release\Baker76.Atlas.%PACKAGE_VERSION%.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push .\src\Baker76.ColorQuant\bin\Release\Baker76.ColorQuant.%PACKAGE_VERSION%.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push .\src\Baker76.Compression\bin\Release\Baker76.Compression.%PACKAGE_VERSION%.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push .\src\Baker76.Core\bin\Release\Baker76.Core.%PACKAGE_VERSION%.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push .\src\Baker76.Imaging\bin\Release\Baker76.Imaging.%PACKAGE_VERSION%.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push .\src\Baker76.Pngcs\bin\Release\Baker76.Pngcs.%PACKAGE_VERSION%.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
dotnet nuget push .\src\Baker76.TileMap\bin\Release\Baker76.TileMap.%PACKAGE_VERSION%.nupkg --api-key %NUGET_API_KEY% --source https://api.nuget.org/v3/index.json
pause