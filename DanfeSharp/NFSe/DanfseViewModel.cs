using System;

namespace DanfeSharp.NFSe
{
    /// <summary>
    /// Prestador, tomador, intermediário ou destinatário da NFS-e, com os textos já formatados para a DANFSe.
    /// </summary>
    public class ParticipanteNfseViewModel
    {
        /// <summary>
        /// CNPJ, CPF ou NIF formatado.
        /// </summary>
        public String Documento { get; set; }
        public String InscricaoMunicipal { get; set; }
        public String Telefone { get; set; }
        public String Nome { get; set; }

        /// <summary>
        /// Município e UF, ex.: Rio de Janeiro / RJ.
        /// </summary>
        public String Municipio { get; set; }

        /// <summary>
        /// Código IBGE do município e CEP, ex.: 33.04557 / 20.080-002.
        /// </summary>
        public String CodigoIbgeCep { get; set; }

        public String Endereco { get; set; }
        public String Email { get; set; }

        /// <summary>
        /// Situação no Simples Nacional na data de competência (apenas do prestador).
        /// </summary>
        public String SimplesNacional { get; set; }

        /// <summary>
        /// Regime de apuração tributária pelo Simples Nacional (apenas do prestador).
        /// </summary>
        public String RegimeApuracaoSimplesNacional { get; set; }
    }

    /// <summary>
    /// Modelo da DANFSe (Documento Auxiliar da NFS-e) do padrão nacional, leiaute v2.0.
    /// </summary>
    public class DanfseViewModel
    {
        #region Cabeçalho e identificação

        public String ChaveAcesso { get; set; }
        public String Numero { get; set; }
        public DateTime? Competencia { get; set; }
        public DateTime? DataHoraEmissao { get; set; }

        public String NumeroDps { get; set; }
        public String SerieDps { get; set; }
        public DateTime? DataHoraEmissaoDps { get; set; }

        /// <summary>
        /// Quem emitiu a NFS-e: Prestador, Tomador ou Intermediário.
        /// </summary>
        public String Emitente { get; set; }
        public String Situacao { get; set; }
        public String Finalidade { get; set; }

        /// <summary>
        /// Município emissor, ex.: Rio de Janeiro - RJ.
        /// </summary>
        public String MunicipioEmissor { get; set; }

        public int AmbienteGerador { get; set; }

        /// <summary>
        /// 1 - Produção, 2 - Homologação.
        /// </summary>
        public int TipoAmbiente { get; set; }

        /// <summary>
        /// Texto do QR Code, com o endereço de consulta pública da NFS-e.
        /// </summary>
        public String QrCode { get; set; }

        public bool IsHomologacao => TipoAmbiente == 2;

        /// <summary>
        /// Exibe o município emissor no cabeçalho. Pela NT 008, não é exibido quando o item do código de
        /// tributação nacional é 99.
        /// </summary>
        public bool ExibirMunicipioEmissor { get; set; } = true;

        /// <summary>
        /// Imprime a marca d'água "CANCELADA" (NT 008, item 2.5.1). O XML da NFS-e não traz o cancelamento,
        /// que é um evento à parte; informe quando a NFS-e estiver cancelada.
        /// </summary>
        public bool Cancelada { get; set; }

        /// <summary>
        /// Imprime a marca d'água "SUBSTITUÍDA" (NT 008, item 2.5.2), para a NFS-e que foi substituída por outra.
        /// O XML da NFS-e substituída não traz essa informação; informe quando for o caso.
        /// </summary>
        public bool Substituida { get; set; }

        #endregion

        #region Participantes

        public ParticipanteNfseViewModel Prestador { get; set; }

        /// <summary>
        /// Tomador ou adquirente, null quando não identificado.
        /// </summary>
        public ParticipanteNfseViewModel Tomador { get; set; }

        /// <summary>
        /// Destinatário da operação, null quando não identificado.
        /// </summary>
        public ParticipanteNfseViewModel Destinatario { get; set; }

        /// <summary>
        /// Intermediário da operação, null quando não identificado.
        /// </summary>
        public ParticipanteNfseViewModel Intermediario { get; set; }

        #endregion

        #region Serviço

        /// <summary>
        /// Código de tributação nacional e municipal, ex.: 03.04.03 / -.
        /// </summary>
        public String CodigoTributacao { get; set; }
        public String CodigoNbs { get; set; }

        /// <summary>
        /// Local da prestação, UF e país, ex.: Volta Redonda / RJ / -.
        /// </summary>
        public String LocalPrestacao { get; set; }

        /// <summary>
        /// Descrição do código de tributação: a municipal (xTribMun), quando informada, ou a nacional (xTribNac).
        /// </summary>
        public String DescricaoTributacaoNacional { get; set; }
        public String DescricaoServico { get; set; }

        #endregion

