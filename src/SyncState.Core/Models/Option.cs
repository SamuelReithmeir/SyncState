using System.Diagnostics.CodeAnalysis;

namespace SyncState.Models;

internal record Option<TValue>(bool HasValue, TValue? Value)
{
    private Option() : this(false, default)
    {
    }

    public static Option<TValue> None => new(false, default);
    public static Option<TValue> Some(TValue value) => new(true, value);

    public bool TryGetValue([NotNullWhen(true)] out TValue? value)
    {
        value = Value;
        return HasValue;
    }
}