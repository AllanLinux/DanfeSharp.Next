using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DanfeSharp.Elementos;
using DanfeSharp.Graphics;
using DanfeSharp.Modelo;
using PdfSharp.Drawing;

namespace DanfeSharp.CTe
{
    /// <summary>
    /// DACTE (Documento Auxiliar do Conhecimento de Transporte Eletrônico), CT-e 4.00 modelo 57, em A4 retrato.
    /// Os documentos originários continuam nas folhas seguintes quando não cabem na primeira.
    /// </summary>
    public class Dacte : DocumentoAuxiliar
    {
        private const float Margem = 4F;
        private const float AlturaCabecalho = 34F;

        public DacteViewModel ViewModel { get; private set; }

        private readonly Estilo _Estilo;
        private readonly Estilo _EstiloTabela;
        private bool _FoiGerado;

        public Dacte(DacteViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            _Estilo = CriarEstilo(5.5F, 8F);
            _EstiloTabela = CriarEstilo();
            DefinirMetadados("DACTE", "DACTE (Documento auxiliar do CT-e)", ViewModel.ChaveAcesso);
        }

        public override void Gerar()
        {
            if (_FoiGerado) throw new InvalidOperationException("O DACTE já foi gerado.");

            var tabela = CriarTabelaDocumentos();
            var paginas = new List<Tuple<Gfx, Campo>>();

            try
            {
                while (true)
                {
                    bool primeira = paginas.Count == 0;
                    var gfx = new Gfx(XGraphics.FromPdfPage(AdicionarPagina(Constantes.A4Largura, Constantes.A4Altura)));
                    var area = new RectangleF(Margem, Margem, Constantes.A4Largura - 2 * Margem, Constantes.A4Altura - 2 * Margem - 3F);

                    Campo campoFolha;
                    var topo = primeira ? MontarTopo(out campoFolha) : MontarCabecalho(out campoFolha);
                    paginas.Add(Tuple.Create(gfx, campoFolha));

                    Preparar(topo, area.Width);
                    topo.SetPosition(area.X, area.Y);
                    topo.Draw(gfx);

                    var corpo = area.CutTop(topo.Height);

                    if (primeira)
                    {
                        var rodape = MontarRodape();
                        Preparar(rodape, area.Width);
                        rodape.SetPosition(area.X, area.Bottom - rodape.Height);
                        rodape.Draw(gfx);
                        corpo = corpo.CutBottom(rodape.Height);
                    }

                    int linhaAnterior = tabela.LinhaAtual;
                    var cabecalhoDocumentos = new CabecalhoBloco(_Estilo, primeira ? "DOCUMENTOS ORIGINÁRIOS" : "DOCUMENTOS ORIGINÁRIOS (CONTINUAÇÃO)") { Width = corpo.Width };
                    cabecalhoDocumentos.SetPosition(corpo.X, corpo.Y);
                    cabecalhoDocumentos.Draw(gfx);

                    var areaTabela = corpo.CutTop(cabecalhoDocumentos.Height);
                    tabela.SetPosition(areaTabela.Location);
                    tabela.SetSize(areaTabela.Size);
                    tabela.Draw(gfx);
                    gfx.StrokeRectangle(areaTabela, 0.25F);
                    gfx.Stroke();

                    if (ViewModel.IsHomologacao) DesenharAvisoHomologacao(gfx, corpo);

                    if (ExibirAssinatura)
                        gfx.DrawString(TextoAssinatura, new RectangleF(area.X, area.Bottom + 0.5F, area.Width, 3F), new Fonte(FonteItalico, 6), AlinhamentoHorizontal.Direita);

                    bool terminou = tabela.LinhaAtual >= tabela.Linhas.Count;
                    bool semProgresso = !primeira && tabela.LinhaAtual == linhaAnterior;
                    if (terminou || semProgresso) break;
                }

                // Número das folhas, conhecido só no final.
                var fonteFolha = new Fonte(FonteNegrito, 8);
                for (int i = 0; i < paginas.Count; i++)
                {
                    var campo = paginas[i].Item2;
                    paginas[i].Item1.DrawString($"{i + 1}/{paginas.Count}", campo.BoundingBox.InflatedRetangle(0.75F), fonteFolha, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Base);
                }
            }
            finally
            {
                foreach (var p in paginas) p.Item1.Dispose();
            }

            _FoiGerado = true;
        }

