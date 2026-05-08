using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using Dacte.CTe.Core.Enumeracoes;
using Dacte.CTe.Core.Esquemas;
using Dacte.CTe.Core.Tools;

namespace Dacte.CTe.Core.Modelo
{
    /// <summary>
    /// Transforma o XML processado do CT-e em um <see cref="DacteViewModel"/>.
    /// </summary>
    public static class DacteViewModelCreator
    {
        // Entrypoints
        public static DacteViewModel CriarDeArquivoXml(string caminho)
        {
            using (var sr = new StreamReader(caminho, true))
                return CriarInternal(sr);
        }

        public static DacteViewModel CriarDeArquivoXml(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            using (var sr = new StreamReader(stream, true))
                return CriarInternal(sr);
        }

        public static DacteViewModel CriarDeStringXml(string xml)
        {
            if (xml == null) throw new ArgumentNullException(nameof(xml));
            using (var sr = new StringReader(xml))
                return CriarInternal(sr);
        }

        private static DacteViewModel CriarInternal(TextReader reader)
        {
            var serializer = new XmlSerializer(typeof(ProcCTe));
            try
            {
                var procCte = (ProcCTe)serializer.Deserialize(reader);
                return CriarDeProc(procCte);
            }
            catch (InvalidOperationException ex) when (ex.InnerException is XmlException xmlEx)
            {
                throw new Exception(
                    $"Não foi possível interpretar o XML do CT-e. Linha {xmlEx.LineNumber}, Posição {xmlEx.LinePosition}.", ex);
            }
            catch (InvalidOperationException ex)
            {
                throw new XmlException("O XML não parece ser um CT-e processado (cteProc).", ex);
            }
        }

