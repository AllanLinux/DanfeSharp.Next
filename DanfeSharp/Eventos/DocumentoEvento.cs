using System;
using System.Drawing;
using System.Linq;
using DanfeSharp.Elementos;
using DanfeSharp.Graphics;
using PdfSharp.Drawing;

namespace DanfeSharp.Eventos
{
    /// <summary>
    /// Documento auxiliar de evento da NF-e/NFC-e em A4: carta de correção eletrônica (CC-e), cancelamento e demais eventos.
    /// </summary>
    public class DocumentoEvento : DocumentoAuxiliar
    {
        private const float Margem = 5F;
        private const float AlturaCabecalho = 30F;

        public EventoViewModel ViewModel { get; private set; }

        private readonly Estilo _Estilo;
        private bool _FoiGerado;

        public DocumentoEvento(EventoViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _Estilo = CriarEstilo();
            DefinirMetadados("EVENTO", $"{ViewModel.Titulo} (Documento auxiliar de evento da NF-e)", ViewModel.ChaveAcesso);
        }

        public override void Gerar()
        {
            if (_FoiGerado) throw new InvalidOperationException("O documento do evento já foi gerado.");

            var pagina = AdicionarPagina(Constantes.A4Largura, Constantes.A4Altura);
            var area = new RectangleF(Margem, Margem, Constantes.A4Largura - 2 * Margem, Constantes.A4Altura - 2 * Margem);

            using (var gfx = new Gfx(XGraphics.FromPdfPage(pagina)))
            {
                var pilha = MontarPilha();
                pilha.Width = area.Width;
                pilha.SetPosition(area.X, area.Y);
                pilha.Draw(gfx);
                gfx.Stroke();

                if (ViewModel.IsHomologacao)
                {
                    var corpo = new RectangleF(area.X, area.Y + pilha.Height, area.Width, area.Height - pilha.Height);
                    var ts = new TextStack(corpo) { AlinhamentoVertical = AlinhamentoVertical.Centro, AlinhamentoHorizontal = AlinhamentoHorizontal.Centro, LineHeightScale = 0.9F }
                        .AddLine("SEM VALOR FISCAL", new Fonte(FonteRegular, 48))
                        .AddLine("AMBIENTE DE HOMOLOGAÇÃO", new Fonte(FonteRegular, 30));

                    gfx.BeginLocalState();
                    gfx.SetFillColor(0.35, 0.35, 0.35);
                    ts.Draw(gfx);
                    gfx.EndLocalState();
                }

                if (ImprimirCreditos)
                    gfx.DrawString(Creditos, new RectangleF(area.X, area.Bottom + 0.5F, area.Width, 4F), new Fonte(FonteItalico, 6), AlinhamentoHorizontal.Direita);
            }

            _FoiGerado = true;
        }