        private Tabela CriarTabelaDocumentos()
        {
            var tabela = new Tabela(_EstiloTabela)
                .ComColuna(16, AlinhamentoHorizontal.Centro, "TIPO DOC")
                .ComColuna(0, AlinhamentoHorizontal.Centro, "CHAVE DE ACESSO / CNPJ / IDENTIFICAÇÃO")
                .ComColuna(22, AlinhamentoHorizontal.Centro, "SÉRIE / NÚMERO");

            tabela.AjustarLarguraColunas();

            foreach (var d in ViewModel.Documentos)
                tabela.AdicionarLinha(new List<String> { d.Tipo, d.Identificacao, d.SerieNumero });

            return tabela;
        }

        #region Primeira folha

        private VerticalStack MontarTopo(out Campo campoFolha)
        {
            var m = ViewModel;
            var pilha = new VerticalStack();

            pilha.Add(MontarCanhoto(), new LinhaTracejada(2));
            pilha.Add(MontarIdentificacao(out campoFolha));

            pilha.Add(Linha()
                .ComCampo("Tipo do CT-e", m.DescricaoTipoCte, AlinhamentoHorizontal.Centro)
                .ComCampo("Tipo do serviço", m.DescricaoTipoServico, AlinhamentoHorizontal.Centro)
                .ComCampo("Protocolo de autorização de uso", m.ProtocoloAutorizacao, AlinhamentoHorizontal.Centro)
                .ComLarguras(20, 28, 0));

            pilha.Add(Linha().ComCampo("CFOP - Natureza da operação", $"{m.Cfop} - {m.NaturezaOperacao}").ComLarguras(100));

            pilha.Add(Linha()
                .ComCampo("Início da prestação", m.InicioPrestacao)
                .ComCampo("Término da prestação", m.TerminoPrestacao)
                .ComLargurasIguais());

            pilha.Add(ParDeParticipantes("Remetente", m.Remetente, "Destinatário", m.Destinatario));

            if (m.Expedidor != null || m.Recebedor != null)
                pilha.Add(ParDeParticipantes("Expedidor", m.Expedidor, "Recebedor", m.Recebedor));

            pilha.Add(Tomador());
            AdicionarCarga(pilha);
            AdicionarComponentes(pilha);
            AdicionarImpostos(pilha);

            return pilha;
        }

        private FlexibleLine MontarCanhoto()
        {
            var m = ViewModel;
            var declaracao = new TextoSimples(_Estilo, "DECLARO QUE RECEBI OS VOLUMES DESTE CONHECIMENTO EM PERFEITO ESTADO PELO QUE DOU POR CUMPRIDO O PRESENTE CONTRATO DE TRANSPORTE")
            {
                Height = 5,
                TamanhoFonte = 5.5F,
                AlinhamentoVertical = AlinhamentoVertical.Centro
            };

            var campos = new LinhaCampos(_Estilo) { Height = 9 }
                .ComCampo("Nome", null)
                .ComCampo("RG", null)
                .ComCampo("Assinatura / carimbo", null)
                .ComCampo("Término prest. - data/hora", null)
                .ComCampo("Início prest. - data/hora", null)
                .ComLarguras(0, 10, 20, 22, 22);

            var coluna = new VerticalStack();
            coluna.Add(declaracao, campos);

            var numero = new CampoMultilinha("CT-e", $"Nº {m.Numero.ToString(Formatador.FormatoNumeroNF)}\nSÉRIE {m.Serie}", _Estilo, AlinhamentoHorizontal.Centro);

            return new FlexibleLine() { Height = coluna.Height }
                .ComElemento(coluna)
                .ComElemento(numero)
                .ComLarguras(0, 15);
        }

        #endregion

        #region Cabeçalho (todas as folhas)

        private VerticalStack MontarCabecalho(out Campo campoFolha)
        {
            var pilha = new VerticalStack();
            pilha.Add(MontarIdentificacao(out campoFolha));
            return pilha;
        }

