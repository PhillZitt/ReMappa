using System;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace ReMappa.Config
{
  public abstract class Base64GZipString
  {
    protected virtual string EncodedZippedString { get; set; }

    public string ReturnDecodedAndDeflatedString()
    {
      return Deflate(Decode(EncodedZippedString));
    }

    private byte[] Decode(string String)
    {
      return Convert.FromBase64String(String);
    }

    private string Deflate(byte[] String)
    {
      using (var inputStream = new MemoryStream(String))
      using (var gzip = new GZipStream(inputStream, CompressionMode.Decompress))
      using (var outputStream = new MemoryStream())
      {
        gzip.CopyTo(outputStream);
        return Encoding.UTF8.GetString(outputStream.ToArray());
      }
    }
  }
}
