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
                ExibirMunicipioEmissor = !(Vazio(dps.serv?.cServ?.cTribNac)?.StartsWith("99") ?? false),
            };

            PreencherServico(m, inf, dps);
            PreencherTributacao(m, inf, dps);
            PreencherIbsCbs(m, inf.IBSCBS, ibsCbsDps);
            PreencherTotais(m, inf, dps);
            m.InformacoesComplementares = InformacoesComplementares(inf, dps);

            return m;
        }

        #region Participantes

        private static ParticipanteNfseViewModel Prestador(EmitenteNFSe emit, PrestadorDps prest)
        {
            if (emit == null && prest == null) return null;

            var p = Pessoa(prest) ?? new ParticipanteNfseViewModel();

            // A DPS normalmente traz só o documento do prestador; nome e endereço vêm do emitente da NFS-e.
            // Telefone e e-mail vêm só da DPS (prest/fone e prest/email, NT 008), como na DANFSe do site.
            if (emit != null)
            {
                if (String.IsNullOrWhiteSpace(p.Documento)) p.Documento = Documento(emit.CNPJ, emit.CPF, null);
                p.InscricaoMunicipal = p.InscricaoMunicipal ?? emit.IM;
                p.Nome = p.Nome ?? emit.xNome;

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
            m.DescricaoTributacaoNacional = Espacos(Vazio(inf.xTribMun) ?? inf.xTribNac);
            m.DescricaoServico = Espacos(c?.xDescServ);

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

            m.ExibirTributacaoMunicipal = mun != null && Vazio(mun.tribISSQN) != "4";

            var regEsp = Vazio(dps.prest?.regTrib?.regEspTrib);
            m.RegimeEspecialTributacao = regEsp == null || regEsp == "0" ? null : Descricao(regEsp, RegimesEspeciaisTributacao);
            m.TipoImunidade = mun?.tpImunidade == null ? null : Descricao(mun.tpImunidade, TiposImunidade);
            m.SuspensaoExigibilidade = mun?.exigSusp?.tpSusp == null ? null : Descricao(mun.exigSusp.tpSusp, TiposSuspensao);
            m.NumeroProcessoSuspensao = Vazio(mun?.exigSusp?.nProcesso);
            m.ExibirRegimeEspecial = m.RegimeEspecialTributacao != null || m.TipoImunidade != null || m.SuspensaoExigibilidade != null || m.NumeroProcessoSuspensao != null;

            m.TipoTributacaoIssqn = mun?.tribISSQN == null ? null : Descricao(mun.tribISSQN, TiposTributacaoIssqn);
            m.RetencaoIssqn = mun?.tpRetISSQN == null ? null : Descricao(mun.tpRetISSQN, TiposRetencaoIssqn);

            if (!String.IsNullOrWhiteSpace(inf.cLocIncid))
                m.MunicipioIncidenciaIssqn = $"{inf.xLocIncid ?? Municipios.Nome(inf.cLocIncid) ?? inf.cLocIncid} / {Municipios.Uf(inf.cLocIncid) ?? "-"} / -";
            else if (!String.IsNullOrWhiteSpace(mun?.cPaisResult))
                m.MunicipioIncidenciaIssqn = $"- / - / {mun.cPaisResult}";

            m.BaseCalculoIssqn = valores?.vBC;
            m.AliquotaIssqn = valores?.pAliqAplic ?? mun?.pAliq;
            m.ValorIssqn = valores?.vISSQN;

            m.BeneficioMunicipal = BeneficioMunicipal(valores?.tpBM, mun?.BM);
            m.CalculoBeneficioMunicipal = valores?.vCalcBM ?? mun?.BM?.vRedBCBM;

            // Total deduções/reduções: vDR ou vCalcDR, mais o valor de reembolso/repasse/ressarcimento (NT 008, item 2.4.5).
            var deducoes = dps.valores?.vDedRed?.vDR ?? valores?.vCalcDR;
            var reembolso = inf.IBSCBS?.valores?.vCalcReeRepRes;
            if (deducoes.HasValue || (reembolso ?? 0) > 0)
                m.TotalDeducoesReducoes = Math.Round((deducoes ?? 0) + (reembolso ?? 0), 2);

            m.ExibirBeneficioMunicipal = valores?.tpBM != null || mun?.BM != null || m.CalculoBeneficioMunicipal.HasValue
                || m.TotalDeducoesReducoes.HasValue || dps.valores?.vDescCondIncond?.vDescIncond != null;

            var fed = trib?.tribFed;
            var piscofins = fed?.piscofins;
            var tpRet = Vazio(piscofins?.tpRetPisCofins);
            m.Irrf = fed?.vRetIRRF;
            m.ContribuicaoPrevidenciariaRetida = fed?.vRetCP;

            if (tpRet == "1")
            {
                // PIS/COFINS Retido (código antigo): as contribuições retidas somam vRetCSLL, vPis e vCofins,
                // e o PIS e a COFINS de apuração própria saem zerados (NT 008, item 2.4.5).
                m.ContribuicoesSociaisRetidas = Soma(fed?.vRetCSLL, piscofins?.vPis, piscofins?.vCofins);
                m.Pis = 0;
                m.Cofins = 0;
            }
            else
            {
                m.ContribuicoesSociaisRetidas = fed?.vRetCSLL;
                m.Pis = piscofins?.vPis;
                m.Cofins = piscofins?.vCofins;
            }

            if (tpRet != null)
                m.DescricaoContribuicoesSociaisRetidas = TiposRetencaoPisCofins.TryGetValue(tpRet, out String d) ? $"{tpRet} - {d}" : tpRet;

            // Linha impressa só para competências até o fim de 2026 (NT 008, nota 6).
            m.ExibirPisCofins = !m.Competencia.HasValue || m.Competencia.Value.Year <= 2026;
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

            // Exclusões e reduções da base do IBS/CBS: somatório de desconto incondicionado, reembolso/repasse/ressarcimento,
            // ISSQN, PIS e COFINS (NT 008, item 2.4.5).
            if (inf.IBSCBS != null)
            {
                var piscofins = valores?.trib?.tribFed?.piscofins;
                m.ExclusoesReducoesBaseCalculo = Soma(m.DescontoIncondicionado, inf.IBSCBS.valores?.vCalcReeRepRes, inf.valores?.vISSQN,
                    piscofins?.vPis, piscofins?.vCofins) ?? 0;
            }
        }

        private static double? Soma(params double?[] valores)
        {
            return valores.Any(v => v.HasValue) ? Math.Round(valores.Sum(v => v ?? 0), 2) : (double?)null;
        }

        private static String BeneficioMunicipal(String tpBM, BeneficioMunicipalDps bm)
        {
            switch (Vazio(tpBM))
            {
                case null: return null;
                case "1": return "Isenção";
                case "2": return bm?.pRedBCBM.HasValue == true ? $"Redução da BC em {Percentual(bm.pRedBCBM)}" : "Redução da BC";
                case "3": return bm?.vRedBCBM.HasValue == true ? $"Redução da BC em {Moeda(bm.vRedBCBM)}" : "Redução da BC";
                case "4": return "Alíquota diferenciada";
                default: return tpBM.Trim();
            }
        }

        /// <summary>
        /// Informações complementares na ordem e com os rótulos da NT 008 (item 2.4.5), separadas por " | ".
        /// A linha dos totais aproximados dos tributos (Lei 12.741/2012) é fixa e fica por último.
        /// </summary>
        private static String InformacoesComplementares(InfNFSe inf, InfDps dps)
        {
            var serv = dps.serv;
            var compl = serv?.infoCompl;
            var imoveis = dps.IBSCBS?.imovel?.gUnidImob?.Select(u => Vazio(u.inscImobFisc)).Where(i => i != null);
            var inscImob = Vazio(serv?.obra?.inscImobFisc) ?? (imoveis != null && imoveis.Any() ? String.Join(", ", imoveis) : null);
            var itensPedido = compl?.gItemPed?.xItemPed?.Select(Vazio).Where(i => i != null).ToList();

            var partes = new List<String>();
            void Adicionar(String rotulo, String valor)
            {
                if (!String.IsNullOrWhiteSpace(valor)) partes.Add($"{rotulo} {valor.Trim()}");
            }

            Adicionar("Inf. Cont.:", Espacos(compl?.xInfComp));
            Adicionar("NFS-e Subst.:", dps.subst?.chSubstda);
            Adicionar("Doc. Ref.:", compl?.docRef);
            Adicionar("Cod. Obra:", serv?.obra?.cObra);
            Adicionar("Insc. Imob.:", inscImob);
            Adicionar("Cod. Evt.:", serv?.atvEvento?.idAtvEvt);
            Adicionar("Doc. Tec.:", compl?.idDocTec);
            Adicionar("Núm. Ped.:", compl?.xPed);
            Adicionar("Item Ped.:", itensPedido == null || itensPedido.Count == 0 ? null : String.Join(", ", itensPedido));
            Adicionar("Inf. A. T. Mun.:", inf.xOutInf);

            var linhas = new List<String>();
            if (partes.Count > 0)
                linhas.Add(Limitar(String.Join(" | ", partes), 1997));

            var tot = dps.valores?.trib?.totTrib;
            if (tot?.vTotTrib != null)
                linhas.Add($"Totais aproximados dos Tributos cfe. Lei n° 12.741/2012: Federais: {Moeda(tot.vTotTrib.vTotTribFed)}; Estaduais: {Moeda(tot.vTotTrib.vTotTribEst)}; Municipais: {Moeda(tot.vTotTrib.vTotTribMun)};");
            else if (tot?.pTotTrib != null)
                linhas.Add($"Totais aproximados dos Tributos cfe. Lei n° 12.741/2012: Federais: {Percentual(tot.pTotTrib.pTotTribFed)}; Estaduais: {Percentual(tot.pTotTrib.pTotTribEst)}; Municipais: {Percentual(tot.pTotTrib.pTotTribMun)};");

            if (tot?.pTotTribSN != null)
                linhas.Add($"Percentual aproximado dos tributos do Simples Nacional: {Percentual(tot.pTotTribSN)}");

            return linhas.Count == 0 ? null : String.Join("\n", linhas);
        }

        /// <summary>
        /// Limita o texto ao número de caracteres sugerido pela NT 008, terminando com reticências.
        /// </summary>
        internal static String Limitar(String texto, int maximo)
        {
            if (texto == null || texto.Length <= maximo) return texto;
            return texto.Substring(0, maximo).TrimEnd() + "...";
        }

        #endregion

        #region Tabelas de códigos

        // Domínios do leiaute da NFS-e Nacional (Anexo VI, v1.04.01) e descrições da NT 008 (DANFSe).
        // Quando o texto da DANFSe do site da NFS-e difere do leiaute, vale o do site (ex.: "Não optante").

        private static readonly Dictionary<String, String> TiposEmitente = new Dictionary<String, String>
        {
            { "1", "Prestador" }, { "2", "Tomador" }, { "3", "Intermediário" },
        };

        private static readonly Dictionary<String, String> Situacoes = new Dictionary<String, String>
        {
            { "100", "NFS-e Gerada" },
            { "102", "NFS-e de Decisão Judicial ou Administrativa" },
            { "103", "NFS-e Avulsa" },
            { "107", "NFS-e MEI" },
        };

        private static readonly Dictionary<String, String> Finalidades = new Dictionary<String, String>
        {
            { "0", "NFS-e regular" }, { "1", "NFS-e de crédito" }, { "2", "NFS-e de débito" },
        };

        private static readonly Dictionary<String, String> OpcoesSimplesNacional = new Dictionary<String, String>
        {
            { "1", "Não optante" },
            { "2", "Optante - Microempreendedor Individual (MEI)" },
            { "3", "Optante - Microempresa ou Empresa de Pequeno Porte (ME/EPP)" },
            { "4", "Optante Pendente" },
        };

        private static readonly Dictionary<String, String> RegimesApuracaoSimplesNacional = new Dictionary<String, String>
        {
            { "1", "Regime de apuração dos tributos federais e municipal pelo SN" },
            { "2", "Regime de apuração dos tributos federais pelo SN e o ISSQN pela NFS-e conforme respectiva legislação municipal do tributo" },
            { "3", "Regime de apuração dos tributos federais e municipal pela NFS-e conforme respectivas legislações federal e municipal de cada tributo" },
        };

        private static readonly Dictionary<String, String> RegimesEspeciaisTributacao = new Dictionary<String, String>
        {
            { "0", "Nenhum" },
            { "1", "Ato Cooperado (Cooperativa)" },
            { "2", "Estimativa" },
            { "3", "Microempresa Municipal" },
            { "4", "Notário ou Registrador" },
            { "5", "Profissional Autônomo" },
            { "6", "Sociedade de Profissionais" },
            { "9", "Outros" },
        };

        private static readonly Dictionary<String, String> TiposTributacaoIssqn = new Dictionary<String, String>
        {
            { "1", "Operação Tributável" }, { "2", "Imunidade" }, { "3", "Exportação de Serviço" }, { "4", "Não Incidência" },
        };

        private static readonly Dictionary<String, String> TiposImunidade = new Dictionary<String, String>
        {
            { "0", "Imunidade (tipo não informado na nota de origem)" },
            { "1", "Patrimônio, renda ou serviços, uns dos outros (CF88, Art 150, VI, a)" },
            { "2", "Entidades religiosas e templos de qualquer culto, inclusive suas organizações assistenciais e beneficentes (CF88, Art 150, VI, b)" },
            { "3", "Patrimônio, renda ou serviços dos partidos políticos, inclusive suas fundações, das entidades sindicais dos trabalhadores, das instituições de educação e de assistência social, sem fins lucrativos, atendidos os requisitos da lei (CF88, Art 150, VI, c)" },
            { "4", "Livros, jornais, periódicos e o papel destinado a sua impressão (CF88, Art 150, VI, d)" },
            { "5", "Fonogramas e videofonogramas musicais produzidos no Brasil contendo obras musicais ou literomusicais de autores brasileiros e/ou obras em geral interpretadas por artistas brasileiros bem como os suportes materiais ou arquivos digitais que os contenham, salvo na etapa de replicação industrial de mídias ópticas de leitura a laser (CF88, Art 150, VI, e)" },
        };

        private static readonly Dictionary<String, String> TiposSuspensao = new Dictionary<String, String>
        {
            { "1", "Exigibilidade Suspensa por Decisão Judicial" },
            { "2", "Exigibilidade Suspensa por Processo Administrativo" },
        };

        private static readonly Dictionary<String, String> TiposRetencaoIssqn = new Dictionary<String, String>
        {
            { "1", "Não Retido" }, { "2", "Retido pelo Tomador" }, { "3", "Retido pelo Intermediário" },
        };

        // Os códigos 1 e 2 são do leiaute anterior e deixam de ser aceitos quando os grupos IBSCBS se tornam obrigatórios.
        private static readonly Dictionary<String, String> TiposRetencaoPisCofins = new Dictionary<String, String>
        {
            { "0", "PIS/COFINS/CSLL Não Retidos" },
            { "1", "PIS/COFINS Retido" },
            { "2", "PIS/COFINS Não Retido" },
            { "3", "PIS/COFINS/CSLL Retidos" },
            { "4", "PIS/COFINS Retidos, CSLL Não Retido" },
            { "5", "PIS Retido, COFINS/CSLL Não Retido" },
            { "6", "COFINS Retido, PIS/CSLL Não Retido" },
            { "7", "PIS Não Retido, COFINS/CSLL Retidos" },
            { "8", "PIS/COFINS Não Retidos, CSLL Retido" },
            { "9", "COFINS Não Retido, PIS/CSLL Retidos" },
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

        /// <summary>
        /// Junta espaços repetidos em um só, como a DANFSe do site faz nos textos livres.
        /// </summary>
        private static String Espacos(String s) => Vazio(s) == null ? null : System.Text.RegularExpressions.Regex.Replace(s.Trim(), @"\s+", " ");

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
