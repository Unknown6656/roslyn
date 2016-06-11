using System.Concepts.Prelude;

/// <summary>
///     Implementation of monoids, inspired by Haskell.
///     <para>
///         This example mainly exists to show the added expressiveness
///         we get from explicit witnesses.
///     </para>
/// </summary>
namespace System.Concepts.Monoid
{
#region Concepts
    /// <summary>
    ///     Concept for semigroups.
    ///     <para>
    ///         A semigroup is a type with an associative binary
    ///         operation.
    ///     </para>
    /// </summary>
    /// <typeparam name="A">
    ///     The type being described as a semigroup.
    /// </typeparam>
    public concept Semigroup<A>
    {
        /// <summary>
        ///     An associative binary operation.
        /// </summary>
        /// <param name="x">
        ///     The first operand to the operation.
        /// </param>
        /// <param name="y">
        ///     The second operand to the operation.
        /// </param>
        /// <returns>
        ///     The result of the operation.
        /// </returns>
        A Append(A x, A y);
    }

    /// <summary>
    ///     Concept for monoids.
    ///     <para>
    ///         A monoid is a type with an identity and an associative
    ///         binary operation.
    ///     </para>
    /// </summary>
    /// <typeparam name="A">
    ///     The type being described as a monoid.
    /// </typeparam>
    public concept Monoid<A> : Semigroup<A>
    {
        /// <summary>
        ///     The identity of <see cref="Append" />.
        /// <summary>
        A Empty();

        // In Haskell 98, we also have MConcat here.
        // For now, however, we define it separately.
    }

#endregion Concept

#region Instances

    /// <summary>
    ///     Booleans form a monoid under conjunction.
    /// </summary>
    public instance All : Monoid<bool>
    {
        bool Empty() => true;
        bool Append(bool x, bool y) => x && y;
    }

    /// <summary>
    ///     Booleans form a monoid under disjunctions.
    /// </summary>
    public instance Any : Monoid<bool>
    {
        bool Empty() => true;
        bool Append(bool x, bool y) => x || y;
    }

    /// <summary>
    ///     Ordered values form a semigroup under minimum.
    /// </summary>
    /// <typeparam name="A">
    ///     The type of the ordered values.
    /// </typeparam>
    public instance Min<A> : Semigroup<A> where OrdA : Ord<A>
    {
        // Is this actually associative?
        A Append(A x, A y) => Leq(x, y) ? x : y;
    }

    /// <summary>
    ///     Ordered values form a semigroup under maximum.
    /// </summary>
    /// <typeparam name="A">
    ///     The type of the ordered values.
    /// </typeparam>
    public instance Max<A> : Semigroup<A> where OrdA : Ord<A>
    {
        // Is this actually associative?
        A Append(A x, A y) => Leq(x, y) ? y : x;
    }

    /// <summary>
    ///     Numbers form a monoid under addition.
    /// </summary>
    /// <typeparam name="A">
    ///     The type of the number being added.
    /// </typeparam>
    public instance Sum<A> : Monoid<A> where NumA : Num<A>
    {
        A Empty() => FromInteger(0);
        A Append(A x, A y) => Add(x, y);
    }

    /// <summary>
    ///     Numbers form a monoid under multiplication.
    /// </summary>
    /// <typeparam name="A">
    ///     The type of the number being multiplied.
    /// </typeparam>
    public instance Product<A> : Monoid<A> where NumA : Num<A>
    {
        A Empty() => FromInteger(1);
        A Append(A x, A y) => Mul(x, y);
    }

#endregion Instances

#region Utilities

    /// <summary>
    ///     Utility functions for monoids and semigroups.
    /// </summary>
    public static class Utils
    {
        /// <summary>
        ///     Folds a non-empty list using a semigroup.
        /// </summary>
        /// <param name="xs">
        ///     The list to fold; must be non-empty.
        /// </param>
        /// <returns>
        ///     The result of folding.
        /// </returns>
        /// <typeparam name="A">
        ///     The semigroup on which this function is being defined.
        /// </typeparam>
        public static A ConcatNonEmpty<A>(A[] xs) where SA : Semigroup<A>
        {
            var lxs = xs.Length;

            if (lxs == 0) throw new ArgumentException(
                "SConcat: list must be non-empty"
            );

            A result = xs[0];
            for (int i = 1; i < lxs; i++)
            {
                result = Append(result, xs[i]);
            }

            return result;
        }

        /// <summary>
        ///     Folds a list using a monoid.
        /// </summary>
        /// <param name="xs">
        ///     The list to fold.
        /// </param>
        /// <typeparam name="A">
        ///     The monoid on which this function is being defined.
        /// </typeparam>
        /// <returns>
        ///     The result of folding.
        /// </returns>
        public static A Concat<A>(A[] xs) where MA : Monoid<A>
        {
            A result = Empty();
            foreach (A x in xs)
            {
                result = Append(result, x);
            }
            return result;
        }
    }

#endregion Utilities
}