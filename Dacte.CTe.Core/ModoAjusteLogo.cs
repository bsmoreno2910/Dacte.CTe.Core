namespace Dacte.CTe.Core
{
    /// <summary>Define como o logo deve ser encaixado no espaço reservado do DACTE.</summary>
    public enum ModoAjusteLogo
    {
        /// <summary>Preenche todo o espaço reservado, podendo alterar a proporção da imagem.</summary>
        Preencher = 0,

        /// <summary>Preserva a proporção original e centraliza o logo dentro do espaço reservado.</summary>
        ConterProporcional = 1
    }
}
