using System.Collections.Generic;

namespace Cte.Core.Modelo
{
    /// <summary>
    /// Quadro "Produto Predominante / Características da Carga" e quantidades.
    /// </summary>
    public class InfoCargaViewModel
    {
        public string ProdutoPredominante { get; set; }
        public string OutrasCaracteristicas { get; set; }
        public decimal? ValorTotalCarga { get; set; }

        /// <summary>Peso bruto em KG.</summary>
        public decimal? PesoBrutoKg { get; set; }

        /// <summary>Peso base de cálculo em KG.</summary>
        public decimal? PesoBaseCalculoKg { get; set; }

        /// <summary>Peso aferido em KG.</summary>
        public decimal? PesoAferidoKg { get; set; }

        /// <summary>Cubagem (m3).</summary>
        public decimal? Cubagem { get; set; }

        public decimal? QuantidadeVolumes { get; set; }

        /// <summary>Pares (tpMed, valor) crus, em ordem de aparição. útil para layouts customizados.</summary>
        public List<(string TipoMedida, string Unidade, decimal Quantidade)> Quantidades { get; set; } = new List<(string, string, decimal)>();
    }
}
