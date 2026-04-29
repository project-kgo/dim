using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;

namespace Dim.Infrastructure.Persistence;

public sealed class DimSlaveDataSource(NpgsqlDataSource value)
{
    public  NpgsqlDataSource Value  => value;
}