        #region Tributação municipal (ISSQN)

        /// <summary>
        /// Exibe o quadro do ISSQN. Quando a NFS-e não traz a tributação municipal, a DANFSe informa
        /// "TRIBUTAÇÃO MUNICIPAL (ISSQN) - OPERAÇÃO NÃO SUJEITA AO ISSQN" (NT 008, nota 4).
        /// </summary>
        public bool ExibirTributacaoMunicipal { get; set; } = true;

        /// <summary>
        /// Regime especial de tributação do ISSQN (regEspTrib), ex.: Estimativa.
        /// </summary>
        public String RegimeEspecialTributacao { get; set; }

        /// <summary>
        /// Tipo de imunidade do ISSQN (tpImunidade).
        /// </summary>
        public String TipoImunidade { get; set; }

        /// <summary>
        /// Suspensão da exigibilidade do ISSQN (tpSusp).
        /// </summary>
        public String SuspensaoExigibilidade { get; set; }

        public String NumeroProcessoSuspensao { get; set; }

        /// <summary>
        /// Exibe a linha do regime especial, imunidade e suspensão, só quando a nota informa algum deles (NT 008, nota 5).
        /// </summary>
        public bool ExibirRegimeEspecial { get; set; }

        public String TipoTributacaoIssqn { get; set; }
        public String MunicipioIncidenciaIssqn { get; set; }
        public double? BaseCalculoIssqn { get; set; }
        public double? AliquotaIssqn { get; set; }
        public String RetencaoIssqn { get; set; }
        public double? ValorIssqn { get; set; }

        /// <summary>
        /// Tipo do benefício municipal do ISSQN, ex.: Alíquota diferenciada.
        /// </summary>
        public String BeneficioMunicipal { get; set; }

        /// <summary>
        /// Valor do cálculo do benefício municipal (vCalcBM).
        /// </summary>
        public double? CalculoBeneficioMunicipal { get; set; }

        /// <summary>
        /// Total das deduções e reduções da base de cálculo do ISSQN.
        /// </summary>
        public double? TotalDeducoesReducoes { get; set; }

        /// <summary>
        /// Exibe a linha do benefício municipal e das deduções, como na DANFSe oficial, só quando a nota os informa.
        /// </summary>
        public bool ExibirBeneficioMunicipal { get; set; }

        #endregion

        #region Tributação federal (exceto CBS)

        public double? Irrf { get; set; }
        public double? ContribuicaoPrevidenciariaRetida { get; set; }
        public double? ContribuicoesSociaisRetidas { get; set; }
        public double? Pis { get; set; }
        public double? Cofins { get; set; }
        public String DescricaoContribuicoesSociaisRetidas { get; set; }

        /// <summary>
        /// Exibe a linha de PIS/COFINS de apuração própria e da descrição das contribuições retidas. Pela NT 008
        /// (nota 6), ela é impressa só nas NFS-e com competência até o fim de 2026.
        /// </summary>
        public bool ExibirPisCofins { get; set; } = true;

        #endregion

        #region IBS e CBS

        public String CstClassificacaoTributaria { get; set; }

        /// <summary>
        /// Indicador da operação, código IBGE, município e UF de incidência, ex.: 020201 / 3306305 / Volta Redonda / RJ.
        /// </summary>
        public String IndicadorOperacaoIncidencia { get; set; }

        public double? ExclusoesReducoesBaseCalculo { get; set; }
        public double? BaseCalculoIbsCbs { get; set; }

        public double? ReducaoAliquotaIbsUf { get; set; }
        public double? ReducaoAliquotaIbsMunicipio { get; set; }
        public double? ReducaoAliquotaCbs { get; set; }

        public double? AliquotaIbsUf { get; set; }
        public double? AliquotaIbsMunicipio { get; set; }
        public double? AliquotaEfetivaIbsUf { get; set; }
        public double? AliquotaEfetivaIbsMunicipio { get; set; }
        public double? ValorIbsUf { get; set; }
        public double? ValorIbsMunicipio { get; set; }
        public double? ValorIbs { get; set; }

        public double? AliquotaCbs { get; set; }
        public double? AliquotaEfetivaCbs { get; set; }
        public double? ValorCbs { get; set; }

        #endregion

        #region Valor total

        public double? ValorServico { get; set; }
        public double? DescontoIncondicionado { get; set; }
        public double? DescontoCondicionado { get; set; }
        public double? TotalRetencoes { get; set; }
        public double? ValorLiquido { get; set; }
        public double? TotalIbsCbs { get; set; }

        /// <summary>
        /// Valor líquido com o IBS e a CBS, quando cobrados (vTotNF).
        /// </summary>
        public double? ValorLiquidoComIbsCbs { get; set; }

        #endregion

        public String InformacoesComplementares { get; set; }
    }
}
