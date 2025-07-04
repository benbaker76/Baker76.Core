﻿namespace Baker76.Pngcs
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.IO;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Utility functions for C# porting
    /// </summary>
    ///
    internal class PngCsUtils
    {
        internal static bool arraysEqual4(byte[] ar1, byte[] ar2)
        {
            return (ar1[0] == ar2[0]) &&
                   (ar1[1] == ar2[1]) &&
                   (ar1[2] == ar2[2]) &&
                   (ar1[3] == ar2[3]);
        }

        internal static bool arraysEqual(byte[] a1, byte[] a2)
        {
            if (a1.Length != a2.Length) return false;
            for (int i = 0; i < a1.Length; i++)
                if (a1[i] != a2[i]) return false;
            return true;
        }
    }
}
