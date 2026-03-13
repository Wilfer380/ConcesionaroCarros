using PdfSharp.Fonts;
using System;
using System.IO;

namespace ConcesionaroCarros
{
    public class FontResolver : IFontResolver
    {
        private static byte[] _fontData;

        public byte[] GetFont(string faceName)
        {
            if (_fontData == null)
            {
                string ruta = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory,
                    "Fonts",
                    "Roboto-Regular.ttf");

                _fontData = File.ReadAllBytes(ruta);
            }

            return _fontData;
        }

        public FontResolverInfo ResolveTypeface(string familyName, bool isBold, bool isItalic)
        {
            return new FontResolverInfo("Roboto-Regular");
        }
    }
}
