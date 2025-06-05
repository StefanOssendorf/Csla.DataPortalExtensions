using Csla;

namespace Demo.Library;

/// <summary>
/// 
/// </summary>
public class Foo : BusinessBase<Foo> {
    [Fetch]
    private void Fetch() { }

    [Create]
    private void Create() { }

    [Update]
    private void Update() { }

    [FetchChild]
    private void FetchChild() { }

    [CreateChild]
    private void CreateChild() { }

    [UpdateChild]
    private void UpdateChild() { }
}
