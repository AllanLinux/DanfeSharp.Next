# DanfeSharp.Next

Biblioteca em C# que gera em PDF os documentos auxiliares dos documentos fiscais eletrônicos brasileiros a partir
do XML autorizado: DANFE da NF-e, DANFE NFC-e, documentos de eventos (carta de correção, cancelamento e outros),
DACTE do CT-e e DANFSe da NFS-e Nacional. Já traz o leiaute da Reforma Tributária, com IBS, CBS e Imposto Seletivo
(NT 2025.002 e NT 2026.010).

A biblioteca só gera o PDF: ela não emite, assina nem transmite documentos. Use-a junto com o sistema que emite as notas.

Este projeto é um fork do [DanfeSharp](https://github.com/SilverCard/DanfeSharp), criado por Ricardo Brito, e é
distribuído como o pacote `DanfeSharp.Next`. Os PDFs são escritos com a biblioteca [PDFsharp](https://www.pdfsharp.net/).

## Documentos suportados

| Documento | Classe | Observações |
|---|---|---|
| DANFE da NF-e (modelo 55) | `DanfeSharp.Danfe` | Retrato e paisagem. Leiaute da Reforma Tributária (NT 2026.010) com IBS, CBS e IS quando a nota os possui |
| DANFE NFC-e (modelo 65) | `DanfeSharp.NFCe.Danfce` | Bobina de 80 mm ou 58 mm, com QR Code. Aceita NFC-e em contingência offline, ainda sem protocolo |
| Eventos da NF-e e da NFC-e | `DanfeSharp.Eventos.DocumentoEvento` | Carta de correção (CC-e), cancelamento, cancelamento por substituição e demais eventos |
| DACTE do CT-e 4.00 (modelo 57) | `DanfeSharp.CTe.Dacte` | Com QR Code, IBS e CBS quando informados e documentos originários em várias folhas |
| DANFSe da NFS-e Nacional | `DanfeSharp.NFSe.Danfse` | Leiaute v2.0 do Sistema Nacional NFS-e, com QR Code e os quadros de ISSQN, tributos federais e IBS/CBS |

## Instalação

O pacote está no [nuget.org](https://www.nuget.org/packages/DanfeSharp.Next):

```
dotnet add package DanfeSharp.Next
```

Ou, no Visual Studio, procure por `DanfeSharp.Next` no Gerenciador de Pacotes NuGet.

Para atualizar um projeto que já usa o pacote, rode o mesmo comando (ele instala a versão mais recente) ou use a aba
**Atualizações** do Gerenciador de Pacotes NuGet. As mudanças de cada versão estão nas
[Releases](https://github.com/AllanLinux/DanfeSharp.Next/releases).

> Atenção: o pacote `DanfeSharp`, sem o `.Next`, é o projeto original de 2017. Ele não tem a Reforma Tributária, a
> DANFSe nem os demais documentos.

### Alternativas pelo GitHub

Cada versão também é publicada no GitHub Packages e anexada às
[Releases](https://github.com/AllanLinux/DanfeSharp.Next/releases) do repositório.

**GitHub Packages:** o GitHub exige autenticação até para pacotes públicos. Crie um token clássico em
*Settings → Developer settings → Personal access tokens* com a permissão `read:packages` e registre a fonte uma vez:

```
dotnet nuget add source https://nuget.pkg.github.com/AllanLinux/index.json -n github-allanlinux -u SEU_USUARIO_GITHUB -p SEU_TOKEN
dotnet add package DanfeSharp.Next
```

No Linux e no macOS, acrescente `--store-password-in-clear-text` ao primeiro comando.

**Release, sem token:** baixe o `.nupkg` da página de Releases para uma pasta e use-a como fonte:

```
dotnet nuget add source C:\pacotes -n pacotes-locais
dotnet add package DanfeSharp.Next
```

## Plataformas

| Plataforma | Versão do PDFsharp | Fontes |
|---|---|---|
| .NET Framework 4.5.2 ou superior | 1.50 | Times New Roman instalada no Windows |
| .NET 8 ou superior (Windows, Linux e macOS) | 6.2.4 ou superior (veja abaixo) | Times New Roman ou Liberation Serif instaladas |

### Versão do PDFsharp no .NET 8+

O pacote instala o PDFsharp 6.2.4. A partir da versão 6.5.0, o PDFsharp passou a ser distribuído sob a **AGPL-3.0** e
a verificar a licença ao gerar o PDF.

| PDFsharp | Licença | Funciona com o DanfeSharp.Next |
|---|---|---|
| 6.0 a 6.2.3 | MIT | Não: o pacote exige no mínimo a 6.2.4. Atualize para a 6.2.4 |
| **6.2.4** (recomendada) | **MIT** | Sim |
| 6.5.0 ou superior | **AGPL-3.0** ou licença comercial da empira | Sim, desde que a licença seja declarada antes de gerar o primeiro documento |

Os testes geram os mesmos PDFs com o PDFsharp 6.2.0, 6.2.4 e 6.5.0. A diferença da 6.5.0 é só de licença: sem a
declaração, a geração falha com `PdfSharpLicenseException`.

```csharp
// Somente com o PDFsharp 6.5.0 ou superior, uma vez no início do programa:
PdfSharp.PdfSharpLicense.SetAgplLicense();          // uso sob a AGPL (exige abrir o código do sistema)
// ou
PdfSharp.PdfSharpLicense.LoadLicense("sua-chave");  // licença comercial comprada da empira
```

Em sistemas comerciais de código fechado, fixe a versão MIT no seu projeto, para que nenhuma atualização leve à 6.5.0
sem você perceber:

```xml
<PackageReference Include="PDFsharp" Version="[6.2.4]" />
```

O .NET Framework usa o PDFsharp 1.50 (MIT) e não é afetado.

No Linux, instale a Liberation Serif, que tem as mesmas métricas da Times New Roman (pacote `fonts-liberation` no
Debian/Ubuntu). Também é possível informar os arquivos da fonte antes de gerar o primeiro documento, o que é útil em
contêineres sem fontes instaladas:

```csharp
DanfeSharp.Fontes.Registrar("serif-regular.ttf", "serif-negrito.ttf", "serif-italico.ttf");
```

## Como usar

Todos os documentos seguem o mesmo padrão:

1. criar o modelo a partir do XML, com o leitor (`...ViewModelCreator`) do documento;
2. ajustar as opções do modelo, se quiser;
3. criar o documento, adicionar o logotipo (opcional), chamar `Gerar()` e `Salvar(...)`.

O modelo pode ser criado de um arquivo (`CriarDeArquivoXml(caminho)`), de um `Stream` (`CriarDeArquivoXml(stream)`)
ou de uma string (`CriarDeStringXml(xml)`). Para os eventos, só há as versões com caminho e com string. O documento
implementa `IDisposable`, então use-o dentro de um `using`.

### Qual classe usar para cada XML

Cada documento tem o seu leitor e o seu gerador. Os nomes do DANFE e da DANFSe diferem só por um "s"; usar o leitor
de um com o gerador do outro causa o erro de compilação "cannot convert from `DanfeViewModel` to `DanfseViewModel`".

| XML (elemento raiz) | Leitor | Gerador | Namespace |
|---|---|---|---|
| NF-e autorizada (`nfeProc`, modelo 55) | `DanfeViewModelCreator` | `Danfe` | `DanfeSharp.Modelo` e `DanfeSharp` |
| NFC-e (`nfeProc` ou `NFe`, modelo 65) | `DanfceViewModelCreator` | `Danfce` | `DanfeSharp.NFCe` |
| Evento processado (`procEventoNFe`) | `EventoViewModelCreator` | `DocumentoEvento` | `DanfeSharp.Eventos` |
| CT-e autorizado (`cteProc`) | `DacteViewModelCreator` | `Dacte` | `DanfeSharp.CTe` |
| NFS-e Nacional (`NFSe`) | `DanfseViewModelCreator` | `Danfse` | `DanfeSharp.NFSe` |

### Identificar o tipo de XML automaticamente

Quando o sistema recebe XMLs de vários tipos, o namespace e o elemento raiz indicam qual documento gerar:

```csharp
using System.Xml;
using DanfeSharp;
using DanfeSharp.CTe;
using DanfeSharp.Modelo;
using DanfeSharp.NFCe;
using DanfeSharp.NFSe;

static void GerarPdf(string xmlPath, string pdfPath)
{
    var xml = new XmlDocument();
    xml.Load(xmlPath);

    var raiz = xml.DocumentElement;
    string ns = raiz?.NamespaceURI ?? "";
    string modelo = xml.GetElementsByTagName("mod", "http://www.portalfiscal.inf.br/nfe").Item(0)?.InnerText;

    DocumentoAuxiliar documento;

    if (ns == "http://www.sped.fazenda.gov.br/nfse")
        documento = new Danfse(DanfseViewModelCreator.CriarDeArquivoXml(xmlPath));
    else if (ns == "http://www.portalfiscal.inf.br/cte")
        documento = new Dacte(DacteViewModelCreator.CriarDeArquivoXml(xmlPath));
    else if (ns == "http://www.portalfiscal.inf.br/nfe" && modelo == "65")
        documento = new Danfce(DanfceViewModelCreator.CriarDeArquivoXml(xmlPath));
    else if (ns == "http://www.portalfiscal.inf.br/nfe" && raiz.LocalName != "procEventoNFe")
        documento = new Danfe(DanfeViewModelCreator.CriarDeArquivoXml(xmlPath));
    else
        throw new NotSupportedException($"XML não suportado: {raiz?.LocalName} ({ns})");

    using (documento)
    {
        documento.Gerar();
        documento.Salvar(pdfPath);
    }
}
```

Para eventos (`procEventoNFe`), use o `EventoViewModelCreator`, que pode receber também o XML da NF-e.

### DANFE da NF-e

```csharp
using DanfeSharp;
using DanfeSharp.Modelo;

var modelo = DanfeViewModelCreator.CriarDeArquivoXml("nfe.xml");

using (var danfe = new Danfe(modelo))
{
    danfe.AdicionarLogoImagem("logo.jpg");
    danfe.Gerar();
    danfe.Salvar("danfe.pdf");
}
```

O XML deve ser a NF-e autorizada (`nfeProc`). A orientação (retrato ou paisagem) vem do campo `tpImp` do XML e pode
ser trocada em `modelo.Orientacao`.

#### Opções do DANFE

| Propriedade de `DanfeViewModel` | Padrão | Efeito |
|---|---|---|
| `Orientacao` | do XML | `Orientacao.Retrato` ou `Orientacao.Paisagem` |
| `ExibirLeiauteReformaTributaria` | automático | `null` usa o leiaute da NT 2026.010 só quando a nota tem IBS, CBS ou IS; `true` ou `false` força |
| `PreferirEmitenteNomeFantasia` | `true` | Mostra o nome fantasia do emitente no lugar da razão social |
| `ExibirPisConfins` | `true` | Exibe os valores de PIS e COFINS |
| `ExibirIcmsInterestadual` | `true` | Exibe os valores de ICMS interestadual (DIFAL) |
| `ExibirBlocoLocalEntrega` | `true` | Exibe o quadro do local de entrega, quando houver |
| `ExibirBlocoLocalRetirada` | `true` | Exibe o quadro do local de retirada, quando houver |

O modelo também pode ser montado sem XML, preenchendo `DanfeViewModel`, `EmpresaViewModel`, `ProdutoViewModel` e as
demais classes do namespace `DanfeSharp.Modelo`.

### DANFE NFC-e

```csharp
using DanfeSharp.NFCe;

var modelo = DanfceViewModelCreator.CriarDeArquivoXml("nfce.xml");
modelo.LarguraBobina = LarguraBobina.Mm58; // padrão: Mm80

using (var danfce = new Danfce(modelo))
{
    danfce.AdicionarLogoImagem("logo.png");
    danfce.Gerar();
    danfce.Salvar("danfce.pdf");
}
```

O documento sai em uma única página contínua, na largura da bobina, pronta para impressoras térmicas. O XML pode ser a
NFC-e autorizada (`nfeProc`) ou a NFC-e emitida em contingência offline (`NFe`, ainda sem protocolo).

### Carta de correção, cancelamento e outros eventos

```csharp
using DanfeSharp.Eventos;

// O XML da NF-e é opcional e serve para exibir emitente, destinatário e data de emissão.
var modelo = EventoViewModelCreator.CriarDeArquivoXml("procEventoNFe.xml", "nfe.xml");

using (var evento = new DocumentoEvento(modelo))
{
    evento.Gerar();
    evento.Salvar("cce.pdf");
}
```

O XML deve ser o evento processado (`procEventoNFe`). Eventos sem leiaute próprio mostram os campos do evento como
"campo: valor".

### DACTE

```csharp
using DanfeSharp.CTe;

var modelo = DacteViewModelCreator.CriarDeArquivoXml("cte.xml");

using (var dacte = new Dacte(modelo))
{
    dacte.AdicionarLogoImagem("logo.jpg");
    dacte.Gerar();
    dacte.Salvar("dacte.pdf");
}
```

O XML deve ser o CT-e autorizado (`cteProc`). Quando os documentos originários não cabem na primeira folha, eles
continuam nas folhas seguintes.

### DANFSe (NFS-e Nacional)

```csharp
using DanfeSharp.NFSe;

var modelo = DanfseViewModelCreator.CriarDeArquivoXml("nfse.xml");

using (var danfse = new Danfse(modelo))
{
    danfse.Gerar();
    danfse.Salvar("danfse.pdf");
}
```

O XML deve ser a NFS-e gerada pelo Sistema Nacional NFS-e (`NFSe`, com a DPS dentro). O leiaute segue a DANFSe v2.0
oficial: grade de 4 colunas, QR Code de consulta pública e os quadros de prestador, tomador, destinatário, intermediário,
serviço, ISSQN, tributos federais, IBS/CBS e valor total. Os nomes dos municípios vêm da tabela do IBGE embutida na
biblioteca, pois a NFS-e informa só o código. NFS-e de padrões municipais (ABRASF e outros) não são suportadas.

Como a DANFSe é preenchida:

- Todos os valores, alíquotas e bases vêm do XML: os da DPS, informados pelo emitente, e os da NFS-e, calculados pelo
  Sistema Nacional. A biblioteca não tem alíquotas no código, então um XML antigo é impresso exatamente como foi
  emitido, mesmo que as alíquotas mudem depois.
- Só dois valores são calculados, para exibição: **Exclusões e Reduções da Base de Cálculo** (valor do serviço menos a
  base do IBS/CBS) e **Total do IBS/CBS** (IBS mais CBS). Na transição da Reforma Tributária, as exclusões são o ISSQN,
  o PIS e a COFINS.
- A linha do **Benefício Municipal** no quadro do ISSQN só aparece quando a nota tem benefício municipal (`tpBM` ou o
  grupo `BM` da DPS) ou deduções da base, como na DANFSe oficial.
- Valores longos, como endereços, continuam nas linhas de baixo (até 3) e empurram o restante da página, como na
  DANFSe oficial. A descrição do código de tributação nacional mostra só a primeira linha, também como na oficial.

### Logotipo

Todos os documentos aceitam o logotipo em imagem (`AdicionarLogoImagem`, JPG ou PNG) ou em PDF (`AdicionarLogoPdf`,
que mantém o desenho vetorial), por caminho de arquivo ou por `Stream`.

### Assinatura no rodapé

Os documentos em A4 (DANFE, eventos, DACTE e DANFSe) imprimem "Impresso com DanfeSharp.Next" no rodapé. Para não
imprimir, desligue antes de gerar:

```csharp
using (var danfe = new Danfe(modelo))
{
    danfe.ExibirAssinatura = false; // padrão: true
    danfe.Gerar();
    danfe.Salvar("danfe.pdf");
}
```

A opção vale para todos os documentos (`Danfe`, `DocumentoEvento`, `Dacte` e `Danfse`). O DANFE NFC-e não tem essa
assinatura.

### Gerar em memória (APIs web)

`Salvar(Stream)` grava o PDF sem fechar o stream, o que permite devolvê-lo direto em uma API:

```csharp
// ASP.NET Core
app.MapPost("/danfe", (HttpRequest request) =>
{
    var modelo = DanfeViewModelCreator.CriarDeArquivoXml(request.Body);
    var pdf = new MemoryStream();

    using (var danfe = new Danfe(modelo))
    {
        danfe.Gerar();
        danfe.Salvar(pdf);
    }

    pdf.Position = 0;
    return Results.File(pdf, "application/pdf", "danfe.pdf");
});
```

### Erros

- XML malformado ou de outro tipo de documento: `XmlException` ou `Exception` com a linha e a posição do erro.
- NFC-e passada ao `DanfeViewModelCreator`: `NotSupportedException`, indicando o uso do `Danfce`.
- XML que não é da NFS-e Nacional passado ao `DanfseViewModelCreator` (por exemplo, só a DPS ou um XML de padrão
  municipal): `XmlException`.
- Forma de emissão não suportada pelo DANFE: `NotSupportedException`.

## Compilar a partir do código

```
dotnet build DanfeSharp.sln
dotnet test DanfeSharp.Test/DanfeSharp.Test.csproj
dotnet pack DanfeSharp/DanfeSharp.csproj -c Release -o nupkgs
```

Os testes geram os PDFs de exemplo na pasta `Output` dentro de `DanfeSharp.Test/bin`, a partir dos XMLs fictícios em
`DanfeSharp.Test/Xml`.

### Publicar uma nova versão

A publicação é feita pelo workflow `.github/workflows/publicar.yml` a cada tag `v*`:

1. aumente a `<Version>` em `DanfeSharp/DanfeSharp.csproj` e faça o merge na `dev`;
2. crie e envie a tag com o mesmo número, por exemplo `git tag -a v3.2.4 -m "DanfeSharp.Next 3.2.4"` e
   `git push origin v3.2.4`.

O workflow compila, roda os testes e publica no nuget.org (Trusted Publishing, sem API key guardada), no GitHub
Packages e em uma Release. Um número de versão publicado no nuget.org não pode ser reutilizado.

## Licença

MIT. Veja [LICENSE.txt](LICENSE.txt), que mantém o aviso de copyright do projeto original.

Dependências e suas licenças, nas versões instaladas pelo pacote:

| Dependência | Versão | Licença |
|---|---|---|
| PDFsharp (.NET 8+) | 6.2.4 | MIT (a partir da 6.5.0, AGPL-3.0; veja "Versão do PDFsharp no .NET 8+") |
| PDFsharp (.NET Framework) | 1.50.5147 | MIT |
| QRCoder | 1.6.0 | MIT |
| System.Security.Cryptography.Pkcs (.NET 8+) | 8.0.2 | MIT |
