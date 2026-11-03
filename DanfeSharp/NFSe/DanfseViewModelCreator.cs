using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace DanfeSharp.NFSe
{
    /// <summary>
    /// Cria o modelo da DANFSe a partir do XML da NFS-e do padrão nacional (Sistema Nacional NFS-e).
    /// </summary>
    public static class DanfseViewModelCreator
    {
        /// <summary>
        /// Endereço de consulta pública da NFS-e codificado no QR Code da DANFSe.
        /// </summary>
        public const String UrlConsultaPublica = "https://www.nfse.gov.br/ConsultaPublica?tpc=1&chave=";

        public static DanfseViewModel CriarDeArquivoXml(String caminho)
        {
            using (var sr = new StreamReader(caminho, true))
            {
                return CriarDeStringXml(sr.ReadToEnd());
            }
        }

        public static DanfseViewModel CriarDeArquivoXml(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            using (var sr = new StreamReader(stream, true))
            {
                return CriarDeStringXml(sr.ReadToEnd());
            }
        }

        public static DanfseViewModel CriarDeStringXml(String xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));

            try
            {
                using (var reader = new StringReader(xml))
                {
                    return CreateFromXml((NotaFiscalServico)new XmlSerializer(typeof(NotaFiscalServico)).Deserialize(reader));
                }
            }
            catch (InvalidOperationException e)
            {
                if (e.InnerException is XmlException ex)
                    throw new Exception(String.Format("Não foi possível interpretar o Xml. Linha {0} Posição {1}.", ex.LineNumber, ex.LinePosition), e);

                throw new XmlException("O Xml não parece ser uma NFS-e do padrão nacional (NFSe).", e);
            }
        }

        public static DanfseViewModel CreateFromXml(NotaFiscalServico nfse)
        {
            var inf = nfse?.infNFSe ?? throw new ArgumentException("A NFS-e não possui o grupo infNFSe.", nameof(nfse));
            var dps = inf.DPS?.infDPS ?? throw new ArgumentException("A NFS-e não possui a DPS (infDPS).", nameof(nfse));

            var chave = inf.Id != null && inf.Id.StartsWith("NFS") ? inf.Id.Substring(3) : inf.Id;
            var ibsCbsDps = dps.IBSCBS;

            var m = new DanfseViewModel
            {
                ChaveAcesso = chave,
                Numero = inf.nNFSe,
                Competencia = Data(dps.dCompet),
                DataHoraEmissao = inf.dhProc?.DateTimeOffsetValue.DateTime,
                NumeroDps = dps.nDPS,
                SerieDps = dps.serie?.Trim(),
                DataHoraEmissaoDps = dps.dhEmi?.DateTimeOffsetValue.DateTime,
                Emitente = Descricao(dps.tpEmit.ToString(), TiposEmitente),
                Situacao = Descricao(inf.cStat.ToString(), Situacoes),
                Finalidade = ibsCbsDps?.finNFSe == null ? null : Descricao(ibsCbsDps.finNFSe, Finalidades),
                MunicipioEmissor = Juntar(" - ", inf.xLocEmi ?? Municipios.Nome(dps.cLocEmi), Municipios.Uf(dps.cLocEmi)),
                AmbienteGerador = inf.ambGer,
                TipoAmbiente = dps.tpAmb,
                QrCode = String.IsNullOrWhiteSpace(chave) ? null : UrlConsultaPublica + chave,
                Prestador = Prestador(inf.emit, dps.prest),
                Tomador = Pessoa(dps.toma),
                Intermediario = Pessoa(dps.interm),
                Destinatario = Pessoa(ibsCbsDps?.dest),
            };

            PreencherServico(m, inf, dps);
            PreencherTributacao(m, inf, dps);
            PreencherIbsCbs(m, inf.IBSCBS, ibsCbsDps);
            PreencherTotais(m, inf, dps);
            m.InformacoesComplementares = InformacoesComplementares(dps);

            return m;
        }

        #region Participantes

        private static ParticipanteNfseViewModel Prestador(EmitenteNFSe emit, PrestadorDps prest)
        {
            if (emit == null && prest == null) return null;

            var p = Pessoa(prest) ?? new ParticipanteNfseViewModel();

            // A DPS normalmente traz só o documento do prestador; nome e endereço vêm do emitente da NFS-e.
            if (emit != null)
            {
                if (String.IsNullOrWhiteSpace(p.Documento)) p.Documento = Documento(emit.CNPJ, emit.CPF, null);
                p.InscricaoMunicipal = p.InscricaoMunicipal ?? emit.IM;
                p.Telefone = p.Telefone ?? Formatador.FormatarTelefone(emit.fone);
                p.Nome = p.Nome ?? emit.xNome;
                p.Email = p.Email ?? emit.email;

                var e = emit.enderNac;
                if (e != null && prest?.end == null)
                {
                    p.Endereco = Juntar(", ", e.xLgr, e.nro, e.xCpl, e.xBairro);
                    p.Municipio = Juntar(" / ", Municipios.Nome(e.cMun), e.UF ?? Municipios.Uf(e.cMun));
                    p.CodigoIbgeCep = $"{CodigoIbge(e.cMun) ?? "-"} / {Cep(e.CEP) ?? "-"}";
                }
            }

            var reg = prest?.regTrib;
            p.SimplesNacional = reg?.opSimpNac == null ? null : Descricao(reg.opSimpNac, OpcoesSimplesNacional);
            p.RegimeApuracaoSimplesNacional = reg?.regApTribSN == null ? null : Descricao(reg.regApTribSN, RegimesApuracaoSimplesNacional);

            return p;
        }

        private static ParticipanteNfseViewModel Pessoa(PessoaDps pessoa)
        {
            if (pessoa == null) return null;

            var p = new ParticipanteNfseViewModel
            {
                Documento = Documento(pessoa.CNPJ, pessoa.CPF, pessoa.NIF),
                InscricaoMunicipal = pessoa.IM,
                Telefone = Formatador.FormatarTelefone(pessoa.fone),
                Nome = pessoa.xNome,
                Email = pessoa.email,
            };

            var end = pessoa.end;
            if (end != null)
            {
                p.Endereco = Juntar(", ", end.xLgr, end.nro, end.xCpl, end.xBairro);

                if (end.endNac != null)
                {
                    p.Municipio = Juntar(" / ", Municipios.Nome(end.endNac.cMun), Municipios.Uf(end.endNac.cMun));
                    p.CodigoIbgeCep = $"{CodigoIbge(end.endNac.cMun) ?? "-"} / {Cep(end.endNac.CEP) ?? "-"}";
                }
                else if (end.endExt != null)
                {
                    p.Municipio = Juntar(" / ", end.endExt.xCidade, end.endExt.xEstProvReg, end.endExt.cPais);
                    p.CodigoIbgeCep = $"- / {end.endExt.cEndPost ?? "-"}";
                }
            }

            return p;
        }

        private static String Documento(String cnpj, String cpf, String nif)
        {
            if (!String.IsNullOrWhiteSpace(cnpj)) return Formatador.FormatarCnpj(cnpj.Trim());
            if (!String.IsNullOrWhiteSpace(cpf)) return Formatador.FormatarCpf(cpf.Trim());
            return String.IsNullOrWhiteSpace(nif) ? null : nif.Trim();
        }

        #endregion

        #region Serviço, tributação e totais

        private static void PreencherServico(DanfseViewModel m, InfNFSe inf, InfDps dps)
        {
            var serv = dps.serv;
            var c = serv?.cServ;

            m.CodigoTributacao = $"{CodigoTributacaoNacional(c?.cTribNac) ?? "-"} / {Vazio(c?.cTribMun) ?? "-"}";
            m.CodigoNbs = CodigoNbs(c?.cNBS);
            m.DescricaoTributacaoNacional = inf.xTribNac;
            m.DescricaoServico = c?.xDescServ;

            var local = serv?.locPrest;
            if (!String.IsNullOrWhiteSpace(local?.cLocPrestacao))
                m.LocalPrestacao = $"{inf.xLocPrestacao ?? Municipios.Nome(local.cLocPrestacao) ?? local.cLocPrestacao} / {Municipios.Uf(local.cLocPrestacao) ?? "-"} / -";
            else if (!String.IsNullOrWhiteSpace(local?.cPaisPrestacao))
                m.LocalPrestacao = $"- / - / {local.cPaisPrestacao}";
        }

        private static void PreencherTributacao(DanfseViewModel m, InfNFSe inf, InfDps dps)
        {
            var trib = dps.valores?.trib;
            var mun = trib?.tribMun;
            var valores = inf.valores;

            m.TipoTributacaoIssqn = mun?.tribISSQN == null ? null : Descricao(mun.tribISSQN, TiposTributacaoIssqn);
            m.RetencaoIssqn = mun?.tpRetISSQN == null ? null : Descricao(mun.tpRetISSQN, TiposRetencaoIssqn);

            if (!String.IsNullOrWhiteSpace(inf.cLocIncid))
                m.MunicipioIncidenciaIssqn = $"{inf.xLocIncid ?? Municipios.Nome(inf.cLocIncid) ?? inf.cLocIncid} / {Municipios.Uf(inf.cLocIncid) ?? "-"} / -";
            else if (!String.IsNullOrWhiteSpace(mun?.cPaisResult))
                m.MunicipioIncidenciaIssqn = $"- / - / {mun.cPaisResult}";

            m.BaseCalculoIssqn = valores?.vBC;
            m.AliquotaIssqn = valores?.pAliqAplic ?? mun?.pAliq;
            m.ValorIssqn = valores?.vISSQN;

            m.BeneficioMunicipal = valores?.tpBM == null ? null : Descricao(valores.tpBM, TiposBeneficioMunicipal);
            m.CalculoBeneficioMunicipal = valores?.vCalcBM;
            m.TotalDeducoesReducoes = valores?.vCalcDR ?? dps.valores?.vDedRed?.vDR;
            m.ExibirBeneficioMunicipal = valores?.tpBM != null || mun?.BM != null || m.TotalDeducoesReducoes.HasValue;

            var fed = trib?.tribFed;
            m.Irrf = fed?.vRetIRRF;
            m.ContribuicaoPrevidenciariaRetida = fed?.vRetCP;
            m.ContribuicoesSociaisRetidas = fed?.vRetCSLL;
            m.Pis = fed?.piscofins?.vPis;
            m.Cofins = fed?.piscofins?.vCofins;

            var tpRet = fed?.piscofins?.tpRetPisCofins;
            if (tpRet != null)
                m.DescricaoContribuicoesSociaisRetidas = TiposRetencaoPisCofins.TryGetValue(tpRet, out String d) ? $"{tpRet} - {d}" : tpRet;
        }

        private static void PreencherIbsCbs(DanfseViewModel m, IbsCbsNFSe ib, IbsCbsDps ibDps)
        {
            var g = ibDps?.valores?.trib?.gIBSCBS;
            if (g != null) m.CstClassificacaoTributaria = $"{g.CST ?? "-"} / {g.cClassTrib ?? "-"}";

            if (ibDps != null || ib != null)
            {
                var cLoc = ib?.cLocalidadeIncid;
                m.IndicadorOperacaoIncidencia = $"{Vazio(ibDps?.cIndOp) ?? "-"} / {Vazio(cLoc) ?? "-"} / {ib?.xLocalidadeIncid ?? Municipios.Nome(cLoc) ?? "-"} / {Municipios.Uf(cLoc) ?? "-"}";
            }

            if (ib == null) return;

            var v = ib.valores;
            m.BaseCalculoIbsCbs = v?.vBC;
            m.ReducaoAliquotaIbsUf = v?.uf?.pRedAliqUF;
            m.ReducaoAliquotaIbsMunicipio = v?.mun?.pRedAliqMun;
            m.ReducaoAliquotaCbs = v?.fed?.pRedAliqCBS;
            m.AliquotaIbsUf = v?.uf?.pIBSUF;
            m.AliquotaIbsMunicipio = v?.mun?.pIBSMun;
            m.AliquotaEfetivaIbsUf = v?.uf?.pAliqEfetUF;
            m.AliquotaEfetivaIbsMunicipio = v?.mun?.pAliqEfetMun;
            m.AliquotaCbs = v?.fed?.pCBS;
            m.AliquotaEfetivaCbs = v?.fed?.pAliqEfetCBS;

            var t = ib.totCIBS;
            m.ValorIbsUf = t?.gIBS?.gIBSUFTot?.vIBSUF;
            m.ValorIbsMunicipio = t?.gIBS?.gIBSMunTot?.vIBSMun;
            m.ValorIbs = t?.gIBS?.vIBSTot;
            m.ValorCbs = t?.gCBS?.vCBS;
            m.ValorLiquidoComIbsCbs = t?.vTotNF;

            if (m.ValorIbs.HasValue || m.ValorCbs.HasValue)
                m.TotalIbsCbs = Math.Round((m.ValorIbs ?? 0) + (m.ValorCbs ?? 0), 2);
        }

        private static void PreencherTotais(DanfseViewModel m, InfNFSe inf, InfDps dps)
        {
            var valores = dps.valores;
            m.ValorServico = valores?.vServPrest?.vServ;
            m.DescontoIncondicionado = valores?.vDescCondIncond?.vDescIncond;
            m.DescontoCondicionado = valores?.vDescCondIncond?.vDescCond;
            m.TotalRetencoes = inf.valores?.vTotalRet;
            m.ValorLiquido = inf.valores?.vLiq;

            // Exclusões e reduções: diferença entre o valor do serviço e a base do IBS e da CBS.
            if (m.ValorServico.HasValue && m.BaseCalculoIbsCbs.HasValue)
                m.ExclusoesReducoesBaseCalculo = Math.Max(0, Math.Round(m.ValorServico.Value - m.BaseCalculoIbsCbs.Value, 2));
        }

        private static String InformacoesComplementares(InfDps dps)
        {
            var linhas = new List<String>();

            if (!String.IsNullOrWhiteSpace(dps.subst?.chSubstda))
                linhas.Add($"NFS-e substituída: {dps.subst.chSubstda}" + (String.IsNullOrWhiteSpace(dps.subst.xMotivo) ? "" : $" - {dps.subst.xMotivo}"));

            if (!String.IsNullOrWhiteSpace(dps.serv?.infoCompl?.xInfComp))
                linhas.Add(dps.serv.infoCompl.xInfComp.Trim());

            var tot = dps.valores?.trib?.totTrib;
            if (tot?.vTotTrib != null)
                linhas.Add($"Totais aproximados dos Tributos cfe. Lei n° 12.741/2012: Federais: {Moeda(tot.vTotTrib.vTotTribFed)}; Estaduais: {Moeda(tot.vTotTrib.vTotTribEst)}; Municipais: {Moeda(tot.vTotTrib.vTotTribMun)};");
            else if (tot?.pTotTrib != null)
                linhas.Add($"Percentuais aproximados dos Tributos cfe. Lei n° 12.741/2012: Federais: {Percentual(tot.pTotTrib.pTotTribFed)}; Estaduais: {Percentual(tot.pTotTrib.pTotTribEst)}; Municipais: {Percentual(tot.pTotTrib.pTotTribMun)};");

            if (tot?.pTotTribSN != null)
                linhas.Add($"Percentual aproximado dos tributos do Simples Nacional: {Percentual(tot.pTotTribSN)}");

            return linhas.Count == 0 ? null : String.Join("\n", linhas);
        }

        #endregion

        #region Tabelas de códigos

        private static readonly Dictionary<String, String> TiposEmitente = new Dictionary<String, String>
        {
            { "1", "Prestador" }, { "2", "Tomador" }, { "3", "Intermediário" },
        };

        private static readonly Dictionary<String, String> Situacoes = new Dictionary<String, String>
        {
            { "100", "NFS-e Gerada" }, { "101", "NFS-e de Substituição Gerada" }, { "102", "NFS-e de Decisão Judicial" }, { "103", "NFS-e Avulsa" },
        };

        private static readonly Dictionary<String, String> Finalidades = new Dictionary<String, String>
        {
            { "0", "NFS-e regular" },
        };

        private static readonly Dictionary<String, String> OpcoesSimplesNacional = new Dictionary<String, String>
        {
            { "1", "Não optante" },
            { "2", "Optante - Microempreendedor Individual (MEI)" },
            { "3", "Optante - Microempresa ou Empresa de Pequeno Porte (ME/EPP)" },
        };

        private static readonly Dictionary<String, String> RegimesApuracaoSimplesNacional = new Dictionary<String, String>
        {
            { "1", "Tributos federais e municipal pelo SN" },
            { "2", "Tributos federais pelo SN e ISSQN pela NFS-e" },
            { "3", "Tributos federais e municipal pela NFS-e" },
        };

        private static readonly Dictionary<String, String> TiposTributacaoIssqn = new Dictionary<String, String>
        {
            { "1", "Operação Tributável" }, { "2", "Imunidade" }, { "3", "Exportação de Serviço" }, { "4", "Não Incidência" },
        };

        private static readonly Dictionary<String, String> TiposRetencaoIssqn = new Dictionary<String, String>
        {
            { "1", "Não Retido" }, { "2", "Retido pelo Tomador" }, { "3", "Retido pelo Intermediário" },
        };

        // Apenas os códigos conferidos em DANFSe oficiais têm descrição; os demais saem só com o código.
        private static readonly Dictionary<String, String> TiposRetencaoPisCofins = new Dictionary<String, String>
        {
            { "0", "PIS/COFINS/CSLL Não Retidos" },
            { "3", "PIS/COFINS/CSLL Retidos" },
        };

        // Apenas o código 4 foi conferido em uma DANFSe oficial; os demais saem só com o código.
        private static readonly Dictionary<String, String> TiposBeneficioMunicipal = new Dictionary<String, String>
        {
            { "4", "Alíquota diferenciada" },
        };

        private static String Descricao(String codigo, Dictionary<String, String> tabela)
        {
            if (String.IsNullOrWhiteSpace(codigo)) return null;
            return tabela.TryGetValue(codigo.Trim(), out String d) ? d : codigo.Trim();
        }

        #endregion

        #region Formatação

        internal static String Moeda(double? valor) => valor.HasValue ? "R$ " + valor.Value.ToString("#,0.00", Formatador.Cultura) : "-";

        internal static String Percentual(double? valor) => valor.HasValue ? valor.Value.ToString("0.00", Formatador.Cultura) + " %" : "-";

        private static DateTime? Data(String data)
        {
            return DateTime.TryParseExact(data, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d) ? d : (DateTime?)null;
        }

        private static String Vazio(String s) => String.IsNullOrWhiteSpace(s) ? null : s.Trim();

        private static String Juntar(String separador, params String[] partes)
        {
            var validas = partes.Where(p => !String.IsNullOrWhiteSpace(p)).Select(p => p.Trim()).ToArray();
            return validas.Length == 0 ? null : String.Join(separador, validas);
        }

        /// <summary>
        /// Código do município como na DANFSe oficial: 3304557 → 33.04557.
        /// </summary>
        private static String CodigoIbge(String c)
        {
            c = Vazio(c);
            return c != null && c.Length == 7 ? $"{c.Substring(0, 2)}.{c.Substring(2)}" : c;
        }

        /// <summary>
        /// CEP como na DANFSe oficial: 20080002 → 20.080-002.
        /// </summary>
        private static String Cep(String c)
        {
            c = Vazio(c);
            return c != null && c.Length == 8 ? $"{c.Substring(0, 2)}.{c.Substring(2, 3)}-{c.Substring(5)}" : c;
        }

        /// <summary>
        /// Código de tributação nacional: 030403 → 03.04.03.
        /// </summary>
        private static String CodigoTributacaoNacional(String c)
        {
            c = Vazio(c);
            return c != null && c.Length == 6 ? $"{c.Substring(0, 2)}.{c.Substring(2, 2)}.{c.Substring(4)}" : c;
        }

        /// <summary>
        /// Código da NBS: 110011210 → 1.1001.12.10.
        /// </summary>
        private static String CodigoNbs(String c)
        {
            c = Vazio(c);
            return c != null && c.Length == 9 ? $"{c.Substring(0, 1)}.{c.Substring(1, 4)}.{c.Substring(5, 2)}.{c.Substring(7)}" : c;
        }

        #endregion
    }
}