        private FlexibleLine MontarIdentificacao(out Campo campoFolha)
        {
            var m = ViewModel;

            var emitente = new IdentificacaoEmitente(_Estilo, m.Emitente, false) { Logo = Logo };

            var titulo = new CaixaTexto(_Estilo, AlturaCabecalho - 3 * Constantes.CampoAltura,
                Tuple.Create("DACTE", new Fonte(FonteNegrito, 12)),
                Tuple.Create("Documento Auxiliar do Conhecimento de Transporte Eletrônico", new Fonte(FonteRegular, 6.5F)));

            var linhaNumero = Linha()
                .ComCampo("Modelo", m.Modelo.ToString(), AlinhamentoHorizontal.Centro)
                .ComCampo("Série", m.Serie.ToString(), AlinhamentoHorizontal.Centro)
                .ComCampo("Número", m.Numero.ToString(Formatador.FormatoNumeroNF), AlinhamentoHorizontal.Centro)
                .ComCampo("Folha", null, AlinhamentoHorizontal.Centro)
                .ComLarguras(17, 15, 0, 18);
            campoFolha = (Campo)linhaNumero.Elementos[3];

            var identificacao = new VerticalStack();
            identificacao.Add(titulo,
                Linha().ComCampo("Modal", m.DescricaoModal, AlinhamentoHorizontal.Centro).ComLarguras(100),
                linhaNumero,
                Linha().ComCampo("Data e hora de emissão", m.DataHoraEmissao.FormatarDataHora24(), AlinhamentoHorizontal.Centro).ComLarguras(100));

            var chave = new VerticalStack();
            chave.Add(new Barcode128C(m.ChaveAcesso, _Estilo, 70F) { Height = AlturaCabecalho - Constantes.CampoAltura - 11F },
                new Campo("Chave de acesso", Formatador.FormatarChaveAcesso(m.ChaveAcesso), _Estilo, AlinhamentoHorizontal.Centro),
                new TextoSimples(_Estilo, "Consulta de autenticidade no portal nacional do CT-e, no site da Sefaz autorizadora ou em http://www.cte.fazenda.gov.br")
                {
                    Height = 11F,
                    TamanhoFonte = 7,
                    AlinhamentoHorizontal = AlinhamentoHorizontal.Centro,
                    AlinhamentoVertical = AlinhamentoVertical.Centro
                });

            var linha = new FlexibleLine() { Height = AlturaCabecalho }
                .ComElemento(emitente)
                .ComElemento(identificacao)
                .ComElemento(chave);

            if (!String.IsNullOrWhiteSpace(m.QrCode))
            {
                linha.ComElemento(new CaixaQrCode(_Estilo, m.QrCode, AlturaCabecalho)).ComLarguras(0, 25, 37, 17);
            }
            else
            {
                linha.ComLarguras(0, 27, 43);
            }

            return linha;
        }

        #endregion

        #region Participantes, carga, valores e impostos

        private FlexibleLine ParDeParticipantes(String titulo1, EmpresaViewModel p1, String titulo2, EmpresaViewModel p2)
        {
            var a = Participante(titulo1, p1);
            var b = Participante(titulo2, p2);

            return new FlexibleLine() { Height = a.Height }
                .ComElemento(a)
                .ComElemento(b)
                .ComLargurasIguais();
        }

        private VerticalStack Participante(String titulo, EmpresaViewModel p)
        {
            var pilha = new VerticalStack();

            pilha.Add(
                new Campo(titulo, p?.RazaoSocial, _Estilo),
                new Campo("Endereço", Endereco(p), _Estilo),
                Linha()
                    .ComCampo("Município", p?.Municipio)
                    .ComCampo("UF", p?.EnderecoUf, AlinhamentoHorizontal.Centro)
                    .ComCampo("CEP", Formatador.FormatarCEP(p?.EnderecoCep), AlinhamentoHorizontal.Centro)
                    .ComLarguras(0, 12, 25),
                Linha()
                    .ComCampo("CNPJ/CPF", Formatador.FormatarCpfCnpj(p?.CnpjCpf), AlinhamentoHorizontal.Centro)
                    .ComCampo("Inscrição estadual", p?.Ie, AlinhamentoHorizontal.Centro)
                    .ComCampo("Fone", Formatador.FormatarTelefone(p?.Telefone), AlinhamentoHorizontal.Centro)
                    .ComLarguras(0, 30, 28));

            return pilha;
        }

