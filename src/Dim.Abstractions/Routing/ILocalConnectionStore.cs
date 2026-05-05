namespace Dim.Abstractions.Routing;

public interface ILocalConnectionRouteStore
{
    public int Count { get; }

    public void Add(DimConectionRoute route);

    public bool TryGet(string connectionId, out DimConectionRoute? route);

    public void Remove(DimConectionRoute route);

    public IReadOnlyCollection<DimConectionRoute> GetAll();

    public IReadOnlyCollection<DimConectionRoute> GetRefreshBatch(int maxCount);

    public void MoveToTail(IReadOnlyCollection<DimConectionRoute> routes);
}
