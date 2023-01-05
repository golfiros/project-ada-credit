using System.Collections.Generic;

namespace AdaCredit.Data
{
    internal interface IRepository<K, V> : IEnumerable<V>
    {
        // single key database interface
        // cut down dictionary for lookup convenience
        // with save and load methods
        // keys have to generated by the implementation
        void Load();
        void Save();

        bool ContainsKey(K key);
        bool Add(V val);
        bool Remove(K key);
        V? Get(K key);
    }
}