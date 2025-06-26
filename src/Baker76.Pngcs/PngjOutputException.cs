using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Baker76.Pngcs
{
    /// <summary>
    /// Exception associated with input (reading) operations
    /// </summary>
    [Serializable]
    public class PngjOutputException : PngjException
    {
        private const long serialVersionUID = 1L;

        public PngjOutputException(String message, Exception cause)
            : base(message, cause)
        {
        }

        public PngjOutputException(String message)
            : base(message)
        {
        }

        public PngjOutputException(Exception cause)
            : base(cause)
        {
        }
    }
}
