using System;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Threading.Tasks;

// Extract a versioned, bundled worker once. No Python installation or global
// PATH changes are needed on the player's Windows computer.
public static class ChineseNeuralRuntime
{
    static readonly object Gate=new object();
    public static Task<string> Prepare(string assets,string cache)
    {
        return Task.Run(()=>
        {
            lock(Gate)
            {
                string expected=File.ReadAllText(Path.Combine(assets,"neural-runtime.sha256.txt")).Trim();
                if(expected.Length!=64||!System.Text.RegularExpressions.Regex.IsMatch(expected,"^[0-9a-f]+$"))throw new InvalidDataException("Invalid runtime version");
                string folder=Path.Combine(cache,expected.Substring(0,16)),python=Path.Combine(folder,"python.exe");
                if(File.Exists(Path.Combine(folder,"ready.txt"))&&File.Exists(python))return python;
                string zip=Path.Combine(assets,"neural-runtime.zip");
                using(var sha=SHA256.Create())using(var stream=File.OpenRead(zip))
                    if(BitConverter.ToString(sha.ComputeHash(stream)).Replace("-","").ToLowerInvariant()!=expected)throw new InvalidDataException("Voice runtime checksum mismatch");
                Directory.CreateDirectory(cache);
                // Extraction interrupted by an application exit resumes in a
                // fresh private directory; incomplete folders are never used.
                string temporary=Path.Combine(cache,"install-"+Guid.NewGuid().ToString("N"));
                ZipFile.ExtractToDirectory(zip,temporary);
                // Keep the canonical version path stable after an interrupted
                // installation. Preserve any incomplete directory for diagnosis.
                if(Directory.Exists(folder))Directory.Move(folder,Path.Combine(cache,"incomplete-"+Guid.NewGuid().ToString("N")));
                Directory.Move(temporary,folder);File.WriteAllText(Path.Combine(folder,"ready.txt"),expected);
                return Path.Combine(folder,"python.exe");
            }
        });
    }
}
