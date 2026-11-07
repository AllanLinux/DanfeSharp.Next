using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DanfeSharp.Graphics;
using PdfSharp.Drawing;
using static DanfeSharp.NFCe.ColunasCupom;

namespace DanfeSharp.NFCe
{
    /// <summary>
    /// DANFE NFC-e (modelo 65) para bobina de impressora térmica de 80 mm ou 58 mm, em uma única página contínua,
    /// com as divisões do Manual de Especificações Técnicas do DANFE NFC-e e QR Code.
    /// </summary>
    public class Danfce : DocumentoAuxiliar
    {
        public DanfceViewModel ViewModel { get; private set; }

        private readonly float _LarguraPapel;
        private readonly float _Margem;
        private readonly float _LadoQrCode;

        private readonly Fonte _Fonte;
        private readonly Fonte _FontePequena;
        private readonly Fonte _FonteNegrito;
        private readonly Fonte _FonteTitulo;

        private bool _FoiGerado;

        public Danfce(DanfceViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            bool bobina80 = ViewModel.LarguraBobina == LarguraBobina.Mm80;
            _LarguraPapel = bobina80 ? 80F : 58F;
            _Margem = bobina80 ? 4F : 5F;
            _LadoQrCode = bobina80 ? 32F : 28F;

            float tamanho = bobina80 ? 7.5F : 6.5F;
            _Fonte = new Fonte(FonteRegular, tamanho);
            _FontePequena = new Fonte(FonteRegular, tamanho - 1F);
            _FonteNegrito = new Fonte(FonteNegrito, tamanho);
            _FonteTitulo = new Fonte(FonteNegrito, tamanho + 1F);

            DefinirMetadados("DANFE NFC-e", "DANFE NFC-e (Documento auxiliar da NFC-e)", ViewModel.ChaveAcesso);
        }

        public override void Gerar()
        {
            if (_FoiGerado) throw new InvalidOperationException("O DANFE NFC-e já foi gerado.");

            var itens = MontarItens();
            float largura = _LarguraPapel - 2 * _Margem;
            float altura = itens.Sum(i => i.Altura(largura)) + 2 * _Margem;

            var pagina = AdicionarPagina(_LarguraPapel, altura);

            using (var gfx = new Gfx(XGraphics.FromPdfPage(pagina)))
            {
                float y = _Margem;

                foreach (var item in itens)
                {
                    item.Desenhar(gfx, _Margem, y, largura);
                    y += item.Altura(largura);
                }
            }

            _FoiGerado = true;
        }

        private List<ItemCupom> MontarItens()
        {
            var m = ViewModel;
            var c = m.CalculoImposto;
            var itens = new List<ItemCupom>();
            var centro = AlinhamentoHorizontal.Centro;
            var direita = AlinhamentoHorizontal.Direita;

            // Divisão I: emitente
            if (Logo != null)
            {
                itens.Add(new LogoCupom(Logo, 15F));
                itens.Add(new EspacoCupom(1F));
            }

            itens.Add(new TextoCupom(m.Emitente.RazaoSocial, _FonteNegrito, centro));
            itens.Add(new TextoCupom($"CNPJ: {Formatador.FormatarCpfCnpj(m.Emitente.CnpjCpf)}  IE: {m.Emitente.Ie}", _FontePequena, centro));
            itens.Add(new TextoCupom($"{m.Emitente.EnderecoLinha1} - {m.Emitente.EnderecoBairro} - {m.Emitente.Municipio}/{m.Emitente.EnderecoUf}", _FontePequena, centro));
            itens.Add(new SeparadorCupom());
            itens.Add(new TextoCupom("DANFE NFC-e - Documento Auxiliar da Nota Fiscal de Consumidor Eletrônica", _FonteNegrito, centro));

            if (m.IsHomologacao)
                itens.Add(new FundoCinzaCupom(new TextoCupom("EMITIDA EM AMBIENTE DE HOMOLOGAÇÃO - SEM VALOR FISCAL", _FonteNegrito, centro)));

            if (m.IsContingencia)
                itens.Add(new TextoCupom("EMITIDA EM CONTINGÊNCIA - Pendente de autorização", _FonteNegrito, centro));

            itens.Add(new SeparadorCupom());

            // Divisão II: detalhe da venda
            itens.Add(new ColunasCupom(_FonteNegrito, Coluna("CÓDIGO", 25), Coluna("DESCRIÇÃO", 75)));
            itens.Add(new ColunasCupom(_FonteNegrito, Coluna("QTDE", 25, direita), Coluna("UN", 15, centro), Coluna("VL UNIT", 30, direita), Coluna("VL TOTAL", 30, direita)));

            foreach (var p in m.Produtos)
            {
                itens.Add(new ColunasCupom(_Fonte, Coluna(p.Codigo, 25), Coluna(p.Descricao, 75)));
                itens.Add(new ColunasCupom(_Fonte, Coluna(p.Quantidade.Formatar(Formatador.FormatoNumero), 25, direita), Coluna(p.Unidade, 15, centro),
                    Coluna(p.ValorUnitario.Formatar(), 30, direita), Coluna(p.ValorTotal.Formatar(), 30, direita)));
            }

            itens.Add(new SeparadorCupom());

            // Divisão III: totais e pagamentos
            itens.Add(Valor("Qtde. total de itens", m.Produtos.Count.ToString(), _Fonte));
            itens.Add(Valor("Valor total R$", c.ValorTotalProdutos.Formatar(), _Fonte));
            if (c.Desconto > 0) itens.Add(Valor("Desconto R$", c.Desconto.Formatar(), _Fonte));
            if (m.Acrescimos > 0) itens.Add(Valor("Acréscimos R$", m.Acrescimos.Formatar(), _Fonte));
            itens.Add(Valor("Valor a Pagar R$", c.ValorTotalNota.Formatar(), _FonteNegrito));

            itens.Add(new EspacoCupom(1F));
            itens.Add(new ColunasCupom(_FonteNegrito, Coluna("FORMA DE PAGAMENTO", 65), Coluna("VALOR PAGO R$", 35, direita)));

            foreach (var pag in m.Pagamentos)
                itens.Add(Valor(pag.Descricao, pag.Valor.Formatar(), _Fonte));

            if (m.Troco > 0) itens.Add(Valor("Troco R$", m.Troco.Formatar(), _Fonte));

            itens.Add(new SeparadorCupom());

            // Divisão IV: consulta pela chave de acesso
            itens.Add(new TextoCupom("Consulte pela Chave de Acesso em", _FonteNegrito, centro));
            itens.Add(new TextoCupom(m.UrlConsulta, _FontePequena, centro));
            itens.Add(new TextoCupom(Formatador.FormatarChaveAcesso(m.ChaveAcesso), _Fonte, centro));
            itens.Add(new SeparadorCupom());

            // Divisão V: consumidor
            itens.Add(new TextoCupom(TextoConsumidor(), _Fonte, centro));
            itens.Add(new SeparadorCupom());

            // Divisão VI: identificação e protocolo
            itens.Add(new TextoCupom($"NFC-e nº {m.NfNumero.ToString(Formatador.FormatoNumeroNF)} Série {m.NfSerie:000} {m.DataHoraEmissao.FormatarDataHora24()}", _FonteNegrito, centro));

            if (!String.IsNullOrWhiteSpace(m.ProtocoloAutorizacao))
            {
                itens.Add(new TextoCupom($"Protocolo de Autorização: {m.ProtocoloAutorizacao}", _Fonte, centro));
                itens.Add(new TextoCupom($"Data de Autorização: {m.DataHoraAutorizacao.FormatarDataHora24()}", _Fonte, centro));
            }
            else if (m.IsContingencia)
            {
                itens.Add(new TextoCupom($"Contingência: {m.ContingenciaDataHora.FormatarDataHora24()} {m.ContingenciaJustificativa}".Trim(), _FontePequena, centro));
            }

            // Divisão VII: QR Code
            if (!String.IsNullOrWhiteSpace(m.QrCode))
            {
                itens.Add(new EspacoCupom(1F));
                itens.Add(new TextoCupom("Consulta via leitor de QR Code", _FonteNegrito, centro));
                itens.Add(new QrCodeCupom(m.QrCode, _LadoQrCode));
            }

            // Divisão VIII: informações de interesse do contribuinte e do fisco
            var informacoes = TextoInformacoes();

            if (informacoes.Length > 0)
            {
                itens.Add(new SeparadorCupom());
                itens.Add(new TextoCupom(informacoes, _FontePequena, centro));
            }

            return itens;
        }

