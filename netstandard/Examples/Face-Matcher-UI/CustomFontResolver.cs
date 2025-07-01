using PdfSharp.Fonts;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Face_Matcher_UI
{
    public class CustomFontResolver : IFontResolver
    {
        private static readonly string fontFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Fonts");

        public byte[] GetFont(string faceName)
        {
            string fontPath = Path.Combine(fontFolder, "Roboto-Regular.ttf");
            return File.ReadAllBytes(fontPath);
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            if (familyName.Equals("Roboto", StringComparison.OrdinalIgnoreCase))
            {
                return new FontResolverInfo("Roboto#");
            }

            // Fallback
            return PlatformFontResolver.ResolveTypeface("Arial", isBold, isItalic);
        }
    }
}
