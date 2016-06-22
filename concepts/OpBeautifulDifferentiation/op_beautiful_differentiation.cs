// Implementation of parts of Conal Elliot's Beautiful Differentiation.
// This version uses the experimental operator-overloaded prelude.
// Remember to reference ConceptAttributes.dll and OpPrelude.dll!

using System.Concepts.OpPrelude;
using System;
using static NumUtils;
using static System.Concepts.OpPrelude.Verbose;

/// <summary>
///     A first-order, scalar automatic derivative.
/// </summary>
/// <typeparam name="A">
///     The underlying numeric representation.
/// </typeparam>
public struct D<A>
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
    public D(A x, A dx)
    {
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
    public static D<A> Const(A k) where NumA : Num<A>
    {
        return new D<A>(k, Zero<A, NumA>());
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
    public static D<A> Id(A t) where NumA : Num<A>
    {
        return new D<A>(t, One<A, NumA>());
    }

    /// <summary>
    ///     Scalar chain rule.
    /// </summary>
    /// <param name="f">
    ///     A function.
    /// </param>
    /// <param name="df">
    ///     The derivative of <paramref name="f" />.
    /// </param>
    /// <typeparam name="A">
    ///     The underlying number representation.
    /// </typeparam>
    /// <returns>
    ///     A function over automatic derivatives applying the
    ///     function and its derivative.
    /// </returns>
    public static Func<D<A>, D<A>> Chain(Func<A, A> f, Func<A, A> df)
        where NumA : Num<A>
        => (d) => new D<A>(f(d.X), d.DX * df(d.X));
}


/// <summary>
///     Numeric utility functions.
/// </summary>
static class NumUtils
{
    /// <summary>
    ///     The zero of a numeric class.
    /// </summary>
    /// <returns>
    ///     Zero.
    /// </returns>
    public static A Zero<A>() where NumA: Num<A> => FromInteger(0);

    /// <summary>
    ///     The unity of a numeric class.
    /// </summary>
    /// <returns>
    ///     One.
    /// </returns>
    public static A One<A>() where NumA: Num<A> => FromInteger(1);

    /// <summary>
    ///     The two of a numeric class.
    /// </summary>
    /// <returns>
    ///     Two.
    /// </returns>
    public static A Two<A>() where NumA: Num<A> => FromInteger(2);

    /// <summary>
    ///     Calculates the negation of a number.
    /// </summary>
    /// <param name="x">
    ///     The number to negate.
    /// </param>
    /// <returns>
    ///     The negation of <paramref name="x"/>.
    /// </returns>
    public static A Neg<A>(A x) where NumA: Num<A> => Mul(FromInteger(-1), x);

    /// <summary>
    ///     Calculates the square of a number.
    /// </summary>
    /// <param name="x">
    ///     The number to square.
    /// </param>
    /// <returns>
    ///     The square of <paramref name="x"/>.
    /// </returns>
    public static A Square<A>(A x) where NumA: Num<A> => Mul(x, x);

    /// <summary>
    ///     Calculates the reciprocal of a number.
    /// </summary>
    /// <param name="x">
    ///     The number to reciprocate.
    /// </param>
    /// <returns>
    ///     The reciprocal of <paramref name="x"/>.
    /// </returns>
    public static A Recip<A>(A x) where FracA: Fractional<A> => Div(FromInteger(1), x);
}

/// <summary>
///     Mark 1 beautiful differentiation.
/// </summary>
namespace BD.Mark1
{
    instance NumDA<A> : Num<D<A>>
        where NumA : Num<A>
    {
        D<A> FromInteger(int x) => D<A>.Const<NumA>(FromInteger(x));

        D<A> operator +(D<A> x, D<A> y) => new D<A>(x.X + y.X, x.DX + y.DX);

        D<A> operator *(D<A> x, D<A> y)
            // Product rule
            => new D<A>(x.X * y.X, (x.DX * y.X) + (y.DX * x.X));

        D<A> operator -(D<A> x, D<A> y)
            => new D<A>(x.X - y.X, x.DX - y.DX);

        D<A> Signum(D<A> x) => new D<A>(Signum(x.X), Zero<A, NumA>());

        D<A> Abs(D<A> x) => new D<A>(Abs(x.X), x.DX * Signum(x.X));
    }

    instance FractionalDA<A> : Fractional<D<A>>
        where FracA : Fractional<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)         => NumDA<A, FracA>.FromInteger(x);
        D<A> operator +(D<A> x, D<A> y) => Add<D<A>, NumDA<A, FracA>>(x, y);
        D<A> operator *(D<A> x, D<A> y) => Mul<D<A>, NumDA<A, FracA>>(x, y);
        D<A> operator -(D<A> x, D<A> y) => Sub<D<A>, NumDA<A, FracA>>(x, y);
        D<A> Signum(D<A> x)             => NumDA<A, FracA>.Signum(x);
        D<A> Abs(D<A> x)                => NumDA<A, FracA>.Abs(x);

        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x)
            => D<A>.Const(FromRational(x));

        D<A> operator /(D<A> x, D<A> y)
            => new D<A>(
                   // Quotient rule
                   x.X / y.X,
                   ((x.DX * y.X) - (y.DX * x.X)) / (y.X * y.X)
               );
    }

    instance FloatingDA<A> : Floating<D<A>>
        where FloatA : Floating<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)         => FractionalDA<A, FloatA>.FromInteger(x);
        D<A> operator +(D<A> x, D<A> y) => Add(x, y);
        D<A> operator *(D<A> x, D<A> y) => Mul(x, y);
        D<A> operator -(D<A> x, D<A> y) => Sub(x, y);
        D<A> Signum(D<A> x)             => FractionalDA<A, FloatA>.Signum(x);
        D<A> Abs(D<A> x)                => FractionalDA<A, FloatA>.Abs(x);

        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x)
            => FractionalDA<A, FloatA>.FromRational(x);
        D<A> operator /(D<A> x, D<A> y) => Div(x, y);

        // Implementation of Floating
        D<A> Pi() => D<A>.Const(Pi());

        // d(e^x) = e^x
        D<A> Exp(D<A> x) => new D<A>(Exp(x.X), Mul(x.DX, Exp(x.X)));

        // d(ln x) = 1/x
        D<A> Log(D<A> x) => new D<A>(Log(x.X), Div(x.DX, x.X));

        // d(sqrt x) = 1/(2 sqrt x)
        D<A> Sqrt(D<A> x)
            => new D<A>(Sqrt(x.X), Div(x.DX, Mul(Two<A>(), Sqrt(x.X))));

        // d(x^y) rewrites to D(e^(ln x * y))
        D<A> Pow(D<A> x, D<A> y) => this.Exp(Mul(this.Log(x), y));

        // d(log b(x)) rewrites to D(log x / log b)
        D<A> LogBase(D<A> b, D<A> x) => Div(this.Log(x), this.Log(b));


        // d(sin x) = cos x
        D<A> Sin(D<A> x) => new D<A>(Sin(x.X), x.DX * Cos(x.X));

        // d(sin x) = -sin x
        D<A> Cos(D<A> x) => new D<A>(Cos(x.X), x.DX * Neg(Sin(x.X)));

        // d(tan x) = 1 + tan^2 x
        D<A> Tan(D<A> x)
            => new D<A>(Tan(x.X), x.DX * (One<A>() + Square(Tan(x.X))));

        // d(asin x) = 1/sqrt(1 - x^2)
        D<A> Asin(D<A> x)
            => new D<A>(Asin(x.X), x.DX / Sqrt(One<A>() - Square(x.X)));

        // d(acos x) = -1/sqrt(1 - x^2)
        D<A> Acos(D<A> x)
            => new D<A>(Acos(x.X), x.DX / Neg(Sqrt(One<A>() - Square(x.X))));

        // d(atan x) = 1/(1 + x^2)
        D<A> Atan(D<A> x)
            => new D<A>(Atan(x.X), x.DX / Add(One<A>(), Square(x.X)));

        // d(sinh x) = cosh x
        D<A> Sinh(D<A> x) => new D<A>(Sinh(x.X), Mul(x.DX, Cosh(x.X)));

        // d(cosh x) = sinh x
        D<A> Cosh(D<A> x) => new D<A>(Cosh(x.X), Mul(x.DX, Sinh(x.X)));

        // d(tanh x) = 1/(cosh^2 x)
        D<A> Tanh(D<A> x) => new D<A>(Tanh(x.X), Div(x.DX, Square(Cosh(x.X))));

        // d(asinh x) = 1 / sqrt(x^2 + 1)
        D<A> Asinh(D<A> x)
            => new D<A>(Asinh(x.X), x.DX / Sqrt(Square(x.X) + One<A>()));

        // d(acosh x) = 1 / sqrt(x^2 - 1)
        D<A> Acosh(D<A> x)
            => new D<A>(Acosh(x.X), x.DX / Sqrt(Square(x.X) - One<A>()));

        // d(atanh x) = 1 / (1 - x^2)
        D<A> Atanh(D<A> x)
            => new D<A>(Atanh(x.X), x.DX / (One<A>() - Square(x.X)));
    }
}

