using System;
using System.Collections.Generic;
using Dacte.CTe.Core.Enumeracoes;

namespace Dacte.CTe.Core.Modelo
{
    /// <summary>
    /// ViewModel principal do DACTE. é preenchido pelo <see cref="DacteViewModelCreator"/>
    /// a partir do XML processado do CT-e e consumido pelo <c>DacteDoc</c>.
    /// </summary>
    public class DacteViewModel
    {
        // Identificação
        public string ChaveAcesso { get; set; }

        /// <summary>57 = CT-e, 67 = CT-e OS, 64 = GTV-e.</summary>
        public int Modelo { get; set; }
        public int Serie { get; set; }
        public long Numero { get; set; }

        public DateTime? DataHoraEmissao { get; set; }

        /// <summary>Modal: 01=Rodoviário, 02=Aéreo, 03=Aquaviário, 04=Ferroviário, 05=Dutoviário, 06=Multimodal.</summary>
        public TipoModal Modal { get; set; }

        public string ModalCodigo { get; set; }

        /// <summary>Tipo de CT-e: 0=Normal, 1=Complemento, 2=Anulação, 3=Substituto.</summary>
        public int TipoCTe { get; set; }

        /// <summary>Tipo de Serviço: 0=Normal, 1=Subcontratação, 2=Redespacho, 3=Redespacho Intermediário, 4=Vinculado a Multimodal.</summary>
        public int TipoServico { get; set; }

        /// <summary>Forma de emissão (1=Normal, 4=EPEC, 5=FS-DA, 7=SVC-RS, 8=SVC-SP).</summary>
        public int TipoEmissao { get; set; } = 1;

        /// <summary>1=Produção, 2=Homologação.</summary>
        public int TipoAmbiente { get; set; }

        public Orientacao Orientacao { get; set; } = Orientacao.Retrato;

        /// <summary>CFOP é Natureza da Operação.</summary>
        public string Cfop { get; set; }
        public string NaturezaOperacao { get; set; }
        public string FormaPagamento { get; set; }

        public string InicioPrestacaoUf { get; set; }
        public string InicioPrestacaoMunicipio { get; set; }
        public string TerminoPrestacaoUf { get; set; }
        public string TerminoPrestacaoMunicipio { get; set; }

        // Protocolo
        public string ProtocoloAutorizacao { get; set; }
        public int CodigoStatusResposta { get; set; }
        public string DescricaoStatusResposta { get; set; }
        public DateTime? DataHoraAutorizacao { get; set; }

        // Contingência
        public DateTime? ContingenciaDataHora { get; set; }
        public string ContingenciaJustificativa { get; set; }

        // Globalizado / Indicadores
        public bool CTeGlobalizado { get; set; }
        public string InfoCTeGlobalizado { get; set; }

        // Pessoas
        public EmpresaViewModel Emitente { get; set; } = new EmpresaViewModel();
        public EmpresaViewModel Remetente { get; set; }
        public EmpresaViewModel Expedidor { get; set; }
        public EmpresaViewModel Recebedor { get; set; }
        public EmpresaViewModel Destinatario { get; set; }
        public EmpresaViewModel LocalEntrega { get; set; }

        /// <summary>
        /// Tomador efetivo do serviço é depois da resolução de toma3 (referência) ou toma4 (dados próprios).
        /// </summary>
        public EmpresaViewModel Tomador { get; set; }

        /// <summary>Texto descritivo do papel do tomador ("Remetente", "Expedidor", etc.) é usado em alguns layouts.</summary>
        public string PapelTomador { get; set; }

        // Carga
        public InfoCargaViewModel Carga { get; set; } = new InfoCargaViewModel();
        public SeguroViewModel Seguro { get; set; }

        // Componentes do valor
        public List<ComponenteValorViewModel> Componentes { get; set; } = new List<ComponenteValorViewModel>();
        public decimal? ValorTotalPrestacao { get; set; }
        public decimal? ValorReceber { get; set; }

        // Imposto
        public ImpostoViewModel Imposto { get; set; } = new ImpostoViewModel();

        // Documentos originºrios
        public List<DocumentoOrigViewModel> DocumentosOrigem { get; set; } = new List<DocumentoOrigViewModel>();

        // Fluxo
        public string FluxoOrigem { get; set; }
        public string FluxoDestino { get; set; }
        public List<string> FluxoPassagem { get; set; } = new List<string>();
        public string FluxoRota { get; set; }

        // Modais
        public ModalRodoViewModel Rodoviario { get; set; }
        public ModalAereoViewModel Aereo { get; set; }
        public ModalAquavViewModel Aquaviario { get; set; }
        public ModalFerrovViewModel Ferroviario { get; set; }
        public ModalDutoViewModel Dutoviario { get; set; }
        public ModalMultiViewModel Multimodal { get; set; }

        // Observações
        public string ObservacoesGerais { get; set; }
        public string CaracteristicasAdicionais { get; set; }
        public string CaracteristicaServico { get; set; }
        public string EmitenteCustomizado { get; set; }

        /// <summary>"Uso Exclusivo do Emissor de CT-e" (ObsCont concatenadas).</summary>
        public string UsoExclusivoEmissor { get; set; }

        /// <summary>"Reservado ao Fisco" (ObsFisco concatenadas).</summary>
        public string ReservadoFisco { get; set; }

        // Quantidade de canhotos a desenhar (1 = padrão)
        public int QuantidadeCanhotos { get; set; } = 1;

        /// <summary>Margem em milímetros aplicada é folha.</summary>
        public float Margem { get; set; } = 4F;

        /// <summary>
        /// Quando verdadeiro, o quadro de identificação do emitente prioriza
        /// xFant (nome fantasia) sobre xNome (razão social) é útil para layouts
        /// onde o usuário publica com a marca em vez da razão social.
        /// </summary>
        public bool PreferirEmitenteNomeFantasia { get; set; } = false;

        /// <summary>
        /// Texto do canhoto. O DACTE permite personalização ("Declaramos ter recebido...").
        /// </summary>
        public string TextoRecebimento { get; set; }
            = "DECLARAMOS QUE RECEBEMOS OS VOLUMES DESCRITOS NO PRESENTE DACTE EM PERFEITO ESTADO";

        /// <summary>
        /// URL completa para consulta do CT-e via QR Code (campo
        /// <c>qrCodCTe</c> de <c>infCTeSupl</c>). Usada para gerar o QR Code
        /// impresso no canto superior direito do DACTE.
        /// </summary>
        public string QrCodeUrl { get; set; }

        /// <summary>
        /// "Data Prevista da Entrega" é extraída de <c>compl/Entrega/noPeriodo/dFim</c>.
        /// Formato dd/MM/yyyy quando informado no XML.
        /// </summary>
        public string DataPrevistaEntrega { get; set; }

        // Helpers
        /// <summary>Verdadeiro se o CT-e foi emitido em contingência (FS-DA = 5, EPEC = 4).</summary>
        public bool EmissaoEmContingencia => TipoEmissao == 4 || TipoEmissao == 5 || TipoEmissao == 7 || TipoEmissao == 8;
    }
}