        // Conversão principal
        public static DacteViewModel CriarDeProc(ProcCTe proc)
        {
            if (proc == null) throw new ArgumentNullException(nameof(proc));

            var infCte = proc.CTe?.infCte
                ?? throw new InvalidDataException("XML não contém CTe/infCte.");

            var ide = infCte.ide
                ?? throw new InvalidDataException("CT-e sem grupo ide.");

            var model = new DacteViewModel
            {
                Modelo = ide.mod,
                Serie = ide.serie,
                Numero = ide.nCT,
                ChaveAcesso = !string.IsNullOrEmpty(infCte.Id) && infCte.Id.Length > 3
                    ? infCte.Id.Substring(3) : null,
                TipoCTe = ide.tpCTe,
                TipoServico = ide.tpServ,
                TipoEmissao = ide.tpEmis == 0 ? 1 : ide.tpEmis,
                TipoAmbiente = (int)ide.tpAmb,
                Cfop = ide.CFOP,
                NaturezaOperacao = ide.natOp,
                FormaPagamento = DescFormaPagamento(ide.forPag),
                Orientacao = ide.tpImp == 1 ? Orientacao.Retrato : Orientacao.Paisagem,
                ModalCodigo = ide.modal,
                Modal = ParseModal(ide.modal),
                InicioPrestacaoUf = ide.UFIni,
                InicioPrestacaoMunicipio = ide.xMunIni,
                TerminoPrestacaoUf = ide.UFFim,
                TerminoPrestacaoMunicipio = ide.xMunFim,
                CTeGlobalizado = ide.indGlobalizado == "1",
            };

            // dhEmi é struct (DateTimeOffsetIso8601) é sempre tem valor após deserialize.
            model.DataHoraEmissao = ide.dhEmi.DateTimeOffsetValue.DateTime;

            if (model.EmissaoEmContingencia && ide.dhCont.HasValue)
                model.ContingenciaDataHora = ide.dhCont.Value.DateTimeOffsetValue.DateTime;

            model.ContingenciaJustificativa = ide.xJust;

            // Pessoas
            if (infCte.emit != null)
                model.Emitente = MapEmitente(infCte.emit);

            if (infCte.rem != null)
                model.Remetente = MapEmpresa(infCte.rem);

            if (infCte.exped != null)
                model.Expedidor = MapEmpresa(infCte.exped);

            if (infCte.receb != null)
                model.Recebedor = MapEmpresa(infCte.receb);

            if (infCte.dest != null)
            {
                model.Destinatario = MapEmpresa(infCte.dest);
                model.Destinatario.Isuf = infCte.dest.ISUF;
            }

            if (infCte.compl?.Entrega?.destEnde?.enderDest != null)
            {
                model.LocalEntrega = new EmpresaViewModel
                {
                    RazaoSocial = infCte.compl.Entrega.destEnde.xNome,
                    CnpjCpf = !string.IsNullOrWhiteSpace(infCte.compl.Entrega.destEnde.CNPJ)
                        ? infCte.compl.Entrega.destEnde.CNPJ
                        : infCte.compl.Entrega.destEnde.CPF
                };
                ApplyEndereco(model.LocalEntrega, infCte.compl.Entrega.destEnde.enderDest);
            }

            // Tomador: toma4 (próprio) ou toma3 (referência)
            if (ide.toma4 != null)
            {
                model.Tomador = new EmpresaViewModel
                {
                    RazaoSocial = ide.toma4.xNome,
                    NomeFantasia = ide.toma4.xFant,
                    CnpjCpf = !string.IsNullOrEmpty(ide.toma4.CNPJ) ? ide.toma4.CNPJ : ide.toma4.CPF,
                    Ie = ide.toma4.IE,
                    Telefone = ide.toma4.fone,
                    Email = ide.toma4.email
                };
                ApplyEndereco(model.Tomador, ide.toma4.enderToma);
                model.PapelTomador = "Outros (4)";
            }
            else if (ide.toma3 != null)
            {
                model.Tomador = ide.toma3.toma switch
                {
                    0 => model.Remetente,
                    1 => model.Expedidor,
                    2 => model.Recebedor,
                    3 => model.Destinatario,
                    _ => null
                };
                model.PapelTomador = ide.toma3.toma switch
                {
                    0 => "Remetente",
                    1 => "Expedidor",
                    2 => "Recebedor",
                    3 => "Destinatário",
                    _ => null
                };
            }

            // Carga
            if (infCte.infCTeNorm?.infCarga != null)
                model.Carga = MapCarga(infCte.infCTeNorm.infCarga);

            if (infCte.infCTeNorm?.seg?.Count > 0)
                model.Seguro = MapSeguro(infCte.infCTeNorm.seg.FirstOrDefault(s => s != null));

            // Componentes do valor
            if (infCte.vPrest != null)
            {
                model.ValorTotalPrestacao = infCte.vPrest.vTPrest;
                model.ValorReceber = infCte.vPrest.vRec;
                foreach (var c in infCte.vPrest.Comp)
                    model.Componentes.Add(new ComponenteValorViewModel { Nome = c.xNome, Valor = c.vComp });
            }

            // Imposto
            if (infCte.imp != null)
                model.Imposto = MapImposto(infCte.imp);

            // Documentos Originºrios
            if (infCte.infCTeNorm?.infDoc != null)
                model.DocumentosOrigem = MapDocumentosOrig(infCte.infCTeNorm.infDoc);

            // Fluxo
            if (infCte.compl?.fluxo != null)
            {
                model.FluxoOrigem = infCte.compl.fluxo.xOrig;
                model.FluxoDestino = infCte.compl.fluxo.xDest;
                model.FluxoRota = infCte.compl.fluxo.xRota;
                model.FluxoPassagem = infCte.compl.fluxo.pass.Select(p => p.xPass).ToList();
            }

            // Modais: infModal vive dentro de infCTeNorm no XML.
            if (infCte.infCTeNorm?.infModal != null)
                MapModal(infCte.infCTeNorm.infModal, model);

            if (model.Aereo != null)
            {
                model.Aereo.Retira = ide.retira == "1";
                model.Aereo.DadosRetirada = ide.xDetRetira;
            }

            // Compl/Entrega (data prevista da entrega)
            string dFim = infCte.compl?.Entrega?.noPeriodo?.dFim;
            if (!string.IsNullOrWhiteSpace(dFim))
            {
                if (DateTime.TryParse(dFim, out var dt))
                    model.DataPrevistaEntrega = dt.ToString("dd/MM/yyyy");
                else
                    model.DataPrevistaEntrega = dFim;
            }

            // QR Code (suplemento)
            model.QrCodeUrl = proc.CTe?.infCTeSupl?.qrCodCTe;

            // Observações
            if (infCte.compl != null)
            {
                model.ObservacoesGerais = infCte.compl.xObs;
                model.CaracteristicasAdicionais = infCte.compl.xCaracAd;
                model.CaracteristicaServico = infCte.compl.xCaracSer;
                model.EmitenteCustomizado = infCte.compl.xEmi;

                if (model.Aereo != null)
                {
                    if (string.IsNullOrWhiteSpace(model.Aereo.CaracteristicaServico))
                        model.Aereo.CaracteristicaServico = model.CaracteristicaServico;
                    if (string.IsNullOrWhiteSpace(model.Aereo.IdentificacaoEmissor))
                        model.Aereo.IdentificacaoEmissor = model.EmitenteCustomizado;
                }

                model.UsoExclusivoEmissor = JoinObs(infCte.compl.ObsCont.Select(o => $"{o.xCampo}: {o.xTexto}"));
                model.ReservadoFisco = JoinObs(infCte.compl.ObsFisco.Select(o => $"{o.xCampo}: {o.xTexto}"));
            }

            // Protocolo
            if (proc.protCTe?.infProt != null)
            {
                var ip = proc.protCTe.infProt;
                model.CodigoStatusResposta = ip.cStat;
                model.DescricaoStatusResposta = ip.xMotivo;

                if (ip.cStat == 100)
                {
                    model.DataHoraAutorizacao = ip.dhRecbto.DateTimeOffsetValue.DateTime;
                    // Formato compacto para caber na coluna do protocolo.
                    model.ProtocoloAutorizacao = string.Format(Formatador.Cultura, "{0} {1:dd/MM/yy HH:mm:ss}",
                        ip.nProt, model.DataHoraAutorizacao);
                }
            }

            return model;
        }

