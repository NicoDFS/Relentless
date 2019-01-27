using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Loom.ZombieBattleground
{
    public class UniquePositionedList<T> : IPositionedList<T>
    {
        private readonly IPositionedList<T> _list;

        public UniquePositionedList(IPositionedList<T> list)
        {
            if (list == null)
                throw new ArgumentNullException(nameof(list));

            IEnumerable<IGrouping<T,T>> duplicates = list.GroupBy(x => x).Where(g => g.Count() > 1);
            foreach (IGrouping<T,T> duplicate in duplicates)
            {
                throw new ArgumentException($"Source list contained duplicate value \"{duplicate.Key}\"", nameof(list));
            }

            _list = list;
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _list.GetEnumerator();
        }

        public int Count => _list.Count;

        public T this[int index]
        {
            get => _list[index];
            set
            {
                if (!Equal(_list[index], value))
                {
                    ThrowIfContains(value);
                }

                _list[index] = value;
            }
        }

        public bool IsReadOnly => _list.IsReadOnly;

        public void Clear()
        {
            _list.Clear();
        }

        public bool Contains(T item)
        {
            return _list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            _list.CopyTo(array, arrayIndex);
        }

        public bool Remove(T item)
        {
            return _list.Remove(item);
        }

        public int IndexOf(T item)
        {
            return _list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            ThrowIfContains(item);
            _list.Insert(index, item);
        }

        public void RemoveAt(int index)
        {
            _list.RemoveAt(index);
        }

        public UniquePositionedList<T> FindAll(Predicate<T> match)
        {
            return new UniquePositionedList<T>(new PositionedList<T>(this.FindAll<T>(match)));
        }

        private void ThrowIfContains(T item)
        {
            if (_list.Contains(item))
                throw new ArgumentException($"Item \"{item}\" is already in the list");
        }

        private static bool Equal(T item1, T item2)
        {
            return EqualityComparer<T>.Default.Equals(item1, item2);
        }
    }
}
