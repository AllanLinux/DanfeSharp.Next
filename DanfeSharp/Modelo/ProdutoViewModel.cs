using System;

namespace DanfeSharp.Modelo
{
    public class ProdutoViewModel
    {
        /// <summary>
        /// <para>Código do produto ou serviço</para>
        /// <para>Tag cProd</para>
        /// </summary>
        public String Codigo { get; set; }

        /// <summary>
        /// <para>Informações Adicionais do Produto</para>
        /// <para>Tag infAdProd</para>
        /// </summary>
        public String InformacoesAdicionais { get; set; }

        /// <summary>
        /// <para>Descrição do produto ou serviço</para>
        /// <para>Tag xProd</para>
        /// </summary>
        public String Descricao { get; set; }

        /// <summary>
        /// <para>Código NCM com 8 dígitos ou 2 dígitos (gênero)</para>
        /// <para>Tag NCM</para>
        /// </summary>
        public String Ncm { get; set; }


        /// <summary>
        /// <para>Origem da mercadoria + espaço + CST/CSOSN (ex.: "0 102")</para>
        /// <para>Tag orig e CST</para>
        /// </summary>
        public String OCst { get; set; }

        /// <summary>
        /// <para>Código Fiscal de Operações e Prestações</para>
        /// <para>Tag CFOP</para>
        /// </summary>
        public int Cfop { get; set; }

        /// <summary>
        /// <para>Unidade Comercial</para>
        /// <para>Tag uCom</para>
        /// </summary>
        public String Unidade { get; set; }

        /// <summary>
        /// <para>Quantidade Comercial</para>
        /// <para>Tag qCom</para>
        /// </summary>
        public double Quantidade { get; set; }

        /// <summary>
        /// <para>Valor Unitário de Comercialização</para>
        /// <para>Tag vUnCom</para>
        /// </summary>
        public double ValorUnitario { get; set; }

        /// <summary>
        /// <para>Valor Total Bruto dos Produtos ou Serviços</para>
        /// <para>Tag vProd</para>
        /// </summary>
        public double ValorTotal { get; set; }

        /// <summary>
        /// <para>Valor da BC do ICMS</para>
        /// <para>Tag vBC</para>
        /// </summary>
        public double BaseIcms { get; set; }

        /// <summary>
        /// <para>Valor do ICMS</para>
        /// <para>Tag vICMS</para>
        /// </summary>
        public double ValorIcms { get; set; }

        /// <summary>
        /// <para>Alíquota do imposto</para>
        /// <para>Tag pICMS</para>
        /// </summary>
        public double AliquotaIcms { get; set; }

        /// <summary>
        /// <para>Valor do IPI</para>
        /// <para>Tag vIPI</para>
        /// </summary>
        public double? ValorIpi { get; set; }

        /// <summary>
        /// <para>Alíquota do IPI</para>
        /// <para>Tag pIPI</para>
        /// </summary>
        public double? AliquotaIpi { get; set; }

        /// <summary>
        /// <para>Valor da BC do IPI</para>
        /// <para>Tag vBC</para>
        /// </summary>
        public double? BaseIpi { get; set; }

        /// <summary>
        /// <para>Valor aproximado total de tributos federais, estaduais e municipais. [NT2013.003]</para>
        /// <para>Tag vTotTrib</para>
        /// </summary>
        public double? ValorAproximadoTributos { get; set; }

        /// <summary>
        /// <para>Valor do Desconto</para>
        /// <para>Tag vDesc</para>
        /// </summary>
        public double? ValorDesconto { get; set; }

        /// <summary>
        /// <para>Valor da BC do ICMS ST</para>
        /// <para>Tag vBCST</para>
        /// </summary>
        public double? BaseIcmsSt { get; set; }

        /// <summary>
        /// <para>Valor do ICMS ST</para>
        /// <para>Tag vICMSST</para>
        /// </summary>
        public double? ValorIcmsSt { get; set; }

        #region Reforma Tributária - IBS, CBS e IS (NT 2025.002-RTC e NT 2026.010)

        /// <summary>
        /// <para>Código de Situação Tributária do IBS e da CBS</para>
        /// <para>Tag CST - UB13</para>
        /// </summary>
        public String CstIbsCbs { get; set; }

        /// <summary>
        /// <para>Classificação Tributária do IBS e da CBS</para>
        /// <para>Tag cClassTrib - UB14</para>
        /// </summary>
        public String ClassificacaoTributaria { get; set; }

        /// <summary>
        /// <para>Base de Cálculo do IBS e da CBS</para>
        /// <para>Tag vBC - UB16</para>
        /// </summary>
        public double? BaseIbsCbs { get; set; }

        /// <summary>
        /// <para>Alíquota do IBS UF, ou a alíquota efetiva quando houver redução</para>
        /// <para>Tag pIBSUF - UB18 ou pAliqEfet - UB28</para>
        /// </summary>
        public double? AliquotaIbsUf { get; set; }

        /// <summary>
        /// <para>Valor do IBS UF</para>
        /// <para>Tag vIBSUF - UB35</para>
        /// </summary>
        public double? ValorIbsUf { get; set; }

        /// <summary>
        /// <para>Alíquota do IBS Município, ou a alíquota efetiva quando houver redução</para>
        /// <para>Tag pIBSMun - UB37 ou pAliqEfet - UB47</para>
        /// </summary>
        public double? AliquotaIbsMunicipio { get; set; }

        /// <summary>
        /// <para>Valor do IBS Município</para>
        /// <para>Tag vIBSMun - UB54</para>
        /// </summary>
        public double? ValorIbsMunicipio { get; set; }

        /// <summary>
        /// <para>Alíquota da CBS, ou a alíquota efetiva quando houver redução</para>
        /// <para>Tag pCBS - UB56 ou pAliqEfet - UB66</para>
        /// </summary>
        public double? AliquotaCbs { get; set; }

        /// <summary>
        /// <para>Valor da CBS</para>
        /// <para>Tag vCBS - UB67</para>
        /// </summary>
        public double? ValorCbs { get; set; }

        /// <summary>
        /// <para>Base de Cálculo do Imposto Seletivo</para>
        /// <para>Tag vBCIS - UB05</para>
        /// </summary>
        public double? BaseIs { get; set; }

        /// <summary>
        /// <para>Alíquota do Imposto Seletivo</para>
        /// <para>Tag pIS - UB06</para>
        /// </summary>
        public double? AliquotaIs { get; set; }

        /// <summary>
        /// <para>Valor do Imposto Seletivo</para>
        /// <para>Tag vIS - UB11</para>
        /// </summary>
        public double? ValorIs { get; set; }

        /// <summary>
        /// Verdadeiro quando o item possui informações de IBS, CBS ou IS.
        /// </summary>
        public bool PossuiIbsCbsIs => !String.IsNullOrWhiteSpace(ClassificacaoTributaria) || BaseIbsCbs.HasValue || ValorIs.HasValue;

        #endregion

        public ProdutoViewModel()
        {
            AliquotaIpi = null;
            ValorIpi = null;
        }

        public String DescricaoCompleta
        {
            get
            {
                String descriCaoCompleta = Descricao;

                if (!String.IsNullOrWhiteSpace(InformacoesAdicionais))
                {
                    descriCaoCompleta += "\r\n" + InformacoesAdicionais;
                }
                
                return descriCaoCompleta;
            }
        }
    }
}
