using System;
using System.IO;
using System.Xml;
using DanfeSharp.NFSe;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DanfeSharp.Test
{
    [TestClass]
    public class DanfseTests
    {
        public readonly string OutputDirectory = Path.Combine("Output", "NFSe");
        public readonly string InputDirectory = Path.Combine("Xml", "NFSe");

        public DanfseTests()
        {
            if (!Directory.Exists(OutputDirectory))
                Directory.CreateDirectory(OutputDirectory);
        }

        private DanfseViewModel Ler(string arquivo) => DanfseViewModelCreator.CriarDeArquivoXml(Path.Combine(InputDirectory, arquivo));

        private int Gerar(DanfseViewModel model, string nomePdf, bool comLogo = false)
        {
            using (var danfse = new Danfse(model))
            {
                if (comLogo) danfse.AdicionarLogoImagem(Path.Combine("Logos", "JPG", "Horizontal.jpg"));
                danfse.Gerar();
                int folhas = danfse.PdfDocument.PageCount;
                danfse.Salvar(Path.Combine(OutputDirectory, nomePdf));
                return folhas;
            }
        }

        [TestMethod]
        public void IbsCbs() => Assert.AreEqual(1, Gerar(Ler("nfse_ibscbs.xml"), "nfse_ibscbs.pdf"));

        [TestMethod]
        public void Imune() => Assert.AreEqual(1, Gerar(Ler("nfse_imune.xml"), "nfse_imune.pdf", true));

        [TestMethod]
        public void Homologacao() => Assert.AreEqual(1, Gerar(Ler("nfse_homologacao.xml"), "nfse_homologacao.pdf"));

        [TestMethod]
        public void LogotipoNfsePadrao()
        {
            using (var danfse = new Danfse(Ler("nfse_ibscbs.xml")))
            {
                Assert.IsNotNull(danfse.Logo);
                Assert.AreEqual(540, danfse.Logo.PixelWidth);
                Assert.AreEqual(107, danfse.Logo.PixelHeight);
            }
        }

        [TestMethod]
        public void IbsCbs_Modelo()
        {
            var m = Ler("nfse_ibscbs.xml");

            Assert.AreEqual("33045572212345678000195000000001238226091234567890", m.ChaveAcesso);
            Assert.AreEqual("12382", m.Numero);
            Assert.AreEqual(new DateTime(2026, 9, 1), m.Competencia);
            Assert.AreEqual(new DateTime(2026, 9, 1, 16, 28, 48), m.DataHoraEmissao);
            Assert.AreEqual("201702", m.NumeroDps);
            Assert.AreEqual("Prestador", m.Emitente);
            Assert.AreEqual("NFS-e Gerada", m.Situacao);
            Assert.AreEqual("NFS-e regular", m.Finalidade);
            Assert.AreEqual("Rio de Janeiro - RJ", m.MunicipioEmissor);
            Assert.AreEqual("https://www.nfse.gov.br/ConsultaPublica?tpc=1&chave=" + m.ChaveAcesso, m.QrCode);

            Assert.AreEqual("12.345.678/0001-95", m.Prestador.Documento);
            Assert.AreEqual("ENERGIA EXEMPLO S A", m.Prestador.Nome);
            Assert.AreEqual("Rio de Janeiro / RJ", m.Prestador.Municipio);
            Assert.AreEqual("33.04557 / 20.080-002", m.Prestador.CodigoIbgeCep);
            Assert.AreEqual("AVENIDA EXEMPLO, 168, CENTRO", m.Prestador.Endereco);
            Assert.AreEqual("Não optante", m.Prestador.SimplesNacional);
            Assert.IsNull(m.Prestador.Telefone);

            Assert.AreEqual("Rio de Janeiro / RJ", m.Tomador.Municipio);
            Assert.AreEqual("33.04557 / 23.075-570", m.Tomador.CodigoIbgeCep);
            Assert.IsNull(m.Destinatario);
            Assert.IsNull(m.Intermediario);

            Assert.AreEqual("03.04.03 / -", m.CodigoTributacao);
            Assert.AreEqual("1.1001.12.10", m.CodigoNbs);
            Assert.AreEqual("Volta Redonda / RJ / -", m.LocalPrestacao);

            Assert.AreEqual("Operação Tributável", m.TipoTributacaoIssqn);
            Assert.AreEqual("Volta Redonda / RJ / -", m.MunicipioIncidenciaIssqn);
            Assert.AreEqual(185.40, m.BaseCalculoIssqn);
            Assert.AreEqual(5.00, m.AliquotaIssqn);
            Assert.AreEqual("Não Retido", m.RetencaoIssqn);
            Assert.AreEqual(9.27, m.ValorIssqn);

            Assert.AreEqual(3.06, m.Pis);
            Assert.AreEqual(14.09, m.Cofins);
            Assert.AreEqual("0 - PIS/COFINS/CSLL Não Retidos", m.DescricaoContribuicoesSociaisRetidas);

            Assert.AreEqual("200 / 200027", m.CstClassificacaoTributaria);
            Assert.AreEqual("020201 / 3306305 / Volta Redonda / RJ", m.IndicadorOperacaoIncidencia);
            Assert.AreEqual(26.42, m.ExclusoesReducoesBaseCalculo);
            Assert.AreEqual(158.98, m.BaseCalculoIbsCbs);
            Assert.AreEqual(70.00, m.ReducaoAliquotaCbs);
            Assert.AreEqual(0.03, m.AliquotaEfetivaIbsUf);
            Assert.AreEqual(0.05, m.ValorIbsUf);
            Assert.AreEqual(0.27, m.AliquotaEfetivaCbs);
            Assert.AreEqual(0.43, m.ValorCbs);
            Assert.AreEqual(0.48, m.TotalIbsCbs);

            Assert.AreEqual(185.40, m.ValorServico);
            Assert.AreEqual(185.40, m.ValorLiquido);
            Assert.AreEqual(185.40, m.ValorLiquidoComIbsCbs);
            StringAssert.Contains(m.InformacoesComplementares, "Federais: R$ 17,15; Estaduais: R$ 0,00; Municipais: R$ 9,27;");
        }

        [TestMethod]
        public void Imune_Modelo()
        {
            var m = Ler("nfse_imune.xml");

            Assert.AreEqual("Não Incidência", m.TipoTributacaoIssqn);
            Assert.IsNull(m.MunicipioIncidenciaIssqn);
            Assert.IsNull(m.BaseCalculoIssqn);
            Assert.AreEqual("(21) 4000-4000", m.Prestador.Telefone);
            Assert.AreEqual("São Paulo / SP", m.Tomador.Municipio);
            Assert.AreEqual("12345678", m.Tomador.InscricaoMunicipal);
            Assert.AreEqual("17.25.01 / -", m.CodigoTributacao);
            Assert.AreEqual("410 / 410008", m.CstClassificacaoTributaria);
            Assert.AreEqual("100301 / 3550308 / São Paulo / SP", m.IndicadorOperacaoIncidencia);
            Assert.IsNull(m.AliquotaCbs);
            Assert.AreEqual(0.0, m.ValorCbs);
            Assert.AreEqual(0.0, m.ExclusoesReducoesBaseCalculo);
            Assert.AreEqual(2288.00, m.ValorLiquidoComIbsCbs);
        }

        [TestMethod]
        public void Homologacao_Modelo()
        {
            var m = Ler("nfse_homologacao.xml");

            Assert.IsTrue(m.IsHomologacao);
            Assert.AreEqual("Tomador", m.Emitente);
            Assert.AreEqual("NFS-e de Substituição Gerada", m.Situacao);
            Assert.AreEqual("900", m.SerieDps);

            Assert.AreEqual("123.456.789-09", m.Prestador.Documento);
            Assert.AreEqual("DESENVOLVEDOR AUTONOMO EXEMPLO", m.Prestador.Nome);
            Assert.AreEqual("RUA DO PRESTADOR, 200, SALA 3, CENTRO", m.Prestador.Endereco);
            Assert.AreEqual("Curitiba / PR", m.Prestador.Municipio);
            Assert.AreEqual("(41) 99999-8888", m.Prestador.Telefone);
            Assert.AreEqual("Optante - Microempreendedor Individual (MEI)", m.Prestador.SimplesNacional);
            Assert.AreEqual("Tributos federais e municipal pelo SN", m.Prestador.RegimeApuracaoSimplesNacional);

            Assert.IsNull(m.Tomador);
            Assert.AreEqual("São Paulo / SP", m.Intermediario.Municipio);
            Assert.AreEqual("DE123456789", m.Destinatario.Documento);
            Assert.AreEqual("Berlin / Berlin / DE", m.Destinatario.Municipio);

            Assert.AreEqual("01.01.01 / 001", m.CodigoTributacao);
            Assert.AreEqual("Retido pelo Tomador", m.RetencaoIssqn);
            Assert.AreEqual(20.00, m.TotalRetencoes);
            Assert.AreEqual(50.00, m.DescontoCondicionado);
            Assert.AreEqual("030101 / - / - / -", m.IndicadorOperacaoIncidencia);
            Assert.IsNull(m.BaseCalculoIbsCbs);
            Assert.IsNull(m.TotalIbsCbs);

            StringAssert.StartsWith(m.InformacoesComplementares, "NFS-e substituída: 41069021");
            StringAssert.Contains(m.InformacoesComplementares, "Pedido 2026-0077.");
            StringAssert.Contains(m.InformacoesComplementares, "Simples Nacional: 6,00 %");
        }

        [TestMethod]
        public void BeneficioMunicipal() => Assert.AreEqual(1, Gerar(Ler("nfse_beneficio_municipal.xml"), "nfse_beneficio_municipal.pdf"));

        [TestMethod]
        public void BeneficioMunicipal_Modelo()
        {
            var m = Ler("nfse_beneficio_municipal.xml");

            Assert.IsTrue(m.ExibirBeneficioMunicipal);
            Assert.AreEqual("Alíquota diferenciada", m.BeneficioMunicipal);
            Assert.IsNull(m.CalculoBeneficioMunicipal);
            Assert.IsNull(m.TotalDeducoesReducoes);
            Assert.AreEqual("3 - PIS/COFINS/CSLL Retidos", m.DescricaoContribuicoesSociaisRetidas);
            StringAssert.EndsWith(m.Prestador.Endereco, "ANEXO DO PREDIO PRINCIPAL, CENTRO");
        }

        [TestMethod]
        public void SemBeneficioMunicipal_NaoExibeALinha()
        {
            Assert.IsFalse(Ler("nfse_ibscbs.xml").ExibirBeneficioMunicipal);
            Assert.IsFalse(Ler("nfse_imune.xml").ExibirBeneficioMunicipal);
        }

        [TestMethod]
        public void XmlDeOutroDocumento_Recusado()
        {
            Assert.ThrowsException<XmlException>(() => DanfseViewModelCreator.CriarDeArquivoXml(Path.Combine("Xml", "CTe", "cte_rodoviario.xml")));
        }
    }
}
