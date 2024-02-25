# DataPortal extensions for [CSLA.NET](https://cslanet.com/)


_![NuGet Version Generator](https://img.shields.io/nuget/v/Ossendorf.Csla.DataPortalExtensionGenerator?label=Generator)_
_![GitHub Release](https://img.shields.io/github/v/release/StefanOssendorf/Csla.DataPortalExtensions?include_prereleases)_  
_![NuGet Version Analyzer](https://img.shields.io/nuget/v/Ossendorf.Csla.DataPortalExtensionGenerator.Analyzers?label=Analyzer)__

_[![PullRequest Validation](https://github.com/StefanOssendorf/Csla.DataPortalExtensions/actions/workflows/pr-ci.yml/badge.svg?branch=master)](https://github.com/StefanOssendorf/Csla.DataPortalExtensions/actions/workflows/pr-ci.yml)_


 A Source Generator package that generates extension methods for `IDataPortal<T>` and `IChildDataPortal<T>`.  
 The extension methods are derived from annotated methods of business objects using CSLA.NET attributes like `Fetch`, `Create`, `...`.

## How to install

```bash
dotnet add package Ossendorf.Csla.DataPortalExtensionsGenerator
```
```xml
<PackageReference Include="Ossendorf.Csla.DataPortalExtensionsGenerator" Version="0.1.0-preview1" PrivateAssets="all" ExcludeAssets="runtime" />
```
Either way adds the source generator to your project. Make sure to add `PrivateAssets="all" ExcludeAssets="runtime"` to mark it as a build dependency. Otherwise it flows to projects which depend on your project.


To use the generator, add the `[Ossendorf.Csla.DataPortalExtensionsGenerator.DataPortalExtensions]` attribute to a class which should containt the extensions.  
For example:
```csharp
[Ossendorf.Csla.DataPortalExtensionsGenerator.DataPortalExtensions]
public static partial class DataPortalExtensions {
}
```
Your business object:
```csharp
namespace MyNamespace;

public class Address : BusinessBase<Address> {
    [Create]
    private void CreateLocally() {
        // creation logic
    }

    [Fetch]
    private async Task ById(Guid id) {
        // fetch logic
    }

    [Fetch]
    private async Task Fetch(string foo) {
        // fetch logic
    }
}
```

This will generate the following code:
```csharp
static partial class DataPortalExtensions {
    public static global::System.Threading.Tasks.Task<global::MyNamespace.Address> CreateLocally(this global::Csla.IDataPortal<global::MyNamespace.Address> portal) => portal.CreateAsync();
    public static global::System.Threading.Tasks.Task<global::MyNamespace.Address> ById(this global::Csla.IDataPortal<global::MyNamespace.Address> portal, global::System.Guid id) => portal.FetchAsync(id);
    public static global::System.Threading.Tasks.Task<global::MyNamespace.Address> Fetch(this global::Csla.IDataPortal<global::MyNamespace.Address> portal, string foo) => portal.FetchAsync(foo);
}
```

> [!WARNING]  
> In the example above the _last_ extension methods has the name `Fetch` which is already defined by the `IDataPortal` interface. That means the extension method is **never** used, because the compiler resolves the instance method and _not_ the extension method to be used.  
> To avoid that use the configuration explained next.

## How to configure the generator

You can configure the following for the generator to respect
* method prefix (default = "")
* method suffix (default = "")
* Enable/Disable nullable annotation context (default = Enable)
* SuppressWarningCS8669 (default = false)

The fetch named method example from above can be resolved with a prefix/suffix to generate a method with the name `YourFetch` which in turn can be used and provides reliable compiler support.

You can add the following properties to your csproj-file to configure the generator.
```xml
<PropertyGroup>
    <DataPortalExtensionGen_MethodPrefix>Prefix</DataPortalExtensionGen_MethodPrefix>
    <DataPortalExtensionGen_MethodSuffix>Suffix</DataPortalExtensionGen_MethodSuffix>
    <DataPortalExtensionGen_NullableContext>Enable/Disable</DataPortalExtensionGen_NullableContext>
    <DataPortalExtensionGen_SuppressWarningCS8669>true/false</DataPortalExtensionGen_SuppressWarningCS8669>
</PropertyGroup>
```

With this added the consuming project the generator picks the values up and adds them as prefix or suffix.

> [!TIP]
> To avoid wrong method resolution when your CSLA methods have the same name as the operation they perform. E.g. the method name is `Fetch()` for the `[Fetch]` attribute. Use either the prefix or suffix configuration to make them different from the methods provided from `IDataPortal`.

### Raodmap
- Special case commands to an extension like `commandPortal.ExecuteCommand(<params>)` which combines `Create`+`Execute`.
- Support for generic business objects
- Add attribute to exclude methods explicitly

A lot of implementation details are derived/taken from the great series [Andrew Lock: Creating a source generator](https://andrewlock.net/series/creating-a-source-generator/). If you want to create your own source generator I can recommend that series wholeheartedly.

#### Why isn't this generator adding the `Async` suffix for it's generated methods?
First of all in the current day nearly everything is async by default and not exception. That mean's I'm expecting that the data portals are used over some kind of wire which is async in nature. 
So since I don't want to support sync-methods (currently, maybe someone wants them badly?) and I _only_ have async methods why should I add noise to the method name?
A great post which explains the point in great detail is [No Async Suffix - NServiceBus](https://docs.particular.net/nservicebus/upgrades/5to6/async-suffix#reason-for-no-async-suffix).  
If you want the suffix for your code, just add it via the prefix configuration property :-).
