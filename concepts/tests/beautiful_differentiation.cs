// Implementation of parts of Conal Elliot's Beautiful Differentiation.
// Remember to reference ConceptAttributes.dll and Prelude.dll!

using System.Concepts.Prelude;
using System;
using System.Concepts;

/// <summary>
///     Mark 1 beautiful differentiation: first-order, scalar,
///     functional.
/// </summary>
namespace BD.Mark1
{
    /// <summary>
    ///     A first-order, scalar automatic derivative.
    /// </summary>
    /// <typeparam name="A">
    ///     The underlying numeric representation.
    /// </typeparam>
    struct D<A>
    {
        /// <summary>
        ///     The base value of the automatic derivative.
        /// </summary>
        public A X { get; }

        /// <summary>
        ///    The calculated first derivative of the value.
        /// </summary>
        public A DX { get; }

        /// <summary>
        ///     Constructs a derivative.
        /// </summary>
        /// <param name="x">
        ///     The base value of the automatic derivative.
        /// </param>
        /// <param name="dx">
        ///    The calculated first derivative of the value.
        /// </param>
        public D(A x, A dx) {
            this.X = x;
            this.DX = dx;
        }

        /// <summary>
        ///     Constructs a derivative for a constant.
        /// <summary>
        /// <param name="k">
        ///     The constant; must be <c>Num</c>.
        /// </param>
        /// <typeparam name="A">
        ///     The type of <paramref name="k"/> and thus the underlying
        ///     representation of the result.
        /// </typeparam>
        /// <returns>
        ///     A <see cref="D{A}"/> with the value
        ///     <paramref name="k"/> and first derivative <c>0</c>.
        /// </returns>
        public static D<A> Const<[ConceptWitness] NumA>(A k) where NumA : struct, Num<A>
        {
            return new D<A>(k, N<A, NumA>.Zero());
        }

        /// <summary>
        ///     Constructs a derivative for a term.
        /// <summary>
        /// <param name="t">
        ///     The term; must be <c>Num</c>.
        /// </param>
        /// <typeparam name="A">
        ///     The type of <paramref name="t"/> and thus the underlying
        ///     representation of the result.
        /// </typeparam>
        /// <returns>
        ///     A <see cref="D{A}"/> with the value
        ///     <paramref name="t"/> and first derivative <c>1</c>.
        /// </returns>
        public static D<A> Id<[ConceptWitness] NumA>(A t) where NumA : struct, Num<A>
        {
            return new D<A>(t, N<A, NumA>.One());
        }
    }

    /// <summary>
    ///     Numeric utility functions.
    /// </summary>
    static class N<A> where NumA: Num<A>
    {
        /// <summary>
        ///     The zero of a numeric class.
        /// </summary>
        /// <returns>
        ///     Zero.
        /// </returns>
        public static A Zero() => NumA.FromInteger(0);

        /// <summary>
        ///     The unity of a numeric class.
        /// </summary>
        /// <returns>
        ///     One.
        /// </returns>
        public static A One() => NumA.FromInteger(1);

        /// <summary>
        ///     The two of a numeric class.
        /// </summary>
        /// <returns>
        ///     Two.
        /// </returns>
        public static A Two() => NumA.FromInteger(2);

        /// <summary>
        ///     Calculates the negation of a number.
        /// </summary>
        /// <param name="x">
        ///     The number to negate.
        /// </param>
        /// <returns>
        ///     The negation of <paramref name="x"/>.
        /// </returns>
        public static A Neg(A x) => NumA.Mul(NumA.FromInteger(-1), x);

        /// <summary>
        ///     Calculates the square of a number.
        /// </summary>
        /// <param name="x">
        ///     The number to square.
        /// </param>
        /// <returns>
        ///     The square of <paramref name="x"/>.
        /// </returns>
        public static A Square(A x) => NumA.Mul(x, x);
    }

    instance NumDA<A> : Num<D<A>>
        where NumA : Num<A>
    {
        D<A> FromInteger(int x)
            => D<A>.Const<NumA>(NumA.FromInteger(x));

        D<A> Add(D<A> x, D<A> y)
            => new D<A>(NumA.Add(x.X, y.X), NumA.Add(x.DX, y.DX));

        D<A> Mul(D<A> x, D<A> y)
            => new D<A>(
                   // Product rule
                   NumA.Mul(x.X, y.X),
                   NumA.Add(NumA.Mul(x.DX, y.X), NumA.Mul(y.DX, x.X))
               );

        D<A> Sub(D<A> x, D<A> y)
            => new D<A>(NumA.Sub(x.X, y.X), NumA.Sub(x.DX, y.DX));

        D<A> Signum(D<A> x)
            => new D<A>(NumA.Signum(x.X), N<A, NumA>.Zero());

        D<A> Abs(D<A> x)
            => new D<A>(
                   NumA.Abs(x.X),
                   NumA.Mul(x.DX, NumA.Signum(x.X))
               );
    }

