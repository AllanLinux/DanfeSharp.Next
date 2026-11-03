using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DanfeSharp.Graphics;
using PdfSharp.Drawing;

namespace DanfeSharp.NFSe
{
    /// <summary>
    /// DANFSe (Documento Auxiliar da NFS-e) do padrão nacional, leiaute v2.0, em A4 retrato.
    /// As medidas seguem a DANFSe gerada pelo Sistema Nacional NFS-e: uma grade de 4 colunas de 51 mm,
    /// com o título de cada quadro na primeira coluna e linhas horizontais separando os quadros.
    /// </summary>
    public class Danfse : DocumentoAuxiliar
    {
        private const float Esquerda = 3F;
        private const float Direita = 207F;
        private const float LarguraColuna = 51F;
        private const float Recuo = 1.2F;

        // Distâncias verticais, em mm, entre a linha do quadro, o rótulo e o valor de cada linha de campos.
        private const float RotuloAposLinha = 2.1F;
        private const float ValorAposRotulo = 2.7F;
        private const float EntreLinhas = 6.7F;
        private const float LinhaTexto = 2.8F;

        private const float TopoCanhoto = 280.7F;
        private const float BaseCanhoto = 287.8F;

        public DanfseViewModel ViewModel { get; private set; }

        private readonly Fonte _Titulo;
        private readonly Fonte _TituloQuadro;
        private readonly Fonte _RotuloIdentificacao;
        private readonly Fonte _Rotulo;
        private readonly Fonte _Valor;
        private readonly Fonte _Pequena;
        private readonly Fonte _Municipio;

        private Gfx _Gfx;
        private bool _FoiGerado;

        public Danfse(DanfseViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            _Titulo = new Fonte(FonteNegrito, 9);
            _TituloQuadro = new Fonte(FonteNegrito, 7);
            _RotuloIdentificacao = new Fonte(FonteNegrito, 7);
            _Rotulo = new Fonte(FonteNegrito, 6);
            _Valor = new Fonte(FonteRegular, 7);
            _Pequena = new Fonte(FonteRegular, 6);
            _Municipio = new Fonte(FonteRegular, 8);

            // Logotipo da NFS-e, como na DANFSe oficial. AdicionarLogoImagem ou AdicionarLogoPdf o substituem.
            using (var logo = typeof(Danfse).Assembly.GetManifestResourceStream("DanfeSharp.NFSe.logo_nfse.png"))
            {
                if (logo != null) AdicionarLogoImagem(logo);
            }

            DefinirMetadados("DANFSe", "DANFSe (Documento auxiliar da NFS-e)", ViewModel.ChaveAcesso);
        }

        public override void Gerar()
        {
            if (_FoiGerado) throw new InvalidOperationException("A DANFSe já foi gerada.");

            using (_Gfx = new Gfx(XGraphics.FromPdfPage(AdicionarPagina(Constantes.A4Largura, Constantes.A4Altura))))
            {
                var m = ViewModel;

                // Borda da página.
                _Gfx.SetLineWidth(1);
                _Gfx.DrawRectangle(1.8F, 1.8F, 206.4F, 293.5F);
                _Gfx.Stroke();

                DesenharCabecalho();
                float y = DesenharIdentificacao();

                y = QuadroPrestador(y);
                y = QuadroParticipante(y, "TOMADOR / ADQUIRENTE", m.Tomador);
                y = QuadroParticipante(y, "DESTINATÁRIO DA OPERAÇÃO", m.Destinatario);
                y = QuadroParticipante(y, "INTERMEDIÁRIO DA OPERAÇÃO", m.Intermediario);
                y = QuadroServico(y);
                y = QuadroTributacaoMunicipal(y);
                y = QuadroTributacaoFederal(y);
                y = QuadroIbsCbs(y);
                y = QuadroValorTotal(y);
                QuadroInformacoesComplementares(y);
                DesenharCanhoto();

                if (m.IsHomologacao) DesenharAvisoHomologacao();

                if (ExibirAssinatura)
                    _Gfx.DrawString(TextoAssinatura, new RectangleF(Esquerda, 289.5F, Direita - Esquerda, 3F), new Fonte(FonteItalico, 6), AlinhamentoHorizontal.Direita);
            }

            _Gfx = null;
            _FoiGerado = true;
        }

