# Dacte.CTe.Core

![NuGet Version](https://img.shields.io/nuget/v/Dacte.CTe.Core.svg)
![NuGet Downloads](https://img.shields.io/nuget/dt/Dacte.CTe.Core.svg)
![License](https://img.shields.io/badge/license-MIT-blue.svg)
![Build](https://img.shields.io/badge/build-dotnet%20build-green.svg)

Gerador de **DACTE** em PDF para **CT-e modelo 57** a partir do XML processado (`cteProc` v4.00).

**Status:** estável (1.0.1) - geração de DACTE para CT-e modelo 57.

## Instalação

```bash
dotnet add package Dacte.CTe.Core
```

## Exemplo de uso

```csharp
using Dacte.CTe.Core;
using Dacte.CTe.Core.Modelo;

var modelo = DacteViewModelCreator.CriarDeArquivoXml("cte.xml");
modelo.QuantidadeCanhotos = 1;
modelo.PreferirEmitenteNomeFantasia = true;

using (var dacte = new DacteDoc(modelo))
{
    dacte.AdicionarLogoImagem("logo.png"); // opcional
    dacte.Gerar();
    dacte.Salvar("dacte.pdf");
}
```

Logo via stream:

```csharp
using (var logo = File.OpenRead("logo.png"))
using (var dacte = new DacteDoc(modelo))
{
    dacte.AdicionarLogoImagem(logo);
    dacte.Gerar();
    dacte.Salvar("dacte.pdf");
}
```

Geração em memória:

```csharp
using (var ms = new MemoryStream())
using (var dacte = new DacteDoc(modelo))
{
    dacte.Gerar();
    dacte.Salvar(ms);
    byte[] pdf = ms.ToArray();
}
```

## Cobertura

- Parser XML para `cteProc` v4.00.
- DACTE em A4 retrato para CT-e modelo 57.
- Identificação do emitente, chave de acesso, protocolo e QR Code.
- Remetente, destinatário, expedidor, recebedor e tomador.
- Carga, componentes do valor, imposto, documentos originários e observações.
- Canhoto inferior e código de barras Code-128C.
- Grupo `IBSCBS` da Reforma Tributária 2026 quando presente no XML.

## Modais suportados

- Rodoviário: RNTRC, CIOT, lotação e data prevista quando informados.
- Aéreo: minuta, conhecimento operacional, tarifa e aeroportos quando informados.
- Aquaviário: navio, AFRMM, balsas e contêineres.
- Ferroviário: tráfego, fluxo, frete e ferrovias envolvidas.
- Dutoviário: data inicial, data final e valor da tarifa.
- Multimodal: COTM, negociável e seguro.

## IBS/CBS - Reforma Tributária 2026

Quando o XML traz o grupo `IBSCBS`, o quadro de imposto usa o layout estendido com IBS, CBS e ICMS. Quando o grupo não existe, o DACTE usa o layout clássico de ICMS.

## Sample

```bash
dotnet run --project Dacte.CTe.Core.Sample/Dacte.CTe.Core.Sample.csproj -- <pasta-xmls> [pasta-saida] [caminho-logo.png]
```

Também é possível informar o logo por variável de ambiente:

```bash
set DACTE_CTE_CORE_SAMPLE_LOGO=C:\logos\minha-empresa.png
dotnet run --project Dacte.CTe.Core.Sample/Dacte.CTe.Core.Sample.csproj -- C:\xmls C:\pdfs
```

## Roadmap

- DACTE OS modelo 67.
- Modo paisagem com canhoto rotacionado.
- Fluxos específicos de contingência FS-DA, EPEC e SVC.

## Build

```bash
dotnet restore
dotnet build Dacte.CTe.Core.sln -c Release
dotnet pack Dacte.CTe.Core/Dacte.CTe.Core.csproj -c Release
```

## Licença

Distribuído sob a licença MIT. Consulte [LICENSE](LICENSE).
