using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Unicord.Abstractions
{
    public interface IMediaAbstractions
    {

        Task<string> GetFileMimeAsync(string path);
        Task<bool> TryTranscodeAudioAsync(string file, Stream stream, bool hq, IProgress<double?> progress);
        Task<bool> TryTranscodeImageAsync(string file, Stream stream, bool hq, IProgress<double?> progress);
        Task<bool> TryTranscodeVideoAsync(string file, Stream stream, bool hq, IProgress<double?> progress);
    }
}