        #region Cabeçalho e identificação

        private void DesenharCabecalho()
        {
            var m = ViewModel;

            if (Logo != null)
                _Gfx.ShowXObject(Logo, new RectangleF(4.2F, 3.6F, 40.8F, 8.1F));

            Centralizado("DANFSe v2.0", 7.3F, _Titulo);
            Centralizado("Documento Auxiliar da NFS-e", 11F, _Titulo);

            Texto($"Município: {m.MunicipioEmissor ?? "-"}", ColunaX(3), 6.6F, _Municipio, LarguraColuna - Recuo);
            Texto($"Ambiente Gerador: {m.AmbienteGerador}", ColunaX(3), 9.2F, _Pequena);
            Texto($"Tipo de Ambiente: {m.TipoAmbiente}", ColunaX(3), 11.5F, _Pequena);

            LinhaHorizontal(14.1F);
        }

        private float DesenharIdentificacao()
        {
            var m = ViewModel;

            Campo(0, 3, 18F, "CHAVE DE ACESSO DA NFS-e", m.ChaveAcesso, _RotuloIdentificacao, 2.8F);

            Campo(0, 1, 25.1F, "NÚMERO DA NFS-e", m.Numero, _RotuloIdentificacao, 2.8F);
            Campo(1, 1, 25.1F, "COMPETÊNCIA DA NFS-e", m.Competencia.Formatar(), _RotuloIdentificacao, 2.8F);
            Campo(2, 1, 25.1F, "DATA E HORA DA EMISSÃO DA NFS-e", m.DataHoraEmissao.FormatarDataHora24(), _RotuloIdentificacao, 2.8F);

            Campo(0, 1, 32.3F, "NÚMERO DA DPS", m.NumeroDps, _RotuloIdentificacao, 2.8F);
            Campo(1, 1, 32.3F, "SÉRIE DA DPS", m.SerieDps, _RotuloIdentificacao, 2.8F);
            Campo(2, 1, 32.3F, "DATA E HORA DA EMISSÃO DA DPS", m.DataHoraEmissaoDps.FormatarDataHora24(), _RotuloIdentificacao, 2.8F);

            Campo(0, 1, 39.4F, "EMITENTE DA NFS-e", m.Emitente, _RotuloIdentificacao, 2.8F);
            Campo(1, 1, 39.4F, "SITUAÇÃO DA NFS-e", m.Situacao, _RotuloIdentificacao, 2.8F);
            Campo(2, 1, 39.4F, "FINALIDADE", m.Finalidade, _RotuloIdentificacao, 2.8F);

            if (!String.IsNullOrWhiteSpace(m.QrCode))
            {
                // A área escura do QR Code mede 15,9 mm, como na DANFSe oficial; o lado inclui a zona de silêncio.
                var qr = new QrCode(m.QrCode, 19.3F);
                qr.SetPosition(181.55F - qr.Lado / 2, 23.75F - qr.Lado / 2);
                qr.Draw(_Gfx);
            }

            // Mesmas quebras de linha da DANFSe oficial.
            Texto("A autenticidade desta NFS-e pode ser verificada", ColunaX(3), 34.4F, _Pequena, LarguraColuna - 2 * Recuo);
            Texto("pela leitura deste código QR ou pela consulta da", ColunaX(3), 36.8F, _Pequena, LarguraColuna - 2 * Recuo);
            Texto("chave de acesso no portal nacional da NFS-e", ColunaX(3), 39.2F, _Pequena, LarguraColuna - 2 * Recuo);

            LinhaHorizontal(44.3F);
            return 44.3F;
        }

        #endregion

        #region Quadros

        private float QuadroPrestador(float y)
        {
            var p = ViewModel.Prestador ?? new ParticipanteNfseViewModel();
            TituloQuadro("PRESTADOR / FORNECEDOR", y);

            float r = DadosParticipante(p, y + RotuloAposLinha);

            float extra = Maior(
                Campo(0, 1, r, "Simples Nacional na Data de Competência", p.SimplesNacional),
                Campo(1, 3, r, "Regime de Apuração Tributária pelo SN", p.RegimeApuracaoSimplesNacional));

            return FecharQuadro(r, extra);
        }

