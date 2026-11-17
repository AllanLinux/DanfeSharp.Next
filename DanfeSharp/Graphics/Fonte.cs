using System;
using System.Collections.Generic;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
#if NET452
using XFontStyleEx = PdfSharp.Drawing.XFontStyle;
#endif

namespace DanfeSharp.Graphics
{
    /// <summary>
    /// Família e estilo de uma fonte do PDF, com cache das fontes por tamanho.
    /// </summary>
    internal class FamiliaFonte
    {
        // De acordo com o item 7.7, a fonte deve ser Times New Roman ou Courier New.
        public const String NomeTimesNewRoman = "Times New Roman";

        // A DANFSe do site usa Arial nos títulos e rótulos.
        public const String NomeArial = "Arial";

        // E Microsoft Sans Serif nos valores.
        public const String NomeSansSerif = "Microsoft Sans Serif";

        private readonly Dictionary<float, XFont> _Fontes = new Dictionary<float, XFont>();

        public String Nome { get; private set; }
        public XFontStyleEx Estilo { get; private set; }

        public FamiliaFonte(String nome, XFontStyleEx estilo)
        {
            Nome = nome ?? throw new ArgumentNullException(nameof(nome));
            Estilo = estilo;
        }

        public XFont ObterFonte(float tamanho)
        {
            lock (_Fontes)
            {
                if (!_Fontes.TryGetValue(tamanho, out XFont fonte))
                {
                    ResolvedorFontes.Configurar();
                    fonte = new XFont(Nome, tamanho, Estilo, new XPdfFontOptions(PdfFontEncoding.WinAnsi));
                    _Fontes.Add(tamanho, fonte);
                }

                return fonte;
            }
        }

        public static FamiliaFonte TimesRegular() => new FamiliaFonte(NomeTimesNewRoman, XFontStyleEx.Regular);
        public static FamiliaFonte TimesNegrito() => new FamiliaFonte(NomeTimesNewRoman, XFontStyleEx.Bold);
        public static FamiliaFonte TimesItalico() => new FamiliaFonte(NomeTimesNewRoman, XFontStyleEx.Italic);
        public static FamiliaFonte ArialNegrito() => new FamiliaFonte(NomeArial, XFontStyleEx.Bold);
        public static FamiliaFonte SansSerifRegular() => new FamiliaFonte(NomeSansSerif, XFontStyleEx.Regular);
    }

    /// <summary>
    /// Define uma fonte do PDF e um tamanho.
    /// </summary>
    internal class Fonte
    {
        // Métricas verticais da Times (AFM da Adobe), as mesmas da versão com PDF Clown,
        // preservando a altura das linhas e dos blocos do leiaute.
        private const float FatorAscendente = 0.683F;
        private const float FatorAlturaLinha = 0.9F;

        private static readonly XGraphics _Medidor = XGraphics.CreateMeasureContext(new XSize(2000, 2000), XGraphicsUnit.Point, XPageDirection.Downwards);

        private float _Tamanho;

        public FamiliaFonte Familia { get; private set; }

        public Fonte(FamiliaFonte familia, float tamanho)
        {
            Familia = familia ?? throw new ArgumentNullException(nameof(familia));
            Tamanho = tamanho;
        }

        /// <summary>
        /// Tamanho da fonte.
        /// </summary>
        public float Tamanho
        {
            get => _Tamanho;
            set
            {
                if (value <= 0) throw new InvalidOperationException("O tamanho deve ser maior que zero.");
                _Tamanho = value;
            }
        }

        /// <summary>
        /// Fonte do PDFsharp no tamanho atual.
        /// </summary>
        public XFont FonteInterna => Familia.ObterFonte(Tamanho);

        /// <summary>
        /// Mede a largura ocupada por uma string.
        /// </summary>
        /// <param name="str">String</param>
        /// <returns>Largura em mm.</returns>
        public float MedirLarguraTexto(String str)
        {
            if (String.IsNullOrEmpty(str)) return 0;

            lock (_Medidor)
            {
                return ((float)_Medidor.MeasureString(str, FonteInterna).Width).ToMm();
            }
        }

        /// <summary>
        /// Mese a largura ocupada por um Char.
        /// </summary>
        /// <param name="c">Char</param>
        /// <returns>Largura em mm.</returns>
        public float MedirLarguraChar(char c) => MedirLarguraTexto(c.ToString());

        /// <summary>
        /// Medida da altura da linha.
        /// </summary>
        public float AlturaLinha => (FatorAlturaLinha * Tamanho).ToMm();

        /// <summary>
        /// Distância do topo da linha até a linha de base do texto, em mm.
        /// </summary>
        public float Ascendente => (FatorAscendente * Tamanho).ToMm();

        public Fonte Clonar() => new Fonte(Familia, Tamanho);

    }
}
