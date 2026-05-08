using System;
using System.IO;
using System.Linq;
using Cte.Core;
using Cte.Core.Modelo;

namespace Cte.Core.Sample
{
    /// <summary>
    /// Carrega cada XML em ./Exemplos e gera o respectivo .pdf em ./out.
    /// O usuário pode passar um caminho de arquivo XML/diretório como argumento
    /// para sobrepor o padrão.
    /// </summary>
    public static class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var (entrada, saida, logoPath) = ResolverCaminhos(args);
                Directory.CreateDirectory(saida);

                var xmls = ColetarXmls(entrada);
                if (xmls.Length == 0)
                {
                    Console.WriteLine($"Nenhum XML encontrado em '{entrada}'.");
                    return 1;
                }

                Console.WriteLine($"Encontrados {xmls.Length} XML(s). Gerando PDFs em '{saida}'...");
                int ok = 0, fail = 0;

                foreach (var xml in xmls)
                {
                    var nome = Path.GetFileNameWithoutExtension(xml);
                    var destino = Path.Combine(saida, nome + ".pdf");

                    try
                    {
                        Gerar(xml, destino, logoPath);
                        Console.WriteLine($"  [OK]  {nome} -> {destino}");
                        ok++;
                    }
                    catch (Exception ex)
                    {
                        var deep = ex;
                        while (deep.InnerException != null) deep = deep.InnerException;
                        Console.WriteLine($"  [ERR] {nome}: {deep.GetType().Name}: {deep.Message}");
                        var st = deep.StackTrace?.Split('\n');
                        if (st != null) foreach (var line in st.Take(8)) Console.WriteLine($"        {line.Trim()}");
                        fail++;
                    }
                }

                Console.WriteLine();
                Console.WriteLine($"Concluído: {ok} ok, {fail} falha(s).");
                return fail == 0 ? 0 : 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Erro fatal: {ex}");
                return 99;
            }
        }

        private static void Gerar(string xmlPath, string pdfPath, string logoPath)
        {
            var modelo = DacteViewModelCreator.CriarDeArquivoXml(xmlPath);

            // Diagnostico rapido da geracao.
            Console.WriteLine($"      Modal:{modelo.Modal} Modelo:{modelo.Modelo} Serie:{modelo.Serie} N{modelo.Numero}");
            Console.WriteLine($"      Emit:{modelo.Emitente?.RazaoSocial}  Tomador:{modelo.PapelTomador ?? modelo.Tomador?.RazaoSocial}");
            Console.WriteLine($"      Carga: prod='{modelo.Carga.ProdutoPredominante}' bruto={modelo.Carga.PesoBrutoKg} taxado={modelo.Carga.PesoAferidoKg} vol={modelo.Carga.QuantidadeVolumes} m3={modelo.Carga.Cubagem}");
            Console.WriteLine($"      vTPrest={modelo.ValorTotalPrestacao}  ICMS={modelo.Imposto?.Valor} BC={modelo.Imposto?.BaseCalculo} aliq={modelo.Imposto?.Aliquota}");
            Console.WriteLine($"      Status SEFAZ: cStat={modelo.CodigoStatusResposta} ({modelo.DescricaoStatusResposta}) prot={modelo.ProtocoloAutorizacao}");
            Console.WriteLine($"      Rodo:{modelo.Rodoviario?.Rntrc ?? "(null)"} Aereo:{modelo.Aereo?.NumeroOperacionalConhecimento ?? "(null)"} Multi:{modelo.Multimodal?.CertificadoOTM ?? "(null)"}");

            using (var dacte = new DacteDoc(modelo))
            {
                if (!string.IsNullOrWhiteSpace(logoPath) && File.Exists(logoPath))
                    dacte.AdicionarLogoImagem(logoPath);

                dacte.Gerar();
                dacte.Salvar(pdfPath);
            }
        }

        private static (string entrada, string saida, string logoPath) ResolverCaminhos(string[] args)
        {
            string entrada;
            string saida;
            string logoPath;

            if (args.Length >= 1) entrada = args[0];
            else                  entrada = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Desktop", "Exemplos");

            if (args.Length >= 2) saida = args[1];
            else                  saida = Path.Combine(AppContext.BaseDirectory, "out");

            if (args.Length >= 3) logoPath = args[2];
            else                  logoPath = Environment.GetEnvironmentVariable("CTE_CORE_SAMPLE_LOGO");

            return (entrada, saida, logoPath);
        }

        private static string[] ColetarXmls(string entrada)
        {
            if (Directory.Exists(entrada))
                return Directory.GetFiles(entrada, "*.xml", SearchOption.TopDirectoryOnly);
            if (File.Exists(entrada))
                return new[] { entrada };
            return Array.Empty<string>();
        }
    }
}