        // Helpers de mapeamento
        private static EmpresaViewModel MapEmitente(Emitente emit)
        {
            var m = new EmpresaViewModel
            {
                RazaoSocial = emit.xNome,
                NomeFantasia = emit.xFant,
                CnpjCpf = emit.CNPJ,
                Ie = emit.IE,
                IeSt = emit.IEST,
                CRT = emit.CRT,
                Telefone = emit.enderEmit?.fone
            };
            ApplyEndereco(m, emit.enderEmit);
            return m;
        }

        private static EmpresaViewModel MapEmpresa(Empresa e)
        {
            var m = new EmpresaViewModel
            {
                RazaoSocial = e.xNome,
                NomeFantasia = e.xFant,
                CnpjCpf = !string.IsNullOrEmpty(e.CNPJ) ? e.CNPJ : e.CPF,
                Ie = e.IE,
                // Em CT-e o fone aparece ora no <fone> do empresa, ora dentro do
                // <enderXxx>. Pegamos o que estiver preenchido.
                Telefone = !string.IsNullOrWhiteSpace(e.fone) ? e.fone : e.Endereco?.fone,
                Email = e.email
            };
            ApplyEndereco(m, e.Endereco);
            return m;
        }

        private static void ApplyEndereco(EmpresaViewModel m, Endereco end)
        {
            if (end == null) return;
            m.EnderecoLogadrouro = end.xLgr;
            m.EnderecoNumero = end.nro;
            m.EnderecoComplemento = end.xCpl;
            m.EnderecoBairro = end.xBairro;
            m.Municipio = end.xMun;
            m.EnderecoUf = end.UF;
            m.EnderecoCep = end.CEP;
            m.EnderecoPais = end.xPais;
        }

