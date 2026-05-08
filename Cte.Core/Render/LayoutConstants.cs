namespace Cte.Core.Render
{
    /// <summary>Constantes principais do layout do DACTE em milimetros.</summary>
    internal static class LayoutConstants
    {
        /// <summary>Largura da pagina A4 em modo retrato.</summary>
        internal const float PAGE_W = 210F;

        /// <summary>Altura da pagina A4 em modo retrato.</summary>
        internal const float PAGE_H = 297F;

        /// <summary>Margem lateral da area util.</summary>
        internal const float M_X = 7F;

        /// <summary>Margem superior da area util.</summary>
        internal const float M_TOP = 4.5F;

        /// <summary>Margem inferior da area util.</summary>
        internal const float M_BOT = 4F;

        /// <summary>Y do topo do quadro de identificacao do emitente.</summary>
        internal const float Y_TOPO = M_TOP;

        /// <summary>Altura do quadro superior.</summary>
        internal const float H_TOPO = 40F;

        /// <summary>Altura da area do emitente no quadro superior.</summary>
        internal const float H_TOPO_EMIT = 25F;

        /// <summary>Y do quadro de tipo de servico.</summary>
        internal const float Y_TIPO = Y_TOPO + H_TOPO_EMIT;

        /// <summary>Altura do quadro de tipo de servico.</summary>
        internal const float H_TIPO = H_TOPO - H_TOPO_EMIT;

        /// <summary>Y do quadro CFOP/protocolo.</summary>
        internal const float Y_CFOP = Y_TOPO + H_TOPO;

        /// <summary>Altura do quadro CFOP/protocolo.</summary>
        internal const float H_CFOP = 8F;

        /// <summary>Y do quadro origem/destino.</summary>
        internal const float Y_ORIGEM = Y_CFOP + H_CFOP;

        /// <summary>Altura do quadro origem/destino.</summary>
        internal const float H_ORIGEM = 8.5F;

        /// <summary>Y do quadro de pessoas do CT-e.</summary>
        internal const float Y_PESSOAS = Y_ORIGEM + H_ORIGEM;

        /// <summary>Altura do quadro de pessoas do CT-e.</summary>
        internal const float H_PESSOAS = 44.3F;

        /// <summary>Y do quadro de informacoes da carga.</summary>
        internal const float Y_CARGA = Y_PESSOAS + H_PESSOAS;

        /// <summary>Altura do quadro de informacoes da carga.</summary>
        internal const float H_CARGA = 20F;

        /// <summary>Y do quadro de componentes do valor.</summary>
        internal const float Y_COMP = Y_CARGA + H_CARGA;

        /// <summary>Altura do quadro de componentes do valor.</summary>
        internal const float H_COMP = 21F;

        /// <summary>Y do quadro de impostos.</summary>
        internal const float Y_IMP = Y_COMP + H_COMP;

        /// <summary>Altura do quadro de impostos.</summary>
        internal const float H_IMP = 11.5F;

        /// <summary>Y do quadro de documentos originarios.</summary>
        internal const float Y_DOCS = Y_IMP + H_IMP;

        /// <summary>Altura do quadro de documentos originarios.</summary>
        internal const float H_DOCS = 11.5F;

        /// <summary>Y do quadro de observacoes.</summary>
        internal const float Y_OBS = Y_DOCS + H_DOCS;

        /// <summary>Altura do quadro de observacoes.</summary>
        internal const float H_OBS = 32F;

        /// <summary>Y do quadro do modal.</summary>
        internal const float Y_MODAL = Y_OBS + H_OBS;

        /// <summary>Altura base do quadro do modal.</summary>
        internal const float H_MODAL = 18F;

        /// <summary>Y do quadro reservado ao fisco.</summary>
        internal const float Y_FISCO = Y_MODAL + H_MODAL;

        /// <summary>Altura do quadro reservado ao fisco.</summary>
        internal const float H_FISCO = 14F;

        /// <summary>Y do canhoto inferior.</summary>
        internal const float Y_CANHOTO_INF = 251.8F;

        /// <summary>Altura do canhoto inferior.</summary>
        internal const float H_CANHOTO_INF = PAGE_H - Y_CANHOTO_INF - M_BOT;
    }
}
