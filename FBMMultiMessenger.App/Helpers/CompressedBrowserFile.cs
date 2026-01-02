using Microsoft.AspNetCore.Components.Forms;

namespace FBMMultiMessenger.Helpers
{
    public class CompressedBrowserFile : IBrowserFile
    {
        public readonly byte[] _data;

        public CompressedBrowserFile(string name, byte[] data, string contentType = "image/jpeg")
        {
            _data = data;
            Name = name;
            ContentType = contentType;
            Size = data.Length;
            LastModified = DateTimeOffset.Now;
        }

        public string Name { get; }
        public DateTimeOffset LastModified { get; }
        public long Size { get; }
        public string ContentType { get; }

        public Stream OpenReadStream(long maxAllowedSize = 25 * 1024 * 1024, CancellationToken cancellationToken = default)
        {
            if (Size > maxAllowedSize)
            {
                throw new IOException($"The file size ({Size} bytes) exceeds the maximum allowed size ({maxAllowedSize} bytes).");
            }

            return new MemoryStream(_data);
        }
    }
}
