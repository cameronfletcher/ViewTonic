namespace ViewTonic
{
    using System;
    using System.Threading;

    internal static class ViewTonicGuardExtensions
    {
        public static void Negative(this Guard guard, Func<long> expression)
        {
            var value = expression();
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(Guard.Expression.Parse(expression), value, "Value has to be positive.");
            }
        }

        public static void NegativeTimeout(this Guard guard, Func<int> expression)
        {
            var value = expression();
            if (value != Timeout.Infinite && value < 0)
            {
                throw new ArgumentOutOfRangeException(Guard.Expression.Parse(expression), value, "Value has to be positive.");
            }
        }
    }
}