        private static InfoCargaViewModel MapCarga(InfCarga c)
        {
            var m = new InfoCargaViewModel
            {
                ProdutoPredominante = c.proPred,
                OutrasCaracteristicas = c.xOutCat,
                ValorTotalCarga = c.vCarga
            };

            foreach (var q in c.infQ)
            {
                // cUnid: 00=M3, 01=KG, 02=TON, 03=Unidade, 04=Litros, 05=mmBTU
                var fator = q.cUnid switch
                {
                    "01" => 1m,        // KG
                    "02" => 1000m,     // TON ? KG
                    _ => 1m
                };
                var qtdConvertida = q.qCarga * fator;

                m.Quantidades.Add((q.tpMed ?? string.Empty, q.cUnid, q.qCarga));

                // Aceita os rotulos do MOC e variantes comuns de mercado.
                var tp = (q.tpMed ?? string.Empty).Trim().ToUpperInvariant().Replace("_", " ");

                // PESO BRUTO
                if (tp.Contains("BRUTO") || tp.Contains("REAL"))
                    m.PesoBrutoKg = qtdConvertida;
            // === PESO CUBADO (vai pro slot "Base de Cálculo" no rodoviário,
                // ou aparece com label próprio em modais aéreo/multimodal) ===
                else if (tp.Contains("CUBADO") || (tp.Contains("BASE") && tp.Contains("CALC")))
                    m.PesoBaseCalculoKg = qtdConvertida;
            // === PESO TAXADO (modais aéreo/multimodal) === / aferido (rodoviário)
                else if (tp.Contains("TAXADO") || tp.Contains("AFERIDO"))
                    m.PesoAferidoKg = qtdConvertida;
                // Cubagem em m³ (cUnid=00)
                else if (tp.Contains("METROS CUBICO") || tp.Contains("CUBAGEM") || tp.Contains("M3"))
                    m.Cubagem = q.qCarga;
                // Quantidade de volumes (cUnid=03)
                else if (tp.Contains("VOLUME"))
                    m.QuantidadeVolumes = q.qCarga;
                // Tara_Container ignorada por padrão (não compõe DACTE).
            }

            return m;
        }

        private static ImpostoViewModel MapImposto(Imp imp)
        {
            var icms = imp.ICMS;
            var m = new ImpostoViewModel
            {
                ValorTotalTributos = imp.vTotTrib
            };

            // IBS/CBS (Reforma Tributária 2026)
            if (imp.IBSCBS?.gIBSCBS != null)
            {
                var g = imp.IBSCBS.gIBSCBS;
                m.TemIbsCbs = true;
                m.BaseCalculoIbsCbs = g.vBC;
                // IBS = UF + municipio consolidados.
                m.AliquotaIbs = (g.gIBSUF?.pIBSUF ?? 0m) + (g.gIBSMun?.pIBSMun ?? 0m);
                m.ValorIbs = g.vIBS ?? ((g.gIBSUF?.vIBSUF ?? 0m) + (g.gIBSMun?.vIBSMun ?? 0m));
                m.AliquotaCbs = g.gCBS?.pCBS;
                m.ValorCbs = g.gCBS?.vCBS;
            }

            if (icms == null) return m;

            if (icms.ICMS00 != null)
            {
                m.SituacaoTributaria = icms.ICMS00.CST;
                m.BaseCalculo = icms.ICMS00.vBC;
                m.Aliquota = icms.ICMS00.pICMS;
                m.Valor = icms.ICMS00.vICMS;
            }
            else if (icms.ICMS20 != null)
            {
                m.SituacaoTributaria = icms.ICMS20.CST;
                m.BaseCalculo = icms.ICMS20.vBC;
                m.Aliquota = icms.ICMS20.pICMS;
                m.Valor = icms.ICMS20.vICMS;
                m.PercentualReducaoBC = icms.ICMS20.pRedBC;
            }
            else if (icms.ICMS45 != null)
            {
                m.SituacaoTributaria = icms.ICMS45.CST;
            }
            else if (icms.ICMS60 != null)
            {
                m.SituacaoTributaria = icms.ICMS60.CST;
                m.BaseCalculo = icms.ICMS60.vBCSTRet;
                m.Aliquota = icms.ICMS60.pICMSSTRet;
                m.Valor = icms.ICMS60.vICMSSTRet;
                m.ValorIcmsSubstituicao = icms.ICMS60.vICMSSTRet;
                m.PercentualReducaoBC = icms.ICMS60.pRedBCOutraUF;
            }
            else if (icms.ICMS90 != null)
            {
                m.SituacaoTributaria = icms.ICMS90.CST;
                m.BaseCalculo = icms.ICMS90.vBC;
                m.Aliquota = icms.ICMS90.pICMS;
                m.Valor = icms.ICMS90.vICMS;
                m.PercentualReducaoBC = icms.ICMS90.pRedBC;
            }
            else if (icms.ICMSOutraUF != null)
            {
                m.SituacaoTributaria = icms.ICMSOutraUF.CST;
                m.BaseCalculo = icms.ICMSOutraUF.vBCOutraUF;
                m.Aliquota = icms.ICMSOutraUF.pICMSOutraUF;
                m.Valor = icms.ICMSOutraUF.vICMSOutraUF;
                m.PercentualReducaoBC = icms.ICMSOutraUF.pRedBCOutraUF;
            }
            else if (icms.ICMSSN != null)
            {
                m.SituacaoTributaria = icms.ICMSSN.CST;
            }

            return m;
        }

