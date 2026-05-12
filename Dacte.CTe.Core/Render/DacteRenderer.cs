using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using PdfSharpCore.Drawing;
using QRCoder;
using Dacte.CTe.Core.Elementos;
using Dacte.CTe.Core.Enumeracoes;
using Dacte.CTe.Core.Graphics;
using Dacte.CTe.Core.Modelo;
using Dacte.CTe.Core.Tools;
using static Dacte.CTe.Core.Render.LayoutConstants;

namespace Dacte.CTe.Core.Render
{
    /// <summary>
    /// Renderer do DACTE em A4 retrato. Desenha os quadros do layout definido pelo MOC v4.00.
    /// </summary>
    internal sealed class DacteRenderer
    {
        private readonly DacteViewModel _vm;
        private readonly Gfx _gfx;
        private readonly Estilo _estilo;
        private readonly XImage _logo;

        // Fontes pré-criadas (reutilizadas em vários quadros).
        private readonly Fonte _fLabel;       // labels de campo (4.5pt, negrito)
        private readonly Fonte _fLabelMin;    // labels pequenos (4pt)
        private readonly Fonte _fValor;       // conteúdo padrão (6pt)
        private readonly Fonte _fValorBold;   // conteúdo em destaque (6.5pt negrito)
        private readonly Fonte _fTitulo;      // "DACTE", "CT-e" (8.5pt negrito)
        private readonly Fonte _fSubtitulo;   // "Documento Auxiliar do..." (5.5pt)
        private readonly Fonte _fCidade;      // cidade origem/destino (10pt negrito)
        private readonly Fonte _fCabecalhoBl; // "COMPONENTES DO VALOR..." (5.5pt negrito)
        private readonly Fonte _fEmitNome;    // razão social emitente (8pt negrito)

        public DacteRenderer(DacteViewModel vm, Gfx gfx, Estilo estilo, XImage logo = null)
        {
            _vm = vm ?? throw new ArgumentNullException(nameof(vm));
            _gfx = gfx ?? throw new ArgumentNullException(nameof(gfx));
            _estilo = estilo ?? throw new ArgumentNullException(nameof(estilo));
            _logo = logo;

            _fLabel       = _estilo.CriarFonteNegrito(4.5F);
            _fLabelMin    = _estilo.CriarFonteNegrito(4.5F);
            _fValor       = _estilo.CriarFonteRegular(6F);
            _fValorBold   = _estilo.CriarFonteRegular(6.0F);
            _fTitulo      = _estilo.CriarFonteRegular(6.3F);
            _fSubtitulo   = _estilo.CriarFonteRegular(5.5F);
            _fCidade      = _estilo.CriarFonteRegular(6F);
            _fCabecalhoBl = _estilo.CriarFonteNegrito(5.0F);
            _fEmitNome    = _estilo.CriarFonteRegular(6F);
        }

        /// <summary>Desenha a folha inteira no XGraphics passado ao construtor.</summary>
        public void Render()
        {
            _gfx.SetLineWidth(0.176F);

            // A chave de acesso e o codigo de barras ficam no bloco superior.
            DesenhaTopo();
            DesenhaTipoServico();
            DesenhaCfopProtocolo();
            DesenhaOrigemDestino();
            DesenhaPessoas();
            DesenhaCargaPesos();
            DesenhaComponentes();
            DesenhaImpostoIbsCbs();
            DesenhaDocumentosOrig();
            DesenhaObservacoes();
            DesenhaModal();
            DesenhaUsoExclusivoFisco();
            DesenhaCanhotoInferior();
        }

        #region Helpers

        /// <summary>Largura útil da folha (entre as margens).</summary>
        private static float W => PAGE_W - 2 * M_X;

        private float ShiftMiolo => _vm.Modal == TipoModal.Rodoviario ? 0F : -4F;

        private static RectangleF Rect(float x, float y, float w, float h) => new RectangleF(x, y, w, h);

