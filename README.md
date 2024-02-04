# DataPortal extensions for [CSLA.NET](https://cslanet.com/)
 A Source Generator package that generates extension methods for `IDataPortal<T>` and `IChildDataPortal<T>`.  
 The extension methods are derived from annotated methods of business objects using CSLA.NET attributes like `Fetch`, `Create`, `...`.

## How to install

```bash
dotnet add package Ossendorf.Csla.DataPortalExtensionsGenerator
```
```xml
<PackageReference Include="Ossendorf.Csla.DataPortalExtensionsGenerator" Version="0.0.1-pre02" PrivateAssets="all" ExcludeAssets="runtime" />
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
}
```

This will generate the following code:
```csharp
static partial class DataPortalExtensions {
    public static global::System.Threading.Tasks.Task<global::MyNamespace.Address> CreateLocally(this global::Csla.IDataPortal<global::MyNamespace.Address> portal) => portal.CreateAsync();
    public static global::System.Threading.Tasks.Task<global::MyNamespace.Address> ById(this global::Csla.IDataPortal<global::MyNamespace.Address> portal, global::System.Guid id) => portal.FetchAsync(id);
}
```


### Raodmap
- Special case commands to an extension like `commandPortal.ExecuteCommand(<params>)` which combines `Create`+`Execute`.
- Support for generic business objects
- Improve handling of csla method parameters which are `internal` and not available
- Add configurability
  - Add attribute as prefix/suffix: 
    - `ById(id)` -> `FetchById(id)`
    - `ById(id)` -> `ByIdFetch(id)`
  - Exclude non-public business objects from generation
  - Exclude methods with non-public parameter types