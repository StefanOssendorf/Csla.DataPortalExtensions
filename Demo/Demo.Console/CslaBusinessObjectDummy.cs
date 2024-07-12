using Csla;

namespace Demo.Console;

[Serializable]
internal class CslaBusinessObjectDummy : BusinessBase<CslaBusinessObjectDummy> {

    //[Fetch]
    //[FetchChild]
    //[Create]
    //[CreateChild]
    //[Delete]

    //[Execute]

    //[ExecuteChild]
    //[Insert]
    //[InsertChild]
    //[Update]
    //[UpdateChild]
    //[DeleteSelf]
    //[DeleteSelfChild]
    private void Dummy() {
        _ = this;
    }

    public static readonly PropertyInfo<int> IdProperty = RegisterProperty<int>(c => c.Id);
    public int Id {
        get => GetProperty(IdProperty);
        private set => LoadProperty(IdProperty, value);
    }

    [Fetch]
    private void FetchX(int id, ParamDummy dummy) {
        _ = dummy;
        using (BypassPropertyChecks) {
            Id = id;
        }

        var i = 10;
    }

    [Fetch]
    private Task FetchWithKrznbf() {
        using (BypassPropertyChecks) {
            Id = 1337;
        }
        return Task.CompletedTask;
    }

    [Fetch]
    private Task FetchWithOneParameter(Guid id) {
        using (BypassPropertyChecks) {
            _ = id;
            Id = 42;
        }

        return Task.CompletedTask;
    }

    [Delete]
    private void Delete() {
        _ = this;
    }

    private async Task Krznbf(global::Csla.IDataPortal<CslaBusinessObjectDummy> tmp) {
        _ = tmp;

        CslaBusinessObjectDummy? dummy = null;

        IDataPortal<CslaBusinessObjectDummy>? portal = null;
        IChildDataPortal<CslaBusinessObjectDummy>? childPortal = null;

        IDataPortal<global::Demo.Console.CslaBusinessObjectDummy>? dataPortal = null;
        
        await dataPortal!.FetchAsync();
        //portal.CreateAsync
        //portal.DeleteAsync
        //portal.ExecuteAsync
        //portal.FetchAsync
        //portal.UpdateAsync

        //childPortal.CreateChildAsync
        //childPortal.FetchChildAsync
        //childPortal.UpdateChildAsync
    }

    // Make private to test DPEGEN002 diagnostic
    [Serializable]
    public class ParamDummy {

    }
}
