using System;
using System.Collections.Generic;
using System.Drawing;
using System.Concepts.Prelude;
using System.Concepts.Monoid;
using System.Linq;
using static System.Concepts.Monoid.Utils;
using static Utils;

/// <summary>
///     Concept for numbers convertible to 32-bit integers.
/// </summary>
/// <typeparam name="A">
///    The type to convert.
/// </typeparam>
public concept ToInt32<A>
{
    /// <summary>
    ///     Converts a number to an Int32.
    /// </summary>
    /// <param name="x">
    ///     The number to convert.
    /// </param>
    /// <returns>
    ///     The number as an Int32.
    /// </returns>
    Int32 ToInt32(A x);
}

/// <summary>
///     Instance of ToIn32 for 32-bit integers.
/// </summary>
public instance ToInt32Int32 : ToInt32<Int32>
{
    /// <summary>
    ///     Converts a number to an Int32.
    /// </summary>
    /// <param name="x">
    ///     The number to convert.
    /// </param>
    /// <returns>
    ///     The number as an Int32.
    /// </returns>
    Int32 ToInt32(Int32 x) => x;
}

/// <summary>
///     A two-dimensional point, generic over any numeric type.
/// </summary>
/// <typeparam name="A">
///     The underlying numeric type.
/// </typeparam>
public struct Point<A>
{
    /// <summary>
    ///     Gets or sets the X co-ordinate of this Point.
    /// </summary>
    public A X { get; set; }

    /// <summary>
    ///     Gets or sets the Y co-ordinate of this Point.
    /// </summary>
    public A Y { get; set; }
}

/// <summary>
///     A line segment between two points.
/// </summary>
/// <typeparam name="A">
///     The underlying numeric type.
/// </typeparam>
public struct Line<A>
{
    /// <summary>
    ///     Gets or sets the first point on this line.
    /// </summary>
    public Point<A> P1 { get; set; }

    /// <summary>
    ///     Gets or sets the second point on this line.
    /// </summary>
    public Point<A> P2 { get; set; }

    /// <summary>
    ///     Flips the points on this line.
    /// </summary>
    /// <returns>
    ///     A new line with the points flipped.
    /// </returns>
    public Line<A> Flip()
    {
        return new Line<A> { P2 = this.P1, P1 = this.P2 };
    }

    /// <summary>
    ///     Decides whether a point is on the right of this line.
    /// </summary>
    /// <param name="point">
    ///     The point to consider.
    /// </param>
    /// <returns>
    ///     True if the point is on the right of this line, including
    ///     points on the line.
    /// </returns>
    public bool OnRight(Point<A> point)
        where OrdA : Ord<A>
        where NumA : Num<A>
    {
        // From http://stackoverflow.com/questions/1560492/
        return Leq(
            FromInteger(1),
            Signum(
                Sub(
                    Mul(
                        Sub(P2.X, P1.X),
                        Sub(point.Y, P1.Y)
                    ),
                    Mul(
                        Sub(P2.Y, P1.Y),
                        Sub(point.X, P1.X)
                    )
                )
            )
        );
    }
}

/// <summary>
///     Ordering of points based on their X co-ordinate.
/// </summary>
instance OrdPointX<A> : Ord<Point<A>> where OrdA : Ord<A>
{
    bool Equals(Point<A> x, Point<A> y) => Equals(x.X, y.X);
    bool Leq(Point<A> x, Point<A> y)    => Leq(x.X, y.X);
}

/// <summary>
///     Ordering of points based on their Y co-ordinate.
/// </summary>
instance OrdPointY<A> : Ord<Point<A>> where OrdA : Ord<A>
{
    bool Equals(Point<A> x, Point<A> y) => Equals(x.Y, y.Y);
    bool Leq(Point<A> x, Point<A> y)    => Leq(x.Y, y.Y);
}

/// <summary>
///     Concept for items drawable onto a graphics context.
/// </summary>
/// <typeparam name="A">
///    The type to draw.
/// </typeparam>
public concept Drawable<A>
{
    /// <summary>
    ///     Draws the item onto a graphics context.
    /// </summary>
    /// <param name="item">
    ///     The item to draw.
    /// </param>
    /// <param name="colour">
    ///     The colour in which to draw the item.
    /// </param>
    /// <param name="gfx">
    ///     The graphics context to draw onto.
    /// </param>
    void Draw(A item, Color colour, Graphics gfx);
}

/// <summary>
///     Drawable instance for points.
/// </summary>
public instance DrawPoint<A> : Drawable<Point<A>>
    where TA : ToInt32<A>
{
    void Draw(Point<A> item, Color colour, Graphics gfx)
    {
        var brush = new SolidBrush(colour);
        var x = ToInt32(item.X);
        var y = ToInt32(item.Y);

        gfx.FillEllipse(brush, x - 4, y - 4, 8, 8);
    }
}