        private VerticalStack Tomador()
        {
            var t = ViewModel.Tomador;
            var pilha = new VerticalStack();

            pilha.Add(
                Linha()
                    .ComCampo("Tomador do serviço", t?.RazaoSocial)
                    .ComCampo("Papel do tomador", ViewModel.TomadorPapel, AlinhamentoHorizontal.Centro)
                    .ComCampo("CNPJ/CPF", Formatador.FormatarCpfCnpj(t?.CnpjCpf), AlinhamentoHorizontal.Centro)
                    .ComCampo("Inscrição estadual", t?.Ie, AlinhamentoHorizontal.Centro)
                    .ComLarguras(0, 14, 18, 16),
                Linha()
                    .ComCampo("Endereço", Endereco(t))
                    .ComCampo("Município", t?.Municipio)
                    .ComCampo("UF", t?.EnderecoUf, AlinhamentoHorizontal.Centro)
                    .ComCampo("CEP", Formatador.FormatarCEP(t?.EnderecoCep), AlinhamentoHorizontal.Centro)
                    .ComLarguras(0, 22, 6, 12));

            return pilha;
        }

        private void AdicionarCarga(VerticalStack pilha)
        {
            var m = ViewModel;

            pilha.Add(Linha()
                .ComCampo("Produto predominante", m.ProdutoPredominante)
                .ComCampo("Outras características da carga", m.OutrasCaracteristicas)
                .ComCampoNumerico("Valor total da carga", m.ValorCarga)
                .ComLarguras(0, 30, 20));

            foreach (var grupo in Grupos(m.Quantidades, 5))
            {
                var linha = Linha();
                foreach (var q in grupo)
                    linha.ComCampoNumerico($"{q.TipoMedida} ({q.Unidade})", q.Quantidade, 4);
                pilha.Add(Completar(linha, grupo.Count, 5));
            }
        }

        private void AdicionarComponentes(VerticalStack pilha)
        {
            var m = ViewModel;
            pilha.Add(new CabecalhoBloco(_Estilo, "COMPONENTES DO VALOR DA PRESTAÇÃO DO SERVIÇO"));

            foreach (var grupo in Grupos(m.Componentes, 4))
            {
                var linha = Linha();
                foreach (var c in grupo)
                    linha.ComCampoNumerico(c.Nome, c.Valor);
                pilha.Add(Completar(linha, grupo.Count, 4));
            }

            pilha.Add(Linha()
                .ComCampoNumerico("Valor total do serviço", m.ValorTotalServico)
                .ComCampoNumerico("Valor a receber", m.ValorReceber)
                .ComCampoNumerico("Valor aproximado dos tributos", m.ValorAproximadoTributos)
                .ComLargurasIguais());
        }

        private void AdicionarImpostos(VerticalStack pilha)
        {
            var m = ViewModel;

            pilha.Add(new CabecalhoBloco(_Estilo, "INFORMAÇÕES RELATIVAS AO IMPOSTO"));
            pilha.Add(Linha()
                .ComCampo("Situação tributária", m.IcmsSituacaoTributaria)
                .ComCampoNumerico("Base de cálculo", m.IcmsBaseCalculo)
                .ComCampoNumerico("Alíq. ICMS", m.IcmsAliquota)
                .ComCampoNumerico("Valor do ICMS", m.IcmsValor)
                .ComCampoNumerico("% Red. BC", m.IcmsReducaoBaseCalculo)
                .ComCampoNumerico("ICMS ST", m.IcmsSt)
                .ComLarguras(0, 15, 10, 15, 10, 15));

            // Grupo do IBS e da CBS do CT-e (Reforma Tributária), exibido quando informado no XML.
            if (m.PossuiIbsCbs)
            {
                pilha.Add(new CabecalhoBloco(_Estilo, "INFORMAÇÕES DO IBS E DA CBS"));
                pilha.Add(Linha()
                    .ComCampo("CST / Class. trib.", $"{m.IbsCbsCst} / {m.IbsCbsClassificacaoTributaria}", AlinhamentoHorizontal.Centro)
                    .ComCampoNumerico("Base de cálculo", m.IbsCbsBaseCalculo)
                    .ComCampoNumerico("Alíq. IBS UF", m.IbsUfAliquota)
                    .ComCampoNumerico("Valor IBS UF", m.IbsUfValor)
                    .ComCampoNumerico("Alíq. IBS Mun.", m.IbsMunicipioAliquota)
                    .ComCampoNumerico("Valor IBS Mun.", m.IbsMunicipioValor)
                    .ComCampoNumerico("Alíq. CBS", m.CbsAliquota)
                    .ComCampoNumerico("Valor CBS", m.CbsValor)
                    .ComLarguras(16, 0, 10, 12, 10, 12, 10, 12));
            }
        }

