using System.IO;
using DanfeSharp.NFSe;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DanfeSharp.Test
{
    /// <summary>
    /// Leiaute da NT 009 da NFS-e (tags renomeadas e reposicionadas) e compatibilidade com o leiaute anterior.
    /// </summary>
    [TestClass]
    public class DanfseNt009Tests
    {
        public readonly string OutputDirectory = Path.Combine("Output", "NFSe");
        public readonly string InputDirectory = Path.Combine("Xml", "NFSe");

        public DanfseNt009Tests()
        {
            if (!Directory.Exists(OutputDirectory))
                Directory.CreateDirectory(OutputDirectory);
        }

        private DanfseViewModel Ler(string arquivo) => DanfseViewModelCreator.CriarDeArquivoXml(Path.Combine(InputDirectory, arquivo));

        private int Gerar(string arquivo)
        {
            using (var danfse = new Danfse(Ler(arquivo)))
            {
                danfse.Gerar();
                int folhas = danfse.PdfDocument.PageCount;
                danfse.Salvar(Path.Combine(OutputDirectory, Path.ChangeExtension(arquivo, ".pdf")));
                return folhas;
            }
        }

        [TestMethod]
        public void Release1_Pdf() => Assert.AreEqual(1, Gerar("nfse_nt009_release1.xml"));

        [TestMethod]
        public void Release2_Pdf() => Assert.AreEqual(1, Gerar("nfse_nt009_release2.xml"));

        [TestMethod]
        public void Release1_CamposRenomeados()
        {
            var m = Ler("nfse_nt009_release1.xml");

            // finNFSe na raiz do infDPS (item 2.2)
            Assert.AreEqual("NFS-e regular", m.Finalidade);
            // CST e cClassTrib direto no trib (item 2.16)
            Assert.AreEqual("000 / 000001", m.CstClassificacaoTributaria);
            // vAjusteBCISSQN (40.000,00) + vCalcAjusteBCIBSCBS (500,00) (item 2.3)
            Assert.AreEqual(40500, m.TotalDeducoesReducoes);
            // desconto incondicionado + vCalcAjusteBCIBSCBS + ISSQN + PIS + COFINS
            Assert.AreEqual(8880, m.ExclusoesReducoesBaseCalculo);
        }

        [TestMethod]
        public void Release2_DestinatarioNaRaizDoInfDps()
        {
            var m = Ler("nfse_nt009_release2.xml");

            Assert.IsNotNull(m.Destinatario);
            Assert.AreEqual("11.222.333/0001-81", m.Destinatario.Documento);
            Assert.AreEqual("DESTINATARIO TESTE NT009 LTDA", m.Destinatario.Nome);
        }

        [TestMethod]
        public void LeiauteAnterior_InscricaoImobiliaria()
        {
            StringAssert.Contains(Ler("nfse_imovel_leiaute_anterior.xml").InformacoesComplementares, "Insc. Imob.: IMOB-123456");
        }

        [TestMethod]
        public void CnpjAlfanumerico()
        {
            Assert.AreEqual("12.ABC.345/01DE-35", Ler("nfse_nt009_cnpj_alfanumerico.xml").Tomador.Documento);
            Assert.AreEqual("12.ABC.345/01DE-35", Formatador.FormatarCnpj("12ABC34501DE35"));
            Assert.AreEqual("11.222.333/0001-81", Formatador.FormatarCnpj("11222333000181"));
        }
    }
}
