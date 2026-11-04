using DanfeSharp.Modelo;

namespace DanfeSharp.Blocos
{
    /// <summary>
    /// Quadro "Total dos produtos e total da nota" do leiaute da Reforma Tributária (NT 2026.010).
    /// </summary>
    class BlocoTotalProdutosNota : BlocoBase
    {
        public BlocoTotalProdutosNota(DanfeViewModel viewModel, Estilo estilo) : base(viewModel, estilo)
        {
            var m = ViewModel.CalculoImposto;

            AdicionarLinhaCampos()
            .ComCampoNumerico("Valor total dos produtos", m.ValorTotalProdutos ?? 0)
            .ComCampoNumerico("Valor do frete", m.ValorFrete)
            .ComCampoNumerico("Valor do seguro", m.ValorSeguro)
            .ComCampoNumerico("Desconto", m.Desconto)
            .ComCampoNumerico("Outras despesas", m.OutrasDespesas)
            .ComCampoNumerico("Valor total da nota", m.ValorTotalNota)
            .ComLargurasIguais();
        }

        public override PosicaoBloco Posicao => PosicaoBloco.Topo;
        public override string Cabecalho => "Total dos produtos e total da nota";
    }
}
