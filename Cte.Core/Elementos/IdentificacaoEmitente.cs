using System.Drawing;
using PdfSharpCore.Drawing;
using Cte.Core.Enumeracoes;
using Cte.Core.Graphics;
using Cte.Core.Modelo;
using Cte.Core.Tools.Extensions;

namespace Cte.Core.Elementos
{
    /// <summary>
    /// Quadro do emitente (lado esquerdo do bloco superior do DACTE).
    /// Renderiza opcionalmente o logo, o título de identificação do emitente
    /// e as três linhas de endereço.
    /// </summary>
    internal class IdentificacaoEmitente : ElementoBase
    {
        public DacteViewModel ViewModel { get; private set; }

        /// <summary>Logo como XImage (raster ou primeira página de um PDF).</summary>
        public XImage Logo { get; set; }

        public IdentificacaoEmitente(Estilo estilo, DacteViewModel viewModel) : base(estilo)
        {
            ViewModel = viewModel;
            Logo = null;
        }

        public override void Draw(Gfx gfx)
        {
            base.Draw(gfx);

            var rp = BoundingBox.InflatedRetangle(0.75F);
            const float alturaMaximaLogoHorizontal = 14F;

            var fonteRazao = Estilo.CriarFonteNegrito(12);
            var fonteEnd = Estilo.CriarFonteRegular(8);

            if (Logo == null)
            {
                var fonteTitulo = Estilo.CriarFonteRegular(6);
            gfx.DrawString("IDENTIFICA\u00C7\u00C3O DO EMITENTE", rp, fonteTitulo, AlinhamentoHorizontal.Centro);
                rp = rp.CutTop(fonteTitulo.AlturaLinha);
            }
            else
            {
                RectangleF rLogo;

                float logoWMm = (float)Logo.PointWidth.ToMm();
                float logoHMm = (float)Logo.PointHeight.ToMm();

                if (logoWMm > logoHMm)
                {
                    rLogo = new RectangleF(rp.X, rp.Y, rp.Width, alturaMaximaLogoHorizontal);
                    rp = rp.CutTop(alturaMaximaLogoHorizontal);
                }
                else
                {
                    float lw = rp.Height * logoWMm / logoHMm;
                    rLogo = new RectangleF(rp.X, rp.Y, lw, rp.Height);
                    rp = rp.CutLeft(lw);
                }

                gfx.ShowXObject(Logo, rLogo);
            }

            var emitente = ViewModel.Emitente;
            string nome = emitente.RazaoSocial;
            if (ViewModel.PreferirEmitenteNomeFantasia && !string.IsNullOrWhiteSpace(emitente.NomeFantasia))
                nome = emitente.NomeFantasia;

            var ts = new TextStack(rp) { LineHeightScale = 1 }
                .AddLine(nome ?? string.Empty, fonteRazao)
                .AddLine(emitente.EnderecoLinha1, fonteEnd)
                .AddLine(emitente.EnderecoLinha2, fonteEnd)
                .AddLine(emitente.EnderecoLinha3, fonteEnd);

            ts.AlinhamentoHorizontal = AlinhamentoHorizontal.Centro;
            ts.AlinhamentoVertical = AlinhamentoVertical.Centro;
            ts.Draw(gfx);
        }
    }
}