        private float QuadroParticipante(float y, String titulo, ParticipanteNfseViewModel p)
        {
            if (p == null)
            {
                Centralizado($"{titulo} NÃO IDENTIFICADO NA NFS-e", y + 2.3F, _Rotulo);
                float fim = y + 2.95F;
                LinhaHorizontal(fim);
                return fim;
            }

            TituloQuadro(titulo, y);
            float r = DadosParticipante(p, y + RotuloAposLinha);

            // r é o rótulo de uma próxima linha; o quadro fecha abaixo dos valores da última linha escrita.
            return FecharQuadroNoValor(r - EntreLinhas + ValorAposRotulo);
        }

        /// <summary>
        /// Escreve as três linhas de dados do participante e retorna a posição do rótulo da linha seguinte.
        /// </summary>
        private float DadosParticipante(ParticipanteNfseViewModel p, float r)
        {
            float extra = Maior(
                Campo(1, 1, r, "CNPJ / CPF / NIF", p.Documento),
                Campo(2, 1, r, "Indicador Municipal (Inscrição)", p.InscricaoMunicipal),
                Campo(3, 1, r, "Telefone", p.Telefone));

            r += EntreLinhas + extra;
            extra = Maior(
                Campo(0, 2, r, "Nome / Nome Empresarial", p.Nome),
                Campo(2, 1, r, "Município / Sigla UF", p.Municipio),
                Campo(3, 1, r, "Código IBGE / CEP", p.CodigoIbgeCep));

            r += EntreLinhas + extra;
            extra = Maior(
                Campo(0, 2, r, "Endereço", p.Endereco),
                Campo(2, 2, r, "E-mail", p.Email));

            return r + EntreLinhas + extra;
        }

        private float QuadroServico(float y)
        {
            var m = ViewModel;
            TituloQuadro("SERVIÇO PRESTADO", y);

            float r = y + RotuloAposLinha;
            float extra = Maior(
                Campo(1, 1, r, "Código de Tributação Nacional/Municipal", m.CodigoTributacao),
                Campo(2, 1, r, "Código da NBS", m.CodigoNbs),
                Campo(3, 1, r, "Local da Prestação / Sigla UF / País", m.LocalPrestacao));

            float ultima = r + ValorAposRotulo + extra;

            if (!String.IsNullOrWhiteSpace(m.DescricaoTributacaoNacional))
                // A DANFSe oficial mostra só a primeira linha da descrição do código de tributação nacional.
                ultima = Paragrafo(m.DescricaoTributacaoNacional, ColunaX(0), ultima + 4.3F, Direita - Esquerda - 2 * Recuo, _Valor, LinhaTexto, 1);

            float rotulo = ultima + 4F;
            Texto("Descrição do Serviço", ColunaX(0), rotulo, _Rotulo);
            ultima = Paragrafo(Valor(m.DescricaoServico), ColunaX(0), rotulo + ValorAposRotulo, Direita - Esquerda - 2 * Recuo, _Valor, LinhaTexto, 12);

            return FecharQuadroNoValor(ultima);
        }

        private float QuadroTributacaoMunicipal(float y)
        {
            var m = ViewModel;
            TituloQuadro("TRIBUTAÇÃO MUNICIPAL (ISSQN)", y);

            float r = y + RotuloAposLinha;
            float extra = Maior(
                Campo(1, 1, r, "Tipo de Tributação do ISSQN", m.TipoTributacaoIssqn),
                Campo(2, 2, r, "Município / Sigla UF / País de Incidência do ISSQN", m.MunicipioIncidenciaIssqn));

            // Linha exibida pela DANFSe oficial só quando a nota tem benefício municipal ou deduções.
            if (m.ExibirBeneficioMunicipal)
            {
                r += EntreLinhas + extra;
                extra = Maior(
                    Campo(0, 1, r, "Benefício Municipal", m.BeneficioMunicipal),
                    Campo(1, 1, r, "Cálculo do BM", Moeda(m.CalculoBeneficioMunicipal)),
                    Campo(2, 1, r, "Total Deduções/Reduções", Moeda(m.TotalDeducoesReducoes)),
                    Campo(3, 1, r, "Desconto Incondicionado", Moeda(m.DescontoIncondicionado)));
            }

            r += EntreLinhas + extra;
            extra = Maior(
                Campo(0, 1, r, "BC ISSQN", Moeda(m.BaseCalculoIssqn)),
                Campo(1, 1, r, "Alíquota Aplicada", Percentual(m.AliquotaIssqn)),
                Campo(2, 1, r, "Retenção do ISSQN", m.RetencaoIssqn),
                Campo(3, 1, r, "ISSQN Apurado", Moeda(m.ValorIssqn)));

            return FecharQuadro(r, extra);
        }

