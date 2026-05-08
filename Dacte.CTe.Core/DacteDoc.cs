using System;
using System.IO;
using PdfSharpCore;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
using Dacte.CTe.Core.Elementos;
using Dacte.CTe.Core.Graphics;
using Dacte.CTe.Core.Modelo;
using Dacte.CTe.Core.Render;

namespace Dacte.CTe.Core
{
    /// <summary>
    /// Documento DACTE (Documento Auxiliar do Conhecimento de Transporte Eletrônico).
    /// Orquestra a criação da página e o desenho do PDF.
    /// </summary>
    public class DacteDoc : IDisposable
    {
        public DacteViewModel ViewModel { get; private set; }

        /// <summary>Documento PDF (PdfSharpCore).</summary>
        public PdfDocument PdfDocument { get; private set; }

        internal Estilo EstiloPadrao { get; private set; }

        private readonly string _fonteFamilia;
        private bool _foiGerado;

        private XImage _logoImage;
        private XImage _logoPdfForm;

        public DacteDoc(DacteViewModel viewModel)
        {
            ViewModel = viewModel ?? throw new ArgumentNullException(nameof(viewModel));

            DacteFontResolverSetup.EnsureInitialized();

            PdfDocument = new PdfDocument();

            // Verdana garante boa legibilidade em A4.
            _fonteFamilia = "Verdana";
            EstiloPadrao = CriarEstilo();

            AdicionarMetadata();
            _foiGerado = false;
        }

        /// <summary>Logo a partir de uma imagem raster (JPG/PNG).</summary>
        public void AdicionarLogoImagem(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            _logoImage = XImage.FromStream(() => stream);
        }

        public void AdicionarLogoImagem(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            _logoImage = XImage.FromFile(path);
        }

        /// <summary>Logo a partir da primeira página de um PDF (logo vetorial).</summary>
        public void AdicionarLogoPdf(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            _logoPdfForm = XImage.FromStream(() => stream);
        }

        public void AdicionarLogoPdf(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            _logoPdfForm = XImage.FromFile(path);
        }

        private void AdicionarMetadata()
        {
            var info = PdfDocument.Info;
            info.CreationDate = DateTime.Now;
            info.Creator = string.Format("{0} {1} - {2}",
                "Dacte.CTe.Core",
                System.Reflection.Assembly.GetExecutingAssembly().GetName().Version,
                "https://github.com/bsmoreno2910/Dacte.CTe.Core");
            info.Title = "DACTE (Documento Auxiliar do CT-e)";
            info.Subject = $"DACTE - Chave: {ViewModel.ChaveAcesso}";
            info.Keywords = "DACTE;CTE;ChaveAcesso=" + ViewModel.ChaveAcesso;
        }

        private Estilo CriarEstilo(float tFonteCampoCabecalho = 6, float tFonteCampoConteudo = 10)
            => new Estilo(_fonteFamilia, tFonteCampoCabecalho, tFonteCampoConteudo);

        public void Gerar()
        {
            if (_foiGerado) throw new InvalidOperationException("O DACTE já foi gerado.");

            // Cria 1 página A4 retrato e desenha tudo via renderer.
            var page = PdfDocument.AddPage();
            page.Size = PageSize.A4;
            page.Orientation = PageOrientation.Portrait;

            using (var xg = XGraphics.FromPdfPage(page))
            {
                var gfx = new Gfx(xg);
                var logo = _logoPdfForm ?? _logoImage;
                var renderer = new DacteRenderer(ViewModel, gfx, EstiloPadrao, logo);
                renderer.Render();
            }

            _foiGerado = true;
        }

        public void Salvar(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) throw new ArgumentException(nameof(path));
            PdfDocument.Save(path);
        }

        public void Salvar(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            PdfDocument.Save(stream, false);
        }

        /// <summary>Retorna os bytes do PDF gerado.</summary>
        public byte[] ObterPdfBytes(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));
            PdfDocument.Save(stream, false);
            if (stream is MemoryStream ms) return ms.ToArray();

            using (var tmp = new MemoryStream())
            {
                stream.Position = 0;
                stream.CopyTo(tmp);
                return tmp.ToArray();
            }
        }

        #region IDisposable
        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                PdfDocument?.Dispose();
                _logoImage?.Dispose();
                _logoPdfForm?.Dispose();
            }
            _disposed = true;
        }
        public void Dispose() => Dispose(true);
        #endregion
    }
}
