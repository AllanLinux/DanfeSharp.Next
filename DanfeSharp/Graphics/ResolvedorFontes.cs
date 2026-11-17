using System;
using System.Collections.Generic;
using System.IO;
#if !NET452
using PdfSharp.Fonts;
#endif

namespace DanfeSharp.Graphics
{
    /// <summary>
    /// Localiza os arquivos da fonte Times New Roman usados no DANFE.
    /// No .NET Framework o PDFsharp usa as fontes instaladas no Windows e este resolvedor não é necessário.
    /// No .NET 8+ ele procura a Times New Roman (Windows e macOS) ou a Liberation Serif, que tem as mesmas métricas (Linux).
    /// Outros arquivos podem ser informados com <see cref="DanfeSharp.Fontes.Registrar(string, string, string)"/>.
    /// </summary>
    internal static class ResolvedorFontes
    {
        internal const String FaceRegular = "DanfeSharp#Regular";
        internal const String FaceNegrito = "DanfeSharp#Negrito";
        internal const String FaceItalico = "DanfeSharp#Italico";
        internal const String FaceArialNegrito = "DanfeSharp#ArialNegrito";
        internal const String FaceSansSerif = "DanfeSharp#SansSerif";

        private static readonly object _Lock = new object();
        private static readonly Dictionary<String, byte[]> _Faces = new Dictionary<String, byte[]>();
#if !NET452
        private static bool _Configurado;
#endif

        private static readonly String[][] _Candidatos =
        {
            // Windows
            new[] { @"%WINDIR%\Fonts\times.ttf", @"%WINDIR%\Fonts\timesbd.ttf", @"%WINDIR%\Fonts\timesi.ttf" },
            // macOS
            new[] { "/System/Library/Fonts/Supplemental/Times New Roman.ttf", "/System/Library/Fonts/Supplemental/Times New Roman Bold.ttf", "/System/Library/Fonts/Supplemental/Times New Roman Italic.ttf" },
            new[] { "/Library/Fonts/Times New Roman.ttf", "/Library/Fonts/Times New Roman Bold.ttf", "/Library/Fonts/Times New Roman Italic.ttf" },
            // Linux, pacote ttf-mscorefonts-installer
            new[] { "/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman.ttf", "/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman_Bold.ttf", "/usr/share/fonts/truetype/msttcorefonts/Times_New_Roman_Italic.ttf" },
            // Linux, Liberation Serif (Debian/Ubuntu, Fedora/RHEL e Alpine)
            new[] { "/usr/share/fonts/truetype/liberation/LiberationSerif-Regular.ttf", "/usr/share/fonts/truetype/liberation/LiberationSerif-Bold.ttf", "/usr/share/fonts/truetype/liberation/LiberationSerif-Italic.ttf" },
            new[] { "/usr/share/fonts/liberation-serif/LiberationSerif-Regular.ttf", "/usr/share/fonts/liberation-serif/LiberationSerif-Bold.ttf", "/usr/share/fonts/liberation-serif/LiberationSerif-Italic.ttf" },
            new[] { "/usr/share/fonts/liberation/LiberationSerif-Regular.ttf", "/usr/share/fonts/liberation/LiberationSerif-Bold.ttf", "/usr/share/fonts/liberation/LiberationSerif-Italic.ttf" },
        };

        // Arial negrito (DANFSe). Sem ela, usa a Times New Roman negrito.
        private static readonly String[] _CandidatosArialNegrito =
        {
            @"%WINDIR%\Fonts\arialbd.ttf",
            "/System/Library/Fonts/Supplemental/Arial Bold.ttf",
            "/Library/Fonts/Arial Bold.ttf",
            "/usr/share/fonts/truetype/msttcorefonts/Arial_Bold.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Bold.ttf",
            "/usr/share/fonts/liberation-sans/LiberationSans-Bold.ttf",
            "/usr/share/fonts/liberation/LiberationSans-Bold.ttf",
        };