        private float QuadroTributacaoFederal(float y)
        {
            var m = ViewModel;
            TituloQuadro("TRIBUTAÇÃO FEDERAL (EXCETO CBS)", y);

            float r = y + RotuloAposLinha;
            float extra = Maior(
                Campo(1, 1, r, "IRRF", Moeda(m.Irrf)),
                Campo(2, 1, r, "Contribuição Previdenciária - Retida", Moeda(m.ContribuicaoPrevidenciariaRetida)),
                Campo(3, 1, r, "Contribuições Sociais - Retidas", Moeda(m.ContribuicoesSociaisRetidas)));

            r += EntreLinhas + extra;
            extra = Maior(
                Campo(0, 1, r, "PIS - Débito Apuração Própria", Moeda(m.Pis)),
                Campo(1, 1, r, "COFINS - Débito Apuração Própria", Moeda(m.Cofins)),
                Campo(2, 2, r, "Descrição Contrib. Sociais - Retidas", m.DescricaoContribuicoesSociaisRetidas));

            return FecharQuadro(r, extra);
        }

        private float QuadroIbsCbs(float y)
        {
            var m = ViewModel;
            TituloQuadro("TRIBUTAÇÃO IBS/CBS", y);

            float r = y + RotuloAposLinha;
            float extra = Maior(
                Campo(1, 1, r, "CST / cClassTrib", m.CstClassificacaoTributaria),
                Campo(2, 2, r, "Indicador de Operação / Código IBGE Incidência / Município Incidência / Sigla UF", m.IndicadorOperacaoIncidencia));

            r += EntreLinhas + extra;
            extra = Maior(
                Campo(0, 1, r, "Exclusões e Reduções da Base de Cálculo", Moeda(m.ExclusoesReducoesBaseCalculo)),
                Campo(1, 1, r, "Base de Cálculo Após Exclusões e Reduções", Moeda(m.BaseCalculoIbsCbs)),
                Campo(2, 1, r, "Red. Alíquota IBS / Red. Alíquota CBS",
                    $"{Percentual(m.ReducaoAliquotaIbsUf)} / {Percentual(m.ReducaoAliquotaIbsMunicipio)} / {Percentual(m.ReducaoAliquotaCbs)}"),
                Campo(3, 1, r, "Alíquota - IBS UF / IBS Mun", $"{Percentual(m.AliquotaIbsUf)} / {Percentual(m.AliquotaIbsMunicipio)}"));

            r += EntreLinhas + extra;
            extra = Maior(
                Campo(0, 1, r, "Alíq. Efetiva Municipal - IBS", Percentual(m.AliquotaEfetivaIbsMunicipio)),
                Campo(1, 1, r, "Valor Apurado Municipal - IBS", Moeda(m.ValorIbsMunicipio)),
                Campo(2, 1, r, "Alíq. Efetiva Estadual - IBS", Percentual(m.AliquotaEfetivaIbsUf)),
                Campo(3, 1, r, "Valor Apurado Estadual - IBS", Moeda(m.ValorIbsUf)));

            r += EntreLinhas + extra;
            extra = Maior(
                Campo(0, 1, r, "Valor Total Apurado - IBS", Moeda(m.ValorIbs)),
                Campo(1, 1, r, "Alíquota - CBS", Percentual(m.AliquotaCbs)),
                Campo(2, 1, r, "Alíquota Efetiva - CBS", Percentual(m.AliquotaEfetivaCbs)),
                Campo(3, 1, r, "Valor Total Apurado - CBS", Moeda(m.ValorCbs)));

            return FecharQuadro(r, extra);
        }

