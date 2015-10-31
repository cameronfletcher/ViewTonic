namespace ViewTonic
{
    using System;

    internal static class GuardExtensions
    {
        public static void Negative(this Guard guard, Func<long> expression)
        {
            var value = expression();
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(Guard.Expression.Parse(expression), value, "Value has to be positive.");
            }
        }
    }
}