        private static List<DocumentoOrigViewModel> MapDocumentosOrig(InfDoc d)
        {
            var list = new List<DocumentoOrigViewModel>();

            foreach (var nfe in d.infNFe)
                list.Add(new DocumentoOrigViewModel
                {
                    Tipo = TipoDocumentoOrig.NFe,
                    Identificacao = nfe.chave,
                    DataEmissao = nfe.dPrev
                });

            foreach (var nf in d.infNF)
                list.Add(new DocumentoOrigViewModel
                {
                    Tipo = TipoDocumentoOrig.NF,
                    Modelo = nf.mod,
                    Serie = nf.serie,
                    Numero = nf.nDoc,
                    ValorDocumento = nf.vNF,
                    DataEmissao = nf.dEmi
                });

            foreach (var o in d.infOutros)
                list.Add(new DocumentoOrigViewModel
                {
                    Tipo = TipoDocumentoOrig.Outros,
                    Modelo = o.tpDoc,
                    Identificacao = o.descOutros,
                    Numero = o.nDoc,
                    ValorDocumento = o.vDocFisc,
                    DataEmissao = o.dEmi
                });

            return list;
        }

        private static void MapModal(InfModal infModal, DacteViewModel model)
        {
            if (infModal.rodo != null)
                model.Rodoviario = new ModalRodoViewModel
                {
                    Rntrc = infModal.rodo.RNTRC,
                    Ciot = infModal.rodo.CIOT,
                    Lotacao = ParseBoolIndicador(infModal.rodo.lota)
                };

            if (infModal.aereo != null)
            {
                var a = infModal.aereo;
                model.Aereo = new ModalAereoViewModel
                {
                    NumeroOperacionalConhecimento = a.nOCA,
                    NumeroMinuta = a.nMinu,
                    AeroportoOrigem = a.aeroOri,
                    AeroportoPassagem = a.aeroPag == null ? null : string.Join(", ", a.aeroPag.Where(p => !string.IsNullOrWhiteSpace(p))),
                    AeroportoDestino = a.aeroDes,
                    DataPrevistaEntrega = a.dPrevAereo,
                    Dimensao = a.natCarga?.xDime,
                    InformacoesManuseio = a.natCarga?.cInfManu,
                    Classe = a.tarifa?.CL,
                    CodigoTarifa = a.tarifa?.cTar,
                    ValorTarifa = a.tarifa?.vTar
                };
            }

            if (infModal.aquav != null)
            {
                var aq = infModal.aquav;
                model.Aquaviario = new ModalAquavViewModel
                {
                    IdentificacaoNavio = aq.xNavio,
                    ValorAfrmm = aq.vAFRMM,
                    Balsas = aq.balsa.Select(b => b.xBalsa).ToList(),
                    Containers = aq.detCont.Select(c => new ContainerInfo
                    {
                        Numero = c.nCont,
                        Lacres = c.lacre.Select(l => l.nLacre).ToList()
                    }).ToList()
                };
            }

            if (infModal.ferrov != null)
            {
                var f = infModal.ferrov;
                model.Ferroviario = new ModalFerrovViewModel
                {
                    TipoTrafego = f.tpTraf,
                    Fluxo = f.fluxo,
                    ResponsavelFaturamento = f.respFat,
                    FerroviaEmitenteCte = f.trafMut?.ferrEmi ?? f.ferrEmi,
                    ValorFrete = f.vFrete,
                    FerroviasEnvolvidas = f.ferroEnv.Select(fe => new FerroviaEnvolvidaInfo
                    {
                        Cnpj = fe.CNPJ,
                        InscricaoEstadual = fe.IE,
                        CodigoInterno = fe.cInt,
                        RazaoSocial = fe.xNome
                    }).ToList()
                };
            }

            if (infModal.duto != null)
            {
                model.Dutoviario = new ModalDutoViewModel
                {
                    ValorTarifa = infModal.duto.vTar,
                    DataInicio = infModal.duto.dIni,
                    DataFim = infModal.duto.dFim
                };
            }

            if (infModal.multimodal != null)
            {
                var mm = infModal.multimodal;
                model.Multimodal = new ModalMultiViewModel
                {
                    CertificadoOTM = mm.COTM,
                    Negociavel = mm.indNegociavel == "1",
                    Seguro = mm.seg == null ? null : new SeguroMultimodal
                    {
                        NomeSeguradora = mm.seg.infSeg?.xSeg,
                        CnpjSeguradora = mm.seg.infSeg?.CNPJ,
                        NumeroApolice = mm.seg.nApol,
                        NumeroAverbacao = mm.seg.nAver
                    }
                };

                if (model.Seguro == null && mm.seg != null)
                    model.Seguro = MapSeguro(mm.seg);
            }
        }

