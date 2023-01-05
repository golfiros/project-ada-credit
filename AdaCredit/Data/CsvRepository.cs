using System;
using System.IO;
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

using CsvHelper;
using CsvHelper.Configuration;

namespace AdaCredit.Data
{
    internal class CsvRepository<K, V, M> : IRepository<K, V>
        where K : notnull
        where V : class
        where M : ClassMap<V>
    {
        private string _filename;
        private Func<V, K> _keygen;

        private IDictionary<K, V> _data;

        public CsvRepository(string filename, Func<V, K> keygen)
        {
            _filename = filename;
            _keygen = keygen;

            _data = new Dictionary<K, V>();
        }

        public void Load() { }

        public void Save()
        {
            using (var writer = new StreamWriter(_filename))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.Context.RegisterClassMap<M>();
                foreach (var record in _data.Values) { csv.WriteRecord(record); }
            }
        }

        public bool ContainsKey(K key) => _data.ContainsKey(key);

        public bool Add(V val)
        {
            K key = _keygen(val);
            if (ContainsKey(key)) { return false; }
            _data.Add(key, val);
            return true;
        }

        public bool Remove(K key)
        {
            if (!ContainsKey(key)) { return false; }
            _data.Remove(key);
            return true;
        }

        public V? Get(K key)
        {
            if (!ContainsKey(key)) { return null; }
            return _data[key];
        }

        public IEnumerator<V> GetEnumerator() => _data.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
    }
}
