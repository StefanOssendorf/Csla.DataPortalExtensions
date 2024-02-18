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

    private void Krznbf(global::Csla.IDataPortal<CslaBusinessObjectDummy> tmp) {
        IDataPortal<CslaBusinessObjectDummy>? portal = null;
        IChildDataPortal<CslaBusinessObjectDummy>? childPortal = null;

        IDataPortal<global::Demo.Console.CslaBusinessObjectDummy>? dataPortal = null;

        //portal.CreateAsync
        //portal.DeleteAsync
        //portal.ExecuteAsync
        //portal.FetchAsync
        //portal.UpdateAsync

        //childPortal.CreateChildAsync
        //childPortal.FetchChildAsync
        //childPortal.UpdateChildAsync
    }

    private class ParamDummy {

    }
}