        #endregion

        #region Rodapé da primeira folha

        private VerticalStack MontarRodape()
        {
            var m = ViewModel;
            var pilha = new VerticalStack();

            var observacoes = m.Observacoes;
            if (!String.IsNullOrWhiteSpace(m.ChaveCteComplementado))
                observacoes = $"CT-e complementado: {Formatador.FormatarChaveAcesso(m.ChaveCteComplementado)} {observacoes}".Trim();

            pilha.Add(new CampoMultilinha("Observações", observacoes, _Estilo) { Height = 12 });

            pilha.Add(new CabecalhoBloco(_Estilo, $"INFORMAÇÕES ESPECÍFICAS DO MODAL {m.DescricaoModal}"));

            if (m.Modal == "01")
            {
                pilha.Add(Linha()
                    .ComCampo("RNTRC da empresa", m.Rntrc, AlinhamentoHorizontal.Centro)
                    .ComCampo(null, "ESTE CONHECIMENTO DE TRANSPORTE ATENDE À LEGISLAÇÃO DE TRANSPORTE RODOVIÁRIO EM VIGOR", AlinhamentoHorizontal.Centro)
                    .ComLarguras(25, 0));
            }
            else
            {
                pilha.Add(new CampoMultilinha("Dados do modal", String.Join("\n", m.InformacoesModal.Select(c => $"{c.Key}: {c.Value}")), _Estilo));
            }

            var fisco = m.InformacoesFisco;
            if (m.TipoEmissao != 1)
                fisco = $"EMITIDO EM CONTINGÊNCIA (tpEmis {m.TipoEmissao}). {fisco}".Trim();

            var usoEmissor = new CampoMultilinha("Uso exclusivo do emissor do CT-e", null, _Estilo);
            var reservadoFisco = new CampoMultilinha("Reservado ao fisco", fisco, _Estilo);

            pilha.Add(new FlexibleLine() { Height = 12 }
                .ComElemento(usoEmissor)
                .ComElemento(reservadoFisco)
                .ComLargurasIguais());

            return pilha;
        }

        #endregion

        private void DesenharAvisoHomologacao(Gfx gfx, RectangleF area)
        {
            var ts = new TextStack(area) { AlinhamentoVertical = AlinhamentoVertical.Centro, AlinhamentoHorizontal = AlinhamentoHorizontal.Centro, LineHeightScale = 0.9F }
                .AddLine("SEM VALOR FISCAL", new Fonte(FonteRegular, 48))
                .AddLine("AMBIENTE DE HOMOLOGAÇÃO", new Fonte(FonteRegular, 30));

            gfx.BeginLocalState();
            gfx.SetFillColor(0.35, 0.35, 0.35);
            ts.Draw(gfx);
            gfx.EndLocalState();
        }

        private LinhaCampos Linha() => new LinhaCampos(_Estilo) { Height = Constantes.CampoAltura };

        /// <summary>
        /// Completa a linha com campos vazios, para que todas as linhas tenham colunas do mesmo tamanho.
        /// </summary>
        private static LinhaCampos Completar(LinhaCampos linha, int quantidade, int colunas)
        {
            for (int i = quantidade; i < colunas; i++)
                linha.ComCampo(null, null);

            return (LinhaCampos)linha.ComLargurasIguais();
        }

        private static IEnumerable<List<T>> Grupos<T>(List<T> itens, int tamanho)
        {
            for (int i = 0; i < itens.Count; i += tamanho)
                yield return itens.Skip(i).Take(tamanho).ToList();
        }

        private static String Endereco(EmpresaViewModel p)
        {
            if (p == null) return null;
            var linha = p.EnderecoLinha1;
            return String.IsNullOrWhiteSpace(p.EnderecoBairro) ? linha : $"{linha} - {p.EnderecoBairro}";
        }

        /// <summary>
        /// Define a largura dos campos multilinha antes de medir a altura da pilha.
        /// </summary>
        private static void Preparar(VerticalStack pilha, float largura)
        {
            pilha.Width = largura;
            foreach (var d in pilha.Drawables.Where(d => d is CampoMultilinha || d is CabecalhoBloco))
                d.Width = largura;
        }
    }
}
