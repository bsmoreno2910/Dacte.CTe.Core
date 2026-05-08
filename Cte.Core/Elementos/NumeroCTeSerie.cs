using System.Drawing;
using Cte.Core.Enumeracoes;
using Cte.Core.Graphics;
using Cte.Core.Modelo;
using Cte.Core.Tools;
using Cte.Core.Tools.Extensions;

namespace Cte.Core.Elementos
{
    /// <summary>
    /// Coluna central do bloco superior do DACTE: "DACTE" em destaque +
    /// subtítulo "Documento Auxiliar do Conhecimento de Transporte
    /// Eletrônico" + tabela horizontal com MODELO / SÉRIE / NÚMERO / FL /
    /// DATA E HORA DE EMISSAO.
    /// </summary>
    internal class NumeroCTeSerie : ElementoBase
    {
        public RectangleF RetanguloNumeroFolhas { get; private set; }
        public DacteViewModel ViewModel { get; private set; }

        public NumeroCTeSerie(Estilo estilo, DacteViewModel viewModel) : base(estilo)
        {
            ViewModel = viewModel;
        }

        public override void Draw(Gfx gfx)
        {
            base.Draw(gfx);

            var rp = BoundingBox.InflatedRetangle(0.5F, 0.3F, 1F);

            var fonteTit = Estilo.CriarFonteNegrito(8.5F);
            var fonteSub = Estilo.CriarFonteRegular(5.5F);
            var fonteLbl = Estilo.CriarFonteNegrito(4.5F);
            var fonteVal = Estilo.CriarFonteRegular(6F);

            // 1ª linha é "DACTE" centralizado
            gfx.DrawString("DACTE", rp, fonteTit, AlinhamentoHorizontal.Centro);
            rp = rp.CutTop(fonteTit.AlturaLinha + 0.3F);

            // 2ª linha é subtítulo (1 linha só, fonte pequena)
            gfx.DrawString("Documento Auxiliar do Conhecimento de Transporte Eletrônico",
                           rp, fonteSub, AlinhamentoHorizontal.Centro);
            rp = rp.CutTop(fonteSub.AlturaLinha + 1.0F);

            // Tabela horizontal de campos.
            float topLabel = rp.Y;
            float topValor = rp.Y + fonteLbl.AlturaLinha + 0.5F;

            DesenhaCelula(gfx, rp.X + 0.00F * rp.Width, rp.X + 0.12F * rp.Width, topLabel, topValor, "MODELO",   ViewModel.Modelo.ToString("00"),    fonteLbl, fonteVal);
            DesenhaCelula(gfx, rp.X + 0.12F * rp.Width, rp.X + 0.24F * rp.Width, topLabel, topValor, "SÉRIE",    ViewModel.Serie.ToString(),         fonteLbl, fonteVal);
            DesenhaCelula(gfx, rp.X + 0.24F * rp.Width, rp.X + 0.42F * rp.Width, topLabel, topValor, "NÚMERO",   ViewModel.Numero.ToString(Formatador.FormatoNumeroNF), fonteLbl, fonteVal);
            DesenhaCelula(gfx, rp.X + 0.42F * rp.Width, rp.X + 0.52F * rp.Width, topLabel, topValor, "FL",       "1/1",                              fonteLbl, fonteVal);
            DesenhaCelula(gfx, rp.X + 0.52F * rp.Width, rp.X + 1.00F * rp.Width, topLabel, topValor, "DATA E HORA DE EMISS\u00C3O",
                          ViewModel.DataHoraEmissao?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty,
                          fonteLbl, fonteVal);

            // Texto da consulta (linha mais abaixo)
            float topConsulta = topValor + fonteVal.AlturaLinha + 0.6F;
            var rConsulta = new RectangleF(rp.X, topConsulta, rp.Width, fonteSub.AlturaLinha + 1F);
            gfx.DrawString("Chave de acesso para consulta de autenticidade no site www.cte.fazenda.gov.br",
                           rConsulta, fonteSub, AlinhamentoHorizontal.Centro);

            // Linha da chave de acesso completa (logo abaixo)
            if (!string.IsNullOrEmpty(ViewModel.ChaveAcesso))
            {
                var rChave = new RectangleF(rp.X, topConsulta + fonteSub.AlturaLinha + 0.4F, rp.Width, fonteVal.AlturaLinha + 1F);
                gfx.DrawString(Formatador.FormatarChaveAcesso(ViewModel.ChaveAcesso),
                               rChave, Estilo.CriarFonteNegrito(6F), AlinhamentoHorizontal.Centro);
            }

            // Folha em destaque no canto direito do bloco.
            RetanguloNumeroFolhas = new RectangleF(
                rp.X + 0.42F * rp.Width,
                topValor,
                0.10F * rp.Width,
                fonteVal.AlturaLinha);
        }

        private static void DesenhaCelula(Gfx gfx, float x0, float x1, float yLabel, float yValor,
                                          string label, string valor, Fonte fonteLbl, Fonte fonteVal)
        {
            var rL = new RectangleF(x0, yLabel, x1 - x0, fonteLbl.AlturaLinha);
            var rV = new RectangleF(x0, yValor, x1 - x0, fonteVal.AlturaLinha);
            gfx.DrawString(label, rL, fonteLbl, AlinhamentoHorizontal.Centro);
            gfx.DrawString(valor ?? string.Empty, rV, fonteVal, AlinhamentoHorizontal.Centro);
        }
    }
}
