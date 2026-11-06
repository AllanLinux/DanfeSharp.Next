using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using DanfeSharp.Esquemas.NFe;

namespace DanfeSharp.Modelo
{
    public static class DanfeViewModelCreator
    {
        public readonly static IEnumerable<FormaEmissao> FormasEmissaoSuportadas = new FormaEmissao[]{ FormaEmissao.Normal, FormaEmissao.ContingenciaSVCAN, FormaEmissao.ContingenciaSVCRS };

        internal static EmpresaViewModel CreateEmpresaFrom(Empresa empresa)
        {
            EmpresaViewModel model = new EmpresaViewModel();

            model.RazaoSocial = empresa.xNome;
            model.CnpjCpf = !String.IsNullOrWhiteSpace(empresa.CNPJ) ? empresa.CNPJ : empresa.CPF;
            model.Ie = empresa.IE;
            model.IeSt = empresa.IEST;
            model.Email = empresa.email;

            var end = empresa.Endereco;

            if (end != null)
            {
                model.EnderecoLogadrouro = end.xLgr;
                model.EnderecoNumero = end.nro;
                model.EnderecoBairro = end.xBairro;
                model.Municipio = end.xMun;
                model.EnderecoUf = end.UF;
                model.EnderecoCep = end.CEP;
                model.Telefone = end.fone;
                model.EnderecoComplemento = end.xCpl;
            }

            if (empresa is Emitente)
            {
                var emit = empresa as Emitente;
                model.IM = emit.IM;
                model.CRT = emit.CRT;
                model.NomeFantasia = emit.xFant;
            }

            return model;
        }

        internal static DanfeViewModel CreateFromXmlString(String xml)
        {
            ProcNFe nfe = null;
            XmlSerializer serializer = new XmlSerializer(typeof(ProcNFe));

            try
            {
                using (TextReader reader = new StringReader(xml))
                {
                    nfe = (ProcNFe)serializer.Deserialize(reader);
                }

                return CreateFromXml(nfe);
            }
            catch (System.InvalidOperationException e)
            {
                throw new Exception("Não foi possível interpretar o texto Xml.", e);
            }
        }

        /// <summary>
        /// Cria o modelo a partir de um arquivo xml.
        /// </summary>
        /// <param name="caminho"></param>
        /// <returns></returns>
        public static DanfeViewModel CriarDeArquivoXml(String caminho)
        {
            using (StreamReader sr = new StreamReader(caminho, true))
            {
                return CriarDeArquivoXmlInternal(sr);
            }
        }

        /// <summary>
        /// Cria o modelo a partir de um arquivo xml contido num stream.
        /// </summary>
        /// <param name="stream"></param>
        /// <returns>Modelo</returns>
        public static DanfeViewModel CriarDeArquivoXml(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));     