        private static ColunasCupom Valor(String rotulo, String valor, Fonte fonte)
            => new ColunasCupom(fonte, Coluna(rotulo, 65), Coluna(valor, 35, AlinhamentoHorizontal.Direita));

        private String TextoConsumidor()
        {
            var d = ViewModel.Destinatario;
            if (d == null) return "CONSUMIDOR NÃO IDENTIFICADO";

            var sb = new StringBuilder("CONSUMIDOR");
            if (!String.IsNullOrWhiteSpace(d.CnpjCpf)) sb.Append(d.CnpjCpf.Length > 11 ? " - CNPJ " : " - CPF ").Append(Formatador.FormatarCpfCnpj(d.CnpjCpf));
            if (!String.IsNullOrWhiteSpace(d.RazaoSocial)) sb.Append(" - ").Append(d.RazaoSocial);
            if (!String.IsNullOrWhiteSpace(d.EnderecoLogadrouro)) sb.Append("\n").Append(d.EnderecoLinha1).Append(" - ").Append(d.EnderecoBairro).Append(" - ").Append(d.Municipio).Append('/').Append(d.EnderecoUf);
            return sb.ToString();
        }

        private String TextoInformacoes()
        {
            var m = ViewModel;
            var c = m.CalculoImposto;
            var linhas = new List<String>();

            if (c.ValorAproximadoTributos.HasValue)
                linhas.Add($"Tributos Totais Incidentes (Lei Federal 12.741/2012): R$ {c.ValorAproximadoTributos.Formatar()}");

            if (c.PossuiIbsCbsIs)
            {
                var rt = new List<String>();
                if (c.ValorCbs.HasValue) rt.Add($"CBS R$ {c.ValorCbs.Formatar()}");
                if (c.ValorIbsUf.HasValue) rt.Add($"IBS UF R$ {c.ValorIbsUf.Formatar()}");
                if (c.ValorIbsMunicipio.HasValue) rt.Add($"IBS Mun. R$ {c.ValorIbsMunicipio.Formatar()}");
                if (c.ValorImpostoSeletivo.HasValue) rt.Add($"IS R$ {c.ValorImpostoSeletivo.Formatar()}");
                linhas.Add(String.Join(" | ", rt));
            }

            if (!String.IsNullOrWhiteSpace(m.InformacoesComplementares)) linhas.Add(m.InformacoesComplementares.Replace(";", "\n"));
            if (!String.IsNullOrWhiteSpace(m.InformacoesAdicionaisFisco)) linhas.Add("Inf. fisco: " + m.InformacoesAdicionaisFisco);

            return String.Join("\n", linhas);
        }
    }
}
