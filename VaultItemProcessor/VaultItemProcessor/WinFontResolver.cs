using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32;
using PdfSharp.Fonts;

namespace VaultItemProcessor
{
    /// <summary>
    /// Font resolver for PDFSharp 6.x on .NET Framework.
    /// Resolves any installed Windows font via the registry.
    /// </summary>
    public class WinFontResolver : IFontResolver
    {
        private static readonly string FontsFolder =
            Environment.GetFolderPath(Environment.SpecialFolder.Fonts);

        // Maps display name (stripped of "(TrueType)" etc.) -> filename, e.g. "Arial Bold" -> "arialbd.ttf"
        private static readonly Dictionary<string, string> RegistryFontMap =
            BuildRegistryMap();

        private static Dictionary<string, string> BuildRegistryMap()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var key = Registry.LocalMachine.OpenSubKey(
                @"SOFTWARE\Microsoft\Windows NT\CurrentVersion\Fonts"))
            {
                if (key == null) return map;
                foreach (string valueName in key.GetValueNames())
                {
                    string fileName = key.GetValue(valueName) as string;
                    if (string.IsNullOrEmpty(fileName)) continue;

                    // Strip trailing qualifiers like "(TrueType)", "(OpenType)"
                    string name = valueName;
                    int paren = name.IndexOf('(');
                    if (paren >= 0) name = name.Substring(0, paren).Trim();

                    map[name] = fileName;
                }
            }
            return map;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            // Try progressively less specific names until something matches
            string[] candidates = isBold && isItalic
                ? new[] { $"{familyName} Bold Italic", $"{familyName} BoldItalic", familyName }
                : isBold
                ? new[] { $"{familyName} Bold", familyName }
                : isItalic
                ? new[] { $"{familyName} Italic", familyName }
                : new[] { familyName };

            foreach (string candidate in candidates)
            {
                if (RegistryFontMap.TryGetValue(candidate, out string fileName))
                    return new FontResolverInfo(fileName);
            }

            // Last resort: return null so PDFSharp reports a meaningful error
            return null;
        }

        public byte[] GetFont(string faceName)
        {
            // faceName is the font filename returned from ResolveTypeface
            string fullPath = Path.IsPathRooted(faceName)
                ? faceName
                : Path.Combine(FontsFolder, faceName);

            return File.ReadAllBytes(fullPath);
        }
    }
}