        private float QuadroValorTotal(float y)
        {
            var m = ViewModel;
            TituloQuadro("VALOR TOTAL DA NFS-e", y);

            float r = y + RotuloAposLinha;
            float extra = Maior(
                Campo(1, 1, r, "VALOR DA OPERAÇÃO / SERVIÇO", Moeda(m.ValorServico)),
                Campo(2, 1, r, "Desconto Incondicionado", Moeda(m.DescontoIncondicionado)),
                Campo(3, 1, r, "Desconto Condicionado", Moeda(m.DescontoCondicionado)));

            r += EntreLinhas + extra;
            extra = Maior(
                Campo(0, 1, r, "Total das Retenções (ISSQN / Federais)", Moeda(m.TotalRetencoes)),
                Campo(1, 1, r, "VALOR LÍQUIDO DA NFS-e", Moeda(m.ValorLiquido)),
                Campo(2, 1, r, "Total do IBS/CBS", Moeda(m.TotalIbsCbs)),
                Campo(3, 1, r, "VALOR LÍQUIDO DA NFS-e + IBS/CBS", Moeda(m.ValorLiquidoComIbsCbs)));

            return FecharQuadro(r, extra);
        }

        private void QuadroInformacoesComplementares(float y)
        {
            float titulo = y + 2.4F;
            Texto("INFORMAÇÕES COMPLEMENTARES", ColunaX(0), titulo, _TituloQuadro);

            // O texto ocupa o espaço até o canhoto.
            float primeira = titulo + 7.1F;
            int maximo = Math.Max(1, (int)((TopoCanhoto - 2F - primeira) / LinhaTexto) + 1);
            Paragrafo(ViewModel.InformacoesComplementares, ColunaX(0), primeira, Direita - Esquerda - 2 * Recuo, _Valor, LinhaTexto, maximo);
        }

        private void DesenharCanhoto()
        {
            var m = ViewModel;

            _Gfx.SetLineWidth(1);
            _Gfx.DrawRectangle(new RectangleF(Esquerda, TopoCanhoto, LarguraColuna, BaseCanhoto - TopoCanhoto));
            _Gfx.DrawRectangle(new RectangleF(Esquerda + LarguraColuna, TopoCanhoto, LarguraColuna, BaseCanhoto - TopoCanhoto));
            _Gfx.DrawRectangle(new RectangleF(Esquerda + 2 * LarguraColuna, TopoCanhoto, 2 * LarguraColuna, BaseCanhoto - TopoCanhoto));
            _Gfx.Stroke();

            Texto("DATA CIENTIFICAÇÃO:", Esquerda + 1.6F, 282.9F, _Rotulo);
            Texto("IDENTIFICAÇÃO E ASSINATURA", Esquerda + LarguraColuna + 1.6F, 282.9F, _Rotulo);
            Texto("N° NFS-e / CHAVE NFS-e", Esquerda + 2 * LarguraColuna + 1.6F, 282.9F, _Rotulo);
            Texto($"{Valor(m.Numero)} / {Valor(m.ChaveAcesso)}", Esquerda + 2 * LarguraColuna + 1.6F, 285.6F, _Valor, 2 * LarguraColuna - 3.2F);
        }

        private void DesenharAvisoHomologacao()
        {
            var area = new RectangleF(Esquerda, 100F, Direita - Esquerda, 100F);
            var ts = new TextStack(area) { AlinhamentoVertical = AlinhamentoVertical.Centro, AlinhamentoHorizontal = AlinhamentoHorizontal.Centro, LineHeightScale = 0.9F }
                .AddLine("SEM VALOR FISCAL", new Fonte(FonteRegular, 48))
                .AddLine("AMBIENTE DE HOMOLOGAÇÃO", new Fonte(FonteRegular, 30));

            _Gfx.BeginLocalState();
            _Gfx.SetFillColor(0.35, 0.35, 0.35);
            ts.Draw(_Gfx);
            _Gfx.EndLocalState();
        }

        #endregion

        #region Desenho

        private static float ColunaX(int coluna) => Esquerda + coluna * LarguraColuna + Recuo;

        private void TituloQuadro(String titulo, float y) => Texto(titulo, ColunaX(0), y + 2.4F, _TituloQuadro, LarguraColuna - 2 * Recuo);

        /// <summary>
        /// Fecha o quadro abaixo da última linha de campos, cujo rótulo está em <paramref name="ultimoRotulo"/>;
        /// <paramref name="extra"/> é a altura das linhas a mais dos valores quebrados.
        /// </summary>
        private float FecharQuadro(float ultimoRotulo, float extra) => FecharQuadroNoValor(ultimoRotulo + ValorAposRotulo + extra);