        private static SeguroViewModel MapSeguro(Seg seg)
        {
            if (seg == null) return null;

            return new SeguroViewModel
            {
                Responsavel = DescResponsavelSeguro(seg.respSeg),
                NomeSeguradora = seg.infSeg?.xSeg,
                CnpjSeguradora = seg.infSeg?.CNPJ,
                NumeroApolice = seg.nApol,
                NumeroAverbacao = seg.nAver
            };
        }

        private static bool? ParseBoolIndicador(string valor)
        {
            if (string.IsNullOrWhiteSpace(valor)) return null;
            if (valor == "1" || valor.Equals("true", StringComparison.OrdinalIgnoreCase)) return true;
            if (valor == "0" || valor.Equals("false", StringComparison.OrdinalIgnoreCase)) return false;
            return null;
        }

        private static string DescFormaPagamento(string codigo)
        {
            switch (codigo)
            {
                case "0": return "Pago";
                case "1": return "A pagar";
                case "2": return "Outros";
                default: return string.IsNullOrWhiteSpace(codigo) ? null : codigo;
            }
        }

        private static string DescResponsavelSeguro(string codigo)
        {
            switch (codigo)
            {
                case "1": return "Remetente";
                case "2": return "Expedidor";
                case "3": return "Recebedor";
                case "4": return "Destinatario";
                case "5": return "Emitente do CT-e";
                default: return string.IsNullOrWhiteSpace(codigo) ? null : codigo;
            }
        }

        private static TipoModal ParseModal(string modal) => modal switch
        {
            "01" => TipoModal.Rodoviario,
            "02" => TipoModal.Aereo,
            "03" => TipoModal.Aquaviario,
            "04" => TipoModal.Ferroviario,
            "05" => TipoModal.Dutoviario,
            "06" => TipoModal.Multimodal,
            _ => TipoModal.Rodoviario
        };

        private static string JoinObs(IEnumerable<string> items)
        {
            var sb = new StringBuilder();
            foreach (var i in items)
            {
                if (string.IsNullOrWhiteSpace(i)) continue;
                if (sb.Length > 0) sb.AppendLine();
                sb.Append(i);
            }
            return sb.Length == 0 ? null : sb.ToString();
        }
    }
}
