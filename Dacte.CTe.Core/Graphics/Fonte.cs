using System;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Dacte.CTe.Core.Tools.Extensions;

namespace Dacte.CTe.Core.Graphics
{
    /// <summary>
    /// Define uma fonte (XFont do PdfSharpCore) e um tamanho em pontos.
    /// A fonte é usada para desenho e medição. O tamanho aqui é o mesmo
    /// passado ao XFont (em pontos), e as medidas de altura/largura são
    /// convertidas de pontos para milímetros para uso interno pelo DACTE.
    /// </summary>
    internal class Fonte
    {
        private static readonly XPdfFontOptions DefaultOptions = new XPdfFontOptions(PdfFontEncoding.Unicode);

        private float _Tamanho;
        private XFont _FonteInterna;

        /// <summary>
        /// Família da fonte (ex: "Times New Roman").
        /// </summary>
        public string FamiliaFonte { get; private set; }

        /// <summary>
        /// Estilo da fonte (Regular, Bold, Italic...).
        /// </summary>
        public XFontStyle EstiloFonte { get; private set; }

        /// <summary>
        /// XFont usado internamente pelo PdfSharpCore.
        /// Recriado automaticamente quando <see cref="Tamanho"/> é alterado.
        /// </summary>
        public XFont FonteInterna => _FonteInterna;

        public Fonte(string familia, XFontStyle estilo, float tamanho)
        {
            if (string.IsNullOrWhiteSpace(familia)) throw new ArgumentNullException(nameof(familia));
            FamiliaFonte = familia;
            EstiloFonte = estilo;
            Tamanho = tamanho;
        }

        /// <summary>
        /// Tamanho da fonte em pontos. Quando alterado, o <see cref="FonteInterna"/>
        /// é recriado — a API do XFont trata o size como imutável.
        /// </summary>
        public float Tamanho
        {
            get => _Tamanho;
            set
            {
                if (value <= 0) throw new InvalidOperationException("O tamanho deve ser maior que zero.");
                if (_Tamanho == value && _FonteInterna != null) return;
                _Tamanho = value;
                _FonteInterna = new XFont(FamiliaFonte, value, EstiloFonte, DefaultOptions);
            }
        }

        /// <summary>
        /// Mede a largura ocupada por uma string.
        /// </summary>
        /// <param name="str">String.</param>
        /// <returns>Largura em milímetros.</returns>
        public float MedirLarguraTexto(string str)
        {
            if (string.IsNullOrEmpty(str)) return 0;
            double widthPt = FontMetrics.MeasureWidthPt(FonteInterna, str);
            return (float)widthPt.ToMm();
        }

        /// <summary>
        /// Mede a largura ocupada por um Char.
        /// </summary>
        /// <param name="c">Char.</param>
        /// <returns>Largura em milímetros.</returns>
        public float MedirLarguraChar(char c)
        {
            double widthPt = FontMetrics.MeasureWidthPt(FonteInterna, c.ToString());
            return (float)widthPt.ToMm();
        }

        /// <summary>
        /// Altura da linha em milímetros, usando métrica compacta para quadros com pouco espaço.
        /// </summary>
        public float AlturaLinha => (float)(Tamanho * 0.9).ToMm();

        public Fonte Clonar() => new Fonte(FamiliaFonte, EstiloFonte, Tamanho);
    }
}