        private static float Maior(params float[] valores) => valores.Max();

        private float FecharQuadroNoValor(float ultimoValor)
        {
            float y = ultimoValor + RotuloAposLinha;
            LinhaHorizontal(y);
            return y;
        }

        private void LinhaHorizontal(float y)
        {
            _Gfx.SetLineWidth(0.5F);
            _Gfx.DrawLine(new PointF(Esquerda, y), new PointF(Direita, y));
            _Gfx.Stroke();
        }

        /// <summary>
        /// Rótulo e valor de um campo que ocupa <paramref name="colunas"/> colunas a partir de <paramref name="coluna"/>.
        /// Como na DANFSe oficial, o valor que não cabe na largura continua nas linhas de baixo (até 3 linhas).
        /// Retorna a altura acrescentada pelas linhas a mais, para empurrar o restante do documento.
        /// </summary>
        private float Campo(int coluna, int colunas, float baseRotulo, String rotulo, String valor, Fonte fonteRotulo = null, float distanciaValor = ValorAposRotulo)
        {
            float largura = colunas * LarguraColuna - 2 * Recuo;
            Texto(rotulo, ColunaX(coluna), baseRotulo, fonteRotulo ?? _Rotulo, largura);

            float primeira = baseRotulo + distanciaValor;
            return Paragrafo(Valor(valor), ColunaX(coluna), primeira, largura, _Valor, LinhaTexto, 3) - primeira;
        }

        private void Centralizado(String texto, float baseLinha, Fonte fonte)
        {
            float x = (Constantes.A4Largura - fonte.MedirLarguraTexto(texto)) / 2F;
            Texto(texto, x, baseLinha, fonte);
        }

        /// <summary>
        /// Escreve o texto com a linha de base em <paramref name="baseLinha"/>. Quando a largura é informada,
        /// reduz a fonte até 5 pt e, se ainda não couber, corta o texto.
        /// </summary>
        private void Texto(String texto, float x, float baseLinha, Fonte fonte, float largura = 0)
        {
            if (String.IsNullOrEmpty(texto)) return;

            if (largura > 0 && fonte.MedirLarguraTexto(texto) > largura)
            {
                fonte = fonte.Clonar();
                while (fonte.Tamanho > 5F && fonte.MedirLarguraTexto(texto) > largura)
                    fonte.Tamanho -= 0.25F;

                if (fonte.MedirLarguraTexto(texto) > largura)
                {
                    while (texto.Length > 1 && fonte.MedirLarguraTexto(texto + "...") > largura)
                        texto = texto.Substring(0, texto.Length - 1);
                    texto = texto.TrimEnd() + "...";
                }
            }

            _Gfx.ShowText(texto, new PointF(x, baseLinha - fonte.Ascendente), fonte);
        }

        /// <summary>
        /// Escreve um texto com quebra de linhas e retorna a linha de base da última linha escrita.
        /// </summary>
        private float Paragrafo(String texto, float x, float primeiraBase, float largura, Fonte fonte, float entreLinhas, int maximoLinhas)
        {
            if (String.IsNullOrWhiteSpace(texto)) return primeiraBase - entreLinhas;

            var linhas = new List<String>();
            foreach (var paragrafo in texto.Replace("\r", "").Split('\n'))
            {
                var bloco = new TextBlock(paragrafo, fonte) { Width = largura };
                if (bloco.Height > 0) linhas.AddRange(bloco.Lines);
            }

            if (linhas.Count > maximoLinhas)
            {
                linhas = linhas.Take(maximoLinhas).ToList();
                linhas[maximoLinhas - 1] = linhas[maximoLinhas - 1].TrimEnd() + "...";
            }

            float y = primeiraBase;
            for (int i = 0; i < linhas.Count; i++)
            {
                y = primeiraBase + i * entreLinhas;
                Texto(linhas[i], x, y, fonte, largura);
            }

            return y;
        }

        private static String Valor(String valor) => String.IsNullOrWhiteSpace(valor) ? "-" : valor.Trim();

        private static String Moeda(double? valor) => DanfseViewModelCreator.Moeda(valor);

        private static String Percentual(double? valor) => DanfseViewModelCreator.Percentual(valor);

        #endregion
    }
}
