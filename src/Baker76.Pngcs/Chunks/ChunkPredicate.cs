﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Baker76.Pngcs.Chunks
{
    /// <summary>
    /// Decides if another chunk "matches", according to some criterion
    /// </summary>
    public interface ChunkPredicate
    {
        /// <summary>
        /// The other chunk matches with this one
        /// </summary>
        /// <param name="chunk">The other chunk</param>
        /// <returns>true if matches</returns>
        bool Matches(PngChunk chunk);
    }
}
