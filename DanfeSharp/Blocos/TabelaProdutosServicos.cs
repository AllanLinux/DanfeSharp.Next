using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using DanfeSharp.Graphics;
using DanfeSharp.Modelo;

namespace DanfeSharp.Blocos
{
    internal class TabelaProdutosServicos : ElementoBase
    {
        public CabecalhoBloco CabecalhoBloco { get; private set; }
        public Tabela Tabela { get; private set; }
        public DanfeViewModel ViewModel { get; private set; }

        public TabelaProdutosServicos(DanfeViewModel viewModel, Estilo estilo) : base(estilo)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));
            CabecalhoBloco = new CabecalhoBloco(estilo, "DADOS DOS PRODUTOS / SERVIÇOS");

            var ad = AlinhamentoHorizontal.Direita;
            var ac = AlinhamentoHorizontal.Centro;
            var ae = AlinhamentoHorizontal.Esquerda;

            Tabela = new Tabela(Estilo);
            String cabecalho4 = ViewModel.Emitente.CRT == "3" ? "CST" : "CSOSN";

            if (ViewModel.UsarLeiauteReformaTributaria)
            {
                ConfigurarLeiauteReformaTributaria();
                return;
            }

            if (ViewModel.IsRetrato)
            {
                Tabela
                .ComColuna(6.5F, ac, "CÓDIGO")
                .ComColuna(0, ae, "DESCRIÇÃO DO PRODUTO / SERVIÇO")
                .ComColuna(5.6F, ac, "NCM/SH")
                .ComColuna(4.1F, ac, cabecalho4)
                .ComColuna(3.5F, ac, "CFOP")
                .ComColuna(2.6F, ac, "UN")
                .ComColuna(5.0F, ad, "QUANTI.")
                .ComColuna(4.7F, ad, "VALOR", "UNIT.")
                .ComColuna(4.7F, ad, "VALOR", "DESC.")
                .ComColuna(5.0F, ad, "VALOR", "LÍQUIDO")
                .ComColuna(5.0F, ad, "B CÁLC", "ICMS")
                .ComColuna(4.7F, ad, "VALOR", "ICMS")
                .ComColuna(5.0F, ad, "B CÁLC", "ICMS ST")
                .ComColuna(4.7F, ad, "VALOR", "ICMS ST")
                .ComColuna(4.7F, ad, "VALOR", "IPI")
                .ComColuna(3.7F, ad, "ALIQ.", "ICMS")
                .ComColuna(3.7F, ad, "ALIQ.", "IPI");
            }
            else
            {
                Tabela
                .ComColuna(6F, ac, "CÓDIGO")
                .ComColuna(0, ae, "DESCRIÇÃO DO PRODUTO / SERVIÇO")
                .ComColuna(4.8F, ac, "NCM/SH")
                .ComColuna(4.1F, ac, cabecalho4)
                .ComColuna(3.5F, ac, "CFOP")
                .ComColuna(2.6F, ac, "UN")
                .ComColuna(4.7F, ad, "QUANTI.")
                .ComColuna(4.5F, ad, "VALOR", "UNIT.")
                .ComColuna(4.5F, ad, "VALOR", "DESC.")
                .ComColuna(4.8F, ad, "VALOR", "LÍQUIDO")
                .ComColuna(4.8F, ad, "B CÁLC", "ICMS")
                .ComColuna(4.5F, ad, "VALOR", "ICMS")
                .ComColuna(4.8F, ad, "B CÁLC", "ICMS ST")
                .ComColuna(4.5F, ad, "VALOR", "ICMS ST")
                .ComColuna(4.5F, ad, "VALOR", "IPI")
                .ComColuna(3.7F, ad, "ALIQ.", "ICMS")
                .ComColuna(3.7F, ad, "ALIQ.", "IPI");
            }

            Tabela.AjustarLarguraColunas();

            foreach (var p in ViewModel.Produtos)
            {
                var linha = new List<String>
                {
                    p.Codigo,
                    p.DescricaoCompleta,
                    p.Ncm,
                    p.OCst,
                    p.Cfop.Formatar("N0"),
                    p.Unidade,
                    p.Quantidade.Formatar(),
                    p.ValorUnitario.Formatar(),
                    p.ValorDesconto.Formatar(),
                    p.ValorLiquido.Formatar(),
                    p.BaseIcms.Formatar(),
                    p.ValorIcms.Formatar(),
                    p.BaseIcmsSt.Formatar(),
                    p.ValorIcmsSt.Formatar(),
                    (p.ValorIpi ?? 0).Formatar(),
                    p.AliquotaIcms.Formatar(),
                    (p.AliquotaIpi ?? 0).Formatar()
                };

                Tabela.AdicionarLinha(linha);
            }
        }

        /// <summary>
        /// Leiaute do quadro "Dados dos Produtos/Serviços" da NT 2026.010 item 4.3, com as bases, alíquotas
        /// e valores agrupados por tributo. Tributos ausentes no XML não são impressos (item 4.4).
        /// </summary>
        private void ConfigurarLeiauteReformaTributaria()
        {
            var ad = AlinhamentoHorizontal.Direita;
            var ac = AlinhamentoHorizontal.Centro;
            var ae = AlinhamentoHorizontal.Esquerda;
            String rotuloCst = ViewModel.Emitente.CRT == "3" ? "CST" : "CSOSN";

            Tabela
            .ComColuna(6.5F, ac, "CÓDIGO", "PRODUTO")
            .ComColuna(0, ae, "DESCRIÇÃO DO PRODUTO / SERVIÇO")
            .ComColuna(6.5F, ae, "CST /", "CFOP")
            .ComColuna(5.5F, ad, "QTD /", "UN")
            .ComColuna(5.5F, ad, "VLR", "UNIT")
            .ComColuna(6F, ad, "VLR", "TOTAL")
            .ComColuna(10F, ad, "BASES DE", "CÁLCULO")
            .ComColuna(17F, ad, "ALÍQUOTAS")
            .ComColuna(17F, ad, "VALOR DOS", "TRIBUTOS");

            Tabela.AjustarLarguraColunas();

            foreach (var p in ViewModel.Produtos)
            {
                var classificacao = new StringBuilder();
                if (!String.IsNullOrWhiteSpace(p.Ncm)) classificacao.Append("NCM ").Append(p.Ncm);
                if (!String.IsNullOrWhiteSpace(p.ClassificacaoTributaria)) classificacao.Append(" cClassTrib ").Append(p.ClassificacaoTributaria);

                var descricao = p.DescricaoCompleta;
                if (classificacao.Length > 0) descricao += "\n" + classificacao.ToString().Trim();

                var linha = new List<String>
                {
                    p.Codigo,
                    descricao,
                    $"{rotuloCst} {p.OCst}\nCFOP {p.Cfop}",
                    $"{p.Quantidade.Formatar()}\n{p.Unidade}",
                    p.ValorUnitario.Formatar(),
                    p.ValorTotal.Formatar(),
                    LinhasTributos(
                        new[] { Tributo("ICMS", p.BaseIcms) },
                        new[] { Tributo("IBS / CBS", p.BaseIbsCbs) },
                        new[] { Tributo("IS", p.BaseIs) },
                        new[] { Tributo("IPI", p.BaseIpi) }),
                    LinhasTributos(
                        new[] { Tributo("ICMS", p.AliquotaIcms, "%"), Tributo("CBS", p.AliquotaCbs, "%") },
                        new[] { Tributo("IBS UF", p.AliquotaIbsUf, "%"), Tributo("IPI", p.AliquotaIpi, "%") },
                        new[] { Tributo("IBS MUN", p.AliquotaIbsMunicipio, "%"), Tributo("IS", p.AliquotaIs, "%") }),
                    LinhasTributos(
                        new[] { Tributo("ICMS", p.ValorIcms), Tributo("CBS", p.ValorCbs) },
                        new[] { Tributo("IBS UF", p.ValorIbsUf), Tributo("IPI", p.ValorIpi) },
                        new[] { Tributo("IBS MUN", p.ValorIbsMunicipio), Tributo("IS", p.ValorIs) })
                };

                Tabela.AdicionarLinha(linha);
            }
        }

        private static String Tributo(String nome, double? valor, String sufixo = null)
        {
            return valor.HasValue ? $"{nome} {valor.Formatar()}{sufixo}" : null;
        }

        /// <summary>
        /// Monta uma linha por grupo de tributos, separados por " / ", omitindo os ausentes.
        /// </summary>
        private static String LinhasTributos(params String[][] grupos)
        {
            return String.Join("\n", grupos
                .Select(g => String.Join(" / ", g.Where(t => t != null)))
                .Where(l => l.Length > 0));
        }

        public override void Draw(Gfx gfx)
        {
            base.Draw(gfx);

            Tabela.SetPosition(RetanguloTabela.Location);
            Tabela.SetSize(RetanguloTabela.Size);
            Tabela.Draw(gfx);

            CabecalhoBloco.SetPosition(X, Y);
            CabecalhoBloco.Width = Width;
            CabecalhoBloco.Draw(gfx);    
        }


        public RectangleF RetanguloTabela => BoundingBox.CutTop(CabecalhoBloco.Height);
        public Boolean CompletamenteDesenhada => Tabela.LinhaAtual == ViewModel.Produtos.Count;
        public override bool PossuiContono => false;
    }
}