            using (StreamReader sr = new StreamReader(stream, true))
            {
                return CriarDeArquivoXmlInternal(sr);
            }
        }

        /// <summary>
        /// Cria o modelo a partir de uma string xml.
        /// </summary>
        public static DanfeViewModel CriarDeStringXml(string str)
        {
            if (str == null) throw new ArgumentNullException(nameof(str));

            using (StringReader sr = new StringReader(str))
            {
                return CriarDeArquivoXmlInternal(sr);
            }
        }


        private static DanfeViewModel CriarDeArquivoXmlInternal(TextReader reader)
        {
            ProcNFe nfe = null;
            XmlSerializer serializer = new XmlSerializer(typeof(ProcNFe));

            try
            {
                nfe = (ProcNFe)serializer.Deserialize(reader);
                return CreateFromXml(nfe);
            }
            catch (System.InvalidOperationException e)
            {
                if (e.InnerException is XmlException)
                {
                    XmlException ex = (XmlException)e.InnerException;
                    throw new Exception(String.Format("Não foi possível interpretar o Xml. Linha {0} Posição {1}.", ex.LineNumber, ex.LinePosition));
                }

                throw new XmlException("O Xml não parece ser uma NF-e processada.", e);
            }
        }

        internal static void ExtrairDatas(DanfeViewModel model, InfNFe infNfe)
        {
            var ide = infNfe.ide;

            if (infNfe.Versao.Maior >= 3)
            {
                if(ide.dhEmi.HasValue) model.DataHoraEmissao = ide.dhEmi?.DateTimeOffsetValue.DateTime;
                if(ide.dhSaiEnt.HasValue) model.DataSaidaEntrada = ide.dhSaiEnt?.DateTimeOffsetValue.DateTime;

                if (model.DataSaidaEntrada.HasValue)
                {
                    model.HoraSaidaEntrada = model.DataSaidaEntrada?.TimeOfDay;
                }
            }
            else
            {
                model.DataHoraEmissao = ide.dEmi;
                model.DataSaidaEntrada = ide.dSaiEnt;

                if (!String.IsNullOrWhiteSpace(ide.hSaiEnt))
                {
                    model.HoraSaidaEntrada = TimeSpan.Parse(ide.hSaiEnt);
                }

            }
        }

        internal static CalculoImpostoViewModel CriarCalculoImpostoViewModel(ICMSTotal i)
        {
            return new CalculoImpostoViewModel()
            {
                ValorAproximadoTributos = i.vTotTrib,
                BaseCalculoIcms = i.vBC,
                ValorIcms = i.vICMS,
                BaseCalculoIcmsSt = i.vBCST,
                ValorIcmsSt = i.vST,
                ValorTotalProdutos = i.vProd,
                ValorFrete = i.vFrete,
                ValorSeguro = i.vSeg,
                Desconto = i.vDesc,
                ValorII = i.vII,
                ValorIpi = i.vIPI,
                ValorPis = i.vPIS,
                ValorCofins = i.vCOFINS,
                OutrasDespesas = i.vOutro,
                ValorTotalNota = i.vNF,
                vFCPUFDest = i.vFCPUFDest,
                vICMSUFDest = i.vICMSUFDest,
                vICMSUFRemet = i.vICMSUFRemet,
                ValorFcp = i.vFCP,
                ValorFcpSt = i.vFCPST,
                QuantidadeBaseIcmsMonofasico = i.qBCMono,
                ValorIcmsMonofasico = i.vICMSMono,
                QuantidadeBaseIcmsMonofasicoRetencao = i.qBCMonoReten,
                ValorIcmsMonofasicoRetencao = i.vICMSMonoReten
            };
        }

        /// <summary>
        /// Extrai os totais do IBS, CBS e IS (NT 2025.002-RTC).
        /// </summary>
        internal static void ExtrairTotaisReformaTributaria(CalculoImpostoViewModel m, Total total)
        {
            m.ValorImpostoSeletivo = total.ISTot?.vIS;
            m.ValorTotalNotaComIbsCbsIs = total.vNFTot;

            var t = total.IBSCBSTot;
            if (t == null) return;

            m.BaseCalculoIbsCbs = t.vBCIBSCBS;
            m.ValorIbs = t.gIBS?.vIBS;
            m.ValorIbsUf = t.gIBS?.gIBSUF?.vIBSUF;
            m.ValorIbsMunicipio = t.gIBS?.gIBSMun?.vIBSMun;
            m.ValorCbs = t.gCBS?.vCBS;

            var mono = t.gMono;

            if (mono != null)
            {
                m.ValorIbsMonofasico = mono.vIBSMono;
                m.ValorCbsMonofasica = mono.vCBSMono;
                m.ValorIbsMonofasicoRetencao = mono.vIBSMonoReten;
                m.ValorCbsMonofasicaRetencao = mono.vCBSMonoReten;
            }
        }

        /// <summary>
        /// Cria o modelo de um item da NF-e ou da NFC-e.
        /// </summary>
        internal static ProdutoViewModel CriarProduto(Detalhe det)
        {
            ProdutoViewModel produto = new ProdutoViewModel();
            produto.Codigo = det.prod.cProd;
            produto.Descricao = det.prod.xProd;
            produto.Ncm = det.prod.NCM;
            produto.Cfop = det.prod.CFOP;
            produto.Unidade = det.prod.uCom;
            produto.Quantidade = det.prod.qCom;
            produto.ValorUnitario = det.prod.vUnCom;
            produto.ValorTotal = det.prod.vProd;
            produto.InformacoesAdicionais = det.infAdProd;

            if (Double.TryParse(det.prod.vDesc, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double vDesc))
                produto.ValorDesconto = vDesc;

            var imposto = det.imposto;

            if (imposto != null)
            {
                if (imposto.ICMS != null)
                {
                    var icms = imposto.ICMS.ICMS;

                    if (icms != null)
                    {
                        produto.ValorIcms = icms.vICMS;
                        produto.BaseIcms = icms.vBC;
                        produto.AliquotaIcms = icms.pICMS;
                        produto.BaseIcmsSt = icms.vBCST;
                        produto.ValorIcmsSt = icms.vICMSST;
                        produto.OCst = icms.orig + " " + icms.CST + icms.CSOSN;
                    }
                }

                if (imposto.IPI != null)
                {
                    var ipi = imposto.IPI.IPITrib;

                    if (ipi != null)
                    {
                        produto.ValorIpi = ipi.vIPI;
                        produto.AliquotaIpi = ipi.pIPI;
                        produto.BaseIpi = ipi.vBC;
                    }
                }

                if (imposto.IBSCBS != null)
                {
                    var ibsCbs = imposto.IBSCBS;
                    produto.CstIbsCbs = ibsCbs.CST;
                    produto.ClassificacaoTributaria = ibsCbs.cClassTrib;

                    var g = ibsCbs.gIBSCBS;

                    if (g != null)
                    {
                        produto.BaseIbsCbs = g.vBC;
                        produto.AliquotaIbsUf = g.gIBSUF?.AliquotaDanfe;
                        produto.ValorIbsUf = g.gIBSUF?.vIBSUF;
                        produto.AliquotaIbsMunicipio = g.gIBSMun?.AliquotaDanfe;
                        produto.ValorIbsMunicipio = g.gIBSMun?.vIBSMun;
                        produto.AliquotaCbs = g.gCBS?.AliquotaDanfe;
                        produto.ValorCbs = g.gCBS?.vCBS;
                    }
                }

                if (imposto.IS != null)
                {
                    produto.BaseIs = imposto.IS.vBCIS;
                    produto.AliquotaIs = imposto.IS.pIS;
                    produto.ValorIs = imposto.IS.vIS;
                }
            }

            return produto;
        }

        public static DanfeViewModel CreateFromXml(ProcNFe procNfe)
        {
            DanfeViewModel model = new DanfeViewModel();

            var nfe = procNfe.NFe;
            var infNfe = nfe.infNFe;
            var ide = infNfe.ide;
            model.TipoEmissao = ide.tpEmis;

            if (ide.mod != 55)
            {
                throw new NotSupportedException("O DANFE é gerado apenas para a NF-e (mod 55). Para a NFC-e (mod 65), use DanfeSharp.NFCe.Danfce.");
            }

            if(!FormasEmissaoSuportadas.Contains(model.TipoEmissao))
            {
                throw new NotSupportedException($"O tpEmis {ide.tpEmis} não é suportado.");
            }

            model.Orientacao = ide.tpImp == 1 ? Orientacao.Retrato : Orientacao.Paisagem;

            var infProt = procNfe.protNFe.infProt;
            model.CodigoStatusReposta = infProt.cStat;
            model.DescricaoStatusReposta = infProt.xMotivo;

            model.TipoAmbiente = (int)ide.tpAmb;
            model.NfNumero = ide.nNF;
            model.NfSerie = ide.serie;
            model.NaturezaOperacao = ide.natOp;
            model.ChaveAcesso = procNfe.NFe.infNFe.Id.Substring(3);
            model.TipoNF = (int)ide.tpNF;

            model.Emitente = CreateEmpresaFrom(infNfe.emit);
            model.Destinatario = CreateEmpresaFrom(infNfe.dest);

            // Local retirada e entrega 
            if (infNfe.retirada != null)
            {
                model.LocalRetirada = CreateLocalRetiradaEntrega(infNfe.retirada);
            }

            if (infNfe.entrega != null)
            {
                model.LocalEntrega = CreateLocalRetiradaEntrega(infNfe.entrega);
            }

            model.NotasFiscaisReferenciadas = ide.NFref.Select(x => x.ToString()).ToList();

            // Informações adicionais de compra
            if (infNfe.compra != null)
            {
                model.Contrato = infNfe.compra.xCont;
                model.NotaEmpenho = infNfe.compra.xNEmp;
                model.Pedido = infNfe.compra.xPed;
            }

            foreach (var det in infNfe.det)
            {
                model.Produtos.Add(CriarProduto(det));
            }

            if (infNfe.cobr != null)
            {
                foreach (var item in infNfe.cobr.dup)
                {
                    DuplicataViewModel duplicata = new DuplicataViewModel();
                    duplicata.Numero = item.nDup;
                    duplicata.Valor = item.vDup;
                    duplicata.Vecimento = item.dVenc;

                    model.Duplicatas.Add(duplicata);
                }
            }

            model.CalculoImposto = CriarCalculoImpostoViewModel(infNfe.total.ICMSTot);
            ExtrairTotaisReformaTributaria(model.CalculoImposto, infNfe.total);

            var issqnTotal = infNfe.total.ISSQNtot;

            if (issqnTotal != null)
            {
                var c = model.CalculoIssqn;
                c.InscricaoMunicipal = infNfe.emit.IM;
                c.BaseIssqn = issqnTotal.vBC;
                c.ValorTotalServicos = issqnTotal.vServ;
                c.ValorIssqn = issqnTotal.vISS;
                c.Mostrar = true;
            }

            var transp = infNfe.transp;
            var transportadora = transp.transporta;
            var transportadoraModel = model.Transportadora;

            transportadoraModel.ModalidadeFrete = (int)transp.modFrete;

            if (transp.veicTransp != null)
            {
                transportadoraModel.VeiculoUf = transp.veicTransp.UF;
                transportadoraModel.CodigoAntt = transp.veicTransp.RNTC;
                transportadoraModel.Placa = transp.veicTransp.placa;
            }

            if (transportadora != null)
            {
                transportadoraModel.RazaoSocial = transportadora.xNome;
                transportadoraModel.EnderecoUf = transportadora.UF;
                transportadoraModel.CnpjCpf = !String.IsNullOrWhiteSpace(transportadora.CNPJ) ? transportadora.CNPJ : transportadora.CPF;
                transportadoraModel.EnderecoLogadrouro = transportadora.xEnder;
                transportadoraModel.Municipio = transportadora.xMun;
                transportadoraModel.Ie = transportadora.IE;
            }


            var vol = transp.vol.FirstOrDefault();

            if (vol != null)
            {
                transportadoraModel.QuantidadeVolumes = vol.qVol;
                transportadoraModel.Especie = vol.esp;
                transportadoraModel.Marca = vol.marca;
                transportadoraModel.Numeracao = vol.nVol;
                transportadoraModel.PesoBruto = vol.pesoB;
                transportadoraModel.PesoLiquido = vol.pesoL;
            }


            var infAdic = infNfe.infAdic;
            if (infAdic != null)
            {
                model.InformacoesComplementares = procNfe.NFe.infNFe.infAdic.infCpl;
                model.InformacoesAdicionaisFisco = procNfe.NFe.infNFe.infAdic.infAdFisco;
            }

            var infoProto = procNfe.protNFe.infProt;

            model.ProtocoloAutorizacao = String.Format(Formatador.Cultura, "{0} - {1}", infoProto.nProt, infoProto.dhRecbto.DateTimeOffsetValue.DateTime);

            ExtrairDatas(model, infNfe);

            // Contingência SVC-AN e SVC-RS
            if(model.TipoEmissao == FormaEmissao.ContingenciaSVCAN || model.TipoEmissao == FormaEmissao.ContingenciaSVCRS)
            {
             
                model.ContingenciaDataHora = ide.dhCont?.DateTimeOffsetValue.DateTime;
                model.ContingenciaJustificativa = ide.xJust;
            }

            return model;
        }

        private static LocalEntregaRetiradaViewModel CreateLocalRetiradaEntrega(LocalEntregaRetirada local)
        {
            var m = new LocalEntregaRetiradaViewModel()
            {
                NomeRazaoSocial = local.xNome,
                CnpjCpf = !String.IsNullOrWhiteSpace(local.CNPJ) ? local.CNPJ : local.CPF,
                InscricaoEstadual = local.IE,
                Bairro = local.xBairro,
                Municipio = local.xMun,
                Uf = local.UF,
                Cep = local.CEP,
                Telefone = local.fone
            };

            StringBuilder sb = new StringBuilder();
            sb.Append(local.xLgr);

            if (!String.IsNullOrWhiteSpace(local.nro))
            {
                sb.Append(", ").Append(local.nro);
            }

            if (!String.IsNullOrWhiteSpace(local.xCpl))
            {
                sb.Append(" - ").Append(local.xCpl);
            }

            m.Endereco = sb.ToString();

            return m;
        }
    }
}
