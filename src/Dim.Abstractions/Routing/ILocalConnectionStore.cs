namespace Dim.Abstractions.Routing;

public interface ILocalConnectionRouteStore
{
    public void Add(DimConectionRoute route);

    public bool TryGet(string connectionId, out DimConectionRoute? route);

    public void Remove(DimConectionRoute route);

    public IReadOnlyCollection<DimConectionRoute> GetAll();
}
