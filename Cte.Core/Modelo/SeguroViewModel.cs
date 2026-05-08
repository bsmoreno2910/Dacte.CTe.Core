namespace Cte.Core.Modelo
{
    /// <summary>Dados do seguro informado no CT-e.</summary>
    public class SeguroViewModel
    {
        public string Responsavel { get; set; }
        public string NumeroApolice { get; set; }
        public string NumeroAverbacao { get; set; }
        public string NomeSeguradora { get; set; }
        public string CnpjSeguradora { get; set; }
    }
}
