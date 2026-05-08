using PdfSharpCore.Fonts;
using PdfSharpCore.Utils;

namespace Dacte.CTe.Core.Graphics
{
    /// <summary>
    /// Inicializa o FontResolver do PdfSharpCore uma única vez.
    /// O FontResolver padrão do PdfSharpCore (<see cref="FontResolver"/>) lê fontes
    /// do sistema operacional em Windows, Linux e macOS, mantendo o DACTE
    /// funcional em qualquer ambiente .NET Core/.NET 8.
    /// </summary>
    internal static class DacteFontResolverSetup
    {
        private static readonly object _Lock = new object();
        private static bool _Initialized;

        /// <summary>
        /// Garante que o FontResolver global está configurado. Idempotente e thread-safe.
        /// Pode ser chamado múltiplas vezes; só age na primeira invocação.
        /// </summary>
        public static void EnsureInitialized()
        {
            if (_Initialized) return;
            lock (_Lock)
            {
                if (_Initialized) return;

                // Se o consumidor já definiu um FontResolver customizado, respeitar.
                if (GlobalFontSettings.FontResolver == null)
                {
                    GlobalFontSettings.FontResolver = new FontResolver();
                }

                _Initialized = true;
            }
        }
    }
}