    instance FractionalDA<A> : Fractional<D<A>>
        where FracA : Fractional<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)
            => NumDA<A, FracA>.FromInteger(x);
        D<A> Add(D<A> x, D<A> y)
            => NumDA<A, FracA>.Add(x, y);
        D<A> Mul(D<A> x, D<A> y)
            => NumDA<A, FracA>.Mul(x, y);
        D<A> Sub(D<A> x, D<A> y)
            => NumDA<A, FracA>.Sub(x, y);
        D<A> Signum(D<A> x)
            => NumDA<A, FracA>.Signum(x);
        D<A> Abs(D<A> x)
            => NumDA<A, FracA>.Abs(x);

        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x)
            => D<A>.Const<FracA>(FracA.FromRational(x));

        D<A> Div(D<A> x, D<A> y)
            => new D<A>(
                   // Quotient rule
                   FracA.Div(x.X, y.X),
                   FracA.Div(
                       FracA.Sub(
                           FracA.Mul(x.DX, y.X),
                           FracA.Mul(y.DX, x.X)
                       ),
                       FracA.Mul(y.X, y.X)
                   )
               );
    }

    instance FloatingDA<A> : Floating<D<A>>
        where FloatA : Floating<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)
            => FractionalDA<A, FloatA>.FromInteger(x);
        D<A> Add(D<A> x, D<A> y)
            => FractionalDA<A, FloatA>.Add(x, y);
        D<A> Mul(D<A> x, D<A> y)
            => FractionalDA<A, FloatA>.Mul(x, y);
        D<A> Sub(D<A> x, D<A> y)
            => FractionalDA<A, FloatA>.Sub(x, y);
        D<A> Signum(D<A> x)
            => FractionalDA<A, FloatA>.Signum(x);
        D<A> Abs(D<A> x)
            => FractionalDA<A, FloatA>.Abs(x);
        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x)
            => FractionalDA<A, FloatA>.FromRational(x);
        D<A> Div(D<A> x, D<A> y)
            => FractionalDA<A, FloatA>.Div(x, y);

        // Implementation of Floating
        D<A> Pi() => D<A>.Const<FloatA>(FloatA.Pi());

        // d(e^x) = e^x
        D<A> Exp(D<A> x)
            => new D<A>(
                   FloatA.Exp(x.X),
                   FloatA.Mul(x.DX, FloatA.Exp(x.X))
               );

        // d(ln x) = 1/x
        D<A> Log(D<A> x)
            => new D<A>(FloatA.Log(x.X), FloatA.Div(x.DX, x.X));

        // d(sqrt x) = 1/(2 sqrt x)
        D<A> Sqrt(D<A> x)
            => new D<A>(
                   FloatA.Sqrt(x.X),
                   FloatA.Div(
                       x.DX,
                       FloatA.Mul(N<A, FloatA>.Two(), FloatA.Sqrt(x.X))
                   )
               );

        // d(x^y) rewrites to D(e^(ln x * y))
        D<A> Pow(D<A> x, D<A> y)
            => Exp(Mul(Log(x), y));

        // d(log b(x)) rewrites to D(log x / log b)
        D<A> LogBase(D<A> b, D<A> x)
            => Div(Log(x), Log(b));

        // d(sin x) = cos x
        D<A> Sin(D<A> x)
            => new D<A>(
                   FloatA.Sin(x.X),
                   FloatA.Mul(x.DX, FloatA.Cos(x.X))
               );

        // d(sin x) = -sin x
        D<A> Cos(D<A> x)
            => new D<A>(
                   FloatA.Cos(x.X),
                   FloatA.Mul(
                       x.DX,
                       N<A, FloatA>.Neg(FloatA.Sin(x.X))
                   )
               );

        // d(tan x) = 1 + tan^2 x
        D<A> Tan(D<A> x)
            => new D<A>(
                   FloatA.Tan(x.X),
                   FloatA.Mul(
                       x.DX,
                       FloatA.Add(
                           N<A, FloatA>.One(),
                           N<A, FloatA>.Square(FloatA.Tan(x.X))
                       )
                   )
               );

        // d(asin x) = 1/sqrt(1 - x^2)
        D<A> Asin(D<A> x)
            => new D<A>(
                   FloatA.Asin(x.X),
                   FloatA.Div(
                       x.DX,
                       FloatA.Sqrt(
                           FloatA.Sub(
                               N<A, FloatA>.One(),
                               N<A, FloatA>.Square(x.X)
                           )
                       )
                   )
               );

        // d(acos x) = -1/sqrt(1 - x^2)
        D<A> Acos(D<A> x)
            => new D<A>(
                   FloatA.Acos(x.X),
                   FloatA.Div(
                       x.DX,
                       N<A, FloatA>.Neg(
                           FloatA.Sqrt(
                               FloatA.Sub(
                                   N<A, FloatA>.One(),
                                   N<A, FloatA>.Square(x.X)
                               )
                           )
                       )
                   )
               );

        // d(atan x) = 1/(1 + x^2)
        D<A> Atan(D<A> x)
            => new D<A>(
                   FloatA.Atan(x.X),
                   FloatA.Div(
                       x.DX,
                       FloatA.Add(
                           N<A, FloatA>.One(),
                           N<A, FloatA>.Square(x.X)
                       )
                   )
               );

        // d(sinh x) = cosh x
        D<A> Sinh(D<A> x)
            => new D<A>(
                   FloatA.Sinh(x.X),
                   FloatA.Mul(x.DX, FloatA.Cosh(x.X))
               );

        // d(cosh x) = sinh x
        D<A> Cosh(D<A> x)
            => new D<A>(
                   FloatA.Cosh(x.X),
                   FloatA.Mul(x.DX, FloatA.Sinh(x.X))
               );

        // d(tanh x) = 1/(cosh^2 x)
        D<A> Tanh(D<A> x)
            => new D<A>(
                   FloatA.Tanh(x.X),
                   FloatA.Div(x.DX, N<A, FloatA>.Square(FloatA.Cosh(x.X)))
               );

        // d(asinh x) = 1 / sqrt(x^2 + 1)
        D<A> Asinh(D<A> x)
            => new D<A>(
                   FloatA.Asinh(x.X),
                   FloatA.Div(
                       x.DX,
                       FloatA.Sqrt(
                           FloatA.Add(
                               N<A, FloatA>.Square(x.X),
                               N<A, FloatA>.One()
                           )
                       )
                   )
               );

        // d(acosh x) = 1 / sqrt(x^2 - 1)
        D<A> Acosh(D<A> x)
            => new D<A>(
                   FloatA.Acosh(x.X),
                  FloatA.Div(
                       x.DX,
                       FloatA.Sqrt(
                           FloatA.Sub(
                               N<A, FloatA>.Square(x.X),
                               N<A, FloatA>.One()
                           )
                       )
                   )
               );

        // d(atanh x) = 1 / (1 - x^2)
        D<A> Atanh(D<A> x)
            => new D<A>(
                   FloatA.Atanh(x.X),
                   FloatA.Div(
                       x.DX,
                       FloatA.Sub(
                           N<A, FloatA>.One(),
                           N<A, FloatA>.Square(x.X)
                       )
                   )
               );
    }
}

