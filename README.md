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

 Either way adds the source generator to your project. Make sure to add `PrivateAssets="all" ExcludeAssets="runtime"` to mark it as a build dependency so it does not flow to projects which depend on your project.
