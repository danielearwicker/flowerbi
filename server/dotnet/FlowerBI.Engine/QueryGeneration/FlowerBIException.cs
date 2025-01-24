using System;

namespace FlowerBI;

public class FlowerBIException(string message, Exception inner) : Exception(message, inner)
{
    public FlowerBIException(string message) : this(message, null) {}
}
