using System.Text;
using Dacte.CTe.Core.Tools;

namespace Dacte.CTe.Core.Modelo
{
    /// <summary>
    /// ViewModel comum para emitente, remetente, expedidor, recebedor,
    /// destinatário e tomador (toma4).
    /// </summary>
    public class EmpresaViewModel
    {
        public string RazaoSocial { get; set; }
        public string NomeFantasia { get; set; }
        public string CnpjCpf { get; set; }
        public string Ie { get; set; }
        public string IeSt { get; set; }
        public string IM { get; set; }
        public int CRT { get; set; }

        public string EnderecoLogadrouro { get; set; }
        public string EnderecoNumero { get; set; }
        public string EnderecoComplemento { get; set; }
        public string EnderecoBairro { get; set; }
        public string Municipio { get; set; }
        public string EnderecoUf { get; set; }
        public string EnderecoCep { get; set; }
        public string EnderecoPais { get; set; }
        public string Telefone { get; set; }
        public string Email { get; set; }

        /// <summary>Inscrição SUFRAMA (apenas destinatário).</summary>
        public string Isuf { get; set; }

        // Helpers de formatacao.

        /// <summary>
        /// Logradouro, numero e complemento, removendo trechos repetidos.
        /// </summary>
        public string EnderecoLinha1
        {
            get
            {
                // Primeiro: deduplica trechos repetidos consecutivos no próprio xLgr.
                var lgr = DedupeRepetidos(EnderecoLogadrouro);

                var sb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(lgr)) sb.Append(lgr);

                // Só anexa o número se ele ainda não estiver presente no logradouro.
                if (DeveAnexarEndereco(lgr, EnderecoNumero))
                {
                    sb.Append(", ").Append(EnderecoNumero);
                }

                if (DeveAnexarEndereco(sb.ToString(), EnderecoComplemento) &&
                    !NormalizaEndereco(EnderecoComplemento).Equals(NormalizaEndereco(EnderecoNumero), System.StringComparison.OrdinalIgnoreCase))
                {
                    sb.Append(" - ").Append(EnderecoComplemento);
                }
                return sb.ToString();
            }
        }

        /// <summary>
        /// Remove tokens consecutivos repetidos separados por vírgula
        /// ("ED. SEDE, ED. SEDE" ? "ED. SEDE").
        /// </summary>
        private static string DedupeRepetidos(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return s;
            var partes = s.Split(',');
            var saida = new System.Collections.Generic.List<string>();
            var vistos = new System.Collections.Generic.HashSet<string>(System.StringComparer.OrdinalIgnoreCase);
            foreach (var p in partes)
            {
                var t = p.Trim();
                if (t.Length == 0) continue;
                if (vistos.Add(NormalizaEndereco(t)))
                {
                    saida.Add(t);
                }
            }
            return string.Join(", ", saida);
        }

        private static bool DeveAnexarEndereco(string atual, string candidato)
        {
            if (string.IsNullOrWhiteSpace(candidato)) return false;
            var normalizado = NormalizaEndereco(candidato);
            if (normalizado.Length == 0 || normalizado == "SEM COMPLEMENTO") return false;
            return string.IsNullOrWhiteSpace(atual) ||
                   NormalizaEndereco(atual).IndexOf(normalizado, System.StringComparison.OrdinalIgnoreCase) < 0;
        }

        private static string NormalizaEndereco(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            var sb = new StringBuilder(s.Trim().Length);
            bool lastWasSpace = false;
            foreach (char ch in s.Trim().ToUpperInvariant())
            {
                if (char.IsLetterOrDigit(ch))
                {
                    sb.Append(ch);
                    lastWasSpace = false;
                }
                else if (!lastWasSpace)
                {
                    sb.Append(' ');
                    lastWasSpace = true;
                }
            }
            return sb.ToString().Trim();
        }

        /// <summary>Bairro, município/UF.</summary>
        public string EnderecoLinha2
        {
            get
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(EnderecoBairro)) sb.Append(EnderecoBairro);
                if (!string.IsNullOrWhiteSpace(Municipio))
                {
                    if (sb.Length > 0) sb.Append(" - ");
                    sb.Append(Municipio);
                }
                if (!string.IsNullOrWhiteSpace(EnderecoUf))
                {
                    if (sb.Length > 0) sb.Append("/");
                    sb.Append(EnderecoUf);
                }
                return sb.ToString();
            }
        }

        /// <summary>CEP, telefone.</summary>
        public string EnderecoLinha3
        {
            get
            {
                var sb = new StringBuilder();
                if (!string.IsNullOrWhiteSpace(EnderecoCep)) sb.Append("CEP: ").Append(Formatador.FormatarCEP(EnderecoCep));
                if (!string.IsNullOrWhiteSpace(Telefone))
                {
                    if (sb.Length > 0) sb.Append(" ");
                    sb.Append("Fone: ").Append(Formatador.FormatarTelefone(Telefone));
                }
                return sb.ToString();
            }
        }
    }
}
