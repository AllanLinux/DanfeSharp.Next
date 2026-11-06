using System;
using System.IO;
using DanfeSharp.Graphics;
using PdfSharp.Drawing;
using PdfSharp.Pdf;

namespace DanfeSharp
{
    /// <summary>
    /// Base dos documentos auxiliares (DANFE, DANFE NFC-e, eventos, DACTE e DANFSe): documento PDF, fontes, logotipo e gravação.
    /// </summary>
    public abstract class DocumentoAuxiliar : IDisposable
    {
        internal PdfDocument PdfDocument { get; private set; }

        internal FamiliaFonte FonteRegular { get; private set; }
        internal FamiliaFonte FonteNegrito { get; private set; }
        internal FamiliaFonte FonteItalico { get; private set; }

        private XImage _LogoObject = null;
        private MemoryStream _LogoStream = null;

        /// <summary>
        /// Logotipo do emitente, quando informado.
        /// </summary>
        internal XImage Logo => _LogoObject;

        protected DocumentoAuxiliar()
        {
            PdfDocument = new PdfDocument();
            PdfDocument.Options.CompressContentStreams = true;

            // De acordo com o item 7.7, a fonte deve ser Times New Roman ou Courier New.
            FonteRegular = FamiliaFonte.TimesRegular();
            FonteNegrito = FamiliaFonte.TimesNegrito();
            FonteItalico = FamiliaFonte.TimesItalico();
        }

        /// <summary>
        /// Texto da assinatura impressa no rodapé dos documentos.
        /// </summary>
        public const String TextoAssinatura = "Impresso com DanfeSharp.Next";

        /// <summary>
        /// Imprime a assinatura (créditos) no rodapé. Padrão: true.
        /// Deve ser definido antes de chamar <see cref="Gerar"/>.
        /// </summary>
        public bool ExibirAssinatura { get; set; } = true;

        /// <summary>
        /// Texto de créditos impresso no rodapé (ex.: nome do seu sistema). Padrão: <see cref="TextoAssinatura"/>.
        /// Vazio ou nulo não imprime nada. Deve ser definido antes de <see cref="Gerar"/>.
        /// </summary>
        public String Creditos { get; set; } = TextoAssinatura;

        /// <summary>
        /// Define o texto de créditos impresso no rodapé. Vazio ou nulo remove os créditos.
        /// </summary>
        public DocumentoAuxiliar DefinirCreditos(String creditos)
        {
            Creditos = creditos;
            return this;
        }

        internal bool ImprimirCreditos => ExibirAssinatura && !String.IsNullOrWhiteSpace(Creditos);

        /// <summary>
        /// Desenha o documento. Deve ser chamado uma única vez, antes de salvar.
        /// </summary>
        public abstract void Gerar();

        public void AdicionarLogoImagem(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            try
            {
                DefinirLogo(stream, s => XImage.FromStream(s));
            }
            catch (Exception e) when (!(e is ArgumentException))
            {
                throw new InvalidOperationException("O logotipo não pode ser carregado, certifique-se que a imagem esteja no formato JPEG, PNG, GIF, BMP ou TIFF.", e);
            }
        }

        public void AdicionarLogoPdf(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            DefinirLogo(stream, s => XPdfForm.FromStream(s));
        }

        public void AdicionarLogoImagem(String path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                AdicionarLogoImagem(fs);
            }
        }

        public void AdicionarLogoPdf(String path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));

            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                AdicionarLogoPdf(fs);
            }
        }

        /// <summary>
        /// Copia o logotipo para a memória, pois o PDFsharp lê o conteúdo novamente ao salvar o documento.
        /// </summary>
        private void DefinirLogo(Stream stream, Func<Stream, XImage> carregar)
        {
            var ms = new MemoryStream();
            stream.CopyTo(ms);
            ms.Position = 0;

            var logo = carregar(ms);

            _LogoObject?.Dispose();
            _LogoStream?.Dispose();
            _LogoObject = logo;
            _LogoStream = ms;
        }

        internal Estilo CriarEstilo(float tFonteCampoCabecalho = 6, float tFonteCampoConteudo = 10)
        {
            return new Estilo(FonteRegular, FonteNegrito, FonteItalico, tFonteCampoCabecalho, tFonteCampoConteudo);
        }

        /// <summary>
        /// Adiciona uma página com as medidas informadas, em mm.
        /// </summary>
        internal PdfPage AdicionarPagina(float largura, float altura)
        {
            var pagina = PdfDocument.AddPage();
            pagina.Width = XUnit.FromPoint(largura.ToPoint());
            pagina.Height = XUnit.FromPoint(altura.ToPoint());
            return pagina;
        }

        /// <summary>
        /// Grava as informações do documento PDF.
        /// </summary>
        internal void DefinirMetadados(String tipoDocumento, String titulo, String chaveAcesso)
        {
            var info = PdfDocument.Info;
            if (!String.IsNullOrWhiteSpace(chaveAcesso)) info.Elements.SetString("/ChaveAcesso", chaveAcesso);
            info.Elements.SetString("/TipoDocumento", tipoDocumento);
            info.CreationDate = DateTime.Now;
            info.Creator = String.Format("{0} {1} - {2}", "DanfeSharp.Next", System.Reflection.Assembly.GetExecutingAssembly().GetName().Version, "https://github.com/AllanLinux/DanfeSharp.Next");
            info.Title = titulo;
        }

        public void Salvar(String path)
        {
            if (String.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));

            PdfDocument.Save(path);
        }

        public void Salvar(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            PdfDocument.Save(stream, false);
        }

        #region IDisposable Support
        private bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    PdfDocument.Dispose();
                    _LogoObject?.Dispose();
                    _LogoStream?.Dispose();
                }

                disposedValue = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
        }
        #endregion
    }
}
