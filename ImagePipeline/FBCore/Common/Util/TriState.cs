using System;

namespace FBCore.Common.Util
{
    /// <summary>
    /// Generic tri-state class for boolean values that can also be unset.
    /// </summary>
    public enum TriState
    {
        /// <summary>
        /// True.
        /// </summary>
        YES = 1,

        /// <summary>
        /// False.
        /// </summary>
        NO = 2,

        /// <summary>
        /// Unset.
        /// </summary>
        UNSET = 3
    }

    /// <summary>
    /// Generic tri-state class for boolean values that can also be unset.
    /// </summary>
    public static class TriStateHelper
    {
        /// <summary>
        /// Whether this value is set; that is, whether it is YES or NO.
        /// </summary>
        public static bool IsSet(TriState value)
        {
            return value != TriState.UNSET;
        }

        /// <summary>
        /// Returns the value of the <see cref="TriState"/> enum that corresponds
        /// to the specified <see cref="bool"/>.
        /// </summary>
        public static TriState ValueOf(bool value)
        {
            return value ? TriState.YES : TriState.NO;
        }

        /// <summary>
        /// Returns the <see cref="bool"/> value that corresponds to 
        /// <see cref="TriState"/> if appropriate.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <returns>
        /// true if <code>value</code> is TriState.YES or false if 
        /// <code>value</code> is TriState.NO.
        /// </returns>
        /// <exception cref="InvalidOperationException">
        /// if <code>value</code> is TriState.UNSET.
        /// </exception>
        public static bool AsBool(TriState value)
        {
            switch (value)
            {
                case TriState.YES:
                    return true;

                case TriState.NO:
                    return false;

                case TriState.UNSET:
                    throw new InvalidOperationException("No boolean equivalent for UNSET");

                default:
                    throw new InvalidOperationException("Unrecognized TriState value: " + value);
            }
        }

        /// <summary>
        /// Returns the <see cref="bool"/> value that corresponds to 
        /// <see cref="TriState"/> if appropriate.
        /// </summary>
        /// <param name="value">Input value.</param>
        /// <param name="defaultValue">Default value to use if not set.</param>
        /// <returns>
        /// true if <code>value</code> is TriState.YES or false if 
        /// <code>value</code> is TriState.NO or <code>defaultValue</code>
        /// if <code>value</code> is TriState.UNSET.
        /// </returns>
        public static bool AsBool(TriState value, bool defaultValue)
        {
            switch (value)
            {
                case TriState.YES:
                    return true;

                case TriState.NO:
                    return false;

                case TriState.UNSET:
                    return defaultValue;

                default:
                    throw new InvalidOperationException("Unrecognized TriState value: " + value);
            }
        }
    }
}