        // Microsoft Sans Serif (DANFSe), ou fontes parecidas. Sem nenhuma, usa a Times New Roman.
        private static readonly String[] _CandidatosSansSerif =
        {
            @"%WINDIR%\Fonts\micross.ttf",
            "/System/Library/Fonts/Supplemental/Microsoft Sans Serif.ttf",
            @"%WINDIR%\Fonts\arial.ttf",
            "/System/Library/Fonts/Supplemental/Arial.ttf",
            "/Library/Fonts/Arial.ttf",
            "/usr/share/fonts/truetype/msttcorefonts/Arial.ttf",
            "/usr/share/fonts/truetype/liberation/LiberationSans-Regular.ttf",
            "/usr/share/fonts/liberation-sans/LiberationSans-Regular.ttf",
            "/usr/share/fonts/liberation/LiberationSans-Regular.ttf",
        };

        /// <summary>
        /// Registra os arquivos de fonte a serem usados no lugar da Times New Roman.
        /// </summary>
        internal static void Registrar(byte[] regular, byte[] negrito, byte[] italico)
        {
            lock (_Lock)
            {
                _Faces[FaceRegular] = regular ?? throw new ArgumentNullException(nameof(regular));
                _Faces[FaceNegrito] = negrito ?? throw new ArgumentNullException(nameof(negrito));
                _Faces[FaceItalico] = italico ?? throw new ArgumentNullException(nameof(italico));
            }
        }

        /// <summary>
        /// Instala o resolvedor no PDFsharp, caso a aplicação não tenha definido um próprio.
        /// </summary>
        internal static void Configurar()
        {
#if !NET452
            lock (_Lock)
            {
                if (_Configurado) return;
                _Configurado = true;

                if (GlobalFontSettings.FontResolver == null)
                {
                    GlobalFontSettings.FontResolver = new Resolvedor();
                }
            }
#endif
        }

        private static void CarregarFacesPadrao()
        {
            if (_Faces.ContainsKey(FaceRegular)) return;

            foreach (var candidato in _Candidatos)
            {
                var arquivos = Array.ConvertAll(candidato, Environment.ExpandEnvironmentVariables);

                if (Array.TrueForAll(arquivos, File.Exists))
                {
                    Registrar(File.ReadAllBytes(arquivos[0]), File.ReadAllBytes(arquivos[1]), File.ReadAllBytes(arquivos[2]));
                    return;
                }
            }

            throw new InvalidOperationException("A fonte Times New Roman não foi encontrada. Instale a Times New Roman ou a Liberation Serif (pacote fonts-liberation), " +
                "ou informe os arquivos da fonte com DanfeSharp.Fontes.Registrar.");
        }

        /// <summary>
        /// Carrega a primeira fonte encontrada entre os candidatos; sem nenhuma, usa a face da Times New Roman informada.
        /// </summary>
        private static void CarregarFaceOpcional(String face, String[] candidatos, String faceTimes)
        {
            if (_Faces.ContainsKey(face)) return;

            var arquivo = Array.Find(Array.ConvertAll(candidatos, Environment.ExpandEnvironmentVariables), File.Exists);
            if (arquivo != null)
            {
                _Faces[face] = File.ReadAllBytes(arquivo);
            }
            else
            {
                CarregarFacesPadrao();
                _Faces[face] = _Faces[faceTimes];
            }
        }

        private static byte[] ObterFace(String face)
        {
            lock (_Lock)
            {
                if (face == FaceArialNegrito) CarregarFaceOpcional(FaceArialNegrito, _CandidatosArialNegrito, FaceNegrito);
                if (face == FaceSansSerif) CarregarFaceOpcional(FaceSansSerif, _CandidatosSansSerif, FaceRegular);

                CarregarFacesPadrao();
                return _Faces.TryGetValue(face, out byte[] dados) ? dados : null;
            }
        }

#if !NET452
        private class Resolvedor : IFontResolver
        {
            public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
            {
                if (String.Equals(familyName, FamiliaFonte.NomeArial, StringComparison.OrdinalIgnoreCase))
                    return new FontResolverInfo(FaceArialNegrito);

                if (String.Equals(familyName, FamiliaFonte.NomeSansSerif, StringComparison.OrdinalIgnoreCase))
                    return new FontResolverInfo(FaceSansSerif);

                if (!String.Equals(familyName, FamiliaFonte.NomeTimesNewRoman, StringComparison.OrdinalIgnoreCase)) return null;

                if (bold) return new FontResolverInfo(FaceNegrito);
                if (italic) return new FontResolverInfo(FaceItalico);
                return new FontResolverInfo(FaceRegular);
            }

            public byte[] GetFont(string faceName) => ObterFace(faceName);
        }
#endif
    }
}
