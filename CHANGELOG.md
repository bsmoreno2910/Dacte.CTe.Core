# Changelog

Todas as mudanças relevantes serão documentadas neste arquivo.

## [1.0.3] - 2026-05-12

### Adicionado
- Modo de ajuste do logo com `ModoAjusteLogo.Preencher` e `ModoAjusteLogo.ConterProporcional`.

### Corrigido
- Encaixe proporcional de imagens para evitar distorção quando o modo proporcional for usado.

## [1.0.2] - 2026-05-12

### Corrigido
- Ajuste fino de alinhamento entre o quadro `DADOS DO CT-E` e a coluna do QR Code no layout multimodal.

## [1.0.1] - 2026-05-12

### Corrigido

- QR Code do topo direito respeita a área do quadro e mantém margem interna.
- Quadro `DADOS DO CT-E` no multimodal não invade mais a coluna do QR Code.
- Campos de peso com unidade `KG` mantêm valor e unidade alinhados na mesma linha.
- Título `IDENTIFICAÇÃO DO EMITENTE` fica centralizado quando o DACTE é gerado sem logo.

## [1.0.0] - 2026-05-08

### Adicionado

- Geração de DACTE em PDF para CT-e modelo 57 (`cteProc` v4.00).
- Suporte aos 6 modais: rodoviário, aéreo, aquaviário, ferroviário, dutoviário e multimodal.
- Suporte ao grupo IBS/CBS da Reforma Tributária 2026.
- Multi-target `netstandard2.0` + `net8.0`.
- QR Code do CT-e.
- Code-128C para chave de acesso.
