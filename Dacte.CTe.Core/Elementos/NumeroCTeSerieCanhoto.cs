using Dacte.CTe.Core.Enumeracoes;
using Dacte.CTe.Core.Graphics;
using Dacte.CTe.Core.Tools.Extensions;

namespace Dacte.CTe.Core.Elementos
{
    /// <summary>
    /// Mini-bloco do canhoto "CT-e / Nº.: X / Série: Y".
    /// </summary>
    internal class NumeroCTeSerieCanhoto : ElementoBase
    {
        public string Numero { get; }
        public string Serie { get; }

        public NumeroCTeSerieCanhoto(Estilo estilo, string numero, string serie) : base(estilo)
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
