using Baker76.Pngcs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace Baker76.Pngcs.Chunks
{
    /// <summary>
    /// Chunks written or queued to be written 
    /// http://www.w3.org/TR/PNG/#table53
    /// </summary>
    ///
    public class ChunksListForWrite : ChunksList
    {
        private List<PngChunk> queuedChunks; // chunks not yet writen - does not include IHDR, IDAT, END, perhaps yes PLTE

        // redundant, just for eficciency
        private Dictionary<String, int> alreadyWrittenKeys;

        internal ChunksListForWrite(ImageInfo info)
            : base(info)
        {
            this.queuedChunks = new List<PngChunk>();
            this.alreadyWrittenKeys = new Dictionary<String, int>();
        }

        /// <summary>
        /// Same as <c>getById()</c>, but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public List<PngChunk> GetQueuedById(String id)
        {
            return GetQueuedById(id, null);
        }
        /// <summary>
        /// Same as <c>getById()</c>, but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="innerid"></param>
        /// <returns></returns>
        public List<PngChunk> GetQueuedById(String id, String innerid)
        {
            return GetXById(queuedChunks, id, innerid);
        }

        /// <summary>
        /// Same as <c>getById()</c>, but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="innerid"></param>
        /// <param name="failIfMultiple"></param>
        /// <returns></returns>
        public PngChunk GetQueuedById1(String id, String innerid, bool failIfMultiple)
        {
            List<PngChunk> list = GetQueuedById(id, innerid);
            if (list.Count == 0)
                return null;
            if (list.Count > 1 && (failIfMultiple || !list[0].AllowsMultiple()))
                throw new PngjException("unexpected multiple chunks id=" + id);
            return list[list.Count - 1];
        }
        /// <summary>
        /// Same as <c>getById1()</c>, but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <param name="failIfMultiple"></param>
        /// <returns></returns>
        public PngChunk GetQueuedById1(String id, bool failIfMultiple)
        {
            return GetQueuedById1(id, null, failIfMultiple);
        }
        /// <summary>
        /// Same as getById1(), but looking in the queued chunks
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public PngChunk GetQueuedById1(String id)
        {
            return GetQueuedById1(id, false);
        }

        /// <summary>
        ///Remove Chunk: only from queued 
        /// </summary>
        /// <remarks>
        /// WARNING: this depends on chunk.Equals() implementation, which is straightforward for SingleChunks. For 
        /// MultipleChunks, it will normally check for reference equality!
        /// </remarks>
        /// <param name="c"></param>
        /// <returns></returns>
        public bool RemoveChunk(PngChunk c)
        {
            return queuedChunks.Remove(c);
        }

        /// <summary>
        /// Adds chunk to queue
        /// </summary>
        /// <remarks>Does not check for duplicated or anything</remarks>
        /// <param name="chunk"></param>
        /// <returns></returns>
        public bool Queue(PngChunk chunk)
        {
            queuedChunks.Add(chunk);
            return true;
        }

        /**
         * this should be called only for ancillary chunks and PLTE (groups 1 - 3 - 5)
         **/
        private static bool shouldWrite(PngChunk c, int currentGroup)
        {
            if (currentGroup == CHUNK_GROUP_2_PLTE)
                return c.Id.Equals(ChunkHelper.PLTE);
            if (currentGroup % 2 == 0)
                throw new PngjOutputException("bad chunk group?");
            int minChunkGroup, maxChunkGroup;
            if (c.mustGoBeforePLTE())
                minChunkGroup = maxChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            else if (c.mustGoBeforeIDAT())
            {
                maxChunkGroup = ChunksList.CHUNK_GROUP_3_AFTERPLTE;
                minChunkGroup = c.mustGoAfterPLTE() ? ChunksList.CHUNK_GROUP_3_AFTERPLTE
                        : ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            }
            else
            {
                maxChunkGroup = ChunksList.CHUNK_GROUP_5_AFTERIDAT;
                minChunkGroup = ChunksList.CHUNK_GROUP_1_AFTERIDHR;
            }

            int preferred = maxChunkGroup;
            if (c.Priority)
                preferred = minChunkGroup;
            if (ChunkHelper.IsUnknown(c) && c.ChunkGroup > 0)
                preferred = c.ChunkGroup;
            if (currentGroup == preferred)
                return true;
            if (currentGroup > preferred && currentGroup <= maxChunkGroup)
                return true;
            return false;
        }

        internal int writeChunks(Stream os, int currentGroup)
        {
            List<int> written = new List<int>();
            for (int i = 0; i < queuedChunks.Count; i++)
            {
                PngChunk c = queuedChunks[i];
                if (!shouldWrite(c, currentGroup))
                    continue;
                if (ChunkHelper.IsCritical(c.Id) && !c.Id.Equals(ChunkHelper.PLTE))
                    throw new PngjOutputException("bad chunk queued: " + c);
                if (alreadyWrittenKeys.ContainsKey(c.Id) && !c.AllowsMultiple())
                    throw new PngjOutputException("duplicated chunk does not allow multiple: " + c);
                c.write(os);
                chunks.Add(c);
                alreadyWrittenKeys[c.Id] = alreadyWrittenKeys.ContainsKey(c.Id) ? alreadyWrittenKeys[c.Id] + 1 : 1;
                written.Add(i);
                c.ChunkGroup = currentGroup;
            }
            for (int k = written.Count - 1; k >= 0; k--)
            {
                queuedChunks.RemoveAt(written[k]);
            }
            return written.Count;
        }

        /// <summary>
        /// chunks not yet writen - does not include IHDR, IDAT, END, perhaps yes PLTE
        /// </summary>
        /// <returns>THis is not a copy! Don't modify</returns>
        internal List<PngChunk> GetQueuedChunks()
        {
            return queuedChunks;
        }
    }
}
