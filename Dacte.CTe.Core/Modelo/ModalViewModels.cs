using System.Collections.Generic;

namespace Dacte.CTe.Core.Modelo
{
    /// <summary>
    /// Tipos de modal do CT-e (item ide.modal do XML).
    /// </summary>
    public enum TipoModal
    {
        Rodoviario = 1,
        Aereo = 2,
        Aquaviario = 3,
        Ferroviario = 4,
        Dutoviario = 5,
        Multimodal = 6
    }

    /// <summary>Dados especificos do modal rodoviario.</summary>
    public class ModalRodoViewModel
    {
        public string Rntrc { get; set; }
        public string Ciot { get; set; }
        public bool? Lotacao { get; set; }
    }

    /// <summary>Modal Aéreo.</summary>
    public class ModalAereoViewModel
    {
        public string NumeroOperacionalConhecimento { get; set; }
        public string NumeroMinuta { get; set; }
        public string AeroportoOrigem { get; set; }
        public string AeroportoPassagem { get; set; }
        public string AeroportoDestino { get; set; }
        public string Classe { get; set; }
        public string CodigoTarifa { get; set; }
        public decimal? ValorTarifa { get; set; }
        public string DataPrevistaEntrega { get; set; }
        public string Dimensao { get; set; }
        public string InformacoesManuseio { get; set; }
        public string CaracteristicaServico { get; set; }
        public bool Retira { get; set; }
        public string DadosRetirada { get; set; }
        public string IdentificacaoInternaTomador { get; set; }
        public string IdentificacaoEmissor { get; set; }
    }

    /// <summary>Modal Aquaviário.</summary>
    public class ModalAquavViewModel
    {
        public string IdentificacaoNavio { get; set; }
        public decimal? ValorAfrmm { get; set; }
        public List<string> Balsas { get; set; } = new List<string>();
        public List<ContainerInfo> Containers { get; set; } = new List<ContainerInfo>();
    }

    public class ContainerInfo
    {
        public string Numero { get; set; }
        public List<string> Lacres { get; set; } = new List<string>();
    }

    /// <summary>Modal Ferroviário.</summary>
    public class ModalFerrovViewModel
    {
        public string TipoTrafego { get; set; }
        public string Fluxo { get; set; }
        public string ResponsavelFaturamento { get; set; }
        public string FerroviaEmitenteCte { get; set; }
        public decimal? ValorFrete { get; set; }
        public List<FerroviaEnvolvidaInfo> FerroviasEnvolvidas { get; set; } = new List<FerroviaEnvolvidaInfo>();
    }

    public class FerroviaEnvolvidaInfo
    {
        public string Cnpj { get; set; }
        public string InscricaoEstadual { get; set; }
        public string CodigoInterno { get; set; }
        public string RazaoSocial { get; set; }
    }

    /// <summary>Modal Dutoviário.</summary>
    public class ModalDutoViewModel
    {
        public decimal? ValorTarifa { get; set; }
        public string DataInicio { get; set; }
        public string DataFim { get; set; }
    }

    /// <summary>Modal Multimodal.</summary>
    public class ModalMultiViewModel
    {
        public string CertificadoOTM { get; set; }
        public bool Negociavel { get; set; }
        public SeguroMultimodal Seguro { get; set; }
    }

    public class SeguroMultimodal
    {
        public string NomeSeguradora { get; set; }
        public string CnpjSeguradora { get; set; }
        public string NumeroApolice { get; set; }
        public string NumeroAverbacao { get; set; }
    }
}
