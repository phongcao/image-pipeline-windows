namespace FBCore.Common.Memory
{
    /// <summary>
    /// A class can implement this interface to react to a 
    /// <see cref="IMemoryTrimmableRegistry"/>'s request to trim memory.
    /// </summary>

    public interface IMemoryTrimmable
    {
        /// <summary>
        /// Trim memory.
        /// </summary>
        void Trim(double trimType);
    }
}