namespace BD.Mark2 {
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
        Func<A, B> operator +(Func<A, B> f, Func<A, B> g)
            => (x) => f(x) + g(x);
        Func<A, B> operator -(Func<A, B> f, Func<A, B> g)
            => (x) => f(x) - g(x);
        Func<A, B> operator *(Func<A, B> f, Func<A, B> g)
            => (x) => f(x) * g(x);
        Func<A, B> Abs(Func<A, B> f)
            => (x) => Abs(f(x));
        Func<A, B> Signum(Func<A, B> f)
            => (x) => Signum(f(x));
        Func<A, B> FromInteger(int k)
            => (x) => FromInteger(k);
    }

    /// <summary>
    ///     Fractional instance for functions.
    /// </summary>
    /// <typeparam name="A">
    ///     The domain of the function; unconstrained.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The range of the function; must be <c>Fractional</c>.
    /// </typeparam>
    instance FracF<A, B> : Fractional<Func<A, B>>
        where FracB : Fractional<B>
    {
        Func<A, B> operator +(Func<A, B> f, Func<A, B> g) => Add(f, g);
        Func<A, B> operator -(Func<A, B> f, Func<A, B> g) => Sub(f, g);
        Func<A, B> operator *(Func<A, B> f, Func<A, B> g) => Mul(f, g);

        Func<A, B> Abs(Func<A, B> f)               => NumF<A, B, FracB>.Abs(f);
        Func<A, B> Signum(Func<A, B> f)            => NumF<A, B, FracB>.Signum(f);
        Func<A, B> FromInteger(int k)              => NumF<A, B, FracB>.FromInteger(k);

        Func<A, B> FromRational(Ratio<int> k)
            => (x) => FromRational(k);
        Func<A, B> operator /(Func<A, B> f, Func<A, B> g)
            => (x) => f(x) / g(x);
    }

    /// <summary>
    ///     Floating instance for functions.
    /// </summary>
    /// <typeparam name="A">
    ///     The domain of the function; unconstrained.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The range of the function; must be <c>Floating</c>.
    /// </typeparam>
    instance FloatF<A, B> : Floating<Func<A, B>>
        where FloatB : Floating<B>
    {
        Func<A, B> operator +(Func<A, B> f, Func<A, B> g) => Add(f, g);
        Func<A, B> operator -(Func<A, B> f, Func<A, B> g) => Sub(f, g);
        Func<A, B> operator *(Func<A, B> f, Func<A, B> g) => Mul(f, g);
        Func<A, B> Abs(Func<A, B> f)               => FracF<A, B, FloatB>.Abs(f);
        Func<A, B> Signum(Func<A, B> f)            => FracF<A, B, FloatB>.Signum(f);
        Func<A, B> FromInteger(int k)              => FracF<A, B, FloatB>.FromInteger(k);
        Func<A, B> FromRational(Ratio<int> k)      => FracF<A, B, FloatB>.FromRational(k);
        Func<A, B> operator /(Func<A, B> f, Func<A, B> g) => Div(f, g);

        Func<A, B> Pi() => (x) => Pi();
        Func<A, B> Sqrt(Func<A, B> f) => (x) => Sqrt(f(x));
        Func<A, B> Exp(Func<A, B> f) => (x) => Exp(f(x));
        Func<A, B> Log(Func<A, B> f) => (x) => Log(f(x));
        Func<A, B> Pow(Func<A, B> f, Func<A, B> g)
            => (x) => Pow(f(x), g(x));
        Func<A, B> LogBase(Func<A, B> f, Func<A, B> g)
            => (x) => LogBase(f(x), g(x));

        Func<A, B> Sin(Func<A, B> f)   => (x) => Sin(f(x));
        Func<A, B> Cos(Func<A, B> f)   => (x) => Cos(f(x));
        Func<A, B> Tan(Func<A, B> f)   => (x) => Tan(f(x));
        Func<A, B> Asin(Func<A, B> f)  => (x) => Asin(f(x));
        Func<A, B> Acos(Func<A, B> f)  => (x) => Acos(f(x));
        Func<A, B> Atan(Func<A, B> f)  => (x) => Atan(f(x));
        Func<A, B> Sinh(Func<A, B> f)  => (x) => Sinh(f(x));
        Func<A, B> Cosh(Func<A, B> f)  => (x) => Cosh(f(x));
        Func<A, B> Tanh(Func<A, B> f)  => (x) => Tanh(f(x));
        Func<A, B> Asinh(Func<A, B> f) => (x) => Asinh(f(x));
        Func<A, B> Acosh(Func<A, B> f) => (x) => Acosh(f(x));
        Func<A, B> Atanh(Func<A, B> f) => (x) => Atanh(f(x));
    }

    instance NumDA<A> : Num<D<A>>
        where NumA : Num<A>
    {
        D<A> FromInteger(int x) => D<A>.Const(FromInteger(x));

        D<A> operator +(D<A> x, D<A> y)
            => new D<A>(Add(x.X, y.X), Add(x.DX, y.DX));


        D<A> operator *(D<A> x, D<A> y)
            // Product rule
            => new D<A>(x.X * y.X, (x.DX * y.X) + (y.DX * x.X));

        D<A> operator -(D<A> x, D<A> y)
            => new D<A>(x.X - y.X, x.DX - y.DX);

        D<A> Signum(D<A> x) =>
            D<A>.Chain(Signum, NumF<A, A, NumA>.FromInteger(0))(x);

        D<A> Abs(D<A> x) => D<A>.Chain(Abs, Signum)(x);
    }

    instance FractionalDA<A> : Fractional<D<A>>
        where FracA : Fractional<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)         => NumDA<A, FracA>.FromInteger(x);
        D<A> operator +(D<A> x, D<A> y) => Add(x, y);
        D<A> operator *(D<A> x, D<A> y) => Mul(x, y);
        D<A> operator -(D<A> x, D<A> y) => Sub(x, y);
        D<A> Signum(D<A> x)             => NumDA<A, FracA>.Signum(x);
        D<A> Abs(D<A> x)                => NumDA<A, FracA>.Abs(x);

        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x) => D<A>.Const(FromRational(x));

        D<A> operator /(D<A> x, D<A> y)
            => new D<A>(
                   // Quotient rule
                   x.X / y.X,
                   ((x.DX * y.X) - (y.DX * x.X)) / (y.X * y.X)
               );
    }

    instance FloatingDA<A> : Floating<D<A>>
        where FloatA : Floating<A>
    {
        // Implementation of Num
        D<A> FromInteger(int x)         => FractionalDA<A, FloatA>.FromInteger(x);
        D<A> operator +(D<A> x, D<A> y) => Add(x, y);
        D<A> operator *(D<A> x, D<A> y) => Mul(x, y);
        D<A> operator -(D<A> x, D<A> y) => Sub(x, y);
        D<A> Signum(D<A> x)             => FractionalDA<A, FloatA>.Signum(x);
        D<A> Abs(D<A> x)                => FractionalDA<A, FloatA>.Abs(x);

        // Implementation of Fractional
        D<A> FromRational(Ratio<int> x)
            => FractionalDA<A, FloatA>.FromRational(x);
        D<A> operator /(D<A> x, D<A> y) => Div(x, y);

        // Implementation of Floating
        D<A> Pi() => D<A>.Const(Pi());

        // d(e^x) = e^x
        D<A> Exp(D<A> x) => D<A>.Chain(Exp, Exp)(x);

        // d(ln x) = 1/x
        D<A> Log(D<A> x) => D<A>.Chain(Log, Recip)(x);

        // d(sqrt x) = 1/(2 sqrt x)
        D<A> Sqrt(D<A> x)
            => D<A>.Chain(Sqrt, Recip(Mul(Two<Func<A, A>>(), Sqrt)))(x);

        // d(x^y) rewrites to D(e^(ln x * y))
        D<A> Pow(D<A> x, D<A> y) =>
            this.Exp(Mul<D<A>, FloatingDA<A, FloatA>>(this.Log(x), y));

        // d(log b(x)) rewrites to D(log x / log b)
        D<A> LogBase(D<A> b, D<A> x) =>
            Div<D<A>, FloatingDA<A, FloatA>>(this.Log(x), this.Log(b));

        // d(sin x) = cos x
        D<A> Sin(D<A> x) => D<A>.Chain(Sin, Cos)(x);

        // d(sin x) = -sin x
        D<A> Cos(D<A> x)
            => D<A>.Chain(Cos, Neg<Func<A, A>>(Sin))(x);

        // d(tan x) = 1 + tan^2 x
        D<A> Tan(D<A> x)
            => D<A>.Chain(
                   Tan, Add(One<Func<A, A>>(), Square<Func<A, A>>(Tan))
               )(x);

        // d(asin x) = 1/sqrt(1 - x^2)
        D<A> Asin(D<A> x)
            => D<A>.Chain(
                   Asin,
                   Recip(
                       FloatF<A, A, FloatA>.Sqrt(
                           Sub(One<Func<A, A>>(), Square)
                       )
                   )
               )(x);

        // d(acos x) = -1/sqrt(1 - x^2)
        D<A> Acos(D<A> x)
            => D<A>.Chain(
                   Acos,
                   Recip(
                       Neg(
                           FloatF<A, A, FloatA>.Sqrt(
                               Sub(One<Func<A, A>>(), Square)
                           )
                       )
                   )
               )(x);

        // d(atan x) = 1/(1 + x^2)
        D<A> Atan(D<A> x)
            => D<A>.Chain(Atan, Recip(Add(One<Func<A, A>>(), Square)))(x);

        // d(sinh x) = cosh x
        D<A> Sinh(D<A> x) => D<A>.Chain(Sinh, Cosh)(x);

        // d(cosh x) = sinh x
        D<A> Cosh(D<A> x) => D<A>.Chain(Cosh, Sinh)(x);

        // d(tanh x) = 1/(cosh^2 x)
        D<A> Tanh(D<A> x)
            => D<A>.Chain(Tanh, Recip(Square<Func<A, A>>(Cosh)))(x);

        // d(asinh x) = 1 / sqrt(x^2 + 1)
        D<A> Asinh(D<A> x)
            => D<A>.Chain(
                   Asinh,
                   Recip(
                       FloatF<A, A, FloatA>.Sqrt(Add(Square, One<Func<A, A>>()))
                   )
               )(x);

        // d(acosh x) = 1 / sqrt(x^2 - 1)
        D<A> Acosh(D<A> x)
            => D<A>.Chain(
                   Acosh,
                   Recip(
                       FloatF<A, A, FloatA>.Sqrt(Sub(Square, One<Func<A, A>>()))
                   )
               )(x);

        // d(atanh x) = 1 / (1 - x^2)
        D<A> Atanh(D<A> x)
            => D<A>.Chain(Atanh, Recip(Sub(One<Func<A, A>>(), Square)))(x);
    }
}

namespace BD {
    public class Program {
        public static A F<A>(A z) where FloatA : Floating<A>
            => Sqrt(Mul(FromInteger(3), Sin(z)));

        public static A G<A>(A z) where FloatA : Floating<A>
            => Mul(Mul(FromInteger(3), Asinh(z)), Log(z));

        public static void Test() where FDA : Floating<D<double>>
        {
            var d = new D<double>(2.0, 1.0);

            var d2 = F(d);
            var d3 = G(d);

            Console.Out.WriteLine($"D {d.X} {d.DX}");
            Console.Out.WriteLine($"D {d2.X} {d2.DX}");
            Console.Out.WriteLine($"D {d3.X} {d3.DX}");
        }

        public static void Main()
        {
            Test<BD.Mark1.FloatingDA<double, PreludeDouble>>();
            Test<BD.Mark2.FloatingDA<double, PreludeDouble>>();
        }
    }
}