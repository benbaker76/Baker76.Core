using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Baker76.Pngcs
{
    /// <summary>
    /// Exception for internal problems
    /// </summary>
    [Serializable]
    public class PngjExceptionInternal : Exception
    {
        private const long serialVersionUID = 1L;

        public PngjExceptionInternal()
            : base()
        {
        }

        public PngjExceptionInternal(String message, Exception cause)
            : base(message, cause)
        {
        }

        public PngjExceptionInternal(String message)
            : base(message)
        {
        }

        public PngjExceptionInternal(Exception cause)
            : base(cause.Message, cause)
        {
        }
    }
}
