namespace Cte.Core.Modelo
{
    /// <summary>
    /// Tipo de documento originºrio referenciado pelo CT-e.
    /// </summary>
    public enum TipoDocumentoOrig
    {
        NFe,
        NF,
        Outros
    }

    /// <summary>
    /// Item do quadro "Documentos Originºrios".
    /// </summary>
    public class DocumentoOrigViewModel
    {
        public TipoDocumentoOrig Tipo { get; set; }

        /// <summary>NF-e: chave de acesso 44 dígitos. NF: serie/numero. Outros: descrição.</summary>
        public string Identificacao { get; set; }

        public string Modelo { get; set; }
        public string Serie { get; set; }
        public string Numero { get; set; }
        public string CnpjCpfEmitente { get; set; }
        public decimal? ValorDocumento { get; set; }
        public string DataEmissao { get; set; }
    }
}
