namespace FBCore.Common.Util
{
    /// <summary>
    /// Provides implementation of hashCode for compound objects. Implementation provided by
    /// this class gives the same results as Objects.hashCode, but does not create array consisting of
    /// all components
    /// </summary>
    public class HashCodeUtil
    {
        /// <summary>
        /// Hash code is computed as value of polynomial whose coefficients are determined by hash codes
        /// of objects passed as parameter to one of hashCode functions. More precisely:
        /// hashCode(o1, o2, ..., on) = P[o1, o2, ..., on](X) =
        /// X^n + o1.hashCode() * X ^ (n - 1) + o2.hashCode() * X ^ (n - 2) + ... + on.hashCode() * X ^ 0
        ///
        /// <para /> Constant X determines point at which polynomial is evaluated.
        /// </summary>
        private const int X = 31;

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            object o1)
        {
            return HashCode(
                o1 == null ? 0 : o1.GetHashCode());
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            object o1,
            object o2)
        {
            return HashCode(
                o1 == null ? 0 : o1.GetHashCode(),
                o2 == null ? 0 : o2.GetHashCode());
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            object o1,
            object o2,
            object o3)
        {
            return HashCode(
                o1 == null ? 0 : o1.GetHashCode(),
                o2 == null ? 0 : o2.GetHashCode(),
                o3 == null ? 0 : o3.GetHashCode());
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            object o1,
            object o2,
            object o3,
            object o4)
        {
            return HashCode(
                o1 == null ? 0 : o1.GetHashCode(),
                o2 == null ? 0 : o2.GetHashCode(),
                o3 == null ? 0 : o3.GetHashCode(),
                o4 == null ? 0 : o4.GetHashCode());
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            object o1,
            object o2,
            object o3,
            object o4,
            object o5)
        {
            return HashCode(
                o1 == null ? 0 : o1.GetHashCode(),
                o2 == null ? 0 : o2.GetHashCode(),
                o3 == null ? 0 : o3.GetHashCode(),
                o4 == null ? 0 : o4.GetHashCode(),
                o5 == null ? 0 : o5.GetHashCode());
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            object o1,
            object o2,
            object o3,
            object o4,
            object o5,
            object o6)
        {
            return HashCode(
                o1 == null ? 0 : o1.GetHashCode(),
                o2 == null ? 0 : o2.GetHashCode(),
                o3 == null ? 0 : o3.GetHashCode(),
                o4 == null ? 0 : o4.GetHashCode(),
                o5 == null ? 0 : o5.GetHashCode(),
                o6 == null ? 0 : o6.GetHashCode());
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            int i1)
        {
            int acc = X + i1;
            return acc;
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            int i1,
            int i2)
        {
            int acc = X + i1;
            acc = X * acc + i2;
            return acc;
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            int i1,
            int i2,
            int i3)
        {
            int acc = X + i1;
            acc = X * acc + i2;
            acc = X * acc + i3;
            return acc;
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            int i1,
            int i2,
            int i3,
            int i4)
        {
            int acc = X + i1;
            acc = X * acc + i2;
            acc = X * acc + i3;
            acc = X * acc + i4;
            return acc;
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            int i1,
            int i2,
            int i3,
            int i4,
            int i5)
        {
            int acc = X + i1;
            acc = X * acc + i2;
            acc = X * acc + i3;
            acc = X * acc + i4;
            acc = X * acc + i5;
            return acc;
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            int i1,
            int i2,
            int i3,
            int i4,
            int i5,
            int i6)
        {
            int acc = X + i1;
            acc = X * acc + i2;
            acc = X * acc + i3;
            acc = X * acc + i4;
            acc = X * acc + i5;
            acc = X * acc + i6;
            return acc;
        }

        /// <summary>
        /// Calculates hash code
        /// </summary>
        public static int HashCode(
            string s)
        {
            int length = s.Length;
            int hash = 0;
            for (int i = 0; i < length; ++i)
            {
                hash = 31 * hash + s[i];
            }

            return hash;
        }
    }
}