namespace BD {
    /// <summary>
    ///     Numeric instance for functions.
    /// </summary>
    /// <typeparam name="A">
    ///     The domain of the function; unconstrained.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The range of the function; must be <c>Num</c>.
    /// </typeparam>
    instance NumF<A, B> : Num<Func<A, B>>
        where NumB : Num<B>
    {
        Func<A, B> Add(Func<A, B> f, Func<A, B> g)
            => (x) => NumB.Add(f(x), g(x));
        Func<A, B> Sub(Func<A, B> f, Func<A, B> g)
            => (x) => NumB.Sub(f(x), g(x));
        Func<A, B> Mul(Func<A, B> f, Func<A, B> g)
            => (x) => NumB.Mul(f(x), g(x));
        Func<A, B> Abs(Func<A, B> f)
            => (x) => NumB.Abs(f(x));
        Func<A, B> Signum(Func<A, B> f)
            => (x) => NumB.Signum(f(x));
        Func<A, B> FromInteger(int k)
            => (x) => NumB.FromInteger(k);
    }

    public class Program {
        public static A F<A>(A z) where FloatA : Floating<A>
        {
            return FloatA.Sqrt(
                FloatA.Mul(
                    FloatA.FromInteger(3),
                    FloatA.Sin(z)
                )
            );
        }

        public static void TestMark1()
        {
            var d = new BD.Mark1.D<double>(2.0, 1.0);
            var d2 =
                F<BD.Mark1.D<double>,
                  BD.Mark1.FloatingDA<double, FloatingDouble>>(d);

            Console.Out.WriteLine($"D {d.X} {d.DX}");
            Console.Out.WriteLine($"D {d2.X} {d2.DX}");
        }

        public static void Main()
        {
            TestMark1();
        }
    }
}