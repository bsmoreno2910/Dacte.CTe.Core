using Cte.Core.Enumeracoes;
using Cte.Core.Graphics;
using Cte.Core.Tools.Extensions;

namespace Cte.Core.Elementos
{
    /// <summary>
    /// Mini-bloco do canhoto "CT-e / Nº.: X / Série: Y". Equivalente ao
    /// <c>NumeroNfSerie</c> do DANFE.
    /// </summary>
    internal class NumeroCteSerieCanhoto : ElementoBase
    {
        public string Numero { get; }
        public string Serie { get; }

        public NumeroCteSerieCanhoto(Estilo estilo, string numero, string serie) : base(estilo)
        {
            Numero = numero;
            Serie = serie;
        }

        public override void Draw(Gfx gfx)
        {
            base.Draw(gfx);

            var r = BoundingBox.InflatedRetangle(1);

            var fonteSigla = Estilo.CriarFonteNegrito(14);
            var fonteCorpo = Estilo.CriarFonteNegrito(11F);

            gfx.DrawString("CT-e", r, fonteSigla, AlinhamentoHorizontal.Centro);
            r = r.CutTop(fonteSigla.AlturaLinha);

            var ts = new TextStack(r)
            {
                AlinhamentoHorizontal = AlinhamentoHorizontal.Centro,
                AlinhamentoVertical = AlinhamentoVertical.Centro,
                LineHeightScale = 1F
            }
                .AddLine($"Nº.: {Numero}", fonteCorpo)
                .AddLine($"Série: {Serie}", fonteCorpo);

            ts.Draw(gfx);
        }
    }
}
