﻿using System.IO;

namespace FBCore.Common.File
{
    /// <summary>
    /// Represents an unknown rename exception
    /// </summary>
    public class RenameException : IOException
    {
        /// <summary>
        /// Instantiates the <see cref="RenameException"/>
        /// </summary>
        /// <param name="message"></param>
        public RenameException(string message) : base(message)
        {
        }
    }
}
