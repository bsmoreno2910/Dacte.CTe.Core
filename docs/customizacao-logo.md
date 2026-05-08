# Customizacao de Logo

O logo e opcional. Se nao for informado, o DACTE e gerado normalmente sem imagem.

## Via path

```csharp
using (var dacte = new DacteDoc(modelo))
{
    dacte.AdicionarLogoImagem("logo.png");
    dacte.Gerar();
    dacte.Salvar("dacte.pdf");
}
```

## Via stream

```csharp
using (var logo = File.OpenRead("logo.png"))
using (var dacte = new DacteDoc(modelo))
{
    dacte.AdicionarLogoImagem(logo);
    dacte.Gerar();
    dacte.Salvar("dacte.pdf");
}
```

## Logo em PDF

```csharp
using (var logo = File.OpenRead("logo.pdf"))
using (var dacte = new DacteDoc(modelo))
{
    dacte.AdicionarLogoPdf(logo);
    dacte.Gerar();
    dacte.Salvar("dacte.pdf");
}
```
