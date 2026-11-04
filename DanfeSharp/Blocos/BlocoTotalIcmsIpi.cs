using DanfeSharp.Modelo;

namespace DanfeSharp.Blocos
{
    /// <summary>
    /// Quadro "Total do ICMS / IPI" do leiaute da Reforma Tributária (NT 2026.010).
    /// As linhas de FCP/DIFAL e de ICMS monofásico são facultativas e só aparecem quando possuem valores (item 4.4).
    /// </summary>
    class BlocoTotalIcmsIpi : BlocoBase
    {
        public BlocoTotalIcmsIpi(DanfeViewModel viewModel, Estilo estilo) : base(viewModel, estilo)
        {
            var m = ViewModel.CalculoImposto;

            AdicionarLinhaCampos()
            .ComCampoNumerico("Base de cálculo do ICMS", m.BaseCalculoIcms)
            .ComCampoNumerico("Valor do ICMS", m.ValorIcms)
            .ComCampoNumerico("Base de cálculo do ICMS ST", m.BaseCalculoIcmsSt)
            .ComCampoNumerico("Valor do ICMS ST", m.ValorIcmsSt)
            .ComCampoNumerico("Valor do IPI", m.ValorIpi)
            .ComLargurasIguais();

            if (m.ExibirFcpDifal)
            {
                var l = AdicionarLinhaCampos()
                .ComCampoNumerico("Valor do FCP", m.ValorFcp)
                .ComCampoNumerico("Valor do FCP retido por ST", m.ValorFcpSt);

                if (ViewModel.ExibirIcmsInterestadual)
                {
                    l.ComCampoNumerico("Valor do DIFAL na UF de destino", m.vICMSUFDest ?? 0)
                     .ComCampoNumerico("Valor do FCP na UF de destino", m.vFCPUFDest ?? 0);
                }

                l.ComLargurasIguais();
            }

            if (m.ExibirIcmsMonofasico)
            {
                AdicionarLinhaCampos()
                .ComCampoNumerico("BC do ICMS monofásico", m.QuantidadeBaseIcmsMonofasico, 4)
                .ComCampoNumerico("Valor do ICMS monofásico", m.ValorIcmsMonofasico)
                .ComCampoNumerico("BC do ICMS monofásico por retenção", m.QuantidadeBaseIcmsMonofasicoRetencao, 4)
                .ComCampoNumerico("Valor do ICMS monofásico por retenção", m.ValorIcmsMonofasicoRetencao)
                .ComLargurasIguais();
            }
        }

        public override PosicaoBloco Posicao => PosicaoBloco.Topo;
        public override string Cabecalho => "Total do ICMS / IPI";
    }
}
