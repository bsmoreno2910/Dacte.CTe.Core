namespace Cte.Core.Modelo
{
    /// <summary>
    /// Quadro "Informações Relativas ao Imposto" é ICMS + (opcional) IBS/CBS
    /// da Reforma Tributária 2026.
    /// </summary>
    public class ImpostoViewModel
    {
        /// <summary>Classificação Tributária do Serviço (CST do ICMS escolhido).</summary>
        public string SituacaoTributaria { get; set; }

        public decimal? BaseCalculo { get; set; }

        /// <summary>Alíquota do ICMS (%)</summary>
        public decimal? Aliquota { get; set; }

        public decimal? Valor { get; set; }

        /// <summary>Valor do ICMS retido por substituicao tributaria.</summary>
        public decimal? ValorIcmsSubstituicao { get; set; }

        /// <summary>Percentual de redução da base de cálculo (%)</summary>
        public decimal? PercentualReducaoBC { get; set; }

        public decimal? ValorTotalTributos { get; set; }

        // IBS/CBS (Reforma Tributária 2026)
        /// <summary>Verdadeiro se o XML traz o grupo IBSCBS preenchido.</summary>
        public bool TemIbsCbs { get; set; }

        /// <summary>Base de cálculo de IBS/CBS.</summary>
        public decimal? BaseCalculoIbsCbs { get; set; }

        /// <summary>Alíquota do IBS (UF + Município) consolidada (%)</summary>
        public decimal? AliquotaIbs { get; set; }

        /// <summary>Valor total do IBS (UF + Município).</summary>
        public decimal? ValorIbs { get; set; }

        /// <summary>Alíquota do CBS (%)</summary>
        public decimal? AliquotaCbs { get; set; }

        /// <summary>Valor do CBS.</summary>
        public decimal? ValorCbs { get; set; }
    }
}