        /// <summary>Desenha um quadro: borda + label do cabeçalho no canto superior esquerdo.</summary>
        private void Quadro(RectangleF r, string cabecalho)
        {
            _gfx.DrawRectangle(r);
            if (!string.IsNullOrWhiteSpace(cabecalho))
            {
                var rl = new RectangleF(r.X + 0.7F, r.Y + 0.4F, r.Width - 1.4F, _fLabelMin.AlturaLinha);
                DrawStringFit(cabecalho, rl, _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            }
        }

        /// <summary>Linha de "header" centralizado num retângulo (ex. "COMPONENTES DO VALOR...").</summary>
        private void Cabecalho(RectangleF r, string texto)
        {
            _gfx.DrawRectangle(r);
            var rl = new RectangleF(r.X, r.Y + 0.3F, r.Width, r.Height - 0.6F);
            DrawStringFit(texto, rl, _fCabecalhoBl, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);
        }

        /// <summary>Campo de uma única linha: label pequeno em cima + conteúdo abaixo.</summary>
        private void Campo(RectangleF r, string label, string valor, AlinhamentoHorizontal alinhValor = AlinhamentoHorizontal.Esquerda,
                           Fonte fonteValor = null, float deslocXValor = 0F, float deslocYValor = 0F)
        {
            // Padding interno: 1 mm de respiro entre label e topo da celula.
            const float PADDING_TOP   = 1.0F;
            float paddingLeft = Math.Min(2.0F, Math.Max(0.65F, r.Width * 0.07F));
            const float SPACING_LABEL_VALOR = 0.4F;

            // Label (canto superior esquerdo, sempre maiúscula).
            var rL = new RectangleF(r.X + paddingLeft, r.Y + PADDING_TOP, r.Width - 2 * paddingLeft, _fLabelMin.AlturaLinha);
            if (!string.IsNullOrEmpty(label))
                DrawStringFit(label.ToUpperInvariant(), rL, _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo, 3.9F);

            // Valor logo abaixo do label, com fonte ajustavel pelo chamador.
            if (!string.IsNullOrEmpty(valor))
            {
                var fonte = fonteValor ?? _fValor;
                float yValor = r.Y + PADDING_TOP + _fLabelMin.AlturaLinha + SPACING_LABEL_VALOR;
                float hValor = r.Bottom - yValor - PADDING_TOP;
                // Em células muito estreitas (ex.: linha de pesos compacta),
                // o espaço pro valor pode ficar zero/negativo. Nesse caso,
                // colamos o valor logo abaixo do label sem padding inferior.
                if (hValor <= 0)
                {
                    yValor = r.Y + PADDING_TOP + _fLabelMin.AlturaLinha;
                    hValor = Math.Max(0.5F, r.Bottom - yValor);
                }
                var rV = new RectangleF(r.X + paddingLeft + deslocXValor, yValor + deslocYValor,
                                        r.Width - 2 * paddingLeft - Math.Max(0, deslocXValor), hValor);
                DrawStringFit(valor, rV, fonte, alinhValor, AlinhamentoVertical.Centro, 4.1F);
            }
        }

        private void CampoComUnidade(RectangleF r, string label, string valor, string unidade)
        {
            const float paddingTop = 1.0F;
            float paddingLeft = Math.Min(2.0F, Math.Max(0.65F, r.Width * 0.07F));
            const float spacingLabelValor = 0.4F;
            const float unidadeW = 7.0F;
            const float gap = 0.65F;

            var rL = new RectangleF(r.X + paddingLeft, r.Y + paddingTop, r.Width - 2F * paddingLeft, _fLabelMin.AlturaLinha);
            if (!string.IsNullOrEmpty(label))
                DrawStringFit(label.ToUpperInvariant(), rL, _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo, 3.9F);

            if (string.IsNullOrWhiteSpace(valor))
                return;

            float yValor = r.Y + paddingTop + _fLabelMin.AlturaLinha + spacingLabelValor;
            float hValor = Math.Max(0.5F, r.Bottom - yValor - paddingTop);
            var rUnidade = new RectangleF(r.Right - paddingLeft - unidadeW, yValor, unidadeW, hValor);
            var rValor = new RectangleF(r.X + paddingLeft, yValor, Math.Max(0.5F, rUnidade.X - r.X - paddingLeft - gap), hValor);

            DrawStringFit(valor, rValor, _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro, 4.1F);
            DrawStringFit(unidade, rUnidade, _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro, 4.1F);
        }

        #endregion

        #region Quadros

        private void DesenhaTopo()
        {
            float xTopo = _vm.Modal == TipoModal.Multimodal ? 7.07F : 6.95F;
            var r = Rect(xTopo, Y_TOPO, 203.2F - xTopo, H_TOPO);
            const float raioTopo = 1.6F;

            // Subdivisão das colunas (px relativo à área útil)
            float xCol1 = r.X;
            float xCol2 = _vm.Modal == TipoModal.Multimodal ? 105.50F : 93.66F;
            float xCol3 = _vm.Modal == TipoModal.Multimodal ? 179.58F : 172.28F;
            float xRight = r.Right;

            float hEmitenteTopo = _vm.Modal == TipoModal.Rodoviario ? 24.10F : H_TOPO_EMIT;
            var rEmitente = new RectangleF(xCol1, r.Y, xCol2 - xCol1, hEmitenteTopo);
            var rDacte = new RectangleF(xCol2, r.Y, xCol3 - xCol2, r.Height);
            var rModal = new RectangleF(xCol3, r.Y, xRight - xCol3, r.Height);
            _gfx.DrawRoundedRectangle(rEmitente, raioTopo);
            _gfx.DrawRoundedRectangle(rDacte, raioTopo);
            _gfx.DrawRoundedRectangle(rModal, raioTopo);

            DesenhaTopoEmitente(rEmitente);
            DesenhaTopoDacte(rDacte);
            DesenhaTopoModal(rModal);
        }

        private void DesenhaTopoEmitente(RectangleF r)
        {
            bool rodoviario = _vm.Modal == TipoModal.Rodoviario;
            float dxEmitente = rodoviario ? -1.05F : 0F;
            float dxTituloEmitente = rodoviario ? dxEmitente : 0.75F;
            float dyTituloEmitente = rodoviario ? 0.49F : 0.75F;

            var rTituloEmitente = _logo == null
                ? new RectangleF(r.X + 1F, r.Y + 0.5F + dyTituloEmitente, r.Width - 2F, 3.5F)
                : new RectangleF(r.X + 34F + dxTituloEmitente, r.Y + 0.5F + dyTituloEmitente, r.Width - 34.5F, 3.5F);

            // Header label
            DrawStringFit("IDENTIFICA\u00C7\u00C3O DO EMITENTE",
                            rTituloEmitente,
                            _estilo.CriarFonteRegular(6.5F), AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);

            float yTop = r.Y + 5.1F + (_vm.Modal == TipoModal.Multimodal ? 0.77F : 0.24F);
            float xTexto = r.X + 1F;
            float wLogo = rodoviario ? 37.8F : 33F;

            // Logo opcional à esquerda
            if (_logo != null)
            {
                var rLogo = _vm.Modal == TipoModal.Multimodal
                    ? new RectangleF(11.36F, 12.35F, 28.84F, 7.93F)
                    : new RectangleF(9.24F, 10.23F, 29.64F, 11.38F);
                _gfx.ShowXObjectStretch(_logo, rLogo);
                xTexto = r.X + wLogo + 1F + (_vm.Modal == TipoModal.Multimodal ? 0.76F : 0F);
            }

            float wTexto = r.Right - xTexto - 0.5F;
            var emit = _vm.Emitente;

            // Razão social em destaque
            DrawStringFit(emit.RazaoSocial ?? string.Empty,
                            new RectangleF(xTexto, yTop, wTexto, 4.5F),
                            _fEmitNome, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);

            // Linhas de endereço
            float lh = 3F;
            float y = yTop + 4.5F;

            string cnpjIe = $"CNPJ: {emit.CnpjCpf} - IE: {emit.Ie}";
            string endereco = $"Endereço: {emit.EnderecoLogadrouro}";
            if (!string.IsNullOrEmpty(emit.EnderecoNumero)) endereco += $", {emit.EnderecoNumero}";
            string bairro = $"Bairro: {emit.EnderecoBairro}";
            string cidade = $"Município: {emit.Municipio} - UF:{emit.EnderecoUf}";
            string foneCep = $"FONE: {emit.Telefone} E CEP: {Formatador.FormatarCEP(emit.EnderecoCep)}";

            var linhas = new[] { cnpjIe, endereco, bairro, cidade, foneCep };
            if (rodoviario)
            {
                float[] yOffsets = { 3.18F, 6.88F, 9.79F, 12.70F, 15.61F };
                for (int i = 0; i < linhas.Length; i++)
                {
                    DrawStringFit(linhas[i],
                                    new RectangleF(xTexto, yTop + yOffsets[i], wTexto, lh),
                                    _fValor, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
                }
                return;
            }

            foreach (var linha in linhas)
            {
                DrawStringFit(linha,
                                new RectangleF(xTexto, y, wTexto, lh),
                                _fValor, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
                y += lh;
            }
        }

        private void DesenhaTopoDacte(RectangleF r)
        {
            // "DACTE" em destaque
            float xTitulo = _vm.Modal == TipoModal.Multimodal ? r.X + 0.34F : r.X - 1.10F;
            float yTitulo = _vm.Modal == TipoModal.Multimodal ? r.Y + 0.92F : r.Y + 0.74F;
            DrawStringFit("DACTE",
                            new RectangleF(xTitulo, yTitulo, r.Width, 5F),
                            _fTitulo, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);

            // Subtítulo
            DrawStringFit("Documento Auxiliar do Conhecimento de Transporte Eletrônico",
                            new RectangleF(r.X + 1, r.Y + 3.05F, r.Width - 2, 3F),
                            _estilo.CriarFonteRegular(4.8F), AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);

            // Linha horizontal separando subtítulo / tabela
            float yTab = _vm.Modal == TipoModal.Multimodal ? r.Y + 5.73F : 10.50F;
            _gfx.DrawLine(new PointF(r.X, yTab), new PointF(r.Right, yTab));

            // Tabela MODELO | SÉRIE | NÚMERO | FL | DATA E HORA EMISSAO
            float[] xStops = _vm.Modal == TipoModal.Multimodal
                ? new[] { 118.76F, 128.92F, 142.40F, 150.72F, r.Right }
                : new[] { 106.52F, 117.03F, 133.69F, 143.77F, r.Right };
            string[] labs = { "MODELO", "SÉRIE", "NÚMERO", "FL", "DATA E HORA DE EMISS\u00C3O" };
            string[] vals =
            {
                _vm.Modelo.ToString("00"),
                _vm.Serie.ToString(),
                _vm.Numero.ToString(),
                "1/1",
                _vm.DataHoraEmissao?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty
            };

            float hTab = _vm.Modal == TipoModal.Multimodal ? 6.70F : 6.17F;
            float x = r.X;
            for (int i = 0; i < xStops.Length; i++)
            {
                float w = xStops[i] - x;
                var cel = new RectangleF(x, yTab, w, hTab);
                if (i > 0) _gfx.DrawLine(new PointF(x, yTab), new PointF(x, yTab + hTab));

                if (_vm.Modal == TipoModal.Rodoviario && i == 4 && _vm.DataHoraEmissao.HasValue)
                {
                    DrawStringFit(labs[i],
                                    new RectangleF(144.18F, 11.36F, cel.Right - 144.18F, _fLabelMin.AlturaLinha),
                                    _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                    DrawStringFit(_vm.DataHoraEmissao.Value.ToString("dd/MM/yyyy"),
                                    new RectangleF(144.18F, 14.13F, 13.8F, 3F),
                                    _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                    DrawStringFit(_vm.DataHoraEmissao.Value.ToString("HH:mm:ss"),
                                    new RectangleF(158.20F, 14.11F, cel.Right - 158.20F, 3F),
                                    _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                }
                else
                {
                    DrawStringFit(labs[i],
                                    new RectangleF(cel.X, cel.Y + 1.24F, cel.Width, _fLabelMin.AlturaLinha),
                                    _fLabelMin, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
                    DrawStringFit(vals[i],
                                    new RectangleF(cel.X, cel.Y + 1.95F, cel.Width, cel.Height - 2.20F),
                                    _fValorBold, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);
                }
                x = xStops[i];
            }
            _gfx.DrawLine(new PointF(r.X, yTab + hTab), new PointF(r.Right, yTab + hTab));

            // CONTROLE DO FISCO logo abaixo
            float yFisco = yTab + hTab + (_vm.Modal == TipoModal.Multimodal ? 0.53F : 2.00F);
            float wFisco = _vm.Modal == TipoModal.Multimodal ? 80.16F : 203.20F - r.X;
            DrawStringFit("CONTROLE DO FISCO",
                            new RectangleF(r.X, yFisco, wFisco, _fLabelMin.AlturaLinha),
                            _fLabelMin, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);

            // Barcode e chave dentro da coluna central.
            // Margens internas calibradas para que o barcode caiba SEM tocar
            // a borda inferior do quadro, e que sobre espaço para o texto da
            // URL e da chave 44 dígitos abaixo.
            var rBar = _vm.Modal == TipoModal.Multimodal
                ? new RectangleF(107.93F, 19.25F, 68.04F, 8.90F)
                : new RectangleF(95.97F, 22.70F, 73.30F, 8.70F);
            float yChave = r.Bottom - 5F;       // 2 linhas no rodapé
            yChave = _vm.Modal == TipoModal.Multimodal ? 29.37F : 35.93F;

            if (!string.IsNullOrEmpty(_vm.ChaveAcesso))
            {
                var bar = new Barcode128CScaled(_vm.ChaveAcesso, _estilo, rBar.Width);
                bar.SetSize(rBar.Width, rBar.Height);
                bar.SetPosition(rBar.X, rBar.Y);
                bar.Draw(_gfx);
            }

            float xTextoChave = _vm.Modal == TipoModal.Multimodal ? 107.40F : 97.45F;
            float xValorChave = _vm.Modal == TipoModal.Multimodal ? 107.40F : 101.85F;
            if (_vm.Modal == TipoModal.Multimodal)
            {
                DrawStringFit("Chave de acesso",
                                new RectangleF(xTextoChave, yChave, r.Right - xTextoChave - 1F, 2.5F),
                                _estilo.CriarFonteRegular(4.5F), AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            }
            else
            {
                DrawStringFit("Chave de acesso para consulta de autenticidade no site",
                                new RectangleF(xTextoChave, yChave, r.Right - xTextoChave - 1F, 2.5F),
                                _estilo.CriarFonteRegular(4.5F), AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                float xSiteChave = 147.20F;
                DrawStringFit("www.cte.fazenda.gov.br",
                                new RectangleF(xSiteChave, yChave, r.Right - xSiteChave - 1F, 2.5F),
                                _estilo.CriarFonteNegrito(4.5F), AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            }
            DrawStringFit(_vm.ChaveAcesso ?? string.Empty,
                            new RectangleF(xValorChave, yChave + 2.7F, r.Right - xValorChave - 1F, 3F),
                            _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
        }

        private void DesenhaTopoModal(RectangleF r)
        {
            // MODAL  /  Rodoviario  /  Nº PROTOCOLO  /  <protocolo>  /  INSC. SUFRAMA DEST.  /  QR
            float yModalFim = 10.23F;
            float yInfoFim = _vm.Modal == TipoModal.Multimodal ? 16.93F : 17.11F;
            float lh = _fLabelMin.AlturaLinha;

            DrawStringFit("MODAL",
                            new RectangleF(r.X, r.Y + 0.85F, r.Width, lh),
                            _fLabelMin, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
            DrawStringFit(_vm.Modal.ToString(),
                            new RectangleF(r.X, r.Y + 3.05F, r.Width, 4F),
                            _fValor, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);

            _gfx.DrawLine(new PointF(r.X, yModalFim), new PointF(r.Right, yModalFim));

            if (_vm.Modal == TipoModal.Multimodal)
            {
                DrawStringFit("INSC. SUFRAMA DEST.",
                                new RectangleF(r.X, yModalFim + 1.30F, r.Width, lh),
                                _fLabelMin, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
            }
            else
            {
                DrawStringFit("Nº PROTOCOLO",
                                new RectangleF(r.X, yModalFim + 1.30F, r.Width, lh),
                                _fLabelMin, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
                DrawStringFit(ExtrairNumeroProtocolo(_vm.ProtocoloAutorizacao),
                                new RectangleF(r.X, yModalFim + 4.10F, r.Width, 4F),
                                _fValor, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
            }

            _gfx.DrawLine(new PointF(r.X, yInfoFim), new PointF(r.Right, yInfoFim));

            // QR Code (parte inferior da coluna MODAL). A quiet zone faz parte
            // do símbolo e mantém os módulos pretos afastados das divisórias.
            if (!string.IsNullOrWhiteSpace(_vm.QrCodeUrl))
            {
                const float qrPadding = 2.40F;
                var rQr = new RectangleF(r.X + qrPadding,
                                         yInfoFim + qrPadding,
                                         r.Width - 2F * qrPadding,
                                         r.Bottom - yInfoFim - 2F * qrPadding);
                DesenhaQrCode(rQr, _vm.QrCodeUrl, drawQuietZones: true);
            }
        }

        private void DesenhaQrCode(RectangleF rect, string conteudo, bool drawQuietZones)
        {
            // QRCoder gera bitmap PNG; convertemos para XImage e desenhamos
            // centralizado no retângulo informado (em mm).
            using (var generator = new QRCodeGenerator())
            using (var data = generator.CreateQrCode(conteudo, QRCodeGenerator.ECCLevel.M))
            using (var qr = new PngByteQRCode(data))
            {
                byte[] png = qr.GetGraphic(20, drawQuietZones);
                using (var ms = new MemoryStream(png))
                {
                    var img = XImage.FromStream(() => ms);
                    float lado = Math.Min(rect.Width, rect.Height);
                    var rCentralizado = new RectangleF(rect.X + (rect.Width - lado) / 2F,
                                                       rect.Y + (rect.Height - lado) / 2F,
                                                       lado,
                                                       lado);
                    _gfx.ShowXObjectStretch(img, rCentralizado);
                }
            }
        }

        private static string ExtrairNumeroProtocolo(string protocolo)
        {
            if (string.IsNullOrWhiteSpace(protocolo)) return string.Empty;
            int sp = protocolo.IndexOf(' ');
            return sp > 0 ? protocolo.Substring(0, sp) : protocolo;
        }


        private void DesenhaTipoServico()
        {
            if (_vm.Modal == TipoModal.Multimodal)
            {
                var rm = Rect(7.21F, 29.81F, 98.07F, 7.18F);
                _gfx.DrawRoundedRectangle(rm, 1.6F);

                float[] frac = { 0.18F, 0.27F, 0.27F, 0.28F };
                string[] labs = { "Tipo do CT-e", "Tomador do Servi\u00e7o", "Forma de Pagamento", "Tipo do Servi\u00e7o" };
                string[] vals = { DescTipoCte(_vm.TipoCTe) ?? string.Empty, _vm.PapelTomador ?? string.Empty, _vm.FormaPagamento ?? string.Empty, DescTipoServico(_vm.TipoServico) ?? string.Empty };
                DesenhaLinhaCampos(rm, frac, labs, vals, AlinhamentoHorizontal.Esquerda, true);
                return;
            }

            var r = Rect(6.95F, 28.59F, 93.66F - 6.95F, 15.52F);
            _gfx.DrawRoundedRectangle(r, 1.6F);

            // Layout 2 linhas por 2 colunas:
            //   Linha 1: TIPO DO CT-E         | TIPO DO SERVIÇO
            //   Linha 2: TOMADOR DO SERVIÇO   | FORMA DE PAGAMENTO
            float xMid = r.X + r.Width / 2F;
            float yMid = r.Y + r.Height / 2F;
            _gfx.DrawLine(new PointF(xMid, r.Y), new PointF(xMid, r.Bottom));
            _gfx.DrawLine(new PointF(r.X, yMid), new PointF(r.Right, yMid));

            // Linha 1: TIPO DO CT-E | TIPO DO SERVIÇO
            DrawStringFit("TIPO DO CT-E",
                            new RectangleF(9.51F, 29.46F, r.Width / 2F - 2F, _fLabelMin.AlturaLinha),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(DescTipoCte(_vm.TipoCTe) ?? string.Empty,
                            new RectangleF(9.51F, 32.46F, r.Width / 2F - 2F, 3.5F),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit("TIPO DO SERVIÇO",
                            new RectangleF(52.19F, 29.81F, r.Width / 2F - 2F, _fLabelMin.AlturaLinha),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(DescTipoServico(_vm.TipoServico) ?? string.Empty,
                            new RectangleF(52.19F, 32.81F, r.Width / 2F - 2F, 3.5F),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            // Linha 2: TOMADOR DO SERVIÇO | FORMA DE PAGAMENTO (sempre presentes,
            // mesmo quando o valor esta vazio.
            DrawStringFit("TOMADOR DO SERVIÇO",
                            new RectangleF(9.30F, 37.22F, r.Width / 2F - 2F, _fLabelMin.AlturaLinha),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(_vm.PapelTomador ?? string.Empty,
                            new RectangleF(9.30F, 40.00F, r.Width / 2F - 2F, 3.5F),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit("FORMA DE PAGAMENTO",
                            new RectangleF(52.19F, 37.22F, r.Width / 2F - 2F, _fLabelMin.AlturaLinha),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(_vm.FormaPagamento ?? string.Empty,
                            new RectangleF(52.19F, 40.00F, r.Width / 2F - 2F, 3.5F),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
        }

        private void DesenhaCfopProtocolo()
        {
            if (_vm.Modal == TipoModal.Multimodal)
            {
                var rCfop = Rect(7.07F, 37.41F, 98.28F, 9.40F);
                _gfx.DrawRoundedRectangle(rCfop, 1.6F);
                DrawStringFit("CFOP. NATUREZA DA PRESTACAO",
                                new RectangleF(rCfop.X + 2.54F, rCfop.Y + 1.55F, rCfop.Width - 3.2F, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit(_vm.Cfop ?? string.Empty,
                                new RectangleF(rCfop.X + 2.23F, rCfop.Y + 4.55F, 10F, rCfop.Height - 4.7F),
                                _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
                DrawStringFit(_vm.NaturezaOperacao ?? string.Empty,
                                new RectangleF(22.70F, rCfop.Y + 4.55F, rCfop.Right - 23.40F, rCfop.Height - 4.7F),
                                _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);

                const float xModalMultimodal = 179.58F;
                var rDados = Rect(105.50F, 34.31F, xModalMultimodal - 105.50F, 12.50F);
                _gfx.DrawRoundedRectangle(rDados, 1.6F);
                _gfx.DrawLine(new PointF(rDados.X, 40.23F), new PointF(rDados.Right, 40.23F));
                DrawStringFit("DADOS DO CT-E",
                                new RectangleF(rDados.X + 1.90F, rDados.Y + 1.30F, rDados.Width - 3.2F, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit("Protocolo de Autorizacao de Uso",
                                new RectangleF(rDados.X + 1.80F, 40.23F + 1.05F, rDados.Width - 3.2F, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit(_vm.ProtocoloAutorizacao ?? string.Empty,
                                new RectangleF(rDados.X + 1.80F, 40.23F + 2.58F, rDados.Width - 3.2F, 3.0F),
                                _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                return;
            }

            if (_vm.Modal == TipoModal.Rodoviario)
            {
                var rCfop = Rect(6.95F, Y_CFOP, 93.66F - 6.95F, H_CFOP);
                _gfx.DrawRoundedRectangle(rCfop, 1.6F);
                DrawStringFit("CFOP. NATUREZA DA PRESTACAO",
                                new RectangleF(rCfop.X + 2.35F, rCfop.Y + 1.88F, rCfop.Width - 3.2F, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit(_vm.Cfop ?? string.Empty,
                                new RectangleF(rCfop.X + 2.35F, rCfop.Y + 4.20F, 10F, rCfop.Height - 4.4F),
                                _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
                DrawStringFit(_vm.NaturezaOperacao ?? string.Empty,
                                new RectangleF(22.70F, rCfop.Y + 4.20F, rCfop.Right - 23.40F, rCfop.Height - 4.4F),
                                _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);

                var rProt = Rect(93.66F, Y_CFOP, 156.83F - 93.66F, H_CFOP);
                _gfx.DrawRoundedRectangle(rProt, 1.6F);
                DrawStringFit("PROTOCOLO DE AUTORIZACAO DE USO",
                                new RectangleF(rProt.X + 1.71F, rProt.Y + 1.97F, rProt.Width - 2.4F, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit(_vm.ProtocoloAutorizacao ?? string.Empty,
                                new RectangleF(rProt.X + 1.71F, rProt.Y + 4.05F, rProt.Width - 2.4F, rProt.Height - 4.2F),
                                _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);

                var rSuframa = Rect(156.83F, Y_CFOP, 203.20F - 156.83F, H_CFOP);
                _gfx.DrawRoundedRectangle(rSuframa, 1.6F);
                DrawStringFit("INSC. SUFRAMA DO DESTINATARIO",
                                new RectangleF(rSuframa.X + 2.54F, rSuframa.Y + 1.88F, rSuframa.Width - 3.2F, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit(_vm.Destinatario?.Isuf ?? string.Empty,
                                new RectangleF(rSuframa.X + 2.54F, rSuframa.Y + 4.05F, rSuframa.Width - 3.2F, rSuframa.Height - 4.2F),
                                _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
                return;
            }

            float yCfop = _vm.Modal == TipoModal.Multimodal ? Y_CFOP - 5.8F : Y_CFOP;
            float hCfop = _vm.Modal == TipoModal.Multimodal ? H_CFOP + 5.8F : H_CFOP;
            var r = Rect(M_X, yCfop, W, hCfop);
            _gfx.DrawRectangle(r);

            // Linha do label do header
            DrawStringFit("CFOP. NATUREZA DA PRESTA\u00C7\u00C3O",
                            new RectangleF(r.X + 0.7F, r.Y + 0.3F, r.Width - 1.4F, _fLabelMin.AlturaLinha),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);

            string txt = $"{_vm.Cfop}    {_vm.NaturezaOperacao}".Trim();
            DrawStringFit(txt,
                            new RectangleF(r.X + 0.7F, r.Y + _fLabelMin.AlturaLinha + 0.3F, r.Width - 1.4F, r.Height - _fLabelMin.AlturaLinha - 0.6F),
                            _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);

            // Direita: Protocolo de Autorização de Uso (em uma "subcaixa" pequena)
            float xProt = r.X + 0.62F * r.Width;
            _gfx.DrawLine(new PointF(xProt, r.Y), new PointF(xProt, r.Bottom));
            DrawStringFit("Protocolo de Autorização de Uso",
                            new RectangleF(xProt + 0.7F, r.Y + 0.3F, r.Width * 0.38F - 1.4F, _fLabelMin.AlturaLinha),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(_vm.ProtocoloAutorizacao ?? string.Empty,
                            new RectangleF(xProt + 0.7F, r.Y + _fLabelMin.AlturaLinha + 0.3F, r.Width * 0.38F - 1.4F, r.Height - _fLabelMin.AlturaLinha - 0.6F),
                            _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
        }

        private void DesenhaOrigemDestino()
        {
            bool multimodal = _vm.Modal == TipoModal.Multimodal;
            float yOrigem = multimodal ? 50.06F : 52.50F;
            float hOrigem = multimodal ? 7.76F : 7.43F;
            var rEsq = Rect(7.07F, yOrigem, 98.06F, hOrigem);
            var rDir = Rect(105.83F, yOrigem, 97.37F, hOrigem);
            _gfx.DrawRoundedRectangle(rEsq, 1.6F);
            _gfx.DrawRoundedRectangle(rDir, 1.6F);

            void DrawCidade(RectangleF box, string label, string municipio, string uf)
            {
                DrawStringFit(label,
                                new RectangleF(box.X + 2.45F, box.Y + 1.80F, box.Width - 3.15F, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);

                var rValor = new RectangleF(box.X + 2.45F, box.Y + _fLabelMin.AlturaLinha + 1.40F,
                                            box.Width - 3.15F, box.Height - _fLabelMin.AlturaLinha - 1F);
                // Cidade em destaque e UF em posicao fixa da meia-coluna.
                DrawStringFit(municipio ?? string.Empty, rValor, _fCidade,
                                AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
                DrawStringFit(uf ?? string.Empty,
                                new RectangleF(box.X + 60.9F, rValor.Y, 12F, rValor.Height),
                                _fCidade, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
            }

            DrawCidade(rEsq, "ORIGEM DA PRESTA\u00C7\u00C3O", _vm.InicioPrestacaoMunicipio, _vm.InicioPrestacaoUf);
            DrawCidade(rDir, "DESTINO DA PRESTA\u00C7\u00C3O", _vm.TerminoPrestacaoMunicipio, _vm.TerminoPrestacaoUf);
        }

        private void DesenhaPessoas()
        {
            bool multimodal = _vm.Modal == TipoModal.Multimodal;
            float yPessoas = multimodal ? 57.93F : 60.32F;
            float yBottom = multimodal ? 101.69F : 104.39F;
            float h1 = multimodal ? 16.44F : 17.15F;
            float h2 = multimodal ? 15.00F : 16.09F;
            float yLin1 = yPessoas + h1;
            float yLin2 = yLin1 + h2;

            var rRem = Rect(7.07F, yPessoas, 98.06F, h1);
            var rDest = Rect(105.83F, yPessoas, 97.37F, h1);
            var rExp = Rect(7.07F, yLin1, 98.06F, h2);
            var rRec = Rect(105.83F, yLin1, 97.37F, h2);
            var rTom = Rect(7.07F, yLin2, 196.13F, yBottom - yLin2);

            _gfx.DrawRoundedRectangle(rRem, 1.6F);
            _gfx.DrawRoundedRectangle(rDest, 1.6F);
            _gfx.DrawRoundedRectangle(rExp, 1.6F);
            _gfx.DrawRoundedRectangle(rRec, 1.6F);
            _gfx.DrawRoundedRectangle(rTom, 1.6F);

            DesenhaPessoa(rRem, "REMETENTE", _vm.Remetente);
            DesenhaPessoa(rDest, "DESTINATÁRIO", _vm.Destinatario);
            DesenhaPessoa(rExp, "EXPEDIDOR", _vm.Expedidor);
            DesenhaPessoa(rRec, "RECEBEDOR", _vm.Recebedor);

            // Tomador (linha completa, mas com Município/CEP na metade direita)
            DesenhaTomador(rTom);
        }

        private void DesenhaPessoa(RectangleF r, string titulo, EmpresaViewModel emp)
        {
            bool colunaEsquerda = Math.Abs(r.X - M_X) < 0.1F;
            float xCol = r.X + 2.10F;
            float yL = r.Y + (_vm.Modal == TipoModal.Multimodal ? 1.16F : 1.34F);
            float rowStep = Math.Min(2.95F, Math.Max(2.25F, (r.Height - 2.26F) / 5F));
            float lhLabel = _fLabelMin.AlturaLinha;
            float lhValor = Math.Min(3.0F, rowStep + 0.18F);
            float vw = r.Width - 1.4F;

            // Linha 1: TÍTULO + nome (nome em fonte Verdana negrito 6.5pt
            // exige um salto vertical de ~lhValor para não colar na próxima
            // linha "ENDEREÇO").
            DrawStringFit(titulo, new RectangleF(xCol, yL, vw, lhLabel),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            float xNome = r.X + (colunaEsquerda ? 17.20F : 20.00F);
            DrawStringFit(emp?.RazaoSocial ?? string.Empty,
                            new RectangleF(xNome, yL, r.Right - xNome - 0.5F, lhValor),
                            _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            yL += rowStep;

            // Linha 2: endereco com bairro concatenado.
            DrawStringFit("ENDEREÇO", new RectangleF(xCol, yL, 18F, lhLabel),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(EnderecoPessoa(emp),
                            new RectangleF(xNome, yL, r.Right - xNome - 0.5F, lhValor),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            yL += rowStep;

            // Linha 3: MUNICIPIO (sem UF — UF aparece em linha separada abaixo) + CEP
            DrawStringFit("MUNICÍPIO", new RectangleF(xCol, yL, 18F, lhLabel),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(emp?.Municipio ?? string.Empty,
                            new RectangleF(xNome, yL, r.Right - xNome - 30F, lhValor),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit("CEP", new RectangleF(r.Right - 30F, yL, 6F, lhLabel),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(Formatador.FormatarCEP(emp?.EnderecoCep) ?? string.Empty,
                            new RectangleF(r.Right - 24F, yL, 23F, lhValor),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            yL += rowStep;

            // Linha 4: CNPJ/CPF + INSC. ESTADUAL
            DrawStringFit("CNPJ / CPF", new RectangleF(xCol, yL, 18F, lhLabel),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(FormataCpfCnpjSeguro(emp?.CnpjCpf),
                            new RectangleF(xNome, yL, 35F, lhValor),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);

            float xIE = xNome + 35F;
            DrawStringFit("INSC. ESTADUAL", new RectangleF(xIE, yL, 18F, lhLabel),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(emp?.Ie ?? string.Empty,
                            new RectangleF(xIE + 18F, yL, r.Right - xIE - 18F - 0.5F, lhValor),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            yL += rowStep;

            // Linha 5: UF + PAIS + FONE
            DrawStringFit("UF", new RectangleF(xCol, yL, 6F, lhLabel),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(emp?.EnderecoUf ?? string.Empty,
                            new RectangleF(xCol + 6F, yL, 8F, lhValor),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);

            DrawStringFit("PAIS", new RectangleF(xNome, yL, 8F, lhLabel),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(emp?.EnderecoPais ?? string.Empty,
                            new RectangleF(xNome + 8F, yL, 22F, lhValor),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);

            DrawStringFit("FONE", new RectangleF(r.Right - 29.2F, yL, 8F, lhLabel),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(emp?.Telefone ?? string.Empty,
                            new RectangleF(r.Right - 21.2F, yL, 20.2F, lhValor),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
        }

        private void DesenhaTomador(RectangleF r)
        {
            // Tomador ocupa toda a largura. Linha vertical separa "esquerda"
            // (Tomador + Endereço + CNPJ/IE/FONE) e "direita" (Município/UF/PAIS/CEP).
            var emp = _vm.Tomador;
            float xMid = r.X + 99.13F;
            _gfx.DrawLine(new PointF(xMid, r.Y), new PointF(xMid, r.Bottom));

            float lhLbl = _fLabelMin.AlturaLinha;
            float lhVal = 3.0F;
            float wLblTomador = 15.10F;

            // Coluna esquerda.
            float xL = r.X + 2.10F;
            float yL = r.Y + (_vm.Modal == TipoModal.Multimodal ? 1.55F : 1.45F);
            float rowTom = Math.Min(3.05F, Math.Max(2.65F, (r.Height - 2.10F) / 3F));

            // Linha 1: TOMADOR DO SERVIÇO  +  razão social
            DrawStringFit("TOMADOR DO SERVIÇO",
                            new RectangleF(xL, yL, 25.30F, lhLbl),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            string nomeTomador = (_vm.Modal == TipoModal.Rodoviario &&
                                  string.Equals(emp?.RazaoSocial, emp?.NomeFantasia, StringComparison.OrdinalIgnoreCase))
                ? emp?.RazaoSocial
                : (FormataNomeTomador(emp) ?? _vm.PapelTomador ?? string.Empty);
            DrawStringFit(nomeTomador,
                            new RectangleF(xL + 25.30F, yL, xMid - xL - 25.30F, lhVal),
                            _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            yL += rowTom;

            // Linha 2: ENDEREÇO (não inclui bairro pois ele é mostrado à direita)
            DrawStringFit("ENDEREÇO",
                            new RectangleF(xL, yL, wLblTomador, lhLbl),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(EnderecoTomador(emp),
                            new RectangleF(xL + wLblTomador, yL, xMid - xL - wLblTomador, lhVal),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            yL += rowTom;

            // Linha 3: CNPJ/CPF + INSC. ESTADUAL + FONE
            DrawStringFit("CNPJ / CPF",
                            new RectangleF(xL, yL, wLblTomador, lhLbl),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(FormataCpfCnpjSeguro(emp?.CnpjCpf),
                            new RectangleF(xL + wLblTomador, yL, 30F, lhVal),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            float xIE = xL + 47.70F;
            DrawStringFit("INSC. ESTADUAL",
                            new RectangleF(xIE, yL, 18F, lhLbl),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            float xFone = xMid - 11.00F;
            DrawStringFit(emp?.Ie ?? string.Empty,
                            new RectangleF(xIE + 18F, yL, xFone - xIE - 18.5F, lhVal),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit("FONE",
                            new RectangleF(xFone, yL, 8F, lhLbl),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            float wFone = xMid - xFone - 8F;
            if (wFone > 8F && !string.IsNullOrWhiteSpace(emp?.Telefone))
            {
                DrawStringFit(emp.Telefone,
                                new RectangleF(xFone + 8F, yL, wFone, lhVal),
                                _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            }

            // Coluna direita.
            float xR = xMid + 2.10F;
            float yR = r.Y + (_vm.Modal == TipoModal.Multimodal ? 1.55F : 1.55F);

            // Linha 1: MUNICÍPIO + CEP
            DrawStringFit("MUNICÍPIO",
                            new RectangleF(xR, yR, 18F, lhLbl),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(emp?.Municipio ?? string.Empty,
                            new RectangleF(xR + 17.53F, yR, r.Right - xR - 42F, lhVal),
                            _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit("CEP",
                            new RectangleF(r.Right - 25F, yR, 5F, lhLbl),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(Formatador.FormatarCEP(emp?.EnderecoCep) ?? string.Empty,
                            new RectangleF(r.Right - 20F, yR, 19F, lhVal),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            yR += rowTom;

            // Linha 2: UF + PAIS + (vazio)
            DrawStringFit("UF",
                            new RectangleF(xR, yR, 6F, lhLbl),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(emp?.EnderecoUf ?? string.Empty,
                            new RectangleF(xR + 6F, yR, 8F, lhVal),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit("PAIS",
                            new RectangleF(xR + 18F, yR, 8F, lhLbl),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(emp?.EnderecoPais ?? string.Empty,
                            new RectangleF(xR + 26F, yR, r.Right - xR - 28F, lhVal),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
        }

        private void DesenhaCargaPesos()
        {
            float yCarga = _vm.Modal == TipoModal.Rodoviario ? 104.76F : 101.94F;
            float hCarga = _vm.Modal == TipoModal.Rodoviario ? 19.11F : 17.39F;
            var r = Rect(M_X, yCarga, W, hCarga);
            _gfx.DrawRoundedRectangle(r, 1.6F);
            float h1 = _vm.Modal == TipoModal.Multimodal ? 6.10F : 7.10F;
            _gfx.DrawLine(new PointF(r.X, r.Y + h1), new PointF(r.Right, r.Y + h1));

            // Linha 1: PRODUTO PREDOMINANTE | OUTRAS CARACTERÍSTICAS DA CARGA | VALOR TOTAL DA MERCADORIA
            // (rendering manual — não usar DesenhaLinhaCampos para evitar duplicação).
            bool ehRodo = _vm.Modal == TipoModal.Rodoviario;
            float[] frac1 = ehRodo
                ? new[] { 0.43F, 0.285F, 0.285F }
                : new[] { 0.345F, 0.361F, 0.294F };
            string[] labs1 = ehRodo
                ? new[] { "Produto Predominante", "Outras Caracts. Carga", "Vl. Total da Mercadoria" }
                : new[] { "Produto Predominante", "Outras Características da Carga", "Valor Total da Mercadoria" };
            string[] vals1 = { _vm.Carga.ProdutoPredominante, _vm.Carga.OutrasCaracteristicas, FormataMoeda(_vm.Carga.ValorTotalCarga) };
            AlinhamentoHorizontal[] al1 = { AlinhamentoHorizontal.Esquerda, AlinhamentoHorizontal.Esquerda, AlinhamentoHorizontal.Esquerda };

            float x = r.X;
            for (int i = 0; i < frac1.Length; i++)
            {
                float w = frac1[i] * r.Width;
                if (i > 0) _gfx.DrawLine(new PointF(x, r.Y), new PointF(x, r.Y + h1));
                float dxValorCarga = i == 2 ? (ehRodo ? 0F : -1.8F) : 0F;
                float dyValorCarga = i == 2 ? 0.4F : 0F;
                Campo(new RectangleF(x, r.Y, w, h1), labs1[i], vals1[i] ?? string.Empty, al1[i],
                      deslocXValor: dxValorCarga, deslocYValor: dyValorCarga);
                x += w;
            }

            // Linha 2: pesos e volumes da carga.
            string lb1Peso = ehRodo ? "Peso Bruto (Kg)" : "Peso Bruto";
            string lb2 = ehRodo ? "Peso Base Calc. (Kg)" : "Peso Cubado";
            string lb3 = ehRodo ? "Peso Aferido (Kg)" : "Peso Taxado";

            // No rodoviario, peso aferido e cubagem ficam em branco neste quadro.
            decimal? pesoTax = ehRodo ? null : _vm.Carga.PesoAferidoKg;
            decimal? cubagem = ehRodo ? null : _vm.Carga.Cubagem;
            var seguro = _vm.Seguro;

            float[] frac2 = ehRodo
                ? new[] { 0.093F, 0.127F, 0.117F, 0.085F, 0.125F, 0.453F }
                : new[] { 0.1345F, 0.1129F, 0.0968F, 0.0807F, 0.1172F, 0.4579F };
            string[] labs2 = { lb1Peso, lb2, lb3, "Cubagem (M3)", "Qtd Volumes (Unid)", "Nome da Seguradora" };
            string[] vals2 =
            {
                FormataNum(_vm.Carga.PesoBrutoKg, 2),
                FormataNum(_vm.Carga.PesoBaseCalculoKg, 2),
                FormataNum(pesoTax, 2),
                FormataNum(cubagem, 4),
                FormataNum(_vm.Carga.QuantidadeVolumes, 0),
                seguro?.NomeSeguradora ?? string.Empty
            };

            float yL2 = r.Y + h1;
            float hL2 = r.Height - h1;
            if (ehRodo)
            {
                float xPeso = r.X;
                float[] dxPesoRodo = { 4.10F, 7.85F, 7.00F, 0F, 10.50F, 0F };
                for (int i = 0; i < frac2.Length; i++)
                {
                    float wPeso = frac2[i] * r.Width;
                    if (i > 0) _gfx.DrawLine(new PointF(xPeso, yL2), new PointF(xPeso, r.Bottom));
                    Campo(new RectangleF(xPeso, yL2, wPeso, hL2),
                          labs2[i], vals2[i], AlinhamentoHorizontal.Esquerda,
                          deslocXValor: dxPesoRodo[i],
                          deslocYValor: i == 2 ? 0.23F : 0F);
                    xPeso += wPeso;
                }
            }
            else
            {
                float xPeso = r.X;
                for (int i = 0; i < frac2.Length; i++)
                {
                    float wPeso = frac2[i] * r.Width;
                    if (i > 0) _gfx.DrawLine(new PointF(xPeso, yL2), new PointF(xPeso, r.Bottom));
                    float dxPeso = i == 0 ? 5.18F : 0F;
                    float dxValorPeso = i == 4 ? 10.20F : 0F;
                    var rPeso = new RectangleF(xPeso + dxPeso, yL2, wPeso - dxPeso, hL2);
                    if (i <= 2)
                    {
                        CampoComUnidade(rPeso, labs2[i], vals2[i], "KG");
                    }
                    else
                    {
                        Campo(rPeso, labs2[i], vals2[i], AlinhamentoHorizontal.Esquerda,
                              deslocXValor: dxValorPeso);
                    }
                    xPeso += wPeso;
                }
            }

            // Sub-linha "Responsável" / "Número da Apólice" / "Número da Averbação" dentro da última coluna.
            float xSeg = r.X + (frac2[0] + frac2[1] + frac2[2] + frac2[3] + frac2[4]) * r.Width;
            float wSeg = frac2[5] * r.Width;
            float ySub = yL2 + 3.4F;
            _gfx.DrawLine(new PointF(xSeg, ySub), new PointF(r.Right, ySub));
            float[] subFrac = { 0.385F, 0.297F, 0.318F };
            string[] subLab = { "Responsável", "Nr. Apólice", "Nr. Averbação" };
            string[] subVal =
            {
                seguro?.Responsavel ?? string.Empty,
                seguro?.NumeroApolice ?? string.Empty,
                seguro?.NumeroAverbacao ?? string.Empty
            };
            float xs = xSeg;
            for (int i = 0; i < subFrac.Length; i++)
            {
                float ww = subFrac[i] * wSeg;
                if (i > 0) _gfx.DrawLine(new PointF(xs, ySub), new PointF(xs, r.Bottom));
                Campo(new RectangleF(xs, ySub, ww, r.Bottom - ySub), subLab[i], subVal[i]);
                xs += ww;
            }
        }

        private void DesenhaComponentes()
        {
            float yComp = _vm.Modal == TipoModal.Rodoviario ? 124.69F : 119.68F;
            float hComp = _vm.Modal == TipoModal.Rodoviario ? 20.55F : 20.53F;
            var r = Rect(M_X, yComp, W, hComp);
            _gfx.DrawRoundedRectangle(r, 1.6F);

            float yCab = r.Y + 3F;
            _gfx.DrawLine(new PointF(r.X, yCab), new PointF(r.Right, yCab));
            DrawStringFit("COMPONENTES DO VALOR DA PRESTA\u00C7\u00C3O DE SERVIÇO",
                            new RectangleF(r.X, r.Y + 0.3F, r.Width, 3F),
                            _fLabel, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);

            // Tabela 4 colunas (Nome | Valor) + coluna direita de totais.
            float xRight = r.X + 0.735F * r.Width;
            _gfx.DrawLine(new PointF(xRight, yCab), new PointF(xRight, r.Bottom));

            const int colsPorLinha = 4;
            const int linhas = 5;
            float hLin = 2.05F;
            float[] fracCols = { 0.185F, 0.183F, 0.183F, 0.184F };
            float[] xCols = new float[colsPorLinha];
            float[] wCols = new float[colsPorLinha];
            float xColAtual = r.X;
            for (int c = 0; c < colsPorLinha; c++)
            {
                xCols[c] = xColAtual;
                wCols[c] = fracCols[c] * r.Width;
                xColAtual += wCols[c];
            }

            // Header das 4 colunas (NOME | VALOR repetido)
            for (int c = 0; c < colsPorLinha; c++)
            {
                float xC = xCols[c];
                float wCol = wCols[c];
                if (c > 0) _gfx.DrawLine(new PointF(xC, yCab), new PointF(xC, r.Bottom));
                DrawStringFit(_vm.Modal == TipoModal.Multimodal ? "NOME" : "Nome",
                                new RectangleF(xC + 0.5F, yCab + 1F, wCol * 0.55F, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit(_vm.Modal == TipoModal.Multimodal ? "VALOR" : "Valor",
                                new RectangleF(xC + wCol * 0.46F, yCab + 1F, wCol * 0.42F, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Direita, AlinhamentoVertical.Topo);
            }

            // Conteudo distribuido por linha.
            var comp = _vm.Componentes ?? new List<ComponenteValorViewModel>();

            // Fonte do componente — reduzida para garantir que caiba na célula
            // mesmo com nomes longos como "EXCEDENTE_PESO" / "TAXAS_DIVERSAS".
            var fNome = _estilo.CriarFonteRegular(5.0F);

            // Gap horizontal evita que o texto cole na proxima celula.
            const float GAP_HORIZ = 0.9F;

            for (int i = 0; i < colsPorLinha * linhas && i < comp.Count; i++)
            {
                int row = i / colsPorLinha;
                int col = i % colsPorLinha;
                float xC = xCols[col];
                float wCol = wCols[col];
                float yL = yCab + 5.22F + row * hLin;

                // Sub-célula útil (com gap em cada lado).
                float xCelula = xC + GAP_HORIZ;
                float wCelula = wCol - 2 * GAP_HORIZ;

                // Nome 55% à esquerda, valor 40% à direita, 5% de gap no meio.
                float xNomeComp = xCelula;
                float wNome = wCelula * 0.58F;
                float wValor = wCelula * 0.36F;

                DrawStringFit((comp[i].Nome ?? string.Empty).Replace('_', ' '),
                                new RectangleF(xNomeComp, yL, wNome, hLin),
                                fNome, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
                DrawStringFit(comp[i].Valor.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture),
                                new RectangleF(xCelula + wCelula - wValor, yL, wValor, hLin),
                                fNome, AlinhamentoHorizontal.Direita, AlinhamentoVertical.Centro);
            }

            // Coluna direita: VALOR TOTAL + VALOR A RECEBER
            float yMid = yCab + 8F;
            _gfx.DrawLine(new PointF(xRight, yMid), new PointF(r.Right, yMid));
            bool componentesMultimodal = _vm.Modal == TipoModal.Multimodal;
            Campo(new RectangleF(xRight, yCab, r.Right - xRight, yMid - yCab),
                  componentesMultimodal ? "VALOR TOTAL DO SERVIÇO" : "Valor Total do Serviço",
                  FormataMoeda(_vm.ValorTotalPrestacao), AlinhamentoHorizontal.Esquerda);
            Campo(new RectangleF(xRight, yMid, r.Right - xRight, r.Bottom - yMid),
                  componentesMultimodal ? "VALOR TOTAL A RECEBER" : "Valor a Receber",
                  FormataMoeda(_vm.ValorReceber), AlinhamentoHorizontal.Esquerda);
        }

        private void DesenhaImpostoIbsCbs()
        {
            bool impostoMultimodal = _vm.Modal == TipoModal.Multimodal;
            var imp = _vm.Imposto ?? new ImpostoViewModel();
            bool usaIbsCbs = imp.TemIbsCbs;

            float yImp = impostoMultimodal ? 140.21F : 145.70F;
            float hImp = impostoMultimodal ? 10.99F : 11.01F;
            var r = Rect(M_X, yImp, W, hImp);
            _gfx.DrawRoundedRectangle(r, 1.6F);

            float yCab = r.Y + (impostoMultimodal ? 3.62F : 3F);
            _gfx.DrawLine(new PointF(r.X, yCab), new PointF(r.Right, yCab));
            DrawStringFit("INFORMACOES RELATIVAS AO IMPOSTO",
                            new RectangleF(r.X, r.Y + 0.3F, r.Width, 3F),
                            _fLabel, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);

            float[] frac;
            string[] labs;
            string[] vals;

            if (usaIbsCbs)
            {
                // Layout estendido (Reforma Tributária 2026): 11 colunas com IBS/CBS.
                frac = new[]
                {
                    0.160F, // Situação Tributária
                    0.116F, // Base Cálc IBS/CBS
                    0.065F, // Alíq IBS
                    0.065F, // Valor IBS
                    0.064F, // Alíq CBS
                    0.061F, // Valor CBS
                    0.103F, // Base Cálc ICMS
                    0.064F, // Alíq ICMS
                    0.080F, // Valor ICMS
                    0.128F, // %Red Base Calc
                    0.094F  // ICMS Subst
                };
                labs = new[]
                {
                    "Situação Tributária", "Base Cálculo IBS/CBS", "Aliq. IBS", "Valor IBS",
                    "Aliq. CBS", "Valor CBS", "Base Cálculo ICMS", "Aliq. ICMS", "Valor ICMS",
                    "%Red. Base Calc.", "ICMS Subst."
                };
                vals = new[]
                {
                    DescCST(imp.SituacaoTributaria),
                    FormataMoeda(imp.BaseCalculoIbsCbs),
                    FormataNum(imp.AliquotaIbs, 2),
                    FormataMoeda(imp.ValorIbs),
                    FormataNum(imp.AliquotaCbs, 2),
                    FormataMoeda(imp.ValorCbs),
                    FormataMoeda(imp.BaseCalculo),
                    FormataNum(imp.Aliquota, 0),
                    FormataMoeda(imp.Valor),
                    FormataNum(imp.PercentualReducaoBC ?? 0, 2),
                    FormataMoeda(imp.ValorIcmsSubstituicao)
                };
            }
            else
            {
                // Layout clássico (somente ICMS) — usado em CT-e antigo.
                frac = new[] { 0.507F, 0.102F, 0.090F, 0.080F, 0.128F, 0.093F };
                labs = new[] { "Situação Tributária", "Base Cálculo", "Aliq. ICMS", "Valor ICMS", "%Red. Base Calc.", "ICMS Subst." };
                vals = new[]
                {
                    DescCST(imp.SituacaoTributaria),
                    FormataMoeda(imp.BaseCalculo),
                    FormataNum(imp.Aliquota, 0),
                    FormataMoeda(imp.Valor),
                    FormataNum(imp.PercentualReducaoBC ?? 0, 2),
                    FormataMoeda(imp.ValorIcmsSubstituicao)
                };
            }

            float dxValorImp = usaIbsCbs ? -2.70F : 0F;
            float dyValorImp = usaIbsCbs ? 1.15F : 0.53F;
            DesenhaLinhaCampos(new RectangleF(r.X, yCab, r.Width, r.Bottom - yCab),
                               frac, labs, vals, AlinhamentoHorizontal.Direita,
                               primeiraColunaEsquerda: true,
                               deslocXValor: dxValorImp,
                               deslocYValor: dyValorImp,
                               deslocarPrimeiraColuna: false,
                               deslocarYPrimeiraColuna: usaIbsCbs);
        }

        private void DesenhaDocumentosOrig()
        {
            float yDocs = _vm.Modal == TipoModal.Multimodal ? 153.46F : 156.70F;
            float hDocs = _vm.Modal == TipoModal.Multimodal ? 11.11F : 12.35F;
            var r = Rect(M_X, yDocs, W, hDocs);
            _gfx.DrawRoundedRectangle(r, 1.6F);
            float yCab = r.Y + 3F;
            _gfx.DrawLine(new PointF(r.X, yCab), new PointF(r.Right, yCab));
            DrawStringFit("DOCUMENTOS ORIGINÁRIOS",
                            new RectangleF(r.X, r.Y + 0.3F, r.Width, 3F),
                            _fLabel, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);

            float xMid = r.X + r.Width / 2F;
            _gfx.DrawLine(new PointF(xMid, yCab), new PointF(xMid, r.Bottom));

            void DesenhaCol(RectangleF box, DocumentoOrigViewModel d)
            {
                float[] f = { 0.11F, 0.20F, 0.69F };
                string terceiroLabel = _vm.Modal == TipoModal.Rodoviario ? "SÉRIE / Nº DOCUMENTO" : "SÉRIE/NRO. DOCUMENTO";
                string[] labs = { "TP. DOC", "CNPJ/CPF EMITENTE", terceiroLabel };
                string[] vals;
                if (d == null)
                {
                    vals = new[] { string.Empty, string.Empty, string.Empty };
                }
                else
                {
                    // Quando infOutros nao traz CNPJ proprio, usa o remetente.
                    string cnpj = !string.IsNullOrWhiteSpace(d.CnpjCpfEmitente)
                        ? d.CnpjCpfEmitente
                        : _vm.Remetente?.CnpjCpf;

                    vals = new[]
                    {
                        (_vm.Modal == TipoModal.Rodoviario && d.Tipo == TipoDocumentoOrig.Outros) ? "NFe Chav" : AbrTipo(d.Tipo),
                        cnpj ?? string.Empty,
                        !string.IsNullOrEmpty(d.Numero) ? d.Numero
                            : (!string.IsNullOrEmpty(d.Identificacao) ? d.Identificacao : string.Empty)
                    };
                }

                float x = box.X;
                for (int i = 0; i < f.Length; i++)
                {
                    float w = f[i] * box.Width;
                    if (i > 0) _gfx.DrawLine(new PointF(x, box.Y), new PointF(x, box.Bottom));

                    var cel = new RectangleF(x, box.Y, w, box.Height);
                    float padDoc = _vm.Modal == TipoModal.Multimodal
                        ? (i == 0 ? 0.80F : (i == 1 ? 1.00F : 1.20F))
                        : 0.40F;
                    var rLab = new RectangleF(cel.X + padDoc, cel.Y + 1F, cel.Width - padDoc - 0.4F, _fLabelMin.AlturaLinha);
                    float yVal = cel.Y + (_vm.Modal == TipoModal.Multimodal ? 3.5F : 4F);
                    var rVal = new RectangleF(cel.X + padDoc, yVal, cel.Width - padDoc - 0.4F, cel.Height - 4.5F);
                    var alinhDoc = _vm.Modal == TipoModal.Multimodal ? AlinhamentoHorizontal.Esquerda : AlinhamentoHorizontal.Centro;
                    DrawStringFit(labs[i], rLab, _fLabelMin, alinhDoc, AlinhamentoVertical.Topo);
                    string valorDoc = vals[i] ?? string.Empty;
                    Fonte fonteValorDoc = i == 2 && valorDoc.Length >= 40 && valorDoc.All(char.IsDigit)
                        ? _estilo.CriarFonteRegular(_vm.Modal == TipoModal.Multimodal ? 4.1F : 4.0F)
                        : _fValor;
                    float minValorDoc = i == 2 ? 3.2F : 4.0F;
                    DrawStringFit(valorDoc, rVal, fonteValorDoc, alinhDoc, AlinhamentoVertical.Centro, minValorDoc, ellipsis: false);
                    x += w;
                }
            }

            var docs = _vm.DocumentosOrigem ?? new List<DocumentoOrigViewModel>();
            DesenhaCol(new RectangleF(r.X, yCab, xMid - r.X, r.Bottom - yCab),
                       docs.ElementAtOrDefault(0));
            DesenhaCol(new RectangleF(xMid, yCab, r.Right - xMid, r.Bottom - yCab),
                       docs.ElementAtOrDefault(1));
        }

        private void DesenhaObservacoes()
        {
            var r = _vm.Modal == TipoModal.Multimodal
                ? Rect(M_X, 166.85F, W, 29.23F)
                : Rect(M_X, 169.47F, W, 31.38F);
            _gfx.DrawRoundedRectangle(r, 1.6F);
            float yCab = r.Y + 3F;
            _gfx.DrawLine(new PointF(r.X, yCab), new PointF(r.Right, yCab));
            DrawStringFit("OBSERVAÇÕES",
                            new RectangleF(r.X, r.Y + 0.3F, r.Width, 3F),
                            _fLabel, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);

            // Observacoes gerais do CT-e.
            var fonteObs = _estilo.CriarFonteRegular(6F);
            string usoEmissorObs = _vm.UsoExclusivoEmissor ?? string.Empty;
            string ramoObs = string.Empty;
            if (_vm.Modal == TipoModal.Multimodal &&
                usoEmissorObs.StartsWith("RAMO:", StringComparison.OrdinalIgnoreCase))
            {
                ramoObs = usoEmissorObs.Substring(5).Trim();
            }

            if (!string.IsNullOrWhiteSpace(ramoObs))
            {
                DrawStringFit(ramoObs,
                                new RectangleF(r.X + 1F, yCab + 1.85F, r.Width - 2F, 4F),
                                fonteObs, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            }

            float yObsTexto = _vm.Modal == TipoModal.Multimodal ? yCab + 5.57F : yCab + 5.7F;
            var rTxt = new RectangleF(r.X + 1F, yObsTexto, r.Width - 2F, 4F);
            DrawStringFit(_vm.ObservacoesGerais ?? string.Empty, rTxt, fonteObs,
                            AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);

            // FISCAIS — separador fixo entre xObs e o bloco Local Entrega.
            if (_vm.Modal == TipoModal.Rodoviario)
            {
                DrawStringFit("FISCAIS",
                                new RectangleF(r.X + 1F, yCab + 8.2F, 30F, 3F),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            }

            if (_vm.Modal == TipoModal.Rodoviario)
            {
                // Rodape do quadro Observacoes.
                float yFooter = r.Bottom - 14.5F;
                var localEntrega = _vm.LocalEntrega ?? _vm.Destinatario;
                string entrega =
                    $"CEP: {Formatador.FormatarCEP(localEntrega?.EnderecoCep)} - " +
                    $"End: {localEntrega?.EnderecoLinha1} - " +
                    $"Cidade: {localEntrega?.Municipio} - UF: {localEntrega?.EnderecoUf}";
                string responsavel = $"{_vm.Emitente?.CnpjCpf}    {_vm.Emitente?.NomeFantasia ?? _vm.Emitente?.RazaoSocial}";

                DrawStringFit("Local de Entrega",
                                new RectangleF(r.X + 1F, yFooter, 40F, 3F),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit(entrega,
                                new RectangleF(r.X + 1F, yFooter + 2.5F, r.Width - 2F, 3F),
                                _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit("Responsável:",
                                new RectangleF(r.X + 1F, yFooter + 5F, 30F, 3F),
                                _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit(responsavel,
                                new RectangleF(r.X + 1F, yFooter + 7.5F, r.Width - 2F, 3F),
                                _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            }
        }

        private void DesenhaModal()
        {
            const float xRodape = 7F;
            const float wRodape = 196F;
            float hModal = _vm.Modal == TipoModal.Rodoviario ? 8.5F : 22.56F;
            var r = _vm.Modal == TipoModal.Rodoviario
                ? Rect(xRodape, Y_MODAL + ShiftMiolo - 0.41F, wRodape, hModal)
                : Rect(M_X, 196.53F, W, hModal);
            _gfx.DrawRoundedRectangle(r, 1.6F);
            float yCab = _vm.Modal == TipoModal.Rodoviario ? r.Y + 3F : r.Y + 2.7F;
            _gfx.DrawLine(new PointF(r.X, yCab), new PointF(r.Right, yCab));

            string titulo = _vm.Modal switch
            {
                TipoModal.Rodoviario => "INFORMAÇÕES ESPECÍFICAS DO MODAL RODOVIÁRIO - CARGA FRACIONADA",
                TipoModal.Aereo      => "INFORMAÇÕES ESPECÍFICAS DO MODAL AÉREO",
                TipoModal.Aquaviario => "INFORMAÇÕES ESPECÍFICAS DO MODAL AQUAVIÁRIO",
                TipoModal.Ferroviario=> "INFORMAÇÕES ESPECÍFICAS DO MODAL FERROVIÁRIO",
                TipoModal.Dutoviario => "INFORMAÇÕES ESPECÍFICAS DO MODAL DUTOVIÁRIO",
                _ => "INFORMAÇÕES ESPECÍFICAS DO MODAL AÉREO"
            };
            float dxTituloModal = _vm.Modal == TipoModal.Rodoviario ? 0.23F : -0.10F;
            float dyTituloModal = _vm.Modal == TipoModal.Rodoviario ? 0.29F : 0.35F;
            DrawStringFit(titulo,
                            new RectangleF(r.X + dxTituloModal, r.Y + 0.3F + dyTituloModal, r.Width, 3F),
                            _fCabecalhoBl, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);

            if (_vm.Modal == TipoModal.Rodoviario)
            {
                // 4 colunas estreitas + declaracao rodoviaria.
                float[] frac = { 0.132F, 0.053F, 0.080F, 0.176F, 0.559F };
                string[] labs = { "RNTRC da Empresa", "CIOT", "Lotação", "Data Prevista da Entrega",
                                  "Esse Conhecimento de Transporte Atende a Legislação de Transporte Rodoviário em Vigor" };
                string lotacao = _vm.Rodoviario?.Lotacao == true
                    ? "Sim"
                    : (_vm.Rodoviario?.Lotacao == false ? "Nao" : string.Empty);
                string[] vals = { _vm.Rodoviario?.Rntrc ?? string.Empty, _vm.Rodoviario?.Ciot ?? string.Empty, lotacao,
                                  _vm.DataPrevistaEntrega ?? string.Empty,
                                  "Sim" };
                DesenhaLinhaCampos(new RectangleF(r.X, yCab, r.Width, r.Bottom - yCab),
                                   frac, labs, vals, AlinhamentoHorizontal.Esquerda);
            }
            else
            {
                if (_vm.Modal != TipoModal.Aereo && _vm.Modal != TipoModal.Multimodal)
                {
                    DesenhaModalGenerico(r, yCab);
                    return;
                }

                // Aéreo / Multimodal com a coluna de minuta atravessando a faixa de tarifa.
                float total = r.Bottom - yCab;
                const float hHdr = 2.35F;
                float h1 = (total - hHdr) / 3F;

                float[] frac1 = { 0.137F, 0.147F, 0.264F, 0.293F, 0.159F };
                string[] labs1 = { "Inf de Manuseio", "Cod. Carga Especial", "Característica Adiconal do Serviço", "Número Operacional do Conhecimento Aéreo", "Data Prevista da Entrega" };
                string[] vals1 =
                {
                    _vm.Aereo?.InformacoesManuseio, null, _vm.Aereo?.CaracteristicaServico,
                    _vm.Aereo?.NumeroOperacionalConhecimento, _vm.Aereo?.DataPrevistaEntrega
                };
                DesenhaLinhaCampos(new RectangleF(r.X, yCab, r.Width, h1),
                                   frac1, labs1, vals1, AlinhamentoHorizontal.Esquerda);

                float yHdr = yCab + h1;
                float[] frac2 = { 0.156F, 0.175F, 0.176F, 0.060F, 0.124F, 0.145F, 0.164F };
                float larguraTarifa = r.Width * frac2.Take(6).Sum();
                float xMinuta = r.X + larguraTarifa + 0.29F;

                _gfx.DrawLine(new PointF(r.X, yHdr), new PointF(r.Right, yHdr));
                _gfx.DrawLine(new PointF(r.X, yHdr + hHdr), new PointF(xMinuta, yHdr + hHdr));
                _gfx.DrawLine(new PointF(xMinuta, yHdr), new PointF(xMinuta, yHdr + hHdr + h1));
                _gfx.DrawLine(new PointF(r.X, yHdr + hHdr + h1), new PointF(r.Right, yHdr + hHdr + h1));
                DrawStringFit("DADOS DA TARIFA",
                                new RectangleF(r.X + 2.17F, yHdr, larguraTarifa, hHdr),
                                _fLabelMin, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);

                string[] labs2 = { "Aeroporto de Origem", "Aeroporto de Passagem", "Aeroporto de Destino", "Classe", "Código da Tarifa", "Valor da Tarifa" };
                string[] vals2 =
                {
                    _vm.Aereo?.AeroportoOrigem, _vm.Aereo?.AeroportoPassagem, _vm.Aereo?.AeroportoDestino, _vm.Aereo?.Classe, _vm.Aereo?.CodigoTarifa,
                    _vm.Aereo?.ValorTarifa.HasValue == true ? _vm.Aereo.ValorTarifa.Value.ToString("N2", Formatador.Cultura) : "0,00"
                };
                float[] fracTarifa = frac2.Take(6).Select(f => f / frac2.Take(6).Sum()).ToArray();
                DesenhaLinhaCampos(new RectangleF(r.X, yHdr + hHdr, larguraTarifa, h1),
                                   fracTarifa, labs2, vals2, AlinhamentoHorizontal.Esquerda);
                Campo(new RectangleF(xMinuta, yHdr - 0.41F, r.Right - xMinuta, hHdr + h1),
                      "Número da Minuta", _vm.Aereo?.NumeroMinuta ?? string.Empty,
                      AlinhamentoHorizontal.Esquerda);

                float fracMinuta = (xMinuta - r.X) / r.Width;
                float[] frac3 = { 0.117F, 0.313F, Math.Max(0.12F, fracMinuta - 0.430F), Math.Max(0.10F, 1F - fracMinuta) };
                string[] labs3 = { "Retira", "Dados Relativos a Retirada da Carga", "Identificação Interna do Tomador", "Identificação do Emissor" };
                string[] vals3 = { string.Empty, _vm.Aereo?.DadosRetirada, _vm.Aereo?.IdentificacaoInternaTomador, _vm.Aereo?.IdentificacaoEmissor };
                var rRetirada = new RectangleF(r.X, yHdr + hHdr + h1, r.Width, h1);
                DesenhaLinhaCampos(rRetirada, frac3, labs3, vals3, AlinhamentoHorizontal.Esquerda);

                float wRetira = frac3[0] * rRetirada.Width;
                var rRetira = new RectangleF(rRetirada.X, rRetirada.Y, wRetira, rRetirada.Height);
                DesenhaCheckRetira(rRetira, _vm.Aereo?.Retira == true);
            }
        }

        private void DesenhaModalGenerico(RectangleF r, float yCab)
        {
            float hLinha = (r.Bottom - yCab) / 2F;

            if (_vm.Modal == TipoModal.Aquaviario)
            {
                var aq = _vm.Aquaviario;
                string containers = aq?.Containers == null
                    ? string.Empty
                    : string.Join(", ", aq.Containers.Select(ResumoContainer).Where(v => !string.IsNullOrWhiteSpace(v)).Take(3));
                float[] frac = { 0.30F, 0.18F, 0.22F, 0.30F };
                string[] labs = { "Identificacao do Navio", "Valor AFRMM", "Balsas", "Containers" };
                string[] vals =
                {
                    aq?.IdentificacaoNavio,
                    FormataMoeda(aq?.ValorAfrmm),
                    aq?.Balsas == null ? string.Empty : string.Join(", ", aq.Balsas.Where(v => !string.IsNullOrWhiteSpace(v)).Take(3)),
                    containers
                };
                DesenhaLinhaCampos(new RectangleF(r.X, yCab, r.Width, hLinha), frac, labs, vals, AlinhamentoHorizontal.Esquerda);
                return;
            }

            if (_vm.Modal == TipoModal.Ferroviario)
            {
                var fe = _vm.Ferroviario;
                float[] frac1 = { 0.18F, 0.22F, 0.20F, 0.20F, 0.20F };
                string[] labs1 = { "Tipo de Trafego", "Fluxo", "Resp. Faturamento", "Ferrovia Emitente", "Valor Frete" };
                string[] vals1 =
                {
                    fe?.TipoTrafego,
                    fe?.Fluxo,
                    fe?.ResponsavelFaturamento,
                    fe?.FerroviaEmitenteCte,
                    FormataMoeda(fe?.ValorFrete)
                };
                DesenhaLinhaCampos(new RectangleF(r.X, yCab, r.Width, hLinha), frac1, labs1, vals1, AlinhamentoHorizontal.Esquerda);

                string ferrovias = fe?.FerroviasEnvolvidas == null
                    ? string.Empty
                    : string.Join(", ", fe.FerroviasEnvolvidas.Select(ResumoFerrovia).Where(v => !string.IsNullOrWhiteSpace(v)).Take(3));
                DesenhaLinhaCampos(new RectangleF(r.X, yCab + hLinha, r.Width, hLinha),
                                   new[] { 1F },
                                   new[] { "Ferrovias Envolvidas" },
                                   new[] { ferrovias },
                                   AlinhamentoHorizontal.Esquerda);
                return;
            }

            var du = _vm.Dutoviario;
            float[] fracDuto = { 0.34F, 0.33F, 0.33F };
            string[] labsDuto = { "Data Inicio", "Data Fim", "Valor da Tarifa" };
            string[] valsDuto = { du?.DataInicio, du?.DataFim, FormataMoeda(du?.ValorTarifa) };
            DesenhaLinhaCampos(new RectangleF(r.X, yCab, r.Width, hLinha), fracDuto, labsDuto, valsDuto, AlinhamentoHorizontal.Esquerda);
        }

        private void DesenhaCheckRetira(RectangleF r, bool retira)
        {
            float box = 2.35F;
            float y = r.Bottom - box - 0.45F;
            float xSim = r.X + 2.65F;
            float xNao = r.X + 11.55F;

            void Caixa(float x, bool marcado)
            {
                var rc = new RectangleF(x, y, box, box);
                _gfx.StrokeRectangle(rc, 0.15F);
                if (marcado)
                {
                    _gfx.DrawLine(new PointF(rc.X + 0.4F, rc.Y + 0.4F), new PointF(rc.Right - 0.4F, rc.Bottom - 0.4F));
                    _gfx.DrawLine(new PointF(rc.Right - 0.4F, rc.Y + 0.4F), new PointF(rc.X + 0.4F, rc.Bottom - 0.4F));
                }
            }

            Caixa(xSim, retira);
            DrawStringFit("SIM",
                            new RectangleF(xSim + box + 0.85F, y - 0.15F, 6.5F, box + 0.6F),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
            Caixa(xNao, !retira);
            DrawStringFit("N\u00C3O",
                            new RectangleF(xNao + box + 0.85F, y - 0.15F, 7.5F, box + 0.6F),
                            _fLabelMin, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
        }

        private void DesenhaUsoExclusivoFisco()
        {
            var r = _vm.Modal == TipoModal.Rodoviario
                ? Rect(6.88F, 209.47F, 196.47F, 15.74F)
                : Rect(7.00F, 219.09F, 196.35F, 15.79F);
            float xMid = 129.00F;
            var rUso = new RectangleF(r.X, r.Y, xMid - r.X, r.Height);
            var rFisco = new RectangleF(xMid, r.Y, r.Right - xMid, r.Height);
            _gfx.DrawRoundedRectangle(rUso, 1.6F);
            _gfx.DrawRoundedRectangle(rFisco, 1.6F);
            float yCab = r.Y + 3F;
            _gfx.DrawLine(new PointF(rUso.X, yCab), new PointF(rUso.Right, yCab));
            _gfx.DrawLine(new PointF(rFisco.X, yCab), new PointF(rFisco.Right, yCab));

            DrawStringFit("USO EXCLUSIVO DO EMISSOR DO CT-e",
                            new RectangleF(r.X, r.Y + 0.3F, xMid - r.X, 3F),
                            _fCabecalhoBl, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);
            DrawStringFit("RESERVADO AO FISCO",
                            new RectangleF(xMid, r.Y + 0.3F, r.Right - xMid, 3F),
                            _fCabecalhoBl, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);

            string usoEmissor = _vm.UsoExclusivoEmissor ?? string.Empty;
            if (_vm.Modal == TipoModal.Multimodal &&
                usoEmissor.StartsWith("RAMO:", StringComparison.OrdinalIgnoreCase))
            {
                usoEmissor = string.Empty;
            }

            DrawStringFit(usoEmissor,
                            new RectangleF(r.X + 1F, yCab + 0.5F, xMid - r.X - 2F, r.Bottom - yCab - 1F),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit(_vm.ReservadoFisco ?? string.Empty,
                            new RectangleF(xMid + 1F, yCab + 0.5F, r.Right - xMid - 2F, r.Bottom - yCab - 1F),
                            _fValor, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);

            if (!string.IsNullOrWhiteSpace(_vm.EmitenteCustomizado))
            {
                float yUsuario = _vm.Modal == TipoModal.Rodoviario ? r.Bottom + 2.9F : 236.4F;
                DrawStringFit("Usuario emissor:",
                                new RectangleF(9.24F, yUsuario, 22.0F, 3F),
                                _fSubtitulo, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                DrawStringFit(_vm.EmitenteCustomizado,
                                new RectangleF(31.82F, yUsuario, r.Width * 0.45F, 3F),
                                _fSubtitulo, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            }

            var rAux = _vm.Modal == TipoModal.Rodoviario
                ? new RectangleF(r.Right - 15.26F, r.Bottom + 0.5F, 15.0F, 6.5F)
                : new RectangleF(r.Right - 15.0F, r.Bottom - 1.3F, 15.0F, 8.8F);
            DesenhaBarcodeAuxiliar(rAux);
        }

        private void DesenhaBarcodeAuxiliar(RectangleF r)
        {
            if (string.IsNullOrWhiteSpace(_vm.ChaveAcesso) || _vm.ChaveAcesso.Length < 6) return;

            string codigo = CodigoBarcodeAuxiliar();
            var bar = new Barcode128CSemContorno(codigo, _estilo, r.Width);
            bar.SetSize(r.Width, r.Height);
            bar.SetPosition(r.X, r.Y);
            bar.Draw(_gfx);
            DrawStringFit(codigo,
                            new RectangleF(r.X, r.Bottom - 1.8F, r.Width, 2F),
                            new Fonte("Times New Roman", XFontStyle.Regular, 7F), AlinhamentoHorizontal.Direita, AlinhamentoVertical.Topo);
        }

        private string CodigoBarcodeAuxiliar()
        {
            bool multimodalContingencia = _vm.Modal == TipoModal.Multimodal && _vm.TipoEmissao != 1;
            if (!multimodalContingencia && !string.IsNullOrWhiteSpace(_vm.ChaveAcesso) && _vm.ChaveAcesso.Length >= 43)
            {
                string cct = _vm.ChaveAcesso.Substring(35, 8).TrimStart('0');
                return string.IsNullOrEmpty(cct) ? "0" : cct;
            }

            if (_vm.Modal == TipoModal.Multimodal)
                return _vm.Numero.ToString();

            return _vm.ChaveAcesso.Length >= 7
                ? _vm.ChaveAcesso.Substring(_vm.ChaveAcesso.Length - 7, 6).TrimStart('0')
                : _vm.Numero.ToString();
        }

        private void DesenhaCanhotoInferior()
        {
            var r = Rect(6.95F, 252.57F, 196.06F, 39.92F);
            _gfx.DrawRoundedRectangle(r, 1.6F);

            // Linha de declaração
            float yDec = r.Y + 5.61F;
            DrawStringFit("DECLARO QUE RECEBI OS VOLUMES DESTE CONHECIMENTO EM PERFEITO ESTADO PELO QUE DOU POR CUMPRIDO O PRESENTE CONTRATO DE TRANSPORTE",
                            new RectangleF(r.X + 1F, r.Y + 1.2F, r.Width - 2F, 3.5F),
                            _fLabel, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Centro);
            _gfx.DrawLine(new PointF(r.X, yDec), new PointF(r.Right, yDec));

            // Coluna 1: Chegada no Cliente
            float x1 = 49.2F;
            float x3 = 144.3F;

            float yBoxTop = yDec + 0.3F;
            float yBoxSplit = 273.2F;
            float yBoxBottom = r.Bottom - 1.33F;
            var rChegada = new RectangleF(8.28F, yBoxTop + 0.35F, x1 - 8.28F - 1.45F, yBoxSplit - (yBoxTop + 0.35F));
            var rAssinatura = new RectangleF(7.88F, yBoxSplit, 39.30F, yBoxBottom - yBoxSplit);
            var rDados = new RectangleF(x1 + 0.03F, yBoxTop + 0.18F, x3 - x1 - 0.08F, yBoxSplit - (yBoxTop + 0.18F));
            var rControle = new RectangleF(x1 - 0.32F, yBoxSplit, x3 - x1 - 0.33F, r.Bottom - 0.60F - yBoxSplit);
            var rStatusCabecalho = new RectangleF(144.09F, 258.65F, 56.83F, 9.35F);
            var rStatusChecks = new RectangleF(144.23F, 268.42F, 56.96F, 23.09F);
            const float raioCanhoto = 2F;

            _gfx.DrawRoundedRectangle(rChegada, raioCanhoto);
            _gfx.DrawRoundedRectangle(rAssinatura, raioCanhoto);
            _gfx.DrawRoundedRectangle(rDados, raioCanhoto);
            _gfx.DrawRoundedRectangle(rControle, raioCanhoto);
            _gfx.DrawRoundedRectangle(rStatusCabecalho, raioCanhoto);
            _gfx.DrawRoundedRectangle(rStatusChecks, raioCanhoto);

            _gfx.DrawLine(new PointF(rChegada.X, rChegada.Y + 4.70F), new PointF(rChegada.Right, rChegada.Y + 4.70F));
            _gfx.DrawLine(new PointF(rChegada.X, rChegada.Y + 9.31F), new PointF(rChegada.Right, rChegada.Y + 9.31F));
            _gfx.DrawLine(new PointF(rChegada.X + 13.09F, rChegada.Y + 4.70F), new PointF(rChegada.X + 13.09F, rChegada.Bottom));
            _gfx.DrawLine(new PointF(rDados.X, rDados.Y + 4.7F), new PointF(rDados.Right, rDados.Y + 4.7F));
            _gfx.DrawLine(new PointF(rDados.X, rDados.Y + 9.7F), new PointF(rDados.Right, rDados.Y + 9.7F));
            _gfx.StrokePen = new XPen(XColors.Black, 0.35F / 25.4F * 72F);
            _gfx.DrawLine(new PointF(rAssinatura.X + 1.6F, rAssinatura.Bottom - 2.60F), new PointF(rAssinatura.Right - 1.5F, rAssinatura.Bottom - 2.60F));
            _gfx.SetLineWidth(0.176F);

            DrawStringFit("CHEGADA NO CLIENTE",
                            new RectangleF(rChegada.X + 0.35F, rChegada.Y + 2.40F, rChegada.Width - 2F, _fLabel.AlturaLinha),
                            _fLabel, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
            DrawStringFit("DATA:",
                            new RectangleF(rChegada.X + 0.65F, rChegada.Y + 6.30F, rChegada.Width - 2F, 3F),
                            _estilo.CriarFonteNegrito(6.3F), AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit("HORA:",
                            new RectangleF(rChegada.X + 0.65F, rChegada.Y + 11.15F, rChegada.Width - 2F, 3F),
                            _estilo.CriarFonteNegrito(6.3F), AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit("Assinatura",
                            new RectangleF(rAssinatura.X + 1F, rAssinatura.Y + 2F, rAssinatura.Width - 2F, 3F),
                            _fLabelMin, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);

            // Coluna 2: DADOS DO RECEBEDOR
            DrawStringFit("DADOS DO RECEBEDOR",
                            new RectangleF(rDados.X - 0.5F, rDados.Y + 1.65F, rDados.Width - 2F, _fLabel.AlturaLinha),
                            _fLabel, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
            DrawStringFit("NOME:",
                            new RectangleF(rDados.X + 1F, rDados.Y + 5.15F, x3 - (x1 + 35F), 3F),
                            _estilo.CriarFonteNegrito(6.3F), AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            DrawStringFit("RG:",
                            new RectangleF(x3 - 37.0F, rDados.Y + 5.25F, 18F, 3F),
                            _estilo.CriarFonteNegrito(6.3F), AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
            bool canhotoRodoviario = _vm.Modal == TipoModal.Rodoviario;
            DrawStringFit(canhotoRodoviario ? "OBSERVACOES:" : "OBSERVACOES",
                            canhotoRodoviario
                                ? new RectangleF(rDados.X + 1.64F, rDados.Y + 10.10F, rDados.Width - 2F, 3F)
                                : new RectangleF(rDados.X + 4.09F, rDados.Y + 10.84F, rDados.Width - 2F, 3F),
                            canhotoRodoviario ? _estilo.CriarFonteNegrito(6.0F) : _fLabel,
                            AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);

            // Coluna 3: controle do fisco, barcode e chave de acesso.
            float yControle = 274.8F;
            float yControleLabel = yControle - 0.95F;
            DrawStringFit("CONTROLE DO FISCO",
                            new RectangleF(rControle.X + 0.4F, yControleLabel, rControle.Width - 2F, _fLabel.AlturaLinha),
                            _fLabel, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);

            if (!string.IsNullOrEmpty(_vm.ChaveAcesso))
            {
                // Barcode mais compacto + margem extra entre coluna 3 e coluna 4
                // pra evitar que a frase da chave/site colida com o primeiro
                // checkbox "ENTREGA REALIZADA COM SUCESSO" da coluna 4.
                const float larguraBarraCanhoto = 80.17F;
                float xBarraCanhoto = _vm.Modal == TipoModal.Rodoviario ? 56.87F : 55.81F;
                float yBarraCanhoto = _vm.Modal == TipoModal.Rodoviario ? 275.09F : 274.45F;
                var rBar = new RectangleF(xBarraCanhoto, yBarraCanhoto, larguraBarraCanhoto, 12.47F);
                var bar = new Barcode128CSemContorno(_vm.ChaveAcesso, _estilo, rBar.Width);
                bar.SetSize(rBar.Width, rBar.Height);
                bar.SetPosition(rBar.X, rBar.Y);
                bar.Draw(_gfx);

                DrawStringFit("Chave de acesso para consulta de autenticidade no site",
                                new RectangleF(_vm.Modal == TipoModal.Rodoviario ? 73.94F : 74.21F,
                                               _vm.Modal == TipoModal.Rodoviario ? 286.32F : 285.95F,
                                               rControle.Right - 74.94F, 3F),
                                _estilo.CriarFonteRegular(4.5F), AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                // Chave em string unica, sem agrupamento.
                DrawStringFit(_vm.ChaveAcesso,
                                new RectangleF(rControle.X + 1F,
                                               rBar.Bottom + (_vm.Modal == TipoModal.Rodoviario ? 1.05F : 1.75F),
                                               rControle.Width - 2F, 3.5F),
                                _fValor, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
            }

            // Coluna 4: SÉRIE / NÚMERO / DATA + Status checkboxes.
            // Usa frac4 para que a célula DATA E HORA receba mais espaço que
            // Série/número: a data recebe mais espaço para não colidir com a coluna anterior.
            float[] frac4 = { 0.175F, 0.290F, 0.535F };
            float wTotal = rStatusCabecalho.Width;
            float xc = rStatusCabecalho.X;
            float yStatusCab = rStatusCabecalho.Bottom;
            string[] labs4 = { "SÉRIE", "NÚMERO", "DATA E HORA DE EMISS\u00C3O" };
            string[] vals4 =
            {
                _vm.Serie.ToString(),
                _vm.Numero.ToString(),
                _vm.DataHoraEmissao?.ToString("dd/MM/yyyy HH:mm:ss") ?? string.Empty
            };
            for (int i = 0; i < 3; i++)
            {
                float ww = frac4[i] * wTotal;
                if (i > 0) _gfx.DrawLine(new PointF(xc, rStatusCabecalho.Y), new PointF(xc, yStatusCab));
                float dxLabStatus = i == 2 ? -4.60F : 0F;
                float wLabStatus = i == 2 ? ww + 5.00F : ww - 1F;
                DrawStringFit(labs4[i],
                                new RectangleF(xc + 0.5F + dxLabStatus, rStatusCabecalho.Y + 2.22F, wLabStatus, _fLabelMin.AlturaLinha),
                                _fLabelMin, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
                if (i == 2 && _vm.DataHoraEmissao.HasValue)
                {
                    DrawStringFit(_vm.DataHoraEmissao.Value.ToString("dd/MM/yyyy"),
                                    new RectangleF(172.90F, rStatusCabecalho.Y + 6.56F, 16.5F, 3.5F),
                                    _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                    DrawStringFit(_vm.DataHoraEmissao.Value.ToString("HH:mm:ss"),
                                    new RectangleF(188.10F, rStatusCabecalho.Y + 6.16F, rStatusCabecalho.Right - 188.60F, 3.5F),
                                    _fValorBold, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Topo);
                }
                else
                {
                    DrawStringFit(vals4[i],
                                    new RectangleF(xc + 0.5F, rStatusCabecalho.Y + 6.16F, ww - 1F, 3.5F),
                                    _fValorBold, AlinhamentoHorizontal.Centro, AlinhamentoVertical.Topo);
                }
                xc += ww;
            }

            // Status checkboxes
            string[] status =
            {
                "ENTREGA REALIZADA COM SUCESSO",
                "CLIENTE AUSENTE",
                "CLIENTE MUDOU-SE",
                "ENDERECO NAO LOCALIZADO",
                "RECUSA DA MERCADORIA",
                "MERCADORIA AVARIADA"
            };
            float yStat = yStatusCab + 1.00F;
            float xStat = 146.51F;
            const float lhStat = 3.864F;
            var fonteStatus = _estilo.CriarFonteRegular(5.5F);
            for (int i = 0; i < status.Length; i++)
            {
                var rChk = new RectangleF(xStat, yStat + i * lhStat + 0.15F, 3.00F, 2.12F);
                _gfx.StrokeRectangle(rChk, 0.35F);
                DrawStringFit(status[i],
                                new RectangleF(xStat + 3.50F, yStat + i * lhStat - 0.47F, rStatusChecks.Right - xStat - 3.50F, lhStat),
                                fonteStatus, AlinhamentoHorizontal.Esquerda, AlinhamentoVertical.Centro);
            }
        }

        #endregion

        #region Formatacao

        private void DrawStringFit(string text, RectangleF rect, Fonte fonte,
                                   AlinhamentoHorizontal ah = AlinhamentoHorizontal.Esquerda,
                                   AlinhamentoVertical av = AlinhamentoVertical.Topo,
                                   float minPt = 4.2F,
                                   bool ellipsis = false)
        {
            if (string.IsNullOrEmpty(text) || fonte == null) return;
            if (rect.Width <= 0.1F || rect.Height <= 0.1F) return;

            Fonte usada = fonte;
            float larguraUtil = Math.Max(0.2F, rect.Width - 0.15F);
            if (usada.MedirLarguraTexto(text) > larguraUtil && usada.Tamanho > minPt)
            {
                usada = fonte.Clonar();
                while (usada.Tamanho > minPt && usada.MedirLarguraTexto(text) > larguraUtil)
                    usada.Tamanho = Math.Max(minPt, usada.Tamanho - 0.25F);
            }

            string saida = text;
            if (ellipsis && usada.MedirLarguraTexto(saida) > larguraUtil)
                saida = TruncaParaLargura(saida, usada, larguraUtil);

            if (string.IsNullOrEmpty(saida)) return;

            using (_gfx.SaveState())
            {
                _gfx.XGraphics.IntersectClip(new XRect(MmToPoint(rect.X), MmToPoint(rect.Y),
                                                       MmToPoint(rect.Width), MmToPoint(rect.Height)));
                _gfx.DrawString(saida, rect, usada, ah, av);
            }
        }

        private static double MmToPoint(float mm) => mm / 25.4D * 72D;

        private static string TruncaParaLargura(string text, Fonte fonte, float larguraMm)
        {
            if (string.IsNullOrEmpty(text) || larguraMm <= 0.2F) return string.Empty;
            if (fonte.MedirLarguraTexto(text) <= larguraMm) return text;

            const string ell = "...";
            if (fonte.MedirLarguraTexto(ell) >= larguraMm) return string.Empty;

            int lo = 0;
            int hi = text.Length;
            while (lo < hi)
            {
                int mid = (lo + hi + 1) / 2;
                string cand = text.Substring(0, mid).TrimEnd() + ell;
                if (fonte.MedirLarguraTexto(cand) <= larguraMm)
                    lo = mid;
                else
                    hi = mid - 1;
            }

            return text.Substring(0, Math.Max(0, lo)).TrimEnd() + ell;
        }

        private void DesenhaLinhaCampos(RectangleF r, float[] fracoes, string[] labels, string[] valores,
                                        AlinhamentoHorizontal alinhValor, bool primeiraColunaEsquerda = false,
                                        float deslocXValor = 0F, float deslocYValor = 0F,
                                        bool deslocarPrimeiraColuna = true,
                                        bool deslocarYPrimeiraColuna = true)
        {
            if (fracoes.Length != labels.Length || fracoes.Length != valores.Length)
                throw new ArgumentException("fracoes/labels/valores precisam ter o mesmo tamanho.");

            float x = r.X;
            for (int i = 0; i < fracoes.Length; i++)
            {
                float w = fracoes[i] * r.Width;
                if (i > 0) _gfx.DrawLine(new PointF(x, r.Y), new PointF(x, r.Bottom));
                var alinh = (primeiraColunaEsquerda && i == 0) ? AlinhamentoHorizontal.Esquerda : alinhValor;
                bool aplicaDeslocX = deslocarPrimeiraColuna || i > 0;
                bool aplicaDeslocY = deslocarYPrimeiraColuna || i > 0;
                Campo(new RectangleF(x, r.Y, w, r.Height), labels[i], valores[i] ?? string.Empty, alinh,
                      deslocXValor: aplicaDeslocX ? deslocXValor : 0F,
                      deslocYValor: aplicaDeslocY ? deslocYValor : 0F);
                x += w;
            }
        }

        private static string JoinEndereco(string linha1, string bairro)
        {
            if (string.IsNullOrWhiteSpace(linha1) && string.IsNullOrWhiteSpace(bairro)) return string.Empty;
            if (string.IsNullOrWhiteSpace(bairro)) return linha1;
            if (string.IsNullOrWhiteSpace(linha1)) return bairro;
            return $"{linha1}, {bairro}";
        }

        private static string ResumoContainer(ContainerInfo container)
        {
            if (container == null) return string.Empty;
            if (container.Lacres == null || container.Lacres.Count == 0)
                return container.Numero ?? string.Empty;

            string lacres = string.Join("/", container.Lacres.Where(v => !string.IsNullOrWhiteSpace(v)).Take(2));
            return string.IsNullOrWhiteSpace(lacres)
                ? (container.Numero ?? string.Empty)
                : $"{container.Numero} ({lacres})";
        }

        private static string ResumoFerrovia(FerroviaEnvolvidaInfo ferrovia)
        {
            if (ferrovia == null) return string.Empty;
            return !string.IsNullOrWhiteSpace(ferrovia.RazaoSocial)
                ? ferrovia.RazaoSocial
                : (!string.IsNullOrWhiteSpace(ferrovia.CodigoInterno) ? ferrovia.CodigoInterno : ferrovia.Cnpj);
        }

        private static string EnderecoPessoa(EmpresaViewModel emp)
        {
            if (emp == null) return string.Empty;
            var partes = new[]
            {
                emp.EnderecoLogadrouro,
                emp.EnderecoNumero,
                emp.EnderecoComplemento,
                emp.EnderecoBairro
            }
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim());
            string endereco = string.Join(", ", partes);
            return endereco.Any(char.IsLetterOrDigit) ? endereco : string.Empty;
        }

        private static string EnderecoTomador(EmpresaViewModel emp)
        {
            if (emp == null) return string.Empty;
            var partes = new List<string>();
            string logradouro = RemoveDuplicacaoConsecutiva(emp.EnderecoLogadrouro ?? string.Empty);
            if (!string.IsNullOrWhiteSpace(logradouro)) partes.Add(logradouro);

            string normalizado = NormalizaTextoComparacao(logradouro);
            string numero = emp.EnderecoNumero?.Trim();
            if (!string.IsNullOrWhiteSpace(numero) &&
                !normalizado.Contains(NormalizaTextoComparacao(numero)))
            {
                partes.Add(numero);
                normalizado += NormalizaTextoComparacao(numero);
            }

            string complemento = emp.EnderecoComplemento?.Trim();
            if (!string.IsNullOrWhiteSpace(complemento) &&
                !normalizado.Contains(NormalizaTextoComparacao(complemento)))
            {
                partes.Add(complemento);
            }

            string endereco = string.Join(", ", partes);
            return endereco.Any(char.IsLetterOrDigit) ? endereco : string.Empty;
        }

        private static string FormataCpfCnpjSeguro(string valor)
        {
            return string.IsNullOrWhiteSpace(valor)
                ? string.Empty
                : (Formatador.FormatarCpfCnpj(valor) ?? string.Empty);
        }

        private static string FormataNomeTomador(EmpresaViewModel emp)
        {
            if (emp == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(emp.NomeFantasia)) return emp.RazaoSocial ?? string.Empty;
            if (string.IsNullOrWhiteSpace(emp.RazaoSocial)) return emp.NomeFantasia;
            return $"{emp.RazaoSocial} - {emp.NomeFantasia}";
        }

        private static string RemoveDuplicacaoConsecutiva(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;

            var partes = texto.Split(',')
                              .Select(p => p.Trim())
                              .Where(p => p.Length > 0)
                              .ToList();
            if (partes.Count <= 1) return texto.Trim();

            var saida = new List<string>();
            string anterior = null;
            foreach (var parte in partes)
            {
                string atual = NormalizaTextoComparacao(parte);
                if (atual != anterior)
                    saida.Add(parte);
                anterior = atual;
            }

            return string.Join(", ", saida);
        }

        private static string NormalizaTextoComparacao(string texto)
        {
            if (string.IsNullOrWhiteSpace(texto)) return string.Empty;
            return new string(texto.Where(c => !char.IsWhiteSpace(c) && c != '.' && c != '-' && c != ',')
                                   .Select(char.ToUpperInvariant)
                                   .ToArray());
        }

        private static string FormataMoeda(decimal? v) =>
            v.HasValue ? v.Value.ToString("N2", Formatador.Cultura) : string.Empty;

        private static string FormataNum(decimal? v, int casas) =>
            v.HasValue ? v.Value.ToString($"N{casas}", Formatador.Cultura) : string.Empty;

        private static string FormataPeso(decimal? v, string unidade) =>
            v.HasValue ? $"{v.Value.ToString("N2", Formatador.Cultura)} {unidade}".Trim() : string.Empty;

        private static string DescTipoCte(int tp) => tp switch
        {
            0 => "Normal",
            1 => "Complemento de Valores",
            2 => "Anulação",
            3 => "Substituto",
            _ => tp.ToString()
        };

        private static string DescTipoServico(int tp) => tp switch
        {
            0 => "Normal",
            1 => "Subcontratação",
            2 => "Redespacho",
            3 => "Redespacho Intermediário",
            4 => "Vinculado a Multimodal",
            _ => tp.ToString()
        };

        private static string DescCST(string cst) => cst switch
        {
            "00" => "00 - Trib. normal ICMS",
            "20" => "20 - BC reduzida",
            "40" => "40 - ICMS isenção",
            "41" => "41 - ICMS não tributado",
            "45" => "45 - Isento/nao trib./dif.",
            "60" => "60 - ICMS subst.",
            "90" => "90 - ICMS Outros",
            _ => string.IsNullOrWhiteSpace(cst) ? string.Empty : cst
        };

        private static string AbrTipo(TipoDocumentoOrig t) => t switch
        {
            TipoDocumentoOrig.NFe    => "NFe Chav",
            TipoDocumentoOrig.NF     => "NF",
            TipoDocumentoOrig.Outros => "Outros",
            _ => string.Empty
        };

        private sealed class Barcode128CScaled : Barcode128C
        {
            public Barcode128CScaled(string code, Estilo estilo, float largura = 75F)
                : base(code, estilo, largura)
            {
            }

            public override bool PossuiContono => false;

            protected override float MargemVerticalEfetiva => 0F;
        }

        private sealed class Barcode128CSemContorno : Barcode128C
        {
            public Barcode128CSemContorno(string code, Estilo estilo, float largura = 75F)
                : base(code, estilo, largura)
            {
            }

            public override bool PossuiContono => false;
        }

        #endregion
    }
}