/// <summary>
///     Drawable instance for lines.
/// </summary>
public instance DrawLine<A> : Drawable<Line<A>>
    where TA : ToInt32<A>
{
    void Draw(Line<A> item, Color colour, Graphics gfx)
    {
        var pen = new Pen(colour, 5.0f);
        var x1 = ToInt32(item.P1.X);
        var y1 = ToInt32(item.P1.Y);
        var x2 = ToInt32(item.P2.X);
        var y2 = ToInt32(item.P2.Y);

        gfx.DrawLine(pen, x1, y1, x2, y2);
    }
}

/// <summary>
///     Composition of enumerations of drawables.
/// </summary>
public instance DrawEnum<A> : Drawable<IEnumerable<A>>
    where DA : Drawable<A>
{
    void Draw(IEnumerable<A> items, Color colour, Graphics gfx)
    {
        foreach (var item in items)
        {
            Draw(item, colour, gfx);
        }
    }
}

static class Utils
{
    /// <summary>
    ///     Computes the maximum of a non-empty list of ordered items.
    /// </summary>
    /// <param name="xs">
    ///     The list of ordered items to consider.  Must be non-empty.
    /// </param>
    /// <returns>
    ///     The maximum element of the list <paramref name="xs"/>.
    /// </returns>
    public static A Maximum<A>(A[] xs) where OrdA : Ord<A>
        => ConcatNonEmpty<A, Max<A, OrdA>>(xs);

    /// <summary>
    ///     Computes the maximum of a non-empty list of ordered items.
    /// </summary>
    /// <param name="xs">
    ///     The list of ordered items to consider.  Must be non-empty.
    /// </param>
    /// <returns>
    ///     The minimum element of the list <paramref name="xs"/>.
    /// </returns>
    public static A Minimum<A>(A[] xs) where OrdA : Ord<A>
        => ConcatNonEmpty<A, Min<A, OrdA>>(xs);
}

public class Quickhull<A>
{
    private List<Line<A>> _lines;
    private readonly Point<A>[] _points;
    private List<Point<A>> _hull;

    public IEnumerable<Line<A>>  Lines  => _lines;
    public IEnumerable<Point<A>> Points => _points;
    public IEnumerable<Point<A>> Hull   => _hull;

    public Quickhull(Point<A>[] ps)
    {
        _lines  = new List<Line<A>>();
        _points = ps;
        _hull   = new List<Point<A>>();
    }

    public void Recur(Line<A> line, IEnumerable<Point<A>> points)
        where OrdA : Ord<A>
        where NumA : Num<A>
    {
        _hull.AddRange(points);
    }

    public void Run()
        where OrdA : Ord<A>
        where NumA : Num<A>
    {
        _lines  = new List<Line<A>>();
        _hull   = new List<Point<A>>();

        var minX = Minimum<Point<A>, OrdPointX<A, OrdA>>(_points);
        var maxX = Maximum<Point<A>, OrdPointX<A, OrdA>>(_points);

        _hull.Add(minX);
        _hull.Add(maxX);

        var ln = new Line<A> { P1 = minX, P2 = maxX };
        _lines.Add(ln);


        var onRight = from Point<A> point in Points
                      where ln.OnRight(point)
                      select point;
        Recur(ln, onRight);

        var onLeft = from Point<A> point in Points
                     where !ln.OnRight(point)
                     select point;
        Recur(ln, onLeft);
    }
}


public class QuickhullDriver
{
    private Graphics gfx;
    private Bitmap bmp;
    private int c;
    private int w;
    private int h;

    public QuickhullDriver(int width, int height, int count)
    {
        bmp = new Bitmap(width, height);
        gfx = Graphics.FromImage(bmp);
        c = count;
        w = width;
        h = height;
    }

    public Bitmap Run()
    {
        var rando = new Random();
        var pts = new Point<Int32>[c];

        var maxx = w - 1;
        var maxy = h - 1;

        for (int i = 0; i < c; i++)
        {
            pts[i] = new Point<Int32> { X = rando.Next(0, maxx), Y = rando.Next(0, maxy) };
        }

        var hull = new Quickhull<Int32>(pts);
        // TODO: improve inference here.
        hull.Run<OrdInt, NumInt>();

        Draw(hull.Points, Color.Green);
        Draw(hull.Lines, Color.Red);
        Draw(hull.Hull, Color.Blue);

        return bmp;
    }

    private void Draw<A>(A item, Color colour)
        where DA : Drawable<A>
    {
        Draw(item, colour, gfx);
    }

    public static void Main()
    {
        var bmp = new QuickhullDriver(640, 480, 100).Run();
        bmp.Save("hull.png", System.Drawing.Imaging.ImageFormat.Png);
    }
}