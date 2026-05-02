using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Dim.Abstractions.Routing;

public interface ILocalConnectionRouteStore
{
    public void Add(DimChatRoute route);
    public void Remove(DimChatRoute route);

    public IReadOnlyCollection<DimChatRoute> GetAll();
}
