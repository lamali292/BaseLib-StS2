using System.Collections;

namespace BaseLib.Utils;

public static class ChainedEnumerableExtension
{
    public static ChainedEnumerable<T> Chain<T>(this IEnumerable<T> enumerable, params IEnumerable<T> additional)
    {
        return new ChainedEnumerable<T>(enumerable, additional);
    }
}

public class ChainedEnumerable<T> : IEnumerable<T>
{
    private readonly IEnumerable<T>[] _inners;

    public ChainedEnumerable(params IEnumerable<T>[] inners)
    {
        _inners = inners;
    }

    public IEnumerator<T> GetEnumerator()
    {
        foreach (IEnumerable<T> inner in _inners)
        {
            foreach (T t in inner)
            {
                yield return t;
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}