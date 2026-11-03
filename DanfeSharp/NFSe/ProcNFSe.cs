using System;
using System.Collections.Generic;
using System.Xml.Serialization;
using DanfeSharp.Esquemas;

namespace DanfeSharp.NFSe
{
    // Subconjunto do leiaute da NFS-e Nacional (versão 1.01, com os grupos do IBS e da CBS) usado na DANFSe.
    // Os códigos ficam como texto para preservar os zeros à esquerda.

    /// <summary>
    /// NFS-e gerada pelo Sistema Nacional NFS-e, com a DPS que a originou.
    /// </summary>
    [XmlType(Namespace = Namespaces.NFSe)]
    [XmlRoot("NFSe", Namespace = Namespaces.NFSe, IsNullable = false)]
    public class NotaFiscalServico
    {
        public InfNFSe infNFSe { get; set; }

        [XmlAttribute]
        public string versao { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class InfNFSe
    {
        [XmlAttribute]
        public string Id { get; set; }

        public string xLocEmi { get; set; }
        public string xLocPrestacao { get; set; }
        public string nNFSe { get; set; }
        public string cLocIncid { get; set; }
        public string xLocIncid { get; set; }
        public string xTribNac { get; set; }
        public string xTribMun { get; set; }
        public string xNBS { get; set; }
        public string verAplic { get; set; }
        public int ambGer { get; set; }
        public int tpEmis { get; set; }
        public int procEmi { get; set; }
        public int cStat { get; set; }
        public DateTimeOffsetIso8601? dhProc { get; set; }
        public string nDFSe { get; set; }

        /// <summary>
        /// Informações de uso da Administração Tributária Municipal.
        /// </summary>
        public string xOutInf { get; set; }

        public EmitenteNFSe emit { get; set; }
        public ValoresNFSe valores { get; set; }
        public IbsCbsNFSe IBSCBS { get; set; }
        public Dps DPS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class EmitenteNFSe
    {
        public string CNPJ { get; set; }
        public string CPF { get; set; }
        public string IM { get; set; }
        public string xNome { get; set; }
        public string xFant { get; set; }
        public EnderecoNacionalNFSe enderNac { get; set; }
        public string fone { get; set; }
        public string email { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class EnderecoNacionalNFSe
    {
        public string xLgr { get; set; }
        public string nro { get; set; }
        public string xCpl { get; set; }
        public string xBairro { get; set; }
        public string cMun { get; set; }
        public string UF { get; set; }
        public string CEP { get; set; }
    }

    /// <summary>
    /// Valores apurados pelo Sistema Nacional NFS-e.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ValoresNFSe
    {
        public double? vCalcDR { get; set; }
        public string tpBM { get; set; }
        public double? vCalcBM { get; set; }
        public double? vBC { get; set; }
        public double? pAliqAplic { get; set; }
        public double? vISSQN { get; set; }
        public double? vTotalRet { get; set; }
        public double? vLiq { get; set; }
    }

    #region IBS e CBS apurados

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class IbsCbsNFSe
    {
        public string cLocalidadeIncid { get; set; }
        public string xLocalidadeIncid { get; set; }
        public double? pRedutor { get; set; }
        public ValoresIbsCbsNFSe valores { get; set; }
        public TotaisIbsCbsNFSe totCIBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ValoresIbsCbsNFSe
    {
        public double? vBC { get; set; }
        public double? vCalcReeRepRes { get; set; }
        public AliquotasIbsUf uf { get; set; }
        public AliquotasIbsMun mun { get; set; }
        public AliquotasCbs fed { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class AliquotasIbsUf
    {
        public double? pIBSUF { get; set; }
        public double? pRedAliqUF { get; set; }
        public double? pAliqEfetUF { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class AliquotasIbsMun
    {
        public double? pIBSMun { get; set; }
        public double? pRedAliqMun { get; set; }
        public double? pAliqEfetMun { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class AliquotasCbs
    {
        public double? pCBS { get; set; }
        public double? pRedAliqCBS { get; set; }
        public double? pAliqEfetCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TotaisIbsCbsNFSe
    {
        /// <summary>
        /// Valor total da NFS-e com o IBS e a CBS, quando cobrados.
        /// </summary>
        public double? vTotNF { get; set; }
        public TotalIbsNFSe gIBS { get; set; }
        public TotalCbsNFSe gCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TotalIbsNFSe
    {
        public double? vIBSTot { get; set; }
        public TotalIbsUfNFSe gIBSUFTot { get; set; }
        public TotalIbsMunNFSe gIBSMunTot { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TotalIbsUfNFSe
    {
        public double? vDifUF { get; set; }
        public double? vIBSUF { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TotalIbsMunNFSe
    {
        public double? vDifMun { get; set; }
        public double? vIBSMun { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TotalCbsNFSe
    {
        public double? vDifCBS { get; set; }
        public double? vCBS { get; set; }
    }

    #endregion

    #region DPS

    /// <summary>
    /// Declaração de Prestação de Serviço, enviada pelo emitente.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class Dps
    {
        public InfDps infDPS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class InfDps
    {
        [XmlAttribute]
        public string Id { get; set; }

        public int tpAmb { get; set; }
        public DateTimeOffsetIso8601? dhEmi { get; set; }
        public string verAplic { get; set; }
        public string serie { get; set; }
        public string nDPS { get; set; }

        /// <summary>
        /// Data de competência (AAAA-MM-DD).
        /// </summary>
        public string dCompet { get; set; }

        public int tpEmit { get; set; }
        public string cLocEmi { get; set; }
        public SubstituicaoDps subst { get; set; }
        public PrestadorDps prest { get; set; }
        public PessoaDps toma { get; set; }
        public PessoaDps interm { get; set; }
        public ServicoDps serv { get; set; }
        public ValoresDps valores { get; set; }
        public IbsCbsDps IBSCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class SubstituicaoDps
    {
        public string chSubstda { get; set; }
        public string cMotivo { get; set; }
        public string xMotivo { get; set; }
    }

    /// <summary>
    /// Tomador, intermediário ou destinatário.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class PessoaDps
    {
        public string CNPJ { get; set; }
        public string CPF { get; set; }
        public string NIF { get; set; }
        public string cNaoNIF { get; set; }
        public string CAEPF { get; set; }
        public string IM { get; set; }
        public string xNome { get; set; }
        public EnderecoDps end { get; set; }
        public string fone { get; set; }
        public string email { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class PrestadorDps : PessoaDps
    {
        public RegimeTributarioDps regTrib { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class RegimeTributarioDps
    {
        public string opSimpNac { get; set; }
        public string regApTribSN { get; set; }
        public string regEspTrib { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class EnderecoDps
    {
        public EnderecoNacionalDps endNac { get; set; }
        public EnderecoExteriorDps endExt { get; set; }
        public string xLgr { get; set; }
        public string nro { get; set; }
        public string xCpl { get; set; }
        public string xBairro { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class EnderecoNacionalDps
    {
        public string cMun { get; set; }
        public string CEP { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class EnderecoExteriorDps
    {
        public string cPais { get; set; }
        public string cEndPost { get; set; }
        public string xCidade { get; set; }
        public string xEstProvReg { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ServicoDps
    {
        public LocalPrestacaoDps locPrest { get; set; }
        public CodigoServicoDps cServ { get; set; }
        public ObraDps obra { get; set; }
        public AtividadeEventoDps atvEvento { get; set; }
        public InformacoesComplementaresDps infoCompl { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ObraDps
    {
        public string inscImobFisc { get; set; }
        public string cObra { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class AtividadeEventoDps
    {
        public string idAtvEvt { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class LocalPrestacaoDps
    {
        public string cLocPrestacao { get; set; }
        public string cPaisPrestacao { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class CodigoServicoDps
    {
        public string cTribNac { get; set; }
        public string cTribMun { get; set; }
        public string xDescServ { get; set; }
        public string cNBS { get; set; }
        public string cIntContrib { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class InformacoesComplementaresDps
    {
        public string idDocTec { get; set; }
        public string docRef { get; set; }
        public string xPed { get; set; }
        public ItensPedidoDps gItemPed { get; set; }
        public string xInfComp { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ItensPedidoDps
    {
        [XmlElement("xItemPed")]
        public List<string> xItemPed { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ValoresDps
    {
        public ValorServicoDps vServPrest { get; set; }
        public DescontosDps vDescCondIncond { get; set; }
        public DeducoesReducoesDps vDedRed { get; set; }
        public TributacaoDps trib { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class DeducoesReducoesDps
    {
        public double? pDR { get; set; }
        public double? vDR { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ValorServicoDps
    {
        public double? vReceb { get; set; }
        public double? vServ { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class DescontosDps
    {
        public double? vDescIncond { get; set; }
        public double? vDescCond { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TributacaoDps
    {
        public TributacaoMunicipalDps tribMun { get; set; }
        public TributacaoFederalDps tribFed { get; set; }
        public TotalTributosDps totTrib { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TributacaoMunicipalDps
    {
        public string tribISSQN { get; set; }
        public string cPaisResult { get; set; }
        public BeneficioMunicipalDps BM { get; set; }
        public ExigibilidadeSuspensaDps exigSusp { get; set; }
        public string tpImunidade { get; set; }
        public string tpRetISSQN { get; set; }
        public double? pAliq { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ExigibilidadeSuspensaDps
    {
        public string tpSusp { get; set; }
        public string nProcesso { get; set; }
    }

    /// <summary>
    /// Benefício municipal do ISSQN informado na DPS.
    /// </summary>
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class BeneficioMunicipalDps
    {
        public string nBM { get; set; }
        public double? vRedBCBM { get; set; }
        public double? pRedBCBM { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TributacaoFederalDps
    {
        public PisCofinsDps piscofins { get; set; }
        public double? vRetCP { get; set; }
        public double? vRetIRRF { get; set; }
        public double? vRetCSLL { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class PisCofinsDps
    {
        public string CST { get; set; }
        public double? vBCPisCofins { get; set; }
        public double? pAliqPis { get; set; }
        public double? pAliqCofins { get; set; }
        public double? vPis { get; set; }
        public double? vCofins { get; set; }
        public string tpRetPisCofins { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TotalTributosDps
    {
        public ValorTotalTributosDps vTotTrib { get; set; }
        public PercentualTotalTributosDps pTotTrib { get; set; }
        public string indTotTrib { get; set; }
        public double? pTotTribSN { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ValorTotalTributosDps
    {
        public double? vTotTribFed { get; set; }
        public double? vTotTribEst { get; set; }
        public double? vTotTribMun { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class PercentualTotalTributosDps
    {
        public double? pTotTribFed { get; set; }
        public double? pTotTribEst { get; set; }
        public double? pTotTribMun { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class IbsCbsDps
    {
        public string finNFSe { get; set; }
        public string indFinal { get; set; }
        public string cIndOp { get; set; }
        public string tpOper { get; set; }
        public string tpEnteGov { get; set; }
        public string indDest { get; set; }
        public PessoaDps dest { get; set; }
        public ImovelDps imovel { get; set; }
        public ValoresIbsCbsDps valores { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ImovelDps
    {
        [XmlElement("gUnidImob")]
        public List<UnidadeImobiliariaDps> gUnidImob { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class UnidadeImobiliariaDps
    {
        public string inscImobFisc { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ValoresIbsCbsDps
    {
        public TributacaoIbsCbsDps trib { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class TributacaoIbsCbsDps
    {
        public ClassificacaoIbsCbsDps gIBSCBS { get; set; }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = Namespaces.NFSe)]
    public class ClassificacaoIbsCbsDps
    {
        public string CST { get; set; }
        public string cClassTrib { get; set; }
        public string cCredPres { get; set; }
    }

    #endregion
}
