using System.Drawing;
using Dacte.CTe.Core.Enumeracoes;
using Dacte.CTe.Core.Graphics;
using Dacte.CTe.Core.Modelo;
using Dacte.CTe.Core.Tools.Extensions;

namespace Dacte.CTe.Core.Elementos
{
    /// <summary>
    /// Coluna direita do bloco superior do DACTE.
    /// </summary>
    internal class QuadroProtocoloModal : ElementoBase
    {
        public DacteViewModel ViewModel { get; private set; }

        public QuadroProtocoloModal(Estilo estilo, DacteViewModel viewModel) : base(estilo)
        {
            ViewModel = viewModel;
        }

        public override void Draw(Gfx gfx)
        {
            base.Draw(gfx);

            var rp = BoundingBox.InflatedRetangle(0.5F);
            var fonteLbl = Estilo.CriarFonteNegrito(4.5F);
            var fonteVal = Estilo.CriarFonteRegular(5.5F);

            // 1) MODAL
            gfx.DrawString("MODAL", rp, fonteLbl, AlinhamentoHorizontal.Centro);
            rp = rp.CutTop(fonteLbl.AlturaLinha + 0.3F);
            gfx.DrawString(ViewModel.Modal.ToString(), rp, fonteVal, AlinhamentoHorizontal.Centro);
            rp = rp.CutTop(fonteVal.AlturaLinha + 1.5F);

            // 2) Nº PROTOCOLO
            gfx.DrawString("Nº PROTOCOLO", rp, fonteLbl, AlinhamentoHorizontal.Centro);
            rp = rp.CutTop(fonteLbl.AlturaLinha + 0.3F);
            gfx.DrawString(ExtrairNumeroProtocolo(ViewModel.ProtocoloAutorizacao),
                           rp, fonteVal, AlinhamentoHorizontal.Centro);
            rp = rp.CutTop(fonteVal.AlturaLinha + 1.5F);

            // 3) CONTROLE DO FISCO
            gfx.DrawString("CONTROLE DO FISCO", rp, fonteLbl, AlinhamentoHorizontal.Centro);
        }

        private static string ExtrairNumeroProtocolo(string protocoloFormatado)
        {
            if (string.IsNullOrWhiteSpace(protocoloFormatado)) return string.Empty;
            int hifen = protocoloFormatado.IndexOf(" - ");
            return hifen > 0 ? protocoloFormatado.Substring(0, hifen) : protocoloFormatado;
        }
    }
}
