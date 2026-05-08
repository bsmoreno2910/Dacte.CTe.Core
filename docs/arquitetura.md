# Arquitetura

O fluxo principal e:

```mermaid
flowchart LR
    A["XML cteProc v4.00"] --> B["Esquemas"]
    B --> C["DacteViewModelCreator"]
    C --> D["DacteViewModel"]
    D --> E["DacteRenderer"]
    E --> F["PDF DACTE"]
```

## Camadas

- `Esquemas`: classes de desserializacao do XML do CT-e.
- `Modelo`: ViewModels usados pelo renderer.
- `Render`: desenho final em PDF usando PdfSharpCore.
- `Sample`: console simples para gerar PDFs a partir de uma pasta de XMLs.
