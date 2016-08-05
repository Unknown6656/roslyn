using System;
using System.Collections;
using System.Collections.Generic;
using System.Concepts;

/// <summary>
/// Testbed for associated types.
/// </summary>
namespace AssociatedTypes
{
    concept CIndexable<C, [AssociatedType] E>
    {
        E At(C container, int i);
    }

    instance CIndexableArray<E> : CIndexable<E[], E>
    {
        E At(E[] container, int i) => container[i];
    }

    instance CIndexableString : CIndexable<string, char>
    {
        char At(string container, int i) => container[i];
    }

    instance CIndexableBitArray : CIndexable<BitArray, bool>
    {
        bool At(BitArray container, int i) => container[i];
    }

    concept CLength<C>
    {
        int Length(C container);
    }

    instance CLengthArray<E> : CLength<E[]>
    {
        int Length(E[] container) => container.Length;
    }

    instance CLengthString : CLength<string>
    {
        int Length(string container) => container.Length;
    }

    instance CLengthBitArray : CLength<BitArray>
    {
        int Length(BitArray container) => container.Length;
    }

    concept CEnumerable<C, [AssociatedType] E>
    {
        IEnumerator<E> Enumerate(C container);
    }

    class LengthIndexEnumerator<C, E, implicit I, implicit L> : IEnumerator<E>
        where I : CIndexable<C, E>
        where L : CLength<C>
    {
        private C _collection;
        private int _index;
        private E _current;

        public LengthIndexEnumerator(C collection)
        {
            _collection = collection;
            _index = -1;
            _current = default(E);
        }

        public E Current => _current;
        object IEnumerator.Current => Current;

        public bool MoveNext()
        {
            if (++_index >= L.Length(_collection)) return false;
            _current = I.At(_collection, _index);
            return true;
        }

        public void Reset()
        {
            _index = -1;
        }

        void IDisposable.Dispose() { }
    }

    instance CEnumerableLE<C, [AssociatedType] E, implicit I, implicit L> : CEnumerable<C, E>
        where I : CIndexable<C, E>
        where L : CLength<C>
    {
        IEnumerator<E> Enumerate(C container) => new LengthIndexEnumerator<C, E>(container);
    }

    class EnumerableShim<C, [AssociatedType] E, implicit N> : IEnumerable<E>
        where N : CEnumerable<C, E>
    {
        private C _collection;

        public EnumerableShim(C collection)
        {
            _collection = collection;
        }

        public IEnumerator<E> GetEnumerator() => N.Enumerate(_collection);
        IEnumerator IEnumerable.GetEnumerator() => N.Enumerate(_collection);
    }

    class Program
    {
        static IEnumerable<E> Enumerate<C, [AssociatedType] E, implicit N>(C c) where N : CEnumerable<C, E> => new EnumerableShim<C, E, N>(c);

        static void Main(string[] args)
        {
            foreach (var letter in Enumerate("DISCO"))
            {

            }
        }
    }
}