        private VerticalStack MontarPilha()
        {
            var m = ViewModel;
            var pilha = new VerticalStack();

            // Cabeçalho: emitente (quando a NF-e foi informada) e título do evento.
            var cabecalho = new FlexibleLine() { Height = AlturaCabecalho };

            if (m.Documento != null)
            {
                cabecalho.ComElemento(new IdentificacaoEmitente(_Estilo, m.Documento) { Logo = Logo })
                         .ComElemento(new TituloEvento(_Estilo, m))
                         .ComLarguras(0, 55);
            }
            else
            {
                cabecalho.ComElemento(new TituloEvento(_Estilo, m)).ComLarguras(100);
            }

            pilha.Add(cabecalho);

            // Documento fiscal ao qual o evento se refere.
            pilha.Add(new CabecalhoBloco(_Estilo, m.Modelo == "65" ? "NOTA FISCAL DE CONSUMIDOR ELETRÔNICA" : "NOTA FISCAL ELETRÔNICA"));
            pilha.Add(Linha()
                .ComCampo("Modelo", m.Modelo, AlinhamentoHorizontal.Centro)
                .ComCampo("Série", m.Serie, AlinhamentoHorizontal.Centro)
                .ComCampo("Número", m.Numero, AlinhamentoHorizontal.Centro)
                .ComCampo("Data de emissão", m.Documento?.DataHoraEmissao.Formatar(), AlinhamentoHorizontal.Centro)
                .ComCampo("Chave de acesso", Formatador.FormatarChaveAcesso(m.ChaveAcesso), AlinhamentoHorizontal.Centro)
                .ComLarguras(8, 8, 14, 14, 0));

            if (!String.IsNullOrWhiteSpace(m.ChaveAcesso))
                pilha.Add(new Barcode128C(m.ChaveAcesso, _Estilo) { Height = 14 });

            if (m.Documento != null)
            {
                var emit = m.Documento.Emitente;
                var dest = m.Documento.Destinatario;

                pilha.Add(Linha()
                    .ComCampo("Emitente", emit.RazaoSocial)
                    .ComCampo("CNPJ/CPF do emitente", Formatador.FormatarCpfCnpj(emit.CnpjCpf), AlinhamentoHorizontal.Centro)
                    .ComLarguras(0, 30));

                pilha.Add(Linha()
                    .ComCampo("Destinatário", dest.RazaoSocial)
                    .ComCampo("CNPJ/CPF do destinatário", Formatador.FormatarCpfCnpj(dest.CnpjCpf), AlinhamentoHorizontal.Centro)
                    .ComLarguras(0, 30));
            }
            else
            {
                pilha.Add(Linha().ComCampo("CNPJ/CPF do autor do evento", Formatador.FormatarCpfCnpj(m.CnpjCpfAutor), AlinhamentoHorizontal.Centro).ComLarguras(100));
            }

            // Dados do evento e do registro na SEFAZ.
            pilha.Add(new CabecalhoBloco(_Estilo, "EVENTO"));
            pilha.Add(Linha()
                .ComCampo("Tipo do evento", $"{m.TipoEvento} - {m.DescricaoEvento}")
                .ComCampo("Sequência", m.Sequencia.ToString(), AlinhamentoHorizontal.Centro)
                .ComCampo("Data e hora do evento", m.DataHoraEvento.FormatarDataHora24(), AlinhamentoHorizontal.Centro)
                .ComCampo("Órgão", m.Orgao.ToString(), AlinhamentoHorizontal.Centro)
                .ComCampo("Ambiente", m.IsHomologacao ? "2 - Homologação" : "1 - Produção", AlinhamentoHorizontal.Centro)
                .ComLarguras(0, 10, 20, 8, 15));

            pilha.Add(Linha()
                .ComCampo("Situação", m.CodigoStatus.HasValue ? $"{m.CodigoStatus} - {m.Motivo}" : "Não registrado")
                .ComCampo("Protocolo do evento", m.ProtocoloEvento, AlinhamentoHorizontal.Centro)
                .ComCampo("Data e hora do registro", m.DataHoraRegistro.FormatarDataHora24(), AlinhamentoHorizontal.Centro)
                .ComLarguras(0, 25, 25));

            // Conteúdo do evento.
            if (m.IsCartaCorrecao)
            {
                pilha.Add(new CabecalhoBloco(_Estilo, "CORREÇÃO"));
                pilha.Add(new CampoMultilinha("Texto da correção", m.Correcao, _Estilo));
                pilha.Add(new CampoMultilinha("Condições de uso", m.CondicaoUso, CriarEstilo(tFonteCampoConteudo: 8)));
            }
            else
            {
                pilha.Add(new CabecalhoBloco(_Estilo, "INFORMAÇÕES DO EVENTO"));

                if (!String.IsNullOrWhiteSpace(m.ProtocoloDocumento) || !String.IsNullOrWhiteSpace(m.ChaveSubstituta))
                {
                    pilha.Add(Linha()
                        .ComCampo("Protocolo de autorização do documento", m.ProtocoloDocumento, AlinhamentoHorizontal.Centro)
                        .ComCampo("Chave de acesso do documento substituto", Formatador.FormatarChaveAcesso(m.ChaveSubstituta), AlinhamentoHorizontal.Centro)
                        .ComLarguras(30, 0));
                }

                if (!String.IsNullOrWhiteSpace(m.Justificativa))
                    pilha.Add(new CampoMultilinha("Justificativa", m.Justificativa, _Estilo));

                if (m.OutrosCampos.Count > 0)
                    pilha.Add(new CampoMultilinha("Detalhes do evento", String.Join("\n", m.OutrosCampos.Select(c => $"{c.Key}: {c.Value}")), CriarEstilo(tFonteCampoConteudo: 8)));
            }

            return pilha;
        }

        private LinhaCampos Linha() => new LinhaCampos(_Estilo) { Height = Constantes.CampoAltura };

        /// <summary>
        /// Título centralizado do documento auxiliar do evento.
        /// </summary>
        private class TituloEvento : ElementoBase
        {
            private readonly EventoViewModel _Evento;

            public TituloEvento(Estilo estilo, EventoViewModel evento) : base(estilo)
            {
                _Evento = evento;
            }

            public override void Draw(Gfx gfx)
            {
                base.Draw(gfx);

                var documento = _Evento.Modelo == "65" ? "da NFC-e" : "da NF-e";
                var ts = new TextStack(BoundingBox.InflatedRetangle(1.5F)) { AlinhamentoHorizontal = AlinhamentoHorizontal.Centro, AlinhamentoVertical = AlinhamentoVertical.Centro }
                    .AddLine("DOCUMENTO AUXILIAR DE EVENTO", Estilo.CriarFonteRegular(8))
                    .AddLine(_Evento.Titulo, Estilo.CriarFonteNegrito(14))
                    .AddLine(documento, Estilo.CriarFonteRegular(9));

                ts.Draw(gfx);
            }
        }
    }
}
