using System;
using System.IO;
using System.Text;
using Synaps.Core;
using Synaps.KnowledgeGraph;

namespace Synaps.Data
{
    // Small local graphs, one writer. A database/cloud repository can replace this via IGraphRepository.
    public sealed class FileGraphRepository : IGraphRepository
    {
        private readonly string path;
        private readonly IGraphSerializer serializer;
        private bool preserveBackupOnNextSave;
        public string FilePath { get { return path; } }

        public FileGraphRepository(string path, IGraphSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException("A file path is required.");
            if (serializer == null) throw new ArgumentNullException("serializer");
            this.path = Path.GetFullPath(path);
            this.serializer = serializer;
        }

        public GraphLoadResult Load()
        {
            if (!File.Exists(path) && !File.Exists(path + ".bak")) return new GraphLoadResult(new GraphDocument());
            Exception primaryFailure = null;
            if (File.Exists(path))
            {
                try { return new GraphLoadResult(Read(path)); }
                catch (InvalidDataException exception) { primaryFailure = exception; }
            }
            if (File.Exists(path + ".bak"))
            {
                try
                {
                    var recovered = Read(path + ".bak");
                    preserveBackupOnNextSave = true;
                    return new GraphLoadResult(recovered, true);
                }
                catch (InvalidDataException exception)
                {
                    throw new InvalidDataException("Both graph and backup are invalid. Restore the files before saving.", exception);
                }
            }
            throw new InvalidDataException("Graph is invalid and no usable backup exists. Original file was preserved.", primaryFailure);
        }

        public void Save(GraphDocument document)
        {
            GraphValidator.Validate(document);
            var bytes = new UTF8Encoding(false).GetBytes(serializer.Serialize(document));
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }
                // Re-read before replacing an existing valid save; catches invalid serializer output.
                Read(temporary);
                if (File.Exists(path))
                {
                    // During recovery, do not overwrite the known-good backup with the corrupt primary.
                    if (preserveBackupOnNextSave)
                    {
                        File.Copy(path, path + ".corrupt-" + Guid.NewGuid().ToString("N"));
                        File.Replace(temporary, path, null);
                    }
                    else File.Replace(temporary, path, path + ".bak");
                }
                else File.Move(temporary, path);
                preserveBackupOnNextSave = false;
            }
            finally { if (File.Exists(temporary)) File.Delete(temporary); }
        }

        private GraphDocument Read(string file)
        {
            var document = serializer.Deserialize(File.ReadAllText(file, Encoding.UTF8));
            GraphValidator.Validate(document);
            return document;
        }
    }
}
