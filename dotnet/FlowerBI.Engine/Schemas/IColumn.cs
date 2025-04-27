using System;

namespace FlowerBI;

public interface IColumn : INamed
{
    Table Table { get; }

    Type ClrType { get; }

    object ConvertValue(object fromDb);
}
