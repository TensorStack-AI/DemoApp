using System;
using System.Collections.Generic;
using System.Linq;
using TensorStack.Common;

namespace DemoApp
{
    public static class Extensions
    {
        public static bool RemoveAll<T>(this ICollection<T> collection, Func<T, bool> predicate)
        {
            if (collection.IsNullOrEmpty())
                return false;

            var items = collection.Where(predicate).ToList();
            if (items.Count == 0)
                return false;

            foreach (var item in items)
                collection.Remove(item);

            return true;
        }
    }
}
