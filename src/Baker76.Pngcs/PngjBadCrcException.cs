using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Baker76.Pngcs
{
    /// <summary>
    /// Exception for CRC check
    /// </summary>
    [Serializable]
    public class PngjBadCrcException : PngjException
    {
        private const long serialVersionUID = 1L;

        public PngjBadCrcException(String message, Exception cause)
            : base(message, cause)
        {
        }

        public PngjBadCrcException(String message)
            : base(message)
        {
        }

        public PngjBadCrcException(Exception cause)
            : base(cause)
        {
        }
    }
}
