using System;
using System.Concepts.Prelude;

using ExpressionUtils;
using static ExpressionUtils.Utils;
/// <summary>
///     Numeric tower instances for functions.
/// </summary>
namespace BeautifulDifferentiation.ExpInstances
{
    /// <summary>
    ///     Numeric instance for functions.
    /// </summary>
    /// <typeparam name="A">
    ///     The domain of the function; unconstrained.
    /// </typeparam>
    /// <typeparam name="B">
    ///     The range of the function; must be <c>Num</c>.
    /// </typeparam>
    instance NumF<A, B> : Num<Exp<Func<A, B>>>
        where NumB : Num<Exp<B>>
    {
        Exp<Func<A, B>> Add(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A,B>((x) => Add(f.Apply(x), g.Apply(x)));
        Exp<Func<A, B>> Sub(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => Sub(f.Apply(x), g.Apply(x)));
        Exp<Func<A, B>> Mul(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A,B>( x => Mul(f.Apply(x), g.Apply(x)));
        Exp<Func<A, B>> Abs(Exp<Func<A, B>> f)
            => Lam<A,B>(x => Abs(f.Apply(x)));
        Exp<Func<A, B>> Signum(Exp<Func<A, B>> f)
            => Lam<A,B>(x => Signum(f.Apply(x)));
        Exp<Func<A, B>> FromInteger(int k)
            => Lam<A, B>(x => FromInteger(k));
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
    instance FracF<A, B> : Fractional<Exp<Func<A, B>>>
        where FracB : Fractional<Exp<B>>
    {
       Exp<Func<A, B>> Add(Exp<Func<A, B>> f, Exp<Func<A, B>> g) 
            => NumF<A, B, FracB>.Add(f, g);
       Exp<Func<A, B>> Sub(Exp<Func<A, B>> f, Exp<Func<A, B>> g) => NumF<A, B, FracB>.Sub(f, g);
       Exp<Func<A, B>> Mul(Exp<Func<A, B>> f, Exp<Func<A, B>> g) => NumF<A, B, FracB>.Mul(f, g);
       Exp<Func<A, B>> Abs(Exp<Func<A, B>> f) => NumF<A, B, FracB>.Abs(f);
       Exp<Func<A, B>> Signum(Exp<Func<A, B>> f) => NumF<A, B, FracB>.Signum(f);
       Exp<Func<A, B>> FromInteger(int k) => NumF<A, B, FracB>.FromInteger(k);

       Exp<Func<A, B>> FromRational(Ratio<int> k)
            => Lam<A,B>(x => FromRational(k));
       Exp<Func<A, B>> Div(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A,B>(x => Div(f.Apply(x), g.Apply(x)));
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
    instance FloatF<A, B> : Floating<Exp<Func<A, B>>>
        where FloatB : Floating<Exp<B>>
    {
        Exp<Func<A,B>> Add(Exp<Func<A,B>> f, Exp<Func<A,B>> g) => FracF<A, B, FloatB>.Add(f, g);
        Exp<Func<A,B>> Sub(Exp<Func<A,B>> f, Exp<Func<A,B>> g) => FracF<A, B, FloatB>.Sub(f, g);
        Exp<Func<A,B>> Mul(Exp<Func<A,B>> f, Exp<Func<A,B>> g) => FracF<A, B, FloatB>.Mul(f, g);
        Exp<Func<A,B>> Abs(Exp<Func<A,B>> f) => FracF<A, B, FloatB>.Abs(f);
        Exp<Func<A,B>> Signum(Exp<Func<A,B>> f) => FracF<A, B, FloatB>.Signum(f);
        Exp<Func<A,B>> FromInteger(int k) => FracF<A, B, FloatB>.FromInteger(k);
        Exp<Func<A,B>> FromRational(Ratio<int> k) => FracF<A, B, FloatB>.FromRational(k);
        Exp<Func<A,B>> Div(Exp<Func<A,B>> f, Exp<Func<A,B>> g) => FracF<A, B, FloatB>.Div(f, g);

        Exp<Func<A,B>> Pi() => Lam<A,B>(x => Pi());
        Exp<Func<A,B>> Sqrt(Exp<Func<A,B>> f) => Lam<A,B>(x => Sqrt(f.Apply(x)));
        Exp<Func<A, B>> Exp(Exp<Func<A, B>> f) => Lam<A, B>(x => Exp(f.Apply(x)));
        Exp<Func<A,B>> Log(Exp<Func<A,B>> f) => Lam<A,B>(x => Log(f.Apply(x)));
        Exp<Func<A, B>> Pow(Exp<Func<A, B>> f, Exp<Func<A, B>> g)
            => Lam<A, B>(x => Pow(f.Apply(x), g.Apply(x)));
        Exp<Func<A,B>> LogBase(Exp<Func<A,B>> f, Exp<Func<A,B>> g)
            => Lam<A,B>(x => LogBase(f.Apply(x), g.Apply(x)));

        Exp<Func<A,B>> Sin(Exp<Func<A,B>> f) =>Lam<A,B>( x => Sin(f.Apply(x)));
        Exp<Func<A,B>> Cos(Exp<Func<A,B>> f) =>Lam<A,B>( x => Cos(f.Apply(x)));
        Exp<Func<A,B>> Tan(Exp<Func<A,B>> f) =>Lam<A,B>( x => Tan(f.Apply(x)));
        Exp<Func<A,B>> Asin(Exp<Func<A,B>> f) =>Lam<A,B>( x => Asin(f.Apply(x)));
        Exp<Func<A,B>> Acos(Exp<Func<A,B>> f) =>Lam<A,B>( x => Acos(f.Apply(x)));
        Exp<Func<A,B>> Atan(Exp<Func<A,B>> f) =>Lam<A,B>( x => Atan(f.Apply(x)));
        Exp<Func<A,B>> Sinh(Exp<Func<A,B>> f) =>Lam<A,B>( x => Sinh(f.Apply(x)));
        Exp<Func<A,B>> Cosh(Exp<Func<A,B>> f) =>Lam<A,B>( x => Cosh(f.Apply(x)));
        Exp<Func<A,B>> Tanh(Exp<Func<A,B>> f) =>Lam<A,B>( x => Tanh(f.Apply(x)));
        Exp<Func<A,B>> Asinh(Exp<Func<A,B>> f) =>Lam<A,B>( x => Asinh(f.Apply(x)));
        Exp<Func<A,B>> Acosh(Exp<Func<A,B>> f) =>Lam<A,B>( x => Acosh(f.Apply(x)));
        Exp<Func<A,B>> Atanh(Exp<Func<A,B>> f) =>Lam<A,B>( x => Atanh(f.Apply(x)));
    }

}