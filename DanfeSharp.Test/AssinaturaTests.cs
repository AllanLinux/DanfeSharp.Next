using System.IO;
using DanfeSharp.CTe;
using DanfeSharp.Eventos;
using DanfeSharp.Modelo;
using DanfeSharp.NFSe;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DanfeSharp.Test
{
    /// <summary>
    /// Gera cada documento com e sem a assinatura "Impresso com DanfeSharp.Next" no rodapé.
    /// </summary>
    [TestClass]
    public class AssinaturaTests
    {
        public readonly string OutputDirectory = Path.Combine("Output", "Assinatura");

        public AssinaturaTests()
        {
            if (!Directory.Exists(OutputDirectory))
                Directory.CreateDirectory(OutputDirectory);
        }

        private void Gerar(DocumentoAuxiliar documento, string nome, bool exibir)
        {
            using (documento)
            {
                Assert.IsTrue(documento.ExibirAssinatura, "A assinatura deve aparecer por padrão.");
                documento.ExibirAssinatura = exibir;
                documento.Gerar();
                documento.Salvar(Path.Combine(OutputDirectory, $"{nome}_{(exibir ? "com" : "sem")}_assinatura.pdf"));
            }
        }

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Danfe(bool exibir) =>
            Gerar(new Danfe(DanfeViewModelCreator.CriarDeArquivoXml(Path.Combine("Xml", "NFe", "v4.00", "v4_ReformaTributaria.xml"))), "danfe", exibir);

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Evento(bool exibir) =>
            Gerar(new DocumentoEvento(EventoViewModelCreator.CriarDeArquivoXml(Path.Combine("Xml", "Eventos", "cce.xml"))), "evento", exibir);

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Dacte(bool exibir) =>
            Gerar(new Dacte(DacteViewModelCreator.CriarDeArquivoXml(Path.Combine("Xml", "CTe", "cte_rodoviario.xml"))), "dacte", exibir);

        [DataTestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void Danfse(bool exibir) =>
            Gerar(new Danfse(DanfseViewModelCreator.CriarDeArquivoXml(Path.Combine("Xml", "NFSe", "nfse_ibscbs.xml"))), "danfse", exibir);

        [TestMethod]
        public void Texto() => Assert.AreEqual("Impresso com DanfeSharp.Next", DocumentoAuxiliar.TextoAssinatura);
    }
}
