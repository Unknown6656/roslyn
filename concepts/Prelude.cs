/// <summary>
///     Prelude of common concepts.
/// </summary>
namespace System.Concepts.Prelude
{
    #region Eq
    
    /// <summary>
    ///     Concept for equality.
    /// </summary>
    /// <typeparam name="A">
    ///     The type for which equality is being defined.
    /// </typeparam>
    public concept Eq<A>
    {
        /// <summary>
        ///     Returns true if <paramref name="a"/> equals <paramref name="b"/>.
        /// </summary>
        /// <param name="a">
        ///     The first item to compare for equality.
        /// </param>
        /// <param name="b">
        ///     The second item to compare for equality.
        /// </param>
        /// <returns>
        ///     True if <paramref name="a"/> equals <paramref name="b"/>.
        /// </returns>
        bool Equals(A a, A b);
    }

    /// <summary>
    ///     Implementation of <see cref="Eq{A}"/> for booleans.
    /// </summary>
    public instance EqBool : Eq<bool>
    {
        bool Equals(bool a, bool b) => a == b;
    }

    /// <summary>
    ///     Implementation of <see cref="Eq{A}"/> for integers.
    /// </summary>
    public instance EqInt : Eq<int>
    {
        bool Equals(int a, int b) => a == b;
    }

    /// <summary>
    ///     Implementation of <see cref="Eq{A}"/> for arrays.
    /// </summary>
    public instance EqArray<A> : Eq<A[]> where EqA: Eq<A>
    {
        bool Equals(A[] a, A[] b)
        {
            if (a == null) return b == null;
            if (b == null) return false;
            if (a.Length != b.Length) return false;
            for (int i = 0; i < a.Length; i++)
            {
                if (!EqA.Equals(a[i], b[i])) return false;
            }
            return true;
        }
    }
    
    #endregion Eq

    #region Ord
    
    /// <summary>
    ///     Concept for total ordering.
    /// </summary>
    /// <typeparam name="A">
    ///     The type for which ordering is being defined.
    /// </typeparam>
    public concept Ord<A> : Eq<A>
    {
        /// <summary>
        ///     Returns true if <paramref name="a"/> is less than or equal to
        ///     <paramref name="b"/>.
        /// </summary>
        /// <param name="a">
        ///     The first item to compare.
        /// </param>
        /// <param name="b">
        ///     The second item to compare.
        /// </param>
        /// <returns>
        ///     True if <paramref name="a"/> is less than or equal to
        ///     <paramref name="b"/>.
        /// </returns>
        bool Leq(A a, A b);
    }
    
    /// <summary>
    ///     Implementation of <see cref="Ord{A}"/> for booleans.
    /// </summary>
    public instance OrdBool : Ord<bool>
    {
        bool Equals(bool a, bool b) => EqBool.Equals(a, b);
        bool Leq(bool a, bool b) => !a || b;
    }
    
    /// <summary>
    ///     Implementation of <see cref="Ord{A}"/> for integers.
    /// </summary>
    public instance OrdInt : Ord<int>
    {
        bool Equals(int a, int b) => EqInt.Equals(a, b);
        bool Leq(int a, int b) => a <= b;
    }
    
    #endregion Ord
}